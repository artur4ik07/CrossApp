using Core.Domain;
using Core.Events;
using Core.Services;
using Core.Storage;
using Xunit;

namespace Core.Tests;

public class OrderServiceTests
{
    [Fact]
    // 1) Інваріант: відмова та збереження стану
    public void AddLine_WithNegativeQuantity_Throws_AndKeepsState()
    {
        var store = new InMemoryOrderStore();
        var service = new OrderService(store);
        var order = service.CreateOrder("C-001");

        Assert.Throws<ArgumentOutOfRangeException>(() => service.AddLine(order.Id, "P-1", "Товар", 100m, -1));
        Assert.Empty(store.GetById(order.Id)!.Lines);
    }

    [Fact]
    // 2) Перевірка виклику події
    public void CreateOrder_RaisesChangedExactlyOnce()
    {
        var service = new OrderService(new InMemoryOrderStore());
        var events = new List<OrderChangedEventArgs>();
        service.Changed += (_, e) => events.Add(e);

        service.CreateOrder("C-001");

        Assert.Single(events);
        Assert.Equal(OrderChangeKind.Created, events[0].Kind);
    }

    [Theory]
    [InlineData(100, 2)]
    [InlineData(500, 0)]
    // 3) Звіт на кількох наборах даних (Theory + InlineData)
    public void Search_FiltersByTotalCorrectly(decimal minTotal, int expectedCount)
    {
        var store = new InMemoryOrderStore(TestData.SampleOrders());
        var service = new OrderService(store);

        var result = service.Search(o => o.Total >= minTotal);

        Assert.Equal(expectedCount, result.Count);
    }

    [Fact]
    // 4) Детермінованість сортування TopBy
    public void TopByQuantity_IsDeterministic()
    {
        var orders = TestData.SampleOrders();
        var top = orders.OrderByDescending(o => o.Total).ThenBy(o => o.Id).ToList();

        Assert.Equal("ORD-T01", top[0].Id);
    }

    [Fact]
    // 5) Перевірка скасування (CancellationToken)
    public async Task ImportAsync_WhenAlreadyCancelled_ThrowsOperationCanceledException()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var service = new OrderService(new InMemoryOrderStore());
        var importer = new Core.Import.JsonOrderImporter();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.ImportAsync(importer, "any.json", cts.Token));
    }
}