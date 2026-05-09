using MTS.Models;
using Network;
using Network.Extensions;

namespace MTS.Network.Models;

public class CharacterSnapshot : IMemorySerializable<CharacterSnapshot>
{
    public int ThanThuc { get; set; }
    public int TocDoBay { get; set; }
    public int NgoTinh { get; set; }
    public int TuChat { get; set; }
    public int ThoNguyen { get; set; }
    public int SinhMenh { get; set; }
    public TimeSpan TuoiTac { get; set; }
    public EMonPhai MonPhai { get; set; }
    public int Level { get; set; }
    public Dictionary<string, Color> Colors { get; set; }
    public Dictionary<string, string> Skins { get; set; }
    public Gender Gender { get; set; }
    public string Name { get; set; }
    public List<int> ThanThongEquip { get; set; }
    public int PhuTu4Used { get; set; }
    public int PhuTu3Used { get; set; }
    public int PhuTu2Used { get; set; }
    public int PhuTu1Used { get; set; }
    public int ChuTu2Used { get; set; }
    public int ChuTu1Used { get; set; }

    public CharacterSnapshot()
    {
    }

    public CharacterSnapshot(CharacterData c)
    {
        var equipment = c.EquipmentManager;
        this.Name = c.Name;
        this.Gender = c.Gender;
        this.Skins = c.Appearance.Skins;
        this.Colors = c.Appearance.Colors;
        this.Level = c.Progression.Level;
        this.MonPhai = c.MonPhai;
        this.TuoiTac = c.Stats.TuoiTac;
        this.SinhMenh = c.Stats.SinhMenh;
        this.ThoNguyen = c.Stats.ThoNguyen;
        this.TuChat = c.Stats.TuChat;
        this.NgoTinh = c.Stats.NgoTinh;
        this.TocDoBay = c.Stats.TocDoBay;
        this.ThanThuc = c.Stats.ThanThuc;
        this.ChuTu1Used = equipment.ChuTu1Used;
        this.ChuTu2Used = equipment.ChuTu2Used;
        this.PhuTu1Used = equipment.PhuTu1Used;
        this.PhuTu2Used = equipment.PhuTu2Used;
        this.PhuTu3Used = equipment.PhuTu3Used;
        this.PhuTu4Used = equipment.PhuTu4Used;
        this.ThanThongEquip = equipment.ThanThongEquips.Select(x => (int)x.Id).ToList();
    }


    public void Deserialize(IReader reader, ref CharacterSnapshot value)
    {
        value.Name = reader.Read();
        value.Gender = reader.Read<Gender>();
        value.Skins = DictionaryUtils.CreateDictionary(reader.ReadArray(), reader.ReadArray());
        value.Colors = DictionaryUtils.CreateDictionary(reader.ReadArray(), reader.ReadArray<Color>());
        value.Level = reader.Read<int>();
        value.MonPhai = reader.Read<EMonPhai>();
        value.TuoiTac = reader.Read<TimeSpan>();
        value.SinhMenh = reader.Read<int>();
        value.ThoNguyen = reader.Read<int>();
        value.TuChat = reader.Read<int>();
        value.NgoTinh = reader.Read<int>();
        value.TocDoBay = reader.Read<int>();
        value.ThanThuc = reader.Read<int>();
        value.ChuTu1Used = reader.Read<int>();
        value.ChuTu2Used = reader.Read<int>();
        value.PhuTu1Used = reader.Read<int>();
        value.PhuTu2Used = reader.Read<int>();
        value.PhuTu3Used = reader.Read<int>();
        value.PhuTu4Used = reader.Read<int>();
        value.ThanThongEquip = reader.ReadArray<int>().ToList();
    }

    public int ByteSize()
    {
        return Name.ByteSize() + Gender.ByteSize()
                               + Skins.Keys.ByteSize() + Skins.Values.ByteSize()
                               + Colors.Keys.ByteSize() + Colors.Values.ByteSize()
                               + Level.ByteSize() + MonPhai.ByteSize() + TuoiTac.ByteSize()
                               + SinhMenh.ByteSize() + ThoNguyen.ByteSize() + TuChat.ByteSize() + NgoTinh.ByteSize()
                               + TocDoBay.ByteSize() + ThanThuc.ByteSize()
                               + ChuTu1Used.ByteSize() + ChuTu2Used.ByteSize()
                               + PhuTu1Used.ByteSize() + PhuTu2Used.ByteSize() + PhuTu3Used.ByteSize()
                               + PhuTu4Used.ByteSize() + ThanThongEquip.ByteSize();
    }

    public void Serialize(IWriter writer)
    {
        writer.Write(Name);
        writer.Write(Gender);
        writer.WriteArray(Skins.Keys);
        writer.WriteArray(Skins.Values);
        writer.WriteArray(Colors.Keys);
        writer.WriteArray(Colors.Values);
        writer.Write(Level);
        writer.Write(MonPhai);
        writer.Write(TuoiTac);
        writer.Write(SinhMenh);
        writer.Write(ThoNguyen);
        writer.Write(TuChat);
        writer.Write(NgoTinh);
        writer.Write(TocDoBay);
        writer.Write(ThanThuc);
        writer.Write(ChuTu1Used);
        writer.Write(ChuTu2Used);
        writer.Write(PhuTu1Used);
        writer.Write(PhuTu2Used);
        writer.Write(PhuTu3Used);
        writer.Write(PhuTu4Used);
        writer.WriteArray(ThanThongEquip);
    }
}