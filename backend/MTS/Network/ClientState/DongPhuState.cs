using MTS.Exceptions;
using MTS.Game.DP;
using MTS.Models;
using MTS.Network.Models;
namespace MTS.Network.ClientState;

public class DongPhuState : IClientState, ISender<MessageTag>
{
    public Client client;
    private DongPhuObserver observer;
    private DongPhuReference referenceTarget;
    private DongPhuReference referenceVisitor;
    public MessageState State => MessageState.DongPhuState;
    private bool isClientReady = false;
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
    public void Send(Span<byte> data)
    {
        if(!isClientReady)
        {
            return;
        }
        client.Send(data);
    }
    public void Handle(Message<MessageTag> message)
    {
        var tag = message.Tag;
        var reader = message.Reader;
        if(tag == MessageTag.GetDongPhuInfo)
        {
            isClientReady = true;
            var result = GetDongPhuInfo();
            client.MemSend(MessageTag.GetDongPhuInfoResult, result);
        }
        else if(tag == MessageTag.WateringHelp)
        {
            var request = reader.MemRead<WateringHelpRequest>();
            try
            {
                HelpWatering(request);
            }
            catch(InvalidException e)
            {
                var result = new WateringHelpResult()
                {
                    Success = false,
                    Message = e.Message,
                };
                client.MemSend(MessageTag.WateringHelpResult, result);
            }
        }
        else if(tag == MessageTag.Plant)
        {
            var request = reader.MemRead<PlantRequest>();
            try
            {
                Plant(request);
            }
            catch (InvalidException e)
            {
                var result = new PlantResult()
                {
                    Success = false,
                    Message = e.Message,
                };
                client.MemSend(MessageTag.PlantResult, result);
            }
        }
        else if(tag == MessageTag.UpgradeDongPhu)
        {
            try
            {
                UpgradeDongPhu();
            }
            catch(InvalidException e)
            {
                var result = new UpgradeDongPhuResult()
                {
                    Success = false,
                    Message = e.Message,
                };
                client.MemSend(MessageTag.UpgradeDongPhuResult, result);
            }
        }
        else if(tag == MessageTag.GetDongPhuShopRequest)
        {
            var shop = GetShop();
            client.MemSend(MessageTag.GetDongPhuShopRequest, shop);
        }
        else if(tag == MessageTag.GetDongPhuHistoriesRequest)
        {
            var histories = GetHistories();
            client.MemSend(MessageTag.GetDongPhuHistoriesRequest, histories);
        }
        else if(tag == MessageTag.GetDongPhuInventoryRequest)
        {
            var inventory = GetInventory();
            client.MemSend(MessageTag.GetDongPhuInventoryRequest, inventory);
        }
        else if(tag == MessageTag.BuySeedRequest)
        {
            var request = reader.MemRead<BuySeed>();
            BuySeedResult result = null!;
            try
            {
                result = BuySeed(request);
            }
            catch(InvalidException e)
            {
                result = new BuySeedResult()
                {
                    Success = false,
                    Message = e.Message
                };
            }
            finally
            {
                client.MemSend(MessageTag.BuySeedRequest, result);
            }
        }
    }
    private BuySeedResult BuySeed(BuySeed request)
    {
        var myDongPhu = MyDongPhu();
        var charData = client.CharacterData;
        return myDongPhu.BuySeed(request, charData);
    }
    private DongPhuInventory GetInventory()
    {
        var myDongPhu = MyDongPhu();
        return myDongPhu.Inventory;
    }

    private Queue<DongPhuHistory> GetHistories()
    {
        var myDongPhu = MyDongPhu();
        return myDongPhu.Histories;
    }

    private DongPhuShop GetShop()
    {
        var myDongPhu = MyDongPhu();
        return myDongPhu.Shop;
    }
    private void UpgradeDongPhu()
    {
        var dongPhu = TargetDongPhu();
        var charData = client.CharacterData;
        var result = dongPhu.TryUpgrade(charData);
        var notify = new UpgradeDongPhuNotify()
        {
            NewLevel = result.NewLevel,
        };
        client.MemSend(MessageTag.UpgradeDongPhuResult, result);
        observer.SendAllIgnore(this, MessageTag.UpgradeDongPhuNotify, notify);
    }

    private GetDongPhuInfoResult GetDongPhuInfo()
    {
        var dongPhu = TargetDongPhu();
        return new GetDongPhuInfoResult()
        {
            Success = true,
            Config = dongPhu.GetConfig(),
            DongPhu = dongPhu
        };
    }
    private void Plant(PlantRequest request)
    {
        var targetDongPhu = TargetDongPhu();
        var charData = client.CharacterData;
        var result = targetDongPhu.Plant(request, charData);
        var notify = new PlantNotify()
        {
            PlantSeed = result.PlantSeed
        };
        client.MemSend(MessageTag.PlantResult, result);
        observer.SendAllIgnore(this, MessageTag.PlantNotify, notify);
    }
    private void HelpWatering(WateringHelpRequest request)
    {
        var myDongPhu = MyDongPhu();
        var targetDongPhu = TargetDongPhu();
        var helper = client.CharacterData!;
        var result = myDongPhu.HelpWatering(request, helper, targetDongPhu);
        var notify = new WateringHelpNotify()
        {
            SlotIndex = result.SlotIndex,
            ReduceTime = result.ReduceTime,
        };
        client.MemSend(MessageTag.WateringHelpResult, result);
        observer.SendAllIgnore(this,MessageTag.WateringHelpNotify, notify);
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