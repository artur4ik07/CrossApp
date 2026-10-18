using Core.Dto;

namespace Core.Domain;

public sealed class Order
{
    private readonly List<OrderLine> _lines = [];

    public string Id { get; }
    public string CustomerId { get; }
    public IReadOnlyList<OrderLine> Lines => _lines.AsReadOnly();
    public OrderStatus Status { get; private set; } = OrderStatus.Draft;
    public bool IsConfirmed => Status == OrderStatus.Confirmed;
    public decimal Total => _lines.Sum(l => l.Price * l.Quantity);

    private Order(string id, string customerId)
    {
        Id = id;
        CustomerId = customerId;
    }

    public static Order Create(string id, string customerId)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Ідентифікатор замовлення обов'язковий", nameof(id));
        if (string.IsNullOrWhiteSpace(customerId))
            throw new ArgumentException("Ідентифікатор клієнта обов'язковий", nameof(customerId));

        return new Order(id.Trim(), customerId.Trim());
    }

    public void AddLine(string productId, string productName, decimal price, int quantity)
    {
        if (Status != OrderStatus.Draft)
            throw new InvalidOperationException($"Замовлення {Id} у стані {Status}, рядки можна додавати лише у стані Draft");
        if (string.IsNullOrWhiteSpace(productId))
            throw new ArgumentException("Ідентифікатор товару обов'язковий", nameof(productId));
        if (string.IsNullOrWhiteSpace(productName))
            throw new ArgumentException("Назва товару обов'язкова", nameof(productName));
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), quantity, "Кількість у рядку має бути більшою за нуль");
        if (price < 0)
            throw new ArgumentOutOfRangeException(nameof(price), price, "Ціна не може бути від'ємною");

        _lines.Add(new OrderLine(productId.Trim(), productName.Trim(), price, quantity));
    }

    public void Confirm()
    {
        if (_lines.Count == 0)
            throw new InvalidOperationException($"Замовлення {Id} не можна підтвердити: немає жодного рядка");

        Status = TransitionTo(Status, "confirm");
    }

    public void Cancel()
    {
        Status = TransitionTo(Status, "cancel");
    }

    // Явна таблиця допустимих переходів станів: switch expression з патерном кортежу
    // (поточний стан, дія). Будь-яка комбінація, якої немає в таблиці, кидає виняток.
    private OrderStatus TransitionTo(OrderStatus current, string action) => (current, action) switch
    {
        (OrderStatus.Draft, "confirm") => OrderStatus.Confirmed,
        (OrderStatus.Draft, "cancel") => OrderStatus.Cancelled,
        (OrderStatus.Confirmed, "cancel") => OrderStatus.Cancelled,
        (OrderStatus.Confirmed, "confirm") => throw new InvalidOperationException($"Замовлення {Id} вже підтверджене"),
        (OrderStatus.Cancelled, _) => throw new InvalidOperationException($"Замовлення {Id} скасоване, змінювати стан не можна"),
        _ => throw new InvalidOperationException($"Перехід '{action}' зі стану {current} неможливий")
    };

    public OrderDto ToDto() => new(Id, CustomerId, Status.ToString(), _lines.Select(l => l.ToDto()).ToList());

    public static Order FromDto(OrderDto dto)
    {
        Order order = Create(dto.Id, dto.CustomerId);
        foreach (OrderLineDto line in dto.Lines)
            order.AddLine(line.ProductId, line.ProductName, line.Price, line.Quantity);

        OrderStatus status = Enum.Parse<OrderStatus>(dto.Status);

        switch (status)
        {
            case OrderStatus.Draft:
                break;
            case OrderStatus.Confirmed:
                order.Confirm();
                break;
            case OrderStatus.Cancelled:
                order.Cancel();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(dto), dto.Status, "Невідомий статус замовлення");
        }

        return order;
    }

    public override string ToString() =>
        $"{Id} (клієнт {CustomerId}) — рядків: {Lines.Count}, сума: {Total:F2} [{Status}]";
}
