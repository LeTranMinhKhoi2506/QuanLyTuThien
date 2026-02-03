using System;
using System.Collections.Generic;

namespace TuThien.Models;

public partial class Donation
{
    public int DonationId { get; set; }

    public int CampaignId { get; set; }

    public int? UserId { get; set; }

    public decimal Amount { get; set; }

    public string? Message { get; set; }

    public bool? IsAnonymous { get; set; }

    public string? PaymentMethod { get; set; }

    public string? TransactionCode { get; set; }

    public string? PaymentStatus { get; set; }

    public DateTime? DonatedAt { get; set; }

    public virtual Campaign Campaign { get; set; } = null!;

    public virtual User? User { get; set; }
}
