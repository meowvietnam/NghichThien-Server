using MTS.Exceptions;
using MTS.Game.Clan;
using MTS.Models;
using MTS.Network.Models;
using MTS.Services;
using MTS.Utils;
using Newtonsoft.Json;

namespace MTS.Network.Core;

public class ClanHandler : IClientHandler, IClanObserverState
{
    private Client client;
    private CharacterData charData;
    private bool isReady;
    private bool isDisposed;
    private ClanObserver? observer;
    private ClanReference? reference;
    private Task watchTask = Task.CompletedTask;
    private readonly object lockObject = new();

    public ClanHandler(Client client)
    {
        this.client = client;
        charData = client.CharacterData;
        client.OnLogout += OnLogout;
        client.OnDisconnectedSessionExpired += OnDisconnectedSessionExpired;
        client.OnOtherClientLogin += OnOtherClientLogin;
        client.OnReconnectedSuccess += OnReconnectedSuccess;
    }

    private void OnReconnectedSuccess()
    {
        if (isDisposed)
        {
            return;
        }

        watchTask = WatchMyClan(true);
    }

    private void OnOtherClientLogin(Client obj)
    {
        UnwatchClan();
        DisposeClientEvents();
    }

    private void OnDisconnectedSessionExpired(Client obj)
    {
        if (!obj.IsOnline())
        {
            UnwatchClan();
            DisposeClientEvents();
        }
    }

    private void OnLogout(Client obj)
    {
        UnwatchClan();
        DisposeClientEvents();
    }

    public void OnEnter()
    {
        watchTask = WatchMyClan(false);
    }

    private async Task WatchMyClan(bool syncToClient)
    {
        try
        {
            var currentReference = await ClanManager.FindReferenceByMember(CharId);
            var clanId = currentReference.Clan.Id;
            ClanObserver watchedObserver;
            ClanReference watchedReference;
            var shouldUnwatchCurrent = false;
            lock (lockObject)
            {
                if (reference?.Clan.Id == clanId && observer != null)
                {
                    if (syncToClient)
                    {
                        MemSend(MessageTag.NewClan, GetMyClan());
                    }

                    return;
                }

                shouldUnwatchCurrent = reference != null || observer != null;
            }

            if (shouldUnwatchCurrent)
            {
                UnwatchClan();
            }

            (watchedObserver, watchedReference) = await ClanManager.WatchClan(clanId, this);
            lock (lockObject)
            {
                observer = watchedObserver;
                reference = watchedReference;
            }

            if (syncToClient)
            {
                MemSend(MessageTag.NewClan, GetMyClan());
            }
        }
        catch (InvalidException)
        {
            UnwatchClan();
        }
        finally
        {
            isReady = true;
        }
    }

