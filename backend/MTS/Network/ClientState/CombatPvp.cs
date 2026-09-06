using MTS.Game;
using MTS.Game.Combat;
using MTS.Models;
using MTS.Network.Models;
using Serilog;

namespace MTS.Network.ClientState;

public class CombatPvp : IClientState, ISender<MessageTag>
{
    private Client client;
    private CharacterData charData;
    private CombatPvp enemy;
    private CombatPvpManager manager;
    public bool IsReady { get; set; }
    public bool IsGameEnded { get; set; }
    private object lockObject = new();

    public CombatPvp(Client client, CharacterData charData)
    {
        this.client = client;
        this.charData = charData;
        client.OnLogout += OnLogout;
        client.OnOtherClientLogin += OnOtherClientLogin;
    }

    private void OnLogout(Client client)
    {
        OnRemove();
    }

    private void OnOtherClientLogin(Client client)
    {
        OnRemove();
    }

    public void SetEnemy(CombatPvp enemy)
    {
        this.enemy = enemy;
    }

    public void SetManager(CombatPvpManager manager)
    {
        this.manager = manager;
    }

    public void OnEnter()
    {
    }

    public CharacterData CharData() => charData;

    public void Handle(Message<MessageTag> message)
    {
        var tag = message.Tag;
        var reader = message.Reader;
        if (tag == MessageTag.ReadyCombatPvp)
        {
            if (IsReady) return;
            IsReady = true;
            manager.OnPlayerReady(this);
        }
        else if (tag == MessageTag.SyncAdjustLinhKhi)
        {
            var model = reader.MemRead<SyncAdjustLinhKhi>();
            enemy.MemSend(MessageTag.SyncAdjustLinhKhi, model);
        }
        else if (tag == MessageTag.SyncBuff)
        {
            var model = reader.MemRead<SyncBuff>();
            enemy.MemSend(MessageTag.SyncBuff, model);
        }
        else if (tag == MessageTag.SyncDame)
        {
            var model = reader.MemRead<SyncDame>();
            enemy.MemSend(MessageTag.SyncDame, model);
        }
        else if (tag == MessageTag.SyncEndTurn)
        {
            var model = reader.MemRead<SyncEndTurn>();
            enemy.MemSend(MessageTag.SyncEndTurn, model);
        }
        else if (tag == MessageTag.SyncHealing)
        {
            var model = reader.MemRead<SyncHealing>();
            enemy.MemSend(MessageTag.SyncHealing, model);
        }
        else if (tag == MessageTag.SyncPlaySkill)
        {
            var model = reader.MemRead<SyncPlaySkill>();
            enemy.MemSend(MessageTag.SyncPlaySkill, model);
        }
        else if (tag == MessageTag.SyncUsePhapBao)
        {
            var model = reader.MemRead<SyncUsePhapBao>();
            enemy.MemSend(MessageTag.SyncUsePhapBao, model);
        }
        else if (tag == MessageTag.ResultCombatPvp)
        {
            var resultCombat = reader.MemRead<ResultCombat>();
            EndCombatWithResult(resultCombat);
        }
    }

    private void EndCombatWithResult(ResultCombat resultCombat)
    {
        
        lock (lockObject)
        {
            if (IsGameEnded)
            {
                return;
            }
            manager.OnResultCombat(this, resultCombat);
            IsGameEnded = true;
        }
    }

    public void OnLeave()
    {
        OnRemove();
    }

    private void OnRemove()
    {
        client.OnLogout -= OnLogout;
        client.OnOtherClientLogin -= OnOtherClientLogin;
        var resultCombat = new ResultCombat()
        {
            CurrentLevelEnemy = enemy.CharData().Progression.Level,
            TypeCombat = ETypeCombat.LuanBanRealTime,
            TypeResultCombat = ETypeResultCombat.LoseByRun,
            CurrentHPUser = charData.Stats.Hp,
            CurrentThanThuc = charData.Stats.ThanThuc,
            CurrentThoNguyen = charData.Stats.ThoNguyen,
            ListIdThanThong = new()
        };
        EndCombatWithResult(resultCombat);
    }
    public void OnDisconnect(IProtocol<MessageTag> protocol)
    {
        OnRemove();
    }

    public MessageState State => MessageState.CombatPvp;

    public void Send(Span<byte> data)
    {
        if (!IsReady) return;
        client.Send(data);
    }
}