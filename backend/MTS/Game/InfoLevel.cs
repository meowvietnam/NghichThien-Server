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
    public List<ItemRef> requireItems;
    public List<ItemRef> buffItems;
    [MemoryPackIgnore]
    public bool IsSpecialLevelUp => requireItems.Count > 0;
}
