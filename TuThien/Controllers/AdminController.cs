using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TuThien.Models;
using System.Text.Json;

namespace TuThien.Controllers
{
    public class AdminController : Controller
    {
        private readonly TuThienContext _context;
        private readonly ILogger<AdminController> _logger;

        public AdminController(TuThienContext context, ILogger<AdminController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Kiểm tra quyền Admin
        private bool IsAdmin()
        {
            var role = HttpContext.Session.GetString("Role");
            return role == "admin";
        }

        private int? GetCurrentUserId()
        {
            return HttpContext.Session.GetInt32("UserId");
        }


        // Ghi audit log
        private async Task LogAuditAsync(string action, string tableName, int recordId, object? oldValue = null, object? newValue = null)
        {
            var userId = GetCurrentUserId();
            // Chuyển đổi giá trị thành JSON để đáp ứng constraint CK_Audit_New_Json và CK_Audit_Old_Json
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



        // 5.2 Dashboard - Thống kê tổng quan
        public async Task<IActionResult> Index()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            // Lấy năm có dữ liệu quyên góp gần nhất (để hỗ trợ dữ liệu test)
            var latestDonationYear = await _context.Donations
                .Where(d => d.DonatedAt.HasValue)
                .OrderByDescending(d => d.DonatedAt)
                .Select(d => d.DonatedAt!.Value.Year)
                .FirstOrDefaultAsync();
            
            // Nếu không có dữ liệu, dùng năm hiện tại
            var chartYear = latestDonationYear > 0 ? latestDonationYear : DateTime.Now.Year;

            var dashboardData = new AdminDashboardViewModel
            {
                TotalUsers = await _context.Users.CountAsync(),
                TotalCampaigns = await _context.Campaigns.CountAsync(),
                TotalDonations = await _context.Donations.SumAsync(d => d.Amount),
                TotalDonationCount = await _context.Donations.CountAsync(),
                PendingDisbursements = await _context.DisbursementRequests.CountAsync(d => d.Status == "pending"),
                PendingReports = await _context.Reports.CountAsync(r => r.Status == "pending"),
                ActiveCampaigns = await _context.Campaigns.CountAsync(c => c.Status == "active"),
                ChartYear = chartYear,
                
                // Thống kê theo tháng - sử dụng năm từ dữ liệu
                MonthlyDonations = await _context.Donations
                    .Where(d => d.DonatedAt.HasValue && d.DonatedAt.Value.Year == chartYear)
                    .GroupBy(d => d.DonatedAt!.Value.Month)
                    .Select(g => new MonthlyStatistic 
                    { 
                        Month = g.Key, 
                        Amount = g.Sum(x => x.Amount),
                        Count = g.Count()
                    })
                    .OrderBy(x => x.Month)
                    .ToListAsync(),

                // Top 5 chiến dịch
                TopCampaigns = await _context.Campaigns
                    .Include(c => c.Creator)
                    .OrderByDescending(c => c.CurrentAmount)
                    .Take(5)
                    .ToListAsync(),

                // Quyên góp gần đây
                RecentDonations = await _context.Donations
                    .Include(d => d.User)
                    .Include(d => d.Campaign)
                    .OrderByDescending(d => d.DonatedAt)
                    .Take(10)
                    .ToListAsync()
            };

            return View(dashboardData);
        }

        #region 5.1 Quản lý người dùng

        public async Task<IActionResult> Users(string? search, string? role, string? status, int page = 1)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            var query = _context.Users.Include(u => u.UserProfile).AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(u => u.Username.Contains(search) || u.Email.Contains(search));
            }

            if (!string.IsNullOrEmpty(role))
            {
                query = query.Where(u => u.Role == role);
            }

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(u => u.Status == status);
            }

            int pageSize = 20;
            var totalItems = await query.CountAsync();
            var users = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            ViewBag.Search = search;
            ViewBag.Role = role;
            ViewBag.Status = status;

