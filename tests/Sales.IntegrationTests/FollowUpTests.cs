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
using SmartSales.Sales.Domain.Entities;
using SmartSales.Sales.Infrastructure.ContactsDirectory;
using SmartSales.Sales.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace SmartSales.Sales.IntegrationTests;

public sealed class FollowUpTests
{
    private const string Base = "/api/sales/v1/follow-ups";
    private static readonly Guid ContactId = Guid.Parse("019a6011-0000-7000-8000-000000000011");
    private static readonly Guid OtherContactId = Guid.Parse("019a6011-0000-7000-8000-000000000012");
    private static readonly Guid CommercialId = Guid.Parse("019a6011-0000-7000-8000-000000000013");
    private static readonly Guid OtherUserId = Guid.Parse("019a6011-0000-7000-8000-000000000014");
    private static readonly Guid AdminId = Guid.Parse("019a6011-0000-7000-8000-000000000015");
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-11-09T04:30:00Z");

    [Fact]
    public async Task Agenda_uses_Bogota_boundaries_owner_scope_and_idempotent_completion()
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
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<TimeProvider>(new FixedTimeProvider());
                services.AddScoped(_ => new ContactsDirectoryClient(new HttpClient(directory, false)
                { BaseAddress = new Uri("https://contacts.example.test") }));
            }));
        using var client = factory.CreateClient();
        Guid otherOpportunityId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SalesDbContext>();
            await db.Database.MigrateAsync();
            otherOpportunityId = Guid.CreateVersion7();
            db.Opportunities.Add(new Opportunity
            {
                Id = otherOpportunityId, ContactId = OtherContactId, Title = "Otro contacto",
                EstimatedAmount = 10, ExpectedCloseDate = new DateOnly(2026, 12, 1),
                OwnerUserId = OtherUserId, Priority = "Medium", Stage = "New",
                CreatedByUserId = OtherUserId, CreatedAt = Now, UpdatedAt = Now, Version = 1
            });
            await db.SaveChangesAsync();
        }
        client.DefaultRequestHeaders.Authorization = Bearer(Issue(rsa, CommercialId, "Commercial"));
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await factory.CreateClient().GetAsync($"{Base}?bucket=Today")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await client.GetAsync($"{Base}?bucket=Today&scope=All")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await client.PostAsJsonAsync(Base, Create(ContactId, null, "", Now))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await client.PostAsJsonAsync(Base, Create(ContactId, null, "Fecha", default(DateTimeOffset)))).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity,
            (await client.PostAsJsonAsync(Base, Create(ContactId, otherOpportunityId, "Relación", Now))).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity,
            (await client.PostAsJsonAsync(Base, Create(Guid.NewGuid(), null, "Contacto", Now))).StatusCode);

        var overdue = await Post(client, Create(ContactId, null, "Vencido", Now.AddSeconds(-1)));
        var today = await Post(client, Create(ContactId, null, "Hoy", Now));
        var upcoming = await Post(client, Create(ContactId, null, "Mañana", DateTimeOffset.Parse("2026-11-09T05:00:00Z")));
        Assert.Equal(CommercialId, overdue.OwnerUserId);
        Assert.Equal("Pending", today.Status);
        Assert.Equal(TimeSpan.Zero, today.ScheduledAt.Offset);
        Assert.Equal(1, await Count(client, "Overdue"));
        Assert.Equal(1, await Count(client, "Today"));
        Assert.Equal(1, await Count(client, "Upcoming"));
        Assert.Equal(0, await Count(client, "Completed"));

        client.DefaultRequestHeaders.Authorization = Bearer(Issue(rsa, OtherUserId, "Commercial"));
        Assert.Equal(0, await Count(client, "Today"));
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"{Base}/{today.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await client.PostAsJsonAsync($"{Base}/{today.Id}/complete", new { version = 1 })).StatusCode);

        client.DefaultRequestHeaders.Authorization = Bearer(Issue(rsa, AdminId, "Administrator"));
        Assert.Equal(1, await Count(client, "Today", "All"));
        Assert.Equal(0, await Count(client, "Today"));
        var completed = await client.PostAsJsonAsync($"{Base}/{today.Id}/complete", new { version = 1 });
        Assert.Equal(HttpStatusCode.OK, completed.StatusCode);
        var completedItem = await completed.Content.ReadFromJsonAsync<FollowUpResult>();
        Assert.Equal("Completed", completedItem?.Status);
        Assert.Equal(AdminId, completedItem?.CompletedByUserId);
        Assert.Equal(Now, completedItem?.CompletedAt);
        Assert.Equal(2, completedItem?.Version);
        var again = await client.PostAsJsonAsync($"{Base}/{today.Id}/complete", new { version = 1 });
        Assert.Equal(HttpStatusCode.OK, again.StatusCode);
        var againItem = await again.Content.ReadFromJsonAsync<FollowUpResult>();
        Assert.Equal(completedItem?.CompletedAt, againItem?.CompletedAt);
        Assert.Equal(completedItem?.CompletedByUserId, againItem?.CompletedByUserId);
        Assert.Equal(2, againItem?.Version);
        Assert.Equal(0, await Count(client, "Today", "All"));
        Assert.Equal(1, await Count(client, "Completed", "All"));
        Assert.Equal(3, await Count(client, "Overdue", "All") + await Count(client, "Upcoming", "All") +
            await Count(client, "Completed", "All"));
        using var scopeAfter = factory.Services.CreateScope();
        var persisted = await scopeAfter.ServiceProvider.GetRequiredService<SalesDbContext>()
            .FollowUps.AsNoTracking().SingleAsync(item => item.Id == today.Id);
        Assert.Equal(ContactId, persisted.ContactId);
        Assert.Equal(Now, persisted.CompletedAt);
        Assert.Equal(3, await scopeAfter.ServiceProvider.GetRequiredService<SalesDbContext>()
            .FollowUps.CountAsync());
    }

    [Fact]
    public async Task Interactions_validate_and_share_one_record_across_contact_and_opportunity()
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
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<TimeProvider>(new FixedTimeProvider());
                services.AddScoped(_ => new ContactsDirectoryClient(new HttpClient(directory, false)
                { BaseAddress = new Uri("https://contacts.example.test") }));
            }));
        using var client = factory.CreateClient();
        var ownId = Guid.CreateVersion7();
        var otherId = Guid.CreateVersion7();
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SalesDbContext>();
            await db.Database.MigrateAsync();
            foreach (var (id, contactId) in new[] { (ownId, ContactId), (otherId, OtherContactId) })
                db.Opportunities.Add(new Opportunity
                {
                    Id = id, ContactId = contactId, Title = "Oportunidad",
                    EstimatedAmount = 10, ExpectedCloseDate = new DateOnly(2026, 12, 1),
                    OwnerUserId = CommercialId, Priority = "Medium", Stage = "New",
                    CreatedByUserId = CommercialId, CreatedAt = Now, UpdatedAt = Now, Version = 1
                });
            await db.SaveChangesAsync();
        }
        client.DefaultRequestHeaders.Authorization = Bearer(Issue(rsa, CommercialId, "Commercial"));
        const string path = "/api/sales/v1/interactions";
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync(path)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await client.PostAsJsonAsync(path, Interaction(ContactId, ownId, "Call", Now.AddSeconds(1), "Futuro"))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await client.PostAsJsonAsync(path, Interaction(ContactId, ownId, "Call", Now, " "))).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity,
            (await client.PostAsJsonAsync(path, Interaction(ContactId, otherId, "Call", Now, "Equivocada"))).StatusCode);
        var response = await client.PostAsJsonAsync(path,
            Interaction(ContactId, ownId, "Meeting", Now.AddMinutes(-10), "  Visita  "));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var created = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var interactionId = created!.RootElement.GetProperty("id").GetGuid();
        Assert.Equal(CommercialId, created.RootElement.GetProperty("authorUserId").GetGuid());
        Assert.Equal("Visita", created.RootElement.GetProperty("summary").GetString());
        Assert.Equal(Now, created.RootElement.GetProperty("createdAt").GetDateTimeOffset());
        using var byContact = await client.GetFromJsonAsync<JsonDocument>($"{path}?contactId={ContactId}");
        using var byOpportunity = await client.GetFromJsonAsync<JsonDocument>($"{path}?opportunityId={ownId}");
        Assert.Equal(1, byContact!.RootElement.GetProperty("totalCount").GetInt32());
        Assert.Equal(interactionId, byContact.RootElement.GetProperty("items")[0].GetProperty("id").GetGuid());
        Assert.Equal(interactionId, byOpportunity!.RootElement.GetProperty("items")[0].GetProperty("id").GetGuid());
        directory.Outage = true;
        Assert.Equal(HttpStatusCode.ServiceUnavailable,
            (await client.PostAsJsonAsync(path,
                Interaction(ContactId, null, "Email", Now, "Sin directorio"))).StatusCode);
        using var check = factory.Services.CreateScope();
        Assert.Equal(1, await check.ServiceProvider.GetRequiredService<SalesDbContext>()
            .Interactions.CountAsync());
    }

    private static object Interaction(Guid contactId, Guid? opportunityId, string type,
        DateTimeOffset occurredAt, string summary) => new { contactId, opportunityId, type, occurredAt, summary };

    private static object Create(Guid contactId, Guid? opportunityId, string subject, DateTimeOffset scheduledAt) =>
        new { contactId, opportunityId, subject, scheduledAt };

    private static async Task<FollowUpResult> Post(HttpClient client, object body)
    {
        var response = await client.PostAsJsonAsync(Base, body);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<FollowUpResult>())!;
    }

    private static async Task<int> Count(HttpClient client, string bucket, string scope = "Mine")
    {
        using var doc = await client.GetFromJsonAsync<JsonDocument>($"{Base}?bucket={bucket}&scope={scope}");
        return doc!.RootElement.GetProperty("totalCount").GetInt32();
    }

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

    private sealed record FollowUpResult(Guid Id, Guid ContactId, Guid? OpportunityId, string Subject,
        DateTimeOffset ScheduledAt, Guid OwnerUserId, string Status, DateTimeOffset CreatedAt,
        DateTimeOffset? CompletedAt, Guid? CompletedByUserId, long Version);

    private sealed class FixedTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private sealed class FakeDirectory : HttpMessageHandler
    {
        public bool Outage { get; set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (Outage) return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
            var id = request.RequestUri?.AbsolutePath.Split('/').LastOrDefault();
            return Task.FromResult(id == ContactId.ToString() || id == OtherContactId.ToString()
                ? new HttpResponseMessage(HttpStatusCode.OK)
                { Content = JsonContent.Create(new { id, name = "Contacto", type = "Prospect" }) }
                : new HttpResponseMessage(HttpStatusCode.NotFound));
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
