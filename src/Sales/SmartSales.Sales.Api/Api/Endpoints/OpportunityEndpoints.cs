using SmartSales.Sales.Application.Opportunities;
using SmartSales.Sales.Infrastructure.ContactsDirectory;

namespace SmartSales.Sales.Api.Endpoints;

public static class OpportunityEndpoints
{
    public static void MapOpportunityEndpoints(this WebApplication app)
    {
        var opportunities = app.MapGroup("/api/sales/v1/opportunities").RequireAuthorization()
            .AddEndpointFilter(async (context, next) =>
            {
                context.HttpContext.Response.Headers.CacheControl = "no-store";
                return await next(context);
            });
        opportunities.MapGet("/", ListAsync);
        opportunities.MapGet("/{id:guid}", GetAsync);
        opportunities.MapPost("/", CreateAsync);
        opportunities.MapPut("/{id:guid}", UpdateAsync);
        opportunities.MapPatch("/{id:guid}/owner", ChangeOwnerAsync);
        opportunities.MapPatch("/{id:guid}/stage", ChangeStageAsync);
        opportunities.MapPost("/{id:guid}/close", CloseAsync);
        opportunities.MapGet("/{id:guid}/owner-changes", OwnerChangesAsync);
    }

    private static async Task<IResult> ListAsync(string? stage, bool? openOnly, Guid? ownerUserId,
        string? priority, Guid? contactId, DateOnly? createdFrom, DateOnly? createdTo,
        int? page, int? pageSize, OpportunityService service, CancellationToken cancellationToken)
    {
        try
        {
            var filters = new OpportunityFilters(stage, openOnly ?? false, ownerUserId, priority,
                contactId, createdFrom, createdTo, page ?? 1, pageSize ?? 20);
            return Results.Ok(await service.ListAsync(filters, cancellationToken));
        }
        catch (OpportunityRequestException error) { return Problem(error); }
    }

    private static async Task<IResult> GetAsync(Guid id, OpportunityService service, CancellationToken cancellationToken)
    {
        var item = await service.GetAsync(id, cancellationToken);
        return item is null ? Results.NotFound() : Results.Ok(item);
    }

    private static async Task<IResult> CreateAsync(OpportunityCreate request, HttpContext context,
        OpportunityService service, CancellationToken cancellationToken)
    {
        var actor = ActorContext.From(context);
        if (actor is null) return Results.Unauthorized();
        try
        {
            var item = await service.CreateAsync(request, actor, cancellationToken);
            return Results.Created($"/api/sales/v1/opportunities/{item.Id}", item);
        }
        catch (OpportunityRequestException error) { return Problem(error); }
        catch (DirectoryFailureException error) { return Problem(error); }
    }

    private static async Task<IResult> UpdateAsync(Guid id, OpportunityUpdate request,
        OpportunityService service, CancellationToken cancellationToken)
    {
        try
        {
            var item = await service.UpdateAsync(id, request, cancellationToken);
            return item is null ? Results.NotFound() : Results.Ok(item);
        }
        catch (OpportunityRequestException error) { return Problem(error); }
    }

    private static async Task<IResult> ChangeOwnerAsync(Guid id, OwnerChangeRequest request,
        HttpContext context, OpportunityService service, CancellationToken cancellationToken)
    {
        var actor = ActorContext.From(context);
        if (actor is null) return Results.Unauthorized();
        try
        {
            var item = await service.ChangeOwnerAsync(id, request, actor, cancellationToken);
            return item is null ? Results.NotFound() : Results.Ok(item);
        }
        catch (OpportunityRequestException error) { return Problem(error); }
        catch (DirectoryFailureException error) { return Problem(error); }
    }

    private static async Task<IResult> ChangeStageAsync(Guid id, StageChangeRequest request,
        OpportunityService service, CancellationToken cancellationToken)
    {
        try
        {
            var item = await service.ChangeStageAsync(id, request, cancellationToken);
            return item is null ? Results.NotFound() : Results.Ok(item);
        }
        catch (OpportunityRequestException error) { return Problem(error); }
    }

    private static async Task<IResult> CloseAsync(Guid id, OpportunityCloseRequest request,
        HttpContext context, OpportunityService service, CancellationToken cancellationToken)
    {
        var actor = ActorContext.From(context);
        if (actor is null) return Results.Unauthorized();
        try
        {
            var item = await service.CloseAsync(id, request, actor, cancellationToken);
            return item is null ? Results.NotFound() : Results.Ok(item);
        }
        catch (OpportunityRequestException error) { return Problem(error); }
    }

    private static async Task<IResult> OwnerChangesAsync(Guid id, int? page, int? pageSize,
        OpportunityService service, CancellationToken cancellationToken)
    {
        try
        {
            var items = await service.OwnerChangesAsync(id, page ?? 1, pageSize ?? 20, cancellationToken);
            return items is null ? Results.NotFound() : Results.Ok(items);
        }
        catch (OpportunityRequestException error) { return Problem(error); }
    }

    private static IResult Problem(OpportunityRequestException error) => Results.Problem(
        statusCode: error.StatusCode, title: error.Message,
        extensions: new Dictionary<string, object?> { ["code"] = error.Code });

    private static IResult Problem(DirectoryFailureException error) => Results.Problem(
        statusCode: error.StatusCode, title: error.Message,
        extensions: new Dictionary<string, object?> { ["code"] = error.Code });
}
