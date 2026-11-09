using Microsoft.EntityFrameworkCore;
using SmartSales.Contacts.Domain.Entities;

namespace SmartSales.Contacts.Infrastructure.Persistence;

public sealed class ContactsDbContext(DbContextOptions<ContactsDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Contact> Contacts => Set<Contact>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users", table => table.HasCheckConstraint("ck_users_role", "role IN ('Administrator', 'Commercial')"));
            entity.HasKey(user => user.Id);
            entity.Property(user => user.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(user => user.DisplayName).HasColumnName("display_name").HasMaxLength(120).IsRequired();
            entity.Property(user => user.Email).HasColumnName("email").HasMaxLength(254).IsRequired();
            entity.Property(user => user.NormalizedEmail).HasColumnName("normalized_email").HasMaxLength(254).IsRequired();
            entity.HasIndex(user => user.NormalizedEmail).IsUnique();
            entity.Property(user => user.PasswordHash).HasColumnName("password_hash").IsRequired();
            entity.Property(user => user.Role).HasColumnName("role").HasMaxLength(20).IsRequired();
            entity.Property(user => user.IsActive).HasColumnName("is_active").IsRequired();
            entity.Property(user => user.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        });

        modelBuilder.Entity<Contact>(entity =>
        {
            entity.ToTable("contacts", table =>
            {
                table.HasCheckConstraint("ck_contacts_type", "type IN ('Prospect', 'Customer')");
                table.HasCheckConstraint("ck_contacts_source", "source IN ('Web', 'Referral', 'WhatsApp', 'Call', 'Other')");
                table.HasCheckConstraint("ck_contacts_channel", "normalized_email IS NOT NULL OR normalized_phone IS NOT NULL");
                table.HasCheckConstraint("ck_contacts_version", "version >= 1");
            });
            entity.HasKey(contact => contact.Id);
            entity.Property(contact => contact.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(contact => contact.Name).HasColumnName("name").HasMaxLength(160).IsRequired();
            entity.Property(contact => contact.Type).HasColumnName("type").HasMaxLength(20).IsRequired();
            entity.Property(contact => contact.Email).HasColumnName("email").HasMaxLength(254);
            entity.Property(contact => contact.NormalizedEmail).HasColumnName("normalized_email").HasMaxLength(254);
            entity.Property(contact => contact.Phone).HasColumnName("phone").HasMaxLength(40);
            entity.Property(contact => contact.NormalizedPhone).HasColumnName("normalized_phone").HasMaxLength(20);
            entity.Property(contact => contact.Source).HasColumnName("source").HasMaxLength(20).IsRequired();
            entity.Property(contact => contact.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone");
            entity.Property(contact => contact.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone");
            entity.Property(contact => contact.Version).HasColumnName("version").IsConcurrencyToken();
            entity.HasIndex(contact => contact.NormalizedEmail);
            entity.HasIndex(contact => contact.NormalizedPhone);
            entity.HasIndex(contact => new { contact.CreatedAt, contact.Id });
        });
    }
}
