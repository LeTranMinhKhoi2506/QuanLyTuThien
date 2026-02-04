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
    public int PendingCampaigns { get; set; }
    public int ChartYear { get; set; }
    public List<MonthlyStatistic> MonthlyDonations { get; set; } = [];
    public List<Campaign> TopCampaigns { get; set; } = [];
    public List<Donation> RecentDonations { get; set; } = [];
    public List<CategoryStatistic> CategoryStatistics { get; set; } = [];
    public List<CampaignStatusStatistic> CampaignStatusStatistics { get; set; } = [];
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

/// <summary>
/// Thống kê theo danh mục
/// </summary>
public class CategoryStatistic
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int CampaignCount { get; set; }
    public decimal TotalDonations { get; set; }
    public int DonationCount { get; set; }
}

/// <summary>
/// Thống kê theo trạng thái chiến dịch
/// </summary>
public class CampaignStatusStatistic
{
    public string Status { get; set; } = string.Empty;
    public string StatusDisplay { get; set; } = string.Empty;
    public int Count { get; set; }
}
