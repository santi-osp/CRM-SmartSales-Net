using System.Text;
using Microsoft.EntityFrameworkCore;
using SmartSales.Sales.Domain.Entities;
using SmartSales.Sales.Infrastructure.ContactsDirectory;
using SmartSales.Sales.Infrastructure.Persistence;

namespace SmartSales.Sales.Application.Opportunities;

public sealed class OpportunityService(SalesDbContext db, ContactsDirectoryClient directory, TimeProvider clock)
{
    private static readonly HashSet<string> Priorities = ["High", "Medium", "Low"];
    private static readonly HashSet<string> Stages = ["New", "Contacted", "Proposal", "Won", "Lost"];
    private static readonly TimeZoneInfo Bogota = TimeZoneInfo.FindSystemTimeZoneById("America/Bogota");

    public async Task<PagedResult<OpportunityResponse>> ListAsync(OpportunityFilters filters,
        CancellationToken cancellationToken)
    {
        CheckPage(filters.Page, filters.PageSize);
        if (filters.Stage is not null && !Stages.Contains(filters.Stage))
            throw Invalid("invalid_stage", "Etapa no válida.");
        if (filters.Priority is not null && !Priorities.Contains(filters.Priority))
            throw Invalid("invalid_priority", "Prioridad no válida.");
        if (filters.CreatedFrom > filters.CreatedTo || filters.CreatedTo == DateOnly.MaxValue)
            throw Invalid("invalid_date_range", "Rango de fechas no válido.");
        IQueryable<Opportunity> query = db.Opportunities.AsNoTracking();
        if (filters.Stage is not null) query = query.Where(item => item.Stage == filters.Stage);
        if (filters.OpenOnly) query = query.Where(item => item.Stage != "Won" && item.Stage != "Lost");
        if (filters.Priority is not null) query = query.Where(item => item.Priority == filters.Priority);
        if (filters.OwnerUserId is not null) query = query.Where(item => item.OwnerUserId == filters.OwnerUserId);
        if (filters.ContactId is not null) query = query.Where(item => item.ContactId == filters.ContactId);
        if (filters.CreatedFrom is { } from)
        {
            var start = BogotaMidnightUtc(from);
            query = query.Where(item => item.CreatedAt >= start);
        }
        if (filters.CreatedTo is { } to)
        {
            var end = BogotaMidnightUtc(to.AddDays(1));
            query = query.Where(item => item.CreatedAt < end);
        }
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(item => item.CreatedAt).ThenBy(item => item.Id)
            .Skip((filters.Page - 1) * filters.PageSize).Take(filters.PageSize)
            .Select(item => new OpportunityResponse(item.Id, item.ContactId, item.Title, item.EstimatedAmount,
                item.ExpectedCloseDate, item.OwnerUserId, item.Priority, item.Stage, item.CreatedByUserId,
                item.CreatedAt, item.UpdatedAt, item.ClosedAt, item.ClosedByUserId, item.FinalAmount,
                item.LossReason, item.Version)).ToListAsync(cancellationToken);
        return new PagedResult<OpportunityResponse>(items, filters.Page, filters.PageSize, totalCount);
    }

