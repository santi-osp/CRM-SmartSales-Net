using Microsoft.Extensions.Configuration;
using SmartSales.Contacts.Infrastructure.Persistence;

namespace SmartSales.Contacts.IntegrationTests;

public sealed class UserSeederTests
{
    [Fact]
    public void User_seeder_has_four_stable_guid_identities()
    {
        Assert.Equal(Guid.Parse("019a6010-0000-7000-8000-000000000001"), UserSeeder.AdministratorId);
        Assert.Equal(Guid.Parse("019a6010-0000-7000-8000-000000000002"), UserSeeder.CommercialId);
        Assert.Equal(Guid.Parse("019a6010-0000-7000-8000-000000000003"), UserSeeder.Commercial2Id);
        Assert.Equal(Guid.Parse("019a6010-0000-7000-8000-000000000004"), UserSeeder.Commercial3Id);
    }

    [Fact]
    public async Task Read_fails_when_password_is_shorter_than_fourteen_characters()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["Seed:Administrator:Email"] = "admin@example.test",
                ["Seed:Administrator:DisplayName"] = "Admin",
                ["Seed:Administrator:Password"] = "short-pass", // < 14 chars
                ["Seed:Commercial:Email"] = "commercial@example.test",
                ["Seed:Commercial:DisplayName"] = "Commercial",
                ["Seed:Commercial:Password"] = "Commercial-Long-Password-2026"
            }).Build();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            UserSeeder.SeedAsync(null!, config, CancellationToken.None));
    }
}
