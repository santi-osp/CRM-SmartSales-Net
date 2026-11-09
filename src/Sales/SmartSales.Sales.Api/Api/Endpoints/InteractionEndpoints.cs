using SmartSales.Sales.Application.Interactions;
using SmartSales.Sales.Infrastructure.ContactsDirectory;

namespace SmartSales.Sales.Api.Endpoints;

public static class InteractionEndpoints
{
    public static void MapInteractionEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/sales/v1/interactions").RequireAuthorization()
            .AddEndpointFilter(async (context, next) =>
            {
                context.HttpContext.Response.Headers.CacheControl = "no-store";
                return await next(context);
            });
        group.MapGet("/", ListAsync);
        group.MapPost("/", CreateAsync);
    }

    private static async Task<IResult> ListAsync(Guid? contactId, Guid? opportunityId, int? page,
        int? pageSize, InteractionService service, CancellationToken cancellationToken)
    {
        try
        {
            return Results.Ok(await service.ListAsync(contactId, opportunityId, page ?? 1,
                pageSize ?? 20, cancellationToken));
        }
        catch (InteractionRequestException error) { return Problem(error); }
    }

    private static async Task<IResult> CreateAsync(InteractionCreate request, HttpContext context,
        InteractionService service, CancellationToken cancellationToken)
    {
        var actor = ActorContext.From(context);
        if (actor is null) return Results.Unauthorized();
        try
        {
            var item = await service.CreateAsync(request, actor, cancellationToken);
            return Results.Created($"/api/sales/v1/interactions?contactId={item.ContactId}", item);
        }
        catch (InteractionRequestException error) { return Problem(error); }
        catch (DirectoryFailureException error) { return Results.Problem(statusCode: error.StatusCode,
            title: error.Message, extensions: new Dictionary<string, object?> { ["code"] = error.Code }); }
    }

    private static IResult Problem(InteractionRequestException error) => Results.Problem(
        statusCode: error.StatusCode, title: error.Message,
        extensions: new Dictionary<string, object?> { ["code"] = error.Code });
}
