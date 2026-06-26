using MemoryPack;
using MTS.Models;

namespace MTS.Network.Models;
[MemoryPackable]
public partial class OwnerIapProduct
{
    public IapProduct Product { get; set; }
    public bool HasPurchased { get; set; }
}