using SmartSales.Sales.Domain.Entities;

namespace SmartSales.Sales.Application.Opportunities;

public sealed record Actor(Guid UserId, string Role, string Bearer);
public sealed record OpportunityCreate(Guid ContactId, string? Title, decimal EstimatedAmount,
    DateOnly ExpectedCloseDate, Guid? OwnerUserId, string? Priority);
public sealed record OpportunityUpdate(string? Title, decimal EstimatedAmount, DateOnly ExpectedCloseDate,
    string? Priority, long Version);
public sealed record OwnerChangeRequest(Guid OwnerUserId, long Version);
public sealed record StageChangeRequest(string? Stage, long Version);
public sealed record OpportunityCloseRequest(string? Result, decimal? FinalAmount, string? LossReason, long Version);
public sealed record OpportunityResponse(Guid Id, Guid ContactId, string Title, decimal EstimatedAmount,
    DateOnly ExpectedCloseDate, Guid OwnerUserId, string Priority, string Stage, Guid CreatedByUserId,
    DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, DateTimeOffset? ClosedAt, Guid? ClosedByUserId,
    decimal? FinalAmount, string? LossReason, long Version)
{
    public static OpportunityResponse From(Opportunity item) => new(item.Id, item.ContactId, item.Title,
        item.EstimatedAmount, item.ExpectedCloseDate, item.OwnerUserId, item.Priority, item.Stage,
        item.CreatedByUserId, item.CreatedAt, item.UpdatedAt, item.ClosedAt, item.ClosedByUserId,
        item.FinalAmount, item.LossReason, item.Version);
}
public sealed record OwnerChangeResponse(Guid Id, Guid PreviousOwnerUserId, Guid NewOwnerUserId,
    Guid ChangedByUserId, DateTimeOffset ChangedAt);
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);
public sealed record OpportunityFilters(string? Stage, bool OpenOnly, Guid? OwnerUserId, string? Priority,
    Guid? ContactId, DateOnly? CreatedFrom, DateOnly? CreatedTo, int Page, int PageSize);

public sealed class OpportunityRequestException(int statusCode, string code, string title) : Exception(title)
{
    public int StatusCode { get; } = statusCode;
    public string Code { get; } = code;
}
