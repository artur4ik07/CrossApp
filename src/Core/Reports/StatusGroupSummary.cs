namespace Core.Reports;

public sealed record StatusGroupSummary(
    string Status,
    int Count,
    decimal TotalAmount);
