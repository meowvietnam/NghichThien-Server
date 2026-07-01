using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using MTS.Data;
using MTS.Game;
using MTS.Game.CreateCharacter;
using MTS.Localization;
using MTS.Models;
using MTS.Network.Models;
using Action = MTS.Game.CreateCharacter.Action;

namespace MTS.Services;

public class CreateCharacterService
{
    public CharacterData Char { get; private set; }

    public CreateCharacterService()
    {
        Char = new CharacterData(GameConfig.CharacterTemplate);
    }

    public async Task SetName(string name)
    {
        await ValidateName(name);
        Char.Name = name;
    }

    public void SetSkin(SetSkinRequest request)
    {
        request.RoundColor(2);
        var gender = request.Gender;
        var skins = request.Skins;
        var colors = request.Colors;
        var newSkins = new Dictionary<string, string>();
        var newColors = new Dictionary<string, Color>();
        var resources = GameConfig.SkinResources;
        var skinGenderGroup = resources.GenderSkinGroups.FirstOrDefault(x => x.Gender == gender);
        if (skinGenderGroup == null)
        {
            throw new Exception(Strings.SkinGenderInvalid);
        }

        foreach (var skinGroup in skinGenderGroup.SkinGroups)
        {
            foreach (var skinGroupCategory in skinGroup.Categories)
            {
                var categoryId = skinGroupCategory.Id;
                if (!skins.TryGetValue(categoryId, out var skin))
                {
                    throw new Exception(Strings.SkinCategoryMissing);
                }

                var existed = skinGroupCategory.Skins.Any(x =>
                    x.Name.Equals(skin, StringComparison.InvariantCultureIgnoreCase));
                if (!existed)
                {
                    throw new Exception(Strings.SkinOptionInvalid);
                }

                newSkins.Add(categoryId, skin);
            }

            foreach (var colorGroup in skinGroup.ColorGroups)
            {
                foreach (var colorId in colorGroup.CategoryIds)
                {
                    if (!colors.TryGetValue(colorId, out var color))
                    {
                        throw new Exception(Strings.SkinColorMissing + $" {colorId}");
                    }

                    color.A = 1;
                    newColors[colorId] = color;
                }
            }
        }

        Char.Gender = gender;
        Char.Appearance.Skins = newSkins;
        Char.Appearance.Colors = newColors;
    }

