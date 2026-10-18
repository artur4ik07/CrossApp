namespace Core.Dto;

public sealed record OrderDto(
    string Id,
    string CustomerId,
    bool IsConfirmed,
    IReadOnlyList<OrderLineDto> Lines);
