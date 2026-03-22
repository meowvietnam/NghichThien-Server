using MTS.Models;

namespace MTS.Game.CreateCharacter;

public class CreateCharacterService
{
    public CharacterData Char { get; private set; }

    public CreateCharacterService()
    {
        Char = new CharacterData(GameConfig.CharacterTemplate);
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
            throw new Exception(ExceptionMessage.IsIncomplete);
        }
        foreach (var model in models)
        {
            var stepId = model.Id;
            var step = steps.FirstOrDefault(s => s.Id == stepId);
            if (step == null)
            {
                throw new Exception(ExceptionMessage.StepInvalid);
            }

            if (step is SelectionStepData s)
            {
                foreach (var selectionGroup in model.Groups)
                {
                    var group = s.Groups.FirstOrDefault(g => g.Id == selectionGroup.Id);
                    if (group == null)
                    {
                        throw new Exception(ExceptionMessage.GroupInvalid);
                    }

                    var exclusives = group.Exclusives;
                    var isViolation = model.Groups.Any(g => exclusives.Contains(g.Id));
                    if (isViolation)
                    {
                        throw new Exception(ExceptionMessage.GroupExclusiveViolation);
                    }

                    var items = selectionGroup.Items;
                    if (items.Count != 1)
                    {
                        throw new Exception(ExceptionMessage.ItemCountInvalid);
                    }

                    var item = items[0];
                    var statOption = group.StatOptions.FirstOrDefault(x => x.Id == item);
                    if (statOption == null)
                    {
                        throw new Exception(ExceptionMessage.StatOptionInvalid);
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
                        throw new Exception(ExceptionMessage.GroupInvalid);
                    }

                    var count = (int)stats.LinhCan + 1;
                    var items = selectionGroup.Items;
                    if (items.Count != count)
                    {
                        throw new Exception(ExceptionMessage.ElementalCountInvalid);
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
                            throw new Exception(ExceptionMessage.ElementalOptionInvalid);
                        }

                        var modifier = modifiers.FirstOrDefault(x => x.Type == elemental.StatType);
                        if (modifier == null)
                        {
                            throw new Exception(ExceptionMessage.ElementalStatInvalid);
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
            throw new Exception($"{ExceptionMessage.NotEnoughMoney}: {money}");
        }
        Char.Stats = stats;
    }

    private void Apply(CharacterStats stats, StatModifier modifier, ref int money)
    {
        var newMoney = money - modifier.Cost;
        stats.Apply(modifier);
        money = newMoney;
    }
    public object Test(List<SelectionModel> selectionModels)
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
}

public static class ExceptionMessage
{
    public const string StepInvalid = "Bước tạo nhân vật không tồn tại hoặc không hợp lệ!";
    public const string GroupInvalid = "Nhóm lựa chọn không hợp lệ!";
    public const string GroupExclusiveViolation = "Phát hiện các nhóm lựa chọn xung đột lẫn nhau!";
    public const string ElementalInvalid = "Lựa chọn nguyên tố linh căn không hợp lệ!";
    public const string ElementalCountInvalid = "Số lượng linh căn được chọn không khớp với yêu cầu của chỉ số Linh Căn!";
    public const string ElementalOptionInvalid = "Tùy chọn nguyên tố linh căn không tồn tại!";
    public const string ElementalStatInvalid = "Chỉ số nguyên tố linh căn không hợp lệ!";
    public const string SelectionInvalid = "Lựa chọn không hợp lệ!";
    public const string ItemCountInvalid = "Số lượng mục lựa chọn không hợp lệ (yêu cầu chọn duy nhất 1 mục)!";
    public const string StatOptionInvalid = "Tùy chọn chỉ số không tồn tại!";
    public const string NotEnoughMoney = "Điểm thiên phú không đủ để thực hiện thao tác này!";
    public const string IsIncomplete = "Chưa thực hiện đầy đủ các bước tiến trình tạo nhân vật!";
}