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
    private ClanObserver? observer;
    private ClanReference? reference;
    private readonly object lockObject = new();

    public ClanHandler(Client client)
    {
        this.client = client;
        charData = client.CharacterData;
    }

    public async void OnEnter()
    {
        try
        {
            reference = await ClanManager.FindReferenceByMember(CharId);
            var clanId = reference.Clan.Id;
            (observer, reference) = await ClanManager.WatchClan(clanId, this);
        }
        catch (InvalidException e)
        {
            
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
        return reference?.Clan ?? null;
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
    }

    public ClanMember? ActorMember()
    {
        return reference?.Clan.Members.FirstOrDefault(x => x.CharId == CharId) ?? null;
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
            observer?.RemoveState(this);
            reference?.RemoveState(this);
            observer = null;
            reference = null;
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

    public void MemSend<T>(MessageTag tag, T data)
    {
        if (!isReady) return;
        client.MemSend(tag, data);
    }

    public int CharId => charData.Id;
    private static readonly MessageTag[] handlers =
    [
        MessageTag.CreateClanRequest,
        MessageTag.UploadClanImageRequest,
        MessageTag.AddClanMemberRequest,
        MessageTag.KickClanMemberRequest,
        MessageTag.LeaveClanRequest,
        MessageTag.ClanContributionRequest
    ];
    public MessageTag[] HandleTag => handlers;
    public void OnLeave()
    {
    }
    public void OnDisconnect(IProtocol<MessageTag> protocol)
    {
    }
}
