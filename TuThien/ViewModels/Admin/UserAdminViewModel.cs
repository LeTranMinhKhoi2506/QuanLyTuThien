using System.ComponentModel.DataAnnotations;

namespace TuThien.ViewModels.Admin;

/// <summary>
/// ViewModel cho Admin tạo người dùng mới
/// </summary>
public class UserCreateViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Tên đăng nhập phải từ 3-50 ký tự")]
    [RegularExpression(@"^[a-zA-Z0-9_]+$", ErrorMessage = "Tên đăng nhập chỉ được chứa chữ cái, số và dấu gạch dưới")]
    [Display(Name = "Tên đăng nhập")]
    public string Username { get; set; } = null!;

    [Required(ErrorMessage = "Vui lòng nhập email")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    [StringLength(100, ErrorMessage = "Email tối đa 100 ký tự")]
    [Display(Name = "Email")]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải từ 6-100 ký tự")]
    [Display(Name = "Mật khẩu")]
    public string Password { get; set; } = null!;

    [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
    [StringLength(15, ErrorMessage = "Số điện thoại tối đa 15 ký tự")]
    [Display(Name = "Số điện thoại")]
    public string? PhoneNumber { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn vai trò")]
    [RegularExpression("^(admin|charity_org|user)$", ErrorMessage = "Vai trò không hợp lệ")]
    [Display(Name = "Vai trò")]
    public string Role { get; set; } = "user";
}

/// <summary>
/// ViewModel cho Admin cập nhật người dùng
/// </summary>
public class UserUpdateViewModel
{
    [Required]
    public int UserId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Tên đăng nhập phải từ 3-50 ký tự")]
    [RegularExpression(@"^[a-zA-Z0-9_]+$", ErrorMessage = "Tên đăng nhập chỉ được chứa chữ cái, số và dấu gạch dưới")]
    [Display(Name = "Tên đăng nhập")]
    public string Username { get; set; } = null!;

    [Required(ErrorMessage = "Vui lòng nhập email")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    [StringLength(100, ErrorMessage = "Email tối đa 100 ký tự")]
    [Display(Name = "Email")]
    public string Email { get; set; } = null!;

    [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải từ 6-100 ký tự")]
    [Display(Name = "Mật khẩu mới")]
    public string? Password { get; set; }

    [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
    [StringLength(15, ErrorMessage = "Số điện thoại tối đa 15 ký tự")]
    [Display(Name = "Số điện thoại")]
    public string? PhoneNumber { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn vai trò")]
    [RegularExpression("^(admin|charity_org|user)$", ErrorMessage = "Vai trò không hợp lệ")]
    [Display(Name = "Vai trò")]
    public string Role { get; set; } = "user";

    [Required(ErrorMessage = "Vui lòng chọn trạng thái")]
    [RegularExpression("^(active|locked|pending)$", ErrorMessage = "Trạng thái không hợp lệ")]
    [Display(Name = "Trạng thái")]
    public string Status { get; set; } = "active";
}
