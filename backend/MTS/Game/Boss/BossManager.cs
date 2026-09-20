using System.Collections.Concurrent;
using MTS.Models;
using MTS.Network;
using MTS.Network.Models;

namespace MTS.Game.Boss;

public class BossManager
{
    public static ConcurrentDictionary<int, IBossFighter> Players { get; private set; } = new();
    public BossData BossData { get; private set; }
    public int Id => BossData.CharWrapper.Character.Id;
    public CharacterStats Stats => BossData.CharWrapper.Character.Stats;
    public BossManager(BossData bossData)
    {
        BossData = bossData.DeepClone();
    }

    public void SendAllIgnore<T>(MessageTag tag, T data, IBossFighter ignorePlayer)
    {
        foreach (var player in Players.Values)
        {
            if (player == ignorePlayer) continue;
            player.MemSend(tag, data);
        }
    }

    public void TakeDame(IBossFighter attacker, SyncDame syncDame)
    {
        if (syncDame.Datas.Any(x => x.Dame > 1e6))
        {
            return;
        }
        if (!Players.ContainsKey(syncDame.CharSendDame))
        {
            return;
        }
        if (Id != syncDame.CharTakeDame)
        {
            return;
        }
        var hp = Stats.Hp;
        foreach (var dameData in syncDame.Datas)
        {
            hp -= dameData.Dame;
        }
        hp = Math.Max(0, hp);
        Stats.SetHp(hp);
        SendAllIgnore(MessageTag.SyncDame, syncDame, attacker);
    }
    public void SyncEndTurnBoss(IBossFighter player,SyncEndTurnBoss model)
    {
        model.IsKietSuc = model.IsKietSuc || model.CharId != player.Id;
        player.MemSend(MessageTag.SyncEndTurnBoss, model);
    }
    public void OnPlayerJoin(IBossFighter player)
    {
        Players.TryAdd(player.Id, player);
    }

    public int GetPlayerCount()
    {
        return Players.Count;
    }

    public void OnPlayerLeave(IBossFighter player)
    {
        Players.TryRemove(player.Id, out var _);
    }


}