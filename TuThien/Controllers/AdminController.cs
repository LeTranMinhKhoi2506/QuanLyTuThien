using Microsoft.AspNetCore.Mvc;
using TuThien.Models;

namespace TuThien.Controllers;

/// <summary>
/// AdminController - Chỉ chứa redirect actions cho backward compatibility
/// Dashboard đã được tách ra AdminDashboardController
/// Các chức năng chi tiết đã được tách ra các controller riêng trong namespace TuThien.Controllers.Admin:
/// - AdminDashboardController: Dashboard thống kê
/// - AdminUsersController: Quản lý người dùng
/// - AdminCampaignsController: Quản lý chiến dịch
/// - AdminDonationsController: Quản lý quyên góp
/// - AdminDisbursementsController: Quản lý giải ngân
/// - AdminReportsController: Quản lý báo cáo sai phạm
/// - AdminAuditController: Nhật ký hệ thống
/// - AdminNotificationsController: Quản lý thông báo
/// </summary>
public class AdminController : Controller
{
    #region Redirect Actions cho backward compatibility
    // Các actions này redirect đến các controllers mới để giữ URL cũ hoạt động

    // Dashboard
    public IActionResult Index()
        => RedirectToAction("Index", "AdminDashboard");

    // Users
    public IActionResult Users(string? search, string? role, string? status, int page = 1)
        => RedirectToAction("Index", "AdminUsers", new { search, role, status, page });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult UpdateUserStatus(int userId, string status)
        => RedirectToAction("UpdateStatus", "AdminUsers", new { userId, status });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult UpdateUserRole(int userId, string role)
        => RedirectToAction("UpdateRole", "AdminUsers", new { userId, role });

    [HttpGet]
    public IActionResult CheckUsernameExists(string username, int? excludeUserId = null)
        => RedirectToAction("CheckUsernameExists", "AdminUsers", new { username, excludeUserId });

    [HttpGet]
    public IActionResult CheckEmailExists(string email, int? excludeUserId = null)
        => RedirectToAction("CheckEmailExists", "AdminUsers", new { email, excludeUserId });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CreateUser()
        => RedirectToAction("Create", "AdminUsers");

    [HttpGet]
    public IActionResult GetUser(int id)
        => RedirectToAction("Get", "AdminUsers", new { id });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult UpdateUser()
        => RedirectToAction("Update", "AdminUsers");

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteUser(int userId)
        => RedirectToAction("Delete", "AdminUsers", new { userId });

    // Campaigns
    public IActionResult Campaigns(string? status, string? search, int page = 1)
        => RedirectToAction("Index", "AdminCampaigns", new { status, search, page });

    public IActionResult CampaignDetail(int id)
        => RedirectToAction("Detail", "AdminCampaigns", new { id });

    [HttpGet]
    public IActionResult CheckCampaignTitleExists(string title, int? excludeCampaignId = null)
        => RedirectToAction("CheckTitleExists", "AdminCampaigns", new { title, excludeCampaignId });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ApproveCampaign(int campaignId, string? note)
        => RedirectToAction("Approve", "AdminCampaigns", new { campaignId, note });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult RejectCampaign(int campaignId, string note)
        => RedirectToAction("Reject", "AdminCampaigns", new { campaignId, note });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CloseCampaign(int campaignId, string? note)
        => RedirectToAction("Close", "AdminCampaigns", new { campaignId, note });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult UpdateCampaignStatus(int campaignId, string status)
        => RedirectToAction("UpdateStatus", "AdminCampaigns", new { campaignId, status });

    [HttpGet]
    public IActionResult GetCategories()
        => RedirectToAction("GetCategories", "AdminCampaigns");

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CreateCampaign()
        => RedirectToAction("Create", "AdminCampaigns");

    [HttpGet]
    public IActionResult GetCampaign(int id)
        => RedirectToAction("Get", "AdminCampaigns", new { id });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult UpdateCampaign()
        => RedirectToAction("Update", "AdminCampaigns");

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteCampaign(int campaignId)
        => RedirectToAction("Delete", "AdminCampaigns", new { campaignId });

    // Donations
    public IActionResult Donations(string? search, string? paymentMethod, DateTime? fromDate, DateTime? toDate, int page = 1)
        => RedirectToAction("Index", "AdminDonations", new { search, paymentMethod, fromDate, toDate, page });

    public IActionResult ExportDonations(DateTime? fromDate, DateTime? toDate, string format = "csv")
        => RedirectToAction("Export", "AdminDonations", new { fromDate, toDate, format });

    public IActionResult ExportReport()
        => RedirectToAction("ExportReport", "AdminDonations");

    // Disbursements
    public IActionResult Disbursements(string? status, int page = 1)
        => RedirectToAction("Index", "AdminDisbursements", new { status, page });

    public IActionResult DisbursementDetail(int id)
        => RedirectToAction("Detail", "AdminDisbursements", new { id });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ApproveDisbursement(int requestId, string adminNote)
        => RedirectToAction("Approve", "AdminDisbursements", new { requestId, adminNote });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult RejectDisbursement(int requestId, string adminNote)
        => RedirectToAction("Reject", "AdminDisbursements", new { requestId, adminNote });

    // Reports
    public IActionResult Reports(string? status, string? targetType, int page = 1)
        => RedirectToAction("Index", "AdminReports", new { status, targetType, page });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult UpdateReportStatus(int reportId, string status)
        => RedirectToAction("UpdateStatus", "AdminReports", new { reportId, status });

    // Audit Logs
    public IActionResult AuditLogs(string? action, string? tableName, int? userId, DateTime? fromDate, DateTime? toDate, int page = 1)
        => RedirectToAction("Index", "AdminAudit", new { action, tableName, userId, fromDate, toDate, page });

    // Notifications
    public IActionResult SendNotification()
        => RedirectToAction("Send", "AdminNotifications");

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SendNotificationPost(string title, string message, string targetType, int? targetUserId)
        => RedirectToAction("Send", "AdminNotifications", new { title, message, targetType, targetUserId });

    [HttpGet]
    public IActionResult GetNotifications()
        => RedirectToAction("Get", "AdminNotifications");

    [HttpPost]
    public IActionResult MarkNotificationRead(int id)
        => RedirectToAction("MarkRead", "AdminNotifications", new { id });

    [HttpPost]
    public IActionResult MarkAllNotificationsRead()
        => RedirectToAction("MarkAllRead", "AdminNotifications");

    public IActionResult Notifications()
        => RedirectToAction("Index", "AdminNotifications");

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SendNotificationByRole(string title, string message, string role)
        => RedirectToAction("SendByRole", "AdminNotifications", new { title, message, role });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SendNotificationToDonors(string title, string message, int campaignId)
        => RedirectToAction("SendToDonors", "AdminNotifications", new { title, message, campaignId });

    #endregion
}
