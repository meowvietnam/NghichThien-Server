using MemoryPack;
using Microsoft.EntityFrameworkCore;

namespace MTS.Utils;
[MemoryPackable]
public partial class Modify<T> where T : class
{
    public HashSet<T> Adds { get; set; } = new();
    public HashSet<T> Updates { get; set; }  = new();
    public HashSet<T> Deletes { get; set; }  = new();

    public void Add(T value)
    {
        if (Deletes.Remove(value))
        {
            Updates.Add(value);
            return;
        }
        Adds.Add(value);
    }
    public void Update(T value)
    {
        if (Adds.Contains(value))
            return;

        if (Deletes.Contains(value))
            return;
        Updates.Add(value);
    }

    public void Delete(T value)
    {
        if (Adds.Remove(value))
            return;

        Updates.Remove(value);
        Deletes.Add(value);
    }
    public bool IsModified()
    {
        return Adds.Count > 0 || Updates.Count > 0 || Deletes.Count > 0;
    }
    public void Clear()
    {
        Adds.Clear();
        Updates.Clear();
        Deletes.Clear();
    }
    public void Apply(DbSet<T> context)
    {
        if (Updates.Count > 0)
            context.UpdateRange(Updates);
        if (Deletes.Count > 0)
            context.RemoveRange(Deletes);
        if (Adds.Count > 0)
            context.AddRange(Adds);
    }
    public void Apply(HashSet<T> set)
    {
        if (Deletes.Count > 0) 
        {
            set.ExceptWith(Deletes);
        }
        if (Updates.Count > 0)
        {
            set.ExceptWith(Updates);
            set.UnionWith(Updates);
        }
        if (Adds.Count > 0)
        {
            set.UnionWith(Adds);
        }
    }
}