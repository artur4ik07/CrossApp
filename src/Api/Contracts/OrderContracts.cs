using Core.Domain;

namespace Api.Contracts;

public sealed record SaveOrderLineRequest(string ProductId, string ProductName, decimal Price, int Quantity);

public sealed record SaveOrderRequest(string CustomerId, string? Status, List<SaveOrderLineRequest>? Lines)
{
    public bool TryValidate(out Dictionary<string, string[]> errors)
    {
        var found = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(CustomerId) || CustomerId.Length > 50)
            found[nameof(CustomerId)] = ["Ідентифікатор клієнта обов'язковий (до 50 символів)."];

        if (!string.IsNullOrWhiteSpace(Status) && !Enum.TryParse<OrderStatus>(Status, ignoreCase: true, out _))
            found[nameof(Status)] = ["Некоректний статус. Допустимі значення: Draft, Confirmed, Cancelled."];

        if (Lines is null || Lines.Count == 0)
        {
            found[nameof(Lines)] = ["Замовлення повинно містити хоча б один рядок товару."];
        }
        else
        {
            for (int i = 0; i < Lines.Count; i++)
            {
                var line = Lines[i];
                if (string.IsNullOrWhiteSpace(line.ProductId))
                    found[$"Lines[{i}].ProductId"] = ["Ідентифікатор товару обов'язковий."];
                if (string.IsNullOrWhiteSpace(line.ProductName) || line.ProductName.Length > 200)
                    found[$"Lines[{i}].ProductName"] = ["Назва товару обов'язкова (до 200 символів)."];
                if (line.Price < 0)
                    found[$"Lines[{i}].Price"] = ["Ціна не може бути від'ємною."];
                if (line.Quantity <= 0)
                    found[$"Lines[{i}].Quantity"] = ["Кількість має бути більшою за нуль."];
            }
        }

        errors = found;
        return found.Count == 0;
    }
}

public sealed record OrderLineResponseDto(string ProductId, string ProductName, decimal Price, int Quantity, decimal LineTotal);

public sealed record OrderResponseDto(string Id, string CustomerId, string Status, decimal Total, IReadOnlyList<OrderLineResponseDto> Lines)
{
    public static OrderResponseDto FromDomain(Order order) => new(
        order.Id,
        order.CustomerId,
        order.Status.ToString(),
        order.Total,
        order.Lines.Select(l => new OrderLineResponseDto(l.ProductId, l.ProductName, l.Price, l.Quantity, l.Price * l.Quantity)).ToList());
}

public sealed record OrderTotalDto(string Id, decimal Total, string Status);
