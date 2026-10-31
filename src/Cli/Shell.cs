using System.Globalization;
using Core;
using Core.Abstractions;
using Core.Domain;
using Core.Dto;
using Core.Events;
using Core.Import;
using Core.Notifications;
using Core.Reports;
using Core.Services;
using Core.Storage;

internal static class Shell
{
    public static int Run(string[] args)
    {
        Console.InputEncoding = System.Text.Encoding.UTF8;

        string logDir = Path.Combine(AppContext.BaseDirectory, "logs");
        Directory.CreateDirectory(logDir);
        string logPath = Path.Combine(logDir, "app.log");
        string skippedPath = Path.Combine(logDir, "import-skipped.log");

        IOrderStore store = OrderStoreFactory.Create(args);
        INotificationSink? sink = CreateSink(args, Path.Combine(logDir, "sink.log"));
        var service = new OrderService(store, sink);

        // Підписник 1: іменований делегат у змінній, щоб можна було відписатися (-=)
        EventHandler<OrderChangedEventArgs> logHandler = (_, e) =>
        {
            string line = $"{e.At:HH:mm:ss} {e.Kind,-10} {e.OrderId} рядків={e.LinesCount,2} сума={e.Total,10:F2}";
            Console.WriteLine($"  [подія] {line}");
            File.AppendAllText(logPath, line + Environment.NewLine);
        };
        bool logging = true;
        service.Changed += logHandler;

        // Підписник 2: лямбда-лічильник із замиканням над змінною events
        int events = 0;
        var customers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        service.Changed += (_, _) => events++;

        Console.WriteLine("=== CrossApp shell (ЛР5-ЛР7) ===");
        Console.WriteLine($"Сховище : {store.GetType().Name}");
        Console.WriteLine($"Sink    : {sink?.GetType().Name ?? "немає"}");
        Console.WriteLine($"Лог подій: {logPath}");
        Console.WriteLine("Команда help покаже всі команди, exit завершує роботу.");

        while (true)
        {
            Console.Write("> ");
            string? input = Console.ReadLine();
            if (input is null)
                break;

            string[] p = Tokenize(input);
            if (p.Length == 0)
                continue;

            try
            {
                if (!Handle(p))
                    break;
            }
            catch (Exception ex)
            {
                // Виняток домену: події НЕ було, бо OnChanged стоїть після успішної зміни
                Console.WriteLine($"  Помилка: {ex.GetType().Name} — {ex.Message}");
            }
        }

        Console.WriteLine($"Подій отримано за сесію: {events}");
        return 0;

        bool Handle(string[] p)
        {
            switch (p[0].ToLowerInvariant())
            {
                case "help":
                    PrintHelp();
                    PrintReportHelp();
                    return true;

                case "exit":
                case "quit":
                    return false;

                case "list":
                {
                    var all = service.All();
                    Console.WriteLine($"  Усього: {all.Count}");
                    foreach (Order o in all)
                        Console.WriteLine($"  {o}");
                    return true;
                }

                case "get":
                {
                    if (p.Length < 2) { Console.WriteLine("  Використання: get <id>"); return true; }
                    Order? found = service.Find(p[1]);
                    Console.WriteLine(found is null ? $"  Замовлення {p[1]} не знайдено." : $"  {found}");
                    return true;
                }

                case "add":
                {
                    if (p.Length < 2) { Console.WriteLine("  Використання: add <customerId>"); return true; }
                    Order created = service.CreateOrder(p[1]);
                    Console.WriteLine($"  Створено: {created}");
                    return true;
                }

                case "line":
                {
                    if (p.Length < 6) { Console.WriteLine("  Використання: line <orderId> <productId> \"назва\" <ціна> <кількість>"); return true; }
                    if (!decimal.TryParse(p[4].Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out decimal price))
                    { Console.WriteLine($"  Ціна '{p[4]}' не є числом."); return true; }
                    if (!int.TryParse(p[5], out int qty))
                    { Console.WriteLine($"  Кількість '{p[5]}' не є цілим числом."); return true; }
                    service.AddLine(p[1], p[2], p[3], price, qty);
                    Console.WriteLine($"  Оновлено: {service.Find(p[1])}");
                    return true;
                }

                case "confirm":
                {
                    if (p.Length < 2) { Console.WriteLine("  Використання: confirm <id>"); return true; }
                    service.Confirm(p[1]);
                    Console.WriteLine($"  Підтверджено: {service.Find(p[1])}");
                    return true;
                }

                case "cancel":
                {
                    if (p.Length < 2) { Console.WriteLine("  Використання: cancel <id>"); return true; }
                    service.Cancel(p[1]);
                    Console.WriteLine($"  Скасовано: {service.Find(p[1])}");
                    return true;
                }

                case "remove":
                {
                    if (p.Length < 2) { Console.WriteLine("  Використання: remove <id>"); return true; }
                    Console.WriteLine(service.Remove(p[1]) ? $"  Видалено {p[1]}." : $"  Замовлення {p[1]} не знайдено.");
                    return true;
                }

                case "search":
                {
                    if (p.Length < 2) { Console.WriteLine("  Використання: search <confirmed|draft|cancelled|customer|min-total|max-total|min-lines> [значення]"); return true; }
                    string? value = p.Length > 2 ? p[2] : null;
                    Func<Order, bool>? predicate = BuildPredicate(p[1].ToLowerInvariant(), value);
                    if (predicate is null)
                    {
                        Console.WriteLine($"  Невідомий або некоректний критерій: '{p[1]}' зі значенням '{value}'.");
                        return true;
                    }
                    IReadOnlyList<Order> found = service.Search(predicate);
                    Console.WriteLine($"  Знайдено: {found.Count}");
                    foreach (Order o in found)
                        Console.WriteLine($"  {o}");
                    return true;
                }

                case "log":
                {
                    string mode = p.Length > 1 ? p[1].ToLowerInvariant() : "";
                    if (mode == "off")
                    {
                        if (logging) { service.Changed -= logHandler; logging = false; }
                        Console.WriteLine("  Логер відписано: консоль і файл тепер мовчать, лічильник працює.");
                    }
                    else if (mode == "on")
                    {
                        if (!logging) { service.Changed += logHandler; logging = true; }
                        Console.WriteLine("  Логер підписано.");
                    }
                    else
                    {
                        Console.WriteLine($"  Логер: {(logging ? "увімкнено" : "вимкнено")}. Використання: log on | log off");
                    }
                    return true;
                }

                case "customer":
                {
                    if (p.Length < 3) { Console.WriteLine("  Використання: customer <id> \"Ім'я\""); return true; }
                    customers[p[1]] = p[2];
                    Console.WriteLine($"  Клієнта {p[1]} ({p[2]}) зареєстровано для report join.");
                    return true;
                }

                case "report":
                {
                    string kind = p.Length > 1 ? p[1].ToLowerInvariant() : "";
                    IReadOnlyList<Order> source = service.All();

                    switch (kind)
                    {
                        case "top":
                        {
                            int n = p.Length > 2 && int.TryParse(p[2], out int parsedN) ? parsedN : 5;
                            Console.WriteLine($"  ТОП-{n} замовлень за сумою:");
                            PrintOrderLines(ReportService.TopByTotal(source, n));
                            return true;
                        }

                        case "ext":
                        {
                            int n = p.Length > 2 && int.TryParse(p[2], out int parsedN) ? parsedN : 3;
                            Console.WriteLine($"  ТОП-{n} замовлень за кількістю позицій (TopBy):");
                            PrintOrderLines(ReportService.TopByLines(source, n));
                            return true;
                        }

                        case "high":
                        {
                            decimal min = p.Length > 2
                                && decimal.TryParse(p[2].Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out decimal parsedMin)
                                ? parsedMin : 800m;
                            Console.WriteLine($"  Замовлення із сумою >= {min:F2}:");
                            PrintOrderLines(ReportService.HighValueOrders(source, min));
                            return true;
                        }

                        case "groups":
                        case "query":
                        {
                            var groups = kind == "groups"
                                ? ReportService.ByStatus(source)
                                : ReportService.ByStatusQuerySyntax(source);
                            Console.WriteLine(kind == "groups"
                                ? "  Розподіл за статусами (method syntax):"
                                : "  Розподіл за статусами (query syntax):");
                            foreach (StatusGroupSummary g in groups)
                                Console.WriteLine($"  Статус: {g.Status,-10} Кількість: {g.Count,3}  Загальна сума: {g.TotalAmount,10:F2}");
                            return true;
                        }

                        case "sum":
                        {
                            var t = ReportService.Totals(source);
                            Console.WriteLine($"  Усього замовлень : {t.Count}");
                            Console.WriteLine($"  Загальна сума    : {t.Total:F2}");
                            Console.WriteLine($"  Середній чек     : {t.Average:F2}");
                            Console.WriteLine($"  Максимальний чек : {t.Max:F2}");
                            return true;
                        }

                        case "join":
                        {
                            var customerList = source.Select(o => o.CustomerId).Distinct()
                                .Select(id => new CustomerDto(id, customers.TryGetValue(id, out var nm) ? nm : "(ім'я не вказано)"))
                                .Concat(customers
                                    .Where(c => !source.Any(o => o.CustomerId.Equals(c.Key, StringComparison.OrdinalIgnoreCase)))
                                    .Select(c => new CustomerDto(c.Key, c.Value)))
                                .ToList();
                            Console.WriteLine("  GroupJoin: клієнти x замовлення (клієнт без замовлень теж лишається):");
                            foreach (CustomerOrdersSummary s in ReportService.CustomerOrdersReport(customerList, source))
                                Console.WriteLine($"  {s.CustomerId,-8} {s.CustomerName,-24} Замовлень: {s.OrdersCount,2}  Загалом: {s.TotalAmount,10:F2}");
                            return true;
                        }

                        case "deferred":
                        {
                            decimal limit = p.Length > 2
                                && decimal.TryParse(p[2].Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out decimal parsedLimit)
                                ? parsedLimit : 500m;

                            var list = service.All().ToList();
                            var query = list.Where(o => o.Total > limit);
                            Console.WriteLine($"  Запит побудовано, але ще не виконано. До змін Count(): {query.Count()}");

                            Order temp = service.CreateOrder("C-DEFERRED");
                            service.AddLine(temp.Id, "P-TMP", "Тимчасовий товар", limit + 1, 1);
                            list.Add(service.Find(temp.Id)!);
                            Console.WriteLine($"  Після додавання реального замовлення {temp.Id} у джерело Count(): {query.Count()}");

                            var snapshot = query.ToList();
                            list.Clear();
                            Console.WriteLine($"  Після Clear(): у знімку (ToList) {snapshot.Count}, у самому запиті Count() {query.Count()}");

                            service.Remove(temp.Id);
                            Console.WriteLine($"  Тимчасове замовлення {temp.Id} видалено зі сховища.");
                            return true;
                        }

                        case "bench":
                        {
                            int count = p.Length > 2 && int.TryParse(p[2], out int parsedCount) ? parsedCount : 100_000;
                            var (withToList, withoutToList) = ReportService.RunBenchmark(count);
                            Console.WriteLine($"  {count} згенерованих записів:");
                            Console.WriteLine($"  1) З ToList() усередині ланцюжка : {withToList} ms");
                            Console.WriteLine($"  2) БЕЗ ToList() усередині        : {withoutToList} ms");
                            return true;
                        }

                        default:
                            PrintReportHelp();
                            return true;
                    }
                }

                case "events":
                    Console.WriteLine($"  Подій отримано лічильником: {events}; логер {(logging ? "увімкнено" : "вимкнено")}.");
                    return true;

                case "import":
                {
                    string csvPath = p.Length > 1 ? p[1] : Path.Combine("data", "sample.csv");
                    string customer = p.Length > 2 ? p[2] : "C-IMPORT";
                    if (!File.Exists(csvPath))
                    {
                        Console.WriteLine($"  Файл не знайдено: {Path.GetFullPath(csvPath)}");
                        return true;
                    }

                    int skipped = 0;
                    // Action<string>: негайний звіт про кожен пропущений рядок під час парсингу
                    ImportResult<ProductDto> result = ProductCsvImporter.Load(csvPath, message =>
                    {
                        skipped++;
                        Console.WriteLine($"  [SKIPPED] {message}");
                        File.AppendAllText(skippedPath, $"{DateTimeOffset.Now:HH:mm:ss} {csvPath}: {message}{Environment.NewLine}");
                    });

                    Order order = service.CreateOrder(customer);
                    foreach (ProductDto product in result.Items)
                        service.AddLine(order.Id, product.Id, product.Name, product.Price, 1);

                    Console.WriteLine($"  Імпортовано {result.Items.Count} позицій у замовлення {order.Id}, пропущено {skipped}.");
                    Console.WriteLine($"  Підсумок: {service.Find(order.Id)}");
                    return true;
                }

                default:
                    Console.WriteLine($"  Невідома команда '{p[0]}'. Введіть help.");
                    return true;
            }
        }
    }

