using SmartSales.Sales.Domain.Entities;

namespace SmartSales.Sales.Application.FollowUps;

public sealed record FollowUpCreate(Guid ContactId, Guid? OpportunityId, string? Subject,
    DateTimeOffset ScheduledAt);
public sealed record FollowUpComplete(long Version);
public sealed record FollowUpResponse(Guid Id, Guid ContactId, Guid? OpportunityId, string Subject,
    DateTimeOffset ScheduledAt, Guid OwnerUserId, string Status, DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt, Guid? CompletedByUserId, long Version)
{
    public static FollowUpResponse From(FollowUp item) => new(item.Id, item.ContactId,
        item.OpportunityId, item.Subject, item.ScheduledAt, item.OwnerUserId, item.Status,
        item.CreatedAt, item.CompletedAt, item.CompletedByUserId, item.Version);
}

public sealed class FollowUpRequestException(int statusCode, string code, string title) : Exception(title)
{
    public int StatusCode { get; } = statusCode;
    public string Code { get; } = code;
}
