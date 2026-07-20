using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using MTS.CP;
using MTS.Data;
using MTS.Game.CreateCharacter;
using MTS.Game.LuyenDan;
using MTS.Localization;
using Network;
using Network.Extensions;
using Action = MTS.Game.CreateCharacter.Action;

namespace MTS.Models;

public class CharacterStats : IMemorySerializable<CharacterStats>
{
    public float Kim { get; private set; }
    public float Moc { get; private set; }
    public float Thuy { get; private set; }
    public float Hoa { get; private set; }
    public float Tho { get; private set; }
    public float Ma { get; private set; }
    public int TuChat { get; private set; }
    public int NgoTinh { get; private set; }
    public int ThanThuc { get; private set; }
    public int TocDoBay { get; private set; }
    public int LinhThach { get; private set; }
    public int Hp { get; private set; }
    public int SinhMenh { get; private set; }
    public int ThoNguyen { get; private set; }
    public int TamCanh { get; private set; }
    public LinhCan LinhCan { get; private set; }
    public Ability Ability { get; private set; }
    public int DanDoc { get; private set; }
    public TimeSpan TuoiTac { get; private set; }

    [NotMapped] private bool isDirty;

    [JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public int CharId { get; set; }

    [ForeignKey(nameof(CharId)), Key]
    [JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public CharacterData CharacterData { get; set; }
    public CharacterStats()
    {
    }

    public CharacterStats(CharacterStats stats)
    {
        CopyFrom(stats);
    }

    public void CopyFrom(CharacterStats stats)
    {
        Kim = stats.Kim;
        Moc = stats.Moc;
        Thuy = stats.Thuy;
        Hoa = stats.Hoa;
        Tho = stats.Tho;
        Ma = stats.Ma;
        TuChat = stats.TuChat;
        NgoTinh = stats.NgoTinh;
        ThanThuc = stats.ThanThuc;
        TocDoBay = stats.TocDoBay;
        LinhThach = stats.LinhThach;
        Hp = stats.Hp;
        SinhMenh = stats.SinhMenh;
        ThoNguyen = stats.ThoNguyen;
        TamCanh = stats.TamCanh;
        LinhCan = stats.LinhCan;
        Ability = stats.Ability;
        DanDoc = stats.DanDoc;
        TuoiTac = stats.TuoiTac;
    }

    public void SetHp(int value)
    {
        if (Hp != value)
        {
            Hp = value;
            isDirty = true;
        }
    }

    public void SetSinhMenh(int value)
    {
        if (SinhMenh != value)
        {
            SinhMenh = value;
            isDirty = true;
        }
    }

    public void SetTocDoBay(int value)
    {
        if (TocDoBay != value)
        {
            TocDoBay = value;
            isDirty  = true;
        }
    }

    public void SetThanThuc(int value)
    {
        if (ThanThuc != value)
        {
            ThanThuc = value;
            isDirty = true;
        }
    }

    public void SetTuoiTac(TimeSpan value)
    {
        if (TuoiTac != value)
        {
            var oldValue = TuoiTac;
            TuoiTac = value;
            isDirty = true;
            CharacterData.Progression.OnSetTuoiTac(oldValue, value);
        }
    }

    public static int GetMinTamCanh()
    {
        return -100;
    }
    public void SetTamCanh(int tamCanh)
    {
        tamCanh = Math.Max(GetMinTamCanh(), tamCanh);
        if (TamCanh != tamCanh)
        {
            TamCanh = tamCanh;
            isDirty = true;
        }
    }

    public int GetMinThoNguyen()
    {
        return 0;
    }
    public void SetThoNguyen(int thoNguyen)
    {
        thoNguyen = Math.Max(GetMinThoNguyen(), thoNguyen);
        if (ThoNguyen != thoNguyen)
        {
            ThoNguyen = thoNguyen;
            isDirty = true;
        }
    }
    
    public void SetLinhThach(int linhThach)
    {
        if (LinhThach != linhThach)
        {
            LinhThach = linhThach;
            isDirty = true;
        }
    }
    public void Apply(StatModifier modifier)
    {
        Apply(modifier.Type, modifier.Action, modifier.Value);
    }

    public void Apply(StatType type, Action action, float value)
    {
        if (type == StatType.None) return;
        if (action == Action.Set)
        {
            SetValue(type, value);
        }
        else if (action == Action.Add)
        {
            var t = GetValue(type);
            SetValue(type, Convert.ToSingle(t) + value);
        }
        else if (action == Action.Or)
        {
            var t = GetValue(type);
            var i = Convert.ToInt32(t);
            var newValue = i | (int)(value);
            SetValue(type, newValue);
        }
    }

    public object GetValue(StatType type)
    {
        switch (type)
        {
            case StatType.Kim: return Kim;
            case StatType.Moc: return Moc;
            case StatType.Thuy: return Thuy;
            case StatType.Hoa: return Hoa;
            case StatType.Tho: return Tho;
            case StatType.Ma: return Ma;
            case StatType.TuChat: return TuChat;
            case StatType.NgoTinh: return NgoTinh;
            case StatType.ThanThuc: return ThanThuc;
            case StatType.TocDoBay: return TocDoBay;
            case StatType.LinhThach: return LinhThach;
            case StatType.Hp: return Hp;
            case StatType.SinhMenh: return SinhMenh;
            case StatType.ThoNguyen: return ThoNguyen;
            case StatType.TamCanh: return TamCanh;
            case StatType.LinhCan: return LinhCan;
            case StatType.Ability: return Ability;
            case StatType.DanDoc: return DanDoc;
            case StatType.TuoiTac: return TuoiTac;
            default: return null;
        }
    }

    private void SetFloat(StatType type, float value)
    {
        switch (type)
        {
            case StatType.Kim: Kim = value; break;
            case StatType.Moc: Moc = value; break;
            case StatType.Thuy: Thuy = value; break;
            case StatType.Hoa: Hoa = value; break;
            case StatType.Tho: Tho = value; break;
            case StatType.Ma: Ma = value; break;
        }
    }

    private void SetInt(StatType type, int value)
    {
        switch (type)
        {
            case StatType.TuChat: TuChat = value; break;
            case StatType.NgoTinh: NgoTinh = value; break;
            case StatType.ThanThuc: ThanThuc = value; break;
            case StatType.TocDoBay: TocDoBay = value; break;
            case StatType.LinhThach: LinhThach = value; break;
            case StatType.Hp: Hp = value; break;
            case StatType.SinhMenh: SinhMenh = value; break;
            case StatType.ThoNguyen: ThoNguyen = value; break;
            case StatType.TamCanh: TamCanh = value; break;
            case StatType.DanDoc: DanDoc = value; break;
            case StatType.TuoiTac: TuoiTac = new TimeSpan(value); break;
        }
    }

    public void SetValue(StatType type, float value)
    {
        switch (type)
        {
            case StatType.Kim:
            case StatType.Moc:
            case StatType.Thuy:
            case StatType.Hoa:
            case StatType.Tho:
            case StatType.Ma:
                SetFloat(type, value);
                break;
            case StatType.TuChat:
            case StatType.NgoTinh:
            case StatType.ThanThuc:
            case StatType.TocDoBay:
            case StatType.LinhThach:
            case StatType.Hp:
            case StatType.SinhMenh:
            case StatType.ThoNguyen:
            case StatType.TamCanh:
            case StatType.NgoDao:
            case StatType.DanDoc:
            case StatType.TuoiTac:
                SetInt(type, (int)value);
                break;
            case StatType.LinhCan:
                LinhCan = (LinhCan)(int)value;
                break;
            case StatType.Ability:
                Ability = (Ability)(int)value;
                break;
            default:
                return;
        }
        isDirty = true;
    }

    public int ByteSize()
    {
        return Kim.ByteSize() + Moc.ByteSize() + Thuy.ByteSize() + Hoa.ByteSize()
               + Tho.ByteSize() + Ma.ByteSize() + TuChat.ByteSize() + NgoTinh.ByteSize()
               + ThanThuc.ByteSize() + TocDoBay.ByteSize() + LinhThach.ByteSize()
               + Hp.ByteSize()
               + SinhMenh.ByteSize()
               + ThoNguyen.ByteSize() + TamCanh.ByteSize() + LinhCan.ByteSize() + Ability.ByteSize()
               + DanDoc.ByteSize() + TuoiTac.ByteSize();
    }

    public void Serialize(IWriter writer)
    {
        writer.Write(Kim);
        writer.Write(Moc);
        writer.Write(Thuy);
        writer.Write(Hoa);
        writer.Write(Tho);
        writer.Write(Ma);
        writer.Write(TuChat);
        writer.Write(NgoTinh);
        writer.Write(ThanThuc);
        writer.Write(TocDoBay);
        writer.Write(LinhThach);
        writer.Write(Hp);
        writer.Write(SinhMenh);
        writer.Write(ThoNguyen);
        writer.Write(TamCanh);
        writer.Write(LinhCan);
        writer.Write(Ability);
        writer.Write(DanDoc);
        writer.Write(TuoiTac);
    }

    public void Deserialize(IReader reader, ref CharacterStats value)
    {
        value.Kim = reader.Read<float>();
        value.Moc = reader.Read<float>();
        value.Thuy = reader.Read<float>();
        value.Hoa = reader.Read<float>();
        value.Tho = reader.Read<float>();
        value.Ma = reader.Read<float>();
        value.TuChat = reader.Read<int>();
        value.NgoTinh = reader.Read<int>();
        value.ThanThuc = reader.Read<int>();
        value.TocDoBay = reader.Read<int>();
        value.LinhThach = reader.Read<int>();
        value.Hp = reader.Read<int>();
        value.SinhMenh = reader.Read<int>();
        value.ThoNguyen = reader.Read<int>();
        value.TamCanh = reader.Read<int>();
        value.LinhCan = reader.Read<LinhCan>();
        value.Ability = reader.Read<Ability>();
        value.DanDoc = reader.Read<int>();
        value.TuoiTac = reader.Read<TimeSpan>();
    }

    public bool IsDefault()
    {
        return CharId == 0;
    }

    public async Task SaveChanges(AppDbContext db)
    {
        if (!isDirty) return;
        await db.CharacterStats.Where(x => x.CharId == CharId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.Kim, Kim)
                .SetProperty(x => x.Moc, Moc)
                .SetProperty(x => x.Thuy, Thuy)
                .SetProperty(x => x.Hoa, Hoa)
                .SetProperty(x => x.Tho, Tho)
                .SetProperty(x => x.Ma, Ma)
                .SetProperty(x => x.TuChat, TuChat)
                .SetProperty(x => x.NgoTinh, NgoTinh)
                .SetProperty(x => x.ThanThuc, ThanThuc)
                .SetProperty(x => x.TocDoBay, TocDoBay)
                .SetProperty(x => x.LinhThach, LinhThach)
                .SetProperty(x => x.Hp, Hp)
                .SetProperty(x => x.SinhMenh, SinhMenh)
                .SetProperty(x => x.ThoNguyen, ThoNguyen)
                .SetProperty(x => x.TamCanh, TamCanh)
                .SetProperty(x => x.LinhCan, LinhCan)
                .SetProperty(x => x.Ability, Ability)
                .SetProperty(x => x.DanDoc, DanDoc)
                .SetProperty(x => x.TuoiTac, TuoiTac));
        isDirty = false;
    }
    public void Buff(LinhDan linhDan, int count)
    {
        var type = linhDan.EBuffType;
        var value = linhDan.BuffValue * count;
        var danDoc = linhDan.DanDoc * count;
        bool isAddDanDoc = true;
        void AddStat(StatType type)
        {
            var currentValue = Convert.ToSingle(GetValue(type));
            SetValue(type, currentValue + value);
        }
        void AddTuVi()
        {
            var progression = CharacterData.Progression;
            progression.SetCurrentExp(progression.CurrentExp + value);
           
        }
        ND.ETypeSkill ConvertETypeBuffDanToETypeND(EDanBuffType eDanBuffType)
        {
            if(eDanBuffType == EDanBuffType.KimChiDao)
            {
                return ND.ETypeSkill.Kim;
            }    
         
            else if (eDanBuffType == EDanBuffType.MocChiDao)
            {
                return ND.ETypeSkill.Moc;

            }

            else if (eDanBuffType == EDanBuffType.ThuyChiDao)
            {
                return ND.ETypeSkill.Thuy;

            }

            else if (eDanBuffType == EDanBuffType.HoaChiDao)
            {
                return ND.ETypeSkill.Hoa;

            }

            else if (eDanBuffType == EDanBuffType.ThoChiDao)
            {
                return ND.ETypeSkill.Tho;

            }
            else if (eDanBuffType == EDanBuffType.KiemChiDao)
            {
                return ND.ETypeSkill.Kiem;

            }
            else if (eDanBuffType == EDanBuffType.ThanThucChiDao)
            {
                return ND.ETypeSkill.Than;
            }
            return ND.ETypeSkill.None;
        }
        void AddCamNgo(EDanBuffType eDanBuffType)
        {

            var camNgoType = ConvertETypeBuffDanToETypeND(eDanBuffType);
            if (camNgoType == ND.ETypeSkill.None) return;
            var userCamNgo = CharacterData.InventoryManager.CamNgos.FirstOrDefault(x => x.Id == camNgoType);
            int currentUserCamNgoValue = userCamNgo != null ? userCamNgo.Value : 0;
            var camNgo = new CamNgo
            {
                Id = camNgoType,
                Value = value + currentUserCamNgoValue,
            };
            CharacterData.InventoryManager.AddOrUpdate(camNgo);

        }
        switch (type)
        {
            case EDanBuffType.None:
                return;
            case EDanBuffType.KimChiDao:
                AddCamNgo(EDanBuffType.KimChiDao);
                break;
            case EDanBuffType.KimLinhCan:
                AddStat(StatType.Kim);
                break;
            case EDanBuffType.MocChiDao:
                AddCamNgo(EDanBuffType.MocChiDao);
                break;
            case EDanBuffType.MocLinhCan:
                AddStat(StatType.Moc);
                break;
            case EDanBuffType.ThuyChiDao:
                AddCamNgo(EDanBuffType.ThuyChiDao);
                break;
            case EDanBuffType.ThuyLinhCan:
                AddStat(StatType.Thuy);
                break;
            case EDanBuffType.HoaChiDao:
                AddCamNgo(EDanBuffType.HoaChiDao);
                break;
            case EDanBuffType.HoaLinhCan:
                AddStat(StatType.Hoa);
                break;
            case EDanBuffType.ThoChiDao:
                AddCamNgo(EDanBuffType.ThoChiDao);
                break;
            case EDanBuffType.ThoLinhCan:
                AddStat(StatType.Tho);
                break;
            case EDanBuffType.KiemChiDao:
                AddCamNgo(EDanBuffType.KiemChiDao);
                break;
                
            case EDanBuffType.NgoDaoDiem:
                var progression = CharacterData.Progression;
                progression.SetNgoDaoDiem(progression.NgoDaoDiem + value);
                break;
            case EDanBuffType.DanDoc:
                SetValue(StatType.DanDoc, DanDoc - value);
                break;
            case EDanBuffType.Hp:
                SetHp(Math.Clamp(Hp + value, 0, SinhMenh));
                break;
            case EDanBuffType.SinhMenh:
                SetSinhMenh(SinhMenh + value);
                break;
            case EDanBuffType.HoiFullHp:
                SetHp(SinhMenh);
                break;
            case EDanBuffType.DonToc:
                SetTocDoBay(TocDoBay + value);
                break;
            case EDanBuffType.NgoTinh:
                AddStat(StatType.NgoTinh);
                break;
            case EDanBuffType.ThanThuc:
                SetThanThuc(ThanThuc + value);
                break;
            case EDanBuffType.ThanThucChiDao:
                AddCamNgo(EDanBuffType.ThanThucChiDao);
                break;
            case EDanBuffType.TuVi:
                AddTuVi();
                break;
            case EDanBuffType.TuViVaDanDocDuoiTrucCo:
                isAddDanDoc = CharacterData.Progression.Level < 4;
                AddTuVi();
                break;
            case EDanBuffType.TuViVaDanDocDuoiKetDan:
                isAddDanDoc = CharacterData.Progression.Level < 7;
                AddTuVi();
                break;
            case EDanBuffType.TuViVaDanDocDuoiNguyenAnh:
                isAddDanDoc = CharacterData.Progression.Level < 10;
                AddTuVi();
                break;
            case EDanBuffType.TuViVaDanDocDuoiHoaThan:
                isAddDanDoc = CharacterData.Progression.Level < 13;
                AddTuVi();
                break;
            case EDanBuffType.TuChat:
                AddStat(StatType.TuChat);
                break;
            case EDanBuffType.TamCanh:
                SetTamCanh(TamCanh + value);
                break;
            case EDanBuffType.ThoNguyen:
                SetThoNguyen(ThoNguyen + value);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
        if(isAddDanDoc)
        {
            SetValue(StatType.DanDoc, DanDoc + danDoc);
        }    
    }

    public void ClampHp()
    {
        Hp = Math.Clamp(Hp, 0, SinhMenh);
        isDirty = true;
    }
}