    private static void PrintOrderLines(IReadOnlyList<OrderReportLine> lines)
    {
        if (lines.Count == 0)
        {
            Console.WriteLine("  (порожньо)");
            return;
        }

        foreach (OrderReportLine l in lines)
            Console.WriteLine($"  {l.OrderId,-10} Клієнт: {l.CustomerId,-8} Рядків: {l.LinesCount,2} Сума: {l.Total,10:F2} [{l.Status}]");
    }

    private static void PrintReportHelp()
    {
        Console.WriteLine("  customer <id> \"Ім'я\"                  - зареєструвати клієнта для report join");
        Console.WriteLine("  report top [n]                         - топ-n замовлень за сумою (TopBy)");
        Console.WriteLine("  report ext [n]                         - топ-n за кількістю позицій");
        Console.WriteLine("  report high [мін.сума]                 - замовлення від суми (Where+сортування)");
        Console.WriteLine("  report groups | report query           - розподіл за статусами (GroupBy)");
        Console.WriteLine("  report sum                             - кількість, сума, середній і макс. чек");
        Console.WriteLine("  report join                            - GroupJoin клієнтів і замовлень");
        Console.WriteLine("  report deferred [поріг]                - відкладене виконання на реальних даних");
        Console.WriteLine("  report bench [n]                       - Stopwatch: ToList усередині ланцюжка чи ні");
    }

