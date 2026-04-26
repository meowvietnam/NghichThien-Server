using MTS.Data;
using MTS.Models;

namespace MTS.Utils;

public class InventoryTracking
{
    public Modify<ItemRef> Items { get; set; } = new();
    public Modify<FurnaceData> Furnaces { get; set; } = new();
    public Modify<NgoDao> NgoDaos { get; set; } = new();
    public Modify<CamNgo> CamNgos { get; set; } = new();
    public Modify<CongPhap> CongPhaps { get; set; } = new();
    public Modify<ThanThong> ThanThongs { get; set; } = new();
    public void Clear()
    {
        Items.Clear();
        Furnaces.Clear();
        NgoDaos.Clear();
        CamNgos.Clear();
        CongPhaps.Clear();
        ThanThongs.Clear();
    }

    public void Apply(AppDbContext db)
    {
        Items.Apply(db.Items);
        Furnaces.Apply(db.Furnaces);
        NgoDaos.Apply(db.NgoDaos);
        CamNgos.Apply(db.CamNgos);
        CongPhaps.Apply(db.CongPhaps);
        ThanThongs.Apply(db.ThanThongs);
    }
}