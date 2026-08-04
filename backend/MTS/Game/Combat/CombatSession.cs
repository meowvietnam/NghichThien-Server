namespace MTS.Game.Combat;

public enum CombatStatus
{
    InCombat,
    End
}

public class CombatSession
{
    public bool HasClaimedAdReward { get; set; } = false;
    public CombatStatus Status { get; set; } = CombatStatus.InCombat;
    public ETypeCombat Type { get; set; }
    public int Level { get; set; }
    public RewardSnapshot? RewardSnapshot { get; set; }

    public bool CanClaim()
    {
        return Status == CombatStatus.End && !HasClaimedAdReward && RewardSnapshot != null && RewardSnapshot.ItemRefs != null;
    }
}