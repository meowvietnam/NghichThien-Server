using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace MTS.Utils;

public class ValueComparerList<T>
{
    public static ValueComparer<List<T>> Comparer()
    {
        return new ValueComparer<List<T>>(
            (a, b) => Compare(a, b),
            c => GetHash(c),
            c => Snapshot(c)
        );
    }
    private static bool Compare(List<T> a, List<T> b)
        => a == b || (a != null && b != null && a.SequenceEqual(b));

    private static int GetHash(List<T> list)
    {
        var hash = new HashCode();
        foreach (var item in list)
            hash.Add(item);
        return hash.ToHashCode();
    }
    private static List<T> Snapshot(List<T> list)
        => list.ToList();
}