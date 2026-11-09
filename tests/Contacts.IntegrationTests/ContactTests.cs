using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using SmartSales.Contacts.Infrastructure.Persistence;

namespace SmartSales.Contacts.IntegrationTests;

[Collection("PostgreSQL integration")]
public sealed class ContactTests
{
    private const string Base = "/api/contacts/v1/contacts";

    [Fact]
    public async Task Demo_contacts_seed_twice_without_duplicates()
    {
        await using var host = await IntegrationHost.StartAsync();
        using var scope = host.Factory.Services.CreateScope();
        var config = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["Demo:Enabled"] = "true" }).Build();
        await DemoContactsSeeder.SeedAsync(scope.ServiceProvider, config, CancellationToken.None);
        await DemoContactsSeeder.SeedAsync(scope.ServiceProvider, config, CancellationToken.None);
        var db = scope.ServiceProvider.GetRequiredService<ContactsDbContext>();
        Assert.Equal(8, await db.Contacts.CountAsync());
        Assert.Equal(2, await db.Users.CountAsync());
        Assert.Equal(8, await db.Contacts.Select(item => item.NormalizedEmail).Distinct().CountAsync());
    }

    [Fact]
    public async Task Create_search_warn_duplicate_and_reject_stale_update()
    {
        await using var host = await IntegrationHost.StartAsync();
        var client = host.Client;
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(Base)).StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            await host.LoginCommercialAsync());

        var invalid = await client.PostAsJsonAsync(Base,
            new { name = "Sin medio", type = "Prospect", email = (string?)null,
                phone = (string?)null, source = "Web" });
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);

        var created = await client.PostAsJsonAsync(Base,
            new { name = "Ana López", type = "Prospect", email = "Ana@Example.Test",
                phone = "+57 300 123 4567", source = "Web" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var first = await created.Content.ReadFromJsonAsync<ContactResult>();
        Assert.NotNull(first);
        Assert.Equal(1, first.Version);
        Assert.Equal($"{Base}/{first.Id}", created.Headers.Location?.OriginalString);

        using var found = await client.GetFromJsonAsync<JsonDocument>($"{Base}?q=ANA&page=1");
        Assert.NotNull(found);
        Assert.Equal(1, found.RootElement.GetProperty("totalCount").GetInt32());
        Assert.Equal(first.Id, found.RootElement.GetProperty("items")[0].GetProperty("id").GetGuid());

        var duplicate = await client.PostAsJsonAsync(Base,
            new { name = "Otra Ana", type = "Customer", email = "ana@example.test",
                phone = (string?)null, source = "Referral" });
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        using var problem = JsonDocument.Parse(await duplicate.Content.ReadAsStringAsync());
        Assert.Equal("potential_duplicate", problem.RootElement.GetProperty("code").GetString());
        Assert.Equal(first.Id, problem.RootElement.GetProperty("duplicateContactIds")[0].GetGuid());

        var confirmed = await client.PostAsJsonAsync(Base,
            new { name = "Otra Ana", type = "Customer", email = "ana@example.test",
                phone = (string?)null, source = "Referral", acceptPotentialDuplicate = true });
        Assert.Equal(HttpStatusCode.Created, confirmed.StatusCode);

        var stale = await client.PutAsJsonAsync($"{Base}/{first.Id}",
            new { name = "Ana actualizada", type = "Customer", email = "Ana@Example.Test",
                phone = "+57 300 123 4567", source = "Call", version = 2 });
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        var updated = await client.PutAsJsonAsync($"{Base}/{first.Id}",
            new { name = "Ana actualizada", type = "Customer", email = "Ana@Example.Test",
                phone = "+57 300 123 4567", source = "Call", version = 1,
                acceptPotentialDuplicate = true });
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        var after = await updated.Content.ReadFromJsonAsync<ContactResult>();
        Assert.NotNull(after);
        Assert.Equal(first.Id, after.Id);
        Assert.Equal(2, after.Version);
        Assert.Equal("Ana actualizada", after.Name);

        using var scope = host.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ContactsDbContext>();
        Assert.Equal(2, await db.Contacts.CountAsync());
        var persisted = await db.Contacts.AsNoTracking().SingleAsync(contact => contact.Id == first.Id);
        Assert.Equal("Customer", persisted.Type);
        Assert.Equal("Call", persisted.Source);
        Assert.Equal(2, persisted.Version);
    }

    private sealed record ContactResult(Guid Id, string Name, string Type, string? Email, string? Phone,
        string Source, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, long Version);
}
