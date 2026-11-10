using Microsoft.Extensions.Configuration;
using SmartSales.Contacts.Infrastructure.Persistence;

namespace SmartSales.Contacts.IntegrationTests;

public sealed class DemoContactsSeederTests
{
    [Fact]
    public void Basic_catalog_contains_exact_eight_original_contacts()
    {
        var rows = DemoContactsSeeder.GetCatalog(isShowcase: false);
        Assert.Equal(8, rows.Length);
        Assert.Equal("Café Aurora", rows[0].Name);
        Assert.Equal("Lumen Co", rows[7].Name);
        Assert.Equal(8, rows.Select(r => r.Email.ToUpperInvariant()).Distinct().Count());
    }

    [Fact]
    public void Showcase_catalog_contains_exact_sixty_contacts_with_valid_constraints()
    {
        var rows = DemoContactsSeeder.GetCatalog(isShowcase: true);
        Assert.Equal(60, rows.Length);

        // First 8 must be exact originals
        Assert.Equal("Café Aurora", rows[0].Name);
        Assert.Equal("Lumen Co", rows[7].Name);

        // All 60 emails must be unique and in .example domain
        var emails = rows.Select(r => r.Email.ToUpperInvariant()).ToArray();
        Assert.Equal(60, emails.Distinct().Count());
        Assert.All(rows, r => Assert.EndsWith(".example", r.Email));

        // Valid types and sources
        var validTypes = new HashSet<string> { "Prospect", "Customer" };
        var validSources = new HashSet<string> { "Web", "Referral", "WhatsApp", "Call", "Other" };

        Assert.All(rows, r =>
        {
            Assert.Contains(r.Type, validTypes);
            Assert.Contains(r.Source, validSources);
            Assert.False(string.IsNullOrWhiteSpace(r.Name));
            Assert.True(r.Name.Length <= 160);
        });
    }

    [Fact]
    public void Seed_fails_when_demo_is_not_enabled()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["Demo:Enabled"] = "false" }).Build();

        var ex = Assert.ThrowsAsync<InvalidOperationException>(() =>
            DemoContactsSeeder.SeedAsync(null!, config, CancellationToken.None));
        Assert.NotNull(ex);
    }
}
