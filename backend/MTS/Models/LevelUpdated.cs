using MemoryPack;

namespace MTS.Models;

[MemoryPackable]
public partial class LevelUpdated
{
    public int Level { get; set; }
    public int CurrentExp { get; set; }
    public int HpMax { get; set; }
    public int ThanThuc { get; set; }
    public int DonToc { get; set; }
    public int ThoNguyen { get; set; }
    public int NewNgoDaoDiem { get; set; }
    public void ApplyTo(CharacterData charData)
    {
        var progression = charData.Progression;
        progression.SetLevel(Level);
        progression.SetCurrentExp(CurrentExp);
        progression.SetNgoDaoDiem(NewNgoDaoDiem);
        var stats = charData.Stats;
        stats.SetSinhMenh(HpMax);
        stats.SetHp(HpMax);
        stats.SetThanThuc(ThanThuc);
        stats.SetTocDoBay(DonToc);
        stats.SetThoNguyen(ThoNguyen);
    }
}