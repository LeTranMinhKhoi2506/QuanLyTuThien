using System;
using System.Collections.Generic;

namespace TuThien.Models;

public partial class User
{
    public int UserId { get; set; }

    public string Username { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string? PhoneNumber { get; set; }

    public string? Role { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

    public virtual ICollection<CampaignUpdate> CampaignUpdates { get; set; } = new List<CampaignUpdate>();

    public virtual ICollection<Campaign> Campaigns { get; set; } = new List<Campaign>();

    public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();

    public virtual ICollection<DisbursementRequest> DisbursementRequestApprovedByNavigations { get; set; } = new List<DisbursementRequest>();

    public virtual ICollection<DisbursementRequest> DisbursementRequestRequesters { get; set; } = new List<DisbursementRequest>();

    public virtual ICollection<Donation> Donations { get; set; } = new List<Donation>();

    public virtual ICollection<IdentityVerification> IdentityVerificationReviewedByNavigations { get; set; } = new List<IdentityVerification>();

    public virtual ICollection<IdentityVerification> IdentityVerificationUsers { get; set; } = new List<IdentityVerification>();

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual ICollection<Report> Reports { get; set; } = new List<Report>();

    public virtual UserProfile? UserProfile { get; set; }
}
