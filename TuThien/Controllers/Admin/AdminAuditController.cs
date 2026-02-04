using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TuThien.Models;

namespace TuThien.Controllers.Admin;

/// <summary>
/// Controller quản lý nhật ký hệ thống (Admin)
/// </summary>
public class AdminAuditController : AdminBaseController
{
    public AdminAuditController(TuThienContext context, ILogger<AdminAuditController> logger)
        : base(context, logger)
    {
    }

    /// <summary>
    /// Danh sách audit logs
    /// </summary>
    public async Task<IActionResult> Index(string? actionFilter, string? tableName, int? userId, DateTime? fromDate, DateTime? toDate)
    {
        if (!IsAdmin())
        {
            return RedirectToLogin();
        }

        // Start with base query - Include User for navigation property
        IQueryable<AuditLog> query = _context.AuditLogs.Include(a => a.User);

        // Apply filters only if they have values
        if (!string.IsNullOrWhiteSpace(actionFilter))
        {
            query = query.Where(a => a.Action.Contains(actionFilter));
        }

        if (!string.IsNullOrWhiteSpace(tableName))
        {
            query = query.Where(a => a.TableName == tableName);
        }

        if (userId.HasValue)
        {
            query = query.Where(a => a.UserId == userId.Value);
        }

        if (fromDate.HasValue)
        {
            var startDate = fromDate.Value.Date;
            query = query.Where(a => a.CreatedAt >= startDate);
        }

        if (toDate.HasValue)
        {
            var endDate = toDate.Value.Date.AddDays(1);
            query = query.Where(a => a.CreatedAt < endDate);
        }

        // Get total count
        var totalItems = await query.CountAsync();

        // Get all logs ordered by CreatedAt descending
        var logs = await query
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

        // Get distinct values for filter dropdowns
        ViewBag.Tables = await _context.AuditLogs.Select(a => a.TableName).Distinct().OrderBy(t => t).ToListAsync();
        ViewBag.Actions = await _context.AuditLogs.Select(a => a.Action).Distinct().OrderBy(a => a).ToListAsync();
        
        // Pass filter values back to view
        ViewBag.ActionFilter = actionFilter;
        ViewBag.TableName = tableName;
        ViewBag.UserId = userId;
        ViewBag.FromDate = fromDate;
        ViewBag.ToDate = toDate;
        ViewBag.TotalItems = totalItems;

        return View("~/Views/Admin/AuditLogs.cshtml", logs);
    }

    /// <summary>
    /// Test audit log - tạo một bản ghi test để kiểm tra hệ thống
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TestLog()
    {
        if (!IsAdmin())
        {
            return Json(new { success = false, message = "Không có quyền" });
        }

        try
        {
            await LogAuditAsync("TEST_AUDIT", "AuditLogs", 0, null, "Test audit log at " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            return Json(new { success = true, message = "Đã tạo bản ghi audit log test thành công!" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating test audit log");
            return Json(new { success = false, message = "Lỗi: " + ex.Message });
        }
    }
}
