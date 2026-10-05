using MemoryPack;
using Microsoft.EntityFrameworkCore;
using MTS.Data;
using MTS.Game;
using MTS.Game.AllMaps;
using MTS.Game.BaseSkill;
using MTS.Game.Clan;
using MTS.Game.DP;
using MTS.Game.Gacha;
using MTS.Game.LuyenDan;
using MTS.Game.LuyenKhi;
using MTS.Game.Resources;
using MTS.Game.TMC;
using MTS.IAP;
using MTS.Models;
using MTS.ND;
using MTS.Network.Models;
using MTS.Utils;
using Newtonsoft.Json;
using Serilog;
using NgoDao = MTS.ND.NgoDao;

namespace MTS.Services;

public class Startup : BackgroundService
{
    private readonly ILogger<Startup> logger;

    public Startup(ILogger<Startup> logger)
    {
        this.logger = logger;
    }

    public static readonly TaskCompletionSource Ready = new(TaskCreationOptions.RunContinuationsAsynchronously);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await RunOnceScripts(stoppingToken);
        }
        finally
        {
            Ready.TrySetResult();
        }
    }
    public static int GetMissingDayCount(
        IEnumerable<DateTime> dates,
        DateTime from,
        DateTime to)
    {
        var existingDates = dates
            .Select(x => x.Date)
            .Where(x => x >= from.Date && x <= to.Date)
            .Distinct()
            .Count();

        var totalDays = (to.Date - from.Date).Days + 1;

        return totalDays - existingDates;
    }
    private async Task Register(AppDbContext db)
    {
        var name = "ThreeOneOne";
        var username = "nhatdz31202";
        var password = "nhat311202";
        var charData = await db.Characters.FirstAsync(x => x.Name == name);
        var userId = charData.UserId;
        var account = new LocalAccount()
        {
            UserId = userId,
            Username = username,
            PasswordHash = PasswordService.Hash(password)
        };
        await db.LocalAccounts.AddAsync(account);
        await db.SaveChangesAsync();
        Log.Information("oke");
    }
    private async Task RunOnceScripts(CancellationToken stoppingToken)
    {
        await using var db = await Program.CreateDbAsync();
    }
    private async Task RelocateClanMainCity(AppDbContext db, string clanName, int targetNodeId,
        CancellationToken stoppingToken)
    {
        try
        {
            var clan = await db.Clans.FirstOrDefaultAsync(x => x.Name == clanName, stoppingToken);
            if (clan == null)
            {
                Log.Warning("RelocateClanMainCity: không tìm thấy tông {ClanName}", clanName);
                return;
            }

            if (clan.NodeId == targetNodeId)
            {
                // Chạy lại (server khởi động lại mà quên comment dòng gọi): không reset lãnh thổ lần nữa.
                Log.Information("RelocateClanMainCity: tông {ClanName} đã ở node {Target}, bỏ qua", clanName, targetNodeId);
                return;
            }

            var maps = GameConfig.MapManager.Maps.ToDictionary(x => x.Id);
            bool IsNormalNode(int nodeId) =>
                maps.TryGetValue(nodeId, out var m) && m.MapType != Game.AllMaps.MapType.Stronghold;

            // NodeId thành chính → ClanId (các tông khác).
            var mainCityOwners = (await db.Clans.AsNoTracking()
                    .Where(x => x.Id != clan.Id && x.NodeId > 0)
                    .Select(x => new { x.Id, x.NodeId })
                    .ToListAsync(stoppingToken))
                .GroupBy(x => x.NodeId)
                .ToDictionary(g => g.Key, g => g.First().Id);
            if (!IsNormalNode(targetNodeId) || mainCityOwners.ContainsKey(targetNodeId))
            {
                Log.Error("RelocateClanMainCity: node {Target} không hợp lệ (không tồn tại, là cứ điểm hoặc thành chính tông khác)",
                    targetNodeId);
                return;
            }

            var now = DateTime.UtcNow;
            var rows = await db.ClanNodes.ToDictionaryAsync(x => x.NodeId, stoppingToken);
            void SetOwner(int nodeId, int? owner)
            {
                if (!rows.TryGetValue(nodeId, out var row))
                {
                    if (owner == null) return;
                    row = new ClanNode { NodeId = nodeId };
                    db.ClanNodes.Add(row);
                    rows[nodeId] = row;
                }
                else if (row.ClanId == owner) return;

                row.ClanId = owner;
                row.Version++;
                row.UpdatedAt = now;
            }

            int? OwnerOf(int nodeId) =>
                mainCityOwners.TryGetValue(nodeId, out var main) ? main
                : nodeId == targetNodeId ? clan.Id
                : rows.TryGetValue(nodeId, out var row) ? row.ClanId : null;

            var oldMainNodeId = clan.NodeId;
            var displacedClanId = rows.TryGetValue(targetNodeId, out var targetRow) &&
                                  targetRow.ClanId is { } c && c != clan.Id
                ? c
                : (int?)null;

            // 1) Toàn bộ lãnh thổ tông đổi vị trí về trung lập; node đích là thành chính nên không lưu ở ClanNodes.
            var resetNodes = 0;
            foreach (var row in rows.Values.Where(x => x.ClanId == clan.Id).ToList())
            {
                SetOwner(row.NodeId, null);
                resetNodes++;
            }

            SetOwner(targetNodeId, null);
            clan.NodeId = targetNodeId;

            // 2) Bù cho tông bị chiếm chỗ: node thường trung lập ngẫu nhiên liền kề lãnh thổ còn lại của họ.
            int? compensationNodeId = null;
            var displacedMainNodeId = 0;
            if (displacedClanId is { } displaced)
            {
                displacedMainNodeId = mainCityOwners.FirstOrDefault(x => x.Value == displaced).Key;
                var territory = rows.Values.Where(x => x.ClanId == displaced).Select(x => x.NodeId).ToHashSet();
                if (displacedMainNodeId > 0) territory.Add(displacedMainNodeId);
                // Đồ thị có thể chỉ lưu một chiều: xét cả cạnh đi ra và đi vào.
                var neighbors = new HashSet<int>();
                foreach (var map in maps.Values)
                {
                    foreach (var next in map.NextMapIds ?? [])
                    {
                        if (territory.Contains(map.Id)) neighbors.Add(next);
                        if (territory.Contains(next)) neighbors.Add(map.Id);
                    }
                }

                var candidates = neighbors.Where(x => IsNormalNode(x) && OwnerOf(x) == null).ToList();
                if (candidates.Count == 0)
                {
                    // Không còn node liền kề trống: lấy bất kỳ node thường trung lập nào.
                    candidates = maps.Keys.Where(x => IsNormalNode(x) && OwnerOf(x) == null).ToList();
                }

                if (candidates.Count > 0)
                {
                    compensationNodeId = candidates[Random.Shared.Next(candidates.Count)];
                    SetOwner(compensationNodeId.Value, displaced);
                }
                else
                {
                    Log.Warning("RelocateClanMainCity: không còn node trống để bù cho tông {ClanId}", displaced);
                }
            }

            int HomeOf(int clanId) =>
                clanId == displacedClanId && compensationNodeId is { } comp ? comp
                : mainCityOwners.FirstOrDefault(x => x.Value == clanId).Key;

            // 3) Dời đơn vị: tông đổi vị trí về thành chính mới; tông khác đang đứng ở node đích về node bù/thành chính.
            var participants = await db.WarParticipants
                .Where(x => x.ClanId == clan.Id || x.NodeId == targetNodeId)
                .ToListAsync(stoppingToken);
            foreach (var p in participants)
            {
                var home = p.ClanId == clan.Id ? targetNodeId : HomeOf(p.ClanId);
                if (home <= 0) continue;
                p.NodeId = home;
                p.LastSafeNodeId = home;
                p.State = Game.TMC.WarUnitState.Idle;
                p.Route = string.Empty;
                p.NextArrivalAt = null;
                p.ArrivedAt = now;
            }

            var puppets = await db.Puppets
                .Where(x => x.ClanId == clan.Id || x.NodeId == targetNodeId)
                .ToListAsync(stoppingToken);
            foreach (var p in puppets)
            {
                var home = p.ClanId == clan.Id ? targetNodeId : HomeOf(p.ClanId);
                if (home <= 0) continue;
                p.NodeId = home;
                p.State = Game.TMC.WarUnitState.Idle;
                p.Route = string.Empty;
                p.NextArrivalAt = null;
                p.ArrivedAt = now;
            }

            await db.SaveChangesAsync(stoppingToken);

            Log.Information(
                "RelocateClanMainCity: tông {ClanName} ({ClanId}) thành chính {Old} → {New}; {Nodes} node về trung lập; " +
                "{Players} người, {Puppets} rối được dời. Tông bị chiếm chỗ: {Displaced}, node bù: {Compensation}",
                clanName, clan.Id, oldMainNodeId, targetNodeId, resetNodes, participants.Count, puppets.Count,
                displacedClanId?.ToString() ?? "không", compensationNodeId?.ToString() ?? "không");
        }
        catch (Exception e)
        {
            Log.Error(e, "RelocateClanMainCity: không đặt lại được thành chính cho tông {ClanName}", clanName);
        }
    }

    private async Task CustomRecharge(
        AppDbContext db,
        string charName,
        string key,
        long amountVnd = 5_000_000,
        long moneyVipSnapshot = 500_000,
        int moneyVipReward = 1_500_000)
    {
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Cần key duy nhất cho lần nạp.", nameof(key));
        var charData = await db.Characters.AsNoTracking()
            .Where(x => x.Name == charName)
            .Select(x => new { x.Id, x.Name })
            .FirstOrDefaultAsync();
        if (charData == null)
        {
            Log.Warning("CustomRecharge: không tìm thấy nhân vật {Name}", charName);
            return;
        }

        var tokenHash = Utils.Utils.HashSHA256($"custom-recharge:{key}");
        var existed = await db.IapPurchases
            .AnyAsync(x => x.Store == Store.Web && x.PurchaseTokenHash == tokenHash);
        if (existed)
        {
            Log.Information("CustomRecharge: key {Key} đã nạp trước đó, bỏ qua", key);
            return;
        }

        db.IapPurchases.Add(new IapPurchase
        {
            CharId = charData.Id,
            ProductId = null,
            Store = Store.Web,
            PurchaseTokenHash = tokenHash,
            AmountSnapshot = amountVnd,
            MoneyVipSnapshot = moneyVipSnapshot,
            PurchaseDate = DateTime.UtcNow
        });
        db.Mails.Add(new Mail
        {
            CharId = charData.Id,
            Title = "Nạp tiền thành công!",
            Content = $"Bạn vừa nạp thành công {amountVnd:N0} VND và nhận {moneyVipReward:N0} Tiên Ngọc.\n" +
                      "Cảm ơn bạn đã ủng hộ game! Chúc bạn chơi game vui vẻ và có những trải nghiệm thật tuyệt!",
            Rewards = [new MailRewardMoneyVip { MoneyVip = moneyVipReward }]
        });
        await db.SaveChangesAsync();
        Log.Information(
            "CustomRecharge: {Name} (CharId {CharId}) nạp {Amount} VND, snapshot {Snapshot} tiên ngọc, thư {Reward} tiên ngọc, key {Key}",
            charData.Name, charData.Id, amountVnd, moneyVipSnapshot, moneyVipReward, key);
    }

    private async Task Gift(AppDbContext db)
    {
        var name = "Thần U Ma Đế";
        var charData = await db.Characters.FirstAsync(x => x.Name == name);
        var dailyRewards = await db.DailyRewardMonthlyPasses.Where(x => x.CharId == charData.Id).ToListAsync();
        for (int i = 0; i < 6; i++)
        {
            foreach (var d in dailyRewards)
            {
                await d.Claim(charData, db);
            }
        }

        Log.Information("ok");
    }

    private async Task RemoveEvents(AppDbContext db)
    {
        await db.Events.ExecuteDeleteAsync();
        await db.SaveChangesAsync();
        Log.Information("oke");
    }


    private async Task AddEvents(AppDbContext db)
    {
        var charIds = await db.EventManagers
            .Select(x => x.CharId)
            .ToArrayAsync();

        foreach (var charId in charIds)
        {
            var events = EventService.CreateEvents();
            foreach (var @event in events)
            {
                @event.CharId = charId;
                await db.Events.AddAsync(@event);
            }
        }

        await db.SaveChangesAsync();
        Log.Information("Oke");
    }

    private async Task ForceMoveOverflowClanMembers(
        AppDbContext db,
        int sourceClanId,
        int targetClanId,
        CancellationToken stoppingToken = default)
    {
        if (sourceClanId <= 0) throw new ArgumentOutOfRangeException(nameof(sourceClanId));
        if (targetClanId <= 0) throw new ArgumentOutOfRangeException(nameof(targetClanId));
        if (sourceClanId == targetClanId)
        {
            throw new ArgumentException("Clan nguồn và clan đích không được trùng nhau.");
        }

        await using var transaction = await db.Database.BeginTransactionAsync(stoppingToken);
        var sourceClan = await db.Clans
                             .FirstOrDefaultAsync(x => x.Id == sourceClanId, stoppingToken)
                         ?? throw new InvalidOperationException($"Không tìm thấy clan nguồn {sourceClanId}.");
        var targetClan = await db.Clans
                             .FirstOrDefaultAsync(x => x.Id == targetClanId, stoppingToken)
                         ?? throw new InvalidOperationException($"Không tìm thấy clan đích {targetClanId}.");

        var sourceMembers = await db.ClanMembers
            .Include(x => x.CharData)
            .ThenInclude(x => x.Progression)
            .Where(x => x.ClanId == sourceClanId)
            .ToListAsync(stoppingToken);
        var targetMemberCount = await db.ClanMembers
            .CountAsync(x => x.ClanId == targetClanId, stoppingToken);
        var overflowCount = sourceMembers.Count - sourceClan.MaxMembers;
        if (overflowCount <= 0)
        {
            sourceClan.MemberCount = sourceMembers.Count;
            targetClan.MemberCount = targetMemberCount;
            await db.SaveChangesAsync(stoppingToken);
            await transaction.CommitAsync(stoppingToken);
            Log.Information("Clan {SourceClanId} không quá slot: {MemberCount}/{MaxMembers}.",
                sourceClanId,
                sourceMembers.Count,
                sourceClan.MaxMembers);
            return;
        }

        var targetFreeSlots = Math.Max(0, targetClan.MaxMembers - targetMemberCount);
        var moveCount = Math.Min(overflowCount, targetFreeSlots);
        if (moveCount <= 0)
        {
            sourceClan.MemberCount = sourceMembers.Count;
            targetClan.MemberCount = targetMemberCount;
            await db.SaveChangesAsync(stoppingToken);
            await transaction.CommitAsync(stoppingToken);
            Log.Warning("Clan đích {TargetClanId} không còn slot trống: {MemberCount}/{MaxMembers}.",
                targetClanId,
                targetMemberCount,
                targetClan.MaxMembers);
            return;
        }

        var movedMembers = sourceMembers
            .Where(x => x.Role != ClanRole.Leader && x.CharId != sourceClan.LeaderCharId)
            .OrderBy(x => x.ContribPoints)
            .ThenBy(x => x.CharData.Progression.Level)
            .ThenBy(x => x.JoinAt)
            .Take(moveCount)
            .ToList();
        if (movedMembers.Count <= 0)
        {
            Log.Warning("Clan {SourceClanId} quá slot nhưng không có thành viên hợp lệ để chuyển.", sourceClanId);
            return;
        }

        var movedCharIds = movedMembers.Select(x => x.CharId).ToArray();
        await db.ClanJoinRequests
            .Where(x => x.ClanId == targetClanId && movedCharIds.Contains(x.CharId))
            .ExecuteDeleteAsync(stoppingToken);

        var movedAt = DateTime.UtcNow;
        var targetMembers = movedMembers
            .Select(x => new ClanMember
            {
                ClanId = targetClanId,
                CharId = x.CharId,
                ContribPoints = x.ContribPoints,
                LastFreeContributionAt = x.LastFreeContributionAt,
                Role = ClanRole.Member,
                JoinAt = movedAt
            })
            .ToList();
        db.ClanMembers.RemoveRange(movedMembers);
        await db.ClanMembers.AddRangeAsync(targetMembers, stoppingToken);

        sourceClan.MemberCount = sourceMembers.Count - movedMembers.Count;
        targetClan.MemberCount = targetMemberCount + movedMembers.Count;
        await db.SaveChangesAsync(stoppingToken);
        await transaction.CommitAsync(stoppingToken);

        Log.Information(
            "Đã chuyển {MoveCount} thành viên từ clan {SourceClanId} sang clan {TargetClanId}: {CharIds}.",
            movedMembers.Count,
            sourceClanId,
            targetClanId,
            string.Join(", ", movedCharIds));
    }

    private async Task ForceMoveAllOverflowClanMembers(
        AppDbContext db,
        CancellationToken stoppingToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(stoppingToken);
        var clans = await db.Clans
            .OrderBy(x => x.Id)
            .ToListAsync(stoppingToken);
        var memberCounts = await db.ClanMembers
            .GroupBy(x => x.ClanId)
            .Select(x => new { ClanId = x.Key, Count = x.Count() })
            .ToDictionaryAsync(x => x.ClanId, x => x.Count, stoppingToken);
        var clanById = clans.ToDictionary(x => x.Id);
        var currentCounts = clans.ToDictionary(
            x => x.Id,
            x => memberCounts.TryGetValue(x.Id, out var count) ? count : 0);
        var freeSlots = clans.ToDictionary(
            x => x.Id,
            x => Math.Max(0, x.MaxMembers - currentCounts[x.Id]));
        var overflowClans = clans
            .Where(x => currentCounts[x.Id] > x.MaxMembers)
            .OrderByDescending(x => currentCounts[x.Id] - x.MaxMembers)
            .ThenBy(x => x.Id)
            .ToList();
        var totalMoved = 0;
        foreach (var sourceClan in overflowClans)
        {
            var overflowCount = currentCounts[sourceClan.Id] - sourceClan.MaxMembers;
            if (overflowCount <= 0) continue;

            var sourceMembers = await db.ClanMembers
                .Include(x => x.CharData)
                .ThenInclude(x => x.Progression)
                .Where(x => x.ClanId == sourceClan.Id)
                .ToListAsync(stoppingToken);
            var candidates = sourceMembers
                .Where(x => x.Role != ClanRole.Leader && x.CharId != sourceClan.LeaderCharId)
                .OrderBy(x => x.ContribPoints)
                .ThenBy(x => x.CharData.Progression.MaxLevel)
                .ThenBy(x => x.JoinAt)
                .Take(overflowCount)
                .ToList();
            if (candidates.Count <= 0)
            {
                Log.Warning("Clan {SourceClanId} quá slot nhưng không có thành viên hợp lệ để chuyển.",
                    sourceClan.Id);
                continue;
            }

            foreach (var member in candidates)
            {
                var targetClan = clans
                    .Where(x => x.Id != sourceClan.Id && freeSlots[x.Id] > 0)
                    .OrderByDescending(x => freeSlots[x.Id])
                    .ThenBy(x => currentCounts[x.Id])
                    .ThenBy(x => x.Id)
                    .FirstOrDefault();
                if (targetClan == null)
                {
                    Log.Warning("Không còn clan đích có slot trống để chuyển tiếp từ clan {SourceClanId}.",
                        sourceClan.Id);
                    break;
                }

                await db.ClanJoinRequests
                    .Where(x => x.ClanId == targetClan.Id && x.CharId == member.CharId)
                    .ExecuteDeleteAsync(stoppingToken);
                db.ClanMembers.Remove(member);
                await db.ClanMembers.AddAsync(new ClanMember
                {
                    ClanId = targetClan.Id,
                    CharId = member.CharId,
                    ContribPoints = member.ContribPoints,
                    LastFreeContributionAt = member.LastFreeContributionAt,
                    Role = ClanRole.Member,
                    JoinAt = DateTime.UtcNow
                }, stoppingToken);

                currentCounts[sourceClan.Id]--;
                currentCounts[targetClan.Id]++;
                freeSlots[targetClan.Id]--;
                totalMoved++;
                Log.Information(
                    "Chuyển CharId {CharId} từ clan {SourceClanId} sang clan {TargetClanId}.",
                    member.CharId,
                    sourceClan.Id,
                    targetClan.Id);
            }
        }

        foreach (var (clanId, count) in currentCounts)
        {
            if (clanById.TryGetValue(clanId, out var clan))
            {
                clan.MemberCount = count;
            }
        }

        await db.SaveChangesAsync(stoppingToken);
        await transaction.CommitAsync(stoppingToken);
        Log.Information("Quét quá slot tông môn xong, đã chuyển {TotalMoved} thành viên.", totalMoved);
    }
}