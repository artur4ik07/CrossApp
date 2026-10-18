using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Core;
using Core.Dto;
using Core.Domain;
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
