using MTS.Data;
using MTS.Game.DP;
using MTS.Models;

namespace MTS.Network.ClientState;

public class DongPhuState : IClientState
{
    public Client client;
    private DongPhuObserver observer;
    private DongPhuConfig config;
    private DongPhuLevel level;
    private int currentWateringHelpCount;
    private bool dirty;
    public DongPhuState(Client client, DongPhuObserver observer, DongPhuConfig config, DongPhuLevel level, int currentWateringHelpCount)
    {
        this.client = client;
        this.observer = observer;
        this.config = config;
        this.level = level;
        this.currentWateringHelpCount = currentWateringHelpCount;
        client.OnOtherClientLogin += OnOtherClientLogin;
        client.OnLogout += OnLogout;
        client.OnRemoveDisconnectedSession += OnRemoveDisconnectedSession;
    }

    public bool IsOwnerDongPhu()
    {
        return IsMyDongPhu(observer.DongPhu.CharId);
    }

    public bool IsMyDongPhu(int charId)
    {
        return client.CharacterData.Id == charId;
    }
    public void SetLevel(DongPhuLevel level)
    {
        this.level = level;
        dirty = true;
    }
    public void OnResetDaily(DongPhu dongPhu)
    {
        currentWateringHelpCount = dongPhu.CurrentWateringHelpCount;
        dirty = true;
    }
    public void OnEnter()
    {
    }
    public async Task SaveChanges(AppDbContext db)
    {
    }
    public void Handle(Message<MessageTag> message)
    {
        var tag = message.Tag;
        var reader = message.Reader;
    }
    public void OnLeave()
    {
        observer.RemoveState(this);
    }

    private void OnRemoveDisconnectedSession(Client client)
    {
        observer.RemoveState(this);
    }
    private void OnLogout(Client client)
    {
        observer.RemoveState(this);
    }
    private void OnOtherClientLogin(Client client)
    {
        observer.RemoveState(this);
    }

    public void OnDisconnect(IProtocol<MessageTag> protocol)
    {
        
    }

    public MessageState State => MessageState.DongPhuState;

    public void OnRemove()
    {
        client.OnOtherClientLogin -= OnOtherClientLogin;
        client.OnLogout -= OnLogout;
        client.OnRemoveDisconnectedSession -= OnRemoveDisconnectedSession;
    }

}