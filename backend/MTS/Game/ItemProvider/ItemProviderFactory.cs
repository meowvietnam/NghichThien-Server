using MTS.Models;

namespace MTS.Game.ItemProvider
{
    public static class ItemProviderFactory
    {
        public static IItemProvider? GetProvider(ItemType itemType, InventoryManager inventoryManager)
        {
            return GetRawProvider(itemType, inventoryManager) as IItemProvider;
        }

        public static T GetProvider<T>(ItemType itemType, InventoryManager inventoryManager) where T : IItemProvider
        {
            return (T)GetRawProvider(itemType, inventoryManager)!;
        }

        private static object? GetRawProvider(ItemType itemType, InventoryManager inventoryManager)
        {
            return itemType switch
            {
                ItemType.VuKhi => new VuKhiEquipmentProvider(inventoryManager),
                ItemType.PhongCu => new PhongCuEquipmentProvider(inventoryManager),
                ItemType.TrangSuc => new TrangSucEquipmentProvider(inventoryManager),
                ItemType.Herb => new HerbProvider(),
                ItemType.Furnace => new FurnaceProvider(inventoryManager),
                ItemType.LinhDan => new LinhDanProvider(),
                ItemType.Recipe => new RecipeProvider(),
                ItemType.Material => new MaterialProvider(),
                ItemType.ThanThong => new ThanThongBooProvider(),
                ItemType.CongPhap => new CongPhapBookProvider(),
                _ => null,
            };
        }
    }
}