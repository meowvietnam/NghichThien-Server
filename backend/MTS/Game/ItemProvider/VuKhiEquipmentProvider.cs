using MTS.Game.LuyenKhi;
using MTS.Models;

namespace MTS.Game.ItemProvider
{
    public class VuKhiEquipmentProvider : EquipmentProvider
    {
        public VuKhiEquipmentProvider(InventoryManager inventoryManager) : base(inventoryManager)
        {
        }

        public override ItemType SupportedType => ItemType.VuKhi;

        public override EquipmentType EquipemtType => EquipmentType.Weapon;

        protected override string GetTextNameEquipment(ArtifactType artifactType)
        {
            return $"Vũ Khí {base.GetTextNameEquipment(artifactType)}";
        }

        public override Equipment GetDataItemById(ushort equipmentId)
        {
            return inventoryManager.VuKhis.FirstOrDefault(x => x.SelfId == equipmentId);
        }
    }
}