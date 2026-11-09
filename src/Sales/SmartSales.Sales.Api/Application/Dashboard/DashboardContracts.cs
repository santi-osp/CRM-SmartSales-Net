namespace SmartSales.Sales.Application.Dashboard;

public sealed record StageCount(string Stage, int Count);
public sealed record DashboardFilters(Guid? OwnerUserId, DateOnly? CreatedFrom, DateOnly? CreatedTo);
public sealed record DashboardResponse(int OpenCount, int WonCount, int LostCount,
    decimal OpenEstimatedAmount, string Currency, IReadOnlyList<StageCount> ByStage,
    DashboardFilters Filters, DateTimeOffset GeneratedAt);
public sealed class DashboardRequestException(string code, string title) : Exception(title)
{
    public string Code { get; } = code;
}
