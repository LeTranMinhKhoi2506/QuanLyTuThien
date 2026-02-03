using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace TuThien.Models;

public partial class TuThienContext : DbContext
{
    public TuThienContext()
    {
    }

    public TuThienContext(DbContextOptions<TuThienContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AuditLog> AuditLogs { get; set; }

    public virtual DbSet<Campaign> Campaigns { get; set; }

    public virtual DbSet<CampaignDocument> CampaignDocuments { get; set; }

    public virtual DbSet<CampaignMilestone> CampaignMilestones { get; set; }

    public virtual DbSet<CampaignUpdate> CampaignUpdates { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<Comment> Comments { get; set; }

    public virtual DbSet<DisbursementRequest> DisbursementRequests { get; set; }

    public virtual DbSet<Donation> Donations { get; set; }

    public virtual DbSet<FinancialTransaction> FinancialTransactions { get; set; }

    public virtual DbSet<IdentityVerification> IdentityVerifications { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<Report> Reports { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserProfile> UserProfiles { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            // Connection string is configured in Program.cs via appsettings.json
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.LogId).HasName("PK__AuditLog__9E2397E0CD1C6BF4");

            entity.Property(e => e.LogId).HasColumnName("log_id");
            entity.Property(e => e.Action)
                .HasMaxLength(50)
                .HasColumnName("action");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created_at");
            entity.Property(e => e.IpAddress)
                .HasMaxLength(45)
                .IsUnicode(false)
                .HasColumnName("ip_address");
            entity.Property(e => e.NewValue).HasColumnName("new_value");
            entity.Property(e => e.OldValue).HasColumnName("old_value");
            entity.Property(e => e.RecordId).HasColumnName("record_id");
            entity.Property(e => e.TableName)
                .HasMaxLength(50)
                .HasColumnName("table_name");
            entity.Property(e => e.UserAgent)
                .HasMaxLength(255)
                .HasColumnName("user_agent");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.AuditLogs)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__AuditLogs__user___0D7A0286");
        });

        modelBuilder.Entity<Campaign>(entity =>
        {
            entity.HasKey(e => e.CampaignId).HasName("PK__Campaign__905B681CB618D582");

            entity.ToTable(tb => tb.HasTrigger("trg_UpdateCampaigns"));

            entity.Property(e => e.CampaignId).HasColumnName("campaign_id");
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatorId).HasColumnName("creator_id");
            entity.Property(e => e.CurrentAmount)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("current_amount");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.ExcessFundOption)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("next_case")
                .HasColumnName("excess_fund_option");
            entity.Property(e => e.StartDate).HasColumnName("start_date");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("draft")
                .HasColumnName("status");
            entity.Property(e => e.TargetAmount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("target_amount");
            entity.Property(e => e.ThumbnailUrl)
                .HasMaxLength(255)
                .HasColumnName("thumbnail_url");
            entity.Property(e => e.Title)
                .HasMaxLength(255)
                .HasColumnName("title");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Category).WithMany(p => p.Campaigns)
                .HasForeignKey(d => d.CategoryId)
                .HasConstraintName("FK__Campaigns__categ__5629CD9C");

            entity.HasOne(d => d.Creator).WithMany(p => p.Campaigns)
                .HasForeignKey(d => d.CreatorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Campaigns__creat__5535A963");
        });

        modelBuilder.Entity<CampaignDocument>(entity =>
        {
            entity.HasKey(e => e.DocumentId).HasName("PK__Campaign__9666E8AC4BB68553");

            entity.Property(e => e.DocumentId).HasColumnName("document_id");
            entity.Property(e => e.CampaignId).HasColumnName("campaign_id");
            entity.Property(e => e.Description)
                .HasMaxLength(255)
                .HasColumnName("description");
            entity.Property(e => e.FileType)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("file_type");
            entity.Property(e => e.FileUrl)
                .HasMaxLength(255)
                .HasColumnName("file_url");

            entity.HasOne(d => d.Campaign).WithMany(p => p.CampaignDocuments)
                .HasForeignKey(d => d.CampaignId)
                .HasConstraintName("FK__CampaignD__campa__5EBF139D");
        });

        modelBuilder.Entity<CampaignMilestone>(entity =>
        {
            entity.HasKey(e => e.MilestoneId).HasName("PK__Campaign__67592EB7A2D63847");

            entity.Property(e => e.MilestoneId).HasColumnName("milestone_id");
            entity.Property(e => e.AmountNeeded)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("amount_needed");
            entity.Property(e => e.CampaignId).HasColumnName("campaign_id");
            entity.Property(e => e.Deadline).HasColumnName("deadline");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("pending")
                .HasColumnName("status");
            entity.Property(e => e.Title)
                .HasMaxLength(150)
                .HasColumnName("title");

            entity.HasOne(d => d.Campaign).WithMany(p => p.CampaignMilestones)
                .HasForeignKey(d => d.CampaignId)
                .HasConstraintName("FK__CampaignM__campa__5AEE82B9");
        });

