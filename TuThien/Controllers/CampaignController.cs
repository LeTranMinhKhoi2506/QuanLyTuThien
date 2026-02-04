using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.Json;
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
        public async Task<IActionResult> Index(int? categoryId)
        {
            var categories = await _context.Categories.ToListAsync();
            ViewBag.SelectedCategoryId = categoryId;
            return View(categories);
        }

        // GET: Campaign/GetCampaigns - Lấy campaigns theo category (dùng cho AJAX/Partial View)
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

        // GET: Campaign/ByCategory/1 - Trang hiển thị tất cả campaigns theo category với phân trang
        [HttpGet("campaigns/category/{id}")]
        public async Task<IActionResult> ByCategory(int id, int page = 1, int pageSize = 12)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }

            var query = _context.Campaigns
                .Where(c => c.CategoryId == id && c.Status == "active")
                .Include(c => c.Category)
                .Include(c => c.Creator)
                .OrderByDescending(c => c.CreatedAt);

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var campaigns = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Category = category;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;

            return View(campaigns);
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

            // Create a new instance of the view model instead of passing null
            var model = new CampaignCreateViewModel
            {
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddMonths(1)
            };
            return View(model);
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

            // 1. Logic Fix: Nếu không chọn "Chia giai đoạn", bỏ qua validate của Milestones
            if (!model.IsPhased)
            {
                var milestoneKeys = ModelState.Keys.Where(k => k.StartsWith("Milestones")).ToList();
                foreach (var key in milestoneKeys)
                {
                    ModelState.Remove(key);
                }
                model.Milestones.Clear();
            }

            // 2. Logic Validation: Ngày kết thúc phải sau ngày bắt đầu
            if (model.EndDate <= model.StartDate)
            {
                ModelState.AddModelError("EndDate", "Ngày kết thúc phải lớn hơn ngày bắt đầu.");
            }
            if (model.StartDate < DateTime.Today) 
            {
                 ModelState.AddModelError("StartDate", "Ngày bắt đầu không được nhỏ hơn ngày hiện tại.");
            }

            // 3. Logic Validation: Milestones
            if (model.IsPhased && model.Milestones != null)
            {
                decimal totalMilestoneAmount = 0;
                for (int i = 0; i < model.Milestones.Count; i++)
                {
                    var m = model.Milestones[i];
                    totalMilestoneAmount += m.AmountNeeded;
                    
                    // a. Kiểm tra ngày kết thúc mốc phải nằm trong khoảng thời gian chiến dịch
                    if (m.Deadline <= model.StartDate)
                    {
                        ModelState.AddModelError($"Milestones[{i}].Deadline", $"Giai đoạn {i+1}: Ngày kết thúc phải sau ngày bắt đầu chiến dịch ({model.StartDate:dd/MM/yyyy}).");
                    }
                    if (m.Deadline > model.EndDate)
                    {
                        ModelState.AddModelError($"Milestones[{i}].Deadline", $"Giai đoạn {i+1}: Ngày kết thúc không được vượt quá ngày kết thúc chiến dịch ({model.EndDate:dd/MM/yyyy}).");
                    }

                    // b. Kiểm tra thứ tự thời gian giữa các mốc
                    if (i > 0)
                    {
                        var prevMilestone = model.Milestones[i - 1];
                        if (m.Deadline <= prevMilestone.Deadline)
                        {
                            ModelState.AddModelError($"Milestones[{i}].Deadline", $"Giai đoạn {i+1}: Ngày kết thúc phải sau giai đoạn {i} ({prevMilestone.Deadline:dd/MM/yyyy}).");
                        }
                    }
                }

                // c. Kiểm tra tổng tiền
                if (totalMilestoneAmount != model.TargetAmount)
                {
                    ModelState.AddModelError("TargetAmount", $"Tổng tiền các giai đoạn ({totalMilestoneAmount:N0}) phải bằng số tiền mục tiêu ({model.TargetAmount:N0}).");
                }
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

                // Handle Milestones (Phases)
                if (model.IsPhased && model.Milestones != null && model.Milestones.Count > 0)
                {
                    foreach (var m in model.Milestones)
                    {
                        var milestone = new CampaignMilestone
                        {
                            Title = m.Title,
                            AmountNeeded = m.AmountNeeded,
                            Deadline = m.Deadline,
                            Status = "pending"
                        };
                        campaign.CampaignMilestones.Add(milestone);
                    }
                }

                _context.Add(campaign);
                await _context.SaveChangesAsync();

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
            // Debug: Log errors if model is invalid
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            TempData["ErrorMessage"] = "Có lỗi xảy ra: " + string.Join("; ", errors);

            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "Name", model.CategoryId);
            return View(model);
        }

        // GET: /my-campaigns
        [HttpGet]
        [Route("my-campaigns")]
        public async Task<IActionResult> MyCampaigns()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account", new { returnUrl = "/my-campaigns" });
            }

            var campaigns = await _context.Campaigns
                .Include(c => c.Category)
                .Include(c => c.Donations)
                .Where(c => c.CreatorId == userId.Value)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            // Thống kê
            ViewBag.TotalCampaigns = campaigns.Count;
            ViewBag.ActiveCampaigns = campaigns.Count(c => c.Status == "active");
            ViewBag.TotalRaised = campaigns.Sum(c => c.CurrentAmount ?? 0);
            ViewBag.TotalDonations = campaigns.Sum(c => c.Donations?.Count ?? 0);

            return View(campaigns);
        }

        // POST: Campaign/ReportCampaign
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReportCampaign(int campaignId, string reason)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập để báo cáo sai phạm." });
            }

            if (string.IsNullOrWhiteSpace(reason))
            {
                return Json(new { success = false, message = "Vui lòng nhập lý do báo cáo." });
            }

            // Kiểm tra chiến dịch tồn tại
            var campaign = await _context.Campaigns.FindAsync(campaignId);
            if (campaign == null)
            {
                return Json(new { success = false, message = "Chiến dịch không tồn tại." });
            }

            // Kiểm tra người dùng đã báo cáo chiến dịch này chưa
            var existingReport = await _context.Reports
                .FirstOrDefaultAsync(r => r.ReporterId == userId.Value 
                                       && r.TargetId == campaignId 
                                       && r.TargetType == "campaign"
                                       && r.Status == "pending");
            if (existingReport != null)
            {
                return Json(new { success = false, message = "Bạn đã báo cáo chiến dịch này rồi. Vui lòng chờ Admin xử lý." });
            }

            // Tạo báo cáo mới
            var report = new Report
            {
                ReporterId = userId.Value,
                TargetId = campaignId,
                TargetType = "campaign",
                Reason = reason.Trim(),
                Status = "pending",
                CreatedAt = DateTime.Now
            };

            _context.Reports.Add(report);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Báo cáo đã được gửi thành công. Admin sẽ xem xét và xử lý." });
        }

        // GET: Campaign/MyDetails/5 - Xem chi tiết chiến dịch của người tạo
        [HttpGet]
        public async Task<IActionResult> MyDetails(int id)
        {
            // Check if user is logged in
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account", new { returnUrl = $"/Campaign/MyDetails/{id}" });
            }

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

            // Check if the user is the creator of this campaign
            if (campaign.CreatorId != userId)
            {
                TempData["ErrorMessage"] = "Bạn không có quyền xem trang quản lý chiến dịch này.";
                return RedirectToAction("Details", new { id = id });
            }

            return View(campaign);
        }

        // GET: Campaign/CreateUpdate/5 - Tạo tin tức cho chiến dịch
        [HttpGet]
        public async Task<IActionResult> CreateUpdate(int id)
        {
            // Check if user is logged in
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account", new { returnUrl = $"/Campaign/CreateUpdate/{id}" });
            }

            var campaign = await _context.Campaigns
                .Include(c => c.Creator)
                .FirstOrDefaultAsync(c => c.CampaignId == id);

            if (campaign == null)
            {
                return NotFound();
            }

            // Check if the user is the creator of this campaign
            if (campaign.CreatorId != userId)
            {
                TempData["ErrorMessage"] = "Anda tidak memiliki izin untuk membuat berita untuk kampanye ini.";
                return RedirectToAction("MyDetails", new { id = id });
            }

            var viewModel = new CampaignUpdateViewModel
            {
                CampaignId = id,
                CampaignTitle = campaign.Title,
                CampaignThumbnail = campaign.ThumbnailUrl,
                Type = "general"
            };

            return View(viewModel);
        }

        // POST: Campaign/CreateUpdate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUpdate(CampaignUpdateViewModel model)
        {
            // Check if user is logged in
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var campaign = await _context.Campaigns.FindAsync(model.CampaignId);
            if (campaign == null)
            {
                return NotFound();
            }

            // Check if the user is the creator of this campaign
            if (campaign.CreatorId != userId)
            {
                TempData["ErrorMessage"] = "Bạn không có quyền tạo tin tức cho chiến dịch này.";
                return RedirectToAction("MyDetails", new { id = model.CampaignId });
            }

            if (ModelState.IsValid)
            {
                // Handle image uploads
                List<string> imageUrls = new List<string>();
                
                if (model.Images != null && model.Images.Count > 0)
                {
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "campaign-updates");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    foreach (var image in model.Images)
                    {
                        if (image.Length > 0)
                        {
                            string uniqueFileName = Guid.NewGuid().ToString() + "_" + image.FileName;
                            string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                            
                            using (var fileStream = new FileStream(filePath, FileMode.Create))
                            {
                                await image.CopyToAsync(fileStream);
                            }
                            
                            imageUrls.Add("/uploads/campaign-updates/" + uniqueFileName);
                        }
                    }
                }

                // Create the update
                var campaignUpdate = new CampaignUpdate
                {
                    CampaignId = model.CampaignId,
                    AuthorId = userId.Value,
                    Title = model.Title.Trim(),
                    Content = model.Content.Trim(),
                    Type = model.Type,
                    ImageUrls = imageUrls.Count > 0 ? JsonSerializer.Serialize(imageUrls) : null,
                    CreatedAt = DateTime.Now
                };

                _context.CampaignUpdates.Add(campaignUpdate);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Đã đăng tin tức thành công!";
                return RedirectToAction("MyDetails", new { id = model.CampaignId });
            }

            // If we got here, something failed, redisplay form
            var campaignInfo = await _context.Campaigns.FindAsync(model.CampaignId);
            model.CampaignTitle = campaignInfo?.Title;
            model.CampaignThumbnail = campaignInfo?.ThumbnailUrl;

            return View(model);
        }

        // GET: Campaign/Edit/5 - Chỉnh sửa chiến dịch (cần admin duyệt)
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            // Check if user is logged in
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account", new { returnUrl = $"/Campaign/Edit/{id}" });
            }

            var campaign = await _context.Campaigns
                .Include(c => c.Category)
                .Include(c => c.Creator)
                .FirstOrDefaultAsync(c => c.CampaignId == id);

            if (campaign == null)
            {
                return NotFound();
            }

            // Check if the user is the creator of this campaign
            if (campaign.CreatorId != userId)
            {
                TempData["ErrorMessage"] = "Bạn không có quyền chỉnh sửa chiến dịch này.";
                return RedirectToAction("MyDetails", new { id = id });
            }

            // Check if there's a pending edit request
            var hasPending = await _context.Database
                .SqlQueryRaw<int>($"SELECT COUNT(*) as Value FROM CampaignEditRequests WHERE campaign_id = {id} AND status = 'pending'")
                .FirstOrDefaultAsync();

            if (hasPending > 0)
            {
                TempData["InfoMessage"] = "Hiện có yêu cầu chỉnh sửa đang chờ admin phê duyệt. Vui lòng đợi xử lý trước khi gửi yêu cầu mới.";
            }

            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "Name", campaign.CategoryId);

            return View(campaign);
        }

        // POST: Campaign/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int campaignId, string title, string description, decimal targetAmount, 
            int? categoryId, DateTime? startDate, DateTime? endDate, string? excessFundOption, 
            string changeNote, IFormFile? newThumbnailImage)
        {
            // Check if user is logged in
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var campaign = await _context.Campaigns.FindAsync(campaignId);
            if (campaign == null)
            {
                return NotFound();
            }

            // Check if the user is the creator of this campaign
            if (campaign.CreatorId != userId)
            {
                TempData["ErrorMessage"] = "Bạn không có quyền chỉnh sửa chiến dịch này.";
                return RedirectToAction("MyDetails", new { id = campaignId });
            }

            // Validation
            if (string.IsNullOrWhiteSpace(title) || title.Length < 10)
            {
                TempData["ErrorMessage"] = "Tiêu đề phải có ít nhất 10 ký tự.";
                return RedirectToAction("Edit", new { id = campaignId });
            }

            if (string.IsNullOrWhiteSpace(description) || description.Length < 50)
            {
                TempData["ErrorMessage"] = "Mô tả phải có ít nhất 50 ký tự.";
                return RedirectToAction("Edit", new { id = campaignId });
            }

            if (targetAmount < 100000)
            {
                TempData["ErrorMessage"] = "Số tiền mục tiêu phải ít nhất là 100,000 VNĐ.";
                return RedirectToAction("Edit", new { id = campaignId });
            }

            if (string.IsNullOrWhiteSpace(changeNote) || changeNote.Length < 20)
            {
                TempData["ErrorMessage"] = "Ghi chú thay đổi phải có ít nhất 20 ký tự.";
                return RedirectToAction("Edit", new { id = campaignId });
            }

            // Handle image upload if provided
            string? thumbnailUrl = null;
            if (newThumbnailImage != null)
            {
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "campaigns");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                string uniqueFileName = Guid.NewGuid().ToString() + "_" + newThumbnailImage.FileName;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await newThumbnailImage.CopyToAsync(fileStream);
                }

                thumbnailUrl = "/images/campaigns/" + uniqueFileName;
            }

            // Create edit request using raw SQL
            await _context.Database.ExecuteSqlRawAsync(@"
                INSERT INTO CampaignEditRequests 
                (campaign_id, requester_id, title, description, target_amount, category_id, start_date, end_date, thumbnail_url, excess_fund_option, change_note, status, created_at)
                VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, 'pending', GETDATE())",
                campaignId, userId.Value, title, description, targetAmount, categoryId, startDate, endDate, 
                thumbnailUrl ?? campaign.ThumbnailUrl, excessFundOption ?? campaign.ExcessFundOption, changeNote);

            TempData["SuccessMessage"] = "Đã gửi yêu cầu chỉnh sửa! Admin sẽ xem xét và phê duyệt thay đổi của bạn.";
            return RedirectToAction("MyDetails", new { id = campaignId });
        }
    }
}