    private async Task ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new Exception(Strings.NameInvalid);
        }

        name = name.Trim();
        if (name.Length <= 5 || name.Length >= 20)
        {
            throw new Exception(Strings.NameLengthInvalid);
        }

        if (!Regex.IsMatch(name, @"^[\p{L}\p{N}\s]+$"))
        {
            throw new Exception(Strings.NameSpecialCharInvalid);
        }

        await using var db = await Program.CreateDbAsync();
        bool existed = await db.Characters.AnyAsync(x => x.Name == name);
        if (existed)
        {
            throw new Exception(Strings.NameExisted);
        }
    }

    private List<SelectionModel> Clean(List<SelectionModel> models)
    {
        models = models
            .DistinctBy(x => x.Id)
            .OrderBy(x => x.Id)
            .ToList();
        for (var i = 0; i < models.Count; i++)
        {
            var model = models[i];
            model.Groups = model.Groups
                .DistinctBy(x => x.Id)
                .ToList();
            foreach (var selectionGroup in model.Groups)
            {
                selectionGroup.Items = selectionGroup
                    .Items.DistinctBy(x => x)
                    .ToList();
            }
        }

        return models;
    }

    public void ApplyStats(List<SelectionModel> models)
    {
        models = Clean(models);
        var money = GameConfig.OriginalMoney;
        var steps = GameConfig.StepConfig.StepDatas;
        var stats = new CharacterStats(GameConfig.CharacterTemplate.Stats);
        var unselectPercents = GameConfig.UnselectPercents;
        var set1 = steps.Select(x => x.Id).ToHashSet();
        var set2 = models.Select(x => x.Id).ToHashSet();
        if (!set1.SetEquals(set2))
        {
            throw new Exception(Strings.IsIncomplete);
        }

        foreach (var model in models)
        {
            var stepId = model.Id;
            var step = steps.FirstOrDefault(s => s.Id == stepId);
            if (step == null)
            {
                throw new Exception(Strings.StepInvalid);
            }

            if (step is SelectionStepData s)
            {
                foreach (var selectionGroup in model.Groups)
                {
                    var group = s.Groups.FirstOrDefault(g => g.Id == selectionGroup.Id);
                    if (group == null)
                    {
                        throw new Exception(Strings.GroupInvalid);
                    }

                    var exclusives = group.Exclusives;
                    var isViolation = model.Groups.Any(g => exclusives.Contains(g.Id));
                    if (isViolation)
                    {
                        throw new Exception(Strings.GroupExclusiveViolation);
                    }

                    var items = selectionGroup.Items;
                    if (items.Count != 1)
                    {
                        throw new Exception(Strings.ItemCountInvalid);
                    }

                    var item = items[0];
                    var statOption = group.StatOptions.FirstOrDefault(x => x.Id == item);
                    if (statOption == null)
                    {
                        throw new Exception(Strings.StatOptionInvalid);
                    }

                    var modifiers = statOption.Modifiers;
                    foreach (var statModifier in modifiers)
                    {
                        Apply(stats, statModifier, ref money);
                    }
                }
            }
            else if (step is ElementalStepData e)
            {
                foreach (var selectionGroup in model.Groups)
                {
                    var modifiers = Enum.GetValues<ElementalType>()
                        .Select(x => new StatModifier((StatType)(x + 1), Action.Set, 0, 0))
                        .ToList();
                    if (e.GroupId != selectionGroup.Id)
                    {
                        throw new Exception(Strings.GroupInvalid);
                    }

                    var count = (int)stats.LinhCan + 1;
                    var items = selectionGroup.Items;
                    if (items.Count != count)
                    {
                        throw new Exception(Strings.ElementalCountInvalid);
                    }

                    var unselectedCount = modifiers.Count - count;
                    var unselectPercent = unselectedCount > 0 ? unselectPercents[unselectedCount - 1] : 0;
                    var selectPercent = (100 - unselectPercent * unselectedCount) / count;
                    foreach (var statModifier in modifiers)
                    {
                        statModifier.Value = unselectPercent;
                    }

                    foreach (var item in items)
                    {
                        var elemental = e.Elementals.FirstOrDefault(x => x.Id == item);
                        if (elemental == null)
                        {
                            throw new Exception(Strings.ElementalOptionInvalid);
                        }

                        var modifier = modifiers.FirstOrDefault(x => x.Type == elemental.StatType);
                        if (modifier == null)
                        {
                            throw new Exception(Strings.ElementalStatInvalid);
                        }

                        modifier.Value = selectPercent;
                    }

                    foreach (var statModifier in modifiers)
                    {
                        Apply(stats, statModifier, ref money);
                    }
                }
            }
        }

        if (money < 0)
        {
            throw new Exception($"{Strings.NotEnoughNatural}: {money}");
        }
        stats.SetValue(StatType.Hp, stats.SinhMenh);
        Char.Stats = stats;
    }

    private void Apply(CharacterStats stats, StatModifier modifier, ref int money)
    {
        var newMoney = money - modifier.Cost;
        stats.Apply(modifier);
        money = newMoney;
    }

    public object GetSelection(List<SelectionModel> selectionModels)
    {
        var stepConfig = GameConfig.StepConfig;
        var res = new List<object>();
        foreach (var selectionModel in selectionModels)
        {
            var modelId = selectionModel.Id;
            var step = stepConfig.StepDatas.FirstOrDefault(s => s.Id == modelId);
            var gs = new List<object>();
            res.Add(new
            {
                Id = modelId,
                Step = step.Title,
                Groups = gs
            });
            if (step is SelectionStepData s)
            {
                foreach (var selectionModelGroup in selectionModel.Groups)
                {
                    var groupId = selectionModelGroup.Id;
                    var group = s.Groups.FirstOrDefault(g => g.Id == groupId);
                    var items = new List<object>();
                    var g = new
                    {
                        Id = groupId,
                        Items = items
                    };
                    gs.Add(g);
                    foreach (var selectionItem in selectionModelGroup.Items)
                    {
                        var item = group.StatOptions.FirstOrDefault(x => x.Id == selectionItem);
                        items.Add(new
                        {
                            Id = selectionItem,
                            Name = item.Name
                        });
                    }
                }
            }
            else if (step is ElementalStepData e)
            {
                foreach (var selectionModelGroup in selectionModel.Groups)
                {
                    var groupId = selectionModelGroup.Id;
                    if (e.GroupId == groupId)
                    {
                        var items = new List<object>();
                        var g = new
                        {
                            Id = groupId,
                            Items = items
                        };
                        gs.Add(g);
                        foreach (var selectionItem in selectionModelGroup.Items)
                        {
                            var item = e.Elementals.FirstOrDefault(x => x.Id == selectionItem);
                            items.Add(new
                            {
                                Id = selectionItem,
                                Name = item.Name
                            });
                        }
                    }
                }
            }
        }

        return res;
    }

    public async Task<CharacterData> Completed(UserData user)
    {
        var charCount = user.CharacterDatas.Count;
        if (charCount >= 1)
        {
            throw new Exception(Strings.CharacterLimitExceeded);
        }
        var userId = user.Id;
        await using var db = await Program.CreateDbAsync();
        await ValidateName(Char.Name);
        Char.UserId = userId;
        Char.CreatedAt = DateTime.UtcNow;
        var newChar = db.Characters.Add(Char);
        await db.SaveChangesAsync();
        Char = newChar.Entity;
        var inventoryManager = Char.InventoryManager;
        inventoryManager.CharacterData = Char;
        inventoryManager.Init();
        var shop = Char.ShopManager;
        shop.CharData = Char;
        shop.ReloadAll();
        user.CharacterDatas.Add(Char);
        return Char;
    }
}