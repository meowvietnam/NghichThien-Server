using MemoryPack;
using MTS.Network.Models;

namespace MTS.Models;

[MemoryPackable]
[MemoryPackUnion(0, typeof(HerbsShop))]
[MemoryPackUnion(1, typeof(MaterialShop))]
[MemoryPackUnion(2, typeof(SkillShop))]
public abstract partial class Shop
{
    public HashSet<ItemRef> Items { get; protected set; }
    public DateTime LastReloadTime { get; protected set; } = DateTime.UtcNow;
    [MemoryPackIgnore]
    public bool Dirty { get; set; }
    public abstract void Reload(ReloadShopRequest request);
    public abstract EShopType GetTypeShop();
    protected virtual ushort RandomCount()
    {
        return (ushort)Random.Shared.Next(1, 6);
    }
    protected virtual int Take()
    {
        return 10;
    }
}