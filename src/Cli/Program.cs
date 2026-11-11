using Core.Import;
using Core.Services;
using Core.Storage;
using Core.Dto;

namespace Cli;

internal class Program
{
    private static async Task Main(string[] args)
    {
        // Перевірка на наявність прапорця --import
        if (args.Length > 0 && args[0].Equals("--import", StringComparison.OrdinalIgnoreCase))
        {
            var dataPath = args.Length > 1 ? args[1] : "data/big.json";

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

            var store = new InMemoryOrderStore();
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
