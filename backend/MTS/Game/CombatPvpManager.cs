using MTS.Game.Combat;
using MTS.Network;
using MTS.Network.ClientState;
using MTS.Network.Models;

namespace MTS.Game;

public class CombatPvpManager
{
    public CombatPvp[] Players { get; set; }
    private bool isStart;
    private object combatLock = new();

    public CombatPvpManager(params CombatPvp[] players)
    {
        Players = players;
    }

    public void OnPlayerReady(CombatPvp player)
    {
        lock (combatLock)
        {
            if (!isStart)
            {
                if (Players.All(x => x.IsReady))
                {
                    isStart = true;
                    StartCombat();
                }
            }
        }
    }

    private async void StartCombat()
    {
        await Task.Delay(TimeSpan.FromMilliseconds(2000));
        var startTurnPlayer = Players.MaxBy(x => x.CharData().Stats.TocDoBay).CharData().Id;
        foreach (var player in Players)
        {
            player.MemSend(MessageTag.SyncStartTurn, startTurnPlayer);
            player.MemSend(MessageTag.Notify, "Trận đấu bắt đầu");
        }
    }

    private void SendOther<T>(MessageTag tag, T data, CombatPvp playerIgnore)
    {
        foreach (var player in Players)
        {
            if (player != playerIgnore)
            {
                player.MemSend(tag,data);
            }
        }
    }
    private void SendOther(MessageTag tag,CombatPvp playerIgnore)
    {
        foreach (var player in Players)
        {
            if (player != playerIgnore)
            {
                player.Send(tag);
            }
        }
    }
    public void OnResultCombat(CombatPvp player, ResultCombat resultCombat)
    {
        lock (combatLock)
        {
            ResultCombatResult result = null!;
            try
            {
                result = player.CharData().ResultCombatPvp(resultCombat);
            }
            catch (InvalidDataException e)
            {
                result = new ResultCombatResult()
                {
                    Success = false,
                    Message = e.Message
                };
            }
            if (resultCombat.TypeResultCombat != ETypeResultCombat.LoseByRun)
            {
                player.MemSend(MessageTag.ReadyCombatPvp, result);
            }
            else
            {
                SendOther(MessageTag.YouWinPvp, player);
            }
        }
    }
}