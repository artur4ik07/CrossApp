using Core.Reports;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Core;
using Core.Dto;
using Core.Domain;
using Core.Abstractions;
using Core.Storage;
using Core.Services;
using Core.Events;
using Core.Notifications;
using Core.Import;

Console.OutputEncoding = System.Text.Encoding.UTF8;

if (args.Length > 0 && args[0] == "--env")
{
    EnvironmentReport report = EnvironmentInfo.Collect();

    if (args.Contains("--json"))
    {
        var context = new ReportJsonContext(new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });
        Console.WriteLine(JsonSerializer.Serialize(report, context.EnvironmentReport));
        return 0;
    }

    Console.WriteLine("CrossApp – практикум з крос-платформного програмування");
    Console.WriteLine("Студент: Подфедько Артур, група ФЕІ-37");
    Console.WriteLine(new string('-', 52));
    Console.WriteLine($"ОС (OSDescription)  : {report.OsDescription}");
    Console.WriteLine($"ОС (Environment)    : {report.OsVersion}");
    Console.WriteLine($"Архітектура процесу : {report.ProcessArchitecture}");
    Console.WriteLine($"Версія .NET (CLR)   : {report.ClrVersion}");
    Console.WriteLine($"Runtime             : {report.FrameworkDescription}");
    Console.WriteLine($"RID (визначено)     : {report.DetectedRid}");
    Console.WriteLine($"RID (від .NET)      : {report.ReportedRid}");
    Console.WriteLine($"Збірка Core         : {report.BuildNote}");
    Console.WriteLine($"Каталог застосунку  : {report.BaseDirectory}");
    Console.WriteLine($"Поточний каталог    : {report.CurrentDirectory}");
    Console.WriteLine(new string('-', 52));
    Console.WriteLine("Предметна область: Замовлення (клієнт, товар, замовлення, рядок замовлення)");
    return 0;
}

if (args.Length > 0 && args[0] == "--mixed")
{
    string mixedPath = args.Length > 1 ? args[1] : Path.Combine("data", "mixed.csv");

    if (!File.Exists(mixedPath))
    {
        Console.WriteLine($"Файл не знайдено: {Path.GetFullPath(mixedPath)}");
        return 1;
    }

    MixedImportResult mixed = MixedImporter.Load(mixedPath);

    Console.WriteLine($"Товарів: {mixed.Products.Count}");
    foreach (ProductDto p in mixed.Products)
        Console.WriteLine($"  P {p.Id,-6} {p.Name,-25} {p.Price,10:F2}");

    Console.WriteLine($"Клієнтів: {mixed.Customers.Count}");
    foreach (CustomerDto c in mixed.Customers)
        Console.WriteLine($"  C {c.Id,-6} {c.Name,-25} {c.Email}");

    if (mixed.Errors.Count > 0)
    {
        Console.WriteLine($"Пропущено рядків: {mixed.Errors.Count}");
        foreach (string e in mixed.Errors)
            Console.WriteLine($"  ! {e}");
    }

    int mixedTotal = mixed.Products.Count + mixed.Customers.Count + mixed.Errors.Count;
    double mixedErrorRate = mixedTotal == 0 ? 0 : (double)mixed.Errors.Count / mixedTotal * 100;
    Console.WriteLine($"Усього: {mixedTotal}, Прийнято: {mixed.Products.Count + mixed.Customers.Count}, Пропущено: {mixed.Errors.Count}, Помилок: {mixedErrorRate:F1}%");
    return 0;
}

if (args.Length > 0 && args[0] == "--domain")
{
    Console.WriteLine("=== Сценарій 1: успіх ===");
    Order order = Order.Create("ORD-001", "C-001");
    Console.WriteLine(order);
    order.AddLine("P-001", "Кобзар", 249.00m, 2);
    order.AddLine("P-002", "Тіні забутих предків", 189.50m, 1);
    Console.WriteLine(order);
    order.Confirm();
    Console.WriteLine(order);
    Console.WriteLine();

    Console.WriteLine("=== Мапінг ToDto / FromDto ===");
    OrderDto dto = order.ToDto();
    Order restored = Order.FromDto(dto);
    Console.WriteLine($"Відновлено з DTO: {restored}");
    Console.WriteLine();

    Console.WriteLine("=== Сценарій 2: порушення інваріантів ===");
    TryDo("додавання рядка до підтвердженого замовлення", () => order.AddLine("P-003", "Лісова пісня", 159.00m, 1));
    TryDo("нульова кількість у рядку", () => Order.Create("ORD-002", "C-002").AddLine("P-001", "Кобзар", 249.00m, 0));
    TryDo("від'ємна ціна", () => Order.Create("ORD-003", "C-003").AddLine("P-001", "Кобзар", -10m, 1));
    TryDo("порожній customerId", () => Order.Create("ORD-004", "  "));
    TryDo("підтвердження порожнього замовлення", () => Order.Create("ORD-005", "C-005").Confirm());

    Console.WriteLine();
    Console.WriteLine($"Стан замовлення ORD-001 після всіх відмов: {order}");
    return 0;
}

