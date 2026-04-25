using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace MTS.Utils;
public class ValueComparerSet<T> where T : ICustomEquals<T>
{
    public static ValueComparer<HashSet<T>> Comparer()
    {
        return new ValueComparer<HashSet<T>>(
            (a, b) => Compare(a, b),
            c => GetHash(c),
            c => Snapshot(c)
        );
    }
    private static bool Compare(HashSet<T>? a, HashSet<T>? b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a is null || b is null) return false;
        if (a.Count != b.Count) return false;
        return a.All(x => b.Any(y => x.CustomEquals(y)));
    }
    private static int GetHash(HashSet<T>? set)
    {
        if (set == null) return 0;
        var hash = new HashCode();
        foreach (var item in set.OrderBy(x => x))
        {
            hash.Add(item);
        }
        return hash.ToHashCode();
    }

    private static HashSet<T>? Snapshot(HashSet<T>? set)
        => set == null ? null : new HashSet<T>(set);
}