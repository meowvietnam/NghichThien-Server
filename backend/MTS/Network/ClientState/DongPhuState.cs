using MTS.Data;
using MTS.Exceptions;
using MTS.Game.DP;
using MTS.Models;
using MTS.Network.Models;

namespace MTS.Network.ClientState;

public class DongPhuState : IClientState
{
    public Client client;
    private DongPhuObserver observer;
    private DongPhuReference referenceTarget;
    private DongPhuReference referenceVisitor;
    public MessageState State => MessageState.DongPhuState;

    public DongPhuState(Client client, DongPhuObserver observer, DongPhuReference referenceTarget,
        DongPhuReference referenceVisitor)
    {
        this.client = client;
        this.observer = observer;
        this.referenceTarget = referenceTarget;
        this.referenceVisitor = referenceVisitor;
        client.OnOtherClientLogin += OnOtherClientLogin;
        client.OnLogout += OnLogout;
        client.OnRemoveDisconnectedSession += OnRemoveDisconnectedSession;
    }

    public DongPhu MyDongPhu()
    {
        return referenceVisitor.DongPhu;
    }

    public DongPhu TargetDongPhu()
    {
        return referenceTarget.DongPhu;
    }

    public bool IsOwnerDongPhu()
    {
        return IsMyDongPhu(observer.DongPhu.CharId);
    }

    public bool IsMyDongPhu(int charId)
    {
        return client.CharacterData.Id == charId;
    }

    public void OnEnter()
    {
    }

    public void Handle(Message<MessageTag> message)
    {
        var tag = message.Tag;
        var reader = message.Reader;
    }

    private void HelpWatering(WateringHelpRequest request)
    {
        var myDongPhu = MyDongPhu();
        var targetDongPhu = TargetDongPhu();
        var helper = client.CharacterData!;
        myDongPhu.HelpWatering(request, helper, targetDongPhu);
    }

    public void OnLeave()
    {
        observer.RemoveState(this);
        referenceTarget.RemoveState(this);
        referenceVisitor.RemoveState(this);
    }

    private void OnRemoveDisconnectedSession(Client client)
    {
        observer.RemoveState(this);
        referenceTarget.RemoveState(this);
        referenceVisitor.RemoveState(this);
    }

    private void OnLogout(Client client)
    {
        observer.RemoveState(this);
        referenceTarget.RemoveState(this);
        referenceVisitor.RemoveState(this);
    }

    private void OnOtherClientLogin(Client client)
    {
        observer.RemoveState(this);
        referenceTarget.RemoveState(this);
        referenceVisitor.RemoveState(this);
    }

    public void OnDisconnect(IProtocol<MessageTag> protocol)
    {
    }


    public void OnRemove()
    {
        client.OnOtherClientLogin -= OnOtherClientLogin;
        client.OnLogout -= OnLogout;
        client.OnRemoveDisconnectedSession -= OnRemoveDisconnectedSession;
    }
}