if (args.Length > 0 && args[0] == "--domain-advanced")
{
    Console.WriteLine("=== Завдання 1: ImportResult -> доменні сутності (зв'язок з тижнем 3) ===");
    string productsPath = Path.Combine("data", "sample.csv");
    ImportResult<ProductDto> importedProducts = ProductCsvImporter.Load(productsPath);

    var productsForDemo = new List<ProductDto>(importedProducts.Items)
    {
        new ProductDto("P-999", "Товар з нульовою ціною", 0m)
    };

    ImportResult<OrderLine> lineResult = OrderLineFactory.FromProducts(productsForDemo);

    Console.WriteLine($"З {productsForDemo.Count} товарів створено {lineResult.Items.Count} рядків замовлення");
    foreach (OrderLine line in lineResult.Items.Take(3))
        Console.WriteLine($"  {line.ProductId,-6} {line.ProductName,-30} {line.Price,10:F2}");
    if (lineResult.Errors.Count > 0)
    {
        Console.WriteLine($"Відхилено доменними інваріантами: {lineResult.Errors.Count}");
        foreach (string e in lineResult.Errors)
            Console.WriteLine($"  ! {e}");
    }
    Console.WriteLine();

    Console.WriteLine("=== Завдання 2: інваріант на двох сутностях (ліміт відкритих замовлень клієнта) ===");
    var existingOrders = new List<Order>();
    for (int i = 1; i <= OrderPlacementRules.MaxOpenOrdersPerCustomer; i++)
    {
        Order o = Order.Create($"ORD-C9-{i:000}", "C-009");
        o.AddLine("P-001", "Кобзар", 249.00m, 1);
        existingOrders.Add(o);
    }
    Console.WriteLine($"У клієнта C-009 вже {existingOrders.Count} непідтверджених замовлень");
    TryDo("створення 6-го відкритого замовлення для того самого клієнта", () =>
    {
        OrderPlacementRules.EnsureCanPlaceNewOrder(existingOrders, "C-009");
        Order sixth = Order.Create("ORD-C9-006", "C-009");
        existingOrders.Add(sixth);
    });
    Console.WriteLine();

    Console.WriteLine("=== Завдання 3: явні переходи станів OrderStatus ===");
    Order so = Order.Create("ORD-900", "C-900");
    so.AddLine("P-001", "Кобзар", 249.00m, 1);
    Console.WriteLine($"Початковий стан: {so}");
    so.Confirm();
    Console.WriteLine($"Після Confirm(): {so}");
    TryDo("повторне підтвердження вже підтвердженого замовлення", () => so.Confirm());
    so.Cancel();
    Console.WriteLine($"Після Cancel(): {so}");
    TryDo("додавання рядка до скасованого замовлення", () => so.AddLine("P-002", "Тіні забутих предків", 100m, 1));
    TryDo("підтвердження скасованого замовлення", () => so.Confirm());

    return 0;
}

if (args.Length > 0 && args[0] == "--store")
{
    bool useFile = args.Contains("--file");
    string dataPath = Path.Combine(AppContext.BaseDirectory, "data", "catalog.json");

    IOrderStore store = useFile
        ? new FileOrderStore(dataPath)
        : new InMemoryOrderStore(SampleData.Orders());

    var service = new OrderService(store);

    Console.WriteLine($"Сховище: {store.GetType().Name}");

    Order created = service.CreateOrder("C-900");
    service.AddLine(created.Id, "P-001", "Кобзар", 249.00m, 2);
    service.Confirm(created.Id);

    Console.WriteLine();
    Console.WriteLine("=== Усі замовлення у сховищі ===");
    foreach (Order o in service.All())
        Console.WriteLine($"  {o}");

    Console.WriteLine();
    Console.WriteLine("=== Сценарій відмови ===");
    TryDo("додавання замовлення з id, що вже існує", () =>
    {
        Order dup = Order.Create(created.Id, "C-901");
        store.Add(dup);
    });
    TryDo("AddLine для неіснуючого id замовлення", () => service.AddLine("UNKNOWN-ID", "P-001", "Кобзар", 100m, 1));

    return 0;
}


