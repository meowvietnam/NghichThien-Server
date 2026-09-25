using MTS.Game.Clan;
using MTS.Models;
using MTS.Network.Models;

namespace MTS.Network.ClientState;

public class TongMonChien : IClientState, IClanObserverState
{
    private readonly Client client;
    public int CharId => client.CharacterData?.Id ?? 0;
    private ClanReference reference;
    private ClanObserver observer;
    public TongMonChien(Client client)
    {
        this.client = client;
    }
    
    public async void OnEnter()
    {
        
    }
    public void Handle(Message<MessageTag> message)
    {
        var tag = message.Tag;
        var reader = message.Reader;
    }
    public void OnLeave()
    {
    }
    public void OnDisconnect(IProtocol<MessageTag> protocol)
    {
    }
    public void OnClanAddMember(ClanMember member)
    {
    }

    public void OnClanRemoveMember(int clanId, int charId, ClanRemoveMemberReason reason)
    {
    }

    public void OnClanContribution(ClanContribution contribution)
    {
    }

    public void MemSend<T>(MessageTag tag, T data)
    {
        client.MemSend(tag, data);
    }
    public void OnClanDeleted(int clanId)
    {
    }
    public MessageState State => MessageState.TongMonChien;
}