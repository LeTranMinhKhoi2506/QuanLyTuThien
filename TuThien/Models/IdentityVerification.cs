using System;
using System.Collections.Generic;

namespace TuThien.Models;

public partial class IdentityVerification
{
    public int VerificationId { get; set; }

    public int? UserId { get; set; }

    public string DocumentType { get; set; } = null!;

    public string DocumentUrlFront { get; set; } = null!;

    public string? DocumentUrlBack { get; set; }

    public string? Status { get; set; }

    public string? AdminNote { get; set; }

    public int? ReviewedBy { get; set; }

    public DateTime? SubmittedAt { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public virtual User? ReviewedByNavigation { get; set; }

    public virtual User? User { get; set; }
}
