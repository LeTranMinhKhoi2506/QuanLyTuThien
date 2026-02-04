using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TuThien.Models;
using TuThien.Services;
using TuThien.Configuration;
using TuThien.ViewModels;
using System.Text.Json;

namespace TuThien.Controllers;

/// <summary>
/// Controller xử lý quyên góp và thanh toán
/// </summary>
public class DonationController : Controller
{
    private readonly TuThienContext _context;
    private readonly ILogger<DonationController> _logger;
    private readonly IEmailService _emailService;
    private readonly IDonationValidationService _validationService;
    private readonly DonationSettings _donationSettings;
    private readonly BankSettings _bankSettings;
    private readonly VNPaySettings _vnpaySettings;
    private readonly MoMoSettings _momoSettings;

    public DonationController(
        TuThienContext context, 
        ILogger<DonationController> logger, 
        IEmailService emailService,
        IDonationValidationService validationService,
        IOptions<DonationSettings> donationSettings,
        IOptions<BankSettings> bankSettings,
        IOptions<VNPaySettings> vnpaySettings,
        IOptions<MoMoSettings> momoSettings)
    {
        _context = context;
        _logger = logger;
        _emailService = emailService;
        _validationService = validationService;
        _donationSettings = donationSettings.Value;
        _bankSettings = bankSettings.Value;
        _vnpaySettings = vnpaySettings.Value;
        _momoSettings = momoSettings.Value;
    }

    private int? GetCurrentUserId()
    {
        return HttpContext.Session.GetInt32("UserId");
    }

    /// <summary>
    /// Trang hướng dẫn đóng góp
    /// </summary>
    [HttpGet]
    public IActionResult HowToDonate()
    {
        return View();
    }

    /// <summary>
    /// Trang lịch sử đóng góp của người dùng
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> DonationUpdates(string? status, int page = 1)
    {
        var userId = GetCurrentUserId();
        
        if (!userId.HasValue)
        {
            TempData["ErrorMessage"] = "Vui lòng đăng nhập để xem lịch sử đóng góp";
            return RedirectToAction("Login", "Account", new { returnUrl = "/Donation/DonationUpdates" });
        }

        var query = _context.Donations
            .Include(d => d.Campaign)
                .ThenInclude(c => c.Category)
            .Where(d => d.UserId == userId.Value)
            .AsQueryable();

        // Filter by status
        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(d => d.PaymentStatus == status);
        }

        // Pagination
        int pageSize = 10;
        var totalItems = await query.CountAsync();
        var donations = await query
            .OrderByDescending(d => d.DonatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Calculate statistics
        var totalDonated = await _context.Donations
            .Where(d => d.UserId == userId.Value && d.PaymentStatus == "completed")
            .SumAsync(d => (decimal?)d.Amount) ?? 0;

        var totalCampaigns = await _context.Donations
            .Where(d => d.UserId == userId.Value && d.PaymentStatus == "completed")
            .Select(d => d.CampaignId)
            .Distinct()
            .CountAsync();

        ViewBag.TotalDonated = totalDonated;
        ViewBag.TotalCampaigns = totalCampaigns;
        ViewBag.TotalDonations = await query.CountAsync();
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        ViewBag.Status = status;

        return View(donations);
    }

    /// <summary>
    /// Trang quyên góp cho chiến dịch
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Donate(int campaignId)
    {
        var campaign = await _context.Campaigns
            .Include(c => c.Creator)
            .FirstOrDefaultAsync(c => c.CampaignId == campaignId);

        if (campaign == null)
        {
            return NotFound();
        }

        if (campaign.Status != "active")
        {
            TempData["ErrorMessage"] = "Chiến dịch này không còn nhận quyên góp.";
            return RedirectToAction("Details", "TrangChu", new { id = campaignId });
        }

        ViewBag.Campaign = campaign;
        ViewBag.DonationSettings = _donationSettings;
        return View();
    }

