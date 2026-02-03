using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TuThien.Models;

namespace TuThien.Controllers
{
    public class NewsController : Controller
    {
        private readonly TuThienContext _context;
        private readonly ILogger<NewsController> _logger;

        public NewsController(TuThienContext context, ILogger<NewsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Trang danh sách tin t?c - Hi?n th? t?t c? CampaignUpdates
        /// </summary>
        public async Task<IActionResult> Index(string? type, string? search, int? campaignId, int page = 1)
        {
            int pageSize = 12; // 12 tin m?i trang

            var query = _context.CampaignUpdates
                .Include(u => u.Campaign)
                    .ThenInclude(c => c.Category)
                .Include(u => u.Author)
                    .ThenInclude(a => a.UserProfile)
                .AsQueryable();

            // Filter theo lo?i tin
            if (!string.IsNullOrEmpty(type))
            {
                query = query.Where(u => u.Type == type);
            }

            // Filter theo chi?n d?ch
            if (campaignId.HasValue && campaignId.Value > 0)
            {
                query = query.Where(u => u.CampaignId == campaignId.Value);
            }

            // Tìm ki?m
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(u => 
                    u.Title!.Contains(search) || 
                    u.Content!.Contains(search) ||
                    u.Campaign.Title.Contains(search));
            }

            // ??m t?ng s?
            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            // L?y d? li?u phân trang
            var updates = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Th?ng kê
            ViewBag.TotalUpdates = totalItems;
            ViewBag.GeneralCount = await _context.CampaignUpdates.CountAsync(u => u.Type == "general");
            ViewBag.FinancialCount = await _context.CampaignUpdates.CountAsync(u => u.Type == "financial_report");
            
            // Phân trang
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.PageSize = pageSize;
            
            // Filters
            ViewBag.Type = type;
            ViewBag.Search = search;
            ViewBag.CampaignId = campaignId;

            // L?y danh sách chi?n d?ch cho dropdown filter
            ViewBag.Campaigns = await _context.Campaigns
                .Where(c => c.Status == "active" || c.Status == "completed")
                .OrderBy(c => c.Title)
                .Select(c => new { c.CampaignId, c.Title })
                .ToListAsync();

            return View(updates);
        }

        /// <summary>
        /// API AJAX - L?y tin t?c d?ng JSON (không reload trang)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetNewsAjax(string? type, string? search, int? campaignId, int page = 1)
        {
            int pageSize = 12;

            var query = _context.CampaignUpdates
                .Include(u => u.Campaign)
                    .ThenInclude(c => c.Category)
                .Include(u => u.Author)
                .AsQueryable();

            // Filters
            if (!string.IsNullOrEmpty(type))
            {
                query = query.Where(u => u.Type == type);
            }

            if (campaignId.HasValue && campaignId.Value > 0)
            {
                query = query.Where(u => u.CampaignId == campaignId.Value);
            }

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(u => 
                    u.Title!.Contains(search) || 
                    u.Content!.Contains(search) ||
                    u.Campaign.Title.Contains(search));
            }

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            // L?y data tr??c, sau ?ó x? lý JSON
            var updatesData = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Process data và extract first image
            var updates = updatesData.Select(u => {
                string? firstImage = null;
                try
                {
                    if (!string.IsNullOrEmpty(u.ImageUrls))
                    {
                        var images = System.Text.Json.JsonSerializer.Deserialize<List<string>>(u.ImageUrls);
                        firstImage = images?.FirstOrDefault();
                    }
                }
                catch { }

                var excerpt = u.Content?.Length > 150
                    ? System.Text.RegularExpressions.Regex.Replace(u.Content.Substring(0, 150), "<.*?>", "") + "..."
                    : System.Text.RegularExpressions.Regex.Replace(u.Content ?? "", "<.*?>", "");

                return new
                {
                    updateId = u.UpdateId,
                    title = u.Title,
                    content = u.Content,
                    type = u.Type,
                    createdAt = u.CreatedAt.HasValue ? u.CreatedAt.Value.ToString("dd/MM/yyyy") : "",
                    campaignId = u.CampaignId,
                    campaignTitle = u.Campaign?.Title,
                    campaignThumbnail = u.Campaign?.ThumbnailUrl,
                    categoryName = u.Campaign?.Category?.Name,
                    authorName = u.Author?.Username ?? "Admin",
                    imageUrls = u.ImageUrls,
                    firstImage = firstImage,
                    excerpt = excerpt
                };
            }).ToList();

