using System.Net;
using System.Net.Http.Json;
using Api.Contracts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Api.Tests;

public sealed class CustomApiFactory : WebApplicationFactory<Program>, IDisposable
{
    private readonly string _tempFile = Path.Combine(Path.GetTempPath(), $"crossapp-test-{Guid.NewGuid():N}.json");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Storage:DataFile"] = _tempFile
            });
        });
    }

    public new void Dispose()
    {
        base.Dispose();
        if (File.Exists(_tempFile))
            File.Delete(_tempFile);
    }
}

public sealed class OrdersApiTests(CustomApiFactory factory)
    : IClassFixture<CustomApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task PostAndGetByLocation_Returns201And200()
    {
        var request = new SaveOrderRequest(
            "C-900",
            "Draft",
            [new SaveOrderLineRequest("P-10", "Тестовий товар", 125.50m, 2)]);

        var postResponse = await _client.PostAsJsonAsync("/api/orders", request);

        Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);
        Assert.NotNull(postResponse.Headers.Location);

        var getResponse = await _client.GetAsync(postResponse.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var order = await getResponse.Content.ReadFromJsonAsync<OrderResponseDto>();
        Assert.NotNull(order);
        Assert.Equal("C-900", order.CustomerId);
        Assert.Equal(251.00m, order.Total);
    }

    [Fact]
    public async Task PostInvalidOrder_Returns400ValidationProblem()
    {
        var invalidRequest = new SaveOrderRequest("", "InvalidStatus", []);

        var response = await _client.PostAsJsonAsync("/api/orders", invalidRequest);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetOrders_WithPaginationAndSearch_ReturnsPagedResult()
    {
        var response = await _client.GetAsync("/api/orders?page=1&pageSize=3&search=Confirmed");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var paged = await response.Content.ReadFromJsonAsync<PagedOrdersResponse>();
        Assert.NotNull(paged);
        Assert.True(paged.Items.Count <= 3);
        Assert.All(paged.Items, item => Assert.Equal("Confirmed", item.Status));
    }
}
