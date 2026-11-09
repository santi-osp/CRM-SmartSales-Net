using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SmartSales.Contacts.Domain.Entities;
using SmartSales.Contacts.Application.Auth;
using SmartSales.Contacts.Infrastructure.Persistence;
using SmartSales.Contacts.Infrastructure.Security;

namespace SmartSales.Contacts.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var auth = app.MapGroup("/api/contacts/v1/auth")
            .AddEndpointFilter(async (context, next) =>
            {
                context.HttpContext.Response.Headers.CacheControl = "no-store";
                return await next(context);
            });

        auth.MapPost("/login", LoginAsync).RequireRateLimiting("login");
        auth.MapGet("/me", MeAsync).RequireAuthorization();
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        ContactsDbContext db,
        IPasswordHasher<User> hasher,
        DummyPasswordHash dummyHash,
        TokenIssuer issuer,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrEmpty(request.Password) ||
            request.Email.Length > 254 || request.Password.Length > 1024)
            return Unauthorized();

        var normalizedEmail = request.Email.Trim().Normalize().ToUpperInvariant();
        var user = await db.Users.AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.NormalizedEmail == normalizedEmail, cancellationToken);
        var result = hasher.VerifyHashedPassword(user ?? dummyHash.User,
            user?.PasswordHash ?? dummyHash.Hash, request.Password);
        if (user is null || !user.IsActive || result == PasswordVerificationResult.Failed)
            return Unauthorized();

        return Results.Ok(new LoginResponse(issuer.Issue(user), "Bearer", 900, UserSummary.From(user)));
    }

    private static async Task<IResult> MeAsync(
        ClaimsPrincipal principal, ContactsDbContext db, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(principal.FindFirstValue("sub"), out var userId))
            return Unauthorized();
        var user = await db.Users.AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == userId && candidate.IsActive, cancellationToken);
        return user is null ? Unauthorized() : Results.Ok(UserSummary.From(user));
    }

    private static IResult Unauthorized() => Results.Problem(
        statusCode: StatusCodes.Status401Unauthorized,
        title: "Credenciales no válidas.",
        extensions: new Dictionary<string, object?> { ["code"] = "invalid_credentials" });
}

public sealed record LoginRequest(string Email, string Password);
public sealed record LoginResponse(string AccessToken, string TokenType, int ExpiresIn, UserSummary User);
