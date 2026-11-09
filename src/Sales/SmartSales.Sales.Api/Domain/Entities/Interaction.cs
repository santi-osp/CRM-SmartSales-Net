namespace SmartSales.Sales.Domain.Entities;

public sealed class Interaction
{
    public Guid Id { get; set; }
    public Guid ContactId { get; set; }
    public Guid? OpportunityId { get; set; }
    public required string Type { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public required string Summary { get; set; }
    public Guid AuthorUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
