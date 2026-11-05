using Core.Domain;

namespace Core.Tests;

public static class TestData
{
    public static List<Order> SampleOrders()
    {
        var o1 = Order.Create("ORD-T01", "C-001");
        o1.AddLine("P-1", "Книжка", 200m, 2);

        var o2 = Order.Create("ORD-T02", "C-002");
        o2.AddLine("P-2", "Зошит", 50m, 5);
        o2.Confirm();

        var o3 = Order.Create("ORD-T03", "C-001");
        o3.AddLine("P-3", "Ручка", 30m, 1);

        return [o1, o2, o3];
    }
}