if (args.Length > 0 && args[0] == "--store-advanced")
{
    Console.WriteLine("=== Завдання 1: декоратор CachingOrderStore ===");
    IOrderStore baseStore = new InMemoryOrderStore(SampleData.Orders());
    var cachingStore = new CachingOrderStore(baseStore);

    IReadOnlyList<Order> firstCall = cachingStore.List();
    Console.WriteLine($"Перший виклик List(): {firstCall.Count} замовлень (читання зі сховища)");

    IReadOnlyList<Order> secondCall = cachingStore.List();
    Console.WriteLine($"Другий виклик List(): {secondCall.Count} замовлень (з кешу, той самий об'єкт списку: {ReferenceEquals(firstCall, secondCall)})");

    cachingStore.Add(Order.Create("ORD-CACHE-01", "C-777"));
    IReadOnlyList<Order> afterAdd = cachingStore.List();
    Console.WriteLine($"Після Add() кеш інвалідовано: {afterAdd.Count} замовлень (новий об'єкт списку: {!ReferenceEquals(firstCall, afterAdd)})");
    Console.WriteLine();

    Console.WriteLine("=== Завдання 2: пошук через Func<Order, bool> ===");
    var service = new OrderService(cachingStore);
    IReadOnlyList<Order> confirmedOnly = service.Search(o => o.IsConfirmed);
    Console.WriteLine($"Підтверджених замовлень: {confirmedOnly.Count}");
    foreach (Order o in confirmedOnly.Take(3))
        Console.WriteLine($"  {o}");

    IReadOnlyList<Order> byCustomer = service.Search(o => o.CustomerId == "C-001");
    Console.WriteLine($"Замовлень клієнта C-001: {byCustomer.Count}");
    foreach (Order o in byCustomer)
        Console.WriteLine($"  {o}");
    Console.WriteLine();

    Console.WriteLine("=== Завдання 3: OrderStoreFactory.Create(args) ===");
    IOrderStore factoryStore = OrderStoreFactory.Create(args);
    Console.WriteLine($"Фабрика створила: {factoryStore.GetType().Name}");
    var factoryService = new OrderService(factoryStore);
    Console.WriteLine($"Записів у сховищі: {factoryService.All().Count}");

    return 0;
}

if (args.Length > 0 && args[0] == "--events")
{
    string logPath = Path.Combine(AppContext.BaseDirectory, "logs", "app.log");
    Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);

    void OnOrderChanged(object? sender, OrderChangedEventArgs e)
    {
        var line = $"{e.At:HH:mm:ss} {e.Kind,-10} {e.OrderId} рядків={e.LinesCount,2} сума={e.Total,10:F2}";
        Console.WriteLine(line);
        File.AppendAllText(logPath, line + Environment.NewLine);
    }

    var store = new InMemoryOrderStore(SampleData.Orders());
    var service = new OrderService(store);

    service.Changed += OnOrderChanged;

    int changes = 0;
    service.Changed += (_, _) => changes++;

    Console.WriteLine("=== Сценарій подій: створення, додавання рядка, підтвердження ===");
    Order created = service.CreateOrder("C-800");
    service.AddLine(created.Id, "P-001", "Кобзар", 249.00m, 2);
    service.Confirm(created.Id);

    Console.WriteLine();
    Console.WriteLine($"Подій отримано: {changes}");

    service.Changed -= OnOrderChanged;

    Console.WriteLine();
    Console.WriteLine("=== Після відписки OnOrderChanged (лямбда-лічильник лишається підписаною) ===");
    Order second = service.CreateOrder("C-801"); // тиха для консолі й логу, але лічильник зросте
    Console.WriteLine($"Подій отримано: {changes}");

    Console.WriteLine();
    Console.WriteLine("=== Сценарій відмови: подія НЕ виникає ===");
    TryDo("AddLine з нульовою кількістю (порушення інваріанту)", () => service.AddLine(second.Id, "P-002", "Тіні забутих предків", 100m, 0));
    TryDo("Confirm неіснуючого id", () => service.Confirm("UNKNOWN-ID"));
    Console.WriteLine($"Подій отримано (без змін після відмов): {changes}");

    Console.WriteLine();
    Console.WriteLine("=== Пошук через Func<Order, bool> (service.Search) ===");
    IReadOnlyList<Order> smallOrders = service.Search(o => o.Lines.Count < 2);
    Console.WriteLine($"Замовлень з менш як 2 рядками: {smallOrders.Count}");
    foreach (Order o in smallOrders.Take(5))
        Console.WriteLine($"  {o}");

    return 0;
}

