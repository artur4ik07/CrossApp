using System.Text.Json;
using Core.Dto;

namespace Core.Import;

public static class ProductJsonImporter
{
    public static ImportResult<ProductDto> Load(string path)
    {
        string json = File.ReadAllText(path, System.Text.Encoding.UTF8);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        List<ProductDto> raw;
        try
        {
            raw = JsonSerializer.Deserialize<List<ProductDto>>(json, options) ?? [];
        }
        catch (JsonException ex)
        {
            return new ImportResult<ProductDto>([], [$"не вдалося розібрати JSON: {ex.Message}"]);
        }

        var items = new List<ProductDto>();
        var errors = new List<string>();

        for (int i = 0; i < raw.Count; i++)
        {
            int number = i + 1;
            ProductDto p = raw[i];

            string? error = p switch
            {
                { Id.Length: 0 } => "id порожній",
                { Name.Length: 0 } => "назва порожня",
                { Price: < 0 } => $"ціна {p.Price} від'ємна",
                _ => null
            };

            if (error is null)
                items.Add(p);
            else
                errors.Add($"запис {number}: {error}");
        }

        return new ImportResult<ProductDto>(items, errors);
    }
}
