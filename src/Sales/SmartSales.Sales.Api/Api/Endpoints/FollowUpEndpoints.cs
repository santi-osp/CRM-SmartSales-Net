using SmartSales.Sales.Application.FollowUps;
using SmartSales.Sales.Infrastructure.ContactsDirectory;

namespace SmartSales.Sales.Api.Endpoints;

public static class FollowUpEndpoints
{
    public static void MapFollowUpEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/sales/v1/follow-ups").RequireAuthorization()
            .AddEndpointFilter(async (context, next) =>
            {
                context.HttpContext.Response.Headers.CacheControl = "no-store";
                return await next(context);
            });
        group.MapGet("/", ListAsync);
        group.MapGet("/{id:guid}", GetAsync);
        group.MapPost("/", CreateAsync);
        group.MapPost("/{id:guid}/complete", CompleteAsync);
    }

    private static async Task<IResult> ListAsync(string? bucket, string? scope, int? page, int? pageSize,
        HttpContext context, FollowUpService service, CancellationToken cancellationToken)
    {
        var actor = ActorContext.From(context);
        if (actor is null) return Results.Unauthorized();
        try
        {
            return Results.Ok(await service.ListAsync(bucket, scope, page ?? 1, pageSize ?? 20,
                actor, cancellationToken));
        }
        catch (FollowUpRequestException error) { return Problem(error); }
    }

    private static async Task<IResult> GetAsync(Guid id, HttpContext context, FollowUpService service,
        CancellationToken cancellationToken)
    {
        var actor = ActorContext.From(context);
        if (actor is null) return Results.Unauthorized();
        var item = await service.GetAsync(id, actor, cancellationToken);
        return item is null ? Results.NotFound() : Results.Ok(item);
    }

    private static async Task<IResult> CreateAsync(FollowUpCreate request, HttpContext context,
        FollowUpService service, CancellationToken cancellationToken)
    {
        var actor = ActorContext.From(context);
        if (actor is null) return Results.Unauthorized();
        try
        {
            var item = await service.CreateAsync(request, actor, cancellationToken);
            return Results.Created($"/api/sales/v1/follow-ups/{item.Id}", item);
        }
        catch (FollowUpRequestException error) { return Problem(error); }
        catch (DirectoryFailureException error) { return Problem(error); }
    }

    private static async Task<IResult> CompleteAsync(Guid id, FollowUpComplete request, HttpContext context,
        FollowUpService service, CancellationToken cancellationToken)
    {
        var actor = ActorContext.From(context);
        if (actor is null) return Results.Unauthorized();
        try
        {
            var item = await service.CompleteAsync(id, request, actor, cancellationToken);
            return item is null ? Results.NotFound() : Results.Ok(item);
        }
        catch (FollowUpRequestException error) { return Problem(error); }
    }

    private static IResult Problem(FollowUpRequestException error) => Results.Problem(
        statusCode: error.StatusCode, title: error.Message,
        extensions: new Dictionary<string, object?> { ["code"] = error.Code });

    private static IResult Problem(DirectoryFailureException error) => Results.Problem(
        statusCode: error.StatusCode, title: error.Message,
        extensions: new Dictionary<string, object?> { ["code"] = error.Code });
}
