using System.Text;
using Microsoft.EntityFrameworkCore;
using SmartSales.Sales.Application.Opportunities;
using SmartSales.Sales.Domain.Entities;
using SmartSales.Sales.Infrastructure.ContactsDirectory;
using SmartSales.Sales.Infrastructure.Persistence;

namespace SmartSales.Sales.Application.FollowUps;

public sealed class FollowUpService(SalesDbContext db, ContactsDirectoryClient directory, TimeProvider clock)
{
    private static readonly TimeZoneInfo Bogota = TimeZoneInfo.FindSystemTimeZoneById("America/Bogota");

    public async Task<FollowUpResponse> CreateAsync(FollowUpCreate request, Actor actor,
        CancellationToken cancellationToken)
    {
        var subject = request.Subject?.Trim().Normalize(NormalizationForm.FormC);
        if (subject is null or { Length: 0 or > 160 })
            throw Invalid("invalid_subject", "Asunto obligatorio de máximo 160 caracteres.");
        if (request.ContactId == Guid.Empty)
            throw Invalid("invalid_contact", "Selecciona un contacto.");
        if (request.OpportunityId == Guid.Empty)
            throw Invalid("invalid_opportunity", "Oportunidad no válida.");
        if (request.ScheduledAt == default || request.ScheduledAt.Year is < 2000 or > 2100)
            throw Invalid("invalid_scheduled_at", "Indica una fecha y hora válidas.");
        if (await directory.GetContactAsync(request.ContactId, actor.Bearer, cancellationToken) is null)
            throw new FollowUpRequestException(422, "invalid_contact", "El contacto no existe.");
        if (request.OpportunityId is { } opportunityId &&
            !await db.Opportunities.AsNoTracking().AnyAsync(item =>
                item.Id == opportunityId && item.ContactId == request.ContactId, cancellationToken))
            throw new FollowUpRequestException(422, "invalid_opportunity",
                "La oportunidad no pertenece al contacto seleccionado.");

        var item = new FollowUp
        {
            Id = Guid.CreateVersion7(), ContactId = request.ContactId,
            OpportunityId = request.OpportunityId, Subject = subject,
            ScheduledAt = request.ScheduledAt.ToUniversalTime(), OwnerUserId = actor.UserId,
            Status = "Pending", CreatedAt = clock.GetUtcNow(), Version = 1
        };
        db.FollowUps.Add(item);
        await db.SaveChangesAsync(cancellationToken);
        return FollowUpResponse.From(item);
    }

    public async Task<PagedResult<FollowUpResponse>> ListAsync(string? bucket, string? scope,
        int page, int pageSize, Actor actor, CancellationToken cancellationToken)
    {
        if (bucket is not ("Overdue" or "Today" or "Upcoming" or "Completed"))
            throw Invalid("invalid_bucket", "Selecciona una categoría de agenda.");
        if (scope is not (null or "Mine" or "All"))
            throw Invalid("invalid_scope", "Alcance no válido.");
        if (scope == "All" && actor.Role != "Administrator")
            throw new FollowUpRequestException(403, "forbidden_scope", "Solo Administrador puede consultar todos.");
        if (page < 1 || pageSize is < 1 or > 100 || (long)(page - 1) * pageSize > int.MaxValue)
            throw Invalid("invalid_pagination", "Paginación no válida.");

        IQueryable<FollowUp> query = db.FollowUps.AsNoTracking();
        if (scope != "All") query = query.Where(item => item.OwnerUserId == actor.UserId);
        if (bucket == "Completed")
            query = query.Where(item => item.Status == "Completed");
        else
        {
            var now = clock.GetUtcNow();
            var localDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(now, Bogota).DateTime);
            var tomorrow = BogotaMidnightUtc(localDate.AddDays(1));
            query = bucket switch
            {
                "Overdue" => query.Where(item => item.Status == "Pending" && item.ScheduledAt < now),
                "Today" => query.Where(item => item.Status == "Pending" && item.ScheduledAt >= now &&
                    item.ScheduledAt < tomorrow),
                _ => query.Where(item => item.Status == "Pending" && item.ScheduledAt >= tomorrow)
            };
        }
        var totalCount = await query.CountAsync(cancellationToken);
        query = bucket == "Completed"
            ? query.OrderByDescending(item => item.CompletedAt).ThenBy(item => item.Id)
            : query.OrderBy(item => item.ScheduledAt).ThenBy(item => item.Id);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize)
            .Select(item => new FollowUpResponse(item.Id, item.ContactId, item.OpportunityId,
                item.Subject, item.ScheduledAt, item.OwnerUserId, item.Status, item.CreatedAt,
                item.CompletedAt, item.CompletedByUserId, item.Version)).ToListAsync(cancellationToken);
        return new PagedResult<FollowUpResponse>(items, page, pageSize, totalCount);
    }

    public async Task<FollowUpResponse?> GetAsync(Guid id, Actor actor, CancellationToken cancellationToken)
    {
        var query = db.FollowUps.AsNoTracking().Where(item => item.Id == id);
        if (actor.Role != "Administrator") query = query.Where(item => item.OwnerUserId == actor.UserId);
        var item = await query.SingleOrDefaultAsync(cancellationToken);
        return item is null ? null : FollowUpResponse.From(item);
    }

    public async Task<FollowUpResponse?> CompleteAsync(Guid id, FollowUpComplete request, Actor actor,
        CancellationToken cancellationToken)
    {
        if (request.Version < 1) throw Invalid("invalid_version", "Versión obligatoria.");
        var query = db.FollowUps.Where(item => item.Id == id);
        if (actor.Role != "Administrator") query = query.Where(item => item.OwnerUserId == actor.UserId);
        var item = await query.SingleOrDefaultAsync(cancellationToken);
        if (item is null) return null;
        if (item.Status == "Completed") return FollowUpResponse.From(item);
        if (item.Version != request.Version)
            throw new FollowUpRequestException(409, "concurrency_conflict",
                "El seguimiento cambió. Recarga antes de completar.");
        item.Status = "Completed";
        item.CompletedAt = clock.GetUtcNow();
        item.CompletedByUserId = actor.UserId;
        item.Version++;
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException)
        {
            throw new FollowUpRequestException(409, "concurrency_conflict",
                "El seguimiento cambió. Recarga antes de completar.");
        }
        return FollowUpResponse.From(item);
    }

    private static DateTimeOffset BogotaMidnightUtc(DateOnly date)
    {
        var local = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        return new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(local, Bogota), TimeSpan.Zero);
    }

    private static FollowUpRequestException Invalid(string code, string title) => new(400, code, title);
}
