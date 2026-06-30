using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using MTS.Data;
using Network;
using Network.Extensions;

namespace MTS.Models;

public class QuestManager : IMemorySerializable<QuestManager>
{
    [JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public int CharId { get; set; }

    public int CurrentMainQuest { get; private set; } = 0;
    public int CurrentCountSideQuest { get; private set; } = 0;
    [NotMapped]
    [JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    private bool dirty = false;
    [ForeignKey(nameof(CharId))]
    [JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public CharacterData CharData { get; set; }

    public void Deserialize(IReader reader, ref QuestManager value)
    {
        value.CurrentMainQuest = reader.Read<int>();
        value.CurrentCountSideQuest = reader.Read<int>();
    }

    public int ByteSize()
    {
        return CurrentMainQuest.ByteSize() + CurrentCountSideQuest.ByteSize();
    }

    public void SetCurrentMainQuest(int mainQuest)
    {
        if (CurrentMainQuest != mainQuest)
        {
            CurrentMainQuest = mainQuest;
            dirty = true;
        }
    }

    public void SetCurrentCountSideQuest(int sideQuest)
    {
        if (CurrentCountSideQuest != sideQuest)
        {
            CurrentCountSideQuest = sideQuest;
            dirty = true;
        }
    }
    public void Serialize(IWriter writer)
    {
        writer.Write(CurrentMainQuest);
        writer.Write(CurrentCountSideQuest);
    }

    public void CopyFrom(QuestManager other)
    {
        CurrentMainQuest = other.CurrentMainQuest;
        CurrentCountSideQuest = other.CurrentCountSideQuest;
        dirty = false;
    }

    public bool IsDefault()
    {
        return CharId == 0;
    }

    public async Task SaveChanges(AppDbContext db)
    {
        if(!dirty) return;
        await db.QuestManagers
            .Where(x => x.CharId == CharId)
            .ExecuteUpdateAsync(s =>
                s.SetProperty(x => x.CurrentMainQuest, CurrentMainQuest)
                    .SetProperty(x => x.CurrentCountSideQuest, CurrentCountSideQuest));
        dirty = false;
    }
}