using Core.Domain;
using Core.Dto;
using Core.Events;
using Core.Import;
using Core.Services;
using Core.Storage;
using Xunit;

namespace Core.Tests;

public class OrderServiceTests
{
    [Fact]
    public void AddLine_WithNegativeQuantity_Throws_AndKeepsState()
    {
        var store = new InMemoryOrderStore();
        var service = new OrderService(store);
        var order = service.CreateOrder("C-001");

        Assert.Throws<ArgumentOutOfRangeException>(() => service.AddLine(order.Id, "P-1", "Товар", 100m, -1));
        Assert.Empty(store.GetById(order.Id)!.Lines);
    }

    [Fact]
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
    public void Search_FiltersByTotalCorrectly(decimal minTotal, int expectedCount)
    {
        var store = new InMemoryOrderStore(TestData.SampleOrders());
        var service = new OrderService(store);

        var result = service.Search(o => o.Total >= minTotal);

        Assert.Equal(expectedCount, result.Count);
    }

    [Fact]
    public void TopByQuantity_IsDeterministic()
    {
        var orders = TestData.SampleOrders();
        var top = orders.OrderByDescending(o => o.Total).ThenBy(o => o.Id).ToList();

        Assert.Equal("ORD-T01", top[0].Id);
    }

    [Fact]
    public async Task ImportAsync_WhenAlreadyCancelled_ThrowsOperationCanceledException()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var service = new OrderService(new InMemoryOrderStore());
        var importer = new JsonOrderImporter();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.ImportAsync(importer, "any.json", cts.Token));
    }

    [Fact]
    public async Task ImportStreamAsync_ReportsProgressCorrectly()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"orders_test_{Guid.NewGuid():N}.json");
        var json = @"[
            {""id"":""ORD-P01"",""customerId"":""C-001"",""status"":""Draft"",""lines"":[]},
            {""id"":""ORD-P02"",""customerId"":""C-002"",""status"":""Draft"",""lines"":[]}
        ]";
        await File.WriteAllTextAsync(tempFile, json);

        try
        {
            var service = new OrderService(new InMemoryOrderStore());
            var importer = new JsonOrderImporter();
            var progressReports = new List<ImportProgressInfo>();
            var progress = new Progress<ImportProgressInfo>(p => progressReports.Add(p));

            int added = await service.ImportStreamAsync(importer, tempFile, progress);

            Assert.Equal(2, added);
            Assert.NotEmpty(progressReports);
            Assert.Equal(100, progressReports.Last().Percent);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }
}
