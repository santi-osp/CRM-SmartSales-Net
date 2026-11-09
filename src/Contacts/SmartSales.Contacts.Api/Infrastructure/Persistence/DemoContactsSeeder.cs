using Microsoft.EntityFrameworkCore;
using SmartSales.Contacts.Domain.Entities;

namespace SmartSales.Contacts.Infrastructure.Persistence;

public static class DemoContactsSeeder
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration,
        CancellationToken cancellationToken)
    {
        if (!configuration.GetValue<bool>("Demo:Enabled"))
            throw new InvalidOperationException("Demo:Enabled must be true to seed demonstration data.");
        var db = services.GetRequiredService<ContactsDbContext>();
        var now = services.GetRequiredService<TimeProvider>().GetUtcNow();
        var rows = new (string Name, string Type, string Email, string Source)[]
        {
            ("Café Aurora", "Customer", "aurora@demo.example", "Referral"),
            ("Montaña Clara", "Prospect", "montana@demo.example", "Web"),
            ("Taller Andino", "Prospect", "taller@demo.example", "Call"),
            ("Casa Nube", "Customer", "nube@demo.example", "Referral"),
            ("Boreal Studio", "Prospect", "boreal@demo.example", "Web"),
            ("Verde Lienzo", "Customer", "verde@demo.example", "Other"),
            ("Ruta Nova", "Prospect", "ruta@demo.example", "Web"),
            ("Lumen Co", "Customer", "lumen@demo.example", "Call")
        };
        for (var index = 0; index < rows.Length; index++)
        {
            var id = Guid.Parse($"019a6020-0000-7000-8000-{index + 1:000000000000}");
            var existing = await db.Contacts.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
            var row = rows[index];
            if (existing is not null)
            {
                if (existing.NormalizedEmail != row.Email.ToUpperInvariant())
                    throw new InvalidOperationException("A demo contact has incompatible persisted identity.");
                continue;
            }
            db.Contacts.Add(new Contact
            {
                Id = id, Name = row.Name, Type = row.Type, Email = row.Email,
                NormalizedEmail = row.Email.ToUpperInvariant(), Phone = null, NormalizedPhone = null,
                Source = row.Source, CreatedAt = now.AddMinutes(-rows.Length + index),
                UpdatedAt = now, Version = 1
            });
        }
        await db.SaveChangesAsync(cancellationToken);
    }
}
