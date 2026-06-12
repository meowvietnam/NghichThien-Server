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
    public int Hp { get; set; }
    public int DanDoc { get; set; }
    public Ability Ability { get; set; }
    public float Kim { get; set; }
    public float Moc { get; set; }
    public float Thuy { get; set; }
    public float Hoa { get; set; }
    public float Tho { get; set; }
    public float Ma { get; set; }
    public HashSet<NgoDao> NgoDaos { get; set; }
    public HashSet<DanCombatUsed> DanCombatUseds { get; set; }
    public Equipment VuKhi { get; set; }
    public Equipment PhongCu { get; set; }
    public Equipment TrangSuc { get; set; }
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

        this.DanCombatUseds = equipment.DanCombatUseds;
        this.NgoDaos = c.InventoryManager.NgoDaos;
        this.VuKhi = c.InventoryManager.VuKhis.FirstOrDefault(x=>x.Id == c.EquipmentManager.VuKhi);
        this.PhongCu = c.InventoryManager.PhongCus.FirstOrDefault(x => x.Id == c.EquipmentManager.PhongCu); 
        this.TrangSuc = c.InventoryManager.TrangSucs.FirstOrDefault(x => x.Id == c.EquipmentManager.TrangSuc);
        this.Kim = c.Stats.Kim;
        this.Moc = c.Stats.Moc;
        this.Thuy = c.Stats.Thuy;
        this.Hoa = c.Stats.Hoa;
        this.Tho = c.Stats.Tho;
        this.Ma = c.Stats.Ma;
        this.Ability = c.Stats.Ability;
        this.DanDoc = c.Stats.DanDoc;
        this.Hp = c.Stats.Hp;
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

        value.DanCombatUseds = reader.ReadArray<DanCombatUsed>().ToHashSet();
        value.NgoDaos = reader.ReadArray<NgoDao>().ToHashSet();
        // Thay thế 3 dòng value.VuKhi = reader.Read<Equipment>(); cũ:
        bool hasVuKhi = reader.Read<bool>();
        if (hasVuKhi)
        {
            if (value.VuKhi == null) value.VuKhi = new Equipment();
            var equipRef = value.VuKhi;
            equipRef.Deserialize(reader, ref equipRef);
            value.VuKhi = equipRef;
        }
        else value.VuKhi = null!;

        bool hasPhongCu = reader.Read<bool>();
        if (hasPhongCu)
        {
            if (value.PhongCu == null) value.PhongCu = new Equipment();
            var equipRef = value.PhongCu;
            equipRef.Deserialize(reader, ref equipRef);
            value.PhongCu = equipRef;
        }
        else value.PhongCu = null!;

        bool hasTrangSuc = reader.Read<bool>();
        if (hasTrangSuc)
        {
            if (value.TrangSuc == null) value.TrangSuc = new Equipment();
            var equipRef = value.TrangSuc;
            equipRef.Deserialize(reader, ref equipRef);
            value.TrangSuc = equipRef;
        }
        else value.TrangSuc = null!;
        value.Kim = reader.Read<float>();
        value.Moc = reader.Read<float>();
        value.Thuy = reader.Read<float>();
        value.Hoa = reader.Read<float>();
        value.Tho = reader.Read<float>();
        value.Ma = reader.Read<float>();
        value.Ability = reader.Read<Ability>();
        value.DanDoc = reader.Read<int>();
        value.Hp = reader.Read<int>();
    }

    public int ByteSize()
    {
        int size = Name.ByteSize() + Gender.ByteSize();

        // Check an toàn cho Dictionary ngoại hình
        size += (Skins != null && Skins.Keys != null) ? (Skins.Keys.ByteSize() + Skins.Values.ByteSize()) : 0;
        size += (Colors != null && Colors.Keys != null) ? (Colors.Keys.ByteSize() + Colors.Values.ByteSize()) : 0;

        size += Level.ByteSize() + MonPhai.ByteSize() + TuoiTac.ByteSize();
        size += SinhMenh.ByteSize() + ThoNguyen.ByteSize() + TuChat.ByteSize() + NgoTinh.ByteSize();
        size += TocDoBay.ByteSize() + ThanThuc.ByteSize();
        size += ChuTu1Used.ByteSize() + ChuTu2Used.ByteSize();
        size += PhuTu1Used.ByteSize() + PhuTu2Used.ByteSize() + PhuTu3Used.ByteSize() + PhuTu4Used.ByteSize();

        // Check an toàn cho mảng danh sách thần thông / đan dược
        size += ThanThongEquip != null ? ThanThongEquip.ByteSize() : 0;
        size += DanCombatUseds != null ? DanCombatUseds.ByteSize() : 0;
        size += NgoDaos != null ? NgoDaos.ByteSize() : 0;

        // Thay thế đoạn tính size trang bị cũ bằng đoạn này:
        size += sizeof(bool); // Cờ kiểm tra VuKhi
        if (VuKhi != null) size += VuKhi.ByteSize();

        size += sizeof(bool); // Cờ kiểm tra PhongCu
        if (PhongCu != null) size += PhongCu.ByteSize();

        size += sizeof(bool); // Cờ kiểm tra TrangSuc
        if (TrangSuc != null) size += TrangSuc.ByteSize();

        size += Kim.ByteSize() + Moc.ByteSize() + Thuy.ByteSize() + Hoa.ByteSize() + Tho.ByteSize() + Ma.ByteSize();

        // Check an toàn cho Object Ability ẩn
        size += Ability.ByteSize();
        size += DanDoc.ByteSize() + Hp.ByteSize();
        return size;
    }

    public void Serialize(IWriter writer)
    {
        writer.Write(Name);
        writer.Write(Gender);

        // Ghi Skins & Colors (Phòng hờ dữ liệu null bằng cách ghi mảng rỗng thay vì nổ lỗi)
        writer.WriteArray(Skins?.Keys ?? Enumerable.Empty<string>());
        writer.WriteArray(Skins?.Values ?? Enumerable.Empty<string>());
        writer.WriteArray(Colors?.Keys ?? Enumerable.Empty<string>());
        writer.WriteArray(Colors?.Values ?? Enumerable.Empty<Color>());

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

        // Ghi các danh sách mảng an toàn
        writer.WriteArray(ThanThongEquip ?? new List<int>());
        writer.WriteArray(DanCombatUseds ?? Enumerable.Empty<DanCombatUsed>());
        writer.WriteArray(NgoDaos ?? Enumerable.Empty<NgoDao>());

        // Thay thế 3 dòng writer.Write(VuKhi...) cũ bằng đoạn check cờ an toàn này:
        bool hasVuKhi = VuKhi != null;
        writer.Write(hasVuKhi);
        if (hasVuKhi) VuKhi.Serialize(writer);

        bool hasPhongCu = PhongCu != null;
        writer.Write(hasPhongCu);
        if (hasPhongCu) PhongCu.Serialize(writer);

        bool hasTrangSuc = TrangSuc != null;
        writer.Write(hasTrangSuc);
        if (hasTrangSuc) TrangSuc.Serialize(writer);

        writer.Write(Kim);
        writer.Write(Moc);
        writer.Write(Thuy);
        writer.Write(Hoa);
        writer.Write(Tho);
        writer.Write(Ma);

        // CHÍNH XÁC LÀ DÒNG 188 CỦA BẠN: Bọc an toàn cho Object Ability ẩn
        writer.Write(Ability);
        writer.Write(DanDoc);
        writer.Write(Hp);
    }
}