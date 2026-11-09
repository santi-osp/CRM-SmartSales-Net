namespace SmartSales.Sales.Domain.Entities;

public sealed class FollowUp
{
    public Guid Id { get; set; }
    public Guid ContactId { get; set; }
    public Guid? OpportunityId { get; set; }
    public required string Subject { get; set; }
    public DateTimeOffset ScheduledAt { get; set; }
    public Guid OwnerUserId { get; set; }
    public required string Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public Guid? CompletedByUserId { get; set; }
    public long Version { get; set; }
}
