using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TuThien.Models;

namespace TuThien.Controllers.Admin;

/// <summary>
/// Controller quản lý quyên góp (Admin)
/// </summary>
public class AdminDonationsController : AdminBaseController
{
    public AdminDonationsController(TuThienContext context, ILogger<AdminDonationsController> logger)
        : base(context, logger)
    {
    }

    /// <summary>
    /// Danh sách quyên góp
    /// </summary>
    public async Task<IActionResult> Index()
    {
        if (!IsAdmin())
        {
            return RedirectToLogin();
        }

        var donations = await _context.Donations
            .Include(d => d.User)
            .Include(d => d.Campaign)
            .OrderByDescending(d => d.DonatedAt)
            .ToListAsync();

        ViewBag.TotalAmount = donations.Where(d => d.PaymentStatus == "success").Sum(d => d.Amount);
        ViewBag.TotalCount = donations.Count;

        return View("~/Views/Admin/Donations.cshtml", donations);
    }

    /// <summary>
    /// Xuất báo cáo quyên góp
    /// </summary>
    public async Task<IActionResult> Export(DateTime? fromDate, DateTime? toDate, string format = "csv")
    {
        if (!IsAdmin())
        {
            return RedirectToLogin();
        }

        var query = _context.Donations
            .Include(d => d.User)
            .Include(d => d.Campaign)
            .AsQueryable();

        if (fromDate.HasValue)
        {
            query = query.Where(d => d.DonatedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(d => d.DonatedAt <= toDate.Value.AddDays(1));
        }

        var donations = await query.OrderByDescending(d => d.DonatedAt).ToListAsync();

        await LogAuditAsync("EXPORT_DONATIONS", "Donations", 0, null, $"Exported {donations.Count} records");

        if (format == "csv")
        {
            var csv = new System.Text.StringBuilder();
            csv.AppendLine("STT,Mã GD,Ngày,Người quyên góp,Chiến dịch,Số tiền,Phương thức,Trạng thái");

            int stt = 1;
            foreach (var d in donations)
            {
                var donorName = d.IsAnonymous == true ? "Ẩn danh" : (d.User?.Username ?? "N/A");
                var campaignTitle = EscapeCsvField(d.Campaign?.Title ?? "");
                var paymentMethod = d.PaymentMethod ?? "";
                var paymentStatus = d.PaymentStatus ?? "";

                csv.AppendLine($"{stt},{d.TransactionCode},{d.DonatedAt:dd/MM/yyyy HH:mm},{donorName},{campaignTitle},{d.Amount},{paymentMethod},{paymentStatus}");
                stt++;
            }

            var bom = new byte[] { 0xEF, 0xBB, 0xBF };
            var csvBytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
            var bytes = new byte[bom.Length + csvBytes.Length];
            bom.CopyTo(bytes, 0);
            csvBytes.CopyTo(bytes, bom.Length);

            return File(bytes, "text/csv; charset=utf-8", $"SaoKe_QuenGop_{DateTime.Now:yyyyMMdd}.csv");
        }

        return BadRequest("Format không hỗ trợ");
    }

    /// <summary>
    /// Trang xuất báo cáo
    /// </summary>
    public IActionResult ExportReport()
    {
        if (!IsAdmin())
        {
            return RedirectToLogin();
        }

        return View("~/Views/Admin/ExportReport.cshtml");
    }

    /// <summary>
    /// Helper: Escape CSV field
    /// </summary>
    private static string EscapeCsvField(string field)
    {
        if (string.IsNullOrEmpty(field)) return field;

        if (field.Contains(',') || field.Contains('"') || field.Contains('\n'))
        {
            field = field.Replace("\"", "\"\"");
            return $"\"{field}\"";
        }
        return field;
    }
}
