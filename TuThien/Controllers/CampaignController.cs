using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TuThien.Models;

namespace TuThien.Controllers
{
    public class CampaignController : Controller
    {
        private readonly TuThienContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public CampaignController(TuThienContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: Campaign/Index
        public async Task<IActionResult> Index()
        {
            var categories = await _context.Categories.ToListAsync();
            return View(categories);
        }

        // GET: Campaign/GetCampaigns
        public async Task<IActionResult> GetCampaigns(int? categoryId)
        {
            var query = _context.Campaigns
                .Where(c => c.Status == "active" || c.Status == "approved") // Show active/approved campaigns
                .Include(c => c.Category)
                .AsQueryable();

            if (categoryId.HasValue)
            {
                query = query.Where(c => c.CategoryId == categoryId.Value);
            }

            // Sort by Amount Donated (High to Low)
            var campaigns = await query.OrderByDescending(c => c.CurrentAmount).ToListAsync();

            return PartialView("_CampaignListPartial", campaigns);
        }

        // GET: Campaign/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var campaign = await _context.Campaigns
                .Include(c => c.Category)
                .Include(c => c.Creator)
                .Include(c => c.Donations)
                    .ThenInclude(d => d.User)
                .Include(c => c.CampaignUpdates)
                .Include(c => c.CampaignMilestones)
                .Include(c => c.CampaignDocuments)
                .Include(c => c.Comments)
                    .ThenInclude(c => c.User)
                .FirstOrDefaultAsync(c => c.CampaignId == id);

            if (campaign == null)
            {
                return NotFound();
            }

            return View(campaign);
        }

        // GET: Campaign/Create
        public IActionResult Create()
        {
            // Check Session for Login
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account", new { returnUrl = "/Campaign/Create" });
            }

            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "Name");
            return View();
        }

        // POST: Campaign/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CampaignCreateViewModel model)
        {
            // Check Session for Login
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (ModelState.IsValid)
            {
                // Handle Image Upload
                string? thumbnailUrl = null;
                if (model.ThumbnailImage != null)
                {
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "campaigns");
                    // Ensure directory exists
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + model.ThumbnailImage.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.ThumbnailImage.CopyToAsync(fileStream);
                    }
                    
                    thumbnailUrl = "/images/campaigns/" + uniqueFileName;

                }



                // Map to Entity
                var campaign = new Campaign
                {
                    Title = model.Title,
                    Description = model.Description,
                    TargetAmount = model.TargetAmount,
                    CurrentAmount = 0,
                    StartDate = model.StartDate,
                    EndDate = model.EndDate,
                    CategoryId = model.CategoryId,
                    ExcessFundOption = model.ExcessFundOption,
                    CreatorId = userId.Value,

                    ThumbnailUrl = thumbnailUrl ?? "/images/default-campaign.jpg", // Fallback image
                    Status = "pending_approval",
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                // Validate Phases if enabled
                if (model.IsPhased)
                {
                    if (model.Milestones == null || !model.Milestones.Any())
                    {
                        ModelState.AddModelError("IsPhased", "Vui lòng thêm ít nhất một giai đoạn.");
                        ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "Name", model.CategoryId);
                        return View(model);
                    }

                    if (model.Milestones.Sum(m => m.AmountNeeded) != model.TargetAmount)
                    {
                        ModelState.AddModelError("TargetAmount", "Tổng tiền các giai đoạn phải bằng số tiền mục tiêu.");
                        ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "Name", model.CategoryId);
                        return View(model);
                    }
                    
                    for (int i = 0; i < model.Milestones.Count; i++)
                    {
                        if (model.Milestones[i].Deadline > model.EndDate)
                        {
                            ModelState.AddModelError($"Milestones[{i}].Deadline", $"Giai đoạn {i + 1} kết thúc sau ngày kết thúc chiến dịch.");
                        }
                        if (i > 0 && model.Milestones[i].Deadline < model.Milestones[i - 1].Deadline)
                        {
                            ModelState.AddModelError($"Milestones[{i}].Deadline", $"Giai đoạn {i + 1} phải kết thúc sau giai đoạn {i}.");
                        }
                    }

                    if (!ModelState.IsValid)
                    {
                        ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "Name", model.CategoryId);
                        return View(model);
                    }
                }

                _context.Add(campaign);
                await _context.SaveChangesAsync();

                // Save Milestones if Valid
                if (model.IsPhased && model.Milestones != null)
                {
                    foreach (var milestone in model.Milestones)
                    {
                        var campaignMilestone = new CampaignMilestone
                        {
                            CampaignId = campaign.CampaignId,
                            Title = milestone.Title,
                            AmountNeeded = milestone.AmountNeeded,
                            Deadline = milestone.Deadline,
                            Status = "pending"
                        };
                        _context.Add(campaignMilestone);
                    }
                    await _context.SaveChangesAsync();
                }


                // Handle Multiple Verification Documents
                if (model.VerificationDocuments != null && model.VerificationDocuments.Count > 0)
                {
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "verification-docs");
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                    for (int i = 0; i < model.VerificationDocuments.Count; i++)
                    {
                        var file = model.VerificationDocuments[i];
                        if (file.Length > 0)
                        {
                            string uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
                            string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                            using (var fileStream = new FileStream(filePath, FileMode.Create))
                            {
                                await file.CopyToAsync(fileStream);
                            }

                            var description = (model.VerificationDocDescriptions != null && model.VerificationDocDescriptions.Count > i) 
                                            ? model.VerificationDocDescriptions[i] 
                                            : "Tài liệu xác nhận";

                            var campaignDoc = new CampaignDocument
                            {
                                CampaignId = campaign.CampaignId,
                                FileUrl = "/uploads/verification-docs/" + uniqueFileName,
                                FileType = file.ContentType.Contains("pdf") ? "pdf" : "image",
                                Description = description
                            };
                            _context.Add(campaignDoc);
                        }
                    }
                    await _context.SaveChangesAsync();
                }
                
                TempData["SuccessMessage"] = "Chiến dịch đã được tạo thành công và đang chờ duyệt!";
                return RedirectToAction("Index", "TrangChu"); // Redirect to Home
            }
            
            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "Name", model.CategoryId);
            return View(model);
        }
    }
}
