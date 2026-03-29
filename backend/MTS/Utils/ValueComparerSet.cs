using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace MTS.Utils;

public class ValueComparerSet<T>
{
    public static ValueComparer<HashSet<T>> Comparer()
    {
        return new ValueComparer<HashSet<T>>(
            (a, b) => Compare(a, b),
            c => GetHash(c),
            c => Snapshot(c)
        );
    }
    private static bool Compare(HashSet<T> a, HashSet<T> b)
        => a == b || (a != null && b != null && a.SetEquals(b));

    private static int GetHash(HashSet<T> list)
    {
        var hash = new HashCode();
        foreach (var item in list)
            hash.Add(item);
        return hash.ToHashCode();
    }
    private static HashSet<T> Snapshot(HashSet<T> list)
        => list.ToHashSet();
}