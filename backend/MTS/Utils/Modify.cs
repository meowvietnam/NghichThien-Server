using Microsoft.EntityFrameworkCore;

namespace MTS.Utils;

public class Modify<T> where T : class
{
    private readonly HashSet<T> _adds = new();
    private readonly HashSet<T> _updates = new();
    private readonly HashSet<T> _deletes = new();

    public IReadOnlyCollection<T> Adds => _adds;
    public IReadOnlyCollection<T> Updates => _updates;
    public IReadOnlyCollection<T> Deletes => _deletes;

    public void Add(T value)
    {
        if (_deletes.Remove(value))
        {
            _updates.Add(value);
            return;
        }
        _adds.Add(value);
    }
    public void Update(T value)
    {
        if (_adds.Contains(value))
            return;

        if (_deletes.Contains(value))
            return;
        _updates.Add(value);
    }

    public void Delete(T value)
    {
        if (_adds.Remove(value))
            return;

        _updates.Remove(value);
        _deletes.Add(value);
    }
    public bool IsModified()
    {
        return _adds.Count > 0 || _updates.Count > 0 || _deletes.Count > 0;
    }
    public void Clear()
    {
        _adds.Clear();
        _updates.Clear();
        _deletes.Clear();
    }
    public void Apply(DbSet<T> context)
    {
        if (_updates.Count > 0)
            context.UpdateRange(_updates);
        if (_deletes.Count > 0)
            context.RemoveRange(_deletes);
        if (_adds.Count > 0)
            context.AddRange(_adds);
    }
}