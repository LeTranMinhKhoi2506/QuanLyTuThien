using System;
using System.Collections.Generic;

namespace TuThien.Models;

public partial class CampaignMilestone
{
    public int MilestoneId { get; set; }

    public int CampaignId { get; set; }

    public string? Title { get; set; }

    public decimal AmountNeeded { get; set; }

    public DateTime? Deadline { get; set; }

    public string? Status { get; set; }

    public virtual Campaign Campaign { get; set; } = null!;
}
