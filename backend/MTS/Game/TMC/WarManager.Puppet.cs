using MTS.Exceptions;
using MTS.Game.Clan;
using MTS.Game.LuyenDan;
using MTS.Models;
using MTS.Network;
using MTS.Network.Models;
using Serilog;

namespace MTS.Game.TMC;

public static partial class WarManager
{
    /// <summary>Tuần tự hoá tạo/nâng rối (có bước trả phí bất đồng bộ giữa kiểm tra và áp dụng).</summary>
    private static readonly SemaphoreSlim PuppetGate = new(1, 1);

    private static void EnsureLeader(Models.Clan clan, int charId)
    {
        if (clan.LeaderCharId != charId) throw new InvalidException("Chỉ Tộc Chủ mới được điều khiển rối.");
    }

    private static WarUnit GetClanPuppet(int clanId, int puppetId)
    {
        if (!Units.TryGetValue((WarUnitKind.Puppet, puppetId), out var unit) || unit.ClanId != clanId)
            throw new InvalidException("Rối không tồn tại.");
        return unit;
    }

    /// <summary>HP chiến tối đa của người tạo/nâng rối, làm gốc cho HP rối.</summary>
    private static int GetCreatorWarHpMax(CharacterData actor)
    {
        if (Units.TryGetValue((WarUnitKind.Player, actor.Id), out var unit) && unit.HpMax > 0) return unit.HpMax;
        return WarConfig.GetPlayerMaxHp(new CharacterDataView(actor.Id, actor.Progression.Level, actor.Stats.SinhMenh));
    }

    private static WarPuppetResult PaymentResult(ClanPaymentResult payment, bool isUpgrade, int puppetId, int targetLevel,
        string? message = null) => new()
    {
        Success = !payment.RequiresConfirmation,
        Message = message ?? string.Empty,
        IsUpgrade = isUpgrade,
        PuppetId = puppetId,
        TargetLevel = targetLevel,
        RequiresConfirmation = payment.RequiresConfirmation,
        ContribPointsCost = payment.ContribPointsCost,
        MoneyVipCost = payment.MoneyVipCost,
        ContribPoints = payment.ContribPoints,
        NewMoneyVip = payment.NewMoneyVip
    };

    /// <summary>
    /// Tộc Chủ tạo rối mới tại thành chính. Số rối tối đa = level tông. Giá 5.000 tiên ngọc (hoặc 10.000 điểm cống hiến):
    /// trừ điểm cống hiến trước, thiếu bù tiên ngọc. Lần gọi với chi phí chưa xác nhận chỉ trả giá để client hỏi lại.
    /// HP rối = (level × 10)% HP chiến tối đa của người tạo; độ bền đầy.
    /// </summary>
    public static async Task<WarPuppetResult> CreatePuppet(CharacterData actor, Models.Clan clan,
        WarCreatePuppetRequest request)
    {
        EnsureLeader(clan, actor.Id);
        if (clan.NodeId <= 0) throw new InvalidException("Tông môn chưa có thành trên bản đồ.");
        await PuppetGate.WaitAsync();
        try
        {
            var level = WarConfig.GetNewPuppetLevel(clan.Level);
            lock (Sync)
            {
                EnsureInitialized();
                var count = Units.Values.Count(x => x.IsPuppet && x.ClanId == clan.Id);
                var max = WarConfig.GetMaxPuppetCount(clan.Level);
                if (count >= max) throw new InvalidException($"Tông môn cấp {clan.Level} chỉ được có tối đa {max} rối.");
            }

            var payment = await ClanManager.PayWithContribOrMoneyVip(clan, actor,
                WarConfig.CreatePuppetMoneyVip * WarConfig.ContribPointsPerMoneyVip,
                request.ExpectedContribPointsCost, request.ExpectedMoneyVipCost, SpendFor.CreateWarPuppet);
            if (payment.RequiresConfirmation) return PaymentResult(payment, false, 0, level);

            var now = DateTime.UtcNow;
            Puppet puppet;
            lock (Sync)
            {
                var hpMax = WarConfig.GetPuppetMaxHp(level, GetCreatorWarHpMax(actor));
                puppet = new Puppet
                {
                    DonToc = WarConfig.GetPuppetDonToc(level, actor.Stats.TocDoBay),
                    ClanId = clan.Id, Level = level, RandomSeed = WarConfig.RollPuppetSeed(), NodeId = clan.NodeId,
                    Hp = hpMax, HpMax = hpMax, Durability = WarConfig.PuppetMaxDurability, LastHpRecoveryAt = now,
                    State = WarUnitState.Idle, ArrivedAt = now, CreatedAt = now
                };
                PuppetVisual.Randomize(puppet);
            }

            // Ghi ngay để có Id bền vững; restart không thể sinh thêm rối vì số rối chỉ tăng qua lệnh này.
            try
            {
                await using var db = await Program.CreateDbAsync();
                db.Puppets.Add(puppet);
                await db.SaveChangesAsync();
            }
            catch (Exception e)
            {
                Log.Error(e, "TongMonChien create puppet failed after payment; refund CharId={CharId}", actor.Id);
                await ClanManager.Refund(clan, actor, payment);
                throw new InvalidException("Không thể tạo rối, chi phí đã được hoàn lại.");
            }

            lock (Sync)
            {
                var unit = new WarUnit(puppet);
                Units[unit.Key] = unit;
                pendingUnits[unit.Key] = unit;
                if (!Clans.ContainsKey(clan.Id)) RemoveUnit(unit);
                Log.Information("TongMonChien clan {ClanId} created puppet {PuppetId} (cost {Points} points + {Vip} MoneyVip)",
                    clan.Id, puppet.Id, payment.ContribPointsCost, payment.MoneyVipCost);
                ResolveNode(puppet.NodeId, now);
                PublishDelta();
                var result = PaymentResult(payment, false, puppet.Id, level);
                result.Puppet = ToView(unit, now);
                return result;
            }
        }
        finally
        {
            PuppetGate.Release();
        }
    }

