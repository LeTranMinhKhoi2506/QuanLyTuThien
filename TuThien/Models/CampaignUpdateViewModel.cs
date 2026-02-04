using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace TuThien.Models
{
    /// <summary>
    /// ViewModel cho việc tạo tin tức/cập nhật chiến dịch
    /// </summary>
    public class CampaignUpdateViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập tiêu đề tin tức")]
        [StringLength(255, MinimumLength = 10, ErrorMessage = "Tiêu đề phải từ 10-255 ký tự")]
        [Display(Name = "Tiêu đề tin tức")]
        public string Title { get; set; } = null!;

        [Required(ErrorMessage = "Vui lòng nhập nội dung")]
        [MinLength(50, ErrorMessage = "Nội dung phải có ít nhất 50 ký tự")]
        [Display(Name = "Nội dung chi tiết")]
        public string Content { get; set; } = null!;

        [Required(ErrorMessage = "Vui lòng chọn loại tin tức")]
        [Display(Name = "Loại tin tức")]
        public string Type { get; set; } = "general";

        [Display(Name = "Hình ảnh minh họa")]
        public List<IFormFile>? Images { get; set; }

        [Required]
        public int CampaignId { get; set; }

        // Thông tin campaign để hiển thị trên form
        public string? CampaignTitle { get; set; }
        public string? CampaignThumbnail { get; set; }
    }
}
