using TuThien.Models;

namespace TuThien.ViewModels.Admin;

/// <summary>
/// ViewModel cho Admin Dashboard
/// </summary>
public class AdminDashboardViewModel
{
    public int TotalUsers { get; set; }
    public int TotalCampaigns { get; set; }
    public decimal TotalDonations { get; set; }
    public int TotalDonationCount { get; set; }
    public int PendingDisbursements { get; set; }
    public int PendingReports { get; set; }
    public int ActiveCampaigns { get; set; }
    public int ChartYear { get; set; }
    public List<MonthlyStatistic> MonthlyDonations { get; set; } = [];
    public List<Campaign> TopCampaigns { get; set; } = [];
    public List<Donation> RecentDonations { get; set; } = [];
}

/// <summary>
/// Thống kê theo tháng
/// </summary>
public class MonthlyStatistic
{
    public int Month { get; set; }
    public decimal Amount { get; set; }
    public int Count { get; set; }
}
