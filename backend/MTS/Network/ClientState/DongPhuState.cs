using MTS.Exceptions;
using MTS.Game.DP;
using MTS.Models;
using MTS.Network.Core;
using MTS.Network.Models;
using Serilog;

namespace MTS.Network.ClientState;

public class DongPhuState : IClientState, ISender<MessageTag>
{
    public Client client;
    private DongPhuObserver observer;
    private DongPhuReference referenceTarget;
    private DongPhuReference referenceVisitor;
    public MessageState State => MessageState.DongPhuState;
    private bool isClientReady = false;
    private object lockObject = new();

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

    public bool IsMe(int charId)
    {
        return MyDongPhu().CharId == charId;
    }
    public DongPhu MyDongPhu()
    {
        return referenceVisitor.DongPhu;
    }

    public DongPhu TargetDongPhu()
    {
        return referenceTarget.DongPhu;
    }

    public void OnEnter()
    {
    }

    public void Send(Span<byte> data)
    {
        if (!isClientReady)
        {
            return;
        }

        client.Send(data);
    }

    public async void Handle(Message<MessageTag> message)
    {
        var tag = message.Tag;
        var reader = message.Reader;
        if (tag == MessageTag.GetDongPhuInfo)
        {
            isClientReady = true;
            var result = GetDongPhuInfo();
            client.MemSend(MessageTag.GetDongPhuInfoResult, result);
        }
        else if (tag == MessageTag.WateringHelp)
        {
            var request = reader.MemRead<WateringHelpRequest>();
            try
            {
                HelpWatering(request);
            }
            catch (InvalidException e)
            {
                var result = new WateringHelpResult()
                {
                    Success = false,
                    Message = e.Message,
                };
                client.MemSend(MessageTag.WateringHelpResult, result);
            }
        }
        else if (tag == MessageTag.Plant)
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
        else if (tag == MessageTag.UpgradeDongPhu)
        {
            var request = reader.MemRead<UpgradeDongPhu>();
            try
            {
                await UpgradeDongPhu(request);
            }
            catch (InvalidException e)
            {
                var result = new UpgradeDongPhuResult()
                {
                    Success = false,
                    Message = e.Message,
                };
                client.MemSend(MessageTag.UpgradeDongPhuResult, result);
            }
        }
        else if (tag == MessageTag.GetDongPhuShopRequest)
        {
            var shop = GetShop();
            client.MemSend(MessageTag.GetDongPhuShopRequest, shop);
        }
        else if (tag == MessageTag.GetDongPhuHistoriesRequest)
        {
            var histories = GetHistories();
            client.MemSend(MessageTag.GetDongPhuHistoriesRequest, histories);
        }
        else if (tag == MessageTag.GetDongPhuInventoryRequest)
        {
            var inventory = GetInventory();
            client.MemSend(MessageTag.GetDongPhuInventoryRequest, inventory);
        }
        else if (tag == MessageTag.BuySeedRequest)
        {
            var request = reader.MemRead<BuySeed>();
            BuySeedResult result = null!;
            try
            {
                result = BuySeed(request);
            }
            catch (InvalidException e)
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
        else if (tag == MessageTag.HarvestSlot)
        {
            var request = reader.MemRead<HarvestRequest>();
            try
            {
                HarvestSlot(request);
            }
            catch (InvalidException e)
            {
                var result = new HarvestSlotResult()
                {
                    Success = false,
                    Message = e.Message
                };
                client.MemSend(MessageTag.HarverstSlotResult, result);
            }
        }
        else if (tag == MessageTag.LikeDongPhu)
        {
            try
            {
                Like();
            }
            catch (InvalidException e)
            {
                var result = new LikeDongPhuResult()
                {
                    Success = false,
                    Message = e.Message
                };
                client.MemSend(MessageTag.LikeDongPhuResult, result);
            }
        }
        else if (tag == MessageTag.VisitDongPhuRequest)
        {
            var request = reader.MemRead<VisitDongPhu>();
            VisitDongPhuResult result = null!;
            try
            {
                lock (lockObject)
                {
                    VisitDongPhu(request).GetAwaiter().GetResult();
                    result = new VisitDongPhuResult()
                    {
                        Success = true
                    };
                }
            }
            catch (InvalidException e)
            {
                result = new VisitDongPhuResult()
                {
                    Success = false,
                    Message = e.Message
                };
            }
            finally
            {
                client.MemSend(MessageTag.VisitDongPhuRequest, result);
            }
        }
        else if (tag == MessageTag.ReloadDpShop)
        {
            try
            {
                var charData = client.CharacterData;
                if (charData.IsNoAds)
                {
                    ReloadShop();
                }
                else
                {
                    var showAds = new ReloadDpShopAds("ReloadDpShop","Xem quảng cáo làm mới cửa hàng",client);
                    var handler = client.GetHandler<AdsHandler>();
                    handler.SendAds(showAds);
                }
            }
            catch (InvalidException e)
            {
                   
            }
        }
    }

    private async Task<VisitDongPhuResult> VisitDongPhu(VisitDongPhu request)
    {
        var name = request.Name;
        var state = await DongPhuManager.VisitDongPhu(name, client);
        client.ChangeState(state);
        return new VisitDongPhuResult()
        {
            Success = true,
        };
    }

    private void Like()
    {
        var targetDongPhu = TargetDongPhu();
        var charData = client.CharacterData;
        var result = targetDongPhu.Like(charData);
        var notify = new LikeDongPhuNotify()
        {
            NewLike = result.NewLike
        };
        client.MemSend(MessageTag.LikeDongPhuResult, result);
        observer.SendAllIgnore(this, MessageTag.LikeDongPhuNotify, notify);
    }

    private void HarvestSlot(HarvestRequest request)
    {
        var targetDongPhu = TargetDongPhu();
        var charData = client.CharacterData;
        var result = targetDongPhu.TryHarvest(request, charData);
        var notify = new HarvestSlotNotify()
        {
            SlotIndex = result.SlotIndex
        };
        client.MemSend(MessageTag.HarverstSlotResult, result);
        observer.SendAllIgnore(this, MessageTag.HarvertSlotNotify, notify);
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
        return myDongPhu.GetInventory();
    }

    private Queue<DongPhuHistory> GetHistories()
    {
        var myDongPhu = MyDongPhu();
        return myDongPhu.GetHistories();
    }

    private DongPhuShop GetShop()
    {
        var myDongPhu = MyDongPhu();
        return myDongPhu.GetShop();
    }

    private async Task UpgradeDongPhu(UpgradeDongPhu request)
    {
        if (request.By == UpgradeDongPhuBy.MoneyVip)
        {
            var dongPhu = MyDongPhu();
            var charData = client.CharacterData;
            var result = dongPhu.TryUpgrade(charData, true);
            SendUpgradeResult(result);
        }
        else
        {
            var link = await DongPhuManager.CreateUpgradePaymentLink(MyDongPhu());
            var openLink = new OpenLink()
            {
                Link = link
            };
            client.MemSend(MessageTag.OpenLink, openLink);
        }
    }
    public void SendUpgradeResult(UpgradeDongPhuResult result)
    {
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
        var charId = client.CharacterData.Id;
        var canLike = dongPhu.CanLike(charId);
        var myDongPhu = MyDongPhu();
        return new GetDongPhuInfoResult()
        {
            Success = true,
            DongPhuInfo = new DongPhuInfo()
            {
                Config = dongPhu.GetConfig(),
                DongPhu = dongPhu,
                MyLevel = myDongPhu.Level,
                CurrentWateringHelpCount = myDongPhu.CurrentWateringHelpCount,
                CanLike = canLike,
            },
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
        observer.SendAllIgnore(this, MessageTag.WateringHelpNotify, notify);
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

    public void ToAllMap()
    {
        client.Send(MessageTag.ToAllMapFromDongPhu);
    }
    public void ReloadShop()
    {
        var myDongPhu = MyDongPhu();
        myDongPhu.Shop.ResetDaily();
        var result = new ReloadDpShopResult()
        {
            Success = true,
            Message = "Làm mới cửa hàng thành công",
            Shop = myDongPhu.Shop
        };
        client.MemSend(MessageTag.ReloadDpShopResult, result);
    }
}