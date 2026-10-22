using Core.Domain;

namespace Core.Reports;

public static class ReportService
{
    // 1) Фільтр + сортування + проєкція у DTO (дороговартісні замовлення)
    public static IReadOnlyList<OrderReportLine> HighValueOrders(IEnumerable<Order> source, decimal minTotal) =>
        source.Where(o => o.Total >= minTotal)
            .OrderByDescending(o => o.Total)
            .ThenBy(o => o.Id, StringComparer.Ordinal)
            .Select(o => new OrderReportLine(o.Id, o.CustomerId, o.Lines.Count, o.Total, o.Status.ToString()))
            .ToList();

    // 2) Топ-N за сумою зі стабільним порядком
    public static IReadOnlyList<OrderReportLine> TopByTotal(IEnumerable<Order> source, int n) =>
        source.OrderByDescending(o => o.Total)
            .ThenBy(o => o.Id, StringComparer.Ordinal)
            .Take(n)
            .Select(o => new OrderReportLine(o.Id, o.CustomerId, o.Lines.Count, o.Total, o.Status.ToString()))
            .ToList();

    // 3) Групування за статусом із підрахунком кількості та суми
    public static IReadOnlyList<StatusGroupSummary> ByStatus(IEnumerable<Order> source) =>
        source.GroupBy(o => o.Status)
            .Select(g => new StatusGroupSummary(g.Key.ToString(), g.Count(), g.Sum(o => o.Total)))
            .OrderByDescending(s => s.TotalAmount)
            .ToList();

    // 4) Агрегати з обробкою порожньої послідовності
    public static (int Count, decimal Total, double Average, decimal Max) Totals(IEnumerable<Order> source)
    {
        var list = source.ToList();
        return list.Count == 0
            ? (0, 0m, 0d, 0m)
            : (list.Count, list.Sum(o => o.Total), (double)list.Average(o => o.Total), list.Max(o => o.Total));
    }
}
