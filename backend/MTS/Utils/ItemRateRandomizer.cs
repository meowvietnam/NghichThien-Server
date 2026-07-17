using MTS.Models;

namespace MTS.Utils;

public sealed class ItemRate<T>
{
    public T Item { get; }
    public double Rate { get; }

    public ItemRate(T item, double rate)
    {
        if (rate <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(rate), "Rate must be greater than 0.");
        }

        Item = item;
        Rate = rate;
    }
}

public sealed class ItemRateRandomizer<T>
{
    private readonly List<ItemRate<T>> items = new();

    public IReadOnlyList<ItemRate<T>> Items => items;
    public double TotalRate { get; private set; }

    public ItemRateRandomizer()
    {
    }

    public ItemRateRandomizer(IEnumerable<ItemRate<T>> items)
    {
        AddRange(items);
    }

    public ItemRateRandomizer<T> Add(T item, double rate)
    {
        var itemRate = new ItemRate<T>(item, rate);
        items.Add(itemRate);
        TotalRate += itemRate.Rate;
        return this;
    }

    public ItemRateRandomizer<T> AddRange(IEnumerable<ItemRate<T>> values)
    {
        foreach (var value in values)
        {
            Add(value.Item, value.Rate);
        }

        return this;
    }

    public bool Remove(T item)
    {
        var index = items.FindIndex(x => EqualityComparer<T>.Default.Equals(x.Item, item));
        if (index < 0)
        {
            return false;
        }

        TotalRate -= items[index].Rate;
        items.RemoveAt(index);
        return true;
    }

    public T Random()
    {
        if (items.Count == 0)
        {
            throw new InvalidOperationException("Item rate randomizer is empty.");
        }

        var roll = System.Random.Shared.NextDouble() * TotalRate;
        var current = 0d;
        foreach (var item in items)
        {
            current += item.Rate;
            if (roll < current)
            {
                return item.Item;
            }
        }

        return items[^1].Item;
    }

    public List<T> RandomMany(int count)
    {
        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), "Count must be greater than or equal to 0.");
        }

        var result = new List<T>(count);
        for (var i = 0; i < count; i++)
        {
            result.Add(Random());
        }

        return result;
    }

    public void Clear()
    {
        items.Clear();
        TotalRate = 0;
    }

}
