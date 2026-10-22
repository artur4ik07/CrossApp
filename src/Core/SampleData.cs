using Core.Domain;

namespace Core;

public static class SampleData
{
    public static List<Order> Orders()
    {
        return new List<Order>
        {
            BuildOrder("ORD-S01", "C-001", OrderStatusAction.Confirm, ("P-001", "Кобзар", 249.00m, 2), ("P-002", "Тіні забутих предків", 189.50m, 1)),
            BuildOrder("ORD-S02", "C-002", OrderStatusAction.Confirm, ("P-003", "Лісова пісня", 159.00m, 3), ("P-004", "Захар Беркут", 199.00m, 1), ("P-005", "Місто", 229.90m, 1)),
            BuildOrder("ORD-S03", "C-003", OrderStatusAction.None, ("P-006", "Тигролови", 179.00m, 1), ("P-007", "Планета Людей", 139.00m, 2)),
            BuildOrder("ORD-S04", "C-001", OrderStatusAction.Confirm, ("P-008", "Доктор Серафікус", 209.00m, 1), ("P-009", "Собор", 259.00m, 2), ("P-010", "Жовтий князь", 289.00m, 1)),
            BuildOrder("ORD-S05", "C-004", OrderStatusAction.None, ("P-001", "Кобзар", 249.00m, 1), ("P-003", "Лісова пісня", 159.00m, 1), ("P-005", "Місто", 229.90m, 2)),
            BuildOrder("ORD-S06", "C-005", OrderStatusAction.Confirm, ("P-002", "Тіні забутих предків", 189.50m, 2), ("P-007", "Планета Людей", 139.00m, 1)),
            BuildOrder("ORD-S07", "C-002", OrderStatusAction.Cancel, ("P-004", "Захар Беркут", 199.00m, 1)),
            BuildOrder("ORD-S08", "C-006", OrderStatusAction.Confirm, ("P-010", "Жовтий князь", 289.00m, 3), ("P-001", "Кобзар", 249.00m, 2)),
            BuildOrder("ORD-S09", "C-003", OrderStatusAction.None, ("P-005", "Місто", 229.90m, 1)),
            BuildOrder("ORD-S10", "C-007", OrderStatusAction.Confirm, ("P-009", "Собор", 259.00m, 4)),
            BuildOrder("ORD-S11", "C-008", OrderStatusAction.Cancel, ("P-003", "Лісова пісня", 159.00m, 2)),
            BuildOrder("ORD-S12", "C-001", OrderStatusAction.None, ("P-006", "Тигролови", 179.00m, 5)),
            BuildOrder("ORD-S13", "C-009", OrderStatusAction.Confirm, ("P-008", "Доктор Серафікус", 209.00m, 2), ("P-002", "Тіні забутих предків", 189.50m, 1)),
            BuildOrder("ORD-S14", "C-004", OrderStatusAction.Confirm, ("P-007", "Планета Людей", 139.00m, 3)),
            BuildOrder("ORD-S15", "C-010", OrderStatusAction.Cancel, ("P-001", "Кобзар", 249.00m, 1)),
            BuildOrder("ORD-S16", "C-005", OrderStatusAction.None, ("P-010", "Жовтий князь", 289.00m, 1), ("P-004", "Захар Беркут", 199.00m, 2))
        };
    }

    private enum OrderStatusAction { None, Confirm, Cancel }

    private static Order BuildOrder(
        string id, string customerId, OrderStatusAction action,
        params (string ProductId, string Name, decimal Price, int Quantity)[] lines)
    {
        Order order = Order.Create(id, customerId);
        foreach (var line in lines)
            order.AddLine(line.ProductId, line.Name, line.Price, line.Quantity);

        switch (action)
        {
            case OrderStatusAction.Confirm:
                order.Confirm();
                break;
            case OrderStatusAction.Cancel:
                order.Cancel();
                break;
        }

        return order;
    }
}
