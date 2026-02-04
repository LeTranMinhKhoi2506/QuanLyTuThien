using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TuThien.Models;

namespace TuThien.Controllers
{
    public class TrangChuController : Controller
    {
        private readonly TuThienContext _context;
        
        public TrangChuController(TuThienContext context)
        {
            _context = context;
        }
        
        public async Task<IActionResult> Index()
        {
            // Lấy tất cả categories
            var categories = await _context.Categories
                .OrderBy(c => c.Name)
                .ToListAsync();

            // Lấy top 3 campaigns mới nhất cho mỗi category
            foreach (var category in categories)
            {
                category.Campaigns = await _context.Campaigns
                    .Where(c => c.CategoryId == category.CategoryId && c.Status == "active")
                    .OrderByDescending(c => c.CreatedAt)
                    .Take(3)
                    .ToListAsync();
            }

            // Lấy top 3 tin tức mới nhất
            var latestNews = await _context.CampaignUpdates
                .Include(u => u.Campaign)
                    .ThenInclude(c => c.Category)
                .Include(u => u.Author)
                .Where(u => u.Campaign.Status == "active")
                .OrderByDescending(u => u.CreatedAt)
                .Take(3)
                .ToListAsync();

            ViewBag.LatestNews = latestNews;
            
            return View("TrangChu", categories);
        }
        
        [HttpGet]
        public async Task<IActionResult> FilterCategory(int? categoryId)
        {
            IEnumerable<Category> categories;
            if(categoryId.HasValue && categoryId.Value > 0)
            {
                var category = await _context.Categories
                    .Where(c => c.CategoryId == categoryId.Value)
                    .FirstOrDefaultAsync();
                
                if (category != null)
                {
                    category.Campaigns = await _context.Campaigns
                        .Where(c => c.CategoryId == category.CategoryId && c.Status == "active")
                        .OrderByDescending(c => c.CreatedAt)
                        .Take(3)
                        .ToListAsync();
                    
                    categories = new List<Category> { category };
                }
                else
                {
                    categories = new List<Category>();
                }
            }
            else
            {
                categories = await _context.Categories
                    .OrderBy(c => c.Name)
                    .ToListAsync();

                // Lấy top 3 campaigns mới nhất cho mỗi category
                foreach (var category in categories)
                {
                    category.Campaigns = await _context.Campaigns
                        .Where(c => c.CategoryId == category.CategoryId && c.Status == "active")
                        .OrderByDescending(c => c.CreatedAt)
                        .Take(3)
                        .ToListAsync();
                }
            }
            return PartialView("patiralView", categories);
        }



        public async Task<IActionResult> Create()
        {
            ViewBag.categories = await _context.Categories
                .OrderBy(c => c.Name)
                .ToListAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CampaignCreate model)
        {
            if (!ModelState.IsValid)
            {
                var categories = await _context.Categories
                    .OrderBy(c => c.Name)
                    .ToListAsync();
                return View(model);
            }
            try
            {
                var campaign = new Campaign
                {
                    CategoryId = model.CategoryId,
                    Title = model.Title,
                    Description = model.Description,
                    TargetAmount = model.TargetAmount,
                    StartDate = model.StartDate,
                    EndDate = model.EndDate,
                    ThumbnailUrl = model.ThumbnailUrl,
                    ExcessFundOption = model.ExcessFundOption,
                    Status = model.Status,

                    CreatorId = 3,
                    CurrentAmount = 0,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                };
                _context.Campaigns.Add(campaign);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "An error occurred while creating the campaign: " + ex.Message);
                var categories = await _context.Categories
                    .OrderBy(c => c.Name)
                    .ToListAsync();
                return View(model);
            }
        }
    }
}
