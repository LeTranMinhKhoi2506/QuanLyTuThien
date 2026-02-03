using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TuThien.Models;

namespace TuThien.Controllers.Admin;

/// <summary>
/// Controller quản lý giải ngân (Admin)
/// </summary>
public class AdminDisbursementsController : AdminBaseController
{
    public AdminDisbursementsController(TuThienContext context, ILogger<AdminDisbursementsController> logger)
        : base(context, logger)
    {
    }

    /// <summary>
    /// Danh sách yêu cầu giải ngân
    /// </summary>
    public async Task<IActionResult> Index(string? status, int page = 1)
    {
        if (!IsAdmin())
        {
            return RedirectToLogin();
        }

        var query = _context.DisbursementRequests
            .Include(d => d.Campaign)
            .Include(d => d.Requester)
            .Include(d => d.ApprovedByNavigation)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(d => d.Status == status);
        }

        int pageSize = 20;
        var totalItems = await query.CountAsync();
        var disbursements = await query
            .OrderByDescending(d => d.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        ViewBag.Status = status;
        ViewBag.PendingCount = await _context.DisbursementRequests.CountAsync(d => d.Status == "pending");

        return View("~/Views/Admin/Disbursements.cshtml", disbursements);
    }

    /// <summary>
    /// Chi tiết yêu cầu giải ngân
    /// </summary>
    public async Task<IActionResult> Detail(int id)
    {
        if (!IsAdmin())
        {
            return RedirectToLogin();
        }

        var disbursement = await _context.DisbursementRequests
            .Include(d => d.Campaign)
                .ThenInclude(c => c.Creator)
            .Include(d => d.Requester)
            .Include(d => d.ApprovedByNavigation)
            .FirstOrDefaultAsync(d => d.RequestId == id);

        if (disbursement == null)
        {
            return NotFound();
        }

        return View("~/Views/Admin/DisbursementDetail.cshtml", disbursement);
    }

    /// <summary>
    /// Phê duyệt giải ngân
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int requestId, string adminNote)
    {
        if (!IsAdmin())
        {
            return UnauthorizedJson();
        }

        var disbursement = await _context.DisbursementRequests.FindAsync(requestId);
        if (disbursement == null)
        {
            return Json(new { success = false, message = "Không tìm thấy yêu cầu" });
        }

        var oldStatus = disbursement.Status;
        disbursement.Status = "approved";
        disbursement.ApprovedBy = GetCurrentUserId();
        disbursement.ApprovedAt = DateTime.Now;
        disbursement.AdminNote = adminNote;

        await _context.SaveChangesAsync();
        await LogAuditAsync("APPROVE_DISBURSEMENT", "DisbursementRequests", requestId, oldStatus, "approved");

        var notification = new Notification
        {
            UserId = disbursement.RequesterId,
            Title = "Yêu cầu giải ngân được duyệt",
            Message = $"Yêu cầu giải ngân #{requestId} của bạn đã được phê duyệt.",
            Type = "payment",
            IsRead = false,
            CreatedAt = DateTime.Now
        };
        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        return Json(new { success = true, message = "Phê duyệt thành công" });
    }

    /// <summary>
    /// Từ chối giải ngân
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int requestId, string adminNote)
    {
        if (!IsAdmin())
        {
            return UnauthorizedJson();
        }

        var disbursement = await _context.DisbursementRequests.FindAsync(requestId);
        if (disbursement == null)
        {
            return Json(new { success = false, message = "Không tìm thấy yêu cầu" });
        }

        var oldStatus = disbursement.Status;
        disbursement.Status = "rejected";
        disbursement.ApprovedBy = GetCurrentUserId();
        disbursement.ApprovedAt = DateTime.Now;
        disbursement.AdminNote = adminNote;

        await _context.SaveChangesAsync();
        await LogAuditAsync("REJECT_DISBURSEMENT", "DisbursementRequests", requestId, oldStatus, "rejected");

        var notification = new Notification
        {
            UserId = disbursement.RequesterId,
            Title = "Yêu cầu giải ngân bị từ chối",
            Message = $"Yêu cầu giải ngân #{requestId} của bạn đã bị từ chối. Lý do: {adminNote}",
            Type = "payment",
            IsRead = false,
            CreatedAt = DateTime.Now
        };
        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        return Json(new { success = true, message = "Từ chối yêu cầu thành công" });
    }
}
