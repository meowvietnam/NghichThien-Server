using MTS.Game.Boss;
using MTS.Models;
using MTS.Network.Models;

namespace MTS.Network.ClientState;

public class BossState : IClientState, IBossFighter
{
    private readonly Client client;
    private readonly BossManager boss;
    private bool isReady;
    public int Id => CharData?.Id ?? 0;
    public CharacterData CharData => client.CharacterData;

    public BossState(Client client, BossManager boss)
    {
        this.client = client;
        this.boss = boss;
        BindEvent();
    }

    private void BindEvent()
    {
        client.OnLogout += OnLogout;
        client.OnOtherClientLogin += OnOtherClientLogin;
    }

    private void UnbindEvent()
    {
        client.OnLogout -= OnLogout;
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
        boss.OnPlayerJoin(this);
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
    }

    public void Send(Span<byte> data)
    {
        if (!isReady) return;
        client.Send(data);
    }

    public void OnDisconnect(IProtocol<MessageTag> protocol)
    {
    }

    public MessageState State => MessageState.BossState;
}