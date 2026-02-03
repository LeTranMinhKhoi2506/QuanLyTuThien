using System;
using System.Collections.Generic;

namespace TuThien.Models;

public partial class DisbursementRequest
{
    public int RequestId { get; set; }

    public int CampaignId { get; set; }

    public int RequesterId { get; set; }

    public decimal Amount { get; set; }

    public string Reason { get; set; } = null!;

    public string? ProofImages { get; set; }

    public string? Status { get; set; }

    public string? AdminNote { get; set; }

    public int? ApprovedBy { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual User? ApprovedByNavigation { get; set; }

    public virtual Campaign Campaign { get; set; } = null!;

    public virtual User Requester { get; set; } = null!;
}
