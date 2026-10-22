namespace Core.Reports;

public static class LinqExtensions
{
    public static IEnumerable<T> TopBy<T, TKey>(
        this IEnumerable<T> source,
        Func<T, TKey> keySelector,
        int count)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(keySelector);

        return source.OrderByDescending(keySelector).Take(count);
    }

    public static IEnumerable<Core.Domain.Order> TopBy<TKey>(
        this IEnumerable<Core.Domain.Order> source,
        Func<Core.Domain.Order, TKey> keySelector,
        int count)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(keySelector);

        return source
            .OrderByDescending(keySelector)
            .ThenBy(o => o.Id, StringComparer.Ordinal)
            .Take(count);
    }
}
