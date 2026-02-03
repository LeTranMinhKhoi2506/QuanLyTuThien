using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TuThien.Models;

namespace TuThien.Controllers.Admin;

/// <summary>
/// Controller quản lý thông báo (Admin)
/// </summary>
public class AdminNotificationsController : AdminBaseController
{
    public AdminNotificationsController(TuThienContext context, ILogger<AdminNotificationsController> logger)
        : base(context, logger)
    {
    }

    /// <summary>
    /// Danh sách thông báo
    /// </summary>
    public async Task<IActionResult> Index()
    {
        if (!IsAdmin())
        {
            return RedirectToLogin();
        }

        var userId = GetCurrentUserId();
        var notifications = await _context.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(50)
            .ToListAsync();

        return View("~/Views/Admin/Notifications.cshtml", notifications);
    }

    /// <summary>
    /// Trang gửi thông báo
    /// </summary>
    public IActionResult Send()
    {
        if (!IsAdmin())
        {
            return RedirectToLogin();
        }

        return View("~/Views/Admin/SendNotification.cshtml");
    }

    /// <summary>
    /// Gửi thông báo
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Send(string title, string message, string targetType, int? targetUserId)
    {
        if (!IsAdmin())
        {
            return UnauthorizedJson();
        }

        var notifications = new List<Notification>();

        if (targetType == "all")
        {
            var users = await _context.Users.Where(u => u.Status == "active").ToListAsync();
            foreach (var user in users)
            {
                notifications.Add(new Notification
                {
                    UserId = user.UserId,
                    Title = title,
                    Message = message,
                    Type = "system",
                    IsRead = false,
                    CreatedAt = DateTime.Now
                });
            }
        }
        else if (targetType == "single" && targetUserId.HasValue)
        {
            notifications.Add(new Notification
            {
                UserId = targetUserId.Value,
                Title = title,
                Message = message,
                Type = "system",
                IsRead = false,
                CreatedAt = DateTime.Now
            });
        }

        _context.Notifications.AddRange(notifications);
        await _context.SaveChangesAsync();
        await LogAuditAsync("SEND_NOTIFICATION", "Notifications", 0, null, $"Sent to {notifications.Count} users");

        return Json(new { success = true, message = $"Đã gửi thông báo đến {notifications.Count} người dùng" });
    }

    /// <summary>
    /// Lấy danh sách thông báo (API)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        if (!IsAdmin())
        {
            return UnauthorizedJson();
        }

        var userId = GetCurrentUserId();

        var userNotifications = await _context.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(10)
            .ToListAsync();

        var pendingCampaigns = await _context.Campaigns
            .Where(c => c.Status == "pending_approval")
            .OrderByDescending(c => c.CreatedAt)
            .Take(5)
            .Select(c => new { c.CampaignId, c.Title, c.CreatedAt })
            .ToListAsync();

        var pendingDisbursements = await _context.DisbursementRequests
            .Include(d => d.Campaign)
            .Where(d => d.Status == "pending")
            .OrderByDescending(d => d.CreatedAt)
            .Take(5)
            .Select(d => new { d.RequestId, d.Campaign!.Title, d.CreatedAt })
            .ToListAsync();

        var pendingReports = await _context.Reports
            .Where(r => r.Status == "pending")
            .OrderByDescending(r => r.CreatedAt)
            .Take(5)
            .Select(r => new { r.ReportId, r.Reason, r.CreatedAt })
            .ToListAsync();

        var notifications = new List<object>();

        foreach (var c in pendingCampaigns)
        {
            notifications.Add(new
            {
                id = c.CampaignId,
                title = "Chiến dịch chờ duyệt",
                message = c.Title?.Length > 50 ? c.Title.Substring(0, 50) + "..." : c.Title,
                type = "campaign",
                isRead = false,
                url = Url.Action("Detail", "AdminCampaigns", new { id = c.CampaignId }),
                timeAgo = GetTimeAgo(c.CreatedAt)
            });
        }

        foreach (var d in pendingDisbursements)
        {
            notifications.Add(new
            {
                id = d.RequestId,
                title = "Yêu cầu giải ngân",
                message = d.Title?.Length > 50 ? d.Title.Substring(0, 50) + "..." : d.Title,
                type = "disbursement",
                isRead = false,
                url = Url.Action("Detail", "AdminDisbursements", new { id = d.RequestId }),
                timeAgo = GetTimeAgo(d.CreatedAt)
            });
        }

