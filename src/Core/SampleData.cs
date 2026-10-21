using Core.Domain;

namespace Core;

public static class SampleData
{
    public static List<Order> Orders()
    {
        var orders = new List<Order>
        {
            BuildOrder("ORD-S01", "C-001", confirm: true,
                ("P-001", "Кобзар", 249.00m, 2),
                ("P-002", "Тіні забутих предків", 189.50m, 1)),

            BuildOrder("ORD-S02", "C-002", confirm: true,
                ("P-003", "Лісова пісня", 159.00m, 3),
                ("P-004", "Захар Беркут", 199.00m, 1),
                ("P-005", "Місто", 229.90m, 1)),

            BuildOrder("ORD-S03", "C-003", confirm: false,
                ("P-006", "Тигролови", 179.00m, 1),
                ("P-007", "Планета Людей", 139.00m, 2)),

            BuildOrder("ORD-S04", "C-001", confirm: true,
                ("P-008", "Доктор Серафікус", 209.00m, 1),
                ("P-009", "Собор", 259.00m, 2),
                ("P-010", "Жовтий князь", 289.00m, 1)),

            BuildOrder("ORD-S05", "C-004", confirm: false,
                ("P-001", "Кобзар", 249.00m, 1),
                ("P-003", "Лісова пісня", 159.00m, 1),
                ("P-005", "Місто", 229.90m, 2)),

            BuildOrder("ORD-S06", "C-005", confirm: true,
                ("P-002", "Тіні забутих предків", 189.50m, 2),
                ("P-007", "Планета Людей", 139.00m, 1))
        };

        return orders;
    }

    private static Order BuildOrder(
        string id, string customerId, bool confirm,
        params (string ProductId, string Name, decimal Price, int Quantity)[] lines)
    {
        Order order = Order.Create(id, customerId);
        foreach (var line in lines)
            order.AddLine(line.ProductId, line.Name, line.Price, line.Quantity);
        if (confirm)
            order.Confirm();
        return order;
    }
}
