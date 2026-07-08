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
    }

    public bool IsDefault()
    {
        return CharId == 0;
    }

    public int ByteSize()
    {
        return Level.ByteSize() + CurrentExp.ByteSize() + CurrentEnergy.ByteSize() + NgoDaoDiem.ByteSize() +
               IdCurrentNode.ByteSize();
    }

    public void Serialize(IWriter writer)
    {
        writer.Write(Level);
        writer.Write(CurrentExp);
        writer.Write(CurrentEnergy);
        writer.Write(NgoDaoDiem);
        writer.Write(IdCurrentNode);
    }

    public void CopyFrom(Progression other)
    {
        CharId = other.CharId;
        Level = other.Level;
        CurrentExp = other.CurrentExp;
        CurrentEnergy = other.CurrentEnergy;
        NgoDaoDiem = other.NgoDaoDiem;
        IdCurrentNode = other.IdCurrentNode;
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
                .SetProperty(x => x.CurrentEnergy, CurrentEnergy));
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
            throw new Exception(Strings.SpecialLevelUpNotEnoughExp);
        }

        if (Level == infoLevels.Length - 1)
        {
            throw new Exception(Strings.SpecialLevelUpMaxLevelReached);
        }
        var itemRequireds = infoLevel.RequireItem;
        if (itemRequireds is { Count: < 1 })
        {
            throw new Exception(Strings.SpecialLevelUpNotRequired);
        }
        var inventory = CharacterData.InventoryManager;
        var linhDans = inventory.LinhDans;
        foreach (var itemRequired in itemRequireds)
        {
            if (linhDans.TryGetValue(itemRequired, out var item))
            {
                if (item.Count < itemRequireds.Count)
                {
                    throw new Exception(Strings.SpecialLevelUpItemQuantityInvalid);
                }
            }
            else
            {
                throw new Exception(Strings.SpecialLevelUpItemNotFound);
            }
        }
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

        CurrentExp = 0;
        SetLevel(Level + 1);
        dirty = true;
        return new SpecialLevelUpResult()
        {
            Success = true,
            StatsSnapshot = new StatsSnapshot(CharacterData.Stats),
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
        Level = level;
        var infoLevels = LevelResource.Instance.infoLevels;
        var newInfoLevel = infoLevels[level - 1];
        var stats = CharacterData.Stats;
        var maxHp = stats.SinhMenh + newInfoLevel.hpMaxBuff;
        if (stats.Ability.HasFlag(Ability.ThanMaHuyetMach))
        {
            maxHp += (int)MathF.Ceiling(maxHp * 0.2f);
        }

        if (Level == 4 && stats.Ability.HasFlag(Ability.DiTocHuyetMach))
        {
            maxHp *= 2;
        }

        var thanThuc = stats.ThanThuc + newInfoLevel.thanThucBuff;
        var docToc = stats.TocDoBay + newInfoLevel.donTocBuff;
        var thoNguyen = stats.ThoNguyen + newInfoLevel.thoNguyenBuff;
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
        CharacterData.Client.MemSend(MessageTag.LevelUpdated, levelUpdated);
    }
}
