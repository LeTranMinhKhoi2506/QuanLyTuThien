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
                    Status = "pending_approval", // Changed to 'pending_approval' per user request
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                _context.Add(campaign);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = "Chiến dịch đã được tạo thành công và đang chờ duyệt!";
                return RedirectToAction("Index", "TrangChu"); // Redirect to Home
            }
            
            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "Name", model.CategoryId);
            return View(model);
        }
    }
}
