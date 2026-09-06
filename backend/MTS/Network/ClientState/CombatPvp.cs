namespace MTS.Network.ClientState;

public class CombatPvp : IClientState
{
    private Client client;
    private CombatPvp enemy;
    public CombatPvp(Client client)
    {
        this.client = client;
    }

    public void SetEnemy(CombatPvp enemy)
    {
        this.enemy = enemy;
    }
    public void OnEnter()
    {
        
    }
    public void Handle(Message<MessageTag> message)
    {
    }
    public void OnLeave()
    {
    }
    public void OnDisconnect(IProtocol<MessageTag> protocol)
    {
    }
    public MessageState State => MessageState.CombatPvp;
}