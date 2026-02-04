using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TuThien.Models;
using BCrypt.Net;

namespace TuThien.Controllers
{
    public class AccountController : Controller
    {
        private readonly TuThienContext _context;
        private readonly ILogger<AccountController> _logger;

        public AccountController(TuThienContext context, ILogger<AccountController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Tìm user theo username hoặc email
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => 
                        u.Username == model.UsernameOrEmail || 
                        u.Email == model.UsernameOrEmail);

                if (user == null)
                {
                    ModelState.AddModelError(string.Empty, "Tên đăng nhập hoặc mật khẩu không đúng");
                    return View(model);
                }

                // Kiểm tra trạng thái tài khoản
                if (user.Status == "locked")
                {
                    ModelState.AddModelError(string.Empty, "Tài khoản đã bị khóa. Vui lòng liên hệ quản trị viên.");
                    return View(model);
                }

                // Kiểm tra password - hỗ trợ cả password chưa hash và đã hash
                bool isPasswordValid = false;
                bool needsPasswordUpgrade = false;

                // Kiểm tra nếu password trong DB là BCrypt hash (bắt đầu bằng $2)
                if (user.PasswordHash != null && user.PasswordHash.StartsWith("$2"))
                {
                    // Password đã được hash, verify bằng BCrypt
                    isPasswordValid = BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash);
                }
                else
                {
                    // Password chưa được hash (plain text), so sánh trực tiếp
                    isPasswordValid = user.PasswordHash == model.Password;
                    if (isPasswordValid)
                    {
                        needsPasswordUpgrade = true;
                    }
                }

                if (!isPasswordValid)
                {
                    ModelState.AddModelError(string.Empty, "Tên đăng nhập hoặc mật khẩu không đúng");
                    return View(model);
                }