    public async Task<CreateClanResult> CreateClan(CreateClanRequest request)
    {
        var config = GetConfig();
        request.LeaderCharId = CharId;
        request.MaxMembers = config.CreateMaxMember;
        CreateClanResult result = null!;
        try
        {
            var createBy = request.CreateBy;
            if (createBy == CreateClanBy.MoneyVip)
            {
                var clanRef = await ClanManager.CreateClan(request, charData);
                await OnClanCreated(clanRef.Clan);
                result = new CreateClanResult()
                {
                    By = request.CreateBy,
                    Success = true,
                    NewMoneyVip = charData.MoneyVip
                };
            }
            else if(createBy == CreateClanBy.Vnd)
            {
                await ClanManager.ValidateCreateClanRequest(request, charData);
                var amount = config.CreateClanVndPrice;
                var paymentLink = await PaymentService.Instance.CreateLink(amount, TimeSpan.FromHours(1));
                var orderCode = paymentLink.OrderCode;
                var expireTime = DateTimeOffset
                    .FromUnixTimeSeconds(paymentLink.ExpiredAt.Value)
                    .UtcDateTime;
                var order = new Order()
                {
                    CharId = CharId,
                    OrderType = OrderType.CreateClan,
                    ProductId = null,
                    OrderId = orderCode,
                    Link = paymentLink.CheckoutUrl,
                    Amount = amount,
                    ExpireAt = expireTime,
                    Data = JsonConvert.SerializeObject(request)
                };
                await using var db = await Program.CreateDbAsync();
                await db.Orders.AddAsync(order);
                await db.SaveChangesAsync();
                result = new CreateClanResult()
                {
                    By = request.CreateBy,
                    Success = true,
                    Limk = paymentLink.CheckoutUrl
                };
            }
        }
        catch (InvalidException e)
        {
            result = new CreateClanResult()
            {
                By = request.CreateBy,
                Success = false,
                Message = e.Message
            };
        }

        return result;
    }
    public Clan? GetMyClan()
    {
        lock (lockObject)
        {
            return reference?.Clan ?? null;
        }
    }
    public async Task OnClanCreated(Clan clan)
    {
        (observer, reference) = await ClanManager.WatchClan(clan.Id, this);
        MemSend(MessageTag.NewClan, GetMyClan());
    }
    public void Handle(Message<MessageTag> message)
    {
        var tag = message.Tag;
        var reader = message.Reader;
        if (tag == MessageTag.CreateClanRequest)
        {
            var request = reader.MemRead<CreateClanRequest>();
            lock (client.CharacterLock)
            {
                var result = CreateClan(request).GetAwaiter().GetResult();
                MemSend(MessageTag.CreateClanRequest, result);
            }
        }
        else if (tag == MessageTag.GetClanRequest)
        {
            reader.MemRead<GetClanRequest>();
            GetClanResult result;
            try
            {
                result = GetClan().GetAwaiter().GetResult();
            }
            catch (Exception exception)
            {
                Serilog.Log.Error(exception, "Không thể tải tông môn; CharacterId={CharacterId}", CharId);
                result = new GetClanResult
                {
                    Success = false,
                    Message = "Không thể tải thông tin tông môn",
                    Config = GetConfig()
                };
            }

            // Đây là response trực tiếp. Không dùng MemSend của IClanObserverState vì hàm đó
            // cố ý bỏ broadcast trước khi WatchMyClan hoàn tất và có thể làm request bị timeout.
            client.MemSend(MessageTag.GetClanRequest, result);
        }
        else if (tag == MessageTag.GetClanMapNodesRequest)
        {
            reader.MemRead<GetClanMapNodesRequest>();
            GetClanMapNodesResult result;
            try
            {
                result = GetClanMapNodes().GetAwaiter().GetResult();
            }
            catch (Exception exception)
            {
                Serilog.Log.Error(exception, "Không thể tải danh sách vị trí tông môn; CharacterId={CharacterId}", CharId);
                result = new GetClanMapNodesResult
                {
                    Success = false,
                    Message = "Không thể tải danh sách vị trí tông môn"
                };
            }

            client.MemSend(MessageTag.GetClanMapNodesRequest, result);
        }
        else if (tag == MessageTag.UploadClanImageRequest)
        {
            var request = reader.MemRead<UploadImageRequest>();
            lock (client.CharacterLock)
            {
                var result = UploadImage(request).GetAwaiter().GetResult();
                MemSend(MessageTag.UploadClanImageRequest, result);
            }
        }
        else if (tag == MessageTag.AddClanMemberRequest)
        {
            var request = reader.MemRead<AddClanMemberRequest>();
            lock (client.CharacterLock)
            {
                var result = AddMember(request).GetAwaiter().GetResult();
                MemSend(MessageTag.AddClanMemberRequest, result);
            }
        }
        else if (tag == MessageTag.KickClanMemberRequest)
        {
            var request = reader.MemRead<KickClanMemberRequest>();
            lock (client.CharacterLock)
            {
                var result = KickMember(request).GetAwaiter().GetResult();
                MemSend(MessageTag.KickClanMemberRequest, result);
            }
        }
        else if (tag == MessageTag.LeaveClanRequest)
        {
            var request = reader.MemRead<LeaveClanRequest>();
            lock (client.CharacterLock)
            {
                var result = LeaveClan(request).GetAwaiter().GetResult();
                MemSend(MessageTag.LeaveClanRequest, result);
            }
        }
        else if (tag == MessageTag.ClanContributionRequest)
        {
            var request = reader.MemRead<ClanContributionRequest>();
            lock (client.CharacterLock)
            {
                var result = Contribute(request).GetAwaiter().GetResult();
                MemSend(MessageTag.ClanContributionRequest, result);
            }
        }
        else if (tag == MessageTag.SearchClanRequest)
        {
            var request = reader.MemRead<SearchClanRequest>();
            var result = SearchClan(request).GetAwaiter().GetResult();
            MemSend(MessageTag.SearchClanRequest, result);
        }
        else if (tag == MessageTag.GetClanShopRequest)
        {
            reader.MemRead<GetClanShopRequest>();
            lock (client.CharacterLock)
            {
                MemSend(tag, GetShop());
            }
        }
        else if (tag == MessageTag.BuyClanShopRequest)
        {
            var request = reader.MemRead<BuyClanShopRequest>();
            lock (client.CharacterLock)
            {
                var result = BuyShopItem(request).GetAwaiter().GetResult();
                MemSend(tag, result);
            }
        }
        else if (tag == MessageTag.AddClanShopItemRequest)
        {
            var request = reader.MemRead<AddClanShopItemRequest>();
            lock (client.CharacterLock)
            {
                var result = AddShopItem(request).GetAwaiter().GetResult();
                MemSend(tag, result);
            }
        }
        else if (tag == MessageTag.RemoveClanShopItemRequest)
        {
            var request = reader.MemRead<RemoveClanShopItemRequest>();
            lock (client.CharacterLock)
            {
                var result = RemoveShopItem(request).GetAwaiter().GetResult();
                MemSend(tag, result);
            }
        }
        else if (tag == MessageTag.ViewClanRequest)
        {
            var id = reader.MemRead<int>();
            var clan = ViewClan(id).GetAwaiter().GetResult();
            MemSend(MessageTag.ViewClanRequest, clan);
        }
        else if (tag == MessageTag.SetClanMemberRoleRequest)
        {
            var request = reader.MemRead<SetClanMemberRoleRequest>();
            lock (client.CharacterLock)
            {
                var result = SetMemberRole(request).GetAwaiter().GetResult();
                MemSend(MessageTag.SetClanMemberRoleRequest, result);
            }
        }
        else if (tag == MessageTag.RequestJoinClanRequest)
        {
            var request = reader.MemRead<RequestJoinClanRequest>();
            lock (client.CharacterLock)
            {
                var result = RequestJoinClan(request).GetAwaiter().GetResult();
                MemSend(MessageTag.RequestJoinClanRequest, result);
            }
        }
        else if (tag == MessageTag.ApproveClanJoinRequestRequest)
        {
            var request = reader.MemRead<ApproveClanJoinRequestRequest>();
            lock (client.CharacterLock)
            {
                var result = ApproveJoinRequest(request).GetAwaiter().GetResult();
                MemSend(MessageTag.ApproveClanJoinRequestRequest, result);
            }
        }
        else if (tag == MessageTag.RejectClanJoinRequestRequest)
        {
            var request = reader.MemRead<RejectClanJoinRequestRequest>();
            lock (client.CharacterLock)
            {
                var result = RejectJoinRequest(request).GetAwaiter().GetResult();
                MemSend(MessageTag.RejectClanJoinRequestRequest, result);
            }
        }
        else if (tag == MessageTag.SetClanInheritanceRequest)
        {
            var request = reader.MemRead<SetClanInheritanceRequest>();
            lock (client.CharacterLock)
            {
                var result = SetInheritance(request).GetAwaiter().GetResult();
                MemSend(MessageTag.SetClanInheritanceRequest, result);
            }
        }
        else if (tag == MessageTag.UpgradeClanWithFundsRequest)
        {
            var request = reader.MemRead<UpgradeClanWithFundsRequest>();
            lock (client.CharacterLock)
            {
                var result = UpgradeWithFunds(request).GetAwaiter().GetResult();
                MemSend(MessageTag.UpgradeClanWithFundsRequest, result);
            }
        }
    }

