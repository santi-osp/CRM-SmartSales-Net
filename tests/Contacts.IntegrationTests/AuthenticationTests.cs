using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartSales.Contacts.Infrastructure.Persistence;

namespace SmartSales.Contacts.IntegrationTests;

[Collection("PostgreSQL integration")]
public sealed class AuthenticationTests
{
    [Fact]
    public async Task Login_uses_real_database_and_protects_identity()
    {
        await using var host = await IntegrationHost.StartAsync();
        var client = host.Client;
        using (var scope = host.Factory.Services.CreateScope())
            Assert.Equal(2, await scope.ServiceProvider.GetRequiredService<ContactsDbContext>().Users.CountAsync());

        var unknown = await client.PostAsJsonAsync("/api/contacts/v1/auth/login",
            new { email = "missing@example.test", password = "wrong" });
        var wrongPassword = await client.PostAsJsonAsync("/api/contacts/v1/auth/login",
            new { email = IntegrationHost.CommercialEmail, password = "wrong" });
        Assert.Equal(HttpStatusCode.Unauthorized, unknown.StatusCode);
        using var unknownProblem = JsonDocument.Parse(await unknown.Content.ReadAsStringAsync());
        using var wrongProblem = JsonDocument.Parse(await wrongPassword.Content.ReadAsStringAsync());
        Assert.Equal(unknownProblem.RootElement.GetProperty("title").GetString(),
            wrongProblem.RootElement.GetProperty("title").GetString());
        Assert.Equal("invalid_credentials", unknownProblem.RootElement.GetProperty("code").GetString());
        Assert.Equal("invalid_credentials", wrongProblem.RootElement.GetProperty("code").GetString());

        using (var scope = host.Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ContactsDbContext>();
            var admin = await db.Users.SingleAsync(user => user.Role == "Administrator");
            admin.IsActive = false;
            await db.SaveChangesAsync();
        }
        var inactive = await client.PostAsJsonAsync("/api/contacts/v1/auth/login",
            new { email = "admin@example.test", password = "Admin-Test-Password-2026" });
        Assert.Equal(HttpStatusCode.Unauthorized, inactive.StatusCode);

        var login = await client.PostAsJsonAsync("/api/contacts/v1/auth/login",
            new { email = "  COMMERCIAL@example.test ", password = IntegrationHost.CommercialPassword });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var result = await login.Content.ReadFromJsonAsync<LoginResult>();
        Assert.NotNull(result);
        Assert.Equal("Bearer", result.TokenType);
        Assert.Equal(900, result.ExpiresIn);
        Assert.Equal("Commercial", result.User.Role);

        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.GetAsync("/api/contacts/v1/auth/me")).StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
        var me = await client.GetFromJsonAsync<UserResult>("/api/contacts/v1/auth/me");
        Assert.Equal(result.User.Id, me?.Id);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.GetAsync("/api/contacts/v1/auth/me")).StatusCode);
    }

    private sealed record LoginResult(string AccessToken, string TokenType, int ExpiresIn, UserResult User);
    private sealed record UserResult(Guid Id, string DisplayName, string Role, bool IsActive);
}
