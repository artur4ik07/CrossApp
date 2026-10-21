using Core.Abstractions;
using Core.Domain;
using Core.Events;
using Core.Notifications;

namespace Core.Services;

public sealed class OrderService(IOrderStore store, INotificationSink? sink = null)
{
    private readonly IOrderStore _store = store ?? throw new ArgumentNullException(nameof(store));
    private readonly INotificationSink? _sink = sink;

    public event EventHandler<OrderChangedEventArgs>? Changed;

    private void OnChanged(Order order, OrderChangeKind kind)
    {
        Changed?.Invoke(this, new OrderChangedEventArgs(order.Id, kind, order.Lines.Count, order.Total));
        _sink?.Notify($"{kind}: {order.Id}, рядків={order.Lines.Count}, сума={order.Total:F2}");
    }

    public Order CreateOrder(string customerId)
    {
        var order = Order.Create(Guid.NewGuid().ToString("N")[..8], customerId);
        _store.Add(order);
        OnChanged(order, OrderChangeKind.Created);
        return order;
    }

    public void AddLine(string orderId, string productId, string productName, decimal price, int quantity)
    {
        var order = _store.GetById(orderId)
            ?? throw new InvalidOperationException($"Немає замовлення з id={orderId}.");
        order.AddLine(productId, productName, price, quantity);
        _store.Update(order);
        OnChanged(order, OrderChangeKind.LineAdded);
    }

    public void Confirm(string orderId)
    {
        var order = _store.GetById(orderId)
            ?? throw new InvalidOperationException($"Немає замовлення з id={orderId}.");
        order.Confirm();
        _store.Update(order);
        OnChanged(order, OrderChangeKind.Confirmed);
    }

    public IReadOnlyList<Order> All() => _store.List();
    public Order? Find(string id) => _store.GetById(id);

    public IReadOnlyList<Order> Search(Func<Order, bool> predicate) =>
        _store.List().Where(predicate).ToList();
}
