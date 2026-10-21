using Core.Abstractions;
using Core.Domain;

namespace Core.Services;

public sealed class OrderService(IOrderStore store)
{
    private readonly IOrderStore _store = store ?? throw new ArgumentNullException(nameof(store));

    public Order CreateOrder(string customerId)
    {
        var order = Order.Create(Guid.NewGuid().ToString("N")[..8], customerId);
        _store.Add(order); // інваріанти вже перевірив Order.Create
        return order;
    }

    public void AddLine(string orderId, string productId, string productName, decimal price, int quantity)
    {
        var order = _store.GetById(orderId)
            ?? throw new InvalidOperationException($"Немає замовлення з id={orderId}.");
        order.AddLine(productId, productName, price, quantity); // метод сутності з тижня 4
        _store.Update(order);
    }

    public void Confirm(string orderId)
    {
        var order = _store.GetById(orderId)
            ?? throw new InvalidOperationException($"Немає замовлення з id={orderId}.");
        order.Confirm();
        _store.Update(order);
    }

    public IReadOnlyList<Order> All() => _store.List();
    public Order? Find(string id) => _store.GetById(id);

    // Пошук за довільним критерієм — підготовка до LINQ-звітів тижня 7.
    public IReadOnlyList<Order> Search(Func<Order, bool> predicate) =>
        _store.List().Where(predicate).ToList();
}
