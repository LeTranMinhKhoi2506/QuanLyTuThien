using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TuThien.Models;
using TuThien.ViewModels.Admin;

namespace TuThien.Controllers.Admin;

/// <summary>
/// Controller quản lý người dùng (Admin)
/// </summary>
public class AdminUsersController : AdminBaseController
{
    public AdminUsersController(TuThienContext context, ILogger<AdminUsersController> logger)
        : base(context, logger)
    {
    }

    /// <summary>
    /// Danh sách người dùng
    /// </summary>
    public async Task<IActionResult> Index(string? search, string? role, string? status, int page = 1)
    {
        if (!IsAdmin())
        {
            return RedirectToLogin();
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

        return View("~/Views/Admin/Users.cshtml", users);
    }

    /// <summary>
    /// Cập nhật trạng thái người dùng
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int userId, string status)
    {
        if (!IsAdmin())
        {
            return UnauthorizedJson();
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

    /// <summary>
    /// Cập nhật vai trò người dùng
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateRole(int userId, string role)
    {
        if (!IsAdmin())
        {
            return UnauthorizedJson();
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

    /// <summary>
    /// Kiểm tra username đã tồn tại
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> CheckUsernameExists(string username, int? excludeUserId = null)
    {
        if (!IsAdmin())
        {
            return Json(new { exists = false });
        }

        var query = _context.Users.Where(u => u.Username.ToLower() == username.ToLower().Trim());
        if (excludeUserId.HasValue)
        {
            query = query.Where(u => u.UserId != excludeUserId.Value);
        }

        var exists = await query.AnyAsync();
        return Json(new { exists });
    }

    /// <summary>
    /// Kiểm tra email đã tồn tại
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> CheckEmailExists(string email, int? excludeUserId = null)
    {
        if (!IsAdmin())
        {
            return Json(new { exists = false });
        }

        var query = _context.Users.Where(u => u.Email.ToLower() == email.ToLower().Trim());
        if (excludeUserId.HasValue)
        {
            query = query.Where(u => u.UserId != excludeUserId.Value);
        }

        var exists = await query.AnyAsync();
        return Json(new { exists });
    }

    /// <summary>
    /// Tạo người dùng mới
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromForm] UserCreateViewModel model)
    {
        if (!IsAdmin())
        {
            return UnauthorizedJson();
        }

        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            return Json(new { success = false, message = string.Join("; ", errors) });
        }

        if (await _context.Users.AnyAsync(u => u.Username == model.Username))
        {
            return Json(new { success = false, message = "Tên đăng nhập đã tồn tại" });
        }

        if (await _context.Users.AnyAsync(u => u.Email == model.Email.ToLower()))
        {
            return Json(new { success = false, message = "Email đã tồn tại" });
        }

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);

        var user = new User
        {
            Username = model.Username.Trim(),
            Email = model.Email.Trim().ToLower(),
            PasswordHash = passwordHash,
            PhoneNumber = model.PhoneNumber?.Trim(),
            Role = model.Role,
            Status = "active",
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        await LogAuditAsync("ADMIN_CREATE_USER", "Users", user.UserId, null, user.Username);

        return Json(new { success = true, message = "Tạo người dùng thành công", userId = user.UserId });
    }

    /// <summary>
    /// Lấy thông tin người dùng
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Get(int id)
    {
        if (!IsAdmin())
        {
            return UnauthorizedJson();
        }

        var user = await _context.Users
            .Where(u => u.UserId == id)
            .Select(u => new
            {
                u.UserId,
                u.Username,
                u.Email,
                u.PhoneNumber,
                u.Role,
                u.Status
            })
            .FirstOrDefaultAsync();

        if (user == null)
        {
            return Json(new { success = false, message = "Không tìm thấy người dùng" });
        }

        return Json(new { success = true, user });
    }

    /// <summary>
    /// Cập nhật thông tin người dùng
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update([FromForm] UserUpdateViewModel model)
    {
        if (!IsAdmin())
        {
            return UnauthorizedJson();
        }

        var user = await _context.Users.FindAsync(model.UserId);
        if (user == null)
        {
            return Json(new { success = false, message = "Không tìm thấy người dùng" });
        }

        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            return Json(new { success = false, message = string.Join("; ", errors) });
        }

        if (await _context.Users.AnyAsync(u => u.Username == model.Username && u.UserId != model.UserId))
        {
            return Json(new { success = false, message = "Tên đăng nhập đã tồn tại" });
        }

        if (await _context.Users.AnyAsync(u => u.Email == model.Email.ToLower() && u.UserId != model.UserId))
        {
            return Json(new { success = false, message = "Email đã tồn tại" });
        }

        var oldUsername = user.Username;
        user.Username = model.Username.Trim();
        user.Email = model.Email.Trim().ToLower();
        user.PhoneNumber = model.PhoneNumber?.Trim();
        user.Role = model.Role;
        user.Status = model.Status;

        if (!string.IsNullOrWhiteSpace(model.Password))
        {
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);
        }

        user.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();
        await LogAuditAsync("ADMIN_UPDATE_USER", "Users", model.UserId, oldUsername, user.Username);

        return Json(new { success = true, message = "Cập nhật người dùng thành công" });
    }

    /// <summary>
    /// Xóa người dùng
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int userId)
    {
        if (!IsAdmin())
        {
            return UnauthorizedJson();
        }

        var currentUserId = GetCurrentUserId();
        if (currentUserId == userId)
        {
            return Json(new { success = false, message = "Không thể xóa chính tài khoản của bạn" });
        }

        var user = await _context.Users
            .Include(u => u.Donations)
            .Include(u => u.Campaigns)
            .FirstOrDefaultAsync(u => u.UserId == userId);

        if (user == null)
        {
            return Json(new { success = false, message = "Không tìm thấy người dùng" });
        }

        if (user.Donations.Any())
        {
            return Json(new { success = false, message = "Không thể xóa người dùng đã có quyên góp. Vui lòng khóa tài khoản thay vì xóa." });
        }

        if (user.Campaigns.Any())
        {
            return Json(new { success = false, message = "Không thể xóa người dùng đã tạo chiến dịch. Vui lòng khóa tài khoản thay vì xóa." });
        }

        var username = user.Username;

        var profile = await _context.UserProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
        if (profile != null)
        {
            _context.UserProfiles.Remove(profile);
        }

        var notifications = await _context.Notifications.Where(n => n.UserId == userId).ToListAsync();
        _context.Notifications.RemoveRange(notifications);

        var comments = await _context.Comments.Where(c => c.UserId == userId).ToListAsync();
        _context.Comments.RemoveRange(comments);

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
        await LogAuditAsync("ADMIN_DELETE_USER", "Users", userId, username, null);

        return Json(new { success = true, message = "Xóa người dùng thành công" });
    }
}
