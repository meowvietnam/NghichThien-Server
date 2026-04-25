using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using MTS.Data;
using MTS.Game;
using Network;
using Network.Extensions;

namespace MTS.Models;

public class CharacterData : IMemorySerializable<CharacterData>
{
    [Key] public int Id { get; set; }
    public Gender Gender { get; set; }
    public string Name { get; set; }
    public int Level { get; set; } = 1;
    public int CurrentExp { get; set; }
    public CharacterStats Stats { get; set; } = new();
    public Dictionary<string, string> Skins { get; set; } = new();
    public Dictionary<string, Color> Colors { get; set; } = new();
    public EMonPhai MonPhai { get; set; }
    public List<int> NgoDaoSaves { get; set; } = new();
    public Dictionary<int, int> CamNgoSaves { get; set; } = new();
    public int NgoDaoDiem { get; set; }
    public List<int> ThanThongOwner { get; set; } = new();
    public List<int> ThanThongEquip { get; set; } = new();
    public Dictionary<int, int> CongPhapOwner { get; set; } = new();
    public int DonThuatUsed { get; set; }
    public int PhuTu1Used { get; set; }
    public int PhuTu2Used { get; set; }
    public int PhuTu3Used { get; set; }
    public int PhuTu4Used { get; set; }
    public int ChuTu1Used { get; set; }
    public int ChuTu2Used { get; set; }
    public List<int> DanCombatUsed { get; set; } = new();
    public int IdCurrentNode { get; set; }
    public int CurrentEnergy { get; set; }
    public Dictionary<int, int> RelationShip { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<Mail> Mails { get; set; } = new();
    [JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public int UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    [JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public UserData User { get; set; }
    public InventoryManager InventoryManager { get; set; } = new();
    public CharacterData()
    {
    }
    public CharacterData(CharacterData clone)
    {
        Clone1(clone);
    }
    private void Clone0(CharacterData clone)
    {
        Level = clone.Level;
        CurrentExp = clone.CurrentExp;
        MonPhai = clone.MonPhai;
        NgoDaoSaves = new(clone.NgoDaoSaves);
        CamNgoSaves = new(clone.CamNgoSaves);
        NgoDaoDiem = clone.NgoDaoDiem;
        ThanThongOwner = new(clone.ThanThongOwner);
        ThanThongEquip = new(clone.ThanThongEquip);
        CongPhapOwner = new(clone.CongPhapOwner);
        DonThuatUsed = clone.DonThuatUsed;
        PhuTu1Used = clone.PhuTu1Used;
        PhuTu2Used = clone.PhuTu2Used;
        PhuTu3Used = clone.PhuTu3Used;
        PhuTu4Used = clone.PhuTu4Used;
        ChuTu1Used = clone.ChuTu1Used;
        ChuTu2Used = clone.ChuTu2Used;
        DanCombatUsed = new(clone.DanCombatUsed);
        IdCurrentNode = clone.IdCurrentNode;
        CurrentEnergy = clone.CurrentEnergy;
        RelationShip = new(clone.RelationShip);
    }
    private void Clone1(CharacterData clone)
    {
        Id = clone.Id;
        Gender = clone.Gender;
        Name = clone.Name;
        Skins = new(clone.Skins);
        Colors = new(clone.Colors);
        Stats.CopyFrom(clone.Stats);
        InventoryManager.Clone(clone.InventoryManager);
        CreatedAt = clone.CreatedAt;
        Mails = new(clone.Mails);
        Clone0(clone);
    }
    public int ByteSize()
    {
        return Id.ByteSize()
               + Gender.ByteSize() + Name.ByteSize()
               + Level.ByteSize()
               + CurrentExp.ByteSize()
               + Stats.ByteSize()
               + Skins.Keys.ByteSize() + Skins.Values.ByteSize()
               + Colors.Keys.ByteSize() + Colors.Values.ByteSize()
               + MonPhai.ByteSize() + NgoDaoSaves.ByteSize()
               + CamNgoSaves.ByteSize()
               + NgoDaoDiem.ByteSize() + ThanThongOwner.ByteSize() + ThanThongEquip.ByteSize()
               + CongPhapOwner.ByteSize()
               + DonThuatUsed.ByteSize()
               + PhuTu1Used.ByteSize() + PhuTu2Used.ByteSize()
               + PhuTu3Used.ByteSize() + PhuTu4Used.ByteSize()
               + ChuTu1Used.ByteSize() + ChuTu2Used.ByteSize()
               + DanCombatUsed.ByteSize()
               + IdCurrentNode.ByteSize()
               + CurrentEnergy.ByteSize()
               + InventoryManager.ByteSize()
               + RelationShip.ByteSize()
               + CreatedAt.ByteSize()
               + Mails.ByteSize();
    }
    public void Serialize(IWriter writer)
    {
        writer.Write(Id);
        writer.Write(Gender);
        writer.Write(Name);
        writer.Write(Level);
        writer.Write(CurrentExp);
        writer.Write(Stats);
        writer.WriteArray(Skins.Keys);
        writer.WriteArray(Skins.Values);
        writer.WriteArray(Colors.Keys);
        writer.WriteArray(Colors.Values);
        writer.Write(MonPhai);
        writer.WriteArray(NgoDaoSaves);
        writer.WriteArray(CamNgoSaves);
        writer.Write(NgoDaoDiem);
        writer.WriteArray(ThanThongOwner);
        writer.WriteArray(ThanThongEquip);
        writer.WriteArray(CongPhapOwner);
        writer.Write(DonThuatUsed);
        writer.Write(PhuTu1Used);
        writer.Write(PhuTu2Used);
        writer.Write(PhuTu3Used);
        writer.Write(PhuTu4Used);
        writer.Write(ChuTu1Used);
        writer.Write(ChuTu2Used);
        writer.WriteArray(DanCombatUsed);
        writer.Write(IdCurrentNode);
        writer.Write(CurrentEnergy);
        writer.Write(InventoryManager);
        writer.WriteArray(RelationShip);
        writer.Write(CreatedAt);
        writer.WriteArray(Mails);
    }
    public void Deserialize(IReader reader, ref CharacterData value)
    {
        value.Id = reader.Read<int>();
        value.Gender = reader.Read<Gender>();
        value.Name = reader.Read();
        value.Level = reader.Read<int>();
        value.CurrentExp = reader.Read<int>();
        value.Stats = reader.Read<CharacterStats>();
        value.Skins = DictionaryUtils.CreateDictionary(reader.ReadArray(), reader.ReadArray());
        value.Colors = DictionaryUtils.CreateDictionary(reader.ReadArray(), reader.ReadArray<Color>());
        value.MonPhai = reader.Read<EMonPhai>();
        value.NgoDaoSaves = reader.ReadArray<int>().ToList();
        value.CamNgoSaves = reader.ReadArray<KeyValuePair<int, int>>().ToDictionary(x => x.Key, x => x.Value);
        value.NgoDaoDiem = reader.Read<int>();
        value.ThanThongOwner = reader.ReadArray<int>().ToList();
        value.ThanThongEquip = reader.ReadArray<int>().ToList();
        value.CongPhapOwner = reader.ReadArray<KeyValuePair<int, int>>().ToDictionary(x => x.Key, x => x.Value);
        value.DonThuatUsed = reader.Read<int>();
        value.PhuTu1Used = reader.Read<int>();
        value.PhuTu2Used = reader.Read<int>();
        value.PhuTu3Used = reader.Read<int>();
        value.PhuTu4Used = reader.Read<int>();
        value.ChuTu1Used = reader.Read<int>();
        value.ChuTu2Used = reader.Read<int>();
        value.DanCombatUsed = reader.ReadArray<int>().ToList();
        value.IdCurrentNode = reader.Read<int>();
        value.CurrentEnergy = reader.Read<int>();
        value.InventoryManager = reader.Read<InventoryManager>();
        value.RelationShip = reader.ReadArray<KeyValuePair<int, int>>().ToDictionary(x => x.Key, x => x.Value);
        value.CreatedAt = reader.Read<DateTime>();
        value.Mails = reader.ReadArray<Mail>().ToList();
    }
    public bool IsSkinEmpty()
    {
        return Skins.Count == 0 || Colors.Count == 0;
    }
    public bool IsInventoryDefault()
    {
        return InventoryManager.IsDefault();
    }

    public async Task SaveChanges(AppDbContext db)
    {
        var charData = await db.Characters
            .FirstAsync(x => x.Id == Id);
        charData.Clone0(this);
        if (!Stats.IsDefault())
        {
            await Stats.SaveChanges(db);
        }
        if (!InventoryManager.IsDefault())
        {
            await InventoryManager.SaveChanges(db);
        }
    }
}