using Microsoft.AspNetCore.Identity;
using SmartSales.Contacts.Domain.Entities;

namespace SmartSales.Contacts.Infrastructure.Security;

public sealed class DummyPasswordHash
{
    public User User { get; } = new()
    {
        Id = Guid.Empty,
        DisplayName = "",
        Email = "",
        NormalizedEmail = "",
        PasswordHash = "",
        Role = "Commercial",
        IsActive = false,
        CreatedAt = DateTimeOffset.UnixEpoch
    };

    public string Hash { get; }

    public DummyPasswordHash()
    {
        Hash = new PasswordHasher<User>().HashPassword(User, Guid.NewGuid().ToString("N"));
    }
}
