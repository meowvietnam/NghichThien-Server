using MTS.Exceptions;
using MTS.Game.LuyenDan;
using MTS.Models;
using MTS.Network;
using MTS.Network.Models;
using Serilog;

namespace MTS.Game.TMC;

public static partial class WarManager
{
    private static readonly SemaphoreSlim PuppetCreateGate = new(1, 1);

    private static void EnsureLeader(Models.Clan clan, int charId)
    {
        if (clan.LeaderCharId != charId) throw new InvalidException("Chỉ Tộc Chủ mới được điều khiển khôi lỗi.");
    }

    private static WarUnit GetClanPuppet(int clanId, int puppetId)
    {
        if (!Units.TryGetValue((WarUnitKind.Puppet, puppetId), out var unit) || unit.ClanId != clanId)
            throw new InvalidException("Khôi lỗi không tồn tại.");
        return unit;
    }

    /// <summary>Leader tạo rối mới tại thành chính. Số rối tối đa theo cấp tông (WarConfig).</summary>
    public static async Task<WarPuppetResult> CreatePuppet(int charId, Models.Clan clan)
    {
        EnsureLeader(clan, charId);
        if (clan.NodeId <= 0) throw new InvalidException("Tông môn chưa có thành trên bản đồ.");
        await PuppetCreateGate.WaitAsync();
        try
        {
            var now = DateTime.UtcNow;
            Puppet puppet;
            lock (Sync)
            {
                EnsureInitialized();
                var count = Units.Values.Count(x => x.IsPuppet && x.ClanId == clan.Id);
                var max = WarConfig.GetMaxPuppetCount(clan.Level);
                if (count >= max) throw new InvalidException($"Tông môn cấp {clan.Level} chỉ được có tối đa {max} khôi lỗi.");
                if (WarConfig.GetCreatePuppetCost(clan.Level, count) > 0)
                    throw new InvalidException("Chi phí tạo khôi lỗi chưa được hỗ trợ.");
                var level = WarConfig.GetNewPuppetLevel(clan.Level);
                var seed = WarConfig.RollPuppetSeed();
                var hpMax = WarConfig.GetPuppetMaxHp(level, clan.Level, seed);
                puppet = new Puppet
                {
                    ClanId = clan.Id, Level = level, RandomSeed = seed, NodeId = clan.NodeId, Hp = hpMax,
                    HpMax = hpMax, LastHpRecoveryAt = now, State = WarUnitState.Idle, ArrivedAt = now,
                    CreatedAt = now
                };
            }

            // Ghi ngay để có Id bền vững; restart không thể sinh thêm rối vì số rối chỉ tăng qua lệnh này.
            await using (var db = await Program.CreateDbAsync())
            {
                db.Puppets.Add(puppet);
                await db.SaveChangesAsync();
            }

            lock (Sync)
            {
                var unit = new WarUnit(puppet);
                Units[unit.Key] = unit;
                pendingUnits[unit.Key] = unit;
                if (!Clans.ContainsKey(clan.Id)) RemoveUnit(unit);
                Log.Information("TongMonChien clan {ClanId} created puppet {PuppetId}", clan.Id, puppet.Id);
                ResolveNode(puppet.NodeId, now);
                PublishDelta();
                return new WarPuppetResult { Success = true, Puppet = ToView(unit, now) };
            }
        }
        finally
        {
            PuppetCreateGate.Release();
        }
    }

    /// <summary>Leader nâng level rối (trần theo cấp tông). HP tối đa tính lại theo WarConfig.</summary>
    public static WarPuppetResult UpgradePuppet(int charId, Models.Clan clan, int puppetId)
    {
        EnsureLeader(clan, charId);
        lock (Sync)
        {
            EnsureInitialized();
            var unit = GetClanPuppet(clan.Id, puppetId);
            if (unit.State == WarUnitState.Fighting) throw new InvalidException("Khôi lỗi đang giao tranh.");
            var puppet = unit.Puppet!;
            var maxLevel = WarConfig.GetMaxPuppetLevel(clan.Level);
            if (puppet.Level >= maxLevel) throw new InvalidException($"Khôi lỗi đã đạt level tối đa ({maxLevel}) của tông.");
            if (WarConfig.GetUpgradePuppetCost(puppet.Level) > 0)
                throw new InvalidException("Chi phí nâng cấp khôi lỗi chưa được hỗ trợ.");
            var now = DateTime.UtcNow;
            unit.ApplyRegen(now);
            puppet.Level++;
            var newMax = WarConfig.GetPuppetMaxHp(puppet.Level, clan.Level, puppet.RandomSeed);
            puppet.Hp = Math.Clamp(puppet.Hp + (newMax - puppet.HpMax), 0, newMax);
            puppet.HpMax = newMax;
            MarkUnit(unit);
            PublishDelta();
            return new WarPuppetResult { Success = true, Puppet = ToView(unit, now) };
        }
    }

    /// <summary>Leader điều rối tới node của tông. Rối phải đầy HP; chỉ đi qua node của tông.</summary>
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
                    ? "Khôi lỗi đang di chuyển."
                    : "Khôi lỗi đang giao tranh/phòng thủ tại node.");
            if (unit.Hp < unit.HpMax) throw new InvalidException("Khôi lỗi phải đầy HP mới được điều đi.");
            if (targetNodeId == unit.NodeId) throw new InvalidException("Khôi lỗi đang ở node này.");
            if (!IsAllyNode(clan.Id, targetNodeId)) throw new InvalidException("Chỉ được điều khôi lỗi tới node của tông.");
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

            unit.Hp = linhDan.EBuffType == EDanBuffType.HoiFullHp
                ? unit.HpMax
                : (int)Math.Min(unit.HpMax, (long)unit.Hp + (long)linhDan.BuffValue * request.Count);
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