    private static INotificationSink? CreateSink(string[] args, string sinkLogPath)
    {
        int i = Array.IndexOf(args, "--sink");
        if (i < 0 || i + 1 >= args.Length)
            return null;

        if (args[i + 1] == "console")
            return new ConsoleSink();
        if (args[i + 1] == "file")
            return new FileSink(sinkLogPath);
        return null;
    }

    private static Func<Order, bool>? BuildPredicate(string type, string? value)
    {
        if (type == "confirmed") return o => o.Status == OrderStatus.Confirmed;
        if (type == "draft") return o => o.Status == OrderStatus.Draft;
        if (type == "cancelled") return o => o.Status == OrderStatus.Cancelled;
        if (type == "customer" && value is not null) return o => o.CustomerId == value;

        if (type == "min-total" && value is not null
            && decimal.TryParse(value.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out decimal min))
            return o => o.Total >= min;

        if (type == "max-total" && value is not null
            && decimal.TryParse(value.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out decimal max))
            return o => o.Total <= max;

        if (type == "min-lines" && value is not null && int.TryParse(value, out int lines))
            return o => o.Lines.Count >= lines;

        return null;
    }

    // Розбиває рядок на слова; текст у лапках лишається одним словом
    private static string[] Tokenize(string input)
    {
        var tokens = new List<string>();
        var current = new System.Text.StringBuilder();
        bool inQuotes = false;

        foreach (char c in input)
        {
            if (c == '"')
            {
                inQuotes = !inQuotes;
                continue;
            }

            if (char.IsWhiteSpace(c) && !inQuotes)
            {
                if (current.Length > 0)
                {
                    tokens.Add(current.ToString());
                    current.Clear();
                }
                continue;
            }

            current.Append(c);
        }

        if (current.Length > 0)
            tokens.Add(current.ToString());

        return tokens.ToArray();
    }

    private static void PrintHelp()
    {
        Console.WriteLine("  list                                   - IOrderStore.List()");
        Console.WriteLine("  get <id>                               - IOrderStore.GetById()");
        Console.WriteLine("  add <customerId>                       - створити замовлення (Add + подія Created)");
        Console.WriteLine("  line <id> <productId> \"назва\" <ціна> <к-сть> - додати рядок (Update + подія LineAdded)");
        Console.WriteLine("  confirm <id>                           - підтвердити (подія Confirmed)");
        Console.WriteLine("  cancel <id>                            - скасувати (подія Cancelled)");
        Console.WriteLine("  remove <id>                            - IOrderStore.Remove() (подія Removed)");
        Console.WriteLine("  search <критерій> [значення]           - Search(Func<Order,bool>)");
        Console.WriteLine("       критерії: confirmed, draft, cancelled, customer, min-total, max-total, min-lines");
        Console.WriteLine("  import [файл.csv] [customerId]         - імпорт CSV із негайним звітом Action<string>");
        Console.WriteLine("  log on | log off                       - підписка / відписка логера від події");
        Console.WriteLine("  events                                 - лічильник подій");
        Console.WriteLine("  exit                                   - вихід");
    }
}
