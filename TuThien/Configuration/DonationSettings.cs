namespace TuThien.Configuration;

/// <summary>
/// Cấu hình cho quyên góp - lấy từ appsettings.json
/// </summary>
public class DonationSettings
{
    public const string SectionName = "DonationSettings";

    /// <summary>
    /// Số tiền quyên góp tối thiểu (VNĐ)
    /// </summary>
    public decimal MinAmount { get; set; } = 10000;

    /// <summary>
    /// Số tiền quyên góp tối đa (VNĐ)
    /// </summary>
    public decimal MaxAmount { get; set; } = 1000000000; // 1 tỷ

    /// <summary>
    /// Độ dài tối đa của lời nhắn
    /// </summary>
    public int MaxMessageLength { get; set; } = 500;
}

/// <summary>
/// Cấu hình ngân hàng nhận quyên góp
/// </summary>
public class BankSettings
{
    public const string SectionName = "BankSettings";

    public string BankName { get; set; } = "Vietcombank";
    public string AccountNumber { get; set; } = "1234567890";
    public string AccountName { get; set; } = "QUY TU THIEN";
    public string Branch { get; set; } = "Chi nhánh Hà Nội";
}

/// <summary>
/// Cấu hình VNPAY
/// </summary>
public class VNPaySettings
{
    public const string SectionName = "VNPaySettings";

    public string TmnCode { get; set; } = string.Empty;
    public string HashSecret { get; set; } = string.Empty;
    public string PaymentUrl { get; set; } = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
    public string ReturnUrl { get; set; } = "/Donation/VNPayCallback";
}

/// <summary>
/// Cấu hình MoMo - Sandbox environment
/// Đăng ký tại: https://developers.momo.vn/
/// </summary>
public class MoMoSettings
{
    public const string SectionName = "MoMoSettings";

    /// <summary>
    /// Partner Code được cấp bởi MoMo
    /// </summary>
    public string PartnerCode { get; set; } = string.Empty;
    
    /// <summary>
    /// Access Key được cấp bởi MoMo
    /// </summary>
    public string AccessKey { get; set; } = string.Empty;
    
    /// <summary>
    /// Secret Key để tạo chữ ký
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;
    
    /// <summary>
    /// API Endpoint - Sandbox: https://test-payment.momo.vn
    /// </summary>
    public string Endpoint { get; set; } = "https://test-payment.momo.vn";
    
    /// <summary>
    /// URL MoMo sẽ redirect về sau khi thanh toán
    /// </summary>
    public string ReturnUrl { get; set; } = "/Donation/MoMoReturn";
    
    /// <summary>
    /// URL MoMo sẽ gọi để thông báo kết quả (IPN)
    /// </summary>
    public string NotifyUrl { get; set; } = "/Donation/MoMoNotify";
}
