namespace Core.Dto;

public sealed record OrderDto(
    string Id,
    string CustomerId,
    string Status,
    IReadOnlyList<OrderLineDto> Lines);
