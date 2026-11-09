namespace SmartSales.Sales.Domain.Entities;

public sealed class OpportunityOwnerChange
{
    public Guid Id { get; set; }
    public Guid OpportunityId { get; set; }
    public Guid PreviousOwnerUserId { get; set; }
    public Guid NewOwnerUserId { get; set; }
    public Guid ChangedByUserId { get; set; }
    public DateTimeOffset ChangedAt { get; set; }
}
