using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using MTS.Data;
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

    [NotMapped] private bool isModified;

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
        if (!isModified) return;
        await db.Progressions.Where(x => x.CharId == CharId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.Level, Level)
                .SetProperty(x => x.CurrentExp, CurrentExp)
                .SetProperty(x => x.IdCurrentNode, IdCurrentNode)
                .SetProperty(x => x.CurrentEnergy, CurrentEnergy));
        isModified = false;
    }
    public void SetIdCurrentNode(int id)
    {
        IdCurrentNode = id;
        isModified = true;
    }
    public void SetCurrentEnergy(int newEnergy)
    {
        CurrentEnergy = newEnergy;
        isModified = true;
    }
}