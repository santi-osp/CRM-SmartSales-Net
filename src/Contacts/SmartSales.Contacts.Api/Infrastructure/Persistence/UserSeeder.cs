using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SmartSales.Contacts.Domain.Entities;

namespace SmartSales.Contacts.Infrastructure.Persistence;

public static class UserSeeder
{
    public static readonly Guid AdministratorId = Guid.Parse("019a6010-0000-7000-8000-000000000001");
    public static readonly Guid CommercialId = Guid.Parse("019a6010-0000-7000-8000-000000000002");
    public static readonly Guid Commercial2Id = Guid.Parse("019a6010-0000-7000-8000-000000000003");
    public static readonly Guid Commercial3Id = Guid.Parse("019a6010-0000-7000-8000-000000000004");

    public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration,
        CancellationToken cancellationToken)
    {
        var isShowcase = (configuration["Demo:Profile"] ?? "Basic")
            .Equals("Showcase", StringComparison.OrdinalIgnoreCase);

        var list = new List<SeedDefinition>
        {
            Read(configuration, "Administrator", "Administrator", AdministratorId),
            Read(configuration, "Commercial", "Commercial", CommercialId)
        };

        if (isShowcase)
        {
            list.Add(Read(configuration, "Commercial2", "Commercial", Commercial2Id,
                defaultEmail: "comercial2@demo.local", defaultDisplayName: "Comercial 2 Demo",
                fallbackPasswordRole: "Commercial"));
            list.Add(Read(configuration, "Commercial3", "Commercial", Commercial3Id,
                defaultEmail: "comercial3@demo.local", defaultDisplayName: "Comercial 3 Demo",
                fallbackPasswordRole: "Commercial"));
        }

        var definitions = list.ToArray();
        if (definitions.Select(d => d.NormalizedEmail).Distinct().Count() != definitions.Length)
            throw new InvalidOperationException("Seed emails must be different.");

        var db = services.GetRequiredService<ContactsDbContext>();
        var hasher = services.GetRequiredService<IPasswordHasher<User>>();
        var clock = services.GetRequiredService<TimeProvider>();
        foreach (var definition in definitions)
        {
            var existing = await db.Users.SingleOrDefaultAsync(user => user.Id == definition.Id, cancellationToken);
            if (existing is not null)
            {
                if (existing.Role != definition.Role)
                    throw new InvalidOperationException("A seed account has incompatible persisted role.");
                continue;
            }

            var user = new User
            {
                Id = definition.Id,
                DisplayName = definition.DisplayName,
                Email = definition.Email,
                NormalizedEmail = definition.NormalizedEmail,
                PasswordHash = "",
                Role = definition.Role,
                IsActive = true,
                CreatedAt = clock.GetUtcNow()
            };
            user.PasswordHash = hasher.HashPassword(user, definition.Password);
            db.Users.Add(user);
        }
        await db.SaveChangesAsync(cancellationToken);
    }

    private static SeedDefinition Read(IConfiguration configuration, string configRole, string entityRole, Guid id,
        string? defaultEmail = null, string? defaultDisplayName = null, string? fallbackPasswordRole = null)
    {
        var path = $"Seed:{configRole}";
        var email = (configuration[$"{path}:Email"] ?? defaultEmail)?.Trim().Normalize();
        if (string.IsNullOrEmpty(email))
            throw new InvalidOperationException($"{path}:Email is required.");

        var displayName = (configuration[$"{path}:DisplayName"] ?? defaultDisplayName)?.Trim().Normalize();
        if (string.IsNullOrEmpty(displayName))
            throw new InvalidOperationException($"{path}:DisplayName is required.");

        var password = configuration[$"{path}:Password"];
        if (string.IsNullOrEmpty(password) && fallbackPasswordRole is not null)
            password = configuration[$"Seed:{fallbackPasswordRole}:Password"];
        if (string.IsNullOrEmpty(password))
            throw new InvalidOperationException($"{path}:Password is required.");

        if (!new EmailAddressAttribute().IsValid(email) || email.Length > 254 ||
            displayName.Length is < 1 or > 120 || password.Length < 14)
            throw new InvalidOperationException($"{path} has invalid email, name or password length.");

        return new SeedDefinition(id, entityRole, email, email.ToUpperInvariant(), displayName, password);
    }

    private sealed record SeedDefinition(Guid Id, string Role, string Email, string NormalizedEmail,
        string DisplayName, string Password);
}
