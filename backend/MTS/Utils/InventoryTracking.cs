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
    public Modify<Equipment> Equipments { get; set; } = new();
    public void Clear()
    {
        Items.Clear();
        Furnaces.Clear();
        NgoDaos.Clear();
        CamNgos.Clear();
        CongPhaps.Clear();
        ThanThongs.Clear();
        Equipments.Clear();
    }

    public void Apply(AppDbContext db)
    {
        Items.Apply(db.Items);
        Furnaces.Apply(db.Furnaces);
        NgoDaos.Apply(db.NgoDaos);
        CamNgos.Apply(db.CamNgos);
        CongPhaps.Apply(db.CongPhaps);
        ThanThongs.Apply(db.ThanThongs);
        Equipments.Apply(db.Equipments);
    }

    public void Apply(InventoryManager inventory,InventoryTracking tracking)
    {
        foreach (var item in Items.Deletes)
        {
            inventory.Remove(item);
        }
        foreach (var item in Items.Updates)
        {
            inventory.Update(item);
        }
        foreach (var item in Items.Adds)
        {
            inventory.Add(item);
        }
        foreach (var item in Equipments.Deletes)
        {
            inventory.Remove(item);
        }
        foreach (var item in Equipments.Updates)
        {
            inventory.Update(item);
        }
        foreach (var item in Equipments.Adds)
        {
            inventory.Add(item);
        }
        Furnaces.Apply(inventory.Furnaces);
        tracking.Furnaces = Furnaces;
        NgoDaos.Apply(inventory.NgoDaos);
        tracking.NgoDaos = NgoDaos;
        CamNgos.Apply(inventory.CamNgos);
        tracking.CamNgos = CamNgos;
        CongPhaps.Apply(inventory.CongPhapOwners);
        tracking.CongPhaps = CongPhaps;
        ThanThongs.Apply(inventory.ThanThongOwners);
        tracking.ThanThongs = ThanThongs;
    }
}