using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SmartSales.Sales.Domain.Entities;
using SmartSales.Sales.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace SmartSales.Sales.IntegrationTests;

public sealed class DashboardTests
{
    private static readonly Guid OwnerA = Guid.Parse("019a6011-0000-7000-8000-000000000031");
    private static readonly Guid OwnerB = Guid.Parse("019a6011-0000-7000-8000-000000000032");
    private static readonly Guid ContactId = Guid.Parse("019a6011-0000-7000-8000-000000000033");

    [Fact]
    public async Task Dashboard_uses_one_cohort_and_Bogota_creation_days()
    {
        await using var database = new PostgreSqlBuilder("postgres:17").Build();
        await database.StartAsync();
        using var rsa = RSA.Create(3072);
        var settings = new Dictionary<string, string?>
        {
            ["ConnectionStrings__Sales"] = database.GetConnectionString(),
            ["Jwt__PublicKeyPem"] = rsa.ExportRSAPublicKeyPem(),
            ["Jwt__KeyId"] = "integration-test",
            ["ContactsBaseUrl"] = "https://contacts.example.test"
        };
        var previous = settings.ToDictionary(pair => pair.Key,
            pair => Environment.GetEnvironmentVariable(pair.Key));
        try
        {
            foreach (var (name, value) in settings) Environment.SetEnvironmentVariable(name, value);
            using var factory = new WebApplicationFactory<Program>();
            using var client = factory.CreateClient();
            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<SalesDbContext>();
                await db.Database.MigrateAsync();
                db.Opportunities.AddRange(
                    Item("New", 100, OwnerA, "2026-11-08T05:00:00Z"),
                    Item("Contacted", 200, OwnerA, "2026-11-09T04:59:59Z"),
                    Item("Proposal", 300, OwnerA, "2026-11-09T05:00:00Z"),
                    Item("Won", 90, OwnerA, "2026-11-08T15:00:00Z"),
                    Item("Lost", 70, OwnerA, "2026-11-08T16:00:00Z"),
                    Item("New", 400, OwnerB, "2026-11-08T17:00:00Z"));
                await db.SaveChangesAsync();
            }
            Assert.Equal(HttpStatusCode.Unauthorized,
                (await client.GetAsync("/api/sales/v1/dashboard")).StatusCode);
            client.DefaultRequestHeaders.Authorization = Bearer(Issue(rsa));
            var all = await client.GetFromJsonAsync<DashboardResult>("/api/sales/v1/dashboard");
            Assert.NotNull(all);
            Assert.Equal((4, 1, 1, 1000m), (all.OpenCount, all.WonCount,
                all.LostCount, all.OpenEstimatedAmount));
            Assert.Equal("COP", all.Currency);
            Assert.Equal(5, all.ByStage.Length);
            var filtered = await client.GetFromJsonAsync<DashboardResult>(
                $"/api/sales/v1/dashboard?ownerUserId={OwnerA}&createdFrom=2026-11-08&createdTo=2026-11-08");
            Assert.NotNull(filtered);
            Assert.Equal((2, 1, 1, 300m), (filtered.OpenCount, filtered.WonCount,
                filtered.LostCount, filtered.OpenEstimatedAmount));
            Assert.Equal(new[] { 1, 1, 0, 1, 1 }, filtered.ByStage.Select(item => item.Count));
            Assert.Equal(new[] { "New", "Contacted", "Proposal", "Won", "Lost" },
                filtered.ByStage.Select(item => item.Stage));
            var empty = await client.GetFromJsonAsync<DashboardResult>(
                "/api/sales/v1/dashboard?createdFrom=2026-11-10&createdTo=2026-11-10");
            Assert.NotNull(empty);
            Assert.Equal(0, empty.OpenEstimatedAmount);
            Assert.All(empty.ByStage, item => Assert.Equal(0, item.Count));
            Assert.Equal(HttpStatusCode.BadRequest,
                (await client.GetAsync("/api/sales/v1/dashboard?createdFrom=2026-11-09&createdTo=2026-11-08"))
                .StatusCode);
            using (var scope = factory.Services.CreateScope())
            {
                var config = new ConfigurationBuilder().AddInMemoryCollection(
                    new Dictionary<string, string?> { ["Demo:Enabled"] = "true" }).Build();
                await DemoSalesSeeder.SeedAsync(scope.ServiceProvider, config, CancellationToken.None);
                await DemoSalesSeeder.SeedAsync(scope.ServiceProvider, config, CancellationToken.None);
                var db = scope.ServiceProvider.GetRequiredService<SalesDbContext>();
                Assert.Equal(14, await db.Opportunities.CountAsync());
                Assert.Equal(3, await db.FollowUps.CountAsync());
                Assert.Equal(3, await db.Interactions.CountAsync());
            }
        }
        finally
        {
            foreach (var (name, value) in previous) Environment.SetEnvironmentVariable(name, value);
        }
    }

    private static Opportunity Item(string stage, decimal amount, Guid owner, string created)
    {
        var timestamp = DateTimeOffset.Parse(created);
        return new Opportunity
        {
            Id = Guid.CreateVersion7(), ContactId = ContactId, Title = "Prueba", EstimatedAmount = amount,
            ExpectedCloseDate = new DateOnly(2026, 12, 1), OwnerUserId = owner,
            Priority = "Medium", Stage = stage, CreatedByUserId = owner,
            CreatedAt = timestamp, UpdatedAt = timestamp,
            ClosedAt = stage is "Won" or "Lost" ? timestamp : null,
            ClosedByUserId = stage is "Won" or "Lost" ? owner : null,
            FinalAmount = stage == "Won" ? amount : null,
            LossReason = stage == "Lost" ? "Sin presupuesto" : null,
            Version = 1
        };
    }

    private static AuthenticationHeaderValue Bearer(string token) => new("Bearer", token);
    private static string Issue(RSA rsa)
    {
        var token = new JwtSecurityToken("smartsales-contacts", "smartsales-api",
            [new Claim("sub", OwnerA.ToString()), new Claim("name", "Comercial"),
                new Claim("role", "Commercial")],
            notBefore: DateTime.UtcNow.AddMinutes(-1), expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: new SigningCredentials(new RsaSecurityKey(rsa) { KeyId = "integration-test" },
                SecurityAlgorithms.RsaSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
    private sealed record StageResult(string Stage, int Count);
    private sealed record DashboardResult(int OpenCount, int WonCount, int LostCount,
        decimal OpenEstimatedAmount, string Currency, StageResult[] ByStage);
}
