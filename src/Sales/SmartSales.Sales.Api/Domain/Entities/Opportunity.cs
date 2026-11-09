namespace SmartSales.Sales.Domain.Entities;

public sealed class Opportunity
{
    public Guid Id { get; set; }
    public Guid ContactId { get; set; }
    public required string Title { get; set; }
    public decimal EstimatedAmount { get; set; }
    public DateOnly ExpectedCloseDate { get; set; }
    public Guid OwnerUserId { get; set; }
    public required string Priority { get; set; }
    public required string Stage { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }
    public Guid? ClosedByUserId { get; set; }
    public decimal? FinalAmount { get; set; }
    public string? LossReason { get; set; }
    public long Version { get; set; }
}