    /// <summary>
    /// Xử lý quyên góp - Tạo giao dịch và chuyển đến cổng thanh toán
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProcessDonation(DonationViewModel model)
    {
        try
        {
            // Server-side validation sử dụng validation service
            var validationResult = _validationService.ValidateDonation(model);
            if (!validationResult.IsValid)
            {
                return Json(new DonationResponseViewModel 
                { 
                    Success = false, 
                    Message = string.Join("; ", validationResult.Errors)
                });
            }

            var campaign = await _context.Campaigns.FindAsync(model.CampaignId);
            if (campaign == null || campaign.Status != "active")
            {
                return Json(new DonationResponseViewModel 
                { 
                    Success = false, 
                    Message = "Chiến dịch không hợp lệ hoặc đã kết thúc" 
                });
            }

            var userId = GetCurrentUserId();

            // Tạo mã giao dịch unique
            var transactionCode = $"TT{DateTime.Now:yyyyMMddHHmmss}{new Random().Next(1000, 9999)}";

            // Tạo donation với trạng thái pending
            var donation = new Donation
            {
                CampaignId = model.CampaignId,
                UserId = userId,
                Amount = model.Amount,
                Message = model.Message,
                IsAnonymous = model.IsAnonymous,
                PaymentMethod = model.PaymentMethod,
                TransactionCode = transactionCode,
                PaymentStatus = "pending",
                DonatedAt = DateTime.Now
            };

            _context.Donations.Add(donation);
            await _context.SaveChangesAsync();

            // Chuyển đến cổng thanh toán tương ứng
            switch (model.PaymentMethod.ToLower())
            {
                case "vnpay":
                    var vnpayUrl = CreateVNPayPaymentUrl(donation);
                    return Json(new DonationResponseViewModel 
                    { 
                        Success = true, 
                        RedirectUrl = vnpayUrl 
                    });

                case "momo":
                    var momoUrl = CreateMoMoPaymentUrl(donation);
                    return Json(new DonationResponseViewModel 
                    { 
                        Success = true, 
                        RedirectUrl = momoUrl 
                    });

                case "bank_transfer":
                    return Json(new DonationResponseViewModel 
                    { 
                        Success = true, 
                        PaymentMethod = "bank_transfer",
                        TransactionCode = transactionCode,
                        BankInfo = GetBankTransferInfo(transactionCode, model.Amount)
                    });

                default:
                    return Json(new DonationResponseViewModel 
                    { 
                        Success = false, 
                        Message = "Phương thức thanh toán không hỗ trợ" 
                    });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing donation");
            return Json(new DonationResponseViewModel 
            { 
                Success = false, 
                Message = "Đã xảy ra lỗi, vui lòng thử lại sau" 
            });
        }
    }

    /// <summary>
    /// Tạo URL thanh toán VNPAY
    /// </summary>
    private string CreateVNPayPaymentUrl(Donation donation)
    {
        var vnpReturnUrl = $"{Request.Scheme}://{Request.Host}{_vnpaySettings.ReturnUrl}";

        // TODO: Implement VNPAY payment URL creation with signature
        // Đây là implementation mẫu - cần tích hợp thực tế với VNPAY SDK

        _logger.LogInformation($"Creating VNPAY payment for transaction {donation.TransactionCode}");
        
        // Mock URL cho development
        return $"/Donation/PaymentSimulation?transactionCode={donation.TransactionCode}&amount={donation.Amount}&method=vnpay";
    }

    /// <summary>
    /// Tạo URL thanh toán MoMo
    /// </summary>
    private string CreateMoMoPaymentUrl(Donation donation)
    {
        // TODO: Implement MoMo payment URL creation
        _logger.LogInformation($"Creating MoMo payment for transaction {donation.TransactionCode}");
        
        // Mock URL cho development
        return $"/Donation/PaymentSimulation?transactionCode={donation.TransactionCode}&amount={donation.Amount}&method=momo";
    }

    /// <summary>
    /// Lấy thông tin chuyển khoản ngân hàng
    /// </summary>
    private BankTransferInfoViewModel GetBankTransferInfo(string transactionCode, decimal amount)
    {
        return new BankTransferInfoViewModel
        {
            BankName = _bankSettings.BankName,
            AccountNumber = _bankSettings.AccountNumber,
            AccountName = _bankSettings.AccountName,
            Branch = _bankSettings.Branch,
            TransferContent = $"TT {transactionCode}",
            Amount = amount
        };
    }

    /// <summary>
    /// Trang mô phỏng thanh toán (cho development)
    /// </summary>
    [HttpGet]
    public IActionResult PaymentSimulation(string transactionCode, decimal amount, string method)
    {
        ViewBag.TransactionCode = transactionCode;
        ViewBag.Amount = amount;
        ViewBag.Method = method;
        return View();
    }

    /// <summary>
    /// Callback từ VNPAY
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> VNPayCallback()
    {
        // Lấy các tham số từ VNPAY callback
        var vnpResponseCode = Request.Query["vnp_ResponseCode"].ToString();
        var vnpTxnRef = Request.Query["vnp_TxnRef"].ToString();
        var vnpAmount = Request.Query["vnp_Amount"].ToString();
        var vnpSecureHash = Request.Query["vnp_SecureHash"].ToString();

        // TODO: Verify signature with vnpSecureHash
        
        if (vnpResponseCode == "00") // Thanh toán thành công
        {
            return await ProcessPaymentSuccess(vnpTxnRef);
        }
        else
        {
            return await ProcessPaymentFailed(vnpTxnRef, "Thanh toán thất bại hoặc bị hủy");
        }
    }

    /// <summary>
    /// Callback từ MoMo
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> MoMoCallback()
    {
        var resultCode = Request.Query["resultCode"].ToString();
        var orderId = Request.Query["orderId"].ToString();

        if (resultCode == "0") // Thanh toán thành công
        {
            return await ProcessPaymentSuccess(orderId);
        }
        else
        {
            return await ProcessPaymentFailed(orderId, "Thanh toán MoMo thất bại");
        }
    }

    /// <summary>
    /// Xác nhận thanh toán thành công (manual hoặc từ simulation)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> ConfirmPayment(string transactionCode)
    {
        return await ProcessPaymentSuccess(transactionCode);
    }

    /// <summary>
    /// Xử lý khi thanh toán thành công
    /// </summary>
    private async Task<IActionResult> ProcessPaymentSuccess(string transactionCode)
    {
        try
        {
            var donation = await _context.Donations
                .Include(d => d.Campaign)
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.TransactionCode == transactionCode);

            if (donation == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy giao dịch";
                return RedirectToAction("Index", "TrangChu");
            }

            if (donation.PaymentStatus == "completed")
            {
                // Đã xử lý rồi
                return RedirectToAction("Details", "TrangChu", new { id = donation.CampaignId });
            }

            // Cập nhật trạng thái donation
            donation.PaymentStatus = "completed";
            donation.DonatedAt = DateTime.Now;

            // Cập nhật số tiền campaign (Real-time tracking)
            var campaign = donation.Campaign;
            campaign.CurrentAmount = (campaign.CurrentAmount ?? 0) + donation.Amount;
            campaign.UpdatedAt = DateTime.Now;

            // Ghi nhận giao dịch tài chính
            var transaction = new FinancialTransaction
            {
                CampaignId = donation.CampaignId,
                Type = "donation",
                Amount = donation.Amount,
                Description = $"Quyên góp từ giao dịch #{donation.TransactionCode}",
                ReferenceId = donation.DonationId,
                CreatedAt = DateTime.Now
            };
            _context.FinancialTransactions.Add(transaction);

            // Tạo thông báo cho người quyên góp
            if (donation.UserId.HasValue)
            {
                var notification = new Notification
                {
                    UserId = donation.UserId.Value,
                    Title = "Quyên góp thành công",
                    Message = $"Cảm ơn bạn đã quyên góp {donation.Amount:N0} VNĐ cho chiến dịch \"{campaign.Title}\"",
                    Type = "donation",
                    IsRead = false,
                    CreatedAt = DateTime.Now
                };
                _context.Notifications.Add(notification);
            }

            // Tạo thông báo cho người tạo chiến dịch
            var creatorNotification = new Notification
            {
                UserId = campaign.CreatorId,
                Title = "Có quyên góp mới",
                Message = $"Chiến dịch \"{campaign.Title}\" vừa nhận được {donation.Amount:N0} VNĐ",
                Type = "donation",
                IsRead = false,
                CreatedAt = DateTime.Now
            };
            _context.Notifications.Add(creatorNotification);

            await _context.SaveChangesAsync();

            // Gửi email cảm ơn (async, không chờ)
            if (donation.User != null && !string.IsNullOrEmpty(donation.User.Email))
            {
                var donorName = donation.IsAnonymous == true ? "Nhà hảo tâm" : donation.User.Username;
                _ = _emailService.SendThankYouEmailAsync(donation.User.Email, donorName, campaign.Title, donation.Amount);
            }

            // Kiểm tra nếu đạt mục tiêu và xử lý tiền thừa
            await CheckAndProcessExcessFundAsync(campaign);

            _logger.LogInformation($"Payment completed for transaction {transactionCode}");

            TempData["SuccessMessage"] = $"Cảm ơn bạn đã quyên góp {donation.Amount:N0} VNĐ cho chiến dịch \"{campaign.Title}\"!";
            return RedirectToAction("Details", "TrangChu", new { id = donation.CampaignId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error processing payment success for {transactionCode}");
            TempData["ErrorMessage"] = "Đã xảy ra lỗi khi xử lý thanh toán";
            return RedirectToAction("Index", "TrangChu");
        }
    }

    /// <summary>
    /// Xử lý khi thanh toán thất bại
    /// </summary>
    private async Task<IActionResult> ProcessPaymentFailed(string transactionCode, string reason)
    {
        try
        {
            var donation = await _context.Donations
                .Include(d => d.Campaign)
                .FirstOrDefaultAsync(d => d.TransactionCode == transactionCode);

            if (donation != null)
            {
                donation.PaymentStatus = "failed";
                await _context.SaveChangesAsync();
            }

            _logger.LogWarning($"Payment failed for transaction {transactionCode}: {reason}");

            TempData["ErrorMessage"] = reason;
            return RedirectToAction("Details", "TrangChu", new { id = donation?.CampaignId ?? 0 });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error processing payment failure for {transactionCode}");
            return RedirectToAction("Index", "TrangChu");
        }
    }

    /// <summary>
    /// Kiểm tra và xử lý điều hướng tiền thừa khi đóng chiến dịch
    /// </summary>
    private async Task CheckAndProcessExcessFundAsync(Campaign campaign)
    {
        // Kiểm tra nếu vượt mục tiêu
        if (campaign.CurrentAmount > campaign.TargetAmount)
        {
            var excessAmount = campaign.CurrentAmount.Value - campaign.TargetAmount;
            
            _logger.LogInformation($"Campaign {campaign.CampaignId} exceeded target by {excessAmount:N0} VNĐ. ExcessFundOption: {campaign.ExcessFundOption}");

            // Xử lý tiền thừa khi chiến dịch đóng sẽ được thực hiện trong CloseCampaign
            // Đây chỉ ghi log để theo dõi
        }
    }

    /// <summary>
    /// Xử lý điều hướng tiền thừa khi đóng chiến dịch
    /// </summary>
    public async Task ProcessExcessFundOnCloseAsync(int campaignId)
    {
        var campaign = await _context.Campaigns.FindAsync(campaignId);
        if (campaign == null || campaign.CurrentAmount <= campaign.TargetAmount)
        {
            return;
        }

        var excessAmount = campaign.CurrentAmount.Value - campaign.TargetAmount;

        switch (campaign.ExcessFundOption?.ToLower())
        {
            case "next_case":
                // Chuyển sang chiến dịch tiếp theo cùng danh mục
                var nextCampaign = await _context.Campaigns
                    .Where(c => c.CategoryId == campaign.CategoryId 
                        && c.Status == "active" 
                        && c.CampaignId != campaign.CampaignId)
                    .OrderBy(c => c.CreatedAt)
                    .FirstOrDefaultAsync();

                if (nextCampaign != null)
                {
                    nextCampaign.CurrentAmount = (nextCampaign.CurrentAmount ?? 0) + excessAmount;
                    
                    // Ghi nhận giao dịch
                    _context.FinancialTransactions.Add(new FinancialTransaction
                    {
                        CampaignId = nextCampaign.CampaignId,
                        Type = "transfer_in",
                        Amount = excessAmount,
                        Description = $"Tiền thừa từ chiến dịch #{campaign.CampaignId}",
                        CreatedAt = DateTime.Now
                    });

                    _logger.LogInformation($"Transferred {excessAmount:N0} VNĐ from campaign {campaign.CampaignId} to {nextCampaign.CampaignId}");
                }
                break;

            case "general_fund":
                // Chuyển vào quỹ chung - không có CampaignId
                // Lưu ý: FinancialTransaction yêu cầu CampaignId, cần xử lý đặc biệt
                _logger.LogInformation($"Excess fund {excessAmount:N0} VNĐ from campaign {campaign.CampaignId} should go to general fund");
                break;

            case "refund":
                // Hoàn trả cho người quyên góp - cần xử lý riêng
                _logger.LogInformation($"Refund requested for {excessAmount:N0} VNĐ from campaign {campaign.CampaignId}");
                // TODO: Implement refund logic
                break;


            default:
                _logger.LogWarning($"Unknown excess fund option: {campaign.ExcessFundOption}");
                break;
        }

        await _context.SaveChangesAsync();
    }
}
