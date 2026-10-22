using System.Diagnostics;
using Core.Domain;
using Core.Dto;

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

    // 2) Власний метод-розширення TopBy (Звіт 1: ТОП за сумою)
    public static IReadOnlyList<OrderReportLine> TopByTotal(IEnumerable<Order> source, int n) =>
        source.TopBy(o => o.Total, n)
            .Select(o => new OrderReportLine(o.Id, o.CustomerId, o.Lines.Count, o.Total, o.Status.ToString()))
            .ToList();

    // 2) Власний метод-розширення TopBy (Звіт 2: ТОП за кількістю позицій)
    public static IReadOnlyList<OrderReportLine> TopByLines(IEnumerable<Order> source, int n) =>
        source.TopBy(o => o.Lines.Count, n)
            .Select(o => new OrderReportLine(o.Id, o.CustomerId, o.Lines.Count, o.Total, o.Status.ToString()))
            .ToList();

    // 3) Методний синтаксис (Method Syntax)
    public static IReadOnlyList<StatusGroupSummary> ByStatus(IEnumerable<Order> source) =>
        source.GroupBy(o => o.Status)
            .Select(g => new StatusGroupSummary(g.Key.ToString(), g.Count(), g.Sum(o => o.Total)))
            .OrderByDescending(s => s.TotalAmount)
            .ToList();

    // 3) Синтаксис запитів (Query Syntax: from ... group ... into ...)
    public static IReadOnlyList<StatusGroupSummary> ByStatusQuerySyntax(IEnumerable<Order> source) =>
        (from o in source
         group o by o.Status into g
         orderby g.Sum(x => x.Total) descending
         select new StatusGroupSummary(g.Key.ToString(), g.Count(), g.Sum(x => x.Total)))
        .ToList();

    // 1) GroupJoin двох колекцій: Клієнти x Замовлення
    public static IReadOnlyList<CustomerOrdersSummary> CustomerOrdersReport(
        IEnumerable<CustomerDto> customers,
        IEnumerable<Order> orders) =>
        customers.GroupJoin(
            orders,
            c => c.Id,
            o => o.CustomerId,
            (c, customerOrders) => new CustomerOrdersSummary(
                c.Id,
                c.Name,
                customerOrders.Count(),
                customerOrders.Sum(o => o.Total)))
            .OrderByDescending(s => s.TotalAmount)
            .ToList();

    // 4) Агрегати
    public static (int Count, decimal Total, double Average, decimal Max) Totals(IEnumerable<Order> source)
    {
        var list = source.ToList();
        return list.Count == 0
            ? (0, 0m, 0d, 0m)
            : (list.Count, list.Sum(o => o.Total), (double)list.Average(o => o.Total), list.Max(o => o.Total));
    }

    // 4) Замір часу Stopwatch на 100 000 згенерованих записах
    public static (long MsWithToList, long MsWithoutToList) RunBenchmark(int count = 100_000)
    {
        var generatedOrders = new List<Order>(count);
        for (int i = 0; i < count; i++)
        {
            var o = Order.Create($"ORD-{i:000000}", $"C-{i % 500:000}");
            o.AddLine($"P-{i % 100}", "Книга", (i % 200) + 10m, (i % 5) + 1);
            if (i % 2 == 0) o.Confirm();
            generatedOrders.Add(o);
        }

        // Варіант A: з ToList() всередині ланцюжка (зайве виділення пам'яті)
        var sw1 = Stopwatch.StartNew();
        var res1 = generatedOrders
            .Where(o => o.Total > 100m)
            .ToList()
            .Select(o => new OrderReportLine(o.Id, o.CustomerId, o.Lines.Count, o.Total, o.Status.ToString()))
            .OrderByDescending(x => x.Total)
            .ToList();
        sw1.Stop();

        // Варіант B: чистий потоковий LINQ без ToList() всередині
        var sw2 = Stopwatch.StartNew();
        var res2 = generatedOrders
            .Where(o => o.Total > 100m)
            .Select(o => new OrderReportLine(o.Id, o.CustomerId, o.Lines.Count, o.Total, o.Status.ToString()))
            .OrderByDescending(x => x.Total)
            .ToList();
        sw2.Stop();

        return (sw1.ElapsedMilliseconds, sw2.ElapsedMilliseconds);
    }
}
