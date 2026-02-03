using System.ComponentModel.DataAnnotations;
using TuThien.Configuration;

namespace TuThien.ViewModels;

/// <summary>
/// ViewModel cho form quyên góp với validation hoàn chỉnh
/// </summary>
public class DonationViewModel
{
    [Required(ErrorMessage = "Vui lòng chọn chiến dịch")]
    public int CampaignId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập số tiền quyên góp")]
    [Range(10000, 1000000000, ErrorMessage = "Số tiền quyên góp phải từ {1:N0} đến {2:N0} VNĐ")]
    [Display(Name = "Số tiền quyên góp")]
    public decimal Amount { get; set; }

    [StringLength(500, ErrorMessage = "Lời nhắn không được vượt quá {1} ký tự")]
    [Display(Name = "Lời nhắn")]
    public string? Message { get; set; }

    [Display(Name = "Quyên góp ẩn danh")]
    public bool IsAnonymous { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn phương thức thanh toán")]
    [PaymentMethodValidation(ErrorMessage = "Phương thức thanh toán không hợp lệ")]
    [Display(Name = "Phương thức thanh toán")]
    public string PaymentMethod { get; set; } = "bank_transfer";
}

/// <summary>
/// Custom validation attribute cho phương thức thanh toán
/// </summary>
public class PaymentMethodValidationAttribute : ValidationAttribute
{
    private static readonly string[] ValidPaymentMethods = { "vnpay", "momo", "bank_transfer" };

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is string paymentMethod)
        {
            if (ValidPaymentMethods.Contains(paymentMethod.ToLower()))
            {
                return ValidationResult.Success;
            }
        }

        return new ValidationResult(ErrorMessage ?? "Phương thức thanh toán không hợp lệ");
    }
}

/// <summary>
/// Custom validation attribute cho số tiền dựa trên DonationSettings
/// </summary>
public class DonationAmountValidationAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is decimal amount)
        {
            // Lấy DonationSettings từ DI container
            var settings = validationContext.GetService(typeof(Microsoft.Extensions.Options.IOptions<DonationSettings>)) 
                as Microsoft.Extensions.Options.IOptions<DonationSettings>;

            var minAmount = settings?.Value.MinAmount ?? 10000;
            var maxAmount = settings?.Value.MaxAmount ?? 1000000000;

            if (amount < minAmount)
            {
                return new ValidationResult($"Số tiền quyên góp tối thiểu là {minAmount:N0} VNĐ");
            }

            if (amount > maxAmount)
            {
                return new ValidationResult($"Số tiền quyên góp tối đa là {maxAmount:N0} VNĐ");
            }

            return ValidationResult.Success;
        }

        return new ValidationResult("Số tiền không hợp lệ");
    }
}

/// <summary>
/// ViewModel cho phản hồi sau khi xử lý quyên góp
/// </summary>
public class DonationResponseViewModel
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? TransactionCode { get; set; }
    public string? PaymentMethod { get; set; }
    public string? RedirectUrl { get; set; }
    public BankTransferInfoViewModel? BankInfo { get; set; }
}

/// <summary>
/// ViewModel cho thông tin chuyển khoản ngân hàng
/// </summary>
public class BankTransferInfoViewModel
{
    public string BankName { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string Branch { get; set; } = string.Empty;
    public string TransferContent { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}
