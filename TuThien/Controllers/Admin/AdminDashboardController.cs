using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TuThien.Models;
using TuThien.ViewModels.Admin;

namespace TuThien.Controllers.Admin;

/// <summary>
/// Controller quản lý Dashboard (Admin)
/// </summary>
public class AdminDashboardController : AdminBaseController
{
    public AdminDashboardController(TuThienContext context, ILogger<AdminDashboardController> logger)
        : base(context, logger)
    {
    }

    /// <summary>
    /// Dashboard - Thống kê tổng quan
    /// </summary>
    public async Task<IActionResult> Index()
    {
        if (!IsAdmin())
        {
            return RedirectToLogin();
        }

        var latestDonationYear = await _context.Donations
            .Where(d => d.DonatedAt.HasValue && d.PaymentStatus == "success")
            .OrderByDescending(d => d.DonatedAt)
            .Select(d => d.DonatedAt!.Value.Year)
            .FirstOrDefaultAsync();

        var chartYear = latestDonationYear > 0 ? latestDonationYear : DateTime.Now.Year;

        // Thống kê theo danh mục
        var categoryStats = await _context.Categories
            .Select(c => new CategoryStatistic
            {
                CategoryId = c.CategoryId,
                CategoryName = c.Name,
                CampaignCount = c.Campaigns.Count,
                TotalDonations = c.Campaigns
                    .SelectMany(camp => camp.Donations)
                    .Where(d => d.PaymentStatus == "success")
                    .Sum(d => d.Amount),
                DonationCount = c.Campaigns
                    .SelectMany(camp => camp.Donations)
                    .Count(d => d.PaymentStatus == "success")
            })
            .ToListAsync();

        // Thống kê trạng thái chiến dịch
        var campaignStatusStats = await _context.Campaigns
            .GroupBy(c => c.Status)
            .Select(g => new CampaignStatusStatistic
            {
                Status = g.Key ?? "unknown",
                Count = g.Count()
            })
            .ToListAsync();

        // Map display name cho status
        var statusDisplayMap = new Dictionary<string, string>
        {
            { "draft", "Bản nháp" },
            { "pending_approval", "Chờ duyệt" },
            { "active", "Đang hoạt động" },
            { "paused", "Tạm dừng" },
            { "completed", "Hoàn thành" },
            { "rejected", "Từ chối" },
            { "locked", "Đã khóa" }
        };

        foreach (var stat in campaignStatusStats)
        {
            stat.StatusDisplay = statusDisplayMap.GetValueOrDefault(stat.Status, stat.Status);
        }

        var dashboardData = new AdminDashboardViewModel
        {
            TotalUsers = await _context.Users.CountAsync(),
            TotalCampaigns = await _context.Campaigns.CountAsync(),
            TotalDonations = await _context.Donations
                .Where(d => d.PaymentStatus == "success")
                .SumAsync(d => d.Amount),
            TotalDonationCount = await _context.Donations
                .CountAsync(d => d.PaymentStatus == "success"),
            PendingDisbursements = await _context.DisbursementRequests.CountAsync(d => d.Status == "pending"),
            PendingReports = await _context.Reports.CountAsync(r => r.Status == "pending"),
            ActiveCampaigns = await _context.Campaigns.CountAsync(c => c.Status == "active"),
            PendingCampaigns = await _context.Campaigns.CountAsync(c => c.Status == "pending_approval"),
            ChartYear = chartYear,
            CategoryStatistics = categoryStats,
            CampaignStatusStatistics = campaignStatusStats,

            MonthlyDonations = await _context.Donations
                .Where(d => d.DonatedAt.HasValue && d.DonatedAt.Value.Year == chartYear && d.PaymentStatus == "success")
                .GroupBy(d => d.DonatedAt!.Value.Month)
                .Select(g => new MonthlyStatistic
                {
                    Month = g.Key,
                    Amount = g.Sum(x => x.Amount),
                    Count = g.Count()
                })
                .OrderBy(x => x.Month)
                .ToListAsync(),

            TopCampaigns = await _context.Campaigns
                .Include(c => c.Creator)
                .Include(c => c.Category)
                .OrderByDescending(c => c.CurrentAmount)
                .Take(5)
                .ToListAsync(),

            RecentDonations = await _context.Donations
                .Include(d => d.User)
                .Include(d => d.Campaign)
                .Where(d => d.PaymentStatus == "success")
                .OrderByDescending(d => d.DonatedAt)
                .Take(10)
                .ToListAsync()
        };

        return View("~/Views/Admin/Index.cshtml", dashboardData);
    }

