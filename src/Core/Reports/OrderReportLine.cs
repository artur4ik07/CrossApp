namespace Core.Reports;

public sealed record OrderReportLine(
    string OrderId,
    string CustomerId,
    int LinesCount,
    decimal Total,
    string Status);
