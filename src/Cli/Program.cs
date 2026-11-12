using System.Reflection;
using Core.Configuration;
using Core.Domain;
using Core.Dto;
using Core.Import;
using Core.Services;
using Core.Storage;
using Microsoft.Extensions.Configuration;

namespace Cli;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var envName = Environment.GetEnvironmentVariable("CROSSAPP_ENVIRONMENT") ?? "Development";

        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddJsonFile($"appsettings.{envName}.json", optional: true, reloadOnChange: false)
            .AddUserSecrets(Assembly.GetExecutingAssembly(), optional: true)
            .AddEnvironmentVariables(prefix: "CROSSAPP_")
            .AddCommandLine(args)
            .Build();

        var options = config.Get<AppOptions>() ?? new AppOptions();
        var legacyDataPath = Environment.GetEnvironmentVariable("CROSSAPP_DATA_PATH");

        var storageOptions = new StorageOptions
        {
            DataPath = string.IsNullOrWhiteSpace(legacyDataPath) ? options.Storage.DataPath : legacyDataPath,
            FileName = options.Storage.FileName
        };

        var fullDataPath = storageOptions.ResolveFullPath(AppContext.BaseDirectory);
        var apiKey = config["Integration:ApiKey"];

        Console.WriteLine($"Профіль : {options.EnvironmentName} (Log: {options.Logging.MinimumLevel}, Валюта: {options.Orders.Currency})");
        Console.WriteLine($"Файл даних: {fullDataPath}");
        Console.WriteLine(apiKey is null
            ? "ApiKey : не заданий"
            : $"ApiKey : присутній, довжина {apiKey.Length}");

        // Команда export для ЛР9 (експорт колекції в JSON через FileOrderStore)
        if (args.Length > 0 && args[0].Equals("export", StringComparison.OrdinalIgnoreCase))
        {
            var store = new FileOrderStore(fullDataPath);
            var service = new OrderService(store);

            if (service.All().Count == 0)
            {
                var o1 = service.CreateOrder("C-101");
                service.AddLine(o1.Id, "P-01", "Кобзар", 249.00m, 2);
                service.Confirm(o1.Id);

                var o2 = service.CreateOrder("C-102");
                service.AddLine(o2.Id, "P-02", "Тіні забутих предків", 189.50m, 1);
            }

            Console.WriteLine($"Експортовано {service.All().Count} записів у {fullDataPath}.");
            return;
        }

        // Перевірка на наявність прапорця --import (ЛР8)
        if (args.Length > 0 && args[0].Equals("--import", StringComparison.OrdinalIgnoreCase))
        {
            var dataPath = args.Length > 1 ? args[1] : fullDataPath;

            Console.WriteLine("=== АСИНХРОННИЙ ІМПОРТ ДАНИХ ===");
            Console.WriteLine($"Файл: {dataPath}");
            Console.WriteLine("Для скасування натисніть [Ctrl+C]...\n");

            using var cts = new CancellationTokenSource();

            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                Console.WriteLine("\n\n[CTRL+C] Отримано сигнал скасування! Зупиняємо імпорт...");
                cts.Cancel();
            };

            var store = new FileOrderStore(fullDataPath);
            var service = new OrderService(store);
            var importer = new JsonOrderImporter();

            var progress = new Progress<ImportProgressInfo>(p =>
            {
                Console.Write($"\rОбробка: {p.Processed}/{p.Total} записів ({p.Percent}%)   ");
            });

            try
            {
                int count = await service.ImportStreamAsync(importer, dataPath, progress, cts.Token);
                Console.WriteLine($"\n\nУспішно імпортовано: {count} записів.");
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine($"\n[РЕЗУЛЬТАТ] Операцію імпорту скасовано користувачем!");
                Console.WriteLine($"Збережено записів у сховищі: {service.All().Count}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nПомилка імпорту: {ex.Message}");
            }

            return;
        }

        // Запуск штатного Shell для інших команд CLI
        Shell.Run(args);
    }
}
