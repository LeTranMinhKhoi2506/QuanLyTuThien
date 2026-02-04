using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TuThien.Models;

namespace TuThien.Controllers.Admin;

/// <summary>
/// Controller quản lý tin tức/cập nhật chiến dịch (Admin)
/// </summary>
public class AdminNewsController : AdminBaseController
{
    public AdminNewsController(TuThienContext context, ILogger<AdminNewsController> logger)
        : base(context, logger)
    {
    }

    /// <summary>
    /// Danh sách tin tức
    /// </summary>
    public async Task<IActionResult> Index()
    {
        if (!IsAdmin())
        {
            return RedirectToLogin();
        }

        var news = await _context.CampaignUpdates
            .Include(u => u.Campaign)
            .Include(u => u.Author)
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync();

        return View("~/Views/Admin/News/Index.cshtml", news);
    }

    /// <summary>
    /// Trang tạo tin tức mới
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        if (!IsAdmin())
        {
            return RedirectToLogin();
        }

        await LoadCampaignsDropdown();
        return View("~/Views/Admin/News/Create.cshtml");
    }

    /// <summary>
    /// Xử lý tạo tin tức mới
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CampaignUpdate model, string? imageUrlsJson)
    {
        if (!IsAdmin())
        {
            return RedirectToLogin();
        }

        try
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return Json(new { success = false, message = "Phiên đăng nhập hết hạn" });
            }

            // Validate
            if (model.CampaignId <= 0)
            {
                return Json(new { success = false, message = "Vui lòng chọn chiến dịch" });
            }

            if (string.IsNullOrWhiteSpace(model.Title))
            {
                return Json(new { success = false, message = "Vui lòng nhập tiêu đề" });
            }

            if (string.IsNullOrWhiteSpace(model.Content))
            {
                return Json(new { success = false, message = "Vui lòng nhập nội dung" });
            }

            var news = new CampaignUpdate
            {
                CampaignId = model.CampaignId,
                AuthorId = userId.Value,
                Title = model.Title.Trim(),
                Content = model.Content,
                Type = model.Type ?? "general",
                ImageUrls = string.IsNullOrWhiteSpace(imageUrlsJson) ? null : imageUrlsJson,
                CreatedAt = DateTime.Now
            };

            _context.CampaignUpdates.Add(news);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Admin {UserId} created news {NewsId}: {Title}", userId, news.UpdateId, news.Title);

            return Json(new { success = true, message = "Tạo tin tức thành công!", newsId = news.UpdateId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating news");
            return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
        }
    }

    /// <summary>
    /// Trang sửa tin tức
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        if (!IsAdmin())
        {
            return RedirectToLogin();
        }

        var news = await _context.CampaignUpdates
            .Include(u => u.Campaign)
            .FirstOrDefaultAsync(u => u.UpdateId == id);

        if (news == null)
        {
            return NotFound();
        }

        await LoadCampaignsDropdown(news.CampaignId);
        return View("~/Views/Admin/News/Edit.cshtml", news);
    }

    /// <summary>
    /// Xử lý cập nhật tin tức
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CampaignUpdate model, string? imageUrlsJson)
    {
        if (!IsAdmin())
        {
            return RedirectToLogin();
        }

        try
        {
            var news = await _context.CampaignUpdates.FindAsync(id);
            if (news == null)
            {
                return Json(new { success = false, message = "Không tìm thấy tin tức" });
            }

            // Validate
            if (model.CampaignId <= 0)
            {
                return Json(new { success = false, message = "Vui lòng chọn chiến dịch" });
            }

            if (string.IsNullOrWhiteSpace(model.Title))
            {
                return Json(new { success = false, message = "Vui lòng nhập tiêu đề" });
            }

            if (string.IsNullOrWhiteSpace(model.Content))
            {
                return Json(new { success = false, message = "Vui lòng nhập nội dung" });
            }

            news.CampaignId = model.CampaignId;
            news.Title = model.Title.Trim();
            news.Content = model.Content;
            news.Type = model.Type ?? "general";
            news.ImageUrls = string.IsNullOrWhiteSpace(imageUrlsJson) ? null : imageUrlsJson;

            await _context.SaveChangesAsync();

            var userId = HttpContext.Session.GetInt32("UserId");
            _logger.LogInformation("Admin {UserId} updated news {NewsId}", userId, id);

            return Json(new { success = true, message = "Cập nhật tin tức thành công!" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating news {NewsId}", id);
            return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
        }
    }

    /// <summary>
    /// Xóa tin tức
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        if (!IsAdmin())
        {
            return Json(new { success = false, message = "Không có quyền truy cập" });
        }

        try
        {
            var news = await _context.CampaignUpdates.FindAsync(id);
            if (news == null)
            {
                return Json(new { success = false, message = "Không tìm thấy tin tức" });
            }

            _context.CampaignUpdates.Remove(news);
            await _context.SaveChangesAsync();

            var userId = HttpContext.Session.GetInt32("UserId");
            _logger.LogInformation("Admin {UserId} deleted news {NewsId}", userId, id);

            return Json(new { success = true, message = "Xóa tin tức thành công!" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting news {NewsId}", id);
            return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
        }
    }

    /// <summary>
    /// API lấy chi tiết tin tức (cho modal edit)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Get(int id)
    {
        if (!IsAdmin())
        {
            return Json(new { success = false, message = "Không có quyền truy cập" });
        }

        var news = await _context.CampaignUpdates
            .Include(u => u.Campaign)
            .FirstOrDefaultAsync(u => u.UpdateId == id);

        if (news == null)
        {
            return Json(new { success = false, message = "Không tìm thấy tin tức" });
        }

        return Json(new
        {
            success = true,
            news = new
            {
                updateId = news.UpdateId,
                campaignId = news.CampaignId,
                campaignTitle = news.Campaign?.Title,
                title = news.Title,
                content = news.Content,
                type = news.Type,
                imageUrls = news.ImageUrls,
                createdAt = news.CreatedAt?.ToString("dd/MM/yyyy HH:mm")
            }
        });
    }

    private async Task LoadCampaignsDropdown(int? selectedId = null)
    {
        var campaigns = await _context.Campaigns
            .Where(c => c.Status == "active" || c.Status == "completed")
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new { c.CampaignId, c.Title })
            .ToListAsync();

        ViewBag.Campaigns = new SelectList(campaigns, "CampaignId", "Title", selectedId);
    }
}
