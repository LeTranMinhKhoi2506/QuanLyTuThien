using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using TuThien.Configuration;

namespace TuThien.Services;

/// <summary>
/// Service xử lý thanh toán MoMo - Sandbox Environment
/// Tài liệu: https://developers.momo.vn/v3/docs/payment/api/wallet/onetime
/// </summary>
public interface IMoMoService
{
    /// <summary>
    /// Tạo request thanh toán MoMo
    /// </summary>
    Task<MoMoPaymentResponse> CreatePaymentAsync(MoMoPaymentRequest request);
    
    /// <summary>
    /// Xác thực chữ ký từ MoMo callback
    /// </summary>
    bool ValidateSignature(string rawData, string signature);
    
    /// <summary>
    /// Tạo chữ ký HMAC SHA256
    /// </summary>
    string CreateSignature(string rawData);
}

public class MoMoService : IMoMoService
{
    private readonly MoMoSettings _settings;
    private readonly ILogger<MoMoService> _logger;
    private readonly HttpClient _httpClient;

    public MoMoService(
        IOptions<MoMoSettings> settings, 
        ILogger<MoMoService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _settings = settings.Value;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient("MoMo");
    }

    public async Task<MoMoPaymentResponse> CreatePaymentAsync(MoMoPaymentRequest request)
    {
        try
        {
            // Tạo orderId unique
            var orderId = request.OrderId;
            var requestId = Guid.NewGuid().ToString();
            var orderInfo = request.OrderInfo;
            var amount = request.Amount;
            var extraData = request.ExtraData ?? "";

            // Tạo raw signature theo format MoMo yêu cầu
            var rawSignature = $"accessKey={_settings.AccessKey}" +
                              $"&amount={amount}" +
                              $"&extraData={extraData}" +
                              $"&ipnUrl={request.NotifyUrl}" +
                              $"&orderId={orderId}" +
                              $"&orderInfo={orderInfo}" +
                              $"&partnerCode={_settings.PartnerCode}" +
                              $"&redirectUrl={request.ReturnUrl}" +
                              $"&requestId={requestId}" +
                              $"&requestType=captureWallet";

            var signature = CreateSignature(rawSignature);

            _logger.LogInformation("MoMo Raw Signature: {RawSignature}", rawSignature);
            _logger.LogInformation("MoMo Signature: {Signature}", signature);

            // Tạo request body
            var requestBody = new
            {
                partnerCode = _settings.PartnerCode,
                accessKey = _settings.AccessKey,
                requestId = requestId,
                amount = amount,
                orderId = orderId,
                orderInfo = orderInfo,
                redirectUrl = request.ReturnUrl,
                ipnUrl = request.NotifyUrl,
                extraData = extraData,
                requestType = "captureWallet",
                signature = signature,
                lang = "vi"
            };

            var jsonContent = JsonSerializer.Serialize(requestBody);
            _logger.LogInformation("MoMo Request: {Request}", jsonContent);

            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            
            // Gọi API MoMo
            var apiUrl = $"{_settings.Endpoint}/v2/gateway/api/create";
            var response = await _httpClient.PostAsync(apiUrl, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("MoMo Response: {Response}", responseContent);

            var momoResponse = JsonSerializer.Deserialize<MoMoApiResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (momoResponse == null)
            {
                return new MoMoPaymentResponse
                {
                    Success = false,
                    Message = "Không thể parse response từ MoMo"
                };
            }

            // ResultCode = 0 là thành công
            if (momoResponse.ResultCode == 0)
            {
                return new MoMoPaymentResponse
                {
                    Success = true,
                    PayUrl = momoResponse.PayUrl,
                    QrCodeUrl = momoResponse.QrCodeUrl,
                    DeepLink = momoResponse.Deeplink,
                    OrderId = orderId,
                    RequestId = requestId
                };
            }
            else
            {
                return new MoMoPaymentResponse
                {
                    Success = false,
                    Message = momoResponse.Message ?? $"MoMo Error: {momoResponse.ResultCode}",
                    ResultCode = momoResponse.ResultCode
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating MoMo payment");
            return new MoMoPaymentResponse
            {
                Success = false,
                Message = $"Lỗi kết nối MoMo: {ex.Message}"
            };
        }
    }

    public bool ValidateSignature(string rawData, string signature)
    {
        var computedSignature = CreateSignature(rawData);
        return computedSignature.Equals(signature, StringComparison.OrdinalIgnoreCase);
    }

    public string CreateSignature(string rawData)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_settings.SecretKey));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(rawData));
        return BitConverter.ToString(hash).Replace("-", "").ToLower();
    }
}

#region MoMo Models

/// <summary>
/// Request tạo thanh toán MoMo
/// </summary>
public class MoMoPaymentRequest
{
    public string OrderId { get; set; } = string.Empty;
    public string OrderInfo { get; set; } = string.Empty;
    public long Amount { get; set; }
    public string ReturnUrl { get; set; } = string.Empty;
    public string NotifyUrl { get; set; } = string.Empty;
    public string? ExtraData { get; set; }
}

/// <summary>
/// Response từ MoMo API
/// </summary>
public class MoMoApiResponse
{
    public string? PartnerCode { get; set; }
    public string? RequestId { get; set; }
    public string? OrderId { get; set; }
    public long Amount { get; set; }
    public long ResponseTime { get; set; }
    public string? Message { get; set; }
    public int ResultCode { get; set; }
    public string? PayUrl { get; set; }
    public string? QrCodeUrl { get; set; }
    public string? Deeplink { get; set; }
}

/// <summary>
/// Response trả về cho client
/// </summary>
public class MoMoPaymentResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? PayUrl { get; set; }
    public string? QrCodeUrl { get; set; }
    public string? DeepLink { get; set; }
    public string? OrderId { get; set; }
    public string? RequestId { get; set; }
    public int ResultCode { get; set; }
}

/// <summary>
/// Callback từ MoMo (IPN)
/// </summary>
public class MoMoCallbackRequest
{
    public string? PartnerCode { get; set; }
    public string? OrderId { get; set; }
    public string? RequestId { get; set; }
    public long Amount { get; set; }
    public string? OrderInfo { get; set; }
    public string? OrderType { get; set; }
    public long TransId { get; set; }
    public int ResultCode { get; set; }
    public string? Message { get; set; }
    public string? PayType { get; set; }
    public long ResponseTime { get; set; }
    public string? ExtraData { get; set; }
    public string? Signature { get; set; }
}

#endregion
