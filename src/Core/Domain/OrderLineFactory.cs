using Core.Dto;

namespace Core.Domain;

// Зв'язок із тижнем 3: бере дані, уже прочитані ProductCsvImporter/ProductJsonImporter,
// і намагається перетворити кожен товар на доменний OrderLine. Повторно застосовує
// доменні інваріанти (тут — суворіше правило: ціна > 0, а не лише >= 0, як у CSV-парсері),
// тому елемент, що пройшов імпорт, усе одно може не пройти доменну перевірку.
public static class OrderLineFactory
{
    public static ImportResult<OrderLine> FromProducts(IReadOnlyList<ProductDto> products, int defaultQuantity = 1)
    {
        var items = new List<OrderLine>();
        var errors = new List<string>();

        foreach (ProductDto p in products)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(p.Id))
                    throw new ArgumentException("ідентифікатор товару порожній", nameof(p.Id));
                if (string.IsNullOrWhiteSpace(p.Name))
                    throw new ArgumentException("назва товару порожня", nameof(p.Name));
                if (p.Price <= 0)
                    throw new ArgumentOutOfRangeException(nameof(p.Price), p.Price, "ціна товару має бути більшою за нуль (доменне правило суворіше за імпорт)");

                items.Add(new OrderLine(p.Id, p.Name, p.Price, defaultQuantity));
            }
            catch (Exception ex)
            {
                errors.Add($"{p.Id} ({p.Name}): {ex.Message}");
            }
        }

        return new ImportResult<OrderLine>(items, errors);
    }
}
