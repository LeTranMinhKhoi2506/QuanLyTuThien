using System.Net;
using System.Net.Mail;
using TuThien.Models;
using Microsoft.EntityFrameworkCore;

namespace TuThien.Services;

/// <summary>
/// Service gửi email tự động
/// </summary>
public interface IEmailService
{
    Task SendEmailAsync(string toEmail, string subject, string body);
    Task SendThankYouEmailAsync(string toEmail, string donorName, string campaignTitle, decimal amount);
    Task SendCampaignApprovedEmailAsync(string toEmail, string creatorName, string campaignTitle);
    Task SendDisbursementApprovedEmailAsync(string toEmail, string requesterName, decimal amount);
}

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Lấy cấu hình email từ appsettings.json
    /// </summary>
    private (string SmtpHost, int SmtpPort, string SmtpUser, string SmtpPass, string FromEmail, string FromName) GetEmailConfig()
    {
        var smtpHost = _configuration["EmailSettings:SmtpHost"] ?? "smtp.gmail.com";
        var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
        var smtpUser = _configuration["EmailSettings:SmtpUser"] ?? "";
        var smtpPass = _configuration["EmailSettings:SmtpPassword"] ?? "";
        var fromEmail = _configuration["EmailSettings:FromEmail"] ?? "noreply@tuthien.vn";
        var fromName = _configuration["EmailSettings:FromName"] ?? "Hệ thống Từ Thiện";

        return (smtpHost, smtpPort, smtpUser, smtpPass, fromEmail, fromName);
    }

    public async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        try
        {
            var config = GetEmailConfig();
            
            // Kiểm tra nếu chưa cấu hình SMTP
            if (string.IsNullOrEmpty(config.SmtpUser) || string.IsNullOrEmpty(config.SmtpPass))
            {
                _logger.LogWarning("Email configuration is not set. Email will not be sent.");
                return;
            }

            using var client = new SmtpClient(config.SmtpHost, config.SmtpPort)
            {
                Credentials = new NetworkCredential(config.SmtpUser, config.SmtpPass),
                EnableSsl = true
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(config.FromEmail, config.FromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            mailMessage.To.Add(toEmail);

            await client.SendMailAsync(mailMessage);
            _logger.LogInformation($"Email sent successfully to {toEmail}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to send email to {toEmail}");
        }
    }

    /// <summary>
    /// Gửi email cảm ơn sau khi quyên góp thành công
    /// </summary>
    public async Task SendThankYouEmailAsync(string toEmail, string donorName, string campaignTitle, decimal amount)
    {
        var amountFormatted = amount.ToString("N0");
        var currentTime = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
        
        var subject = $"Cảm ơn bạn đã ủng hộ chiến dịch \"{campaignTitle}\"";
        var body = $@"
            <html>
            <body style='font-family: Arial, sans-serif; line-height: 1.6;'>
                <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                    <h2 style='color: #2c5e2e;'>Xin chào {donorName},</h2>
                    
                    <p>Cảm ơn bạn đã quyên góp <strong>{amountFormatted} VNĐ</strong> cho chiến dịch <strong>""{campaignTitle}""</strong>.</p>
                    
                    <p>Sự đóng góp của bạn sẽ giúp đỡ những hoàn cảnh khó khăn và lan tỏa yêu thương đến cộng đồng.</p>
                    
                    <div style='background: #f5f5f5; padding: 15px; border-radius: 8px; margin: 20px 0;'>
                        <p style='margin: 0;'><strong>Chi tiết quyên góp:</strong></p>
                        <ul style='margin: 10px 0;'>
                            <li>Số tiền: <strong>{amountFormatted} VNĐ</strong></li>
                            <li>Chiến dịch: {campaignTitle}</li>
                            <li>Thời gian: {currentTime}</li>
                        </ul>
                    </div>
                    
                    <p>Trân trọng,<br/>Đội ngũ Từ Thiện</p>
                </div>
            </body>
            </html>";

        await SendEmailAsync(toEmail, subject, body);
    }

    /// <summary>
    /// Gửi email thông báo chiến dịch được duyệt
    /// </summary>
    public async Task SendCampaignApprovedEmailAsync(string toEmail, string creatorName, string campaignTitle)
    {
        var subject = $"Chiến dịch \"{campaignTitle}\" đã được phê duyệt";
        var body = $@"
            <html>
            <body style='font-family: Arial, sans-serif; line-height: 1.6;'>
                <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                    <h2 style='color: #2c5e2e;'>Xin chào {creatorName},</h2>
                    
                    <p>Chúng tôi vui mừng thông báo chiến dịch <strong>""{campaignTitle}""</strong> của bạn đã được phê duyệt và đang hoạt động.</p>
                    
                    <p>Bạn có thể chia sẻ link chiến dịch để kêu gọi cộng đồng ủng hộ.</p>
                    
                    <p>Chúc bạn thành công!</p>
                    
                    <p>Trân trọng,<br/>Đội ngũ Từ Thiện</p>
                </div>
            </body>
            </html>";

        await SendEmailAsync(toEmail, subject, body);
    }

    /// <summary>
    /// Gửi email thông báo giải ngân được duyệt
    /// </summary>
    public async Task SendDisbursementApprovedEmailAsync(string toEmail, string requesterName, decimal amount)
    {
        var amountFormatted = amount.ToString("N0");
        
        var subject = "Yêu cầu giải ngân đã được phê duyệt";
        var body = $@"
            <html>
            <body style='font-family: Arial, sans-serif; line-height: 1.6;'>
                <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                    <h2 style='color: #2c5e2e;'>Xin chào {requesterName},</h2>
                    
                    <p>Yêu cầu giải ngân <strong>{amountFormatted} VNĐ</strong> của bạn đã được phê duyệt.</p>
                    
                    <p>Số tiền sẽ được chuyển vào tài khoản của bạn trong vòng 1-3 ngày làm việc.</p>
                    
                    <p>Trân trọng,<br/>Đội ngũ Từ Thiện</p>
                </div>
            </body>
            </html>";

        await SendEmailAsync(toEmail, subject, body);
    }
}
