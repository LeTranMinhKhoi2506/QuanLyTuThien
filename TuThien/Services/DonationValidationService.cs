using Microsoft.Extensions.Options;
using TuThien.Configuration;
using TuThien.ViewModels;

namespace TuThien.Services;

/// <summary>
/// Interface cho dịch vụ validation quyên góp
/// </summary>
public interface IDonationValidationService
{
    /// <summary>
    /// Validate số tiền quyên góp
    /// </summary>
    ValidationResultModel ValidateAmount(decimal amount);

    /// <summary>
    /// Validate lời nhắn
    /// </summary>
    ValidationResultModel ValidateMessage(string? message);

    /// <summary>
    /// Validate phương thức thanh toán
    /// </summary>
    ValidationResultModel ValidatePaymentMethod(string paymentMethod);

    /// <summary>
    /// Validate toàn bộ donation
    /// </summary>
    ValidationResultModel ValidateDonation(DonationViewModel donation);

    /// <summary>
    /// Lấy thông tin cấu hình donation
    /// </summary>
    DonationSettings GetDonationSettings();

    /// <summary>
    /// Lấy thông tin ngân hàng
    /// </summary>
    BankSettings GetBankSettings();
}

/// <summary>
/// Kết quả validation
/// </summary>
public class ValidationResultModel
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();

    public static ValidationResultModel Success() => new() { IsValid = true };

    public static ValidationResultModel Failure(string error) => new() 
    { 
        IsValid = false, 
        Errors = new List<string> { error } 
    };

    public static ValidationResultModel Failure(List<string> errors) => new() 
    { 
        IsValid = false, 
        Errors = errors 
    };

    public void AddError(string error)
    {
        IsValid = false;
        Errors.Add(error);
    }
}

/// <summary>
/// Dịch vụ validation quyên góp - sử dụng configuration từ appsettings.json
/// </summary>
public class DonationValidationService : IDonationValidationService
{
    private readonly DonationSettings _donationSettings;
    private readonly BankSettings _bankSettings;
    private static readonly string[] ValidPaymentMethods = { "vnpay", "momo", "bank_transfer" };

    public DonationValidationService(
        IOptions<DonationSettings> donationSettings,
        IOptions<BankSettings> bankSettings)
    {
        _donationSettings = donationSettings.Value;
        _bankSettings = bankSettings.Value;
    }

    public ValidationResultModel ValidateAmount(decimal amount)
    {
        if (amount < _donationSettings.MinAmount)
        {
            return ValidationResultModel.Failure(
                $"Số tiền quyên góp tối thiểu là {_donationSettings.MinAmount:N0} VNĐ");
        }

        if (amount > _donationSettings.MaxAmount)
        {
            return ValidationResultModel.Failure(
                $"Số tiền quyên góp tối đa là {_donationSettings.MaxAmount:N0} VNĐ");
        }

        if (amount <= 0)
        {
            return ValidationResultModel.Failure("Số tiền quyên góp phải lớn hơn 0");
        }

        // Kiểm tra số tiền có phải là bội số của 1000 không (optional)
        if (amount % 1000 != 0)
        {
            return ValidationResultModel.Failure("Số tiền quyên góp phải là bội số của 1,000 VNĐ");
        }

        return ValidationResultModel.Success();
    }

    public ValidationResultModel ValidateMessage(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return ValidationResultModel.Success(); // Message là optional
        }

        if (message.Length > _donationSettings.MaxMessageLength)
        {
            return ValidationResultModel.Failure(
                $"Lời nhắn không được vượt quá {_donationSettings.MaxMessageLength} ký tự");
        }

        // Kiểm tra nội dung không phù hợp (có thể mở rộng)
        var inappropriateWords = new[] { "spam", "quảng cáo" }; // Ví dụ
        foreach (var word in inappropriateWords)
        {
            if (message.Contains(word, StringComparison.OrdinalIgnoreCase))
            {
                return ValidationResultModel.Failure("Lời nhắn chứa nội dung không phù hợp");
            }
        }

        return ValidationResultModel.Success();
    }

    public ValidationResultModel ValidatePaymentMethod(string paymentMethod)
    {
        if (string.IsNullOrWhiteSpace(paymentMethod))
        {
            return ValidationResultModel.Failure("Vui lòng chọn phương thức thanh toán");
        }

        if (!ValidPaymentMethods.Contains(paymentMethod.ToLower()))
        {
            return ValidationResultModel.Failure(
                $"Phương thức thanh toán không hợp lệ. Các phương thức hỗ trợ: {string.Join(", ", ValidPaymentMethods)}");
        }

        return ValidationResultModel.Success();
    }

    public ValidationResultModel ValidateDonation(DonationViewModel donation)
    {
        var result = new ValidationResultModel { IsValid = true };

        // Validate Campaign ID
        if (donation.CampaignId <= 0)
        {
            result.AddError("Vui lòng chọn chiến dịch hợp lệ");
        }

        // Validate Amount
        var amountResult = ValidateAmount(donation.Amount);
        if (!amountResult.IsValid)
        {
            result.Errors.AddRange(amountResult.Errors);
            result.IsValid = false;
        }

        // Validate Message
        var messageResult = ValidateMessage(donation.Message);
        if (!messageResult.IsValid)
        {
            result.Errors.AddRange(messageResult.Errors);
            result.IsValid = false;
        }

        // Validate Payment Method
        var paymentResult = ValidatePaymentMethod(donation.PaymentMethod);
        if (!paymentResult.IsValid)
        {
            result.Errors.AddRange(paymentResult.Errors);
            result.IsValid = false;
        }

        return result;
    }

    public DonationSettings GetDonationSettings() => _donationSettings;

    public BankSettings GetBankSettings() => _bankSettings;
}