        foreach (var r in pendingReports)
        {
            notifications.Add(new
            {
                id = r.ReportId,
                title = "Báo cáo sai phạm",
                message = r.Reason?.Length > 50 ? r.Reason.Substring(0, 50) + "..." : r.Reason,
                type = "report",
                isRead = false,
                url = Url.Action("Index", "AdminReports"),
                timeAgo = GetTimeAgo(r.CreatedAt)
            });
        }

        foreach (var n in userNotifications)
        {
            notifications.Add(new
            {
                id = n.NotificationId,
                title = n.Title,
                message = n.Message?.Length > 50 ? n.Message.Substring(0, 50) + "..." : n.Message,
                type = n.Type ?? "system",
                isRead = n.IsRead ?? false,
                url = "#",
                timeAgo = GetTimeAgo(n.CreatedAt)
            });
        }

        var sortedNotifications = notifications.Take(10).ToList();
        var unreadCount = pendingCampaigns.Count + pendingDisbursements.Count + pendingReports.Count
            + userNotifications.Count(n => n.IsRead != true);

        return Json(new
        {
            success = true,
            unreadCount,
            notifications = sortedNotifications
        });
    }

    /// <summary>
    /// Đánh dấu đã đọc
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> MarkRead(int id)
    {
        if (!IsAdmin())
        {
            return Json(new { success = false });
        }

        var notification = await _context.Notifications.FindAsync(id);
        if (notification != null)
        {
            notification.IsRead = true;
            await _context.SaveChangesAsync();
        }

        return Json(new { success = true });
    }

    /// <summary>
    /// Đánh dấu tất cả đã đọc
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> MarkAllRead()
    {
        if (!IsAdmin())
        {
            return Json(new { success = false });
        }

        var userId = GetCurrentUserId();
        var notifications = await _context.Notifications
            .Where(n => n.UserId == userId && n.IsRead != true)
            .ToListAsync();

        foreach (var n in notifications)
        {
            n.IsRead = true;
        }

        await _context.SaveChangesAsync();

        return Json(new { success = true });
    }

    /// <summary>
    /// Gửi thông báo theo vai trò
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendByRole(string title, string message, string role)
    {
        if (!IsAdmin())
        {
            return UnauthorizedJson();
        }

        var users = await _context.Users
            .Where(u => u.Status == "active" && u.Role == role)
            .ToListAsync();

        if (!users.Any())
        {
            return Json(new { success = false, message = $"Không có người dùng nào với vai trò '{role}'" });
        }

        var notifications = users.Select(user => new Notification
        {
            UserId = user.UserId,
            Title = title,
            Message = message,
            Type = "system",
            IsRead = false,
            CreatedAt = DateTime.Now
        }).ToList();

        _context.Notifications.AddRange(notifications);
        await _context.SaveChangesAsync();
        await LogAuditAsync("SEND_NOTIFICATION_BY_ROLE", "Notifications", 0, null, $"Sent to {notifications.Count} users with role '{role}'");

        return Json(new { success = true, message = $"Đã gửi thông báo đến {notifications.Count} người dùng vai trò '{role}'" });
    }

    /// <summary>
    /// Gửi thông báo đến donors của chiến dịch
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendToDonors(string title, string message, int campaignId)
    {
        if (!IsAdmin())
        {
            return UnauthorizedJson();
        }

        var donorIds = await _context.Donations
            .Where(d => d.CampaignId == campaignId && d.UserId.HasValue)
            .Select(d => d.UserId!.Value)
            .Distinct()
            .ToListAsync();

        if (!donorIds.Any())
        {
            return Json(new { success = false, message = "Không có người quyên góp nào cho chiến dịch này" });
        }

        var notifications = donorIds.Select(userId => new Notification
        {
            UserId = userId,
            Title = title,
            Message = message,
            Type = "campaign",
            IsRead = false,
            CreatedAt = DateTime.Now
        }).ToList();

        _context.Notifications.AddRange(notifications);
        await _context.SaveChangesAsync();
        await LogAuditAsync("SEND_NOTIFICATION_TO_DONORS", "Notifications", campaignId, null, $"Sent to {notifications.Count} donors");

        return Json(new { success = true, message = $"Đã gửi thông báo đến {notifications.Count} người quyên góp" });
    }
}
