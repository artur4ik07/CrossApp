using System.Security.Cryptography;
using Api.Contracts;
using Api.Validation;
using Core.Abstractions;
using Core.Domain;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Api.Endpoints;

public static class OrderEndpoints
{
    public static void MapOrders(this WebApplication app)
    {
        var group = app.MapGroup("/api/orders").WithTags("Orders");

        group.MapGet("/", (IOrderStore store, int? page, int? pageSize, string? search) =>
        {
            int currentPage = Math.Max(1, page ?? 1);
            int currentSize = Math.Clamp(pageSize ?? 20, 1, 100);

            var query = store.List().AsEnumerable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(o =>
                    o.Id.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    o.CustomerId.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    o.Status.ToString().Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    o.Lines.Any(l => l.ProductName.Contains(search, StringComparison.OrdinalIgnoreCase)));
            }

            var ordered = query.OrderBy(o => o.Id, StringComparer.OrdinalIgnoreCase).ToList();
            int total = ordered.Count;

            var items = ordered
                .Skip((currentPage - 1) * currentSize)
                .Take(currentSize)
                .Select(OrderResponseDto.FromDomain)
                .ToList();

            return TypedResults.Ok(new PagedOrdersResponse(items, total, currentPage, currentSize));
        });

        group.MapGet("/{id}", Results<Ok<OrderResponseDto>, NotFound> (string id, IOrderStore store) =>
            store.GetById(id) is { } found
                ? TypedResults.Ok(OrderResponseDto.FromDomain(found))
                : TypedResults.NotFound());

        group.MapGet("/{id}/total", Results<Ok<OrderTotalDto>, NotFound> (string id, IOrderStore store) =>
            store.GetById(id) is { } found
                ? TypedResults.Ok(new OrderTotalDto(found.Id, found.Total, found.Status.ToString()))
                : TypedResults.NotFound());

        group.MapPost("/", Created<OrderResponseDto> (SaveOrderRequest request, IOrderStore store) =>
        {
            var id = Convert.ToHexString(RandomNumberGenerator.GetBytes(4)).ToLowerInvariant();
            var created = BuildOrderFromRequest(id, request);

            store.Add(created);
            return TypedResults.Created($"/api/orders/{created.Id}", OrderResponseDto.FromDomain(created));
        })
        .AddEndpointFilter<ValidationFilter<SaveOrderRequest>>();

        group.MapPut("/{id}", Results<Ok<OrderResponseDto>, NotFound> (string id, SaveOrderRequest request, IOrderStore store) =>
        {
            if (store.GetById(id) is null)
                return TypedResults.NotFound();

            var replacement = BuildOrderFromRequest(id, request);
            store.Update(replacement);
            return TypedResults.Ok(OrderResponseDto.FromDomain(replacement));
        })
        .AddEndpointFilter<ValidationFilter<SaveOrderRequest>>();

        group.MapDelete("/{id}", Results<NoContent, NotFound> (string id, IOrderStore store) =>
            store.Remove(id)
                ? TypedResults.NoContent()
                : TypedResults.NotFound());
    }

    private static Order BuildOrderFromRequest(string id, SaveOrderRequest request)
    {
        var order = Order.Create(id, request.CustomerId);
        foreach (var line in request.Lines!)
            order.AddLine(line.ProductId, line.ProductName, line.Price, line.Quantity);

        if (!string.IsNullOrWhiteSpace(request.Status) &&
            Enum.TryParse<OrderStatus>(request.Status, ignoreCase: true, out var status))
        {
            if (status == OrderStatus.Confirmed)
                order.Confirm();
            else if (status == OrderStatus.Cancelled)
                order.Cancel();
        }

        return order;
    }
}
