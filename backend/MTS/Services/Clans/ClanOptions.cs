namespace MTS.Services.Clans;

public class ClanOptions
{
    public int CreationMoneyVipCost { get; set; } = 0; // 0 = not configured; paid creation disabled.
    public int CreationPayOsVnd { get; set; } = 0;
    public int DonationUnitMoneyVip { get; set; } = 0;
    public ClanInheritanceDefinition[] Inheritances { get; set; } = [];
    public int InitialCapacity { get; set; } = 30;
    public int MaxCapacity { get; set; } = 200;
    public int JoinCooldownHours { get; set; } = 24;
    public int ApplicationLifetimeHours { get; set; } = 72;
    public int MaxPendingApplications { get; set; } = 10;
    public int CalendarUtcOffsetHours { get; set; } = 7;
}

// Execute while holding SyncRoot. This must be the same lock used by character autosave.
public interface IClanActor
{
    object SyncRoot { get; }
    int CharId { get; }
    int Level { get; }
    int MoneyVip { get; set; }
}

public sealed class ClanRuleException(string code) : Exception(code)
{
    public string Code { get; } = code;
}

// Effect 1: starting elemental energy; effect 2: starting shield (Hộ Thuẫn).
public class ClanInheritanceDefinition
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int Effect { get; set; }
    public int Element { get; set; }
    public int Value { get; set; }
    public long UnlockCost { get; set; }
    public long LearnCost { get; set; }
    public int MinClanLevel { get; set; } = 1;
}

public interface IClanWorldCatalog { bool Contains(int nodeId); }
public sealed class ClanWorldCatalog : IClanWorldCatalog
{
    public bool Contains(int nodeId) => MTS.Game.GameConfig.MapManager?.Maps?.Any(x => x.Id == nodeId) == true;
}
