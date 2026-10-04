using System.Globalization;
using Core.Dto;

namespace Core.Import;

public sealed record MixedImportResult(
    IReadOnlyList<ProductDto> Products,
    IReadOnlyList<CustomerDto> Customers,
    IReadOnlyList<string> Errors);

public static class MixedImporter
{
    private const char Separator = ';';

    public static MixedImportResult Load(string path)
    {
        var products = new List<ProductDto>();
        var customers = new List<CustomerDto>();
        var errors = new List<string>();

        string[] lines = File.ReadAllLines(path, System.Text.Encoding.UTF8);

        for (int i = 0; i < lines.Length; i++)
        {
            int number = i + 1;
            string line = lines[i];

            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
                continue;

            switch (ParseLine(line))
            {
                case ParsedProduct p:
                    products.Add(p.Value);
                    break;
                case ParsedCustomer c:
                    customers.Add(c.Value);
                    break;
                case ParsedError e:
                    errors.Add($"рядок {number}: {e.Reason}");
                    break;
            }
        }

        return new MixedImportResult(products, customers, errors);
    }

    private static ParsedLine ParseLine(string line)
    {
        string[] parts = line.Split(Separator, StringSplitOptions.TrimEntries);

        return parts switch
        {
            ["P", var id, var name, var price] when decimal.TryParse(
                price, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal p) && p >= 0
                => new ParsedProduct(new ProductDto(id, name, p)),

            ["P", ..] => new ParsedError("некоректний рядок товару (очікую P;id;name;price)"),

            ["C", var id, var name] => new ParsedCustomer(new CustomerDto(id, name)),
            ["C", var id, var name, var email] => new ParsedCustomer(new CustomerDto(id, name, email)),

            [var prefix, ..] => new ParsedError($"невідомий префікс '{prefix}'"),
            _ => new ParsedError("порожній або некоректний рядок")
        };
    }

    private abstract record ParsedLine;
    private sealed record ParsedProduct(ProductDto Value) : ParsedLine;
    private sealed record ParsedCustomer(CustomerDto Value) : ParsedLine;
    private sealed record ParsedError(string Reason) : ParsedLine;
}