if (args.Length > 0 && args[0] == "--sink")
{
    string logPath = Path.Combine(AppContext.BaseDirectory, "logs", "sink.log");

    INotificationSink sink = args.Contains("--file-sink")
        ? new FileSink(logPath)
        : new ConsoleSink();

    var store = new InMemoryOrderStore(SampleData.Orders());
    var service = new OrderService(store, sink);

    Console.WriteLine($"Sink: {sink.GetType().Name}");
    Console.WriteLine();

    Order order = service.CreateOrder("C-850");
    service.AddLine(order.Id, "P-001", "Кобзар", 249.00m, 1);
    service.Confirm(order.Id);

    if (sink is FileSink)
    {
        Console.WriteLine();
        Console.WriteLine("Записано у файл, а не в консоль. Вміст:");
        Console.WriteLine(File.ReadAllText(logPath));
    }

    return 0;
}

if (args.Length > 0 && args[0] == "--import-report")
{
    string csvPath = Path.Combine("data", "sample.csv");

    Console.WriteLine("=== Імпорт з негайним звітом через Action<string> ===");
    ImportResult<ProductDto> importResult = ProductCsvImporter.Load(
        csvPath,
        line => Console.WriteLine($"  [SKIPPED] {line}"));

    Console.WriteLine();
    Console.WriteLine($"Завантажено: {importResult.Items.Count}, пропущено: {importResult.Errors.Count}");
    Console.WriteLine("(Той самий перелік помилок доступний і після завершення — у importResult.Errors)");

    return 0;
}
if (args.Length > 0 && args[0].StartsWith("report"))
{
    IOrderStore store = OrderStoreFactory.Create(args);
    var items = store.List();

    switch (args[0])
    {
        case "report-top":
            Console.WriteLine("=== ТОП-5 замовлень за сумою ===");
            PrintOrders(ReportService.TopByTotal(items, 5));
            break;

        case "report-ext":
            Console.WriteLine("=== Власний метод-розширення TopBy (ТОП-3 за кількістю позицій) ===");
            PrintOrders(ReportService.TopByLines(items, 3));
            break;

        case "report-high":
            decimal threshold = 800.00m;
            Console.WriteLine($"=== Замовлення із сумою >= {threshold:F2} ===");
            PrintOrders(ReportService.HighValueOrders(items, threshold));
            break;

        case "report-groups":
            Console.WriteLine("=== Розподіл замовлень за статусами (Method Syntax) ===");
            foreach (var g in ReportService.ByStatus(items))
                Console.WriteLine($"  Статус: {g.Status,-10} Кількість: {g.Count,3}  Загальна сума: {g.TotalAmount,10:F2}");
            break;

        case "report-query":
            Console.WriteLine("=== Розподіл за статусами (Query Syntax: from ... group ... into) ===");
            foreach (var g in ReportService.ByStatusQuerySyntax(items))
                Console.WriteLine($"  Статус: {g.Status,-10} Кількість: {g.Count,3}  Загальна сума: {g.TotalAmount,10:F2}");
            break;

        case "report-join":
            var sampleCustomers = new List<CustomerDto>
            {
                new("C-001", "Шевченко Тарас"),
                new("C-002", "Франко Іван"),
                new("C-003", "Леся Українка"),
                new("C-004", "Коцюбинський Михайло"),
                new("C-005", "Багряний Іван"),
                new("C-999", "Порожній Клієнт Без Замовлень")
            };
            Console.WriteLine("=== GroupJoin: Клієнти x Замовлення ===");
            var joinResult = ReportService.CustomerOrdersReport(sampleCustomers, items);
            foreach (var c in joinResult)
                Console.WriteLine($"  Клієнт: {c.CustomerId,-6} {c.CustomerName,-30} Замовлень: {c.OrdersCount,2}  Загалом: {c.TotalAmount,10:F2}");
            break;

        case "report-bench":
            Console.WriteLine("=== Замір часу Stopwatch на 100 000 згенерованих записах ===");
            var bench = ReportService.RunBenchmark(100_000);
            Console.WriteLine($"  1) З ToList() всередині ланцюжка : {bench.MsWithToList} ms");
            Console.WriteLine($"  2) БЕЗ ToList() всередині (чистий LINQ) : {bench.MsWithoutToList} ms");
            Console.WriteLine($"  Різниця: {bench.MsWithToList - bench.MsWithoutToList} ms (проміжна матеріалізація вимагає зайвого виділення пам'яті)");
            break;

        case "report-sum":
            var t = ReportService.Totals(items);
            Console.WriteLine("=== Підсумкова статистика замовлень ===");
            Console.WriteLine($"  Всього замовлень : {t.Count}");
            Console.WriteLine($"  Загальна сума    : {t.Total:F2}");
            Console.WriteLine($"  Середній чек     : {t.Average:F2}");
            Console.WriteLine($"  Максимальний чек : {t.Max:F2}");
            break;

        case "report-deferred":
            Console.WriteLine("=== Демонстрація відкладеного виконання LINQ ===");
            var source = store.List().ToList();
            var query = source.Where(o => o.Total > 500m);

            Console.WriteLine($"Кількість дорожчих за 500m ДО змін: {query.Count()}");

            Order extra = Order.Create("ORD-TMP-99", "C-999");
            extra.AddLine("P-999", "Тестова дорога книга", 1000m, 1);
            source.Add(extra);

            Console.WriteLine($"Кількість дорожчих за 500m ПІСЛЯ додавання у джерело: {query.Count()}");

            var snapshot = query.ToList();
            source.Clear();
            Console.WriteLine($"У матеріалізованому snapshot після Clear(): {snapshot.Count}");
            break;

        default:
            Console.WriteLine("Доступні команди: report-top | report-ext | report-high | report-groups | report-query | report-join | report-bench | report-sum | report-deferred");
            break;
    }

    return 0;
}