    public async Task<Clan?> ViewClan(int id)
    {
        return await ClanManager.ViewClan(id);
    }
    public async Task<GetClanResult> GetClan()
    {
        await watchTask;
        return new GetClanResult
        {
            Success = true,
            Clan = GetMyClan(),
            Config = GetConfig()
        };
    }

    public async Task<GetClanMapNodesResult> GetClanMapNodes()
    {
        return new GetClanMapNodesResult
        {
            Success = true,
            Nodes = await ClanManager.GetClanMapNodes()
        };
    }

    public async Task<SetClanInheritanceResult> SetInheritance(SetClanInheritanceRequest request)
    {
        try
        {
            var clanReference = reference ?? throw new InvalidException("Chưa gia nhập tông môn");
            return await ClanManager.SetInheritance(clanReference, charData, request);
        }
        catch (InvalidException e)
        {
            return new SetClanInheritanceResult
            {
                Success = false,
                Message = e.Message,
                NewMoneyVip = charData.MoneyVip
            };
        }
    }

    public async Task<UpgradeClanWithFundsResult> UpgradeWithFunds(UpgradeClanWithFundsRequest request)
    {
        try
        {
            var clanReference = reference ?? throw new InvalidException("Chưa gia nhập tông môn");
            return await ClanManager.UpgradeWithFunds(clanReference, charData, request.Funds);
        }
        catch (InvalidException e)
        {
            var clan = reference?.Clan;
            return new UpgradeClanWithFundsResult
            {
                Success = false,
                Message = e.Message,
                ClanFunds = clan?.Funds ?? 0,
                Exp = clan?.Exp ?? 0,
                Level = clan?.Level ?? 0,
                InheritanceSlotsPerColumn = clan == null ? 0 : ClanManager.GetInheritanceSlots(clan.Level),
                Effects = clan?.InheritanceEffects.ToArray() ?? []
            };
        }
    }

