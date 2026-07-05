using MemoryPack;
using MTS.Models;

namespace MTS.Game;

[MemoryPackable]
public partial class InfoLevel
{
    public int tuviRequireNextLevel;
    public int hpMaxBuff;
    public int thanThucBuff;
    public int donTocBuff;
    public int thoNguyenBuff;
    public List<ItemRef> RequireItem;
    public List<ItemRef> BuffItem;
    [MemoryPackIgnore]
    public bool IsSpecialLevelUp => RequireItem.Count > 0;
}
