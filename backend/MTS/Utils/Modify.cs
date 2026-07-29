using MemoryPack;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using MTS.Data;

namespace MTS.Utils;

[MemoryPackable]
public partial class Modify<T> where T : class
{
    public HashSet<T> Adds { get; set; } = new();
    public HashSet<T> Updates { get; set; } = new();
    public HashSet<T> Deletes { get; set; } = new();

    public void Add(T value)
    {
        if (Deletes.Remove(value))
        {
            Replace(Updates, value);
            return;
        }

        if (Updates.Remove(value))
        {
            Replace(Updates, value);
            return;
        }

        Replace(Adds, value);
    }

    public void Update(T value)
    {
        if (Adds.Contains(value))
        {
            Replace(Adds, value);
            return;
        }

        if (Deletes.Contains(value))
            return;

        Replace(Updates, value);
    }

    public void Delete(T value)
    {
        if (Adds.Remove(value))
            return;
        Updates.Remove(value);
        Replace(Deletes, value);
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
    public virtual void Apply(DbSet<T> context)
    {
        if (Deletes.Count > 0)
            context.RemoveRange(Deletes);
        if (Updates.Count > 0)
            context.UpdateRange(Updates);
        if (Adds.Count > 0)
            context.AddRange(Adds);
    }

    public virtual void Apply(AppDbContext db, DbSet<T> context)
    {
        var entityType = db.Model.FindEntityType(typeof(T))
                         ?? throw new InvalidOperationException($"Entity type {typeof(T).Name} is not registered in DbContext.");
        var primaryKey = entityType.FindPrimaryKey()
                         ?? throw new InvalidOperationException($"Entity type {typeof(T).Name} does not have a primary key.");

        var upserts = new Dictionary<EntityKey, T>();
        var deletes = new HashSet<EntityKey>();

        foreach (var value in Deletes)
        {
            deletes.Add(GetKey(primaryKey, value));
        }

        ApplyUpserts(primaryKey, Adds, upserts, deletes);
        ApplyUpserts(primaryKey, Updates, upserts, deletes);

        foreach (var key in deletes)
        {
            if (upserts.ContainsKey(key))
            {
                continue;
            }

            var existing = context.Find(key.Values);
            if (existing != null)
            {
                context.Remove(existing);
            }
        }

        foreach (var (key, value) in upserts)
        {
            var existing = context.Find(key.Values);
            if (existing == null)
            {
                context.Add(value);
                continue;
            }

            db.Entry(existing).CurrentValues.SetValues(value);
        }
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

    private static void Replace(HashSet<T> set, T value)
    {
        set.Remove(value);
        set.Add(value);
    }

    protected virtual bool ShouldDelete(T value)
    {
        return false;
    }

    private void ApplyUpserts(
        IKey primaryKey,
        IEnumerable<T> values,
        Dictionary<EntityKey, T> upserts,
        HashSet<EntityKey> deletes)
    {
        foreach (var value in values)
        {
            var key = GetKey(primaryKey, value);
            if (ShouldDelete(value))
            {
                upserts.Remove(key);
                deletes.Add(key);
                continue;
            }

            deletes.Remove(key);
            upserts[key] = value;
        }
    }

    private static EntityKey GetKey(IKey primaryKey, T value)
    {
        var values = primaryKey.Properties
            .Select(property =>
            {
                var propertyInfo = property.PropertyInfo
                                   ?? throw new InvalidOperationException(
                                       $"Primary key property {property.Name} on entity {typeof(T).Name} is not a CLR property.");
                return propertyInfo.GetValue(value)
                       ?? throw new InvalidOperationException(
                           $"Primary key property {property.Name} on entity {typeof(T).Name} is null.");
            })
            .ToArray();
        return new EntityKey(values);
    }

    private readonly struct EntityKey : IEquatable<EntityKey>
    {
        public object[] Values { get; }

        public EntityKey(object[] values)
        {
            Values = values;
        }

        public bool Equals(EntityKey other)
        {
            return Values.SequenceEqual(other.Values);
        }

        public override bool Equals(object? obj)
        {
            return obj is EntityKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            foreach (var value in Values)
            {
                hashCode.Add(value);
            }

            return hashCode.ToHashCode();
        }
    }
}
