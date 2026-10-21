using Core.Abstractions;

namespace Core.Storage;

// Фабрика: інкапсулює логіку вибору реалізації, яку раніше тримав Program.cs
// напряму (тернарний вираз + ручний new). Composition root тепер викликає
// лише OrderStoreFactory.Create(args), а не збирає реалізацію сам.
public static class OrderStoreFactory
{
    public static IOrderStore Create(string[] args)
    {
        bool useFile = args.Contains("--file");
        bool useCaching = args.Contains("--cache");

        string dataPath = Path.Combine(AppContext.BaseDirectory, "data", "catalog.json");

        IOrderStore store = useFile
            ? new FileOrderStore(dataPath)
            : new InMemoryOrderStore(Core.SampleData.Orders());

        return useCaching ? new CachingOrderStore(store) : store;
    }
}
