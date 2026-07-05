namespace MTS.Models;

[Flags]
public enum Ability
{
    None,
    YeuDuoiNhieuBenh = 1 << 0,
    ThanMaHuyetMach = 1 << 1,
    HuyenAmTuyetMach = 1 << 2,
    TienThienDaoThe = 1 << 3,
    DiTocHuyetMach = 1 << 4,
    NguKhiTrieuNguyen = 1 << 5,
    TuyetAmDocThe = 1 << 6,
    LongTocHuyetMach = 1 << 7,
    ThienSinhDiDong = 1 << 8,
    ChuyenTheTienNhan = 1 << 9,
    ThanBiLucBinh = 1 << 10,
    TinhThanLe = 1 << 11,
    DuocDuongHocDo = 1 << 12,
    TeNuiKyNgo = 1 << 13,
    DongQuatSauNui = 1 << 14,
    ThanBiVoSo = 1 << 15,
    CoSoDaoHoa = 1 << 16,
    SoKhuyMonKinh = 1 << 17,
    MacCaKyXao = 1 << 18,
    GiaTocTuChan = 1 << 19,
    TienThienDiMach = 1 << 20,
}