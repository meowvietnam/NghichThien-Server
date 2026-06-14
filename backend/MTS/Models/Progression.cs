using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using MTS.Data;
using MTS.Game;
using MTS.Game.CreateCharacter;
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

    public LevelUpdated? SetCurrentExp(int exp)
    {
        if (CurrentExp == exp) return null;
        var infoLevel = GameConfig.infoLevels[Level - 1];
        var require = infoLevel.tuviRequireNextLevel;
        if (exp < require)
        {
            CurrentExp = exp;
            dirty = true;
            return null;
        }
        if (Level == GameConfig.infoLevels.Length - 1)
        {
            CurrentExp = require;
            dirty = true;
            return null;
        }
        var nextLevelRequireItems = GameConfig.infoLevels[Level].requireItems;
        if (nextLevelRequireItems is { Length: > 0 })
        {
            CurrentExp = require;
            dirty = true;
            return null;
        }
        CurrentExp = exp - require;
        var level = Level + 1;
        return SetLevel(level);
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

    public LevelUpdated? SetLevel(int level)
    {
        if (Level == level) return null;
        Level = level;
        var newInfoLevel = GameConfig.infoLevels[level - 1];
        var stats = CharacterData.Stats;
        var maxHp = stats.SinhMenh + newInfoLevel.hpMaxBuff;
        var thanThuc = stats.ThanThuc + newInfoLevel.thanThucBuff;
        var docToc = stats.TocDoBay + newInfoLevel.donTocBuff;
        var thoNguyen = stats.ThoNguyen + newInfoLevel.thoNguyenBuff;
        var ngoDaoDiem = NgoDaoDiem + 1;
        dirty = true;
        var levelUpdated = new LevelUpdated()
        {
            Level = level,
            CurrentExp = CurrentExp,
            HpMax =  maxHp,
            ThanThuc = thanThuc,
            DonToc =  docToc,
            ThoNguyen = thoNguyen,
            NewNgoDaoDiem = ngoDaoDiem,
        };
        levelUpdated.ApplyTo(CharacterData);
        return levelUpdated;
    }
}