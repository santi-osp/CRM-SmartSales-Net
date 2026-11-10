using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using SmartSales.Sales.Domain.Entities;

namespace SmartSales.Sales.Infrastructure.Persistence;

public static class DemoSalesSeeder
{
    public static readonly Guid AdministratorId = Guid.Parse("019a6010-0000-7000-8000-000000000001");
    public static readonly Guid CommercialId = Guid.Parse("019a6010-0000-7000-8000-000000000002");
    public static readonly Guid Commercial2Id = Guid.Parse("019a6010-0000-7000-8000-000000000003");
    public static readonly Guid Commercial3Id = Guid.Parse("019a6010-0000-7000-8000-000000000004");
    public static readonly Guid[] ShowcaseOwnerIds = [CommercialId, Commercial2Id, Commercial3Id, AdministratorId];

    public static readonly Guid[] ContactIds = Enumerable.Range(1, 60)
        .Select(index => Guid.Parse($"019a6020-0000-7000-8000-{index:000000000000}")).ToArray();

    public static readonly Guid[] OpportunityIds = Enumerable.Range(1, 100)
        .Select(index => Guid.Parse($"019a6030-0000-7000-8000-{index:000000000000}")).ToArray();

    public static readonly TimeZoneInfo Bogota = TimeZoneInfo.FindSystemTimeZoneById("America/Bogota");

    private static readonly int[] OpenOpportunityIndices = [0, 1, 2, 5, 6, 7, .. Enumerable.Range(8, 69)];

    public static Opportunity[] GenerateOpportunities(bool isShowcase, DateTimeOffset now, DateOnly today)
    {
        var targetDate = today.AddDays(7);
        var baseRows = new (string Title, string Stage, decimal Amount, Guid Owner)[]
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

        var count = isShowcase ? 100 : 8;
        var list = new List<Opportunity>(count);

        for (var index = 0; index < 8; index++)
        {
            var row = baseRows[index];
            var closed = row.Stage is "Won" or "Lost";
            list.Add(new Opportunity
            {
                Id = OpportunityIds[index],
                ContactId = ContactIds[index],
                Title = row.Title,
                EstimatedAmount = row.Amount,
                ExpectedCloseDate = targetDate,
                OwnerUserId = row.Owner,
                Priority = index % 3 == 0 ? "High" : "Medium",
                Stage = row.Stage,
                CreatedByUserId = row.Owner,
                CreatedAt = now.AddHours(-8).AddMinutes(index),
                UpdatedAt = now,
                ClosedAt = closed ? now : null,
                ClosedByUserId = closed ? row.Owner : null,
                FinalAmount = row.Stage == "Won" ? row.Amount : null,
                LossReason = row.Stage == "Lost" ? "Presupuesto aplazado" : null,
                Version = 1
            });
        }

        if (!isShowcase) return list.ToArray();

        // Additional 92 opportunities: exactly 23 New, 23 Contacted, 23 Proposal, 14 Won, 9 Lost.
        for (var index = 8; index < 100; index++)
        {
            var stage = index < 31 ? "New" : index < 54 ? "Contacted" : index < 77 ? "Proposal" : index < 91 ? "Won" : "Lost";
            var closed = stage is "Won" or "Lost";
            var owner = ShowcaseOwnerIds[index % ShowcaseOwnerIds.Length];
            var amount = 1500000m + (index % 19) * 450000m;
            var contactId = ContactIds[(index * 7 + 3) % 60];
            var ageDays = index % 60;
            var titlePrefix = stage switch
            {
                "New" => "Reunión exploratoria",
                "Contacted" => "Presentación de alcance",
                "Proposal" => "Propuesta de valor",
                "Won" => "Acuerdo cerrado",
                _ => "Oportunidad declinada"
            };

            list.Add(new Opportunity
            {
                Id = OpportunityIds[index],
                ContactId = contactId,
                Title = $"{titlePrefix} #{index + 1:000}",
                EstimatedAmount = amount,
                ExpectedCloseDate = today.AddDays(3 + (index % 90)),
                OwnerUserId = owner,
                Priority = (index % 3) switch { 0 => "High", 1 => "Medium", _ => "Low" },
                Stage = stage,
                CreatedByUserId = owner,
                CreatedAt = now.AddDays(-ageDays).AddMinutes(-index),
                UpdatedAt = now,
                ClosedAt = closed ? now.AddDays(-(index % 5)) : null,
                ClosedByUserId = closed ? owner : null,
                FinalAmount = stage == "Won" ? amount : null,
                LossReason = stage == "Lost" ? "Presupuesto reasignado por el cliente" : null,
                Version = 1
            });
        }

        return list.ToArray();
    }

