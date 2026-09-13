using MTS.Exceptions;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using MTS.Data;
using MTS.Game;
using MTS.Game.CreateCharacter;
using MTS.Localization;
using MTS.Network;
using MTS.Network.Models;
using Network;
using Network.Extensions;
using Serilog;

namespace MTS.Models;

public class Progression : IMemorySerializable<Progression>
{
    [JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public int CharId { get; set; }

    public int Level { get; private set; }
    public int CurrentExp { get; private set; }
    public int CurrentEnergy { get; private set; }
    public int NgoDaoDiem { get; private set; }
    public int IdCurrentNode { get; private set; }
    public int MaxLevel { get; set; }
    [JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    [ForeignKey(nameof(CharId))]
    public CharacterData CharacterData { get; set; }

    [NotMapped] private bool dirty;

    public void Deserialize(IReader reader, ref Progression value)
    {
        value.Level = reader.Read<int>();
        value.CurrentExp = reader.Read<int>();
        value.CurrentEnergy = reader.Read<int>();
        value.NgoDaoDiem = reader.Read<int>();
        value.IdCurrentNode = reader.Read<int>();
        value.MaxLevel = reader.Read<int>();
    }

    public bool IsDefault()
    {
        return CharId == 0;
    }

    public int ByteSize()
    {
        return Level.ByteSize() + CurrentExp.ByteSize() + CurrentEnergy.ByteSize() + NgoDaoDiem.ByteSize() +
               IdCurrentNode.ByteSize() + MaxLevel.ByteSize();
    }

    public void Serialize(IWriter writer)
    {
        writer.Write(Level);
        writer.Write(CurrentExp);
        writer.Write(CurrentEnergy);
        writer.Write(NgoDaoDiem);
        writer.Write(IdCurrentNode);
        writer.Write(MaxLevel);
    }

    public void CopyFrom(Progression other)
    {
        CharId = other.CharId;
        Level = other.Level;
        CurrentExp = other.CurrentExp;
        CurrentEnergy = other.CurrentEnergy;
        NgoDaoDiem = other.NgoDaoDiem;
        IdCurrentNode = other.IdCurrentNode;
        MaxLevel = other.MaxLevel;
        CharacterData = other.CharacterData;
    }

    public async Task SaveChanges(AppDbContext db)
    {
        if (!dirty) return;
        await db.Progressions.Where(x => x.CharId == CharId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.Level, Level)
                .SetProperty(x => x.CurrentExp, CurrentExp)
                .SetProperty(x => x.IdCurrentNode, IdCurrentNode)
                .SetProperty(x => x.NgoDaoDiem, NgoDaoDiem)
                .SetProperty(x => x.CurrentEnergy, CurrentEnergy)
                .SetProperty(x => x.MaxLevel, MaxLevel));
        dirty = false;
    }

    public void SetIdCurrentNode(int id)
    {
        if (IdCurrentNode == id) return;
        IdCurrentNode = id;
        dirty = true;
    }

    public SpecialLevelUpResult SpecialLevelUp()
    {
        var infoLevels = LevelResource.Instance.infoLevels;
        var infoLevel = infoLevels[Level - 1];
        var require = infoLevel.tuviRequireNextLevel;
        if (CurrentExp < require)
        {
            throw new InvalidException(Strings.SpecialLevelUpNotEnoughExp);
        }

        if (Level == infoLevels.Length - 1)
        {
            throw new InvalidException(Strings.SpecialLevelUpMaxLevelReached);
        }
        var itemRequireds = infoLevel.RequireItem;
        if (itemRequireds is { Count: < 1 })
        {
            throw new InvalidException(Strings.SpecialLevelUpNotRequired);
        }
        var inventory = CharacterData.InventoryManager;
        var linhDans = inventory.LinhDans;
        foreach (var itemRequired in itemRequireds)
        {
            if (linhDans.TryGetValue(itemRequired, out var item))
            {
                if (item.Count < itemRequireds.Count)
                {
                    throw new InvalidException(Strings.SpecialLevelUpItemQuantityInvalid);
                }
            }
            else
            {
                throw new InvalidException(Strings.SpecialLevelUpItemNotFound);
            }
        }
        var requires = itemRequireds.Select(x => new ItemRef()
        {
            Id = x.Id,
            Type = x.Type,
            Count = x.Count,
        }).ToArray();
        foreach (var itemRef in requires)
        {
            inventory.Decrease(itemRef);
        }
        CurrentExp = 0;
        SetLevel(Level + 1);
        var buffItems = infoLevel.BuffItem;
        var actualyBuffItems = buffItems.Select(x => new ItemRef()
        {
            Id = x.Id,
            Type = x.Type,
            Count = x.Count,
        }).ToList();
        for (var i = 0; i < buffItems.Count; i++)
        {
            var actualyBuffItem = actualyBuffItems[i];
            if (linhDans.TryGetValue(actualyBuffItem, out var item))
            {
                actualyBuffItem.Count = Math.Min(actualyBuffItem.Count, item.Count);
            }
            else
            {
                actualyBuffItem.Count = 0;
            }
        }
        actualyBuffItems.RemoveAll(x => x.Count == 0);
        foreach (var item in actualyBuffItems)
        {
            try
            {
                CharacterData.BuffDan(item);
            }
            catch
            {
                // ignored
            }
        }

        dirty = true;
        return new SpecialLevelUpResult()
        {
            Success = true,
            StatsSnapshot = new StatsSnapshot(CharacterData.Stats),
            BuffItems = actualyBuffItems,
            CurrentExp = CurrentExp,
            CamNgos = inventory.CamNgos,
        };
    }

    public void SetCurrentExp(int exp)
    {
        if (CurrentExp == exp) return;
        var infoLevels = LevelResource.Instance.infoLevels;
        var remainingExp = exp;
        while (true)
        {
            var infoLevel = infoLevels[Level - 1];
            var require = infoLevel.tuviRequireNextLevel;
            if (remainingExp < require)
            {
                CurrentExp = remainingExp;
                dirty = true;
                return;
            }

            if (Level == infoLevels.Length - 1)
            {
                CurrentExp = require;
                dirty = true;
                return;
            }

            var nextLevelRequireItems = infoLevel.RequireItem;
            if (nextLevelRequireItems is { Count: > 0 })
            {
                CurrentExp = require;
                dirty = true;
                return;
            }
            
            remainingExp -= require;
            CurrentExp = remainingExp;
            SetLevel(Level + 1);
        }
    }

    public void SetCurrentEnergy(int newEnergy)
    {
        if (CurrentEnergy == newEnergy) return;
        CurrentEnergy = newEnergy;
        dirty = true;
    }

    public void SetNgoDaoDiem(int newNgoDaoDiem)
    {
        if (NgoDaoDiem == newNgoDaoDiem) return;
        NgoDaoDiem = newNgoDaoDiem;
        dirty = true;
    }

    public void SetLevel(int level)
    {
        if (Level == level) return;
        if (Level > level)
        {
             Log.Information("{name} gặp lỗi set level hiện tại: {current} level mới: {new}", CharacterData.Name, Level, level);
             throw new InvalidException("Có lỗi xảy ra");
        }
        var infoLevels = LevelResource.Instance.infoLevels;
        var oldLevelInfo = infoLevels[Level - 1];
        Level = level;
        if (Level > MaxLevel)
        {
            MaxLevel = Level;
        }
        var newInfoLevel = infoLevels[level - 1];
        var stats = CharacterData.Stats;
        var maxHp = stats.SinhMenh + oldLevelInfo.hpMaxBuff;
        if (stats.Ability.HasFlag(Ability.ThanMaHuyetMach))
        {
            maxHp += (int)MathF.Ceiling(maxHp * 0.2f);
        }
        if (Level == 4 && stats.Ability.HasFlag(Ability.DiTocHuyetMach))
        {
            maxHp *= 2;
        }
        var thanThuc = stats.ThanThuc + oldLevelInfo.thanThucBuff;
        var docToc = stats.TocDoBay + oldLevelInfo.donTocBuff;
        var thoNguyen = stats.ThoNguyen + oldLevelInfo.thoNguyenBuff;
        var ngoDaoDiemBuff = 1;
        if (stats.Ability.HasFlag(Ability.ChuyenTheTienNhan))
        {
            ngoDaoDiemBuff = 2;
        }
        var ngoDaoDiem = NgoDaoDiem + ngoDaoDiemBuff;
        dirty = true;
        var levelUpdated = new LevelUpdated()
        {
            Level = level,
            CurrentExp = CurrentExp,
            HpMax = maxHp,
            ThanThuc = thanThuc,
            DonToc = docToc,
            ThoNguyen = thoNguyen,
            NewNgoDaoDiem = ngoDaoDiem,
        };
        levelUpdated.ApplyTo(CharacterData);
        var client = CharacterData.Client;
        if (client != null)
        {
            client.MemSend(MessageTag.LevelUpdated, levelUpdated);
        }
    }

    public void OnSetTuoiTac(TimeSpan oldValue, TimeSpan newValue)
    {
        var days1 = (int)(oldValue.TotalDays - 15 * 12 * 30);
        var days2 = (int)(newValue.TotalDays - 15 * 12 * 30);
        var month1 = days1 / 30;
        var month2 = days2 / 30;
        var delta = month2 - month1;
        if (delta <= 0)
        {
            return;
        }
        var buffExp = CharacterData.GetBuffExp(delta, 0);
        SetCurrentExp(CurrentExp + buffExp);
        var client = CharacterData.Client;
        if (client != null)
        {
            var reward = new ExperienceReward()
            {
                AddExp = buffExp,
                NewExp = CurrentExp
            };
            client.MemSend(MessageTag.ExperienceReward, reward);
        }
    }

    public void ResetLevel(int targetLevel)
    {
        Level = targetLevel;
        CurrentExp = 0;
        dirty = true;
    }
}
