using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SmartSales.Sales.Infrastructure.Persistence;

public sealed class SalesDbContextFactory : IDesignTimeDbContextFactory<SalesDbContext>
{
    public SalesDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Sales")
            ?? "Host=localhost;Database=sales_db;Username=sales_migrator";
        var options = new DbContextOptionsBuilder<SalesDbContext>()
            .UseNpgsql(connectionString, npgsql => npgsql.SetPostgresVersion(17, 0))
            .Options;
        return new SalesDbContext(options);
    }
}
