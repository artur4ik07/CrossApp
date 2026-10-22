namespace Core.Reports;

public sealed record CustomerOrdersSummary(
    string CustomerId,
    string CustomerName,
    int OrdersCount,
    decimal TotalAmount);
