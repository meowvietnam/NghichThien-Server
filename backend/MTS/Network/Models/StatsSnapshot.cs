using MemoryPack;
using MTS.Models;

namespace MTS.Network.Models;
[MemoryPackable]
public partial class StatsSnapshot
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
    public int DanDoc { get; private set; }
    [MemoryPackConstructor]
    public StatsSnapshot()
    {
            
    }
    public StatsSnapshot(CharacterStats snapshot)
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
}