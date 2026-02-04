using System.ComponentModel.DataAnnotations;

namespace TuThien.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Vui lòng nh?p tên ??ng nh?p ho?c email")]
        [Display(Name = "Tên ??ng nh?p ho?c Email")]
        public string UsernameOrEmail { get; set; } = null!;

        [Required(ErrorMessage = "Vui lòng nh?p m?t kh?u")]
        [DataType(DataType.Password)]
        [Display(Name = "M?t kh?u")]
        public string Password { get; set; } = null!;

        [Display(Name = "Ghi nh? ??ng nh?p")]
        public bool RememberMe { get; set; }
    }

    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Vui lòng nh?p tên ??ng nh?p")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Tên ??ng nh?p ph?i t? 3-50 ký t?")]
        [Display(Name = "Tên ??ng nh?p")]
        public string Username { get; set; } = null!;

        [Required(ErrorMessage = "Vui lòng nh?p email")]
        [EmailAddress(ErrorMessage = "Email không h?p l?")]
        [Display(Name = "Email")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Vui lòng nh?p m?t kh?u")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "M?t kh?u ph?i t? 6-100 ký t?")]
        [DataType(DataType.Password)]
        [Display(Name = "M?t kh?u")]
        public string Password { get; set; } = null!;

        [Required(ErrorMessage = "Vui lòng xác nh?n m?t kh?u")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "M?t kh?u xác nh?n không kh?p")]
        [Display(Name = "Xác nh?n m?t kh?u")]
        public string ConfirmPassword { get; set; } = null!;

        [Required(ErrorMessage = "Vui lòng nh?p h? tên")]
        [Display(Name = "H? và tên")]
        public string FullName { get; set; } = null!;

        [Phone(ErrorMessage = "S? ?i?n tho?i không h?p l?")]
        [Display(Name = "S? ?i?n tho?i")]
        public string? PhoneNumber { get; set; }
    }

    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Vui lòng nh?p email")]
        [EmailAddress(ErrorMessage = "Email không h?p l?")]
        [Display(Name = "Email")]
        public string Email { get; set; } = null!;
    }
}
