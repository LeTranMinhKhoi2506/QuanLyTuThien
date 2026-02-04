using System;
using System.Collections.Generic;

namespace TuThien.Models;

public partial class Campaign
{
    public int CampaignId { get; set; }

    public int CreatorId { get; set; }

    public int? CategoryId { get; set; }

    public string Title { get; set; } = null!;

    public string Description { get; set; } = null!;

    public decimal TargetAmount { get; set; }

    public decimal? CurrentAmount { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public string? ThumbnailUrl { get; set; }

    public string? ExcessFundOption { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }



    public virtual ICollection<CampaignDocument> CampaignDocuments { get; set; } = new List<CampaignDocument>();

    public virtual ICollection<CampaignMilestone> CampaignMilestones { get; set; } = new List<CampaignMilestone>();

    public virtual ICollection<CampaignUpdate> CampaignUpdates { get; set; } = new List<CampaignUpdate>();

    public virtual Category? Category { get; set; }

    public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();

    public virtual User Creator { get; set; } = null!;

    public virtual ICollection<DisbursementRequest> DisbursementRequests { get; set; } = new List<DisbursementRequest>();

    public virtual ICollection<Donation> Donations { get; set; } = new List<Donation>();

    public virtual ICollection<FinancialTransaction> FinancialTransactions { get; set; } = new List<FinancialTransaction>();
}
