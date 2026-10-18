namespace Core.Dto;

public sealed record OrderLineDto(
    string ProductId,
    string ProductName,
    decimal Price,
    int Quantity);
