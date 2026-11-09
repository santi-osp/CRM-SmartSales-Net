using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SmartSales.Contacts.Domain.Entities;

namespace SmartSales.Contacts.Infrastructure.Persistence;

public static class UserSeeder
{
    private static readonly Guid AdministratorId = Guid.Parse("019a6010-0000-7000-8000-000000000001");
    private static readonly Guid CommercialId = Guid.Parse("019a6010-0000-7000-8000-000000000002");

    public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration,
        CancellationToken cancellationToken)
    {
        var definitions = new[]
        {
            Read(configuration, "Administrator", AdministratorId),
            Read(configuration, "Commercial", CommercialId)
        };
        if (definitions[0].NormalizedEmail == definitions[1].NormalizedEmail)
            throw new InvalidOperationException("Seed emails must be different.");

        var db = services.GetRequiredService<ContactsDbContext>();
        var hasher = services.GetRequiredService<IPasswordHasher<User>>();
        var clock = services.GetRequiredService<TimeProvider>();
        foreach (var definition in definitions)
        {
            var existing = await db.Users.SingleOrDefaultAsync(user => user.Id == definition.Id, cancellationToken);
            if (existing is not null)
            {
                if (existing.Role != definition.Role || existing.NormalizedEmail != definition.NormalizedEmail)
                    throw new InvalidOperationException("A seed account has incompatible persisted identity.");
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

    private static SeedDefinition Read(IConfiguration configuration, string role, Guid id)
    {
        var path = $"Seed:{role}";
        var email = configuration[$"{path}:Email"]?.Trim().Normalize()
            ?? throw new InvalidOperationException($"{path}:Email is required.");
        var displayName = configuration[$"{path}:DisplayName"]?.Trim().Normalize()
            ?? throw new InvalidOperationException($"{path}:DisplayName is required.");
        var password = configuration[$"{path}:Password"]
            ?? throw new InvalidOperationException($"{path}:Password is required.");
        if (!new EmailAddressAttribute().IsValid(email) || email.Length > 254 ||
            displayName.Length is < 1 or > 120 || password.Length < 14)
            throw new InvalidOperationException($"{path} has invalid email, name or password length.");
        return new SeedDefinition(id, role, email, email.ToUpperInvariant(), displayName, password);
    }

    private sealed record SeedDefinition(Guid Id, string Role, string Email, string NormalizedEmail,
        string DisplayName, string Password);
}
