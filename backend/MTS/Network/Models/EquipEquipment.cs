using MemoryPack;
using MTS.Game.LuyenKhi;

namespace MTS.Network.Models
{
    [MemoryPackable]
    public partial class EquipEquipment
    {
        public ushort Id { get; set; }
        public EquipmentType TypeEquipment { get; set; }
    }
}