        modelBuilder.Entity<CampaignUpdate>(entity =>
        {
            entity.HasKey(e => e.UpdateId).HasName("PK__Campaign__68D2E0797C2D946D");

            entity.Property(e => e.UpdateId).HasColumnName("update_id");
            entity.Property(e => e.AuthorId).HasColumnName("author_id");
            entity.Property(e => e.CampaignId).HasColumnName("campaign_id");
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created_at");
            entity.Property(e => e.ImageUrls).HasColumnName("image_urls");
            entity.Property(e => e.Title)
                .HasMaxLength(255)
                .HasColumnName("title");
            entity.Property(e => e.Type)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("general")
                .HasColumnName("type");

            entity.HasOne(d => d.Author).WithMany(p => p.CampaignUpdates)
                .HasForeignKey(d => d.AuthorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__CampaignU__autho__7D439ABD");

            entity.HasOne(d => d.Campaign).WithMany(p => p.CampaignUpdates)
                .HasForeignKey(d => d.CampaignId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__CampaignU__campa__7C4F7684");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("PK__Categori__D54EE9B49EA37C79");

            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
        });

        modelBuilder.Entity<Comment>(entity =>
        {
            entity.HasKey(e => e.CommentId).HasName("PK__Comments__E79576871EC5E446");

            entity.Property(e => e.CommentId).HasColumnName("comment_id");
            entity.Property(e => e.CampaignId).HasColumnName("campaign_id");
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Campaign).WithMany(p => p.Comments)
                .HasForeignKey(d => d.CampaignId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Comments__campai__02084FDA");

            entity.HasOne(d => d.User).WithMany(p => p.Comments)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Comments__user_i__02FC7413");
        });

        modelBuilder.Entity<DisbursementRequest>(entity =>
        {
            entity.HasKey(e => e.RequestId).HasName("PK__Disburse__18D3B90F9635B099");

            entity.Property(e => e.RequestId).HasColumnName("request_id");
            entity.Property(e => e.AdminNote).HasColumnName("admin_note");
            entity.Property(e => e.Amount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("amount");
            entity.Property(e => e.ApprovedAt).HasColumnName("approved_at");
            entity.Property(e => e.ApprovedBy).HasColumnName("approved_by");
            entity.Property(e => e.CampaignId).HasColumnName("campaign_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created_at");
            entity.Property(e => e.ProofImages).HasColumnName("proof_images");
            entity.Property(e => e.Reason).HasColumnName("reason");
            entity.Property(e => e.RequesterId).HasColumnName("requester_id");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("pending")
                .HasColumnName("status");

            entity.HasOne(d => d.ApprovedByNavigation).WithMany(p => p.DisbursementRequestApprovedByNavigations)
                .HasForeignKey(d => d.ApprovedBy)
                .HasConstraintName("FK__Disbursem__appro__75A278F5");

            entity.HasOne(d => d.Campaign).WithMany(p => p.DisbursementRequests)
                .HasForeignKey(d => d.CampaignId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Disbursem__campa__73BA3083");

            entity.HasOne(d => d.Requester).WithMany(p => p.DisbursementRequestRequesters)
                .HasForeignKey(d => d.RequesterId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Disbursem__reque__74AE54BC");
        });

        modelBuilder.Entity<Donation>(entity =>
        {
            entity.HasKey(e => e.DonationId).HasName("PK__Donation__296B91DCCD5E19A9");

            entity.HasIndex(e => e.TransactionCode, "UQ__Donation__DD5740BE0B309887").IsUnique();

            entity.Property(e => e.DonationId).HasColumnName("donation_id");
            entity.Property(e => e.Amount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("amount");
            entity.Property(e => e.CampaignId).HasColumnName("campaign_id");
            entity.Property(e => e.DonatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("donated_at");
            entity.Property(e => e.IsAnonymous)
                .HasDefaultValue(false)
                .HasColumnName("is_anonymous");
            entity.Property(e => e.Message).HasColumnName("message");
            entity.Property(e => e.PaymentMethod)
                .HasMaxLength(50)
                .HasColumnName("payment_method");
            entity.Property(e => e.PaymentStatus)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("pending")
                .HasColumnName("payment_status");
            entity.Property(e => e.TransactionCode)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("transaction_code");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Campaign).WithMany(p => p.Donations)
                .HasForeignKey(d => d.CampaignId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Donations__campa__6754599E");

            entity.HasOne(d => d.User).WithMany(p => p.Donations)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__Donations__user___68487DD7");
        });

        modelBuilder.Entity<FinancialTransaction>(entity =>
        {
            entity.HasKey(e => e.TransactionId).HasName("PK__Financia__85C600AF6AC9400D");

            entity.Property(e => e.TransactionId).HasColumnName("transaction_id");
            entity.Property(e => e.Amount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("amount");
            entity.Property(e => e.CampaignId).HasColumnName("campaign_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created_at");
            entity.Property(e => e.Description)
                .HasMaxLength(255)
                .HasColumnName("description");
            entity.Property(e => e.ReferenceId).HasColumnName("reference_id");
            entity.Property(e => e.Type)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("type");

            entity.HasOne(d => d.Campaign).WithMany(p => p.FinancialTransactions)
                .HasForeignKey(d => d.CampaignId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Financial__campa__6D0D32F4");
        });

        modelBuilder.Entity<IdentityVerification>(entity =>
        {
            entity.HasKey(e => e.VerificationId).HasName("PK__Identity__24F179695192E1A4");

            entity.Property(e => e.VerificationId).HasColumnName("verification_id");
            entity.Property(e => e.AdminNote).HasColumnName("admin_note");
            entity.Property(e => e.DocumentType)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("document_type");
            entity.Property(e => e.DocumentUrlBack)
                .HasMaxLength(255)
                .HasColumnName("document_url_back");
            entity.Property(e => e.DocumentUrlFront)
                .HasMaxLength(255)
                .HasColumnName("document_url_front");
            entity.Property(e => e.ReviewedAt).HasColumnName("reviewed_at");
            entity.Property(e => e.ReviewedBy).HasColumnName("reviewed_by");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("pending")
                .HasColumnName("status");
            entity.Property(e => e.SubmittedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("submitted_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.ReviewedByNavigation).WithMany(p => p.IdentityVerificationReviewedByNavigations)
                .HasForeignKey(d => d.ReviewedBy)
                .HasConstraintName("FK__IdentityV__revie__49C3F6B7");

            entity.HasOne(d => d.User).WithMany(p => p.IdentityVerificationUsers)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__IdentityV__user___48CFD27E");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.NotificationId).HasName("PK__Notifica__E059842FEE5FC2AE");

            entity.Property(e => e.NotificationId).HasColumnName("notification_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created_at");
            entity.Property(e => e.IsRead)
                .HasDefaultValue(false)
                .HasColumnName("is_read");
            entity.Property(e => e.Message).HasColumnName("message");
            entity.Property(e => e.Title)
                .HasMaxLength(255)
                .HasColumnName("title");
            entity.Property(e => e.Type)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("system")
                .HasColumnName("type");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Notificat__user___160F4887");
        });

        modelBuilder.Entity<Report>(entity =>
        {
            entity.HasKey(e => e.ReportId).HasName("PK__Reports__779B7C587D6FD6D3");

            entity.Property(e => e.ReportId).HasColumnName("report_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created_at");
            entity.Property(e => e.Reason).HasColumnName("reason");
            entity.Property(e => e.ReporterId).HasColumnName("reporter_id");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("pending")
                .HasColumnName("status");
            entity.Property(e => e.TargetId).HasColumnName("target_id");
            entity.Property(e => e.TargetType)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("target_type");

            entity.HasOne(d => d.Reporter).WithMany(p => p.Reports)
                .HasForeignKey(d => d.ReporterId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Reports__reporte__09A971A2");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__B9BE370FEBC7925C");

            entity.ToTable(tb => tb.HasTrigger("trg_UpdateUsers"));

            entity.HasIndex(e => e.Email, "UQ__Users__AB6E6164AB6A66DB").IsUnique();

            entity.HasIndex(e => e.Username, "UQ__Users__F3DBC572004F04A3").IsUnique();

            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created_at");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .HasColumnName("email");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .HasColumnName("password_hash");
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(15)
                .IsUnicode(false)
                .HasColumnName("phone_number");
            entity.Property(e => e.Role)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("user")
                .HasColumnName("role");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("active")
                .HasColumnName("status");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("updated_at");
            entity.Property(e => e.Username)
                .HasMaxLength(50)
                .HasColumnName("username");
        });

        modelBuilder.Entity<UserProfile>(entity =>
        {
            entity.HasKey(e => e.ProfileId).HasName("PK__UserProf__AEBB701FC8F51AEF");

            entity.HasIndex(e => e.UserId, "UQ__UserProf__B9BE370E6FA35A69").IsUnique();

            entity.Property(e => e.ProfileId).HasColumnName("profile_id");
            entity.Property(e => e.Address).HasColumnName("address");
            entity.Property(e => e.AvatarUrl)
                .HasMaxLength(255)
                .HasColumnName("avatar_url");
            entity.Property(e => e.Bio).HasColumnName("bio");
            entity.Property(e => e.FullName)
                .HasMaxLength(100)
                .HasColumnName("full_name");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithOne(p => p.UserProfile)
                .HasForeignKey<UserProfile>(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__UserProfi__user___4222D4EF");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
