namespace Core.Dto;

public static class ImportResultExtensions
{
    public static string FormatStatistics<T>(this ImportResult<T> result)
    {
        int total = result.Items.Count + result.Errors.Count;
        double errorRate = total == 0 ? 0 : (double)result.Errors.Count / total * 100;
        return $"Усього: {total}, Прийнято: {result.Items.Count}, Пропущено: {result.Errors.Count}, Помилок: {errorRate:F1}%";
    }
}