                // Nếu password chưa được hash, tự động hash và cập nhật
                if (needsPasswordUpgrade)
                {
                    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);
                    user.UpdatedAt = DateTime.Now;
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Password upgraded to BCrypt hash for user {user.Username}");
                }

                // Đăng nhập thành công - Lưu thông tin vào session
                HttpContext.Session.SetInt32("UserId", user.UserId);
                HttpContext.Session.SetString("Username", user.Username);
                HttpContext.Session.SetString("Email", user.Email);
                HttpContext.Session.SetString("Role", user.Role ?? "user");

                // Ghi log
                _logger.LogInformation($"User {user.Username} logged in successfully");

                // Redirect về trang trước đó hoặc trang chủ
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                if (user.Role == "admin")
                {
                    return RedirectToAction("Index", "Admin");
                }

                return RedirectToAction("Index", "TrangChu");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi. Vui lòng thử lại sau.");
                return View(model);
            }
        }

        // GET: /Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Kiểm tra username đã tồn tại
                if (await _context.Users.AnyAsync(u => u.Username == model.Username))
                {
                    ModelState.AddModelError("Username", "Tên đăng nhập đã tồn tại");
                    return View(model);
                }

                // Kiểm tra email đã tồn tại
                if (await _context.Users.AnyAsync(u => u.Email == model.Email))
                {
                    ModelState.AddModelError("Email", "Email đã được sử dụng");
                    return View(model);
                }

                // Hash password với BCrypt
                var passwordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);

                // Tạo user mới
                var user = new User
                {
                    Username = model.Username,
                    Email = model.Email,
                    PasswordHash = passwordHash,
                    PhoneNumber = model.PhoneNumber,
                    Role = "user",
                    Status = "active",
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Tạo profile
                var profile = new UserProfile
                {
                    UserId = user.UserId,
                    FullName = model.FullName
                };

                _context.UserProfiles.Add(profile);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"New user registered: {user.Username}");

                TempData["SuccessMessage"] = "Đăng ký thành công! Vui lòng đăng nhập.";
                return RedirectToAction(nameof(Login));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration");
                ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi. Vui lòng thử lại sau.");
                return View(model);
            }
        }

        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            _logger.LogInformation("User logged out");
            return RedirectToAction("Index", "TrangChu");
        }

        // GET: /Account/Profile
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction(nameof(Login));
            }

            var user = await _context.Users
                .Include(u => u.UserProfile)
                .FirstOrDefaultAsync(u => u.UserId == userId.Value);

            if (user == null)
                        {
                            return NotFound();
                        }

                        return View(user);
                    }

                    // POST: /Account/UpdateProfile
                    [HttpPost]
                    [ValidateAntiForgeryToken]
                    public async Task<IActionResult> UpdateProfile(int userId, string email, string? fullName, string? phoneNumber, string? address, string? bio)
                    {
                        var sessionUserId = HttpContext.Session.GetInt32("UserId");
                        if (sessionUserId == null || sessionUserId != userId)
                        {
                            return RedirectToAction(nameof(Login));
                        }

                        try
                        {
                            var user = await _context.Users
                                .Include(u => u.UserProfile)
                                .FirstOrDefaultAsync(u => u.UserId == userId);

                            if (user == null)
                            {
                                return NotFound();
                            }

                            // Kiểm tra email trùng (nếu thay đổi)
                            if (user.Email != email && await _context.Users.AnyAsync(u => u.Email == email && u.UserId != userId))
                            {
                                TempData["ErrorMessage"] = "Email đã được sử dụng bởi tài khoản khác";
                                return RedirectToAction(nameof(Profile));
                            }

                            // Cập nhật thông tin user
                            user.Email = email;
                            user.PhoneNumber = phoneNumber;
                            user.UpdatedAt = DateTime.Now;

                            // Cập nhật hoặc tạo profile
                            if (user.UserProfile == null)
                            {
                                user.UserProfile = new UserProfile
                                {
                                    UserId = userId,
                                    FullName = fullName,
                                    Address = address,
                                    Bio = bio
                                };
                                _context.UserProfiles.Add(user.UserProfile);
                            }
                            else
                            {
                                user.UserProfile.FullName = fullName;
                                user.UserProfile.Address = address;
                                user.UserProfile.Bio = bio;
                            }

                            await _context.SaveChangesAsync();

                            // Cập nhật session
                            HttpContext.Session.SetString("Email", email);

                            TempData["SuccessMessage"] = "Cập nhật thông tin thành công!";
                            _logger.LogInformation($"User {user.Username} updated profile");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error updating profile");
                            TempData["ErrorMessage"] = "Đã xảy ra lỗi khi cập nhật thông tin";
                        }

                        return RedirectToAction(nameof(Profile));
                    }

                    // POST: /Account/ChangePassword
                    [HttpPost]
                    [ValidateAntiForgeryToken]
                    public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
                    {
                        var userId = HttpContext.Session.GetInt32("UserId");
                        if (userId == null)
                        {
                            return RedirectToAction(nameof(Login));
                        }

                        try
                        {
                            if (newPassword != confirmPassword)
                            {
                                TempData["ErrorMessage"] = "Mật khẩu xác nhận không khớp";
                                return RedirectToAction(nameof(Profile));
                            }

                            if (newPassword.Length < 6)
                            {
                                TempData["ErrorMessage"] = "Mật khẩu mới phải có ít nhất 6 ký tự";
                                return RedirectToAction(nameof(Profile));
                            }

                            var user = await _context.Users.FindAsync(userId.Value);
                            if (user == null)
                            {
                                return NotFound();
                            }

                            // Kiểm tra mật khẩu hiện tại
                            bool isCurrentPasswordValid = false;
                            if (user.PasswordHash != null && user.PasswordHash.StartsWith("$2"))
                            {
                                isCurrentPasswordValid = BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash);
                            }
                            else
                            {
                                isCurrentPasswordValid = user.PasswordHash == currentPassword;
                            }

                            if (!isCurrentPasswordValid)
                            {
                                TempData["ErrorMessage"] = "Mật khẩu hiện tại không đúng";
                                return RedirectToAction(nameof(Profile));
                            }

                            // Hash và cập nhật mật khẩu mới
                            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
                            user.UpdatedAt = DateTime.Now;

                            await _context.SaveChangesAsync();

                        TempData["SuccessMessage"] = "Đổi mật khẩu thành công!";
                            _logger.LogInformation($"User {user.Username} changed password");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error changing password");
                            TempData["ErrorMessage"] = "Đã xảy ra lỗi khi đổi mật khẩu";
                        }

                        return RedirectToAction(nameof(Profile));
                    }

        // GET: /Account/Notifications
        [HttpGet]
        public async Task<IActionResult> Notifications()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction(nameof(Login));
            }

            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId.Value)
                .OrderByDescending(n => n.CreatedAt)
                .Take(50)
                .ToListAsync();

            return View(notifications);
        }

        // GET: /Account/GetNotifications (API for dropdown)
        [HttpGet]
        public async Task<IActionResult> GetNotifications()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return Json(new { success = false, message = "Chưa đăng nhập" });
            }

            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId.Value)
                .OrderByDescending(n => n.CreatedAt)
                .Take(10)
                .Select(n => new
                {
                    id = n.NotificationId,
                    title = n.Title,
                    message = n.Message != null && n.Message.Length > 60 ? n.Message.Substring(0, 60) + "..." : n.Message,
                    type = n.Type ?? "system",
                    isRead = n.IsRead ?? false,
                    createdAt = n.CreatedAt,
                    timeAgo = GetTimeAgo(n.CreatedAt)
                })
                .ToListAsync();

            var unreadCount = await _context.Notifications
                .Where(n => n.UserId == userId.Value && (n.IsRead == null || n.IsRead == false))
                .CountAsync();

            return Json(new { success = true, notifications, unreadCount });
        }

        // POST: /Account/MarkNotificationRead
        [HttpPost]
        public async Task<IActionResult> MarkNotificationRead(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return Json(new { success = false, message = "Chưa đăng nhập" });
            }

            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.NotificationId == id && n.UserId == userId.Value);

            if (notification != null)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
            }

            return Json(new { success = true });
        }

        // POST: /Account/MarkAllNotificationsRead
        [HttpPost]
        public async Task<IActionResult> MarkAllNotificationsRead()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return Json(new { success = false, message = "Chưa đăng nhập" });
            }

            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId.Value && (n.IsRead == null || n.IsRead == false))
                .ToListAsync();

            foreach (var n in notifications)
            {
                n.IsRead = true;
            }

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = $"Đã đánh dấu {notifications.Count} thông báo là đã đọc" });
        }

        // Helper method to get time ago string
        private static string GetTimeAgo(DateTime? dateTime)
        {
            if (!dateTime.HasValue) return "";

            var timeSpan = DateTime.Now - dateTime.Value;

            if (timeSpan.TotalMinutes < 1) return "Vừa xong";
            if (timeSpan.TotalMinutes < 60) return $"{(int)timeSpan.TotalMinutes} phút trước";
            if (timeSpan.TotalHours < 24) return $"{(int)timeSpan.TotalHours} giờ trước";
            if (timeSpan.TotalDays < 7) return $"{(int)timeSpan.TotalDays} ngày trước";
            if (timeSpan.TotalDays < 30) return $"{(int)(timeSpan.TotalDays / 7)} tuần trước";
            if (timeSpan.TotalDays < 365) return $"{(int)(timeSpan.TotalDays / 30)} tháng trước";
            return $"{(int)(timeSpan.TotalDays / 365)} năm trước";
        }
    }
}
