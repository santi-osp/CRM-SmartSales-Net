using SmartSales.Sales.Application.Dashboard;

namespace SmartSales.Sales.Api.Endpoints;

public static class DashboardEndpoints
{
    public static void MapDashboardEndpoints(this WebApplication app)
    {
        app.MapGet("/api/sales/v1/dashboard", async (Guid? ownerUserId, DateOnly? createdFrom,
            DateOnly? createdTo, DashboardService service, CancellationToken cancellationToken) =>
        {
            try { return Results.Ok(await service.GetAsync(ownerUserId, createdFrom, createdTo, cancellationToken)); }
            catch (DashboardRequestException error)
            {
                return Results.Problem(statusCode: 400, title: error.Message,
                    extensions: new Dictionary<string, object?> { ["code"] = error.Code });
            }
        }).RequireAuthorization().AddEndpointFilter(async (context, next) =>
        {
            context.HttpContext.Response.Headers.CacheControl = "no-store";
            return await next(context);
        });
    }
}
