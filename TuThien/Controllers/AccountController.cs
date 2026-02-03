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
                // Tìm user theo username ho?c email
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => 
                        u.Username == model.UsernameOrEmail || 
                        u.Email == model.UsernameOrEmail);

                if (user == null)
                {
                    ModelState.AddModelError(string.Empty, "Tên ??ng nh?p ho?c m?t kh?u không ?úng");
                    return View(model);
                }

                // Ki?m tra tr?ng thái tài kho?n
                if (user.Status == "locked")
                {
                    ModelState.AddModelError(string.Empty, "Tài kho?n ?ã b? khóa. Vui lòng liên h? qu?n tr? viên.");
                    return View(model);
                }

                // Verify password v?i BCrypt
                if (!BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
                {
                    ModelState.AddModelError(string.Empty, "Tên ??ng nh?p ho?c m?t kh?u không ?úng");
                    return View(model);
                }

                // ??ng nh?p thành công - L?u thông tin vào session
                HttpContext.Session.SetInt32("UserId", user.UserId);
                HttpContext.Session.SetString("Username", user.Username);
                HttpContext.Session.SetString("Email", user.Email);
                HttpContext.Session.SetString("Role", user.Role ?? "user");

                // Ghi log
                _logger.LogInformation($"User {user.Username} logged in successfully");

                // Redirect v? trang tr??c ?ó ho?c trang ch?
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                return RedirectToAction("Index", "TrangChu");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                ModelState.AddModelError(string.Empty, "?ã x?y ra l?i. Vui lòng th? l?i sau.");
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
                // Ki?m tra username ?ã t?n t?i
                if (await _context.Users.AnyAsync(u => u.Username == model.Username))
                {
                    ModelState.AddModelError("Username", "Tên ??ng nh?p ?ã t?n t?i");
                    return View(model);
                }

                // Ki?m tra email ?ã t?n t?i
                if (await _context.Users.AnyAsync(u => u.Email == model.Email))
                {
                    ModelState.AddModelError("Email", "Email ?ã ???c s? d?ng");
                    return View(model);
                }

                // Hash password v?i BCrypt
                var passwordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);

                // T?o user m?i
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

                // T?o profile
                var profile = new UserProfile
                {
                    UserId = user.UserId,
                    FullName = model.FullName
                };

                _context.UserProfiles.Add(profile);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"New user registered: {user.Username}");

                TempData["SuccessMessage"] = "??ng ký thành công! Vui lòng ??ng nh?p.";
                return RedirectToAction(nameof(Login));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration");
                ModelState.AddModelError(string.Empty, "?ã x?y ra l?i. Vui lòng th? l?i sau.");
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
    }
}
