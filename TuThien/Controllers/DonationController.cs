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
    private readonly IMoMoService _momoService;
    private readonly DonationSettings _donationSettings;
    private readonly BankSettings _bankSettings;
    private readonly VNPaySettings _vnpaySettings;
    private readonly MoMoSettings _momoSettings;

    public DonationController(
        TuThienContext context, 
        ILogger<DonationController> logger, 
        IEmailService emailService,
        IDonationValidationService validationService,
        IMoMoService momoService,
        IOptions<DonationSettings> donationSettings,
        IOptions<BankSettings> bankSettings,
        IOptions<VNPaySettings> vnpaySettings,
        IOptions<MoMoSettings> momoSettings)
    {
        _context = context;
        _logger = logger;
        _emailService = emailService;
        _validationService = validationService;
        _momoService = momoService;
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
    /// Trang quyên góp của tôi (Tổng hợp + Lịch sử)
    /// </summary>
    [HttpGet]
    [Route("my-donations")]
    public async Task<IActionResult> MyDonations()
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return RedirectToAction("Login", "Account", new { returnUrl = "/my-donations" });
        }

        var donations = await _context.Donations
            .Include(d => d.Campaign)
            .Where(d => d.UserId == userId.Value)
            .OrderByDescending(d => d.DonatedAt)
            .ToListAsync();

        // Thống kê
        ViewBag.TotalDonations = donations.Count;
        ViewBag.SuccessfulDonations = donations.Count(d => d.PaymentStatus == "success");
        ViewBag.TotalAmount = donations.Where(d => d.PaymentStatus == "success").Sum(d => d.Amount);
        ViewBag.CampaignsSupported = donations.Where(d => d.PaymentStatus == "success")
            .Select(d => d.CampaignId).Distinct().Count();

        return View(donations);
    }


    /// <summary>
    /// Trang chính quyên góp - Hiển thị danh sách chiến dịch để người dùng chọn
    /// Route: /donate, /donate/{id} hoặc /Donation/Index
    /// </summary>
    [HttpGet]
    [Route("donate")]
    [Route("donate/{campaignId:int}")]
    [Route("Donation")]
    [Route("Donation/Index")]
    public async Task<IActionResult> Index(int? campaignId = null)
    {
        // Kiểm tra đăng nhập - yêu cầu đăng nhập để quyên góp
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            TempData["ErrorMessage"] = "Vui lòng đăng nhập để quyên góp";
            // Lưu URL hiện tại để redirect sau khi đăng nhập
            var returnUrl = campaignId.HasValue ? $"/donate/{campaignId}" : "/donate";
            return RedirectToAction("Login", "Account", new { returnUrl });
        }

        // Lấy danh sách chiến dịch đang hoạt động
        var activeCampaigns = await _context.Campaigns
            .Include(c => c.Category)
            .Include(c => c.Creator)
            .Include(c => c.CampaignMilestones.OrderBy(m => m.MilestoneId))
            .Where(c => c.Status == "active")
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        ViewBag.Campaigns = activeCampaigns;
        ViewBag.DonationSettings = _donationSettings;

        // Nếu có campaignId, preselect chiến dịch đó
        if (campaignId.HasValue)
        {
            var selectedCampaign = activeCampaigns.FirstOrDefault(c => c.CampaignId == campaignId.Value);
            ViewBag.SelectedCampaign = selectedCampaign;
        }

        return View();
    }

    /// <summary>
    /// Trang quyên góp cho chiến dịch cụ thể
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
            .Where(d => d.UserId == userId.Value && d.PaymentStatus == "success")
            .SumAsync(d => (decimal?)d.Amount) ?? 0;

        var totalCampaigns = await _context.Donations
            .Where(d => d.UserId == userId.Value && d.PaymentStatus == "success")
            .Select(d => d.CampaignId)
            .Distinct()
            .CountAsync();

        var totalDonations = await _context.Donations
            .Where(d => d.UserId == userId.Value && d.PaymentStatus == "success")
            .CountAsync();

        ViewBag.TotalDonated = totalDonated;
        ViewBag.TotalCampaigns = totalCampaigns;
        ViewBag.TotalDonations = totalDonations;
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
            .Include(c => c.CampaignMilestones.OrderBy(m => m.MilestoneId))
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
    public async Task<IActionResult> ProcessDonation([FromBody] DonationViewModel model)
    {
        _logger.LogInformation("ProcessDonation called with CampaignId: {CampaignId}, Amount: {Amount}, PaymentMethod: {PaymentMethod}", 
            model?.CampaignId, model?.Amount, model?.PaymentMethod);

        try
        {
            // Kiểm tra đăng nhập
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                _logger.LogWarning("ProcessDonation: User not logged in");
                return Json(new DonationResponseViewModel 
                { 
                    Success = false, 
                    Message = "Vui lòng đăng nhập để quyên góp",
                    RequireLogin = true
                });
            }

            if (model == null)
            {
                _logger.LogWarning("ProcessDonation: Model is null");
                return Json(new DonationResponseViewModel 
                { 
                    Success = false, 
                    Message = "Dữ liệu không hợp lệ"
                });
            }

            // Server-side validation sử dụng validation service
            var validationResult = _validationService.ValidateDonation(model);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("ProcessDonation validation failed: {Errors}", string.Join("; ", validationResult.Errors));
                return Json(new DonationResponseViewModel 
                { 
                    Success = false, 
                    Message = string.Join("; ", validationResult.Errors)
                });
            }

            var campaign = await _context.Campaigns.FindAsync(model.CampaignId);
            if (campaign == null || campaign.Status != "active")
            {
                _logger.LogWarning("ProcessDonation: Campaign not found or inactive. CampaignId: {CampaignId}", model.CampaignId);
                return Json(new DonationResponseViewModel 
                { 
                    Success = false, 
                    Message = "Chiến dịch không hợp lệ hoặc đã kết thúc" 
                });
            }

            // Tạo mã giao dịch unique
            var transactionCode = $"TT{DateTime.Now:yyyyMMddHHmmss}{new Random().Next(1000, 9999)}";

            _logger.LogInformation("Creating donation with TransactionCode: {TransactionCode}", transactionCode);

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
                    var momoResult = await CreateMoMoPaymentAsync(donation, campaign.Title);
                    if (momoResult.Success)
                    {
                        return Json(new DonationResponseViewModel 
                        { 
                            Success = true, 
                            RedirectUrl = momoResult.PayUrl,
                            TransactionCode = transactionCode
                        });
                    }
                    else
                    {
                        // Rollback donation nếu tạo payment thất bại
                        _context.Donations.Remove(donation);
                        await _context.SaveChangesAsync();
                        
                        return Json(new DonationResponseViewModel 
                        { 
                            Success = false, 
                            Message = momoResult.Message ?? "Không thể tạo thanh toán MoMo"
                        });
                    }

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
    /// Tạo thanh toán MoMo thực tế qua API
    /// </summary>
    private async Task<MoMoPaymentResponse> CreateMoMoPaymentAsync(Donation donation, string campaignTitle)
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        
        var request = new MoMoPaymentRequest
        {
            OrderId = donation.TransactionCode ?? $"TT{DateTime.Now.Ticks}",
            OrderInfo = $"Quyên góp cho chiến dịch: {campaignTitle}",
            Amount = (long)donation.Amount,
            ReturnUrl = $"{baseUrl}{_momoSettings.ReturnUrl}",
            NotifyUrl = $"{baseUrl}{_momoSettings.NotifyUrl}",
            ExtraData = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(
                JsonSerializer.Serialize(new { donationId = donation.DonationId })))
        };

        _logger.LogInformation("Creating MoMo payment for donation {DonationId}, TransactionCode: {TransactionCode}", 
            donation.DonationId, donation.TransactionCode);

        return await _momoService.CreatePaymentAsync(request);
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
    /// Trang mô phỏng thanh toán (cho development khi không có ngrok)
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
    /// MoMo Return URL - Redirect sau khi thanh toán
    /// </summary>
    [HttpGet]
    [Route("Donation/MoMoReturn")]
    public async Task<IActionResult> MoMoReturn()
    {
        var partnerCode = Request.Query["partnerCode"].ToString();
        var orderId = Request.Query["orderId"].ToString();
        var requestId = Request.Query["requestId"].ToString();
        var amount = Request.Query["amount"].ToString();
        var orderInfo = Request.Query["orderInfo"].ToString();
        var orderType = Request.Query["orderType"].ToString();
        var transId = Request.Query["transId"].ToString();
        var resultCode = Request.Query["resultCode"].ToString();
        var message = Request.Query["message"].ToString();
        var payType = Request.Query["payType"].ToString();
        var responseTime = Request.Query["responseTime"].ToString();
        var extraData = Request.Query["extraData"].ToString();
        var signature = Request.Query["signature"].ToString();

        _logger.LogInformation("MoMo Return - OrderId: {OrderId}, ResultCode: {ResultCode}, Message: {Message}, TransId: {TransId}", 
            orderId, resultCode, message, transId);

        // Trong môi trường dev, bỏ qua verify signature vì MoMo sandbox đôi khi không khớp
        // Production nên bật lại signature verification
        #if !DEBUG
        // Verify signature
        var rawSignature = $"accessKey={_momoSettings.AccessKey}" +
                          $"&amount={amount}" +
                          $"&extraData={extraData}" +
                          $"&message={message}" +
                          $"&orderId={orderId}" +
                          $"&orderInfo={orderInfo}" +
                          $"&orderType={orderType}" +
                          $"&partnerCode={partnerCode}" +
                          $"&payType={payType}" +
                          $"&requestId={requestId}" +
                          $"&responseTime={responseTime}" +
                          $"&resultCode={resultCode}" +
                          $"&transId={transId}";

        var isValidSignature = _momoService.ValidateSignature(rawSignature, signature);
        
        if (!isValidSignature)
        {
            _logger.LogWarning("MoMo Return - Invalid signature for OrderId: {OrderId}", orderId);
            TempData["ErrorMessage"] = "Chữ ký không hợp lệ";
            return RedirectToAction("Index", "TrangChu");
        }
        #endif

        // Chỉ xử lý khi thanh toán thành công (resultCode = 0)
        if (resultCode == "0")
        {
            _logger.LogInformation("MoMo payment SUCCESS - Processing OrderId: {OrderId}", orderId);
            return await ProcessPaymentSuccess(orderId);
        }
        else
        {
            // Không ghi đơn failed vào DB cho các trường hợp hủy
            // Chỉ log và redirect về trang thất bại
            _logger.LogWarning("MoMo payment FAILED - OrderId: {OrderId}, ResultCode: {ResultCode}, Message: {Message}", 
                orderId, resultCode, message);
            
            var errorMessage = resultCode switch
            {
                "1001" => "Giao dịch thất bại do tài khoản không đủ số dư",
                "1002" => "Giao dịch bị từ chối bởi nhà phát hành",
                "1003" => "Giao dịch bị hủy bởi người dùng",
                "1004" => "Số tiền vượt quá hạn mức giao dịch",
                "1005" => "Url hoặc QR code đã hết hạn",
                "1006" => "Người dùng đã từ chối xác nhận thanh toán",
                "1007" => "Không đủ thông tin",
                "49" => "Người dùng chưa hoàn thành thanh toán",
                _ => $"Thanh toán MoMo thất bại (Mã lỗi: {resultCode})"
            };
            
            // Không cập nhật status failed, chỉ redirect với thông báo lỗi
            TempData["ErrorMessage"] = errorMessage;
            
            // Tìm donation để lấy campaignId
            var donation = await _context.Donations
                .FirstOrDefaultAsync(d => d.TransactionCode == orderId);
            
            if (donation != null)
            {
                return RedirectToAction("DonationFailed", new { transactionCode = orderId, message = errorMessage });
            }
            
            return RedirectToAction("Index", "TrangChu");
        }
    }

    /// <summary>
    /// MoMo IPN (Instant Payment Notification) - Server to Server callback
    /// </summary>
    [HttpPost]
    [Route("Donation/MoMoNotify")]
    public async Task<IActionResult> MoMoNotify([FromBody] MoMoCallbackRequest callback)
    {
        _logger.LogInformation("MoMo IPN - OrderId: {OrderId}, ResultCode: {ResultCode}", 
            callback.OrderId, callback.ResultCode);

        try
        {
            // Verify signature
            var rawSignature = $"accessKey={_momoSettings.AccessKey}" +
                              $"&amount={callback.Amount}" +
                              $"&extraData={callback.ExtraData}" +
                              $"&message={callback.Message}" +
                              $"&orderId={callback.OrderId}" +
                              $"&orderInfo={callback.OrderInfo}" +
                              $"&orderType={callback.OrderType}" +
                              $"&partnerCode={callback.PartnerCode}" +
                              $"&payType={callback.PayType}" +
                              $"&requestId={callback.RequestId}" +
                              $"&responseTime={callback.ResponseTime}" +
                              $"&resultCode={callback.ResultCode}" +
                              $"&transId={callback.TransId}";

            var isValidSignature = _momoService.ValidateSignature(rawSignature, callback.Signature ?? "");
            
            if (!isValidSignature)
            {
                _logger.LogWarning("MoMo IPN - Invalid signature for OrderId: {OrderId}", callback.OrderId);
                return BadRequest(new { message = "Invalid signature" });
            }

            if (callback.ResultCode == 0) // Thanh toán thành công
            {
                await ProcessPaymentSuccessInternal(callback.OrderId ?? "");
            }
            else
            {
                // Không cập nhật status failed cho đơn bị huỷ - chỉ log
                _logger.LogWarning("MoMo IPN - Payment failed/cancelled - OrderId: {OrderId}, ResultCode: {ResultCode}", 
                    callback.OrderId, callback.ResultCode);
            }

            // Return success response to MoMo
            return Ok(new { message = "Success" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing MoMo IPN for OrderId: {OrderId}", callback.OrderId);
            return StatusCode(500, new { message = "Internal error" });
        }
    }

    /// <summary>
    /// Callback cũ từ MoMo (backward compatibility)
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
            // Không cập nhật status failed, chỉ redirect với thông báo lỗi
            _logger.LogWarning("MoMoCallback - Payment failed - OrderId: {OrderId}, ResultCode: {ResultCode}", orderId, resultCode);
            TempData["ErrorMessage"] = "Thanh toán MoMo thất bại";
            return RedirectToAction("DonationFailed", new { transactionCode = orderId, message = "Thanh toán MoMo thất bại" });
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
        _logger.LogInformation("ProcessPaymentSuccess called for TransactionCode: {TransactionCode}", transactionCode);
        
        var result = await ProcessPaymentSuccessInternal(transactionCode);
        
        if (result)
        {
            var donation = await _context.Donations
                .Include(d => d.Campaign)
                .FirstOrDefaultAsync(d => d.TransactionCode == transactionCode);

            if (donation != null)
            {
                TempData["SuccessMessage"] = $"Cảm ơn bạn đã quyên góp {donation.Amount:N0} VNĐ!";
                return RedirectToAction("DonationSuccess", new { transactionCode });
            }
        }

        TempData["ErrorMessage"] = "Không thể xử lý giao dịch";
        return RedirectToAction("Index", "TrangChu");
    }

    /// <summary>
    /// Xử lý nội bộ khi thanh toán thành công (không redirect - dùng cho IPN)
    /// </summary>
    /// <returns>True nếu xử lý thành công</returns>
    private async Task<bool> ProcessPaymentSuccessInternal(string transactionCode)
    {
        try
        {
            _logger.LogInformation("ProcessPaymentSuccessInternal starting for TransactionCode: {TransactionCode}", transactionCode);
            
            var donation = await _context.Donations
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.TransactionCode == transactionCode);

            if (donation == null)
            {
                _logger.LogWarning("Transaction not found: {TransactionCode}", transactionCode);
                return false;
            }

            _logger.LogInformation("Found donation - Id: {DonationId}, Status: {Status}, Amount: {Amount}, CampaignId: {CampaignId}", 
                donation.DonationId, donation.PaymentStatus, donation.Amount, donation.CampaignId);

            if (donation.PaymentStatus == "success")
            {
                _logger.LogInformation("Transaction already completed: {TransactionCode}", transactionCode);
                return true; // Return true because it was already processed
            }

            // Cập nhật trạng thái donation
            donation.PaymentStatus = "success";
            donation.DonatedAt = DateTime.Now;

            // Load Campaign trực tiếp từ database để đảm bảo tracking đúng
            var campaign = await _context.Campaigns.FindAsync(donation.CampaignId);
            if (campaign != null)
            {
                var oldAmount = campaign.CurrentAmount ?? 0;
                campaign.CurrentAmount = oldAmount + donation.Amount;
                campaign.UpdatedAt = DateTime.Now;

                _logger.LogInformation("Updating campaign {CampaignId} ({Title}) amount: {OldAmount} -> {NewAmount}", 
                    campaign.CampaignId, campaign.Title, oldAmount, campaign.CurrentAmount);

                // Đánh dấu campaign đã thay đổi
                _context.Entry(campaign).State = EntityState.Modified;

                // Ghi nhận giao dịch tài chính
                var transaction = new FinancialTransaction
                {
                    CampaignId = donation.CampaignId,
                    Type = "in", // 'in' = tiền vào, 'out' = tiền ra
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
                        Type = "payment", // CHECK constraint: 'system', 'campaign_update', 'payment'
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
                    Type = "payment", // CHECK constraint: 'system', 'campaign_update', 'payment'
                    IsRead = false,
                    CreatedAt = DateTime.Now
                };
                _context.Notifications.Add(creatorNotification);

                // Kiểm tra nếu đạt mục tiêu
                await CheckAndProcessExcessFundAsync(campaign);
                
                // Save changes
                var savedCount = await _context.SaveChangesAsync();
                _logger.LogInformation("SaveChanges completed - {Count} entities saved for transaction {TransactionCode}", 
                    savedCount, transactionCode);
                
                // Verify the update
                var verifyAmount = await _context.Campaigns
                    .Where(c => c.CampaignId == campaign.CampaignId)
                    .Select(c => c.CurrentAmount)
                    .FirstOrDefaultAsync();
                _logger.LogInformation("Verified campaign {CampaignId} CurrentAmount in DB: {Amount}", 
                    campaign.CampaignId, verifyAmount);
            }
            else
            {
                _logger.LogWarning("Campaign not found for donation {DonationId}, CampaignId: {CampaignId}", 
                    donation.DonationId, donation.CampaignId);
                await _context.SaveChangesAsync();
            }

            _logger.LogInformation("Payment completed successfully for transaction {TransactionCode}", transactionCode);

            // Gửi email cảm ơn (async, không chờ)
            if (donation.User != null && !string.IsNullOrEmpty(donation.User.Email) && campaign != null)
            {
                var donorName = donation.IsAnonymous == true ? "Nhà hảo tâm" : donation.User.Username;
                _ = _emailService.SendThankYouEmailAsync(donation.User.Email, donorName, campaign.Title, donation.Amount);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment success for {TransactionCode}", transactionCode);
            return false;
        }
    }

    /// <summary>
    /// Trang hiển thị quyên góp thành công
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> DonationSuccess(string transactionCode)
    {
        var donation = await _context.Donations
            .Include(d => d.Campaign)
            .FirstOrDefaultAsync(d => d.TransactionCode == transactionCode);

        if (donation == null)
        {
            return RedirectToAction("Index", "TrangChu");
        }

        return View(donation);
    }

    /// <summary>
    /// Xử lý khi thanh toán thất bại
    /// </summary>
    private async Task<IActionResult> ProcessPaymentFailed(string transactionCode, string reason)
    {
        await ProcessPaymentFailedInternal(transactionCode, reason);
        
        var donation = await _context.Donations
            .FirstOrDefaultAsync(d => d.TransactionCode == transactionCode);

        TempData["ErrorMessage"] = reason;
        return RedirectToAction("DonationFailed", new { transactionCode, message = reason });
    }

    /// <summary>
    /// Xử lý nội bộ khi thanh toán thất bại (không redirect - dùng cho IPN)
    /// </summary>
    private async Task ProcessPaymentFailedInternal(string transactionCode, string reason)
    {
        try
        {
            var donation = await _context.Donations
                .FirstOrDefaultAsync(d => d.TransactionCode == transactionCode);

            if (donation != null && donation.PaymentStatus != "success")
            {
                donation.PaymentStatus = "failed";
                await _context.SaveChangesAsync();
            }

            _logger.LogWarning("Payment failed for transaction {TransactionCode}: {Reason}", transactionCode, reason);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment failure for {TransactionCode}", transactionCode);
        }
    }

    /// <summary>
    /// Trang hiển thị quyên góp thất bại
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> DonationFailed(string transactionCode, string? message)
    {
        var donation = await _context.Donations
            .Include(d => d.Campaign)
            .FirstOrDefaultAsync(d => d.TransactionCode == transactionCode);

        ViewBag.ErrorMessage = message ?? "Thanh toán thất bại";
        return View(donation);
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
