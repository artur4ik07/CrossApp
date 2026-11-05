using Core.Abstractions;

namespace Core.Storage;

public static class OrderStoreFactory
{
    public static string ResolveDataPath(string[] args)
    {
        int i = Array.IndexOf(args, "--data");
        return i >= 0 && i + 1 < args.Length
            ? args[i + 1]
            : Path.Combine(AppContext.BaseDirectory, "data", "catalog.json");
    }

    public static IOrderStore Create(string[] args)
    {
        bool useFile = args.Contains("--file") || args.Contains("--data");
        bool useCaching = args.Contains("--cache");
        string dataPath = ResolveDataPath(args);

        IOrderStore store = useFile
            ? new FileOrderStore(dataPath)
            : new InMemoryOrderStore(Core.SampleData.Orders());

        return useCaching ? new CachingOrderStore(store) : store;
    }
}