    public GetClanShopResult GetShop()
    {
        try
        {
            var clanReference = reference ?? throw new InvalidException("Chưa gia nhập tông môn");
            return ClanManager.GetShop(clanReference, CharId);
        }
        catch (InvalidException e)
        {
            return new GetClanShopResult { Success = false, Message = e.Message };
        }
    }

    public async Task<BuyClanShopResult> BuyShopItem(BuyClanShopRequest request)
    {
        try
        {
            var clanReference = reference ?? throw new InvalidException("Chưa gia nhập tông môn");
            return await ClanManager.BuyShopItem(clanReference, charData, request);
        }
        catch (InvalidException e)
        {
            return new BuyClanShopResult
            {
                Success = false, Message = e.Message,
                ShopItemId = request.ShopItemId, Quantity = request.Quantity
            };
        }
    }

    public async Task<AddClanShopItemResult> AddShopItem(AddClanShopItemRequest request)
    {
        try
        {
            var clanReference = reference ?? throw new InvalidException("Chưa gia nhập tông môn");
            var actorMember = ActorMember();
            if (actorMember == null || !actorMember.HasPermission(ClanPermission.AddShopItem))
            {
                throw new InvalidException("Không có quyền thêm vật phẩm vào kho tông môn");
            }

            return await ClanManager.AddShopItem(clanReference, charData, request);
        }
        catch (InvalidException e)
        {
            return new AddClanShopItemResult { Success = false, Message = e.Message };
        }
    }

    public async Task<RemoveClanShopItemResult> RemoveShopItem(RemoveClanShopItemRequest request)
    {
        try
        {
            var clanReference = reference ?? throw new InvalidException("Chưa gia nhập tông môn");
            var actorMember = ActorMember();
            if (actorMember == null || !actorMember.HasPermission(ClanPermission.RemoveShopItem))
            {
                throw new InvalidException("Không có quyền xóa vật phẩm khỏi kho tông môn");
            }

            return await ClanManager.RemoveShopItem(clanReference, charData, request);
        }
        catch (InvalidException e)
        {
            return new RemoveClanShopItemResult
            {
                Success = false,
                ShopItemId = request.ShopItemId,
                Message = e.Message
            };
        }
    }

    public ClanMember? ActorMember()
    {
        lock (lockObject)
        {
            return reference?.Clan.Members.FirstOrDefault(x => x.CharId == CharId) ?? null;
        }
    }
    public async Task<AddClanMemberResult> AddMember(AddClanMemberRequest request)
    {
        AddClanMemberResult result;
        try
        {
            if (reference == null)
            {
                throw new InvalidException("Chưa gia nhập tông môn");
            }
            var actorMember = ActorMember();
            if (actorMember == null || !actorMember.HasPermission(ClanPermission.AddMember))
            {
                throw new InvalidException("Không có quyền thêm thành viên");
            }
            if (!Enum.IsDefined(request.Role) || request.Role >= actorMember.Role)
            {
                throw new InvalidException("Chỉ được cấp chức vụ thấp hơn chức vụ của bạn");
            }

            var member = await ClanManager.AddMember(reference, request.CharId, request.Role);
            result = new AddClanMemberResult
            {
                Success = true,
                Member = member
            };
        }
        catch (InvalidException e)
        {
            result = new AddClanMemberResult
            {
                Success = false,
                Message = e.Message
            };
        }

        return result;
    }

    public async Task<SetClanMemberRoleResult> SetMemberRole(SetClanMemberRoleRequest request)
    {
        try
        {
            var clanReference = reference ?? throw new InvalidException("Chưa gia nhập tông môn");
            var change = await ClanManager.SetMemberRole(clanReference, CharId, request.CharId, request.Role);
            return new SetClanMemberRoleResult { Success = true, Change = change };
        }
        catch (InvalidException e)
        {
            return new SetClanMemberRoleResult { Success = false, Message = e.Message };
        }
    }