    public static FollowUp[] GenerateFollowUps(bool isShowcase, DateTimeOffset now, DateOnly today,
        IReadOnlyDictionary<Guid, Opportunity> opportunities)
    {
        var tomorrowUtc = BogotaMidnightUtc(today.AddDays(1));
        var baseRows = new (Guid Id, int ContactIndex, Guid? OpportunityId, string Subject, DateTimeOffset ScheduledAt)[]
        {
            (Guid.Parse("019a6040-0000-7000-8000-000000000001"), 0, OpportunityIds[0],
                "Retomar conversación", now.AddDays(-1)),
            (Guid.Parse("019a6040-0000-7000-8000-000000000002"), 1, OpportunityIds[1],
                "Confirmar necesidades", tomorrowUtc.AddSeconds(-1)),
            (Guid.Parse("019a6040-0000-7000-8000-000000000003"), 2, OpportunityIds[2],
                "Revisar propuesta", tomorrowUtc.AddHours(12))
        };

        var count = isShowcase ? 120 : 3;
        var list = new List<FollowUp>(count);

        foreach (var row in baseRows)
        {
            var oppId = row.OpportunityId;
            var contactId = ContactIds[row.ContactIndex];
            if (oppId is { } oid && opportunities.TryGetValue(oid, out var opp))
            {
                if (opp.Stage is "Won" or "Lost")
                {
                    oppId = null;
                }
                contactId = opp.ContactId;
            }

            list.Add(new FollowUp
            {
                Id = row.Id,
                ContactId = contactId,
                OpportunityId = oppId,
                Subject = row.Subject,
                ScheduledAt = row.ScheduledAt,
                OwnerUserId = CommercialId,
                Status = "Pending",
                CreatedAt = now,
                Version = 1
            });
        }

        if (!isShowcase) return list.ToArray();

        // 117 additional follow-ups: exactly 29 Overdue, 29 Today, 29 Upcoming, 30 Completed.
        for (var index = 3; index < 120; index++)
        {
            var id = Guid.Parse($"019a6040-0000-7000-8000-{index + 1:000000000000}");
            var group = (index - 3) / 29;
            var completed = group >= 3;
            var scheduled = group switch
            {
                0 => now.AddDays(-1 - (index % 10)),
                1 => now.AddTicks(Math.Max(1, (tomorrowUtc - now).Ticks / 2)),
                2 => tomorrowUtc.AddDays(1 + (index % 15)).AddHours(9),
                _ => now.AddDays(-2 - (index % 15))
            };

            // Link only to open opportunities (indices 0..74: New, Contacted, Proposal) or null
            Guid? oppId = null;
            Guid contactId;

            if (index % 5 == 0)
            {
                // Unlinked to opportunity, directly on contact
                contactId = ContactIds[(index * 7 + 1) % 60];
            }
            else
            {
                // For pending follow-ups, use a persisted open opportunity; never reopen a closed one.
                var oppIndex = completed
                    ? (index * 7) % 100
                    : OpenOpportunityIndices[(index * 7) % OpenOpportunityIndices.Length];
                oppId = OpportunityIds[oppIndex];
                if (opportunities.TryGetValue(oppId.Value, out var opp))
                {
                    if (!completed && opp.Stage is "Won" or "Lost")
                    {
                        // The selected opportunity was closed after an earlier seed run.
                        // Keep the follow-up linked only to its contact instead.
                        oppId = null;
                    }
                    contactId = opp.ContactId;
                }
                else
                {
                    contactId = oppIndex < 8 ? ContactIds[oppIndex] :
                        ContactIds[(oppIndex * 7 + 3) % 60];
                }
            }

            var owner = ShowcaseOwnerIds[index % ShowcaseOwnerIds.Length];

            list.Add(new FollowUp
            {
                Id = id,
                ContactId = contactId,
                OpportunityId = oppId,
                Subject = $"Seguimiento comercial #{index + 1:000}",
                ScheduledAt = scheduled,
                OwnerUserId = owner,
                Status = completed ? "Completed" : "Pending",
                CreatedAt = now.AddDays(-7),
                CompletedAt = completed ? now.AddHours(-1) : null,
                CompletedByUserId = completed ? owner : null,
                Version = 1
            });
        }

        return list.ToArray();
    }

