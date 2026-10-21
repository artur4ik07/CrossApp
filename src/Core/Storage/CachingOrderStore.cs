using Core.Abstractions;
using Core.Domain;

namespace Core.Storage;

// Декоратор: реалізує той самий контракт IOrderStore, але додає кешування
// результату List() у пам'яті. Кеш скидається (інвалідується) при будь-якій
// зміні через Add/Update/Remove, тому дані ніколи не "застарівають".
public sealed class CachingOrderStore(IOrderStore inner) : IOrderStore
{
    private readonly IOrderStore _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    private IReadOnlyList<Order>? _cachedList;

    public IReadOnlyList<Order> List()
    {
        _cachedList ??= _inner.List();
        return _cachedList;
    }

    public Order? GetById(string id) => _inner.GetById(id);

    public void Add(Order item)
    {
        _inner.Add(item);
        Invalidate();
    }

    public void Update(Order item)
    {
        _inner.Update(item);
        Invalidate();
    }

    public bool Remove(string id)
    {
        bool removed = _inner.Remove(id);
        if (removed)
            Invalidate();
        return removed;
    }

    private void Invalidate() => _cachedList = null;
}
