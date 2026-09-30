using MTS.Exceptions;
using MTS.Game.DP;
using MTS.Game.DP.Achievements;
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
        client.OnDisconnectedSessionExpired += OnDisconnectedSessionExpired;
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
            NotifyAchievements();
        }
        else if (tag == MessageTag.GetDongPhuAchievements)
        {
            var achievements = MyDongPhu().Achievements;
            var result = achievements == null
                ? new GetDongPhuAchievementsResult { Success = false, Message = "Thành tựu chưa sẵn sàng" }
                : new GetDongPhuAchievementsResult { Success = true, Achievements = achievements.BuildViews() };
            client.MemSend(MessageTag.GetDongPhuAchievements, result);
        }
        else if (tag == MessageTag.ClaimDongPhuAchievement)
        {
            var request = reader.MemRead<ClaimDongPhuAchievementRequest>();
            ClaimDongPhuAchievementResult result;
            try
            {
                result = await ClaimAchievement(request);
            }
            catch (InvalidException e)
            {
                result = new ClaimDongPhuAchievementResult
                {
                    Success = false, Message = e.Message, AchievementId = request.AchievementId,
                    TierId = request.TierId
                };
            }

            client.MemSend(MessageTag.ClaimDongPhuAchievement, result);
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
                client.MemSend(MessageTag.HarvestSlotResult, result);
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

    /// <summary>Nhận thưởng một mốc thành tựu của động phủ mình; lưu ngay tiến độ và nhân vật.</summary>
    private async Task<ClaimDongPhuAchievementResult> ClaimAchievement(ClaimDongPhuAchievementRequest request)
    {
        var achievements = MyDongPhu().Achievements ?? throw new InvalidException("Thành tựu chưa sẵn sàng");
        var charData = client.CharacterData ?? throw new InvalidException("Nhân vật không tồn tại");
        var rewards = achievements.Claim(request.AchievementId, request.TierId);
        lock (client.CharacterLock)
        {
            foreach (var reward in rewards) reward.Claim(charData);
        }

        Log.Information("DongPhu achievement claimed: CharId={CharId} {AchievementId}:{TierId}", charData.Id,
            request.AchievementId, request.TierId);
        await referenceVisitor.SaveChanges();
        _ = Task.Run(client.SaveChanges);
        return new ClaimDongPhuAchievementResult
        {
            Success = true,
            AchievementId = request.AchievementId,
            TierId = request.TierId,
            Rewards = rewards,
            ClaimableCount = achievements.CountClaimable()
        };
    }

    /// <summary>Cộng chỉ số thành tựu cho động phủ của mình; vừa đạt mốc mới thì báo chấm đỏ.</summary>
    private void AddMyStat(DongPhuStat stat, long amount = 1)
    {
        if (MyDongPhu().Achievements?.Add(stat, amount) == true) NotifyAchievements();
    }

    /// <summary>Cộng chỉ số cho động phủ đang thăm (chủ động phủ có thể offline; chấm đỏ cập nhật khi họ mở bảng).</summary>
    private void AddTargetStat(DongPhuStat stat)
    {
        var target = TargetDongPhu();
        if (ReferenceEquals(target, MyDongPhu())) return;
        target.Achievements?.Add(stat);
    }

    public void NotifyAchievements()
    {
        var achievements = MyDongPhu().Achievements;
        if (achievements == null) return;
        client.MemSend(MessageTag.DongPhuAchievementNotify,
            new DongPhuAchievementNotify { ClaimableCount = achievements.CountClaimable() });
    }

    private void Like()
    {
        var targetDongPhu = TargetDongPhu();
        var charData = client.CharacterData;
        var result = targetDongPhu.Like(charData);
        AddMyStat(DongPhuStat.LikeGiven);
        AddTargetStat(DongPhuStat.LikeReceived);
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
        AddMyStat(DongPhuStat.Harvest);
        var notify = new HarvestSlotNotify()
        {
            SlotIndex = result.SlotIndex
        };
        client.MemSend(MessageTag.HarvestSlotResult, result);
        observer.SendAllIgnore(this, MessageTag.HarvestSlotNotify, notify);
    }

    private BuySeedResult BuySeed(BuySeed request)
    {
        var myDongPhu = MyDongPhu();
        var charData = client.CharacterData;
        var result = myDongPhu.BuySeed(request, charData);
        if (result.Success) AddMyStat(DongPhuStat.SeedBought, request.Count);
        return result;
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
        // Cấp động phủ là chỉ số thành tựu (đọc trực tiếp): báo lại chấm đỏ.
        if (result.Success) NotifyAchievements();
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
        AddMyStat(DongPhuStat.Plant);
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
        AddMyStat(DongPhuStat.WaterHelpGiven);
        AddTargetStat(DongPhuStat.WaterHelpReceived);
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

    private void OnDisconnectedSessionExpired(Client client)
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
        Log.Information("On other login");
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
        client.OnDisconnectedSessionExpired -= OnDisconnectedSessionExpired;
    }

    public void ToAllMap()
    {
        client.Send(MessageTag.ToAllMapFromDongPhu);
    }
    public void ReloadShop()
    {
        var myDongPhu = MyDongPhu();
        myDongPhu.ReloadShop();
        var result = new ReloadDpShopResult()
        {
            Success = true,
            Message = "Làm mới cửa hàng thành công",
            Shop = myDongPhu.Shop
        };
        client.MemSend(MessageTag.ReloadDpShopResult, result);
    }
}
