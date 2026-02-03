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
            var categorise = await _context.Categories
                .Include(c => c.Campaigns)
                .OrderBy(c => c.Name)
                .ToListAsync();
            return View("TrangChu", categorise);
        }
        
        [HttpGet]
        public async Task<IActionResult> FilterCategory(int? categoryId)
        {
            IEnumerable<Category> categories;
            if(categoryId.HasValue && categoryId.Value > 0)
            {
                categories = await _context.Categories
                    .Where(c => c.CategoryId == categoryId.Value)
                    .Include(c => c.Campaigns)
                    .ToListAsync();
            }
            else
            {
                categories = await _context.Categories
                    .Include(c => c.Campaigns)
                    .OrderBy(c=>c.Name)
                    .ToListAsync();
            }
            return PartialView("patiralView", categories);
        }

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
