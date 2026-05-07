namespace MTS.Utils;

using System;
using System.Collections.Generic;

public class IdAllocator<T> where T : struct, IComparable<T>
{
    private readonly SortedSet<(T start, T end)> freeRanges;
    private T nextId;
    private readonly Func<T, T> increment;
    private readonly Func<T, T> decrement;
    public IdAllocator(T start,
                       Func<T, T> increment,
                       Func<T, T> decrement,
                       IComparer<(T, T)>? comparer = null)
    {
        nextId = start;
        this.increment = increment;
        this.decrement = decrement;

        freeRanges = new SortedSet<(T, T)>(
            comparer ?? Comparer<(T, T)>.Create((a, b) =>
            {
                int cmp = a.Item1.CompareTo(b.Item1);
                return cmp != 0 ? cmp : a.Item2.CompareTo(b.Item2);
            })
        );
    }
    public T GetId()
    {
        if (freeRanges.Count > 0)
        {
            var range = freeRanges.Min;
            freeRanges.Remove(range);
            var id = range.start;
            if (range.start.CompareTo(range.end) < 0)
            {
                freeRanges.Add((increment(range.start), range.end));
            }
            return id;
        }

        var result = nextId;
        nextId = increment(nextId);
        return result;
    }

    public void ReleaseId(T id)
    {
        var newRange = (id, id);

        (T start, T end)? left = null;
        (T start, T end)? right = null;

        foreach (var r in freeRanges)
        {
            if (Comparer<T>.Default.Compare(increment(r.end), id) == 0)
                left = r;

            if (Comparer<T>.Default.Compare(decrement(r.start), id) == 0)
                right = r;
        }

        if (left.HasValue)
        {
            freeRanges.Remove(left.Value);
            newRange = (left.Value.start, newRange.Item2);
        }

        if (right.HasValue)
        {
            freeRanges.Remove(right.Value);
            newRange = (newRange.Item1, right.Value.end);
        }

        freeRanges.Add(newRange);
    }

    public void AddUsedId(T id)
    {
        if (id.CompareTo(nextId) >= 0)
        {
            nextId = increment(id);
            return;
        }
        var toUpdate = new List<(T start, T end)>();

        foreach (var r in freeRanges)
        {
            if (r.start.CompareTo(id) <= 0 && r.end.CompareTo(id) >= 0)
            {
                toUpdate.Add(r);
            }
        }
        foreach (var r in toUpdate)
        {
            freeRanges.Remove(r);
            if (r.start.CompareTo(id) < 0)
                freeRanges.Add((r.start, decrement(id)));
            if (r.end.CompareTo(id) > 0)
                freeRanges.Add((increment(id), r.end));
        }
    }
    public IEnumerable<(T start, T end)> GetFreeRanges() => freeRanges;
}