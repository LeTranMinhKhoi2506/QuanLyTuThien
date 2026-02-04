using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace TuThien.Models
{
    public class CampaignCreateViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập tiêu đề chiến dịch")]
        [StringLength(200, ErrorMessage = "Tiêu đề không được vượt quá 200 ký tự")]
        [Display(Name = "Tiêu đề chiến dịch")]
        public string Title { get; set; } = null!;

        [Required(ErrorMessage = "Vui lòng nhập mô tả chi tiết")]
        [Display(Name = "Mô tả chi tiết")]
        public string Description { get; set; } = null!;

        [Required(ErrorMessage = "Vui lòng nhập số tiền mục tiêu")]
        [Range(100000, double.MaxValue, ErrorMessage = "Số tiền mục tiêu phải ít nhất là 100,000 VNĐ")]
        [Display(Name = "Số tiền mục tiêu (VNĐ)")]
        // Format currency for display? No, input should be raw number.
        public decimal TargetAmount { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ngày bắt đầu")]
        [Display(Name = "Ngày bắt đầu")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ngày kết thúc")]
        [Display(Name = "Ngày kết thúc")]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        [Display(Name = "Ảnh đại diện")]
        public IFormFile? ThumbnailImage { get; set; }

        [Display(Name = "Giấy xác nhận hoàn cảnh/chiến dịch")]
        public List<IFormFile> VerificationDocuments { get; set; } = new List<IFormFile>();

        [Display(Name = "Mô tả tài liệu")]
        public List<string> VerificationDocDescriptions { get; set; } = new List<string>();

        [Required(ErrorMessage = "Vui lòng chọn danh mục")]
        [Display(Name = "Danh mục")]
        public int? CategoryId { get; set; }
        
        [Display(Name = "Phương án xử lý quỹ dư thừa")]
        public string? ExcessFundOption { get; set; }
    }
}
