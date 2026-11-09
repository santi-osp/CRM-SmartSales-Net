using SmartSales.Contacts.Application.Contacts;

namespace SmartSales.Contacts.Api.Endpoints;

public static class ContactEndpoints
{
    public static void MapContactEndpoints(this WebApplication app)
    {
        var contacts = app.MapGroup("/api/contacts/v1/contacts").RequireAuthorization()
            .AddEndpointFilter(async (context, next) =>
            {
                context.HttpContext.Response.Headers.CacheControl = "no-store";
                return await next(context);
            });
        contacts.MapGet("/", ListAsync);
        contacts.MapGet("/{id:guid}", GetAsync);
        contacts.MapPost("/", CreateAsync);
        contacts.MapPut("/{id:guid}", UpdateAsync);
    }

    private static async Task<IResult> ListAsync(string? q, string? type, int? page, int? pageSize,
        ContactService service, CancellationToken cancellationToken)
    {
        try
        {
            return Results.Ok(await service.ListAsync(q, type, page ?? 1, pageSize ?? 20, cancellationToken));
        }
        catch (ContactRequestException error) { return Problem(error); }
    }

    private static async Task<IResult> GetAsync(Guid id, ContactService service, CancellationToken cancellationToken)
    {
        var contact = await service.GetAsync(id, cancellationToken);
        return contact is null ? Results.NotFound() : Results.Ok(contact);
    }

    private static async Task<IResult> CreateAsync(CreateContactRequest request, ContactService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var contact = await service.CreateAsync(request, cancellationToken);
            return Results.Created($"/api/contacts/v1/contacts/{contact.Id}", contact);
        }
        catch (ContactRequestException error) { return Problem(error); }
    }

    private static async Task<IResult> UpdateAsync(Guid id, UpdateContactRequest request,
        ContactService service, CancellationToken cancellationToken)
    {
        try
        {
            var contact = await service.UpdateAsync(id, request, cancellationToken);
            return contact is null ? Results.NotFound() : Results.Ok(contact);
        }
        catch (ContactRequestException error) { return Problem(error); }
    }

    private static IResult Problem(ContactRequestException error)
    {
        var extensions = new Dictionary<string, object?> { ["code"] = error.Code };
        if (error.DuplicateContactIds is not null)
            extensions["duplicateContactIds"] = error.DuplicateContactIds;
        return Results.Problem(statusCode: error.StatusCode, title: error.Message, extensions: extensions);
    }
}
