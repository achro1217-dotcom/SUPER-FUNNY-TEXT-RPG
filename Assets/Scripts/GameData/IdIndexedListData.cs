using System;
using System.Collections.Generic;

public abstract class IdIndexedListData<T>
{
    private readonly Dictionary<string, T> _itemById;

    public IReadOnlyList<T> Items { get; }

    protected IdIndexedListData(IReadOnlyList<T> items, Func<T, string> idSelector, string itemLabel)
    {
        if (items == null)
        {
            throw new ArgumentNullException(nameof(items));
        }

        if (idSelector == null)
        {
            throw new ArgumentNullException(nameof(idSelector));
        }

        if (string.IsNullOrWhiteSpace(itemLabel))
        {
            throw new ArgumentException("Item label is required.", nameof(itemLabel));
        }

        Items = items;
        _itemById = BuildIndex(items, idSelector, itemLabel);
    }

    public bool ContainsId(string id)
    {
        ValidateId(id);
        return _itemById.ContainsKey(id);
    }

    public bool TryGetById(string id, out T item)
    {
        ValidateId(id);
        return _itemById.TryGetValue(id, out item);
    }

    public T GetById(string id)
    {
        ValidateId(id);
        if (_itemById.TryGetValue(id, out T item))
        {
            return item;
        }

        throw new KeyNotFoundException($"Item not found. id={id}");
    }

    private static Dictionary<string, T> BuildIndex(IReadOnlyList<T> items, Func<T, string> idSelector, string itemLabel)
    {
        var itemById = new Dictionary<string, T>(items.Count);
        for (int i = 0; i < items.Count; i++)
        {
            T item = items[i];
            if (item == null)
            {
                throw new ArgumentException($"{itemLabel} list cannot contain null entries.", nameof(items));
            }

            string id = idSelector(item);
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException($"{itemLabel} id cannot be null or empty.", nameof(items));
            }

            if (itemById.ContainsKey(id))
            {
                throw new ArgumentException($"Duplicate {itemLabel} id detected. id={id}", nameof(items));
            }

            itemById.Add(id, item);
        }

        return itemById;
    }

    private static void ValidateId(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Id cannot be null or empty.", nameof(id));
        }
    }
}
