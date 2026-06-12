using MemoryPack;
using MTS.Game.Combat;
using MTS.Game.LuyenKhi;

namespace MTS.Network.Models
{
    [MemoryPackable]
    public partial class EquipEquipmentResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public ushort Id { get; set; }
        public EquipmentType TypeEquipment { get; set; }
       
    }
}
