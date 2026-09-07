using MTS.Exceptions;
using MTS.Game.LuyenDan;
using MTS.Localization;
using MTS.Network.Models;
using MTS.Utils;

namespace MTS.Models;

public partial class CharacterData
{
    private const int ConvertDanLuRequiredCount = 4;

    public ConvertDanLuResult ConvertDanLu(ConvertDanLuRequest request)
    {
        var furnaceSelfIds = request.FurnaceSelfIds;
        // Validate: exactly 4 đan lô
        if (furnaceSelfIds == null || furnaceSelfIds.Count != ConvertDanLuRequiredCount)
        {
            throw new InvalidException(Strings.ConvertDanLuItemCountInvalid);
        }
        var furnaceConverts = furnaceSelfIds.Select(x => new FurnaceData()
        {
            SelfId = x
        }).ToList();
        var resources = LuyenDanResources.Instance;

        // Validate each furnace, verify ownership and retrieve its Tier
        var furnaceTiers = furnaceConverts.Select(f =>
        {
            // Must be owned by this character
            if (!InventoryManager.Furnaces.TryGetValue(f, out var owned))
            {
                throw new InvalidException(Strings.ConvertDanLuFurnaceNotOwned);
            }

            var data = resources.TempFurnaceDatas.FirstOrDefault(x => x.Id == owned.Id);
            if (data == null)
            {
                throw new InvalidException(Strings.ConvertDanLuResourceNotFound);
            }

            return new { Furnace = owned, Tier = data.Tier };
        }).ToArray();

        // Determine result tier weighted by input tiers
        var oneRate = 100f / ConvertDanLuRequiredCount;
        var tierCounts = furnaceTiers
            .GroupBy(x => x.Tier)
            .Select(x => new { Tier = x.Key, Count = x.Count() })
            .ToArray();

        var randomItem = new ItemRateRandomizer<Tier>();
        foreach (var tc in tierCounts)
        {
            randomItem.Add(tc.Tier, tc.Count * oneRate);
        }

        // Find a matching recipe (đan phương) for the rolled tier
        RecipeData resultRecipe = null!;
        while (resultRecipe == null && randomItem.Items.Count > 0)
        {
            var tier = randomItem.Random();
            randomItem.Remove(tier);

            var candidates = resources.Recipes
                .Where(x => x.Tier == tier)
                .ToArray();

            if (candidates.Any())
            {
                resultRecipe = candidates.Random();
            }
        }

        if (resultRecipe == null)
        {
            throw new InvalidException(Strings.ConvertDanLuResultNotFound);
        }

        var recipeRef = new ItemRef()
        {
            Type = ItemType.Recipe,
            Id = (ushort)resultRecipe.Id,
            Count = 1
        };

        lock (InventoryManager.SyncRoot)
        {
            // Remove the 4 đan lô
            foreach (var item in furnaceConverts)
            {
                InventoryManager.RemoveFurnace(item);
            }

            // Add the đan phương
            InventoryManager.Add(recipeRef);
        }

        return new ConvertDanLuResult()
        {
            Success = true,
            Message = Strings.ConvertDanLuSuccess,
            FurnaceSelfIds = furnaceSelfIds,
            ItemResult = recipeRef
        };
    }
}
