using MTS.Game.LuyenKhi;
using MTS.Models;

namespace MTS.Game.ItemProvider
{
    public class TrangSucEquipmentProvider : EquipmentProvider
    {
        public TrangSucEquipmentProvider(InventoryManager inventoryManager) : base(inventoryManager)
        {
        }

        public override ItemType SupportedType => ItemType.TrangSuc;

        public override EquipmentType EquipemtType => EquipmentType.Jewelry;

        protected override string GetTextNameEquipment(ArtifactType artifactType)
        {

            return $"Trang Sức {base.GetTextNameEquipment(artifactType)}";
        }
        public override Equipment GetDataItemById(ushort equipmentId)
        {
            return inventoryManager.TrangSucs.FirstOrDefault(x => x.SelfId == equipmentId);
        }
    }
}
