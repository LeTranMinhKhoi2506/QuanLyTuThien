using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TuThien.Models;

namespace TuThien.Controllers.Admin;

/// <summary>
/// Controller quản lý báo cáo sai phạm (Admin)
/// </summary>
public class AdminReportsController : AdminBaseController
{
    public AdminReportsController(TuThienContext context, ILogger<AdminReportsController> logger)
        : base(context, logger)
    {
    }

    /// <summary>
    /// Danh sách báo cáo
    /// </summary>
    public async Task<IActionResult> Index(string? status, string? targetType, int page = 1)
    {
        if (!IsAdmin())
        {
            return RedirectToLogin();
        }

        var query = _context.Reports
            .Include(r => r.Reporter)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(r => r.Status == status);
        }

        if (!string.IsNullOrEmpty(targetType))
        {
            query = query.Where(r => r.TargetType == targetType);
        }

        int pageSize = 20;
        var totalItems = await query.CountAsync();
        var reports = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        ViewBag.Status = status;
        ViewBag.TargetType = targetType;
        ViewBag.PendingCount = await _context.Reports.CountAsync(r => r.Status == "pending");

        return View("~/Views/Admin/Reports.cshtml", reports);
    }

    /// <summary>
    /// Cập nhật trạng thái báo cáo
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int reportId, string status)
    {
        if (!IsAdmin())
        {
            return UnauthorizedJson();
        }

        var report = await _context.Reports.FindAsync(reportId);
        if (report == null)
        {
            return Json(new { success = false, message = "Không tìm thấy báo cáo" });
        }

        var oldStatus = report.Status;
        report.Status = status;

        await _context.SaveChangesAsync();
        await LogAuditAsync("UPDATE_REPORT_STATUS", "Reports", reportId, oldStatus, status);

        return Json(new { success = true, message = "Cập nhật trạng thái thành công" });
    }
}