            return View(users);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateUserStatus(int userId, string status)
        {
            if (!IsAdmin())
            {
                return Json(new { success = false, message = "Không có quyền" });
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return Json(new { success = false, message = "Không tìm thấy người dùng" });
            }

            var oldStatus = user.Status;
            user.Status = status;
            user.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            await LogAuditAsync("UPDATE_USER_STATUS", "Users", userId, oldStatus, status);

            return Json(new { success = true, message = "Cập nhật trạng thái thành công" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateUserRole(int userId, string role)
        {
            if (!IsAdmin())
            {
                return Json(new { success = false, message = "Không có quyền" });
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return Json(new { success = false, message = "Không tìm thấy người dùng" });
            }

            var oldRole = user.Role;
            user.Role = role;
            user.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            await LogAuditAsync("UPDATE_USER_ROLE", "Users", userId, oldRole, role);

            return Json(new { success = true, message = "Cập nhật vai trò thành công" });
        }

        #endregion

        #region 3.3 Xem lịch sử quyên góp

        public async Task<IActionResult> Donations(string? search, string? paymentMethod, DateTime? fromDate, DateTime? toDate, int page = 1)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            var query = _context.Donations
                .Include(d => d.User)
                .Include(d => d.Campaign)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(d => 
                    (d.User != null && d.User.Username.Contains(search)) ||
                    d.Campaign.Title.Contains(search) ||
                    (d.TransactionCode != null && d.TransactionCode.Contains(search)));
            }

            if (!string.IsNullOrEmpty(paymentMethod))
            {
                query = query.Where(d => d.PaymentMethod == paymentMethod);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(d => d.DonatedAt >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(d => d.DonatedAt <= toDate.Value.AddDays(1));
            }

            int pageSize = 20;
            var totalItems = await query.CountAsync();
            var donations = await query
                .OrderByDescending(d => d.DonatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Thống kê
            ViewBag.TotalAmount = await query.SumAsync(d => d.Amount);
            ViewBag.TotalCount = totalItems;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            ViewBag.Search = search;
            ViewBag.PaymentMethod = paymentMethod;
            ViewBag.FromDate = fromDate;
            ViewBag.ToDate = toDate;

            return View(donations);
        }

        #endregion

        #region 3.4 Phê duyệt giải ngân

        public async Task<IActionResult> Disbursements(string? status, int page = 1)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
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

            return View(disbursements);
        }

        public async Task<IActionResult> DisbursementDetail(int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
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

            return View(disbursement);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveDisbursement(int requestId, string adminNote)
        {
            if (!IsAdmin())
            {
                return Json(new { success = false, message = "Không có quyền" });
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

            // Gửi thông báo cho người yêu cầu
            var notification = new Notification
            {
                UserId = disbursement.RequesterId,
                Title = "Yêu cầu giải ngân được duyệt",
                Message = $"Yêu cầu giải ngân #{requestId} của bạn đã được phê duyệt.",
                Type = "disbursement",
                IsRead = false,
                CreatedAt = DateTime.Now
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Phê duyệt thành công" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectDisbursement(int requestId, string adminNote)
        {
            if (!IsAdmin())
            {
                return Json(new { success = false, message = "Không có quyền" });
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

            // Gửi thông báo
            var notification = new Notification
            {
                UserId = disbursement.RequesterId,
                Title = "Yêu cầu giải ngân bị từ chối",
                Message = $"Yêu cầu giải ngân #{requestId} của bạn đã bị từ chối. Lý do: {adminNote}",
                Type = "disbursement",
                IsRead = false,
                CreatedAt = DateTime.Now
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Từ chối yêu cầu thành công" });
        }

        #endregion

        #region 4.4 Báo cáo sai phạm

        public async Task<IActionResult> Reports(string? status, string? targetType, int page = 1)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
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

            return View(reports);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateReportStatus(int reportId, string status)
        {
            if (!IsAdmin())
            {
                return Json(new { success = false, message = "Không có quyền" });
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

        #endregion

        #region 5.4 Nhật ký hệ thống (Audit Logs)

        public async Task<IActionResult> AuditLogs(string? action, string? tableName, int? userId, DateTime? fromDate, DateTime? toDate, int page = 1)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
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

            // Lấy danh sách các bảng và action để filter
            ViewBag.Tables = await _context.AuditLogs.Select(a => a.TableName).Distinct().ToListAsync();
            ViewBag.Actions = await _context.AuditLogs.Select(a => a.Action).Distinct().ToListAsync();
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            ViewBag.Action = action;
            ViewBag.TableName = tableName;
            ViewBag.UserId = userId;
            ViewBag.FromDate = fromDate;
            ViewBag.ToDate = toDate;

            return View(logs);
        }

        #endregion

        #region 2.2 Phê duyệt chiến dịch

        public async Task<IActionResult> Campaigns(string? status, string? search, int page = 1)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            var query = _context.Campaigns
                .Include(c => c.Creator)
                .Include(c => c.Category)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(c => c.Status == status);
            }

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(c => c.Title.Contains(search) || c.Creator.Username.Contains(search));
            }

            int pageSize = 20;
            var totalItems = await query.CountAsync();
            var campaigns = await query
                .OrderByDescending(c => c.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            ViewBag.Status = status;
            ViewBag.Search = search;
            ViewBag.PendingCount = await _context.Campaigns.CountAsync(c => c.Status == "pending_approval");

            return View(campaigns);
        }

        public async Task<IActionResult> CampaignDetail(int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            var campaign = await _context.Campaigns
                .Include(c => c.Creator)
                    .ThenInclude(u => u.UserProfile)
                .Include(c => c.Category)
                .Include(c => c.CampaignDocuments)
                .Include(c => c.CampaignMilestones)
                .Include(c => c.DisbursementRequests)
                    .ThenInclude(d => d.Requester)
                .FirstOrDefaultAsync(c => c.CampaignId == id);

            if (campaign == null)
            {
                return NotFound();
            }

            return View(campaign);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveCampaign(int campaignId, string? note)
        {
            if (!IsAdmin())
            {
                return Json(new { success = false, message = "Không có quyền" });
            }

            var campaign = await _context.Campaigns.FindAsync(campaignId);
            if (campaign == null)
            {
                return Json(new { success = false, message = "Không tìm thấy chiến dịch" });
            }

            var oldStatus = campaign.Status;
            campaign.Status = "active";
            campaign.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            await LogAuditAsync("APPROVE_CAMPAIGN", "Campaigns", campaignId, oldStatus, "active");

            // Gửi thông báo
            var notification = new Notification
            {
                UserId = campaign.CreatorId,
                Title = "Chiến dịch được phê duyệt",
                Message = $"Chiến dịch \"{campaign.Title}\" của bạn đã được phê duyệt và đang hoạt động.",
                Type = "campaign",
                IsRead = false,
                CreatedAt = DateTime.Now
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Phê duyệt chiến dịch thành công" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectCampaign(int campaignId, string note)
        {
            if (!IsAdmin())
            {
                return Json(new { success = false, message = "Không có quyền" });
            }

            var campaign = await _context.Campaigns.FindAsync(campaignId);
            if (campaign == null)
            {
                return Json(new { success = false, message = "Không tìm thấy chiến dịch" });
            }

            var oldStatus = campaign.Status;
            campaign.Status = "rejected";
            campaign.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            await LogAuditAsync("REJECT_CAMPAIGN", "Campaigns", campaignId, oldStatus, "rejected");

            // Gửi thông báo
            var notification = new Notification
            {
                UserId = campaign.CreatorId,
                Title = "Chiến dịch bị từ chối",
                Message = $"Chiến dịch \"{campaign.Title}\" của bạn đã bị từ chối. Lý do: {note}",
                Type = "campaign",
                IsRead = false,
                CreatedAt = DateTime.Now
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Từ chối chiến dịch thành công" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CloseCampaign(int campaignId, string note)
        {
            if (!IsAdmin())
            {
                return Json(new { success = false, message = "Không có quyền" });
            }

            var campaign = await _context.Campaigns.FindAsync(campaignId);
            if (campaign == null)
            {
                return Json(new { success = false, message = "Không tìm thấy chiến dịch" });
            }

            var oldStatus = campaign.Status;
            campaign.Status = "closed";
            campaign.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            await LogAuditAsync("CLOSE_CAMPAIGN", "Campaigns", campaignId, oldStatus, "closed");

            return Json(new { success = true, message = "Đóng chiến dịch thành công" });
        }

        #endregion

        #region 4.3 Gửi thông báo

        public IActionResult SendNotification()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendNotification(string title, string message, string targetType, int? targetUserId)
        {
            if (!IsAdmin())
            {
                return Json(new { success = false, message = "Không có quyền" });
            }

            var notifications = new List<Notification>();

            if (targetType == "all")
            {
                var users = await _context.Users.Where(u => u.Status == "active").ToListAsync();
                foreach (var user in users)
                {
                    notifications.Add(new Notification
                    {
                        UserId = user.UserId,
                        Title = title,
                        Message = message,
                        Type = "system",
                        IsRead = false,
                        CreatedAt = DateTime.Now
                    });
                }
            }
            else if (targetType == "single" && targetUserId.HasValue)
            {
                notifications.Add(new Notification
                {
                    UserId = targetUserId.Value,
                    Title = title,
                    Message = message,
                    Type = "system",
                    IsRead = false,
                    CreatedAt = DateTime.Now
                });
            }

            _context.Notifications.AddRange(notifications);
            await _context.SaveChangesAsync();
            await LogAuditAsync("SEND_NOTIFICATION", "Notifications", 0, null, $"Sent to {notifications.Count} users");

            return Json(new { success = true, message = $"Đã gửi thông báo đến {notifications.Count} người dùng" });
        }

        #endregion

        #region 3.5 Xuất báo cáo sao kê

        public async Task<IActionResult> ExportDonations(DateTime? fromDate, DateTime? toDate, string format = "csv")
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
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
                    // Escape các giá trị có thể chứa dấu phẩy bằng cách bọc trong dấu ngoặc kép
                    var donorName = d.IsAnonymous == true ? "Ẩn danh" : (d.User?.Username ?? "N/A");
                    var campaignTitle = EscapeCsvField(d.Campaign?.Title ?? "");
                    var paymentMethod = d.PaymentMethod ?? "";
                    var paymentStatus = d.PaymentStatus ?? "";
                    
                    // Không format số tiền với dấu phẩy, để nguyên số
                    csv.AppendLine($"{stt},{d.TransactionCode},{d.DonatedAt:dd/MM/yyyy HH:mm},{donorName},{campaignTitle},{d.Amount},{paymentMethod},{paymentStatus}");
                    stt++;
                }

                // Thêm BOM để Excel nhận diện UTF-8
                var bom = new byte[] { 0xEF, 0xBB, 0xBF };
                var csvBytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
                var bytes = new byte[bom.Length + csvBytes.Length];
                bom.CopyTo(bytes, 0);
                csvBytes.CopyTo(bytes, bom.Length);
                
                return File(bytes, "text/csv; charset=utf-8", $"SaoKe_QuenGop_{DateTime.Now:yyyyMMdd}.csv");
            }

            return BadRequest("Format không hỗ trợ");
        }
        
        // Helper method để escape các field CSV có chứa dấu phẩy hoặc dấu ngoặc kép
        private string EscapeCsvField(string field)
        {
            if (string.IsNullOrEmpty(field)) return field;
            
            // Nếu field chứa dấu phẩy, dấu ngoặc kép hoặc xuống dòng, bọc trong dấu ngoặc kép
            if (field.Contains(',') || field.Contains('"') || field.Contains('\n'))
            {
                // Escape dấu ngoặc kép bằng cách nhân đôi
                field = field.Replace("\"", "\"\"");
                return $"\"{field}\"";
            }
            return field;
        }

        public IActionResult ExportReport()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            return View();
        }

        #endregion

        #region API Notifications (Realtime)

        /// <summary>
        /// Lấy danh sách thông báo cho admin (bao gồm pending items)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetNotifications()
        {
            if (!IsAdmin())
            {
                return Json(new { success = false, message = "Không có quyền" });
            }

            var userId = GetCurrentUserId();
            
            // Lấy các thông báo của admin user
            var userNotifications = await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(10)
                .ToListAsync();

            // Lấy các pending items (chiến dịch chờ duyệt, giải ngân chờ duyệt, báo cáo chờ xử lý)
            var pendingCampaigns = await _context.Campaigns
                .Where(c => c.Status == "pending")
                .OrderByDescending(c => c.CreatedAt)
                .Take(5)
                .Select(c => new { c.CampaignId, c.Title, c.CreatedAt })
                .ToListAsync();

            var pendingDisbursements = await _context.DisbursementRequests
                .Include(d => d.Campaign)
                .Where(d => d.Status == "pending")
                .OrderByDescending(d => d.CreatedAt)
                .Take(5)
                .Select(d => new { d.RequestId, d.Campaign!.Title, d.CreatedAt })
                .ToListAsync();

            var pendingReports = await _context.Reports
                .Where(r => r.Status == "pending")
                .OrderByDescending(r => r.CreatedAt)
                .Take(5)
                .Select(r => new { r.ReportId, r.Reason, r.CreatedAt })
                .ToListAsync();

            // Tạo danh sách thông báo tổng hợp
            var notifications = new List<object>();

            // Thêm pending campaigns
            foreach (var c in pendingCampaigns)
            {
                notifications.Add(new
                {
                    id = c.CampaignId,
                    title = "Chiến dịch chờ duyệt",
                    message = c.Title?.Length > 50 ? c.Title.Substring(0, 50) + "..." : c.Title,
                    type = "campaign",
                    isRead = false,
                    url = Url.Action("CampaignDetail", "Admin", new { id = c.CampaignId }),
                    timeAgo = GetTimeAgo(c.CreatedAt)
                });
            }

            // Thêm pending disbursements
            foreach (var d in pendingDisbursements)
            {
                notifications.Add(new
                {
                    id = d.RequestId,
                    title = "Yêu cầu giải ngân",
                    message = d.Title?.Length > 50 ? d.Title.Substring(0, 50) + "..." : d.Title,
                    type = "disbursement",
                    isRead = false,
                    url = Url.Action("DisbursementDetail", "Admin", new { id = d.RequestId }),
                    timeAgo = GetTimeAgo(d.CreatedAt)
                });
            }

            // Thêm pending reports
            foreach (var r in pendingReports)
            {
                notifications.Add(new
                {
                    id = r.ReportId,
                    title = "Báo cáo sai phạm",
                    message = r.Reason?.Length > 50 ? r.Reason.Substring(0, 50) + "..." : r.Reason,
                    type = "report",
                    isRead = false,
                    url = Url.Action("Reports", "Admin"),
                    timeAgo = GetTimeAgo(r.CreatedAt)
                });
            }

            // Thêm user notifications
            foreach (var n in userNotifications)
            {
                notifications.Add(new
                {
                    id = n.NotificationId,
                    title = n.Title,
                    message = n.Message?.Length > 50 ? n.Message.Substring(0, 50) + "..." : n.Message,
                    type = n.Type ?? "system",
                    isRead = n.IsRead ?? false,
                    url = "#",
                    timeAgo = GetTimeAgo(n.CreatedAt)
                });
            }

            // Sắp xếp theo thời gian
            var sortedNotifications = notifications.Take(10).ToList();
            var unreadCount = pendingCampaigns.Count + pendingDisbursements.Count + pendingReports.Count 
                + userNotifications.Count(n => n.IsRead != true);

            return Json(new
            {
                success = true,
                unreadCount = unreadCount,
                notifications = sortedNotifications
            });
        }

        /// <summary>
        /// Đánh dấu thông báo đã đọc
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> MarkNotificationRead(int id)
        {
            if (!IsAdmin())
            {
                return Json(new { success = false });
            }

            var notification = await _context.Notifications.FindAsync(id);
            if (notification != null)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
            }

            return Json(new { success = true });
        }

        /// <summary>
        /// Đánh dấu tất cả thông báo đã đọc
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> MarkAllNotificationsRead()
        {
            if (!IsAdmin())
            {
                return Json(new { success = false });
            }

            var userId = GetCurrentUserId();
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId && n.IsRead != true)
                .ToListAsync();

            foreach (var n in notifications)
            {
                n.IsRead = true;
            }

            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        /// <summary>
        /// Xem tất cả thông báo
        /// </summary>
        public async Task<IActionResult> Notifications()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            var userId = GetCurrentUserId();
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(50)
                .ToListAsync();

            return View(notifications);
        }

        /// <summary>
        /// Helper: Tính thời gian tương đối
        /// </summary>
        private string GetTimeAgo(DateTime? dateTime)
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

        #endregion
    }

    // ViewModels cho Admin
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
        public List<MonthlyStatistic> MonthlyDonations { get; set; } = new();
        public List<Campaign> TopCampaigns { get; set; } = new();
        public List<Donation> RecentDonations { get; set; } = new();
    }

    public class MonthlyStatistic
    {
        public int Month { get; set; }
        public decimal Amount { get; set; }
        public int Count { get; set; }
    }
}
