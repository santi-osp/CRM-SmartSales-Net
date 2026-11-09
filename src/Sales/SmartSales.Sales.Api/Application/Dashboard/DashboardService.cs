using Microsoft.EntityFrameworkCore;
using SmartSales.Sales.Infrastructure.Persistence;

namespace SmartSales.Sales.Application.Dashboard;

public sealed class DashboardService(SalesDbContext db, TimeProvider clock)
{
    private static readonly TimeZoneInfo Bogota = TimeZoneInfo.FindSystemTimeZoneById("America/Bogota");
    private static readonly string[] Stages = ["New", "Contacted", "Proposal", "Won", "Lost"];

    public async Task<DashboardResponse> GetAsync(Guid? ownerUserId, DateOnly? createdFrom,
        DateOnly? createdTo, CancellationToken cancellationToken)
    {
        if (createdFrom > createdTo || createdTo == DateOnly.MaxValue)
            throw new DashboardRequestException("invalid_date_range",
                "La fecha inicial no puede ser posterior a la final.");
        var query = db.Opportunities.AsNoTracking();
        if (ownerUserId is not null) query = query.Where(item => item.OwnerUserId == ownerUserId);
        if (createdFrom is { } from)
        {
            var start = BogotaMidnightUtc(from);
            query = query.Where(item => item.CreatedAt >= start);
        }
        if (createdTo is { } to)
        {
            var end = BogotaMidnightUtc(to.AddDays(1));
            query = query.Where(item => item.CreatedAt < end);
        }
        // Un único SELECT agrupado evita mezclar tarjetas y gráfico de distintas instantáneas.
        var grouped = await query.GroupBy(item => item.Stage)
            .Select(group => new
            {
                Stage = group.Key,
                Count = group.Count(),
                OpenAmount = group.Sum(item => item.Stage == "Won" || item.Stage == "Lost"
                    ? 0m : item.EstimatedAmount)
            }).ToListAsync(cancellationToken);
        var counts = Stages.Select(stage => new StageCount(stage,
            grouped.SingleOrDefault(row => row.Stage == stage)?.Count ?? 0)).ToArray();
        var openCount = counts.Where(item => item.Stage is "New" or "Contacted" or "Proposal")
            .Sum(item => item.Count);
        return new DashboardResponse(openCount,
            counts.Single(item => item.Stage == "Won").Count,
            counts.Single(item => item.Stage == "Lost").Count,
            grouped.Sum(item => item.OpenAmount), "COP", counts,
            new DashboardFilters(ownerUserId, createdFrom, createdTo), clock.GetUtcNow());
    }

    private static DateTimeOffset BogotaMidnightUtc(DateOnly date)
    {
        var local = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        return new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(local, Bogota), TimeSpan.Zero);
    }
}
