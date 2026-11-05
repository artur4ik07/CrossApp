using System.Runtime.CompilerServices;
using System.Text.Json;
using Core.Abstractions;
using Core.Domain;
using Core.Dto;

namespace Core.Import;

public sealed class JsonOrderImporter : IOrderImporter, IStreamOrderImporter
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

    public async Task<IReadOnlyList<Order>> LoadAsync(string path, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (!File.Exists(path))
            throw new FileNotFoundException($"Файл не знайдено: {path}");

        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);

        List<OrderDto>? dtos;
        try
        {
            dtos = await JsonSerializer.DeserializeAsync<List<OrderDto>>(stream, Options, cancellationToken: ct);
        }
        catch (JsonException ex)
        {
            throw new DomainException($"Помилка структури JSON у файлі {Path.GetFileName(path)}", ex);
        }

        if (dtos is null)
            throw new DomainException($"Файл {Path.GetFileName(path)} порожній або містить некоректний JSON.");

        var result = new List<Order>(dtos.Count);
        for (int i = 0; i < dtos.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                result.Add(Order.FromDto(dtos[i]));
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
            {
                throw new DomainException($"Запис {i + 1}: {ex.Message}", ex);
            }
        }

        return result;
    }

    public async IAsyncEnumerable<Order> LoadStreamAsync(
        string path, 
        IProgress<ImportProgressInfo>? progress = null, 
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (!File.Exists(path))
            throw new FileNotFoundException($"Файл не знайдено: {path}");

        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);

        List<OrderDto>? dtos;
        try
        {
            dtos = await JsonSerializer.DeserializeAsync<List<OrderDto>>(stream, Options, cancellationToken: ct);
        }
        catch (JsonException ex)
        {
            throw new DomainException($"Помилка структури JSON у файлі {Path.GetFileName(path)}", ex);
        }

        if (dtos is null)
            yield break;

        int total = dtos.Count;
        for (int i = 0; i < total; i++)
        {
            ct.ThrowIfCancellationRequested();
            Order order;
            try
            {
                order = Order.FromDto(dtos[i]);
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
            {
                throw new DomainException($"Запис {i + 1}: {ex.Message}", ex);
            }

            int processed = i + 1;
            int percent = (int)((double)processed / total * 100);
            progress?.Report(new ImportProgressInfo(processed, total, percent));

            yield return order;
        }
    }
}
