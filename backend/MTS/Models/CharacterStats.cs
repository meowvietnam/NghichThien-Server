using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using MemoryPack;
using Microsoft.EntityFrameworkCore;
using MTS.Data;
using MTS.Game;
using MTS.Game.BaseSkill;
using MTS.Game.CreateCharacter;
using MTS.Game.LuyenDan;
using MTS.Network.Models;
using Action = MTS.Game.CreateCharacter.Action;
using Network.Extensions.MemoryPack;
namespace MTS.Models;
[MemoryPackable]
public partial class CharacterStats 
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

    [NotMapped] 
    [MemoryPackIgnore]
    private bool isDirty;

    [JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    [MemoryPackIgnore]
    public int CharId { get; set; }

    [ForeignKey(nameof(CharId)), Key]
    [JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    [MemoryPackIgnore]
    public CharacterData CharacterData { get; set; }
    [MemoryPackConstructor]
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
        TuoiTac = ClampTuoiTac(stats.TuoiTac);
        isDirty = true;
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
        value = Math.Max(0, value);
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
            isDirty = true;
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

    /// <summary>
    /// Tuổi tác tối đa: 10.000 năm (năm 360 ngày như cách tính tháng 30 ngày của game). Giữ xa giới hạn của TimeSpan
    /// (~29.000 năm) và của các phép đổi sang ngày/tháng kiểu int để cộng tuổi không bao giờ tràn số.
    /// </summary>
    public static readonly TimeSpan MaxTuoiTac = TimeSpan.FromDays(360d * 20_000);

    /// <summary>Kẹp tuổi tác vào [0, MaxTuoiTac].</summary>
    public static TimeSpan ClampTuoiTac(TimeSpan value) =>
        value < TimeSpan.Zero ? TimeSpan.Zero : value > MaxTuoiTac ? MaxTuoiTac : value;

    /// <summary>Cộng tuổi an toàn: không tràn TimeSpan, kết quả luôn trong [0, MaxTuoiTac].</summary>
    public static TimeSpan AddTuoiTac(TimeSpan current, TimeSpan delta)
    {
        current = ClampTuoiTac(current);
        if (delta >= MaxTuoiTac - current) return MaxTuoiTac;
        if (delta <= -current) return TimeSpan.Zero;
        return current + delta;
    }

    /// <summary>Cộng tuổi theo số ngày (double), chặn số ngày quá lớn/không hợp lệ trước khi tạo TimeSpan.</summary>
    public static TimeSpan AddTuoiTacDays(TimeSpan current, double days)
    {
        if (double.IsNaN(days)) return ClampTuoiTac(current);
        var maxDays = MaxTuoiTac.TotalDays;
        days = Math.Clamp(days, -maxDays, maxDays);
        return AddTuoiTac(current, TimeSpan.FromDays(days));
    }

    public void SetTuoiTac(TimeSpan value)
    {
        value = ClampTuoiTac(value);
        if (TuoiTac != value)
        {
            var oldValue = TuoiTac;
            TuoiTac = value;
            isDirty = true;
            CharacterData?.Progression.OnSetTuoiTac(oldValue, value);
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
    public void ApplyFrom(StatsSnapshot snapshot)
    {
        Kim = snapshot.Kim;
        Moc = snapshot.Moc;
        Thuy = snapshot.Thuy;
        Hoa = snapshot.Hoa;
        Tho = snapshot.Tho;
        Ma = snapshot.Ma;
        TuChat = snapshot.TuChat;
        NgoTinh = snapshot.NgoTinh;
        ThanThuc = snapshot.ThanThuc;
        TocDoBay = snapshot.TocDoBay;
        LinhThach = snapshot.LinhThach;
        Hp = snapshot.Hp;
        SinhMenh = snapshot.SinhMenh;
        ThoNguyen = snapshot.ThoNguyen;
        TamCanh = snapshot.TamCanh;
        DanDoc = snapshot.DanDoc;
    }
    public void Apply(StatModifier modifier)
    {
        Apply(modifier.Type, modifier.Action, modifier.Value);
    }

    public void Apply(StatType type, Action action, double value)
    {
        if (type == StatType.None) return;
        if (action == Action.Set)
        {
            SetValue(type, value);
        }
        else if (action == Action.Add)
        {
            var t = GetValue(type);
            SetValue(type, Convert.ToDouble(t) + value);
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

    private void SetFloat(StatType type, double newValue)
    {
        var value = (float)newValue;
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
            case StatType.TuoiTac: TuoiTac = ClampTuoiTac(new TimeSpan(value)); break;
        }
    }

    public void SetValue(StatType type, double value)
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

        ETypeSkill ConvertETypeBuffDanToETypeND(EDanBuffType eDanBuffType)
        {
            if (eDanBuffType == EDanBuffType.KimChiDao)
            {
                return ETypeSkill.Kim;
            }
            if (eDanBuffType == EDanBuffType.MocChiDao)
            {
                return ETypeSkill.Moc;
            }
            if (eDanBuffType == EDanBuffType.ThuyChiDao)
            {
                return ETypeSkill.Thuy;
            }
            if (eDanBuffType == EDanBuffType.HoaChiDao)
            {
                return ETypeSkill.Hoa;
            }
            if (eDanBuffType == EDanBuffType.ThoChiDao)
            {
                return ETypeSkill.Tho;
            }
            if (eDanBuffType == EDanBuffType.KiemChiDao)
            {
                return ETypeSkill.Kiem;
            }
            if (eDanBuffType == EDanBuffType.ThanThucChiDao)
            {
                return ETypeSkill.Than;
            }
            return ETypeSkill.None;
        }

        void AddCamNgo(EDanBuffType eDanBuffType)
        {
            var camNgoType = ConvertETypeBuffDanToETypeND(eDanBuffType);
            if (camNgoType == ETypeSkill.None) return;
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
                ClampDanDoc();
                break;
            case EDanBuffType.Hp:
                SetHp(Math.Clamp(Hp + (int)(value / 100f * SinhMenh), 0, SinhMenh));
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

        if (isAddDanDoc)
        {
            SetValue(StatType.DanDoc, DanDoc + danDoc);
            ClampDanDoc();
        }
        CharacterData.AddUseDan((ushort)linhDan.Id, count);
    }

    public void ClampDanDoc()
    {
        DanDoc = Math.Max(0, DanDoc);
        isDirty = true;
    }
    public void ClampHp()
    {
        Hp = Math.Clamp(Hp, 0, SinhMenh);
        isDirty = true;
    }

    public CharacterStats Clone()
    {
        var bytes = MemoryPackSerializer.Serialize(this);
        var charStats = MemoryPackSerializer.Deserialize<CharacterStats>(bytes);
        return charStats;
    }

    public void SetLinhCan(LinhCan linhCan)
    {
        LinhCan = linhCan;
        isDirty = true;
    }

    public void ResetLinhCan()
    {
        var percent = 100f / 5;
        Kim = percent;
        Moc = percent;
        Thuy = percent;
        Hoa = percent;
        Tho = percent;
        isDirty = true;
    }

    public void SetDanDoc(int danDoc)
    {
       DanDoc = danDoc;
       isDirty = true;
    }

    public void ResetAbility()
    {
        Ability = Ability.None;
        isDirty = true;
    }

    public void ResetTuoiTac()
    {
        TuoiTac = GameConfig.CharacterTemplate.Stats.TuoiTac;
        isDirty = true;
    }
    public void ResetTuChat()
    {
        TuChat = GameConfig.CharacterTemplate.Stats.TuChat;
        isDirty = true;
    }

    public void ResetTamCanh()
    {
        TamCanh = GameConfig.CharacterTemplate.Stats.TamCanh;
        isDirty = true;
    }

    public void ResetNgoTinh()
    {
        NgoTinh = GameConfig.CharacterTemplate.Stats.NgoTinh;
        isDirty = true;
    }

    public void SetAbility(Ability ability)
    {
        Ability = ability;
        isDirty = true;
    }

    public void SetMa(float ma)
    {
        Ma = ma;
        isDirty = true;
    }
}