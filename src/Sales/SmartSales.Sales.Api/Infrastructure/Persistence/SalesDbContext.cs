using Microsoft.EntityFrameworkCore;
using SmartSales.Sales.Domain.Entities;

namespace SmartSales.Sales.Infrastructure.Persistence;

public sealed class SalesDbContext(DbContextOptions<SalesDbContext> options) : DbContext(options)
{
    public DbSet<Opportunity> Opportunities => Set<Opportunity>();
    public DbSet<OpportunityOwnerChange> OpportunityOwnerChanges => Set<OpportunityOwnerChange>();
    public DbSet<FollowUp> FollowUps => Set<FollowUp>();
    public DbSet<Interaction> Interactions => Set<Interaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Opportunity>(entity =>
        {
            entity.ToTable("opportunities", table =>
            {
                table.HasCheckConstraint("ck_opportunities_priority", "priority IN ('High', 'Medium', 'Low')");
                table.HasCheckConstraint("ck_opportunities_stage", "stage IN ('New', 'Contacted', 'Proposal', 'Won', 'Lost')");
                table.HasCheckConstraint("ck_opportunities_amount", "estimated_amount >= 0");
                table.HasCheckConstraint("ck_opportunities_version", "version >= 1");
                table.HasCheckConstraint("ck_opportunities_closed", "(stage IN ('New','Contacted','Proposal') AND closed_at IS NULL AND closed_by_user_id IS NULL AND final_amount IS NULL AND loss_reason IS NULL) OR (stage = 'Won' AND closed_at IS NOT NULL AND closed_by_user_id IS NOT NULL AND final_amount IS NOT NULL AND final_amount >= 0 AND loss_reason IS NULL) OR (stage = 'Lost' AND closed_at IS NOT NULL AND closed_by_user_id IS NOT NULL AND final_amount IS NULL AND loss_reason IS NOT NULL AND length(trim(loss_reason)) > 0)");
            });
            entity.HasKey(item => item.Id);
            entity.HasAlternateKey(item => new { item.Id, item.ContactId });
            entity.Property(item => item.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(item => item.ContactId).HasColumnName("contact_id");
            entity.Property(item => item.Title).HasColumnName("title").HasMaxLength(160).IsRequired();
            entity.Property(item => item.EstimatedAmount).HasColumnName("estimated_amount").HasColumnType("numeric(14,2)");
            entity.Property(item => item.ExpectedCloseDate).HasColumnName("expected_close_date").HasColumnType("date");
            entity.Property(item => item.OwnerUserId).HasColumnName("owner_user_id");
            entity.Property(item => item.Priority).HasColumnName("priority").HasMaxLength(10).IsRequired();
            entity.Property(item => item.Stage).HasColumnName("stage").HasMaxLength(20).IsRequired();
            entity.Property(item => item.CreatedByUserId).HasColumnName("created_by_user_id");
            entity.Property(item => item.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone");
            entity.Property(item => item.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone");
            entity.Property(item => item.ClosedAt).HasColumnName("closed_at").HasColumnType("timestamp with time zone");
            entity.Property(item => item.ClosedByUserId).HasColumnName("closed_by_user_id");
            entity.Property(item => item.FinalAmount).HasColumnName("final_amount").HasColumnType("numeric(14,2)");
            entity.Property(item => item.LossReason).HasColumnName("loss_reason").HasMaxLength(1000);
            entity.Property(item => item.Version).HasColumnName("version").IsConcurrencyToken();
            entity.HasIndex(item => new { item.Stage, item.CreatedAt, item.Id });
            entity.HasIndex(item => new { item.OwnerUserId, item.Priority, item.CreatedAt, item.Id });
            entity.HasIndex(item => new { item.ContactId, item.CreatedAt, item.Id });
        });

        modelBuilder.Entity<OpportunityOwnerChange>(entity =>
        {
            entity.ToTable("opportunity_owner_changes", table =>
                table.HasCheckConstraint("ck_owner_changed", "previous_owner_user_id <> new_owner_user_id"));
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(item => item.OpportunityId).HasColumnName("opportunity_id");
            entity.Property(item => item.PreviousOwnerUserId).HasColumnName("previous_owner_user_id");
            entity.Property(item => item.NewOwnerUserId).HasColumnName("new_owner_user_id");
            entity.Property(item => item.ChangedByUserId).HasColumnName("changed_by_user_id");
            entity.Property(item => item.ChangedAt).HasColumnName("changed_at").HasColumnType("timestamp with time zone");
            entity.HasOne<Opportunity>().WithMany().HasForeignKey(item => item.OpportunityId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(item => new { item.OpportunityId, item.ChangedAt, item.Id });
        });

        modelBuilder.Entity<FollowUp>(entity =>
        {
            entity.ToTable("follow_ups", table =>
            {
                table.HasCheckConstraint("ck_follow_ups_status", "status IN ('Pending', 'Completed')");
                table.HasCheckConstraint("ck_follow_ups_completion", "(status = 'Pending' AND completed_at IS NULL AND completed_by_user_id IS NULL) OR (status = 'Completed' AND completed_at IS NOT NULL AND completed_by_user_id IS NOT NULL)");
                table.HasCheckConstraint("ck_follow_ups_version", "version >= 1");
            });
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(item => item.ContactId).HasColumnName("contact_id");
            entity.Property(item => item.OpportunityId).HasColumnName("opportunity_id");
            entity.Property(item => item.Subject).HasColumnName("subject").HasMaxLength(160).IsRequired();
            entity.Property(item => item.ScheduledAt).HasColumnName("scheduled_at").HasColumnType("timestamp with time zone");
            entity.Property(item => item.OwnerUserId).HasColumnName("owner_user_id");
            entity.Property(item => item.Status).HasColumnName("status").HasMaxLength(20).IsRequired();
            entity.Property(item => item.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone");
            entity.Property(item => item.CompletedAt).HasColumnName("completed_at").HasColumnType("timestamp with time zone");
            entity.Property(item => item.CompletedByUserId).HasColumnName("completed_by_user_id");
            entity.Property(item => item.Version).HasColumnName("version").IsConcurrencyToken();
            entity.HasOne<Opportunity>().WithMany()
                .HasForeignKey(item => new { item.OpportunityId, item.ContactId })
                .HasPrincipalKey(item => new { item.Id, item.ContactId })
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(item => new { item.OwnerUserId, item.Status, item.ScheduledAt, item.Id });
            entity.HasIndex(item => new { item.ContactId, item.ScheduledAt, item.Id });
        });

        modelBuilder.Entity<Interaction>(entity =>
        {
            entity.ToTable("interactions", table =>
                table.HasCheckConstraint("ck_interactions_type", "type IN ('Call', 'Email', 'Meeting')"));
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(item => item.ContactId).HasColumnName("contact_id");
            entity.Property(item => item.OpportunityId).HasColumnName("opportunity_id");
            entity.Property(item => item.Type).HasColumnName("type").HasMaxLength(20).IsRequired();
            entity.Property(item => item.OccurredAt).HasColumnName("occurred_at").HasColumnType("timestamp with time zone");
            entity.Property(item => item.Summary).HasColumnName("summary").HasMaxLength(2000).IsRequired();
            entity.Property(item => item.AuthorUserId).HasColumnName("author_user_id");
            entity.Property(item => item.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone");
            entity.HasOne<Opportunity>().WithMany()
                .HasForeignKey(item => new { item.OpportunityId, item.ContactId })
                .HasPrincipalKey(item => new { item.Id, item.ContactId })
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(item => new { item.ContactId, item.OccurredAt, item.Id });
            entity.HasIndex(item => new { item.OpportunityId, item.OccurredAt, item.Id });
        });
    }
}
