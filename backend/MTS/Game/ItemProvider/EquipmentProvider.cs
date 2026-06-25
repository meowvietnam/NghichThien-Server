using MTS.Game.LuyenKhi;
using MTS.Models;
using MTS.Network;
using MTS.Utils;

namespace MTS.Game.ItemProvider;

public abstract class EquipmentProvider : ItemProvider<Equipment>
{
    public virtual int BaseValueLevelWithTier => 6;

    public abstract override  ItemType SupportedType { get; }
    public abstract EquipmentType EquipemtType { get; }
    protected InventoryManager inventoryManager;

    public EquipmentProvider(InventoryManager inventoryManager)
    {
        this.inventoryManager = inventoryManager;
    }
    public abstract override Equipment GetDataItemByIdRef(ushort Id);
    protected virtual string GetTextNameEquipment(ArtifactType artifactType)
    {
        return $" - {GetArtifactName(artifactType)}";
    }
    public static string GetArtifactName(ArtifactType artifactType)
    {
        string artifactName = "";
        switch (artifactType)
        {
            case ArtifactType.PhiKiem:
                artifactName = "Phi Kiếm";
                break;
            case ArtifactType.BaoChung:
                artifactName = "Bảo Chung";

                break;
            case ArtifactType.ChiHoan:
                artifactName = "Chỉ Hoàn";

                break;
            case ArtifactType.PhiCham:
                artifactName = "Phi Châm";

                break;
            case ArtifactType.BaoHap:
                artifactName = "Bảo Hạp";

                break;
            case ArtifactType.TruongBao:
                artifactName = "Trường Bào";

                break;
            case ArtifactType.GiapTru:
                artifactName = "Giáp Trụ";

                break;
            case ArtifactType.Linh:
                artifactName = "Linh";

                break;
            case ArtifactType.BaoLenh:
                artifactName = "Bảo Lệnh";
                break;
            case ArtifactType.BaoAn:
                artifactName = "Bảo Ấn";

                break;
            default:
                break;
        }
        return artifactName;
    }

    public override int GetPriceItemSell(ushort id)
    {
        var equipmentData = GetDataItemByIdRef(id);
        if (equipmentData == null) return 0;

        int totalPrice = 0;

        foreach (var item in equipmentData.Effects)
        {
            var materials = equipmentData.GetMaterialsWithEffectType(item.Type);
            int materialCost = materials.Sum(x => GameConfig.PriceVatLieuWithTier[x.Tier]);
            var (min, max) = equipmentData.GetMinMaxValue(item.Type);
            float craftPercent = 0;
            if (max > min)
            {
                float mid = (min + max) / 2f;
                craftPercent = (item.Value - mid) / (max - min);
            }

            float gradeBonus = equipmentData.GradeConfig.MinGrade / 100f;
            float basePriceWithTier = materialCost * (1 + gradeBonus);

            totalPrice += Mathf.RoundToInt(basePriceWithTier * (1 + craftPercent));
        }

        return totalPrice;
    }

    public override int GetPriceItemBuy(ushort id)
    {
        return GetPriceItemSell(id);
    }

}