    public static Interaction[] GenerateInteractions(bool isShowcase, DateTimeOffset now,
        IReadOnlyDictionary<Guid, Opportunity> opportunities)
    {
        var baseRows = new (Guid Id, int ContactIndex, Guid? OpportunityId, string Type, string Summary)[]
        {
            (Guid.Parse("019a6050-0000-7000-8000-000000000001"), 0, OpportunityIds[0],
                "Call", "Conversamos sobre el alcance inicial."),
            (Guid.Parse("019a6050-0000-7000-8000-000000000002"), 1, null,
                "Email", "Enviamos información del servicio."),
            (Guid.Parse("019a6050-0000-7000-8000-000000000003"), 2, OpportunityIds[2],
                "Meeting", "Revisamos la propuesta con el equipo.")
        };

        var count = isShowcase ? 150 : 3;
        var list = new List<Interaction>(count);

        for (var index = 0; index < 3; index++)
        {
            var row = baseRows[index];
            list.Add(new Interaction
            {
                Id = row.Id,
                ContactId = ContactIds[row.ContactIndex],
                OpportunityId = row.OpportunityId,
                Type = row.Type,
                Summary = row.Summary,
                OccurredAt = now.AddHours(-3).AddMinutes(index),
                AuthorUserId = CommercialId,
                CreatedAt = now
            });
        }

        if (!isShowcase) return list.ToArray();

        // 147 additional interactions: exactly 49 Call, 49 Email, 49 Meeting (total 50 of each).
        for (var index = 3; index < 150; index++)
        {
            var id = Guid.Parse($"019a6050-0000-7000-8000-{index + 1:000000000000}");
            var type = (index % 3) switch { 0 => "Call", 1 => "Email", _ => "Meeting" };
            Guid? oppId = null;
            Guid contactId;

            if (index % 4 == 0)
            {
                contactId = ContactIds[(index * 3) % 60];
            }
            else
            {
                var oppIndex = (index * 7 + 2) % 100;
                oppId = OpportunityIds[oppIndex];
                contactId = opportunities.TryGetValue(oppId.Value, out var opp)
                    ? opp.ContactId
                    : (oppIndex < 8 ? ContactIds[oppIndex] : ContactIds[(oppIndex * 7 + 3) % 60]);
            }

            var author = ShowcaseOwnerIds[index % ShowcaseOwnerIds.Length];
            var summary = type switch
            {
                "Call" => "Llamada telefónica para coordinar próximos pasos y despejar inquietudes.",
                "Email" => "Envío de documentación técnica, términos de servicio y cotización.",
                _ => "Reunión de demostración de producto y análisis de requerimientos."
            };

            list.Add(new Interaction
            {
                Id = id,
                ContactId = contactId,
                OpportunityId = oppId,
                Type = type,
                Summary = $"{summary} #{index + 1:000}",
                OccurredAt = now.AddDays(-(index % 45)).AddHours(-2),
                AuthorUserId = author,
                CreatedAt = now
            });
        }

        return list.ToArray();
    }

    public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration,
        CancellationToken cancellationToken)
    {
        var isShowcase = DemoSeedProfile.IsShowcase(configuration, services.GetService<IHostEnvironment>());
        var db = services.GetRequiredService<SalesDbContext>();
        var now = DemoSeedProfile.Reference(configuration, services.GetRequiredService<TimeProvider>());
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(now, Bogota).DateTime);

        // 1. Opportunities
        var opportunities = GenerateOpportunities(isShowcase, now, today);
        var existingOppIds = await db.Opportunities.AsNoTracking()
            .Select(o => o.Id).ToHashSetAsync(cancellationToken);

        foreach (var opp in opportunities)
        {
            if (!existingOppIds.Contains(opp.Id))
            {
                db.Opportunities.Add(opp);
            }
        }
        await db.SaveChangesAsync(cancellationToken);

        // Load all opportunities to resolve FKs accurately
        var allOpps = await db.Opportunities.AsNoTracking()
            .ToDictionaryAsync(o => o.Id, cancellationToken);

        // 2. Follow-ups
        var followUps = GenerateFollowUps(isShowcase, now, today, allOpps);
        var existingFollowUpIds = await db.FollowUps.AsNoTracking()
            .Select(f => f.Id).ToHashSetAsync(cancellationToken);

        foreach (var f in followUps)
        {
            if (!existingFollowUpIds.Contains(f.Id))
            {
                db.FollowUps.Add(f);
            }
        }
        await db.SaveChangesAsync(cancellationToken);

        // 3. Interactions
        var interactions = GenerateInteractions(isShowcase, now, allOpps);
        var existingInteractionIds = await db.Interactions.AsNoTracking()
            .Select(i => i.Id).ToHashSetAsync(cancellationToken);

        foreach (var i in interactions)
        {
            if (!existingInteractionIds.Contains(i.Id))
            {
                db.Interactions.Add(i);
            }
        }
        await db.SaveChangesAsync(cancellationToken);
    }

    public static DateTimeOffset BogotaMidnightUtc(DateOnly date)
    {
        var local = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        return new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(local, Bogota), TimeSpan.Zero);
    }
}
