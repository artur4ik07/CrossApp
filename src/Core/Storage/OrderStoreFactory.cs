using Core.Abstractions;

namespace Core.Storage;

public static class OrderStoreFactory
{
    public static string ResolveDataPath(string[] args, string? defaultDataPath = null)
    {
        int i = Array.IndexOf(args, "--data");
        if (i >= 0 && i + 1 < args.Length)
            return args[i + 1];

        return !string.IsNullOrWhiteSpace(defaultDataPath)
            ? defaultDataPath
            : Path.Combine(AppContext.BaseDirectory, "data", "orders.json");
    }

    public static IOrderStore Create(string[] args, string? defaultDataPath = null)
    {
        bool useFile = args.Contains("--file") || args.Contains("--data");
        bool useCaching = args.Contains("--cache");
        string dataPath = ResolveDataPath(args, defaultDataPath);

        IOrderStore store = useFile
            ? new FileOrderStore(dataPath)
            : new InMemoryOrderStore(Core.SampleData.Orders());

        return useCaching ? new CachingOrderStore(store) : store;
    }
}