    public async Task<OpportunityResponse?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var item = await db.Opportunities.AsNoTracking().SingleOrDefaultAsync(row => row.Id == id, cancellationToken);
        return item is null ? null : OpportunityResponse.From(item);
    }

    public async Task<OpportunityResponse> CreateAsync(OpportunityCreate request, Actor actor,
        CancellationToken cancellationToken)
    {
        var title = ValidateFields(request.Title, request.EstimatedAmount, request.ExpectedCloseDate,
            request.Priority ?? "Medium");
        if (request.ContactId == Guid.Empty) throw Invalid("invalid_contact", "Contacto obligatorio.");
        var ownerId = request.OwnerUserId ?? actor.UserId;
        if (ownerId == Guid.Empty) throw Invalid("invalid_owner", "Responsable obligatorio.");
        if (actor.Role != "Administrator" && ownerId != actor.UserId)
            throw new OpportunityRequestException(403, "forbidden_owner", "No puedes asignar otro responsable.");
        if (await directory.GetContactAsync(request.ContactId, actor.Bearer, cancellationToken) is null)
            throw new OpportunityRequestException(422, "invalid_contact", "El contacto no existe.");
        var owner = await directory.GetUserAsync(ownerId, actor.Bearer, cancellationToken);
        if (owner is null || !owner.IsActive)
            throw new OpportunityRequestException(422, "inactive_owner", "El responsable no está activo.");

        var now = clock.GetUtcNow();
        var opportunity = new Opportunity
        {
            Id = Guid.CreateVersion7(), ContactId = request.ContactId, Title = title,
            EstimatedAmount = request.EstimatedAmount, ExpectedCloseDate = request.ExpectedCloseDate,
            OwnerUserId = ownerId, Priority = request.Priority ?? "Medium", Stage = "New",
            CreatedByUserId = actor.UserId, CreatedAt = now, UpdatedAt = now, Version = 1
        };
        db.Opportunities.Add(opportunity);
        await db.SaveChangesAsync(cancellationToken);
        return OpportunityResponse.From(opportunity);
    }

    public async Task<OpportunityResponse?> UpdateAsync(Guid id, OpportunityUpdate request,
        CancellationToken cancellationToken)
    {
        var title = ValidateFields(request.Title, request.EstimatedAmount, request.ExpectedCloseDate,
            request.Priority);
        if (request.Version < 1) throw Invalid("invalid_version", "Versión obligatoria.");
        var item = await db.Opportunities.SingleOrDefaultAsync(row => row.Id == id, cancellationToken);
        if (item is null) return null;
        CheckEditable(item, request.Version);
        item.Title = title;
        item.EstimatedAmount = request.EstimatedAmount;
        item.ExpectedCloseDate = request.ExpectedCloseDate;
        item.Priority = request.Priority!;
        item.UpdatedAt = clock.GetUtcNow();
        item.Version++;
        await SaveAsync(cancellationToken);
        return OpportunityResponse.From(item);
    }

    public async Task<OpportunityResponse?> ChangeOwnerAsync(Guid id, OwnerChangeRequest request,
        Actor actor, CancellationToken cancellationToken)
    {
        if (actor.Role != "Administrator")
            throw new OpportunityRequestException(403, "forbidden_owner", "Solo Administrador puede reasignar.");
        if (request.OwnerUserId == Guid.Empty || request.Version < 1)
            throw Invalid("invalid_owner", "Responsable y versión obligatorios.");
        var item = await db.Opportunities.SingleOrDefaultAsync(row => row.Id == id, cancellationToken);
        if (item is null) return null;
        CheckEditable(item, request.Version);
        var user = await directory.GetUserAsync(request.OwnerUserId, actor.Bearer, cancellationToken);
        if (user is null || !user.IsActive)
            throw new OpportunityRequestException(422, "inactive_owner", "El responsable no está activo.");
        if (item.OwnerUserId == request.OwnerUserId) return OpportunityResponse.From(item);

        var now = clock.GetUtcNow();
        db.OpportunityOwnerChanges.Add(new OpportunityOwnerChange
        {
            Id = Guid.CreateVersion7(), OpportunityId = item.Id,
            PreviousOwnerUserId = item.OwnerUserId, NewOwnerUserId = request.OwnerUserId,
            ChangedByUserId = actor.UserId, ChangedAt = now
        });
        item.OwnerUserId = request.OwnerUserId;
        item.UpdatedAt = now;
        item.Version++;
        await SaveAsync(cancellationToken);
        return OpportunityResponse.From(item);
    }

    public async Task<OpportunityResponse?> ChangeStageAsync(Guid id, StageChangeRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Stage is not ("New" or "Contacted" or "Proposal"))
            throw Invalid("invalid_stage", "Selecciona una etapa activa.");
        if (request.Version < 1) throw Invalid("invalid_version", "Versión obligatoria.");
        var item = await db.Opportunities.SingleOrDefaultAsync(row => row.Id == id, cancellationToken);
        if (item is null) return null;
        CheckEditable(item, request.Version);
        if (item.Stage == request.Stage) return OpportunityResponse.From(item);
        item.Stage = request.Stage;
        item.UpdatedAt = clock.GetUtcNow();
        item.Version++;
        await SaveAsync(cancellationToken);
        return OpportunityResponse.From(item);
    }

    public async Task<OpportunityResponse?> CloseAsync(Guid id, OpportunityCloseRequest request,
        Actor actor, CancellationToken cancellationToken)
    {
        if (request.Result is not ("Won" or "Lost"))
            throw Invalid("invalid_result", "Resultado de cierre no válido.");
        if (request.Version < 1) throw Invalid("invalid_version", "Versión obligatoria.");
        string? lossReason = null;
        if (request.Result == "Won")
        {
            if (request.FinalAmount is null or < 0 or > 999999999999.99m ||
                decimal.Round(request.FinalAmount.Value, 2) != request.FinalAmount ||
                request.LossReason is not null)
                throw Invalid("invalid_close", "Ganada requiere monto final no negativo y sin motivo.");
        }
        else
        {
            lossReason = request.LossReason?.Trim().Normalize(NormalizationForm.FormC);
            if (lossReason is null or { Length: 0 or > 1000 } || request.FinalAmount is not null)
                throw Invalid("invalid_close", "Perdida requiere motivo y sin monto final.");
        }
        var item = await db.Opportunities.SingleOrDefaultAsync(row => row.Id == id, cancellationToken);
        if (item is null) return null;
        CheckEditable(item, request.Version);
        var now = clock.GetUtcNow();
        item.Stage = request.Result;
        item.ClosedAt = now;
        item.ClosedByUserId = actor.UserId;
        item.FinalAmount = request.Result == "Won" ? request.FinalAmount : null;
        item.LossReason = lossReason;
        item.UpdatedAt = now;
        item.Version++;
        await SaveAsync(cancellationToken);
        return OpportunityResponse.From(item);
    }

    public async Task<PagedResult<OwnerChangeResponse>?> OwnerChangesAsync(Guid id, int page, int pageSize,
        CancellationToken cancellationToken)
    {
        CheckPage(page, pageSize);
        if (!await db.Opportunities.AsNoTracking().AnyAsync(item => item.Id == id, cancellationToken))
            return null;
        var query = db.OpportunityOwnerChanges.AsNoTracking().Where(item => item.OpportunityId == id);
        var count = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(item => item.ChangedAt).ThenBy(item => item.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(item => new OwnerChangeResponse(item.Id, item.PreviousOwnerUserId,
                item.NewOwnerUserId, item.ChangedByUserId, item.ChangedAt))
            .ToListAsync(cancellationToken);
        return new PagedResult<OwnerChangeResponse>(items, page, pageSize, count);
    }

    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException)
        {
            throw new OpportunityRequestException(409, "concurrency_conflict", "La oportunidad cambió. Recarga antes de guardar.");
        }
    }

    private static string ValidateFields(string? title, decimal amount, DateOnly expectedDate, string? priority)
    {
        var normalized = title?.Trim().Normalize(NormalizationForm.FormC);
        if (normalized is null or { Length: 0 or > 160 })
            throw Invalid("invalid_title", "Título obligatorio de máximo 160 caracteres.");
        if (amount is < 0 or > 999999999999.99m || decimal.Round(amount, 2) != amount)
            throw Invalid("invalid_amount", "Monto COP no válido.");
        if (expectedDate == default)
            throw Invalid("invalid_date", "Fecha esperada obligatoria.");
        if (priority is null || !Priorities.Contains(priority))
            throw Invalid("invalid_priority", "Prioridad no válida.");
        return normalized;
    }

    private static void CheckEditable(Opportunity item, long version)
    {
        if (item.Stage is "Won" or "Lost")
            throw new OpportunityRequestException(409, "opportunity_closed", "La oportunidad está cerrada.");
        if (item.Version != version)
            throw new OpportunityRequestException(409, "concurrency_conflict", "La oportunidad cambió. Recarga antes de guardar.");
    }

    private static void CheckPage(int page, int pageSize)
    {
        if (page < 1 || pageSize is < 1 or > 100 || (long)(page - 1) * pageSize > int.MaxValue)
            throw Invalid("invalid_pagination", "Paginación no válida.");
    }

    private static DateTimeOffset BogotaMidnightUtc(DateOnly date)
    {
        var local = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        return new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(local, Bogota), TimeSpan.Zero);
    }

    private static OpportunityRequestException Invalid(string code, string title) => new(400, code, title);
}
