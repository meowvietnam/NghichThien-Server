using MTS.Exceptions;
using System.Text.RegularExpressions;
using Google.Apis.AndroidPublisher.v3.Data;
using Microsoft.EntityFrameworkCore;
using MTS.Data;
using MTS.Game;
using MTS.Game.BaseSkill;
using MTS.Game.CreateCharacter;
using MTS.Localization;
using MTS.Models;
using MTS.ND;
using MTS.Network.Models;
using MTS.Utils;
using Newtonsoft.Json;
using Serilog;
using Action = MTS.Game.CreateCharacter.Action;

namespace MTS.Services;

public class CreateCharacterService
{
    public CharacterData Char { get; private set; }
    private int originalMoney;
    private StepConfig config;
    public CreateCharacterService(int money, StepConfig config)
    {
        Char = CharacterData.DeepClone(GameConfig.CharacterTemplate);
        originalMoney = money;
        this.config = config;
    }

    public async Task SetName(string name)
    {
        name = await ValidateName(name);
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
            throw new InvalidException(Strings.SkinGenderInvalid);
        }

        foreach (var skinGroup in skinGenderGroup.SkinGroups)
        {
            foreach (var skinGroupCategory in skinGroup.Categories)
            {
                var categoryId = skinGroupCategory.Id;
                if (!skins.TryGetValue(categoryId, out var skin))
                {
                    throw new InvalidException(Strings.SkinCategoryMissing);
                }

                var existed = skinGroupCategory.Skins.Any(x =>
                    x.Name.Equals(skin, StringComparison.InvariantCultureIgnoreCase));
                if (!existed)
                {
                    throw new InvalidException(Strings.SkinOptionInvalid);
                }

                newSkins.Add(categoryId, skin);
            }

            foreach (var colorGroup in skinGroup.ColorGroups)
            {
                foreach (var colorId in colorGroup.CategoryIds)
                {
                    if (!colors.TryGetValue(colorId, out var color))
                    {
                        throw new InvalidException(Strings.SkinColorMissing + $" {colorId}");
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

    private async Task<string> ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidException(Strings.NameInvalid);
        }

        name = name.Trim();
        if (name.Length <= 5 || name.Length >= 20)
        {
            throw new InvalidException(Strings.NameLengthInvalid);
        }

        if (!Regex.IsMatch(name, @"^[\p{L}\p{N}\s]+$"))
        {
            throw new InvalidException(Strings.NameSpecialCharInvalid);
        }

        await using var db = await Program.CreateDbAsync();
        bool existed = await db.Characters.AnyAsync(x => x.Name == name);
        if (existed)
        {
            throw new InvalidException(Strings.NameExisted);
        }
        return name;
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

    public void ApplyStats(List<SelectionModel> models,CharacterStats template)
    {
        models = Clean(models);
        var money = originalMoney;
        var steps = config.StepDatas;
        var stats = new CharacterStats(template);
        var unselectPercents = GameConfig.UnselectPercents;
        var set1 = steps.Select(x => x.Id).ToHashSet();
        var set2 = models.Select(x => x.Id).ToHashSet();
        if (!set1.SetEquals(set2))
        {
            throw new InvalidException(Strings.IsIncomplete);
        }

        foreach (var model in models)
        {
            var stepId = model.Id;
            var step = steps.FirstOrDefault(s => s.Id == stepId);
            if (step == null)
            {
                throw new InvalidException(Strings.StepInvalid);
            }

            if (step is SelectionStepData s)
            {
                foreach (var selectionGroup in model.Groups)
                {
                    var group = s.Groups.FirstOrDefault(g => g.Id == selectionGroup.Id);
                    if (group == null)
                    {
                        throw new InvalidException(Strings.GroupInvalid);
                    }

                    var exclusives = group.Exclusives;
                    var isViolation = model.Groups.Any(g => exclusives.Contains(g.Id));
                    if (isViolation)
                    {
                        throw new InvalidException(Strings.GroupExclusiveViolation);
                    }

                    var items = selectionGroup.Items;
                    if (items.Count != 1)
                    {
                        throw new InvalidException(Strings.ItemCountInvalid);
                    }

                    var item = items[0];
                    var statOption = group.StatOptions.FirstOrDefault(x => x.Id == item);
                    if (statOption == null)
                    {
                        throw new InvalidException(Strings.StatOptionInvalid);
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
                        throw new InvalidException(Strings.GroupInvalid);
                    }

                    var count = (int)stats.LinhCan + 1;
                    var items = selectionGroup.Items;
                    if (items.Count != count)
                    {
                        throw new InvalidException(Strings.ElementalCountInvalid);
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
                            throw new InvalidException(Strings.ElementalOptionInvalid);
                        }

                        var modifier = modifiers.FirstOrDefault(x => x.Type == elemental.StatType);
                        if (modifier == null)
                        {
                            throw new InvalidException(Strings.ElementalStatInvalid);
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
            throw new InvalidException($"{Strings.NotEnoughNatural}: {money}");
        }
        stats.SetHp(stats.SinhMenh / 3);
        stats.SetLinhThach(stats.LinhThach + GameConfig.NewCharacterLinhThachBonus);
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
        var res = new List<object>();
        foreach (var selectionModel in selectionModels)
        {
            var modelId = selectionModel.Id;
            var step = config.StepDatas.FirstOrDefault(s => s.Id == modelId);
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
            throw new InvalidException(Strings.CharacterLimitExceeded);
        }

        var userId = user.Id;
        await using var db = await Program.CreateDbAsync();
        Char.Name = await ValidateName(Char.Name);
        Char.UserId = userId;
        Char.CreatedAt = DateTime.UtcNow;
        Char.QuestManager.InitDailyQuests();
        var quests = Char.QuestManager.DailyQuests
            .SelectMany(x => x.Value)
            .OrderBy(x => x.LastReload)
            .ToList();
        db.DailyQuests.AddRange(quests);
        Char.ArenaManager.Init();
        Char.MonsterKill.Init();
        var inventory = Char.InventoryManager;
        inventory.CharacterData = Char;
        var stats = Char.Stats;
        stats.CharacterData = Char;
        var equipment = Char.EquipmentManager;
        equipment.CharacterData = Char;
        var eventManger = Char.EventManager;
        eventManger.Events = EventService.CreateEvents();
        var ability = stats.Ability;
        if (ability.HasFlag(Ability.SoKhuyMonKinh))
        {
            var values = Enum.GetValues<ETypeSkill>().ToList();
            values.Remove(ETypeSkill.None);
            var camNgos = values.Select(x => new CamNgo()
            {
                Id = x,
                Value = 1000,
            }).ToList();
            foreach (var camNgo in camNgos)
            {
                inventory.Add(camNgo);
            }
        }

        if (ability.HasFlag(Ability.TeNuiKyNgo))
        {
            var herbs = GameConfig.LuyenDanResources.HerbDatas;
            var herbIds = new[] { 2067, 2077, 2073, 2039 };
            var items = herbIds.Select(x => herbs.First(i => i.Id == x))
                .Select(x => new ItemRef()
                {
                    Id = (ushort)x.Id,
                    Type = ItemType.Herb,
                    Count = 1
                })
                .ToList();
            inventory.ItemRefs.AddRange(items);
        }

        var congPhapId = ability.HasFlag(Ability.GiaTocTuChan) ? (ushort)536 : (ushort)501;
        var congPhap = GameConfig.NDCPTTManager.CongPhaps.FirstOrDefault(x => x.Id == congPhapId);
        var congPhapRef = new CongPhap()
        {
            Id = congPhapId,
            Level = 1,
        };
        inventory.Add(congPhapRef);
        var equipCongPhapRequest = new EquipCongPhap()
        {
            Id = congPhapId,
            Slot = ETypeSlotCP.ChuTu1
        };
        Char.EquipCongPhap(equipCongPhapRequest);
        var thanThongIds = new[] { 1, 201, 101, 301, 401, 501, 519 };
        var thanThongs = thanThongIds
            .Select(x => GameConfig.NDCPTTManager.ThanThongs.First(i => i.Id == x))
            .Select(x => new ThanThong()
            {
                Id = (ushort)x.Id,
            }).ToList();
        for (var i = 0; i < thanThongs.Count; i++)
        {
            var thanThong = thanThongs[i];
            inventory.Add(thanThong);
            var request = new EquipThanThong()
            {
                Id = thanThong.Id,
                Index = (byte)i
            };
            Char.EquipThanThong(request);
        }
        var shop = Char.ShopManager;
        shop.CharData = Char;
        shop.FirstReload();
        var newChar = db.Characters.Add(Char);
        await db.SaveChangesAsync();
        Char = newChar.Entity;
        var inventoryManager = Char.InventoryManager;
        inventoryManager.CharacterData = Char;
        inventoryManager.Init();
        await inventoryManager.Load(db);
        user.CharacterDatas.Add(Char);
        return Char;
    }
}