    public async Task<RequestJoinClanResult> RequestJoinClan(RequestJoinClanRequest request)
    {
        try
        {
            if (reference != null)
            {
                throw new InvalidException("Đã gia nhập tông môn");
            }

            await ClanManager.RequestJoinClan(request.ClanId, charData);
            return new RequestJoinClanResult { Success = true };
        }
        catch (InvalidException e)
        {
            return new RequestJoinClanResult { Success = false, Message = e.Message };
        }
    }

    public async Task<ApproveClanJoinRequestResult> ApproveJoinRequest(ApproveClanJoinRequestRequest request)
    {
        try
        {
            if (reference == null)
            {
                throw new InvalidException("Chưa gia nhập tông môn");
            }

            var actorMember = ActorMember();
            if (actorMember == null || !actorMember.HasPermission(ClanPermission.AddMember))
            {
                throw new InvalidException("Không có quyền phê duyệt đơn xin gia nhập");
            }

            var member = await ClanManager.ApproveJoinRequest(reference, request.CharId);
            return new ApproveClanJoinRequestResult { Success = true, Member = member };
        }
        catch (InvalidException e)
        {
            return new ApproveClanJoinRequestResult { Success = false, Message = e.Message };
        }
    }

    public async Task<RejectClanJoinRequestResult> RejectJoinRequest(RejectClanJoinRequestRequest request)
    {
        try
        {
            if (reference == null)
            {
                throw new InvalidException("Chưa gia nhập tông môn");
            }

            var actorMember = ActorMember();
            if (actorMember == null || !actorMember.HasPermission(ClanPermission.AddMember))
            {
                throw new InvalidException("Không có quyền từ chối đơn xin gia nhập");
            }

            var charId = await ClanManager.RejectJoinRequest(reference, request.CharId);
            return new RejectClanJoinRequestResult { Success = true, CharId = charId };
        }
        catch (InvalidException e)
        {
            return new RejectClanJoinRequestResult
            {
                Success = false,
                CharId = request.CharId,
                Message = e.Message
            };
        }
    }

    public async Task<ClanContributionResult> Contribute(ClanContributionRequest request)
    {
        ClanContributionResult result;
        try
        {
            if (reference == null)
            {
                throw new InvalidException("Chưa gia nhập tông môn");
            }

            var contribution = await ClanManager.Contribute(reference, charData, request.Times);
            result = new ClanContributionResult
            {
                Success = true,
                Contribution = contribution,
                NewMoneyVip = charData.MoneyVip
            };
        }
        catch (InvalidException e)
        {
            result = new ClanContributionResult
            {
                Success = false,
                NewMoneyVip = charData.MoneyVip,
                Message = e.Message
            };
        }

        return result;
    }

    public async Task<SearchClanResult> SearchClan(SearchClanRequest request)
    {
        SearchClanResult result;
        try
        {
            var clans = await ClanManager.SearchClans(request.Name, request.Limit);
            result = new SearchClanResult
            {
                Success = true,
                Clans = clans
            };
        }
        catch (InvalidException e)
        {
            result = new SearchClanResult
            {
                Success = false,
                Message = e.Message
            };
        }

        return result;
    }

    public async Task<KickClanMemberResult> KickMember(KickClanMemberRequest request)
    {
        KickClanMemberResult result;
        try
        {
            if (reference == null)
            {
                throw new InvalidException("Chưa gia nhập tông môn");
            }

            if (request.CharId == CharId)
            {
                throw new InvalidException("Hãy dùng rời tông môn");
            }

            var actorMember = ActorMember();
            if (actorMember == null || !actorMember.HasPermission(ClanPermission.KickMember))
            {
                throw new InvalidException("Không có quyền trục xuất thành viên");
            }

            var member = await ClanManager.KickMember(reference, request.CharId);
            result = new KickClanMemberResult
            {
                Success = true,
                CharId = member.CharId
            };
        }
        catch (InvalidException e)
        {
            result = new KickClanMemberResult
            {
                Success = false,
                CharId = request.CharId,
                Message = e.Message
            };
        }

        return result;
    }

