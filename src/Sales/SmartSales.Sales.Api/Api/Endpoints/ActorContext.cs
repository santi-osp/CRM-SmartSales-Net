using System.Net.Http.Headers;
using System.Security.Claims;
using SmartSales.Sales.Application.Opportunities;

namespace SmartSales.Sales.Api.Endpoints;

internal static class ActorContext
{
    public static Actor? From(HttpContext context)
    {
        if (!Guid.TryParse(context.User.FindFirstValue("sub"), out var id)) return null;
        var role = context.User.FindFirstValue("role");
        if (role is not ("Administrator" or "Commercial")) return null;
        if (!AuthenticationHeaderValue.TryParse(context.Request.Headers.Authorization, out var auth) ||
            auth.Scheme != "Bearer" || string.IsNullOrWhiteSpace(auth.Parameter)) return null;
        return new Actor(id, role, auth.Parameter);
    }
}