    /// <summary>
    /// Tộc Chủ nâng rối lên level kế (trần theo cấp tông). Giá 500 × level cần lên (tiên ngọc, hoặc ×2 điểm cống hiến).
    /// Sau khi nâng: HP tối đa tính lại = (level × 10)% HP chiến tối đa của người nâng, HP hồi đầy, độ bền hồi đầy.
    /// </summary>
    public static async Task<WarPuppetResult> UpgradePuppet(CharacterData actor, Models.Clan clan,
        WarUpgradePuppetRequest request)
    {
        EnsureLeader(clan, actor.Id);
        await PuppetGate.WaitAsync();
        try
        {
            int targetLevel;
            lock (Sync)
            {
                EnsureInitialized();
                var unit = GetClanPuppet(clan.Id, request.PuppetId);
                if (unit.State == WarUnitState.Fighting) throw new InvalidException("Rối đang giao tranh.");
                var maxLevel = WarConfig.GetMaxPuppetLevel(clan.Level);
                if (unit.Puppet!.Level >= maxLevel) throw new InvalidException("Rối đã đạt cảnh giới tối đa của tông.");
                targetLevel = unit.Puppet.Level + 1;
            }

            var payment = await ClanManager.PayWithContribOrMoneyVip(clan, actor,
                WarConfig.GetUpgradePuppetMoneyVip(targetLevel) * WarConfig.ContribPointsPerMoneyVip,
                request.ExpectedContribPointsCost, request.ExpectedMoneyVipCost, SpendFor.UpgradeWarPuppet);
            if (payment.RequiresConfirmation) return PaymentResult(payment, true, request.PuppetId, targetLevel);

            string? failure = null;
            lock (Sync)
            {
                var now = DateTime.UtcNow;
                if (!Units.TryGetValue((WarUnitKind.Puppet, request.PuppetId), out var unit) || unit.ClanId != clan.Id)
                    failure = "Rối không còn tồn tại.";
                else if (unit.State == WarUnitState.Fighting)
                    failure = "Rối vừa vào giao tranh.";
                else if (unit.Puppet!.Level + 1 != targetLevel)
                    failure = "Level rối đã thay đổi.";
                else
                {
                    var puppet = unit.Puppet;
                    puppet.Level = targetLevel;
                    puppet.HpMax = WarConfig.GetPuppetMaxHp(targetLevel, GetCreatorWarHpMax(actor));
                    puppet.DonToc = WarConfig.GetPuppetDonToc(targetLevel, actor.Stats.TocDoBay);
                    puppet.Hp = puppet.HpMax;
                    puppet.LastHpRecoveryAt = now;
                    puppet.Durability = WarConfig.PuppetMaxDurability;
                    MarkUnit(unit);
                    ResolveNode(unit.NodeId, now);
                    PublishDelta();
                    Log.Information("TongMonChien puppet {PuppetId} upgraded to {Level} (cost {Points} points + {Vip} MoneyVip)",
                        puppet.Id, targetLevel, payment.ContribPointsCost, payment.MoneyVipCost);
                    var result = PaymentResult(payment, true, puppet.Id, targetLevel);
                    result.Puppet = ToView(unit, now);
                    return result;
                }
            }

            await ClanManager.Refund(clan, actor, payment);
            throw new InvalidException($"{failure} Chi phí đã được hoàn lại.");
        }
        finally
        {
            PuppetGate.Release();
        }
    }

