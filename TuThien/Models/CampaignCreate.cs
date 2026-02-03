using Microsoft.Build.Framework;

namespace TuThien.Models
{
    public class CampaignCreate
    {
        [Required]public int? CategoryId { get; set; }

        [Required] public string Title { get; set; } 

        [Required] public string Description { get; set; }

        [Required] public decimal TargetAmount { get; set; }

        [Required] public DateTime? StartDate { get; set; }

        [Required] public DateTime? EndDate { get; set; }

        public string? ThumbnailUrl { get; set; }

        public string? ExcessFundOption { get; set; }

        [Required] public string? Status { get; set; }
    }
}
