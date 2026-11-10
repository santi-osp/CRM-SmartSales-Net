using System.Globalization;

namespace SmartSales.Contacts.Infrastructure.Persistence;

public static class DemoSeedProfile
{
    public static bool IsShowcase(IConfiguration configuration, IHostEnvironment environment)
    {
        if (!environment.IsDevelopment() && !environment.IsEnvironment("Demo"))
            throw new InvalidOperationException("Demonstration seeding requires Development or Demo environment.");
        if (!configuration.GetValue<bool>("Demo:Enabled"))
            throw new InvalidOperationException("Demo:Enabled must be true to seed demonstration data.");
        return (configuration["Demo:Profile"] ?? "Basic") switch
        {
            "Basic" => false,
            "Showcase" => true,
            _ => throw new InvalidOperationException("Demo:Profile must be Basic or Showcase.")
        };
    }

    public static DateTimeOffset Reference(IConfiguration configuration, TimeProvider clock)
    {
        var configured = configuration["Demo:ReferenceUtc"];
        if (string.IsNullOrWhiteSpace(configured)) return clock.GetUtcNow();
        if (!DateTimeOffset.TryParse(configured, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var value)
            || !configured.EndsWith('Z'))
            throw new InvalidOperationException("Demo:ReferenceUtc must be an ISO UTC instant ending in Z.");
        return value;
    }
}
