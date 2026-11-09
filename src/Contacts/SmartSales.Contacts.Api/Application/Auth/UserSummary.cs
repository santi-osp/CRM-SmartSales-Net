using SmartSales.Contacts.Domain.Entities;

namespace SmartSales.Contacts.Application.Auth;

public sealed record UserSummary(Guid Id, string DisplayName, string Role, bool IsActive)
{
    public static UserSummary From(User user) => new(user.Id, user.DisplayName, user.Role, user.IsActive);
}
