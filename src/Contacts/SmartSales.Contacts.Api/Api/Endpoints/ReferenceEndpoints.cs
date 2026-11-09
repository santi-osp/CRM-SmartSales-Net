using Microsoft.EntityFrameworkCore;
using SmartSales.Contacts.Application.Auth;
using SmartSales.Contacts.Infrastructure.Persistence;

namespace SmartSales.Contacts.Api.Endpoints;

public static class ReferenceEndpoints
{
    public static void MapReferenceEndpoints(this WebApplication app)
    {
        var references = app.MapGroup("/api/contacts/v1/references").RequireAuthorization();
        references.MapGet("/contacts/{id:guid}", ContactAsync);
        references.MapPost("/contacts/lookup", LookupContactsAsync);
        references.MapGet("/users", UsersAsync);
        references.MapGet("/users/{id:guid}", UserAsync);
    }

    private static async Task<IResult> ContactAsync(Guid id, ContactsDbContext db, CancellationToken cancellationToken)
    {
        var contact = await db.Contacts.AsNoTracking().Where(item => item.Id == id)
            .Select(item => new ContactReference(item.Id, item.Name, item.Type))
            .SingleOrDefaultAsync(cancellationToken);
        return contact is null ? Results.NotFound() : Results.Ok(contact);
    }

    private static async Task<IResult> LookupContactsAsync(ContactLookupRequest request,
        ContactsDbContext db, CancellationToken cancellationToken)
    {
        if (request.Ids is null || request.Ids.Length is < 1 or > 100 ||
            request.Ids.Distinct().Count() != request.Ids.Length)
            return Results.Problem(statusCode: 400, title: "Lista de contactos no válida.",
                extensions: new Dictionary<string, object?> { ["code"] = "invalid_lookup" });
        var items = await db.Contacts.AsNoTracking().Where(item => request.Ids.Contains(item.Id))
            .Select(item => new ContactReference(item.Id, item.Name, item.Type))
            .ToListAsync(cancellationToken);
        var found = items.Select(item => item.Id).ToHashSet();
        return Results.Ok(new ContactLookupResponse(items, request.Ids.Where(id => !found.Contains(id)).ToArray()));
    }

    private static async Task<IResult> UsersAsync(bool? activeOnly, ContactsDbContext db,
        CancellationToken cancellationToken)
    {
        var users = db.Users.AsNoTracking();
        if (activeOnly ?? true) users = users.Where(user => user.IsActive);
        var items = await users.OrderBy(user => user.DisplayName).ThenBy(user => user.Id)
            .Select(user => new UserSummary(user.Id, user.DisplayName, user.Role, user.IsActive))
            .ToListAsync(cancellationToken);
        return Results.Ok(new UserLookupResponse(items));
    }

    private static async Task<IResult> UserAsync(Guid id, ContactsDbContext db,
        CancellationToken cancellationToken)
    {
        var user = await db.Users.AsNoTracking().Where(item => item.Id == id)
            .Select(item => new UserSummary(item.Id, item.DisplayName, item.Role, item.IsActive))
            .SingleOrDefaultAsync(cancellationToken);
        return user is null ? Results.NotFound() : Results.Ok(user);
    }
}

public sealed record ContactReference(Guid Id, string Name, string Type);
public sealed record ContactLookupRequest(Guid[] Ids);
public sealed record ContactLookupResponse(IReadOnlyList<ContactReference> Items, Guid[] MissingIds);
public sealed record UserLookupResponse(IReadOnlyList<UserSummary> Items);
