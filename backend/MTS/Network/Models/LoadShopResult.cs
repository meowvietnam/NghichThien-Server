using MemoryPack;
using MTS.Models;

namespace MTS.Network.Models
{
    [MemoryPackable]
    public partial class LoadShopResult : Result
    {
        public Shop Shop { get; set; }
        public EMonPhai MonPhai { get; set; }
    }
}