    public async Task<LeaveClanResult> LeaveClan(LeaveClanRequest request)
    {
        LeaveClanResult result;
        var clanId = reference?.Clan.Id ?? 0;
        try
        {
            if (reference == null)
            {
                throw new InvalidException("Chưa gia nhập tông môn");
            }

            var member = await ClanManager.LeaveClan(reference, CharId);
            result = new LeaveClanResult
            {
                Success = true,
                ClanId = member.ClanId
            };
        }
        catch (InvalidException e)
        {
            result = new LeaveClanResult
            {
                Success = false,
                ClanId = clanId,
                Message = e.Message
            };
        }

        return result;
    }

    public async Task<UploadImageResult> UploadImage(UploadImageRequest request)
    {
        UploadImageResult result = null!;
        try
        {
            var imageFile = new ImageFile(request.Data);
            imageFile.Resize(128, 128);
            var uploadResult = await imageFile.UploadAsync($"clan/{Guid.NewGuid():N}");
            if (uploadResult.PublicUrl == null)
            {
                throw new InvalidException("Lỗi tải ảnh");
            }
            result = new UploadImageResult()
            {
                Success = true,
                Url = uploadResult.PublicUrl
            };
        }
        catch (InvalidException e)
        {
            result = new UploadImageResult()
            {
                Success = false,
                Message = e.Message
            };
        }
        return result;
    }

    public ClanConfig GetConfig()
    {
        return ClanManager.Config;
    }
    public void OnClanAddMember(ClanMember member)
    {
        if (!isReady) return;
        MemSend(MessageTag.ClanAddMember, new ClanAddMember
        {
            Member = member
        });
    }

    public void OnClanRemoveMember(int clanId, int charId, ClanRemoveMemberReason reason)
    {
        if (!isReady) return;
        MemSend(MessageTag.ClanRemoveMember, new ClanRemoveMember
        {
            ClanId = clanId,
            CharId = charId,
            Reason = reason
        });

        if (charId != CharId)
        {
            return;
        }

        lock (lockObject)
        {
            UnwatchClanUnsafe();
        }
    }

    public void OnClanContribution(ClanContribution contribution)
    {
        if (!isReady) return;
        MemSend(MessageTag.ClanContribution, contribution);
    }

    public void OnClanDeleted(int clanId)
    {
        lock (lockObject)
        {
            observer = null;
            reference = null;
        }
    }

    private void UnwatchClan()
    {
        lock (lockObject)
        {
            UnwatchClanUnsafe();
        }
    }

    private void UnwatchClanUnsafe()
    {
        observer?.RemoveState(this);
        reference?.RemoveState(this);
        observer = null;
        reference = null;
    }

    private void DisposeClientEvents()
    {
        if (isDisposed)
        {
            return;
        }

        isDisposed = true;
        client.OnLogout -= OnLogout;
        client.OnDisconnectedSessionExpired -= OnDisconnectedSessionExpired;
        client.OnOtherClientLogin -= OnOtherClientLogin;
        client.OnReconnectedSuccess -= OnReconnectedSuccess;
    }

    public void MemSend<T>(MessageTag tag, T data)
    {
        if (!isReady) return;
        client.MemSend(tag, data);
    }

    public int CharId => charData.Id;
    private static readonly MessageTag[] handlers =
    [
        MessageTag.CreateClanRequest,
        MessageTag.GetClanRequest,
        MessageTag.UploadClanImageRequest,
        MessageTag.AddClanMemberRequest,
        MessageTag.KickClanMemberRequest,
        MessageTag.LeaveClanRequest,
        MessageTag.ClanContributionRequest,
        MessageTag.SearchClanRequest,
        MessageTag.GetClanShopRequest,
        MessageTag.BuyClanShopRequest,
        MessageTag.AddClanShopItemRequest,
        MessageTag.RemoveClanShopItemRequest,
        MessageTag.SetClanMemberRoleRequest,
        MessageTag.RequestJoinClanRequest,
        MessageTag.ApproveClanJoinRequestRequest,
        MessageTag.RejectClanJoinRequestRequest,
        MessageTag.SetClanInheritanceRequest,
        MessageTag.UpgradeClanWithFundsRequest,
        MessageTag.GetClanMapNodesRequest,
        MessageTag.ViewClanRequest
    ];
    public MessageTag[] HandleTag => handlers;
    public void OnLeave()
    {
        UnwatchClan();
        DisposeClientEvents();
    }
    public void OnDisconnect(IProtocol<MessageTag> protocol)
    {
        UnwatchClan();
        DisposeClientEvents();
    }
}
