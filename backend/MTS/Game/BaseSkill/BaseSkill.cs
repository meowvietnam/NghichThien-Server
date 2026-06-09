using MTS.Game.Resources;
using Network;
using Network.Extensions;

namespace MTS.Game.BaseSkill;



public enum EBuffAndDeBuff
{
    None,
    HoTrao,
    DeTonThuong,
    KiemKhi,
    KietSuc,
    TreKhi,
    SucThe,
    GiamTonThuong,
    NoiThuong,
    TrienNhieu,
    TriLieu,
    ChanKim,
    TrungDoc,
    HuyenVu,
    ChayMau,
    ChanMoc,
    ChanThuy,
    ChanHoa,
    ChanTho,
    TuKhi,
    Cham,
    SuongGia,
    DongKet,
    ChanKiem,
    ChiThuy,
    KinhDao,
    SucLuc,
    ThieuDot,
    HoaDiem,
    Diem,
    Tran,
    DanLuc,
    PhaThe,
    Cat,
    AnNac,
    Hon,
    Anh,
    AnhDich,
    AnhNac,
    ChanLongChiHuyet,
    NgungThan,
    ThanThuong,
    QuyCuu,
    QuyTon,
    HonLuc,
    NguKiemHiep,
    NhanKiem,
    DiaKiem,
    ThienKiem,
    QuyLinh,
    QuyBinh,
    QuyTuong,
    QuyDe,
    KiemTam,
    ChanThan,
    NhieuHon,
    TinhHonChiLuc,
    NguKhiHiep,
    NguKimHiep,
    NguMocHiep,
    NguThuyHiep,
    NguHoaHiep,
    NguThoHiep,
    NguThanThucHiep,
    ChanKhi,
    TangToc,
    TroiChan,
    TichHoa,
    TichKim,
    TichMoc,
    TichThuy,
    TichDoc,
    NguKim,
    NguMoc,
    NguThuy,
    NguHoa,
    NguTho,
    PhanTonThuong,
}
public enum ETierSkill
{
    None,
    NhanGiaiHa,
    NhanGiaiTrung,
    NhanGiaiThuong,
    DiaGiaiHa,
    DiaGiaiTrung,
    DiaGiaiThuong,
    ThienGiaiHa,
    ThienGiaiTrung,
    ThienGiaiThuong,
}
public enum ETypeSkill
{
    None,
    Kim,
    Moc,
    Thuy,
    Hoa,
    Tho,
    Ma,
    Kiem,
    Khi,
    The,
    Than,
    DiHoa,
    TranPhap
}

public enum EKeyWord
{
    None,
    LienKich,
    HauKeVoLuc,
    TuongSinh,
    TuongKhac,
    TuongDong,
    KiemY,
    AmKinh,
    TranPhap,
    ThanThuc,
    ChanThucTonThuong
}

public class BaseSkill : BaseData
{
    public int baseTimeLearn;
    public ETierSkill eTierSkill;
    public ETypeSkill[] eTypeSkill;

    private EKeyWord[] eKeywordSkillHave;
    private EBuffAndDeBuff[] eKeywordBuffSkillHave;

    public override int ByteSize()
    {
        return base.ByteSize() + eTierSkill.ByteSize() + eTypeSkill.ByteSize()
               + eKeywordSkillHave.ByteSize() + eKeywordBuffSkillHave.ByteSize()
               + baseTimeLearn.ByteSize();
    }

    public void Deserialize(IReader reader, ref BaseSkill value)
    {
        BaseData baseValue = value;
        baseValue.Deserialize(reader, ref baseValue);
        value.eTierSkill = reader.Read<ETierSkill>();
        value.eTypeSkill = reader.ReadArray<ETypeSkill>();
        value.eKeywordSkillHave = reader.ReadArray<EKeyWord>();
        value.eKeywordBuffSkillHave = reader.ReadArray<EBuffAndDeBuff>();
        value.baseTimeLearn = reader.Read<int>();
    }

    public override void Serialize(IWriter writer)
    {
        base.Serialize(writer);
        writer.Write(eTierSkill);
        writer.WriteArray(eTypeSkill);
        writer.WriteArray(eKeywordSkillHave);
        writer.WriteArray(eKeywordBuffSkillHave);
        writer.Write(baseTimeLearn);
    }
}