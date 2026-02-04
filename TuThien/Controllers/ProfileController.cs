using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TuThien.Models;
using TuThien.ViewModels.Profile;
using BCrypt.Net;

namespace TuThien.Controllers
{
    public class ProfileController : Controller
    {
        private readonly TuThienContext _context;
        private readonly ILogger<ProfileController> _logger;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ProfileController(TuThienContext context, ILogger<ProfileController> logger, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _logger = logger;
            _webHostEnvironment = webHostEnvironment;
        }

        private int? GetCurrentUserId()
        {
            return HttpContext.Session.GetInt32("UserId");
        }

        // GET: /Profile
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var user = await _context.Users
                .Include(u => u.UserProfile)
                .FirstOrDefaultAsync(u => u.UserId == userId.Value);

            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var model = new ProfileViewModel
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                FullName = user.UserProfile?.FullName ?? "User",
                Address = user.UserProfile?.Address,
                Bio = user.UserProfile?.Bio,
                AvatarUrl = user.UserProfile?.AvatarUrl
            };

            return View(model);
        }

        // POST: /Profile/Update
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(ProfileViewModel model)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Remove password fields validation if not changing password
            if (string.IsNullOrEmpty(model.CurrentPassword) && string.IsNullOrEmpty(model.NewPassword))
            {
                ModelState.Remove("CurrentPassword");
                ModelState.Remove("NewPassword");
                ModelState.Remove("ConfirmPassword");
            }

            if (!ModelState.IsValid)
            {
                return View("Index", model);
            }

            var user = await _context.Users
                .Include(u => u.UserProfile)
                .FirstOrDefaultAsync(u => u.UserId == userId.Value);

            if (user == null)
            {
                return NotFound();
            }

            // check uniqueness
            if (await _context.Users.AnyAsync(u => u.Username == model.Username && u.UserId != userId.Value))
            {
                ModelState.AddModelError("Username", "Tên đăng nhập đã tồn tại");
                return View("Index", model);
            }
            if (await _context.Users.AnyAsync(u => u.Email == model.Email && u.UserId != userId.Value))
            {
                ModelState.AddModelError("Email", "Email đã tồn tại");
                return View("Index", model);
            }

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // Handle Avatar Upload
                    if (model.AvatarFile != null)
                    {
                        var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "avatars");
                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        var uniqueFileName = Guid.NewGuid().ToString() + "_" + model.AvatarFile.FileName;
                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await model.AvatarFile.CopyToAsync(fileStream);
                        }

                        // Remove old avatar if exists (optional cleanup)
                        // Update model path
                        user.UserProfile ??= new UserProfile { UserId = userId.Value };
                        user.UserProfile.AvatarUrl = "/uploads/avatars/" + uniqueFileName;
                        model.AvatarUrl = user.UserProfile.AvatarUrl; // Update view model to show new image
                    }

                    // Update User Info
                    user.Username = model.Username;
                    user.Email = model.Email;
                    user.PhoneNumber = model.PhoneNumber;
                    user.UpdatedAt = DateTime.Now;

                    // Update Profile Info
                    if (user.UserProfile == null)
                    {
                        user.UserProfile = new UserProfile { UserId = userId.Value };
                        _context.UserProfiles.Add(user.UserProfile);
                    }
                    user.UserProfile.FullName = model.FullName;
                    user.UserProfile.Address = model.Address;
                    user.UserProfile.Bio = model.Bio;

                    // Handle Password Change
                    if (!string.IsNullOrEmpty(model.NewPassword))
                    {
                        if (string.IsNullOrEmpty(model.CurrentPassword))
                        {
                            ModelState.AddModelError("CurrentPassword", "Vui lòng nhập mật khẩu hiện tại để thay đổi mật khẩu");
                            return View("Index", model);
                        }

                        bool isPasswordValid = false;
                        if (user.PasswordHash.StartsWith("$2"))
                        {
                             isPasswordValid = BCrypt.Net.BCrypt.Verify(model.CurrentPassword, user.PasswordHash);
                        }
                        else
                        {
                             isPasswordValid = user.PasswordHash == model.CurrentPassword;
                        }

                        if (!isPasswordValid)
                        {
                            ModelState.AddModelError("CurrentPassword", "Mật khẩu hiện tại không đúng");
                            return View("Index", model);
                        }

                        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    // Update Session if username changes
                    HttpContext.Session.SetString("Username", user.Username);

                    TempData["SuccessMessage"] = "Cập nhật hồ sơ thành công!";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error updating profile");
                    ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi khi cập nhật hồ sơ.");
                    return View("Index", model);
                }
            }
        }
    }
}
