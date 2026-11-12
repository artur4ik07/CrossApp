using Core.Configuration;
using Core.Domain;
using Core.Storage;
using Xunit;

namespace Core.Tests;

public class ConfigurationAndStorageTests
{
    [Fact]
    public void StorageOptions_ResolveFullPath_CombinesRelativePathWithBaseDir()
    {
        var tempBase = Path.Combine(Path.GetTempPath(), $"crossapp_base_{Guid.NewGuid():N}");
        try
        {
            var options = new StorageOptions { DataPath = "custom_data", FileName = "orders.json" };
            var resolved = options.ResolveFullPath(tempBase);

            var expected = Path.GetFullPath(Path.Combine(tempBase, "custom_data", "orders.json"));
            Assert.Equal(expected, resolved);
            Assert.True(Directory.Exists(Path.GetDirectoryName(resolved)));
        }
        finally
        {
            if (Directory.Exists(tempBase)) Directory.Delete(tempBase, recursive: true);
        }
    }

    [Fact]
    public void FileOrderStore_SavesCyrillicWithoutEscaping_AndLoadsBack()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"orders_cyr_{Guid.NewGuid():N}.json");
        try
        {
            var store = new FileOrderStore(tempFile);
            var order = Order.Create("ORD-CYR", "C-001");
            order.AddLine("P-1", "Лісова пісня", 159.00m, 1);
            store.Add(order);

            var rawJson = File.ReadAllText(tempFile);
            Assert.Contains("Лісова пісня", rawJson);
            Assert.DoesNotContain(@"\u041B", rawJson);

            var reloadedStore = new FileOrderStore(tempFile);
            var loaded = reloadedStore.GetById("ORD-CYR");
            Assert.NotNull(loaded);
            Assert.Equal("Лісова пісня", loaded.Lines[0].ProductName);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void FileOrderStore_CorruptedJson_ThrowsDomainException()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"orders_bad_{Guid.NewGuid():N}.json");
        File.WriteAllText(tempFile, "{ invalid json !!!");
        try
        {
            var store = new FileOrderStore(tempFile);
            Assert.Throws<DomainException>(() => store.List());
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }
}