    /// <summary>Leader điều rối tới node của tông. Rối phải đạt 99% HP; chỉ đi qua node của tông.</summary>
    public static int[] DeployPuppet(int charId, Models.Clan clan, int puppetId, int targetNodeId)
    {
        EnsureLeader(clan, charId);
        lock (Sync)
        {
            EnsureInitialized();
            var unit = GetClanPuppet(clan.Id, puppetId);
            var now = DateTime.UtcNow;
            if (unit.ApplyRegen(now)) MarkUnit(unit);
            if (unit.State != WarUnitState.Idle)
                throw new InvalidException(unit.State == WarUnitState.Moving
                    ? "Rối đang di chuyển."
                    : "Rối đang giao tranh/phòng thủ tại node.");
            if (!WarConfig.CanMove(unit.Hp, unit.HpMax)) throw new InvalidException(WarConfig.MoveHpMessage("Rối"));
            if (targetNodeId == unit.NodeId) throw new InvalidException("Rối đang ở node này.");
            if (!IsAllyNode(clan.Id, targetNodeId)) throw new InvalidException("Chỉ được điều rối tới node của tông.");
            var route = FindPuppetRoute(clan.Id, unit.NodeId, targetNodeId)
                        ?? throw new InvalidException("Không có đường đi chỉ qua node của tông.");
            StartMoving(unit, route, now);
            PublishDelta();
            return route.ToArray();
        }
    }

    public static bool IsWarHpPill(ushort danId)
    {
        var linhDan = GameConfig.LuyenDanResources.LinhDans.FirstOrDefault(x => x.Id == danId);
        return linhDan?.EBuffType is EDanBuffType.Hp or EDanBuffType.HoiFullHp;
    }

    /// <summary>
    /// Dùng đan Hp/HoiFullHp ngoài trận cho bản thân hoặc cho rối (chỉ Leader, lấy từ túi cá nhân).
    /// Chỉ tác động HP chiến, không đổi Stats.Hp. Trừ đan và cộng HP trong cùng một khối khoá.
    /// </summary>
    public static WarUseHpPillResult UseHpPill(Client client, Models.Clan? clan, WarUseHpPillRequest request)
    {
        var charData = client.CharacterData ?? throw new InvalidException("Nhân vật không tồn tại.");
        if (request.Count == 0) throw new InvalidException("Số lượng không hợp lệ.");
        var linhDan = GameConfig.LuyenDanResources.LinhDans.FirstOrDefault(x => x.Id == request.DanId)
                      ?? throw new InvalidException("Đan dược không tồn tại.");
        if (linhDan.EBuffType is not (EDanBuffType.Hp or EDanBuffType.HoiFullHp))
            throw new InvalidException("Chỉ đan hồi khí huyết mới hồi được HP chiến.");
        lock (Sync)
        {
            EnsureInitialized();
            WarUnit unit;
            if (request.PuppetId > 0)
            {
                if (clan == null) throw new InvalidException("Chưa gia nhập tông môn.");
                EnsureLeader(clan, charData.Id);
                unit = GetClanPuppet(clan.Id, request.PuppetId);
            }
            else if (!Units.TryGetValue((WarUnitKind.Player, charData.Id), out unit))
            {
                throw new InvalidException("Bạn chưa vào Tông môn chiến.");
            }

            if (unit.State == WarUnitState.Fighting) throw new InvalidException("Không thể dùng đan khi đang giao tranh.");
            var now = DateTime.UtcNow;
            unit.ApplyRegen(now);
            if (unit.Hp >= unit.HpMax) throw new InvalidException("HP chiến đã đầy.");
            int remaining;
            lock (client.CharacterLock)
            {
                var itemRef = new ItemRef { Id = request.DanId, Type = ItemType.LinhDan };
                if (!charData.InventoryManager.LinhDans.TryGetValue(itemRef, out var item) || item.Count < request.Count)
                    throw new InvalidException("Không đủ đan dược trong túi.");
                remaining = item.Count - request.Count;
                charData.InventoryManager.Decrease(new ItemRef
                {
                    Id = request.DanId, Type = ItemType.LinhDan, Count = request.Count
                });
            }

            // Đan Hp hồi theo % HP tối đa (giống ngoài chiến: BuffValue% Sinh Mệnh mỗi viên), không phải số HP cố định.
            unit.Hp = linhDan.EBuffType == EDanBuffType.HoiFullHp
                ? unit.HpMax
                : (int)Math.Min(unit.HpMax,
                    unit.Hp + (long)Math.Floor((double)unit.HpMax * linhDan.BuffValue / 100d * request.Count));
            if (unit.Hp >= unit.HpMax) unit.LastHpRecoveryAt = now;
            MarkUnit(unit);
            Log.Information("TongMonChien char {CharId} used pill {DanId} x{Count} on {Kind} {Id}: hp {Hp}/{Max}",
                charData.Id, request.DanId, request.Count, unit.Kind, unit.Id, unit.Hp, unit.HpMax);
            ResolveNode(unit.NodeId, now);
            PublishDelta();
            return new WarUseHpPillResult
            {
                Success = true, PuppetId = request.PuppetId, Hp = unit.Hp, HpMax = unit.HpMax, DanId = request.DanId,
                RemainingCount = remaining
            };
        }
    }
}
