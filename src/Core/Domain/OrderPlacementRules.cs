namespace Core.Domain;

// Цей інваріант охоплює ДВІ сутності: конкретне нове замовлення і всю множину
// замовлень клієнта. Order не має (і не повинен мати) посилання на інші Order —
// інакше кожна сутність тягнула б за собою весь граф об'єктів. Тому таку перевірку
// не можна розмістити в Order.Create(): фабричний метод сутності знає лише про себе.
// На тижні 5 ця логіка переїде у повноцінний OrderService, що матиме доступ до
// сховища й зможе підвантажити всі замовлення клієнта перед створенням нового.
public static class OrderPlacementRules
{
    public const int MaxOpenOrdersPerCustomer = 5;

    public static void EnsureCanPlaceNewOrder(IEnumerable<Order> existingOrders, string customerId)
    {
        int openCount = existingOrders.Count(o => o.CustomerId == customerId && !o.IsConfirmed);

        if (openCount >= MaxOpenOrdersPerCustomer)
            throw new InvalidOperationException(
                $"Клієнт {customerId} уже має {openCount} непідтверджених замовлень — нове створити не можна, доки частину не підтверджено або не скасовано");
    }
}
