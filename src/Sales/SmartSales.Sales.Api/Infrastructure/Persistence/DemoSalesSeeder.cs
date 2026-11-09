using Microsoft.EntityFrameworkCore;
using SmartSales.Sales.Domain.Entities;

namespace SmartSales.Sales.Infrastructure.Persistence;

public static class DemoSalesSeeder
{
    private static readonly Guid AdministratorId = Guid.Parse("019a6010-0000-7000-8000-000000000001");
    private static readonly Guid CommercialId = Guid.Parse("019a6010-0000-7000-8000-000000000002");
    private static readonly Guid[] ContactIds = Enumerable.Range(1, 8)
        .Select(index => Guid.Parse($"019a6020-0000-7000-8000-{index:000000000000}")).ToArray();
    private static readonly Guid[] OpportunityIds = Enumerable.Range(1, 8)
        .Select(index => Guid.Parse($"019a6030-0000-7000-8000-{index:000000000000}")).ToArray();
    private static readonly TimeZoneInfo Bogota = TimeZoneInfo.FindSystemTimeZoneById("America/Bogota");

    public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration,
        CancellationToken cancellationToken)
    {
        if (!configuration.GetValue<bool>("Demo:Enabled"))
            throw new InvalidOperationException("Demo:Enabled must be true to seed demonstration data.");
        var db = services.GetRequiredService<SalesDbContext>();
        var now = services.GetRequiredService<TimeProvider>().GetUtcNow();
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(now, Bogota).DateTime);
        var targetDate = today.AddDays(7);
        var rows = new (string Title, string Stage, decimal Amount, Guid Owner)[]
        {
            ("Primera conversación", "New", 1800000, CommercialId),
            ("Presentación de producto", "Contacted", 3200000, CommercialId),
            ("Propuesta comercial", "Proposal", 5400000, AdministratorId),
            ("Acuerdo anual", "Won", 7200000, CommercialId),
            ("Proyecto pospuesto", "Lost", 2600000, AdministratorId),
            ("Nueva sede", "New", 4100000, CommercialId),
            ("Renovación de servicio", "Contacted", 3500000, AdministratorId),
            ("Expansión regional", "Proposal", 8900000, CommercialId)
        };
        for (var index = 0; index < rows.Length; index++)
        {
            var id = OpportunityIds[index];
            var existing = await db.Opportunities.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
            var row = rows[index];
            if (existing is not null)
            {
                if (existing.ContactId != ContactIds[index])
                    throw new InvalidOperationException("A demo opportunity has incompatible contact identity.");
                continue;
            }
            var closed = row.Stage is "Won" or "Lost";
            db.Opportunities.Add(new Opportunity
            {
                Id = id, ContactId = ContactIds[index], Title = row.Title,
                EstimatedAmount = row.Amount, ExpectedCloseDate = targetDate,
                OwnerUserId = row.Owner, Priority = index % 3 == 0 ? "High" : "Medium",
                Stage = row.Stage, CreatedByUserId = row.Owner,
                CreatedAt = now.AddHours(-8).AddMinutes(index), UpdatedAt = now,
                ClosedAt = closed ? now : null, ClosedByUserId = closed ? row.Owner : null,
                FinalAmount = row.Stage == "Won" ? row.Amount : null,
                LossReason = row.Stage == "Lost" ? "Presupuesto aplazado" : null,
                Version = 1
            });
        }

        var tomorrowUtc = BogotaMidnightUtc(today.AddDays(1));
        var followUps = new (Guid Id, int ContactIndex, Guid? OpportunityId, string Subject,
            DateTimeOffset ScheduledAt)[]
        {
            (Guid.Parse("019a6040-0000-7000-8000-000000000001"), 0, OpportunityIds[0],
                "Retomar conversación", now.AddDays(-1)),
            (Guid.Parse("019a6040-0000-7000-8000-000000000002"), 1, OpportunityIds[1],
                "Confirmar necesidades", tomorrowUtc.AddSeconds(-1)),
            (Guid.Parse("019a6040-0000-7000-8000-000000000003"), 2, OpportunityIds[2],
                "Revisar propuesta", tomorrowUtc.AddHours(12))
        };
        foreach (var row in followUps)
        {
            var existing = await db.FollowUps.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Id == row.Id, cancellationToken);
            if (existing is not null)
            {
                if (existing.ContactId != ContactIds[row.ContactIndex] ||
                    existing.OpportunityId != row.OpportunityId)
                    throw new InvalidOperationException("A demo follow-up has incompatible references.");
                continue;
            }
            db.FollowUps.Add(new FollowUp
            {
                Id = row.Id, ContactId = ContactIds[row.ContactIndex],
                OpportunityId = row.OpportunityId, Subject = row.Subject,
                ScheduledAt = row.ScheduledAt, OwnerUserId = CommercialId,
                Status = "Pending", CreatedAt = now, Version = 1
            });
        }

        var interactions = new (Guid Id, int ContactIndex, Guid? OpportunityId,
            string Type, string Summary)[]
        {
            (Guid.Parse("019a6050-0000-7000-8000-000000000001"), 0, OpportunityIds[0],
                "Call", "Conversamos sobre el alcance inicial."),
            (Guid.Parse("019a6050-0000-7000-8000-000000000002"), 1, null,
                "Email", "Enviamos información del servicio."),
            (Guid.Parse("019a6050-0000-7000-8000-000000000003"), 2, OpportunityIds[2],
                "Meeting", "Revisamos la propuesta con el equipo.")
        };
        for (var index = 0; index < interactions.Length; index++)
        {
            var row = interactions[index];
            var existing = await db.Interactions.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Id == row.Id, cancellationToken);
            if (existing is not null)
            {
                if (existing.ContactId != ContactIds[row.ContactIndex] ||
                    existing.OpportunityId != row.OpportunityId)
                    throw new InvalidOperationException("A demo interaction has incompatible references.");
                continue;
            }
            db.Interactions.Add(new Interaction
            {
                Id = row.Id, ContactId = ContactIds[row.ContactIndex],
                OpportunityId = row.OpportunityId, Type = row.Type, Summary = row.Summary,
                OccurredAt = now.AddHours(-3).AddMinutes(index),
                AuthorUserId = CommercialId, CreatedAt = now
            });
        }
        await db.SaveChangesAsync(cancellationToken);
    }

    private static DateTimeOffset BogotaMidnightUtc(DateOnly date)
    {
        var local = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        return new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(local, Bogota), TimeSpan.Zero);
    }
}