    /// <summary>
    /// API: Lấy dữ liệu biểu đồ theo năm
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetChartData(int year)
    {
        if (!IsAdmin())
        {
            return UnauthorizedJson();
        }

        var monthlyData = await _context.Donations
            .Where(d => d.DonatedAt.HasValue && d.DonatedAt.Value.Year == year && d.PaymentStatus == "success")
            .GroupBy(d => d.DonatedAt!.Value.Month)
            .Select(g => new
            {
                Month = g.Key,
                Amount = g.Sum(x => x.Amount),
                Count = g.Count()
            })
            .OrderBy(x => x.Month)
            .ToListAsync();

        // Đảm bảo có đủ 12 tháng
        var fullYearData = Enumerable.Range(1, 12)
            .Select(month => monthlyData.FirstOrDefault(m => m.Month == month) ?? new { Month = month, Amount = 0m, Count = 0 })
            .ToList();

        return Json(new { success = true, data = fullYearData });
    }

    /// <summary>
        /// API: Lấy thống kê nhanh
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetQuickStats()
        {
            if (!IsAdmin())
            {
                return UnauthorizedJson();
            }

            var today = DateTime.Today;
            var startOfWeek = today.AddDays(-(int)today.DayOfWeek);
            var startOfMonth = new DateTime(today.Year, today.Month, 1);

            var stats = new
            {
                TodayDonations = await _context.Donations
                    .Where(d => d.DonatedAt.HasValue && d.DonatedAt.Value.Date == today && d.PaymentStatus == "success")
                    .SumAsync(d => d.Amount),
            
                WeekDonations = await _context.Donations
                    .Where(d => d.DonatedAt.HasValue && d.DonatedAt.Value.Date >= startOfWeek && d.PaymentStatus == "success")
                    .SumAsync(d => d.Amount),
            
                MonthDonations = await _context.Donations
                    .Where(d => d.DonatedAt.HasValue && d.DonatedAt.Value.Date >= startOfMonth && d.PaymentStatus == "success")
                    .SumAsync(d => d.Amount),
            
                NewUsersToday = await _context.Users
                    .Where(u => u.CreatedAt.HasValue && u.CreatedAt.Value.Date == today)
                    .CountAsync(),
            
                PendingCampaigns = await _context.Campaigns
                    .CountAsync(c => c.Status == "pending" || c.Status == "pending_approval"),
            
                PendingDisbursements = await _context.DisbursementRequests
                    .CountAsync(d => d.Status == "pending"),
            
                PendingReports = await _context.Reports
                    .CountAsync(r => r.Status == "pending")
            };

            return Json(new { success = true, data = stats });
        }

        /// <summary>
        /// API: Lấy thống kê theo danh mục
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetCategoryStats()
        {
            if (!IsAdmin())
            {
                return UnauthorizedJson();
            }

            var categoryStats = await _context.Categories
                .Select(c => new
                {
                    CategoryId = c.CategoryId,
                    CategoryName = c.Name,
                    CampaignCount = c.Campaigns.Count,
                    TotalDonations = c.Campaigns
                        .SelectMany(camp => camp.Donations)
                        .Where(d => d.PaymentStatus == "success")
                        .Sum(d => d.Amount),
                    DonationCount = c.Campaigns
                        .SelectMany(camp => camp.Donations)
                        .Count(d => d.PaymentStatus == "success")
                })
                .ToListAsync();

            return Json(new { success = true, data = categoryStats });
        }
    }
