using Core.Domain;

namespace Api.Contracts;

public sealed record SaveOrderLineRequest(string ProductId, string ProductName, decimal Price, int Quantity);

public sealed record SaveOrderRequest(string CustomerId, string? Status, List<SaveOrderLineRequest>? Lines);

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

public sealed record PagedOrdersResponse(IReadOnlyList<OrderResponseDto> Items, int Total, int Page, int PageSize);
