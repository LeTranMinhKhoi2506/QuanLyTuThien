using Microsoft.AspNetCore.Mvc;
using TuThien.Models;
using System.Text.Json;

namespace TuThien.Controllers.Admin;

/// <summary>
/// Base controller cho các Admin controllers - chứa các phương thức dùng chung
/// </summary>
public abstract class AdminBaseController : Controller
{
    protected readonly TuThienContext _context;
    protected readonly ILogger _logger;

    protected AdminBaseController(TuThienContext context, ILogger logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Kiểm tra quyền Admin
    /// </summary>
    protected bool IsAdmin()
    {
        var role = HttpContext.Session.GetString("Role");
        return role == "admin";
    }

    /// <summary>
    /// Lấy UserId hiện tại từ session
    /// </summary>
    protected int? GetCurrentUserId()
    {
        return HttpContext.Session.GetInt32("UserId");
    }

    /// <summary>
    /// Ghi audit log
    /// </summary>
    protected async Task LogAuditAsync(string action, string tableName, int recordId, object? oldValue = null, object? newValue = null)
    {
        var userId = GetCurrentUserId();
        string? oldValueJson = oldValue != null ? JsonSerializer.Serialize(new { value = oldValue }) : null;
        string? newValueJson = newValue != null ? JsonSerializer.Serialize(new { value = newValue }) : null;

        var log = new AuditLog
        {
            UserId = userId,
            Action = action,
            TableName = tableName,
            RecordId = recordId,
            OldValue = oldValueJson,
            NewValue = newValueJson,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = HttpContext.Request.Headers["User-Agent"].ToString(),
            CreatedAt = DateTime.Now
        };
        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Helper: Tính thời gian tương đối
    /// </summary>
    protected string GetTimeAgo(DateTime? dateTime)
    {
        if (!dateTime.HasValue) return "";

        var span = DateTime.Now - dateTime.Value;

        if (span.TotalMinutes < 1) return "Vừa xong";
        if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes} phút trước";
        if (span.TotalHours < 24) return $"{(int)span.TotalHours} giờ trước";
        if (span.TotalDays < 7) return $"{(int)span.TotalDays} ngày trước";
        if (span.TotalDays < 30) return $"{(int)(span.TotalDays / 7)} tuần trước";
        return dateTime.Value.ToString("dd/MM/yyyy");
    }

    /// <summary>
    /// Trả về kết quả không có quyền (JSON)
    /// </summary>
    protected IActionResult UnauthorizedJson()
    {
        return Json(new { success = false, message = "Không có quyền" });
    }

    /// <summary>
    /// Redirect đến trang Login nếu không phải Admin
    /// </summary>
    protected IActionResult RedirectToLogin()
    {
        return RedirectToAction("Login", "Account");
    }
}
