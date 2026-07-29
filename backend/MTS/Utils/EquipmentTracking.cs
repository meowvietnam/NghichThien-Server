using MTS.Data;
using MTS.Models;

namespace MTS.Utils;

public class EquipmentTracking
{
    public Modify<ThanThongEquip> ThanThongEquips { get; set; } = new();
    public Modify<DanCombatUsed> DanCombatUseds { get; set; } = new();
    public void Clear()
    {
        ThanThongEquips.Clear();
        DanCombatUseds.Clear();
    }

    public void Apply(AppDbContext db)
    {
        ThanThongEquips.Apply(db, db.ThanThongEquips);
        DanCombatUseds.Apply(db, db.DanCombatUseds);
    }
}
