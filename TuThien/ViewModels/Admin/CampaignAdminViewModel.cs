using System.ComponentModel.DataAnnotations;

namespace TuThien.ViewModels.Admin;

/// <summary>
/// ViewModel cho Admin tạo/sửa chiến dịch
/// </summary>
public class CampaignAdminViewModel
{
    public int CampaignId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên chiến dịch")]
    [StringLength(200, MinimumLength = 10, ErrorMessage = "Tên chiến dịch phải từ 10-200 ký tự")]
    [Display(Name = "Tên chiến dịch")]
    public string Title { get; set; } = null!;

    [Required(ErrorMessage = "Vui lòng nhập mô tả chiến dịch")]
    [StringLength(10000, MinimumLength = 50, ErrorMessage = "Mô tả phải từ 50-10000 ký tự")]
    [Display(Name = "Mô tả")]
    public string Description { get; set; } = null!;

    [Required(ErrorMessage = "Vui lòng nhập mục tiêu quyên góp")]
    [Range(100000, 100000000000, ErrorMessage = "Mục tiêu phải từ 100,000đ đến 100 tỷ đồng")]
    [Display(Name = "Mục tiêu (VNĐ)")]
    public decimal TargetAmount { get; set; }

    [Display(Name = "Danh mục")]
    public int? CategoryId { get; set; }

    [Display(Name = "Ngày bắt đầu")]
    public DateTime? StartDate { get; set; }

    [Display(Name = "Ngày kết thúc")]
    [DateGreaterThan("StartDate", ErrorMessage = "Ngày kết thúc phải sau ngày bắt đầu")]
    public DateTime? EndDate { get; set; }

    [Url(ErrorMessage = "URL ảnh không hợp lệ")]
    [Display(Name = "URL Ảnh đại diện")]
    public string? ThumbnailUrl { get; set; }


    [Required(ErrorMessage = "Vui lòng chọn cách xử lý tiền dư")]
    [RegularExpression("^(reserve_fund|next_case)$", ErrorMessage = "Giá trị xử lý tiền dư không hợp lệ")]
    [Display(Name = "Xử lý tiền dư")]
    public string ExcessFundOption { get; set; } = "next_case";

    [RegularExpression("^(draft|pending_approval|active|paused|completed|rejected|locked)$", ErrorMessage = "Trạng thái không hợp lệ")]
    [Display(Name = "Trạng thái")]
    public string? Status { get; set; }
}

/// <summary>
/// Custom validation attribute để so sánh ngày
/// </summary>
public class DateGreaterThanAttribute : ValidationAttribute
{
    private readonly string _comparisonProperty;

    public DateGreaterThanAttribute(string comparisonProperty)
    {
        _comparisonProperty = comparisonProperty;
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null) return ValidationResult.Success;

        var property = validationContext.ObjectType.GetProperty(_comparisonProperty);
        if (property == null)
            return new ValidationResult($"Không tìm thấy property {_comparisonProperty}");

        var comparisonValue = property.GetValue(validationContext.ObjectInstance) as DateTime?;
        if (comparisonValue == null) return ValidationResult.Success;

        if ((DateTime)value <= comparisonValue)
        {
            return new ValidationResult(ErrorMessage ?? $"Ngày phải lớn hơn {_comparisonProperty}");
        }

        return ValidationResult.Success;
    }
}
