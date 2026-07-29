using MemoryPack;
using Microsoft.EntityFrameworkCore;
using MTS.Data;
using MTS.Models;

namespace MTS.Utils;

[MemoryPackable]
public partial class ModifyItemRef : Modify<ItemRef>
{
    protected override bool ShouldDelete(ItemRef value)
    {
        return value.Count == 0;
    }
}