static void PrintOrders(IReadOnlyList<OrderReportLine> lines)
{
    foreach (var l in lines)
        Console.WriteLine($"  {l.OrderId,-10} Клієнт: {l.CustomerId,-6} Рядків: {l.LinesCount,2} Сума: {l.Total,10:F2} [{l.Status}]");
}

string path = args.Length > 0 ? args[0] : Path.Combine("data", "sample.csv");

if (!File.Exists(path))
{
    Console.WriteLine($"Файл не знайдено: {Path.GetFullPath(path)}");
    return 1;
}

ImportResult<ProductDto> result;
try
{
    result = Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".csv" => ProductCsvImporter.Load(path),
        ".json" => ProductJsonImporter.Load(path),
        var ext => throw new NotSupportedException($"Непідтримуване розширення файлу: '{ext}' (очікую .csv або .json)")
    };
}
catch (NotSupportedException ex)
{
    Console.WriteLine(ex.Message);
    return 1;
}

Console.WriteLine($"Завантажено записів: {result.Items.Count}");
foreach (ProductDto p in result.Items.Take(5))
    Console.WriteLine($"  {p.Id,-6} {p.Name,-30} {p.Price,10:F2}");

if (result.Errors.Count > 0)
{
    Console.WriteLine($"Пропущено рядків: {result.Errors.Count}");
    foreach (string e in result.Errors)
        Console.WriteLine($"  ! {e}");
}

Console.WriteLine(result.FormatStatistics());
return 0;

static void TryDo(string title, Action action)
{
    try
    {
        action();
        Console.WriteLine($"  {title}: виняток НЕ спрацював — інваріант відсутній!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  {title}: {ex.GetType().Name} — {ex.Message}");
    }
}

[JsonSerializable(typeof(EnvironmentReport))]
internal partial class ReportJsonContext : JsonSerializerContext
{
}
