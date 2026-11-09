using System.Net.Http.Json;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartSales.Contacts.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace SmartSales.Contacts.IntegrationTests;

[CollectionDefinition("PostgreSQL integration", DisableParallelization = true)]
public sealed class PostgreSqlIntegrationCollection;

public sealed class IntegrationHost : IAsyncDisposable
{
    public const string CommercialEmail = "commercial@example.test";
    public const string CommercialPassword = "Commercial-Test-Password-2026";

    private readonly PostgreSqlContainer database;
    private readonly RSA rsa;
    private readonly Dictionary<string, string?> previousEnvironment;

    public WebApplicationFactory<Program> Factory { get; }
    public HttpClient Client { get; }

    private IntegrationHost(PostgreSqlContainer database, RSA rsa, Dictionary<string, string?> previousEnvironment,
        WebApplicationFactory<Program> factory, HttpClient client)
    {
        this.database = database;
        this.rsa = rsa;
        this.previousEnvironment = previousEnvironment;
        Factory = factory;
        Client = client;
    }

    public static async Task<IntegrationHost> StartAsync()
    {
        var database = new PostgreSqlBuilder("postgres:17").Build();
        await database.StartAsync();
        var rsa = RSA.Create(3072);
        var settings = new Dictionary<string, string?>
        {
            ["ConnectionStrings__Contacts"] = database.GetConnectionString(),
            ["Jwt__PublicKeyPem"] = rsa.ExportRSAPublicKeyPem(),
            ["Jwt__PrivateKeyPem"] = rsa.ExportPkcs8PrivateKeyPem(),
            ["Jwt__KeyId"] = "integration-test",
            ["Seed__Administrator__Email"] = "admin@example.test",
            ["Seed__Administrator__DisplayName"] = "Admin Demo",
            ["Seed__Administrator__Password"] = "Admin-Test-Password-2026",
            ["Seed__Commercial__Email"] = CommercialEmail,
            ["Seed__Commercial__DisplayName"] = "Commercial Demo",
            ["Seed__Commercial__Password"] = CommercialPassword
        };
        var previous = settings.Keys.ToDictionary(key => key, Environment.GetEnvironmentVariable);
        foreach (var (name, value) in settings)
            Environment.SetEnvironmentVariable(name, value);
        try
        {
            var factory = new WebApplicationFactory<Program>();
            var client = factory.CreateClient();
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ContactsDbContext>();
            await db.Database.MigrateAsync();
            var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            await UserSeeder.SeedAsync(scope.ServiceProvider, config, CancellationToken.None);
            await UserSeeder.SeedAsync(scope.ServiceProvider, config, CancellationToken.None);
            return new IntegrationHost(database, rsa, previous, factory, client);
        }
        catch
        {
            foreach (var (name, value) in previous)
                Environment.SetEnvironmentVariable(name, value);
            rsa.Dispose();
            await database.DisposeAsync();
            throw;
        }
    }

    public async Task<string> LoginCommercialAsync()
    {
        var response = await Client.PostAsJsonAsync("/api/contacts/v1/auth/login",
            new { email = CommercialEmail, password = CommercialPassword });
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<LoginToken>();
        return result?.AccessToken ?? throw new InvalidOperationException("Login did not return a token.");
    }

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        Factory.Dispose();
        foreach (var (name, value) in previousEnvironment)
            Environment.SetEnvironmentVariable(name, value);
        rsa.Dispose();
        await database.DisposeAsync();
    }

    private sealed record LoginToken(string AccessToken);
}
