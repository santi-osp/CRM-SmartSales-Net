using SmartSales.Sales.Domain.Entities;

namespace SmartSales.Sales.Application.Interactions;

public sealed record InteractionCreate(Guid ContactId, Guid? OpportunityId, string? Type,
    DateTimeOffset OccurredAt, string? Summary);
public sealed record InteractionResponse(Guid Id, Guid ContactId, Guid? OpportunityId,
    string Type, DateTimeOffset OccurredAt, string Summary, Guid AuthorUserId, DateTimeOffset CreatedAt)
{
    public static InteractionResponse From(Interaction item) => new(item.Id, item.ContactId,
        item.OpportunityId, item.Type, item.OccurredAt, item.Summary, item.AuthorUserId, item.CreatedAt);
}
public sealed class InteractionRequestException(int statusCode, string code, string title) : Exception(title)
{
    public int StatusCode { get; } = statusCode;
    public string Code { get; } = code;
}
