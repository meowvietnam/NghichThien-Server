using MTS.Game.LuyenKhi;
using MTS.Models;

namespace MTS.Game.ItemProvider
{
    public class PhongCuEquipmentProvider : EquipmentProvider
    {
        public PhongCuEquipmentProvider(InventoryManager inventoryManager) : base(inventoryManager)
        {
        }

        public override ItemType SupportedType => ItemType.PhongCu;

        public override EquipmentType EquipemtType => EquipmentType.Armor;

        protected override string GetTextNameEquipment(ArtifactType artifactType)
        {
            return $"Phòng Cụ {base.GetTextNameEquipment(artifactType)}";
        }

        public override Equipment GetDataItemById(ushort equipmentId)
        {
            return inventoryManager.PhongCus.FirstOrDefault(x => x.SelfId == equipmentId);
        }
    }
}
