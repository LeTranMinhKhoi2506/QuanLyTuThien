using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TuThien.Models;
using TuThien.ViewModels.Admin;

namespace TuThien.Controllers.Admin;

/// <summary>
/// Controller quản lý chiến dịch (Admin)
/// </summary>
public class AdminCampaignsController : AdminBaseController
{
    public AdminCampaignsController(TuThienContext context, ILogger<AdminCampaignsController> logger)
        : base(context, logger)
    {
    }

    /// <summary>
    /// Danh sách chiến dịch
    /// </summary>
    public async Task<IActionResult> Index(string? status, string? search, int page = 1)
    {
        if (!IsAdmin())
        {
            return RedirectToLogin();
        }

        var query = _context.Campaigns
            .Include(c => c.Creator)
            .Include(c => c.Category)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(c => c.Status == status);
        }

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(c => c.Title.Contains(search) || c.Creator.Username.Contains(search));
        }

        int pageSize = 20;
        var totalItems = await query.CountAsync();
        var campaigns = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        ViewBag.Status = status;
        ViewBag.Search = search;
        ViewBag.PendingCount = await _context.Campaigns.CountAsync(c => c.Status == "pending_approval");

        return View("~/Views/Admin/Campaigns.cshtml", campaigns);
    }

    /// <summary>
    /// Chi tiết chiến dịch
    /// </summary>
    public async Task<IActionResult> Detail(int id)
    {
        if (!IsAdmin())
        {
            return RedirectToLogin();
        }

        var campaign = await _context.Campaigns
            .Include(c => c.Creator)
                .ThenInclude(u => u.UserProfile)
            .Include(c => c.Category)
            .Include(c => c.CampaignDocuments)
            .Include(c => c.CampaignMilestones)
            .Include(c => c.DisbursementRequests)
                .ThenInclude(d => d.Requester)
            .FirstOrDefaultAsync(c => c.CampaignId == id);

        if (campaign == null)
        {
            return NotFound();
        }

        return View("~/Views/Admin/CampaignDetail.cshtml", campaign);
    }

    /// <summary>
    /// Kiểm tra tên chiến dịch đã tồn tại
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> CheckTitleExists(string title, int? excludeCampaignId = null)
    {
        if (!IsAdmin())
        {
            return Json(new { exists = false });
        }

        var query = _context.Campaigns.Where(c => c.Title.ToLower() == title.ToLower().Trim());
        if (excludeCampaignId.HasValue)
        {
            query = query.Where(c => c.CampaignId != excludeCampaignId.Value);
        }

        var exists = await query.AnyAsync();
        return Json(new { exists });
    }

    /// <summary>
    /// Phê duyệt chiến dịch
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int campaignId, string? note)
    {
        try
        {
            if (!IsAdmin())
            {
                return UnauthorizedJson();
            }

            var campaign = await _context.Campaigns.FindAsync(campaignId);
            if (campaign == null)
            {
                return Json(new { success = false, message = "Không tìm thấy chiến dịch" });
            }

            var oldStatus = campaign.Status;
            campaign.Status = "active";
            campaign.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            await LogAuditAsync("APPROVE_CAMPAIGN", "Campaigns", campaignId, oldStatus, "active");

            var notification = new Notification
            {
                UserId = campaign.CreatorId,
                Title = "Chiến dịch được phê duyệt",
                Message = $"Chiến dịch \"{campaign.Title}\" của bạn đã được phê duyệt và đang hoạt động.",
                Type = "campaign_update",
                IsRead = false,
                CreatedAt = DateTime.Now
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Phê duyệt chiến dịch thành công" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving campaign {CampaignId}", campaignId);
            return Json(new { success = false, message = "Lỗi: " + ex.Message + (ex.InnerException != null ? " (" + ex.InnerException.Message + ")" : "") });
        }
    }

    /// <summary>
    /// Từ chối chiến dịch
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int campaignId, string note)
    {
        try
        {
            if (!IsAdmin())
            {
                return UnauthorizedJson();
            }

            var campaign = await _context.Campaigns.FindAsync(campaignId);
            if (campaign == null)
            {
                return Json(new { success = false, message = "Không tìm thấy chiến dịch" });
            }

            var oldStatus = campaign.Status;
            campaign.Status = "rejected";
            campaign.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            await LogAuditAsync("REJECT_CAMPAIGN", "Campaigns", campaignId, oldStatus, "rejected");

            var notification = new Notification
            {
                UserId = campaign.CreatorId,
                Title = "Chiến dịch bị từ chối",
                Message = $"Chiến dịch \"{campaign.Title}\" của bạn đã bị từ chối.",
                Type = "campaign_update",
                IsRead = false,
                CreatedAt = DateTime.Now
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Từ chối chiến dịch thành công" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting campaign {CampaignId}", campaignId);
            return Json(new { success = false, message = "Lỗi: " + ex.Message });
        }
    }

    /// <summary>
    /// Đóng/hoàn thành chiến dịch
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Close(int campaignId, string? note)
    {
        if (!IsAdmin())
        {
            return UnauthorizedJson();
        }

        var campaign = await _context.Campaigns.FindAsync(campaignId);
        if (campaign == null)
        {
            return Json(new { success = false, message = "Không tìm thấy chiến dịch" });
        }

        var oldStatus = campaign.Status;
        campaign.Status = "completed";
        campaign.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();
        await LogAuditAsync("CLOSE_CAMPAIGN", "Campaigns", campaignId, oldStatus, "completed");

        return Json(new { success = true, message = "Hoàn thành chiến dịch thành công" });
    }

    /// <summary>
    /// Cập nhật trạng thái chiến dịch
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int campaignId, string status)
    {
        if (!IsAdmin())
        {
            return UnauthorizedJson();
        }

        var campaign = await _context.Campaigns.FindAsync(campaignId);
        if (campaign == null)
        {
            return Json(new { success = false, message = "Không tìm thấy chiến dịch" });
        }

        var validStatuses = new[] { "active", "paused", "completed", "rejected", "locked", "draft", "pending_approval" };
        if (!validStatuses.Contains(status))
        {
            return Json(new { success = false, message = "Trạng thái không hợp lệ" });
        }

        var oldStatus = campaign.Status;
        campaign.Status = status;
        campaign.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();
        await LogAuditAsync("UPDATE_CAMPAIGN_STATUS", "Campaigns", campaignId, oldStatus, status);

        return Json(new { success = true, message = "Cập nhật trạng thái thành công" });
    }

    /// <summary>
    /// Lấy danh sách categories
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetCategories()
    {
        if (!IsAdmin())
        {
            return UnauthorizedJson();
        }

        var categories = await _context.Categories
            .Select(c => new { c.CategoryId, c.Name })
            .ToListAsync();

        return Json(new { success = true, categories });
    }

    /// <summary>
    /// Tạo chiến dịch mới
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromForm] CampaignAdminViewModel model)
    {
        try
        {
            if (!IsAdmin())
            {
                return UnauthorizedJson();
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                _logger.LogWarning("CreateCampaign validation failed: {Errors}", string.Join("; ", errors));
                return Json(new { success = false, message = string.Join("; ", errors) });
            }

            var adminUserId = GetCurrentUserId();
            if (!adminUserId.HasValue)
            {
                return Json(new { success = false, message = "Không thể xác định người dùng" });
            }

            var validExcessOptions = new[] { "reserve_fund", "next_case", "general_fund", "extend" };
            if (!validExcessOptions.Contains(model.ExcessFundOption))
            {
                model.ExcessFundOption = "next_case";
            }

            var normalizedTitle = model.Title.Trim().ToLower();
            var duplicateCampaign = await _context.Campaigns
                .AnyAsync(c => c.Title.ToLower() == normalizedTitle);
            if (duplicateCampaign)
            {
                return Json(new { success = false, message = "Đã tồn tại chiến dịch với tên này. Vui lòng chọn tên khác." });
            }

            var campaign = new Campaign
            {
                CreatorId = adminUserId.Value,
                CategoryId = model.CategoryId,
                Title = model.Title.Trim(),
                Description = model.Description.Trim(),
                TargetAmount = model.TargetAmount,
                CurrentAmount = 0,
                StartDate = model.StartDate ?? DateTime.Now,
                EndDate = model.EndDate,
                ThumbnailUrl = model.ThumbnailUrl,
                ExcessFundOption = model.ExcessFundOption,
                Status = "active",
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            _context.Campaigns.Add(campaign);
            await _context.SaveChangesAsync();
            await LogAuditAsync("ADMIN_CREATE_CAMPAIGN", "Campaigns", campaign.CampaignId, null, campaign.Title);

            return Json(new { success = true, message = "Tạo chiến dịch thành công", campaignId = campaign.CampaignId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating campaign");
            return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
        }
    }

    /// <summary>
    /// Lấy thông tin chiến dịch
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Get(int id)
    {
        if (!IsAdmin())
        {
            return UnauthorizedJson();
        }

        var campaign = await _context.Campaigns
            .Where(c => c.CampaignId == id)
            .Select(c => new
            {
                c.CampaignId,
                c.Title,
                c.Description,
                c.TargetAmount,
                c.CategoryId,
                StartDate = c.StartDate.HasValue ? c.StartDate.Value.ToString("yyyy-MM-dd") : null,
                EndDate = c.EndDate.HasValue ? c.EndDate.Value.ToString("yyyy-MM-dd") : null,
                c.ThumbnailUrl,
                c.ExcessFundOption,
                c.Status
            })
            .FirstOrDefaultAsync();

        if (campaign == null)
        {
            return Json(new { success = false, message = "Không tìm thấy chiến dịch" });
        }

        return Json(new { success = true, campaign });
    }

    /// <summary>
    /// Cập nhật chiến dịch
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update([FromForm] CampaignAdminViewModel model)
    {
        try
        {
            if (!IsAdmin())
            {
                return UnauthorizedJson();
            }

            var campaign = await _context.Campaigns.FindAsync(model.CampaignId);
            if (campaign == null)
            {
                return Json(new { success = false, message = "Không tìm thấy chiến dịch" });
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                _logger.LogWarning("UpdateCampaign validation failed: {Errors}", string.Join("; ", errors));
                return Json(new { success = false, message = string.Join("; ", errors) });
            }

            var validExcessOptions = new[] { "reserve_fund", "next_case", "general_fund", "extend" };
            if (!validExcessOptions.Contains(model.ExcessFundOption))
            {
                model.ExcessFundOption = "next_case";
            }

            var normalizedTitle = model.Title.Trim().ToLower();
            var duplicateCampaign = await _context.Campaigns
                .AnyAsync(c => c.Title.ToLower() == normalizedTitle && c.CampaignId != model.CampaignId);
            if (duplicateCampaign)
            {
                return Json(new { success = false, message = "Đã tồn tại chiến dịch với tên này. Vui lòng chọn tên khác." });
            }

            var oldTitle = campaign.Title;
            campaign.Title = model.Title.Trim();
            campaign.Description = model.Description.Trim();
            campaign.TargetAmount = model.TargetAmount;
            campaign.CategoryId = model.CategoryId;
            campaign.StartDate = model.StartDate;
            campaign.EndDate = model.EndDate;
            campaign.ThumbnailUrl = model.ThumbnailUrl;
            campaign.ExcessFundOption = model.ExcessFundOption;
            if (!string.IsNullOrEmpty(model.Status))
            {
                campaign.Status = model.Status;
            }
            campaign.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            await LogAuditAsync("ADMIN_UPDATE_CAMPAIGN", "Campaigns", model.CampaignId, oldTitle, campaign.Title);

            return Json(new { success = true, message = "Cập nhật chiến dịch thành công" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating campaign {CampaignId}", model.CampaignId);
            return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
        }
    }

    /// <summary>
    /// Xóa chiến dịch
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int campaignId)
    {
        if (!IsAdmin())
        {
            return UnauthorizedJson();
        }

        var campaign = await _context.Campaigns
            .Include(c => c.Donations)
            .FirstOrDefaultAsync(c => c.CampaignId == campaignId);

        if (campaign == null)
        {
            return Json(new { success = false, message = "Không tìm thấy chiến dịch" });
        }

        if (campaign.Donations.Any())
        {
            return Json(new { success = false, message = "Không thể xóa chiến dịch đã có quyên góp. Vui lòng đóng chiến dịch thay vì xóa." });
        }

        var campaignTitle = campaign.Title;

        var documents = await _context.CampaignDocuments.Where(d => d.CampaignId == campaignId).ToListAsync();
        _context.CampaignDocuments.RemoveRange(documents);

        var milestones = await _context.CampaignMilestones.Where(m => m.CampaignId == campaignId).ToListAsync();
        _context.CampaignMilestones.RemoveRange(milestones);

        var updates = await _context.CampaignUpdates.Where(u => u.CampaignId == campaignId).ToListAsync();
        _context.CampaignUpdates.RemoveRange(updates);

        var comments = await _context.Comments.Where(cm => cm.CampaignId == campaignId).ToListAsync();
        _context.Comments.RemoveRange(comments);

        _context.Campaigns.Remove(campaign);
        await _context.SaveChangesAsync();
        await LogAuditAsync("ADMIN_DELETE_CAMPAIGN", "Campaigns", campaignId, campaignTitle, null);

        return Json(new { success = true, message = "Xóa chiến dịch thành công" });
    }
}
