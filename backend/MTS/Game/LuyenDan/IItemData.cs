namespace MTS.Game.LuyenDan;

public interface IItemData
{
    int Id { get; }
    string Name { get; }
    Tier Tier { get; }
    string Description { get; }
}