            // Th?ng kê
            var generalCount = await _context.CampaignUpdates.CountAsync(u => u.Type == "general");
            var financialCount = await _context.CampaignUpdates.CountAsync(u => u.Type == "financial_report");

            return Json(new
            {
                success = true,
                updates = updates,
                currentPage = page,
                totalPages = totalPages,
                totalUpdates = totalItems,
                generalCount = generalCount,
                financialCount = financialCount,
                type = type,
                search = search,
                campaignId = campaignId
            });
        }

        /// <summary>
        /// Trang chi ti?t tin t?c
        /// </summary>
        public async Task<IActionResult> Details(int id)
        {
            var update = await _context.CampaignUpdates
                .Include(u => u.Campaign)
                    .ThenInclude(c => c.Category)
                .Include(u => u.Campaign)
                    .ThenInclude(c => c.Creator)
                        .ThenInclude(cr => cr.UserProfile)
                .Include(u => u.Author)
                    .ThenInclude(a => a.UserProfile)
                .FirstOrDefaultAsync(u => u.UpdateId == id);

            if (update == null)
            {
                return NotFound();
            }

            // L?y các tin t?c liên quan (cùng chi?n d?ch)
            ViewBag.RelatedUpdates = await _context.CampaignUpdates
                .Include(u => u.Campaign)
                .Include(u => u.Author)
                .Where(u => u.CampaignId == update.CampaignId && u.UpdateId != id)
                .OrderByDescending(u => u.CreatedAt)
                .Take(4)
                .ToListAsync();

            return View(update);
        }

        /// <summary>
        /// API l?y tin t?c theo chi?n d?ch (dùng cho AJAX)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetUpdatesByCampaign(int campaignId, int page = 1, int pageSize = 6)
        {
            var query = _context.CampaignUpdates
                .Include(u => u.Author)
                .Where(u => u.CampaignId == campaignId)
                .OrderByDescending(u => u.CreatedAt);

            var totalItems = await query.CountAsync();
            var updates = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new
                {
                    u.UpdateId,
                    u.Title,
                    u.Content,
                    u.Type,
                    u.CreatedAt,
                    AuthorName = u.Author.Username,
                    u.ImageUrls
                })
                .ToListAsync();

            return Json(new
            {
                success = true,
                updates = updates,
                totalItems = totalItems,
                totalPages = (int)Math.Ceiling(totalItems / (double)pageSize),
                currentPage = page
            });
        }

        /// <summary>
        /// Trang tin t?c m?i nh?t (Latest News)
        /// </summary>
        public async Task<IActionResult> Latest(int count = 10)
        {
            var latestUpdates = await _context.CampaignUpdates
                .Include(u => u.Campaign)
                    .ThenInclude(c => c.Category)
                .Include(u => u.Author)
                .OrderByDescending(u => u.CreatedAt)
                .Take(count)
                .ToListAsync();

            return View(latestUpdates);
        }

        /// <summary>
        /// Trang tin t?c báo cáo tài chính
        /// </summary>
        public async Task<IActionResult> FinancialReports(int page = 1)
        {
            int pageSize = 10;

            var query = _context.CampaignUpdates
                .Include(u => u.Campaign)
                    .ThenInclude(c => c.Category)
                .Include(u => u.Author)
                .Where(u => u.Type == "financial_report")
                .OrderByDescending(u => u.CreatedAt);

            var totalItems = await query.CountAsync();
            var reports = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            return View(reports);
        }
    }
}
