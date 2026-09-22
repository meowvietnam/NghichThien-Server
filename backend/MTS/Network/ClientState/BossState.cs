using MTS.Game.Boss;
using MTS.Exceptions;
using MTS.Models;
using MTS.Network.Models;

namespace MTS.Network.ClientState;

public class BossState : IClientState, IBossFighter
{
    private bool isReady;
    private ResultCombatResult? completedResult;
    private readonly Client client;
    private readonly BossManager boss;
    public bool IsConnected => client.TcpClient.IsConnected;
    public int Id => CharData?.Id ?? 0;
    public CharacterData CharData => client.CharacterData;
    public object MainLock => client.CharacterLock;
    public BossState(Client client, BossManager boss)
    {
        this.client = client;
        this.boss = boss;
    }

    private void BindEvent()
    {
        client.OnLogout += OnLogout;
        client.OnDisconnectedSessionExpired += OnLogout;
        client.OnSessionReconnected += OnReconnected;
        client.OnOtherClientLogin += OnOtherClientLogin;
    }

    private void UnbindEvent()
    {
        client.OnLogout -= OnLogout;
        client.OnDisconnectedSessionExpired -= OnLogout;
        client.OnSessionReconnected -= OnReconnected;
        client.OnOtherClientLogin -= OnOtherClientLogin;
    }

    private void OnPlayerLeave()
    {
        UnbindEvent();
        boss.OnPlayerLeave(this);
    }

    private void OnOtherClientLogin(Client client)
    {
        OnPlayerLeave();
    }

    private void OnReconnected(Client client)
    {
        boss.SyncPlayer(this);
    }

    private void OnLogout(Client client)
    {
        OnPlayerLeave();
    }

    public void OnLeave()
    {
        OnPlayerLeave();
    }

    public void OnEnter()
    {
        BindEvent();
    }

    public void Handle(Message<MessageTag> message)
    {
        var tag = message.Tag;
        var reader = message.Reader;
        if (tag == MessageTag.SyncDame)
        {
            var model = reader.MemRead<SyncDame>();
            boss.TakeDame(this, model);
        }
        else if (tag == MessageTag.SyncEndTurnBoss)
        {
            var model = reader.MemRead<SyncEndTurnBoss>();
            boss.SyncEndTurnBoss(this, model);
        }
        else if (tag == MessageTag.ReadyFightingBoss)
        {
            if (isReady) return;
            isReady = boss.OnPlayerReady(this);
            if (!isReady)
            {
                client.MemSend(MessageTag.ResultCombatBoss, new ResultCombatResult
                {
                    Success = false,
                    Message = "Boss đã kết thúc hoặc lượt đánh không còn hợp lệ."
                });
                OnPlayerLeave();
            }
        }
        else if (tag == MessageTag.ResultCombatBoss)
        {
            var resultCombat = reader.MemRead<ResultCombat>();
            EndCombatWithResult(resultCombat);
        }
    }
    private void EndCombatWithResult(ResultCombat resultCombat)
    {
        if (completedResult != null)
        {
            client.MemSend(MessageTag.ResultCombatBoss, completedResult);
            return;
        }
        if (!isReady) return;
        try
        {
            completedResult = boss.OnResultCombat(this, resultCombat);
            isReady = false;
            OnPlayerLeave();
            client.MemSend(MessageTag.ResultCombatBoss, completedResult);
        }
        catch (InvalidException e)
        {
            client.MemSend(MessageTag.ResultCombatBoss, new ResultCombatResult
            {
                Success = false,
                Message = e.Message
            });
        }
    }

    public void Send(Span<byte> data)
    {
        client.Send(data);
    }

    public void OnDisconnect(IProtocol<MessageTag> protocol)
    {
        // Keep this attempt during the reconnect grace period. Session expiry removes it.
    }

    public MessageState State => MessageState.BossState;
}