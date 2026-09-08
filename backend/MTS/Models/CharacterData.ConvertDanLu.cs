using MTS.Exceptions;
using MTS.Game.LuyenDan;
using MTS.Localization;
using MTS.Network.Models;
using MTS.Utils;
using Serilog;

namespace MTS.Models;

public partial class CharacterData
{
    private const int ConvertDanLuRequiredCount = 4;

    public ConvertDanLuResult ConvertDanLu(ConvertDanLuRequest request)
    {
        var recipeItems = request.RecipeItems;

        // Validate: tổng số lượng phải đúng 4 đan phương
        if (recipeItems == null || recipeItems.Count == 0)
        {
            throw new InvalidException(Strings.ConvertDanLuItemCountInvalid);
        }

        var totalCount = recipeItems.Sum(x => x.Count);
        if (totalCount != ConvertDanLuRequiredCount)
        {
            throw new InvalidException(Strings.ConvertDanLuItemCountInvalid);
        }

        if (recipeItems.Any(x => x.Count <= 0))
        {
            throw new InvalidException(Strings.ConvertDanLuItemCountInvalid);
        }

        var resources = LuyenDanResources.Instance;

        // Xác thực từng đan phương và lấy Tier
        var recipeTiers = recipeItems.SelectMany(itemRef =>
        {
            // Phải là loại Recipe
            if (itemRef.Type != ItemType.Recipe)
            {
                throw new InvalidException(Strings.ConvertDanLuResourceNotFound);
            }

            // Phải được sở hữu
            if (!InventoryManager.Recipes.TryGetValue(itemRef, out var owned))
            {
                throw new InvalidException(Strings.ConvertDanLuFurnaceNotOwned);
            }

            if (owned.Count < itemRef.Count)
            {
                throw new InvalidException(Strings.ConvertDanLuFurnaceNotOwned);
            }

            // Lấy dữ liệu RecipeData để biết Tier
            var recipeData = resources.FindRecipe(itemRef.Id);
            if (recipeData == null)
            {
                throw new InvalidException(Strings.ConvertDanLuResourceNotFound);
            }

            // Trả về tier cho từng "đơn vị" đan phương (Count lần)
            return Enumerable.Repeat(recipeData.Tier, itemRef.Count);
        }).ToArray();

        // Tính tỉ lệ tier theo số lượng (chia đều mỗi đan phương)
        var oneRate = 100f / ConvertDanLuRequiredCount;
        var tierCounts = recipeTiers
            .GroupBy(t => t)
            .Select(g => new { Tier = g.Key, Count = g.Count() })
            .ToArray();

        var randomItem = new ItemRateRandomizer<Tier>();
        foreach (var tc in tierCounts)
        {
            randomItem.Add(tc.Tier, tc.Count * oneRate);
        }
        // Tìm ngẫu nhiên đan lô (TempFurnaceData) khớp tier
        TempFurnaceData resultFurnaceData = null!;
        while (resultFurnaceData == null && randomItem.Items.Count > 0)
        {
            var tier = randomItem.Random();
            randomItem.Remove(tier);

            var candidates = resources.TempFurnaceDatas
                .Where(x => x.Tier == tier)
                .ToArray();

            if (candidates.Any())
            {
                resultFurnaceData = candidates.Random();
            }
        }

        if (resultFurnaceData == null)
        {
            throw new InvalidException(Strings.ConvertDanLuResultNotFound);
        }

        // Tạo FurnaceData mới để thêm vào inventory
        var newFurnace = new FurnaceData()
        {
            Id = (ushort)resultFurnaceData.Id,
            Durability = 100,
        };

        lock (InventoryManager.SyncRoot)
        {
            // Trừ các đan phương đầu vào
            foreach (var itemRef in recipeItems)
            {
                InventoryManager.Decrease(itemRef);
            }

            // Thêm đan lô kết quả
            InventoryManager.AddFurnace(newFurnace);
        }

        return new ConvertDanLuResult()
        {
            Success = true,
            Message = Strings.ConvertDanLuSuccess,
            RecipeItems = recipeItems,
            FurnaceResult = newFurnace
        };
    }
}
