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
    public async Task<IActionResult> Index(string? action, string? tableName, int? userId, DateTime? fromDate, DateTime? toDate, int page = 1)
    {
        if (!IsAdmin())
        {
            return RedirectToLogin();
        }

        var query = _context.AuditLogs
            .Include(a => a.User)
            .AsQueryable();

        if (!string.IsNullOrEmpty(action))
        {
            query = query.Where(a => a.Action.Contains(action));
        }

        if (!string.IsNullOrEmpty(tableName))
        {
            query = query.Where(a => a.TableName == tableName);
        }

        if (userId.HasValue)
        {
            query = query.Where(a => a.UserId == userId.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(a => a.CreatedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(a => a.CreatedAt <= toDate.Value.AddDays(1));
        }

        int pageSize = 50;
        var totalItems = await query.CountAsync();
        var logs = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.Tables = await _context.AuditLogs.Select(a => a.TableName).Distinct().ToListAsync();
        ViewBag.Actions = await _context.AuditLogs.Select(a => a.Action).Distinct().ToListAsync();
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        ViewBag.Action = action;
        ViewBag.TableName = tableName;
        ViewBag.UserId = userId;
        ViewBag.FromDate = fromDate;
        ViewBag.ToDate = toDate;

        return View("~/Views/Admin/AuditLogs.cshtml", logs);
    }
}
