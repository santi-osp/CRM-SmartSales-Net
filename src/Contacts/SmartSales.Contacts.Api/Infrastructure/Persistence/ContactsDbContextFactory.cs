using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SmartSales.Contacts.Infrastructure.Persistence;

public sealed class ContactsDbContextFactory : IDesignTimeDbContextFactory<ContactsDbContext>
{
    public ContactsDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Contacts")
            ?? "Host=localhost;Database=contacts_db;Username=contacts_migrator";
        var options = new DbContextOptionsBuilder<ContactsDbContext>()
            .UseNpgsql(connectionString, npgsql => npgsql.SetPostgresVersion(17, 0))
            .Options;
        return new ContactsDbContext(options);
    }
}
