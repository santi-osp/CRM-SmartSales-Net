using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using SmartSales.Sales.Infrastructure.ContactsDirectory;
using SmartSales.Sales.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace SmartSales.Sales.IntegrationTests;

public sealed class OpportunityTests
{
    private static readonly Guid ContactId = Guid.Parse("019a6011-0000-7000-8000-000000000001");
    private static readonly Guid CommercialId = Guid.Parse("019a6011-0000-7000-8000-000000000002");
    private static readonly Guid AdminId = Guid.Parse("019a6011-0000-7000-8000-000000000003");
    private static readonly Guid OtherId = Guid.Parse("019a6011-0000-7000-8000-000000000004");
    private static readonly Guid InactiveId = Guid.Parse("019a6011-0000-7000-8000-000000000005");
    private const string Base = "/api/sales/v1/opportunities";

    [Fact]
    public async Task Create_validate_reassign_filter_move_and_close()
    {
        await using var database = new PostgreSqlBuilder("postgres:17").Build();
        await database.StartAsync();
        using var rsa = RSA.Create(3072);
        using var environment = new TestEnvironment(new Dictionary<string, string?>
        {
            ["ConnectionStrings__Sales"] = database.GetConnectionString(),
            ["Jwt__PublicKeyPem"] = rsa.ExportRSAPublicKeyPem(),
            ["Jwt__KeyId"] = "integration-test",
            ["ContactsBaseUrl"] = "https://contacts.example.test"
        });
        var directory = new FakeDirectory();
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services => services.AddScoped(_ =>
                new ContactsDirectoryClient(new HttpClient(directory, disposeHandler: false)
                { BaseAddress = new Uri("https://contacts.example.test"), Timeout = TimeSpan.FromSeconds(3) }))));
        using var client = factory.CreateClient();
        using (var scope = factory.Services.CreateScope())
            await scope.ServiceProvider.GetRequiredService<SalesDbContext>().Database.MigrateAsync();

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(Base)).StatusCode);
        client.DefaultRequestHeaders.Authorization = Bearer(Issue(rsa, CommercialId, "Commercial"));

        var forbiddenOwner = await client.PostAsJsonAsync(Base,
            Create(ContactId, 100m, OtherId));
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenOwner.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await client.PostAsJsonAsync(Base, Create(ContactId, -1m, null))).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity,
            (await client.PostAsJsonAsync(Base, Create(Guid.NewGuid(), 100m, null))).StatusCode);

        var created = await client.PostAsJsonAsync(Base, Create(ContactId, 100m, null));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var opportunity = await created.Content.ReadFromJsonAsync<OpportunityResult>();
        Assert.NotNull(opportunity);
        Assert.Equal("New", opportunity.Stage);
        Assert.Equal("Medium", opportunity.Priority);
        Assert.Equal(CommercialId, opportunity.OwnerUserId);
        Assert.Equal(1, opportunity.Version);
        Assert.True(directory.SawBearer);

        var commercialReassign = await PatchAsync(client, $"{Base}/{opportunity.Id}/owner",
            new { ownerUserId = OtherId, version = 1 });
        Assert.Equal(HttpStatusCode.Forbidden, commercialReassign.StatusCode);

        client.DefaultRequestHeaders.Authorization = Bearer(Issue(rsa, AdminId, "Administrator"));
        var inactive = await PatchAsync(client, $"{Base}/{opportunity.Id}/owner",
            new { ownerUserId = InactiveId, version = 1 });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, inactive.StatusCode);
        var changed = await PatchAsync(client, $"{Base}/{opportunity.Id}/owner",
            new { ownerUserId = OtherId, version = 1 });
        Assert.Equal(HttpStatusCode.OK, changed.StatusCode);
        var assigned = await changed.Content.ReadFromJsonAsync<OpportunityResult>();
        Assert.NotNull(assigned);
        Assert.Equal(OtherId, assigned.OwnerUserId);
        Assert.Equal(2, assigned.Version);

        using var changes = await client.GetFromJsonAsync<JsonDocument>($"{Base}/{opportunity.Id}/owner-changes");
        Assert.NotNull(changes);
        Assert.Equal(1, changes.RootElement.GetProperty("totalCount").GetInt32());
        var audit = changes.RootElement.GetProperty("items")[0];
        Assert.Equal(AdminId, audit.GetProperty("changedByUserId").GetGuid());
        Assert.Equal(CommercialId, audit.GetProperty("previousOwnerUserId").GetGuid());

        using var filtered = await client.GetFromJsonAsync<JsonDocument>(
            $"{Base}?ownerUserId={OtherId}&priority=Medium");
        Assert.Equal(1, filtered!.RootElement.GetProperty("totalCount").GetInt32());
        using var excluded = await client.GetFromJsonAsync<JsonDocument>(
            $"{Base}?ownerUserId={CommercialId}&priority=High");
        Assert.Equal(0, excluded!.RootElement.GetProperty("totalCount").GetInt32());
        using var cleared = await client.GetFromJsonAsync<JsonDocument>(Base);
        Assert.Equal(1, cleared!.RootElement.GetProperty("totalCount").GetInt32());

        var moved = await PatchAsync(client, $"{Base}/{opportunity.Id}/stage",
            new { stage = "Contacted", version = 2 });
        Assert.Equal(HttpStatusCode.OK, moved.StatusCode);
        Assert.Equal(3, (await moved.Content.ReadFromJsonAsync<OpportunityResult>())?.Version);
        var staleMove = await PatchAsync(client, $"{Base}/{opportunity.Id}/stage",
            new { stage = "Proposal", version = 2 });
        Assert.Equal(HttpStatusCode.Conflict, staleMove.StatusCode);
        var movedBack = await PatchAsync(client, $"{Base}/{opportunity.Id}/stage",
            new { stage = "New", version = 3 });
        Assert.Equal(HttpStatusCode.OK, movedBack.StatusCode);
        using var contactStage = await client.GetFromJsonAsync<JsonDocument>($"{Base}?stage=Contacted");
        Assert.Equal(0, contactStage!.RootElement.GetProperty("totalCount").GetInt32());

        var invalidClose = await client.PostAsJsonAsync($"{Base}/{opportunity.Id}/close",
            new { result = "Lost", finalAmount = (decimal?)null, lossReason = "", version = 4 });
        Assert.Equal(HttpStatusCode.BadRequest, invalidClose.StatusCode);
        var won = await client.PostAsJsonAsync($"{Base}/{opportunity.Id}/close",
            new { result = "Won", finalAmount = (decimal?)120m, lossReason = (string?)null, version = 4 });
        Assert.Equal(HttpStatusCode.OK, won.StatusCode);
        var wonItem = await won.Content.ReadFromJsonAsync<OpportunityResult>();
        Assert.Equal("Won", wonItem?.Stage);
        Assert.Equal(AdminId, wonItem?.ClosedByUserId);
        Assert.NotNull(wonItem?.ClosedAt);
        Assert.Equal(HttpStatusCode.Conflict,
            (await client.PostAsJsonAsync($"{Base}/{opportunity.Id}/close",
                new { result = "Won", finalAmount = (decimal?)120m, lossReason = (string?)null, version = 5 })).StatusCode);

        var second = await client.PostAsJsonAsync(Base, Create(ContactId, 50m, OtherId));
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
        var secondItem = await second.Content.ReadFromJsonAsync<OpportunityResult>();
        var lost = await client.PostAsJsonAsync($"{Base}/{secondItem!.Id}/close",
            new { result = "Lost", finalAmount = (decimal?)null, lossReason = "No hubo presupuesto", version = 1 });
        Assert.Equal(HttpStatusCode.OK, lost.StatusCode);
        Assert.Equal("Lost", (await lost.Content.ReadFromJsonAsync<OpportunityResult>())?.Stage);
        using var active = await client.GetFromJsonAsync<JsonDocument>($"{Base}?openOnly=true");
        Assert.Equal(0, active!.RootElement.GetProperty("totalCount").GetInt32());

        directory.Outage = true;
        var unavailable = await client.PostAsJsonAsync(Base, Create(ContactId, 500m, null));
        Assert.Equal(HttpStatusCode.ServiceUnavailable, unavailable.StatusCode);
        using var check = factory.Services.CreateScope();
        var db = check.ServiceProvider.GetRequiredService<SalesDbContext>();
        Assert.Equal(2, await db.Opportunities.CountAsync());
        Assert.Equal(1, await db.OpportunityOwnerChanges.CountAsync());
    }

    private static object Create(Guid contactId, decimal amount, Guid? ownerId) => new
    {
        contactId, title = "Venta de prueba", estimatedAmount = amount,
        expectedCloseDate = "2026-12-01", ownerUserId = ownerId
    };

    private static AuthenticationHeaderValue Bearer(string token) => new("Bearer", token);

    private static string Issue(RSA rsa, Guid id, string role)
    {
        var token = new JwtSecurityToken("smartsales-contacts", "smartsales-api",
            [new Claim("sub", id.ToString()), new Claim("name", role), new Claim("role", role)],
            notBefore: DateTime.UtcNow.AddMinutes(-1), expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: new SigningCredentials(new RsaSecurityKey(rsa) { KeyId = "integration-test" },
                SecurityAlgorithms.RsaSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static async Task<HttpResponseMessage> PatchAsync(HttpClient client, string path, object body)
    {
        using var request = new HttpRequestMessage(HttpMethod.Patch, path)
        { Content = JsonContent.Create(body) };
        return await client.SendAsync(request);
    }

    private sealed record OpportunityResult(Guid Id, Guid ContactId, string Title, decimal EstimatedAmount,
        DateOnly ExpectedCloseDate, Guid OwnerUserId, string Priority, string Stage, Guid CreatedByUserId,
        DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, DateTimeOffset? ClosedAt, Guid? ClosedByUserId,
        decimal? FinalAmount, string? LossReason, long Version);

    private sealed class FakeDirectory : HttpMessageHandler
    {
        public bool Outage { get; set; }
        public bool SawBearer { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            SawBearer |= request.Headers.Authorization?.Scheme == "Bearer";
            if (Outage) return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
            var path = request.RequestUri?.AbsolutePath ?? "";
            HttpResponseMessage response;
            if (path.EndsWith($"/contacts/{ContactId}", StringComparison.Ordinal))
                response = new HttpResponseMessage(HttpStatusCode.OK)
                { Content = JsonContent.Create(new { id = ContactId, name = "Contacto", type = "Prospect" }) };
            else if (path.EndsWith($"/users/{InactiveId}", StringComparison.Ordinal))
                response = new HttpResponseMessage(HttpStatusCode.OK)
                { Content = JsonContent.Create(new { id = InactiveId, displayName = "Inactivo", role = "Commercial", isActive = false }) };
            else if (path.EndsWith($"/users/{CommercialId}", StringComparison.Ordinal) ||
                     path.EndsWith($"/users/{OtherId}", StringComparison.Ordinal))
            {
                var id = path.EndsWith($"/{CommercialId}", StringComparison.Ordinal) ? CommercialId : OtherId;
                response = new HttpResponseMessage(HttpStatusCode.OK)
                { Content = JsonContent.Create(new { id, displayName = "Vendedor", role = "Commercial", isActive = true }) };
            }
            else response = new HttpResponseMessage(HttpStatusCode.NotFound);
            return Task.FromResult(response);
        }
    }

    private sealed class TestEnvironment : IDisposable
    {
        private readonly Dictionary<string, string?> previous = new();
        public TestEnvironment(Dictionary<string, string?> settings)
        {
            foreach (var (name, value) in settings)
            {
                previous[name] = Environment.GetEnvironmentVariable(name);
                Environment.SetEnvironmentVariable(name, value);
            }
        }
        public void Dispose()
        {
            foreach (var (name, value) in previous)
                Environment.SetEnvironmentVariable(name, value);
        }
    }
}
