namespace MTS.Services.Clans;

public class ClanOptions
{
    public int CreationMoneyVipCost { get; set; } = 0;
    public int CreationPayOsVnd { get; set; } = 0;
    public int DonationUnitMoneyVip { get; set; } = 0;
    public int InitialCapacity { get; set; } = 30;
    public int MaxCapacity { get; set; } = 200;
    public int JoinCooldownHours { get; set; } = 24;
    public int ApplicationLifetimeHours { get; set; } = 72;
    public int MaxPendingApplications { get; set; } = 10;
    public int CalendarUtcOffsetHours { get; set; } = 7;
    public ClanInheritanceDefinition[] Inheritances { get; set; } = [];

    public bool IsValid() => CreationMoneyVipCost >= 0 && CreationPayOsVnd >= 0 &&
        DonationUnitMoneyVip is >= 0 and <= int.MaxValue / 10 &&
        InitialCapacity > 0 && InitialCapacity <= MaxCapacity && MaxCapacity <= 1000 &&
        JoinCooldownHours is >= 0 and <= 8760 && ApplicationLifetimeHours is > 0 and <= 8760 &&
        MaxPendingApplications is > 0 and <= 100 && CalendarUtcOffsetHours is >= -12 and <= 14 &&
        Inheritances != null && Inheritances.Length <= 100 &&
        Inheritances.All(x => x != null && x.Id > 0 && !string.IsNullOrWhiteSpace(x.Name) && x.Name.Length <= 64 &&
            x.Effect is 1 or 2 && (x.Effect != 1 || x.Element is >= 1 and <= 5) && x.Value is > 0 and <= 10000 &&
            x.UnlockCost >= 0 && x.LearnCost >= 0 && x.MinClanLevel > 0) &&
        Inheritances.Select(x => x.Id).Distinct().Count() == Inheritances.Length;
}

public interface IClanWorldCatalog { bool Contains(int nodeId); }
public sealed class ClanWorldCatalog : IClanWorldCatalog
{
    public bool Contains(int nodeId) => MTS.Game.GameConfig.MapManager?.Maps?.Any(x => x.Id == nodeId) == true;
}

// Execute while holding SyncRoot. This must be the same lock used by character autosave.
public interface IClanActor
{
    object SyncRoot { get; }
    int CharId { get; }
    int Level { get; }
    int MoneyVip { get; set; }
}

public class ClanInheritanceDefinition
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    // 1: initial elemental energy; 2: initial HoTrao stacks.
    public int Effect { get; set; }
    public int Element { get; set; }
    public int Value { get; set; }
    public long UnlockCost { get; set; }
    public long LearnCost { get; set; }
    public int MinClanLevel { get; set; } = 1;
}

public sealed class ClanRuleException(string code) : Exception(code)
{
    public string Code { get; } = code;
}
