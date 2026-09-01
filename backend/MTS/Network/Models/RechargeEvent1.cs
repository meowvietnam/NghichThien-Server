using MemoryPack;
using MTS.Data;
using MTS.Exceptions;
using MTS.Game.BaseSkill;
using MTS.IAP;
using MTS.Models;
using MTS.Services;
using Newtonsoft.Json;
using Serilog;

namespace MTS.Network.Models;

public class RechargeEvent1Data
{
    public ushort Id { get; set; }
}
[MemoryPackable]
public partial class RechargeEvent1 : IGameEvent
{
    [MemoryPackIgnore]
    private CharacterData charData;
    public List<RechargeMilestone1> Milestones { get; set; }
    public void Init(CharacterData charData)
    {
        this.charData = charData;
    }

    private RechargeMilestone1 Validate(ushort id)
    {
        var milestone = Milestones.FirstOrDefault(x => x.Id == id);
        if (milestone == null)
        {
            throw new InvalidException("Gói này không tồn tại");
        }
        if (milestone.IsClaim)
        {
            throw new InvalidException("Bạn đã nạp gói này rồi");
        }

        if (milestone.Type == ETypeSkill.TranPhap)
        {
            throw new InvalidException("Không hỗ trợ hệ này");
        }
        return milestone;
    }
    public async Task<IEventResultData> HandleEvent(IEventRequestData eventRequestData, AppDbContext db)
    {
        if (eventRequestData is CreateOrderRechargeEvent1 createOrder)
        {
            try
            {
                var id = createOrder.Id;
                var milestone = Validate(id);
                var data = new RechargeEvent1Data()
                {
                    Id = id
                };
                var link = await PaymentService.Instance.CreateLink(milestone.Amount, TimeSpan.FromHours(1));
                var expireTime = DateTimeOffset
                    .FromUnixTimeSeconds(link.ExpiredAt.Value)
                    .UtcDateTime;
                var order = new Order()
                {
                    ProductId = null,
                    CharId = charData.Id,
                    OrderType = OrderType.RechargeEvent1,
                    Amount = milestone.Amount,
                    Link = link.CheckoutUrl,
                    OrderId = link.OrderCode,
                    ExpireAt = expireTime,
                    Data = JsonConvert.SerializeObject(data)
                };
                await db.Orders.AddAsync(order);
                await db.SaveChangesAsync();
                return new RechargeEvent1Result()
                {
                    Success = true,
                    Link = link.CheckoutUrl
                };
            }
            catch (InvalidException e)
            {
                return new RechargeEvent1Result()
                {
                    Success = false,
                    Message = e.Message
                };
            }
        }
        if (eventRequestData is HandleOrderRechargeEvent1 orderData)
        {
            try
            {
                var order = orderData.Order;
                Log.Information("{Name} vừa nạp sự kiện {Amount} VNĐ", charData.Name, Utils.Utils.FormatCount(order.Amount));
                var data = JsonConvert.DeserializeObject<RechargeEvent1Data>(order.Data);
                var id = data.Id;
                var milestone = Validate(id);
                order.Status = OrderStatus.Complete;
                var purchase = new IapPurchase()
                {
                    Store = Store.Web,
                    ProductId = null,
                    CharId = order.CharId,
                    AmountSnapshot = order.Amount,
                    PurchaseTokenHash = Array.Empty<byte>()
                };
                await db.IapPurchases.AddAsync(purchase);
                milestone.IsClaim = true;
                var rewards = milestone.GetRewards();
                var mail = new Mail()
                {
                    CharId = order.CharId,
                    Title = "Phần thưởng nạp tiền",
                    Content = $"Chúc mừng! Bạn nhận được phần thưởng {milestone.Name}.",
                    Rewards = rewards
                };
                var mailManager = new MailManager(charData, charData.Client);
                await mailManager.SendMail(mail, db, false);
                return new HandleOrderRechargeEvent1Result()
                {
                    Success = true
                };
            }
            catch (InvalidException e)
            {
                return new HandleOrderRechargeEvent1Result()
                {
                    Success = false,
                    Message = e.Message
                };
            }
        }
        return null;
    }
}