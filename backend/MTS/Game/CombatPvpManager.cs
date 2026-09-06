using MTS.Network;
using MTS.Network.ClientState;

namespace MTS.Game;

public class CombatPvpManager
{
    public CombatPvp[] Players { get; set; }

    public CombatPvpManager(params CombatPvp[] players)
    {
        Players = players;
    }

    public void OnPlayerReady(CombatPvp player)
    {
        if (Players.All(x => x.IsReady))
        {
            StartCombat();
        }
    }

    private void StartCombat()
    {
        var startTurnPlayer = Players.MaxBy(x => x.CharData().Stats.TocDoBay).CharData().Id;
        foreach (var player in Players)
        {
            player.MemSend(MessageTag.SyncStartTurn, startTurnPlayer);
            player.MemSend(MessageTag.Notify, "Trận đấu bắt đầu");
        }
    }
}