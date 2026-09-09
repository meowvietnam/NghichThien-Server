namespace MTS.Services.Clans;

public class ClanOptions
{
    public int CreationCost { get; set; } = 0;
    public int InitialCapacity { get; set; } = 30;
    public int MaxCapacity { get; set; } = 200;
    public int JoinCooldownHours { get; set; } = 24;
    public int ApplicationLifetimeHours { get; set; } = 72;
    public int MaxPendingApplications { get; set; } = 10;
    public int DailyDonationLimit { get; set; } = 100000;
    public int CalendarUtcOffsetHours { get; set; } = 7;
}

// Execute while holding SyncRoot. This must be the same lock used by character autosave.
public interface IClanActor
{
    object SyncRoot { get; }
    int CharId { get; }
    int Level { get; }
    int LinhThach { get; set; }
}

public sealed class ClanRuleException(string code) : Exception(code)
{
    public string Code { get; } = code;
}
