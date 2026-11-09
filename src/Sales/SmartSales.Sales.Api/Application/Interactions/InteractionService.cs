using System.Text;
using Microsoft.EntityFrameworkCore;
using SmartSales.Sales.Application.Opportunities;
using SmartSales.Sales.Domain.Entities;
using SmartSales.Sales.Infrastructure.ContactsDirectory;
using SmartSales.Sales.Infrastructure.Persistence;

namespace SmartSales.Sales.Application.Interactions;

public sealed class InteractionService(SalesDbContext db, ContactsDirectoryClient directory, TimeProvider clock)
{
    public async Task<InteractionResponse> CreateAsync(InteractionCreate request, Actor actor,
        CancellationToken cancellationToken)
    {
        if (request.ContactId == Guid.Empty)
            throw Invalid("invalid_contact", "Selecciona un contacto.");
        if (request.OpportunityId == Guid.Empty)
            throw Invalid("invalid_opportunity", "Oportunidad no válida.");
        if (request.Type is not ("Call" or "Email" or "Meeting"))
            throw Invalid("invalid_type", "Tipo de interacción no válido.");
        if (request.OccurredAt == default || request.OccurredAt > clock.GetUtcNow())
            throw Invalid("invalid_occurred_at", "La fecha de la interacción debe ser pasada o actual.");
        var summary = request.Summary?.Trim().Normalize(NormalizationForm.FormC);
        if (summary is null or { Length: 0 or > 2000 })
            throw Invalid("invalid_summary", "Resumen obligatorio de máximo 2000 caracteres.");
        if (await directory.GetContactAsync(request.ContactId, actor.Bearer, cancellationToken) is null)
            throw new InteractionRequestException(422, "invalid_contact", "El contacto no existe.");
        if (request.OpportunityId is { } opportunityId &&
            !await db.Opportunities.AsNoTracking().AnyAsync(item =>
                item.Id == opportunityId && item.ContactId == request.ContactId, cancellationToken))
            throw new InteractionRequestException(422, "invalid_opportunity",
                "La oportunidad no pertenece al contacto seleccionado.");
        var item = new Interaction
        {
            Id = Guid.CreateVersion7(), ContactId = request.ContactId,
            OpportunityId = request.OpportunityId, Type = request.Type,
            OccurredAt = request.OccurredAt.ToUniversalTime(), Summary = summary,
            AuthorUserId = actor.UserId, CreatedAt = clock.GetUtcNow()
        };
        db.Interactions.Add(item);
        await db.SaveChangesAsync(cancellationToken);
        return InteractionResponse.From(item);
    }

    public async Task<PagedResult<InteractionResponse>> ListAsync(Guid? contactId, Guid? opportunityId,
        int page, int pageSize, CancellationToken cancellationToken)
    {
        if (contactId is null && opportunityId is null)
            throw Invalid("missing_filter", "Indica contacto u oportunidad.");
        if (contactId == Guid.Empty || opportunityId == Guid.Empty)
            throw Invalid("invalid_filter", "Filtro de contacto u oportunidad no válido.");
        if (page < 1 || pageSize is < 1 or > 100 || (long)(page - 1) * pageSize > int.MaxValue)
            throw Invalid("invalid_pagination", "Paginación no válida.");
        IQueryable<Interaction> query = db.Interactions.AsNoTracking();
        if (contactId is not null) query = query.Where(item => item.ContactId == contactId);
        if (opportunityId is not null) query = query.Where(item => item.OpportunityId == opportunityId);
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(item => item.OccurredAt).ThenBy(item => item.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(item => new InteractionResponse(item.Id, item.ContactId, item.OpportunityId,
                item.Type, item.OccurredAt, item.Summary, item.AuthorUserId, item.CreatedAt))
            .ToListAsync(cancellationToken);
        return new PagedResult<InteractionResponse>(items, page, pageSize, totalCount);
    }

    private static InteractionRequestException Invalid(string code, string title) => new(400, code, title);
}
