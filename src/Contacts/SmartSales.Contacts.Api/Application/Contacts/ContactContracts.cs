using SmartSales.Contacts.Domain.Entities;

namespace SmartSales.Contacts.Application.Contacts;

public sealed record ContactFields(string? Name, string? Type, string? Email, string? Phone, string? Source);
public sealed record CreateContactRequest(string? Name, string? Type, string? Email, string? Phone,
    string? Source, bool AcceptPotentialDuplicate = false);
public sealed record UpdateContactRequest(string? Name, string? Type, string? Email, string? Phone,
    string? Source, long? Version, bool AcceptPotentialDuplicate = false);
public sealed record ContactResponse(Guid Id, string Name, string Type, string? Email, string? Phone,
    string Source, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, long Version)
{
    public static ContactResponse From(Contact contact) => new(contact.Id, contact.Name, contact.Type,
        contact.Email, contact.Phone, contact.Source, contact.CreatedAt, contact.UpdatedAt, contact.Version);
}
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);

public sealed class ContactRequestException(int statusCode, string code, string title,
    IReadOnlyList<Guid>? duplicateContactIds = null) : Exception(title)
{
    public int StatusCode { get; } = statusCode;
    public string Code { get; } = code;
    public IReadOnlyList<Guid>? DuplicateContactIds { get; } = duplicateContactIds;
}
