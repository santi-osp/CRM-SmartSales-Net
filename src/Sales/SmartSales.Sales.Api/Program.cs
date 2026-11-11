using System.Text.Json.Serialization;
using SmartSales.Sales.Infrastructure.ContactsDirectory;
using SmartSales.Sales.Api.Endpoints;
using SmartSales.Sales.Application.Opportunities;
using SmartSales.Sales.Application.FollowUps;
using SmartSales.Sales.Application.Interactions;
using SmartSales.Sales.Application.Dashboard;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using SmartSales.Sales.Infrastructure.Persistence;
using SmartSales.Sales.Infrastructure.Security;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 64 * 1024);
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

var connectionString = builder.Configuration.GetConnectionString("Sales")
    ?? throw new InvalidOperationException("ConnectionStrings:Sales is required.");
builder.Services.AddDbContext<SalesDbContext>(options =>
    options.UseNpgsql(connectionString, npgsql => npgsql.SetPostgresVersion(17, 0)));
var jwt = JwtConfiguration.Load(builder.Configuration);
var contactsBaseUrl = builder.Configuration["ContactsBaseUrl"]
    ?? throw new InvalidOperationException("ContactsBaseUrl is required.");
if (!Uri.TryCreate(contactsBaseUrl, UriKind.Absolute, out var contactsUri) ||
    (contactsUri.Scheme != Uri.UriSchemeHttps && !builder.Environment.IsDevelopment()))
    throw new InvalidOperationException("ContactsBaseUrl must be an absolute HTTPS URL outside Development.");
builder.Services.AddSingleton(jwt);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddHttpClient<ContactsDirectoryClient>(client =>
{
    client.BaseAddress = contactsUri;
    client.Timeout = TimeSpan.FromSeconds(3);
});
builder.Services.AddScoped<OpportunityService>();
builder.Services.AddScoped<FollowUpService>();
builder.Services.AddScoped<InteractionService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi("v1");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => JwtConfiguration.ConfigureValidation(options, jwt));
builder.Services.AddAuthorization();

var app = builder.Build();
app.UseExceptionHandler();
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-SmartSales-Lab-Revision", "demo-02");
    await next();
});
app.UseAuthentication();
app.UseAuthorization();
app.MapGet("/api/sales/health/live", () => Results.Ok(new { status = "healthy" }));
app.MapGet("/api/sales/health/ready", async (SalesDbContext db, CancellationToken cancellationToken) =>
    await db.Database.CanConnectAsync(cancellationToken)
        ? Results.Ok(new { status = "ready" })
        : Results.StatusCode(StatusCodes.Status503ServiceUnavailable));
app.MapGet("/api/sales/health/version", () => Results.Ok(new { service = "sales", version = "1.1.0-lab", revision = "demo-02" }));
if (app.Environment.IsDevelopment())
    app.MapOpenApi("/api/sales/openapi/{documentName}.json");
app.MapOpportunityEndpoints();
app.MapFollowUpEndpoints();
app.MapInteractionEndpoints();
app.MapDashboardEndpoints();
if (args.Contains("--seed-demo", StringComparer.Ordinal))
{
    using var scope = app.Services.CreateScope();
    await DemoSalesSeeder.SeedAsync(scope.ServiceProvider, app.Configuration, CancellationToken.None);
    return;
}
app.Run();

public partial class Program;
