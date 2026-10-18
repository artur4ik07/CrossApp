namespace Core.Domain;

public sealed record OrderLine(string ProductId, string ProductName, decimal Price, int Quantity)
{
    public Core.Dto.OrderLineDto ToDto() => new(ProductId, ProductName, Price, Quantity);
}
