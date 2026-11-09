using System.Text.Json.Serialization;
using SmartSales.Contacts.Application.Contacts;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using SmartSales.Contacts.Api.Endpoints;
using SmartSales.Contacts.Domain.Entities;
using SmartSales.Contacts.Infrastructure.Persistence;
using SmartSales.Contacts.Infrastructure.Security;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 64 * 1024);
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

var connectionString = builder.Configuration.GetConnectionString("Contacts")
    ?? throw new InvalidOperationException("ConnectionStrings:Contacts is required.");
builder.Services.AddDbContext<ContactsDbContext>(options =>
    options.UseNpgsql(connectionString, npgsql => npgsql.SetPostgresVersion(17, 0)));

var jwt = JwtConfiguration.Load(builder.Configuration, requirePrivateKey: true);
builder.Services.AddSingleton(jwt);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddSingleton<DummyPasswordHash>();
builder.Services.AddScoped<TokenIssuer>();
builder.Services.AddScoped<ContactService>();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi("v1");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => JwtConfiguration.ConfigureValidation(options, jwt));
builder.Services.AddAuthorization();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("login", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
});

var app = builder.Build();
app.UseExceptionHandler();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/api/contacts/health/live", () => Results.Ok(new { status = "healthy" }));
app.MapGet("/api/contacts/health/ready", async (ContactsDbContext db, CancellationToken cancellationToken) =>
    await db.Database.CanConnectAsync(cancellationToken)
        ? Results.Ok(new { status = "ready" })
        : Results.StatusCode(StatusCodes.Status503ServiceUnavailable));
if (app.Environment.IsDevelopment())
    app.MapOpenApi("/api/contacts/openapi/{documentName}.json");

app.MapAuthEndpoints();
app.MapContactEndpoints();
app.MapReferenceEndpoints();
if (args.Contains("--seed-users", StringComparer.Ordinal))
{
    using var scope = app.Services.CreateScope();
    await UserSeeder.SeedAsync(scope.ServiceProvider, app.Configuration, CancellationToken.None);
    return;
}
if (args.Contains("--seed-demo", StringComparer.Ordinal))
{
    using var scope = app.Services.CreateScope();
    await DemoContactsSeeder.SeedAsync(scope.ServiceProvider, app.Configuration, CancellationToken.None);
    return;
}
app.Run();

public partial class Program;
