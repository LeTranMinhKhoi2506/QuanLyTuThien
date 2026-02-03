using System;
using System.Collections.Generic;

namespace TuThien.Models;

public partial class FinancialTransaction
{
    public int TransactionId { get; set; }

    public int CampaignId { get; set; }

    public string Type { get; set; } = null!;

    public decimal Amount { get; set; }

    public string? Description { get; set; }

    public int? ReferenceId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Campaign Campaign { get; set; } = null!;
}
