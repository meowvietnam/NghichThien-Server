using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace MTS.Utils;

public class ValueComparerDict<T,V>
{
    public static ValueComparer<Dictionary<T,V>> Comparer()
    {
        return new ValueComparer<Dictionary<T,V>>(
            (a, b) => Compare(a, b),
            c => GetHash(c),
            c => Snapshot(c)
        );
    }
    private static bool Compare(Dictionary<T, V> a, Dictionary<T, V> b)
    {
        if (a == b) return true;
        if (a == null || b == null || a.Count != b.Count) return false;

        foreach (var kv in a)
        {
            if (!b.TryGetValue(kv.Key, out var value) || !value.Equals(kv.Value))
                return false;
        }
        return true;
    }

    private static int GetHash(Dictionary<T,V> list)
    {
        var hash = new HashCode();
        foreach (var item in list.OrderBy(kv => kv.Key))
        {
            hash.Add(item.Key);
            hash.Add(item.Value);
        }
        return hash.ToHashCode();
    }
    private static Dictionary<T, V> Snapshot(Dictionary<T, V> dict)
        => dict == null ? null : dict.ToDictionary(x => x.Key, x => x.Value);
}