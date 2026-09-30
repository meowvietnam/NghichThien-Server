using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using MTS.Data;
using MTS.Exceptions;
using MTS.Game.AllMaps;
using MTS.Game.LuyenKhi;
using MTS.IAP;
using MTS.Models;
using MTS.Network;
using MTS.Network.Core;
using MTS.Network.Models;
using Newtonsoft.Json;
using Serilog;

namespace MTS.Game.Clan;

public sealed class ClanCreationGate
{
    public SemaphoreSlim Semaphore { get; } = new(1, 1);
    public int RefCount;
}

public static partial class ClanManager
{
    private static readonly ConcurrentDictionary<int, ClanObserver> Observers = new();
    private static readonly ConcurrentDictionary<int, ClanReference> References = new();
    private static readonly ConcurrentDictionary<int, ClanCreationGate> CreationLocks = new();

    private static readonly ConcurrentDictionary<int, int> MemberClanIds = new();

    // Hồn Tinh cộng dồn từ farm quái, chờ ClanSoulEssenceFlushService ghi xuống DB theo chu kỳ
    // thay vì ghi mỗi lần giết quái (xem QueuePendingSoulEssence/FlushPendingSoulEssence).
    private static readonly ConcurrentDictionary<int, long> PendingSoulEssence = new();

    public static readonly ClanConfig Config = new()
    {
        CreateClanMoneyVipPrice = 300_000,
        CreateClanVndPrice = 500_000,
        CreateClanVndBonusMoneyVip = 100_000,
        CreateMaxMember = 20,
        MemberSlotLevelInterval = 3,
        MemberSlotsPerInterval = 1,
        CreateClanHonLimitLevel = 1,
        SoulEssenceHonCostBase = 1_000,
        SoulEssenceHonCostTierSize = 50,
        SoulEssenceHonCostGrowthPerTier = 1_000,
        MonsterKillSoulEssenceReward = 1,
        MaxHonByLimitLevel = [],
        MaxSoulEssenceByLimitLevel = [],
        UpgradeHonLimitFundCostByLevel = [],
        ContributionMoneyVipPerTime = 100,
        MaxContributionTimesPerRequest = 10,
        RolePermissions = ClanRolePermissions.Permissions,
        BaseInheritanceSlots = 1,
        InheritanceSlotsPerLevel = 1,
        MaxInheritanceSlotsPerColumn = 10,
        ChangeInheritanceContribPointsCost = 4_000,
        InheritanceBaseValue = 1,
        InheritanceValuePerClanLevel = 1,
        InheritanceBuffWhitelist = GameConfig.ClanBuffResources.EffectTypeConfigs.Select(x => x.Type).ToArray(),
        ClanLevelRequiredTotalExp = [0, 10_000, 20_000, 40_000, 80_000, 160_000, 320_000, 640_000, 1_280_000, 2_560_000],
        LeaveClanCooldownHours = 24,
        RoleAssignCooldownHours = 24
    };

    static ClanManager()
    {
        var limitHonLevel = 100;
        var maxHonByLimitLevel = Enumerable.Range(0, limitHonLevel).Select(x => (x + 1) * 10L).ToArray();
        var maxSoulEssenceByLimitLevel = Enumerable.Range(0, limitHonLevel).Select(x => (x + 1) * 1000L).ToArray();
        var upgradeHonLimitFundCostByLevel =
            Enumerable.Range(0, limitHonLevel).Select(x => 2000L * ((x + 10) / 10)).ToArray();
        Config.MaxHonByLimitLevel = maxHonByLimitLevel;
        Config.MaxSoulEssenceByLimitLevel = maxSoulEssenceByLimitLevel;
        Config.UpgradeHonLimitFundCostByLevel = upgradeHonLimitFundCostByLevel;
    }

    private static async Task<T> ExecuteClanTransactionAsync<T>(Func<AppDbContext, Task<T>> operation)
    {
        await using var db = await Program.CreateDbAsync();
        var strategy = db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            db.ChangeTracker.Clear();
            await using var transaction = await db.Database.BeginTransactionAsync();
            var result = await operation(db);
            await transaction.CommitAsync();
            return result;
        });
    }

    public static async Task<ClanReference> FindReference(int clanId)
    {
        await using var db = await Program.CreateDbAsync();
        return await FindReference(clanId, db);
    }

    public static async Task<ClanReference> FindReferenceByMember(int charId)
    {
        if (MemberClanIds.TryGetValue(charId, out var cachedClanId))
        {
            return await FindReference(cachedClanId);
        }

        await using var db = await Program.CreateDbAsync();
        var clanId = await db.ClanMembers
            .Where(x => x.CharId == charId)
            .Select(x => x.ClanId)
            .FirstOrDefaultAsync();
        if (clanId == 0)
        {
            throw new InvalidException("Chưa gia nhập tông môn");
        }

        MemberClanIds[charId] = clanId;
        return await FindReference(clanId, db);
    }

    public static async Task<Models.Clan[]> SearchClans(string name, int limit)
    {
        limit = Math.Clamp(limit, 1, 50);
        await using var db = await Program.CreateDbAsync();
        var nomalizeName = Utils.Utils.NormalizeSearchText(name);
        var clans = await db.Clans
            .AsNoTracking()
            .Where(x => x.NormalizeName.Contains(nomalizeName))
            .OrderByDescending(x => x.NormalizeName == nomalizeName)
            .ThenByDescending(x => x.NormalizeName.StartsWith(nomalizeName))
            .ThenBy(x => x.Name)
            .Take(limit)
            .ToArrayAsync();
        return clans
            .Select(x => x.CloneAsView())
            .ToArray();
    }

    public static async Task<Models.Clan?> ViewClan(int id)
    {
        Models.Clan? clan;
        if (References.TryGetValue(id, out var reference))
        {
            clan = reference.Clan.CloneAsView();
        }
        else
        {
            await using var db = await Program.CreateDbAsync();
            clan = await LoadClan(db, id, includeJoinRequests: false, includeShopItems: false);
        }

        return clan;
    }

    public static async Task<ClanMapNode[]> GetClanMapNodes()
    {
        await using var db = await Program.CreateDbAsync();
        return await db.Clans
            .AsNoTracking()
            .Where(x => x.NodeId > 0)
            .OrderBy(x => x.NodeId)
            .Select(x => new ClanMapNode
            {
                ClanId = x.Id,
                Name = x.Name,
                NodeId = x.NodeId,
                ImageUrl = x.ImageUrl
            })
            .ToArrayAsync();
    }

    public static async Task<ClanReference> CreateClan(CreateClanRequest request)
    {
        await ValidateCreateClanRequest(request);
        return await CreateClan(
            request.LeaderCharId,
            request.Name,
            request.NodeId,
            request.MaxMembers,
            request.Description,
            request.Announcement,
            request.ImageUrl,
            moneyVipCost: request.CreateBy == CreateClanBy.MoneyVip
                ? Config.CreateClanMoneyVipPrice
                : 0);
    }

    public static async Task<ClanReference> CreateClan(
        CreateClanRequest request,
        CharacterData leader)
    {
        request.LeaderCharId = leader.Id;
        await ValidateCreateClanRequest(request, leader);
        var remainingMoneyVip = leader.MoneyVip;
        if (request.CreateBy == CreateClanBy.MoneyVip)
        {
            remainingMoneyVip -= Config.CreateClanMoneyVipPrice;
            if (remainingMoneyVip < 0)
            {
                throw new InvalidException("Không đủ tiên ngọc");
            }
        }

        var reference = await CreateClan(
            request.LeaderCharId,
            request.Name,
            request.NodeId,
            request.MaxMembers,
            request.Description,
            request.Announcement,
            request.ImageUrl,
            leader,
            request.CreateBy == CreateClanBy.MoneyVip ? Config.CreateClanMoneyVipPrice : 0);

        return reference;
    }

    public static async Task ValidateCreateClanRequest(
        CreateClanRequest request,
        CharacterData? leader = null)
    {
        if (request == null)
        {
            throw new InvalidException("Dữ liệu tạo tông môn không hợp lệ");
        }

        var clanName = NormalizeClanName(request.Name);
        var normalizeName = Utils.Utils.NormalizeSearchText(clanName);
        if (normalizeName.Length == 0)
        {
            throw new InvalidException("Tên tông môn không hợp lệ");
        }

        if (clanName.Length > 15)
        {
            throw new InvalidException("Tên tông môn tối đa 15 ký tự");
        }

        if ((request.Description?.Length ?? 0) > 512)
        {
            throw new InvalidException("Mô tả tông môn tối đa 512 ký tự");
        }

        if ((request.Announcement?.Length ?? 0) > 512)
        {
            throw new InvalidException("Thông báo tông môn tối đa 512 ký tự");
        }

        if ((request.ImageUrl?.Length ?? 0) > 512)
        {
            throw new InvalidException("Link ảnh tông môn tối đa 512 ký tự");
        }

        if (!Enum.IsDefined(request.CreateBy))
        {
            throw new InvalidException("Phương thức tạo tông môn không hợp lệ");
        }

        if (request.CreateBy == CreateClanBy.Vnd && !Config.EnableCreateClanByVnd)
        {
            throw new InvalidException("Tạo tông môn bằng VND đang tạm đóng");
        }

        if (request.MaxMembers < 1)
        {
            throw new InvalidException("Số lượng thành viên tối đa không hợp lệ");
        }

        if (request.LeaderCharId <= 0)
        {
            throw new InvalidException("Nhân vật không tồn tại");
        }

        var node = GameConfig.MapManager.GetMap(request.NodeId);
        if (node.MapType == MapType.Stronghold)
        {
            throw new InvalidException("Không thể chọn cứ điểm chiến đấu làm vị trí tông môn");
        }

        if (request.CreateBy == CreateClanBy.MoneyVip && leader != null &&
            leader.MoneyVip < Config.CreateClanMoneyVipPrice)
        {
            throw new InvalidException("Không đủ tiên ngọc");
        }

        await using var db = await Program.CreateDbAsync();
        if (!await db.Characters.AnyAsync(x => x.Id == request.LeaderCharId))
        {
            throw new InvalidException("Nhân vật không tồn tại");
        }

        if (await db.ClanMembers.AnyAsync(x => x.CharId == request.LeaderCharId))
        {
            throw new InvalidException("Đã gia nhập tông môn");
        }

        if (await db.Clans.AnyAsync(x => x.NormalizeName == normalizeName))
        {
            throw new InvalidException("Tên tông môn đã tồn tại");
        }

        if (await db.Clans.AnyAsync(x => x.NodeId == request.NodeId))
        {
            throw new InvalidException("Vị trí này đã có tông môn");
        }

        request.Name = clanName;
        request.Description ??= string.Empty;
        request.Announcement ??= string.Empty;
    }

    public static async Task<ClanReference> CreateClan(
        int leaderCharId,
        string name,
        int nodeId,
        int maxMembers,
        string description = "",
        string announcement = "",
        string? imageUrl = null,
        CharacterData? leader = null,
        int moneyVipCost = 0,
        int moneyVipBonus = 0,
        Func<AppDbContext, Task>? transactionWork = null)
    {
        var clanName = NormalizeClanName(name);
        var normalizeName = Utils.Utils.NormalizeSearchText(clanName);
        if (normalizeName.Length == 0)
        {
            throw new InvalidException("Tên tông môn không hợp lệ");
        }

        if (maxMembers < 1)
        {
            throw new InvalidException("Số lượng thành viên tối đa không hợp lệ");
        }

        var gate = CreationLocks.AddOrUpdate(
            leaderCharId,
            _ => new ClanCreationGate { RefCount = 1 },
            (_, existing) =>
            {
                Interlocked.Increment(ref existing.RefCount);
                return existing;
            });

        await gate.Semaphore.WaitAsync();
        try
        {
            if (MemberClanIds.ContainsKey(leaderCharId))
            {
                throw new InvalidException("Đã gia nhập tông môn");
            }

            var now = DateTime.UtcNow;
            var persisted = await ExecuteClanTransactionAsync(async db =>
            {
                var persistedLeader = await LoadCharForClan(db, leaderCharId)
                                      ?? throw new InvalidException("Nhân vật không tồn tại");

                // A retry after an uncertain commit must observe the clan created by the
                // previous attempt and must not charge the leader a second time.
                var existingClan = await db.Clans
                    .Include(x => x.Members)
                    .FirstOrDefaultAsync(x => x.LeaderCharId == leaderCharId &&
                                              x.NormalizeName == normalizeName && x.NodeId == nodeId);
                if (existingClan != null)
                {
                    if (transactionWork != null)
                    {
                        await transactionWork(db);
                        await db.SaveChangesAsync();
                    }

                    var existingMember = existingClan.Members.First(x => x.CharId == leaderCharId);
                    return (Clan: existingClan, Member: existingMember,
                        Leader: persistedLeader, MoneyVip: persistedLeader.MoneyVip);
                }

                if (await db.ClanMembers.AnyAsync(x => x.CharId == leaderCharId))
                    throw new InvalidException("Đã gia nhập tông môn");
                if (await db.Clans.AnyAsync(x => x.NormalizeName == normalizeName))
                    throw new InvalidException("Tên tông môn đã tồn tại");
                if (await db.Clans.AnyAsync(x => x.NodeId == nodeId))
                    throw new InvalidException("Vị trí này đã có tông môn");

                var newMoneyVip = persistedLeader.MoneyVip;
                if (moneyVipCost > 0)
                {
                    var updated = await db.Characters
                        .Where(x => x.Id == leaderCharId && x.MoneyVip >= moneyVipCost)
                        .ExecuteUpdateAsync(x => x.SetProperty(c => c.MoneyVip, c => c.MoneyVip - moneyVipCost));
                    if (updated != 1)
                        throw new InvalidException("Không đủ tiên ngọc");

                    await db.SpendingMoneyVips.AddAsync(new SpendingMoneyVip
                    {
                        CharId = leaderCharId,
                        SpendFor = SpendFor.CreateClan,
                        MoneyVip = moneyVipCost,
                        Time = now
                    });
                    newMoneyVip -= moneyVipCost;
                }

                if (moneyVipBonus > 0)
                {
                    var updated = await db.Characters
                        .Where(x => x.Id == leaderCharId && x.MoneyVip <= int.MaxValue - moneyVipBonus)
                        .ExecuteUpdateAsync(x => x.SetProperty(c => c.MoneyVip, c => c.MoneyVip + moneyVipBonus));
                    if (updated != 1)
                        throw new InvalidException("Tiên ngọc đã đạt giới hạn");

                    newMoneyVip += moneyVipBonus;
                }

                var member = new ClanMember
                {
                    CharId = leaderCharId,
                    Role = ClanRole.Leader,
                    JoinAt = now,
                    LastFreeContributionAt = persistedLeader.LastFreeClanContributionAt
                };
                var clan = new Models.Clan
                {
                    Name = clanName,
                    NormalizeName = normalizeName,
                    NodeId = nodeId,
                    Description = description ?? string.Empty,
                    Announcement = announcement ?? string.Empty,
                    ImageUrl = imageUrl,
                    MemberCount = 1,
                    MaxMembers = maxMembers,
                    Level = 1,
                    MaxHon = GetMaxHon(Config.CreateClanHonLimitLevel),
                    HonLimitLevel = Config.CreateClanHonLimitLevel,
                    LeaderCharId = leaderCharId,
                    CreatedAt = now,
                    Members = [member]
                };

                await db.Clans.AddAsync(clan);
                if (transactionWork != null) await transactionWork(db);
                await db.SaveChangesAsync();
                return (Clan: clan, Member: member, Leader: persistedLeader,
                    MoneyVip: newMoneyVip);
            });

            var clan = persisted.Clan;
            var member = persisted.Member;
            member.CharData = leader ?? persisted.Leader;
            if (leader != null && (moneyVipCost > 0 || moneyVipBonus > 0))
                leader.SetMoneyVip(persisted.MoneyVip);
            MemberClanIds[leaderCharId] = clan.Id;
            var reference = References.GetOrAdd(clan.Id, _ => new ClanReference(clan));
            FindOrCreateObserver(reference.Clan);
            PublishClanMapNodeChanged(reference.Clan);
            return reference;
        }
        finally
        {
            gate.Semaphore.Release();

            if (Interlocked.Decrement(ref gate.RefCount) == 0)
            {
                ((ICollection<KeyValuePair<int, ClanCreationGate>>)CreationLocks)
                    .Remove(new KeyValuePair<int, ClanCreationGate>(leaderCharId, gate));
            }
        }
    }

    public static async Task<ClanMember> AddMember(
        ClanReference reference,
        int charId,
        ClanRole role = ClanRole.Member)
    {
        var request = reference.Clan.JoinRequests.FirstOrDefault(x => x.CharId == charId);
        if (request?.CharData == null)
        {
            throw new InvalidException("Không tìm thấy đơn xin gia nhập trong RAM");
        }

        return await AddMember(reference, request.CharData, role);
    }

    public static async Task<ClanMember> AddMember(
        ClanReference reference,
        CharacterData charData,
        ClanRole role = ClanRole.Member)
    {
        if (role == ClanRole.Leader)
        {
            throw new InvalidException("Không thể thêm thành viên với quyền tông chủ");
        }

        var clan = reference.Clan;
        var clanId = clan.Id;
        var charId = charData.Id;
        var gate = CreationLocks.AddOrUpdate(
            clanId,
            _ => new ClanCreationGate { RefCount = 1 },
            (_, existing) =>
            {
                Interlocked.Increment(ref existing.RefCount);
                return existing;
            });

        await gate.Semaphore.WaitAsync();
        try
        {
            if (clan.MemberCount >= clan.MaxMembers)
            {
                throw new InvalidException("Tông môn đã đầy");
            }

            if (clan.Members.Any(x => x.CharId == charId) || MemberClanIds.ContainsKey(charId))
            {
                throw new InvalidException("Đã gia nhập tông môn");
            }

            ValidateUniqueRole(clan, role, charId);

            var joinedAt = DateTime.UtcNow;
            var member = await ExecuteClanTransactionAsync(async db =>
            {
                var existing = await db.ClanMembers.FirstOrDefaultAsync(x => x.CharId == charId);
                if (existing != null)
                {
                    if (existing.ClanId == clanId) return existing;
                    throw new InvalidException("Đã gia nhập tông môn");
                }

                if (IsUniqueRole(role) && await db.ClanMembers.AnyAsync(x =>
                        x.ClanId == clanId && x.Role == role && x.CharId != charId))
                    throw new InvalidException($"Tông môn chỉ được có 1 {GetRoleName(role)}");

                var persistedMember = new ClanMember
                {
                    ClanId = clanId,
                    CharId = charId,
                    Role = role,
                    JoinAt = joinedAt,
                    LastFreeContributionAt = charData.LastFreeClanContributionAt
                };
                await db.ClanMembers.AddAsync(persistedMember);
                await db.ClanJoinRequests
                    .Where(x => x.ClanId == clanId && x.CharId == charId)
                    .ExecuteDeleteAsync();
                var affected = await db.Clans
                    .Where(x => x.Id == clanId && x.MemberCount < x.MaxMembers)
                    .ExecuteUpdateAsync(x => x.SetProperty(c => c.MemberCount, c => c.MemberCount + 1));
                if (affected == 0)
                    throw new InvalidException("Tông môn không tồn tại hoặc đã đầy");

                await db.SaveChangesAsync();
                return persistedMember;
            });

            member.CharData = charData;
            MemberClanIds[charId] = clanId;
            clan.MemberCount++;
            clan.Members.Add(member);
            clan.JoinRequests.RemoveAll(x => x.CharId == charId);
            if (Observers.TryGetValue(clanId, out var observer))
            {
                observer.NotifyAddMember(member);
            }

            return member;
        }
        finally
        {
            gate.Semaphore.Release();

            if (Interlocked.Decrement(ref gate.RefCount) == 0)
            {
                ((ICollection<KeyValuePair<int, ClanCreationGate>>)CreationLocks)
                    .Remove(new KeyValuePair<int, ClanCreationGate>(clanId, gate));
            }
        }
    }

    public static async Task<ClanMember> KickMember(ClanReference reference, int charId)
    {
        if (charId <= 0)
        {
            throw new InvalidException("Thành viên không hợp lệ");
        }

        var clan = reference.Clan;
        var member = clan.Members.FirstOrDefault(x => x.CharId == charId);
        if (member == null)
        {
            throw new InvalidException("Thành viên không tồn tại");
        }

        if (member.Role == ClanRole.Leader || clan.LeaderCharId == charId)
        {
            throw new InvalidException("Không thể trục xuất tông chủ");
        }

        return await RemoveMember(reference, member, ClanRemoveMemberReason.Kick);
    }

    public static async Task<ClanContribution> Contribute(
        ClanReference reference,
        CharacterData charData,
        int times)
    {
        if (charData.Progression.MaxLevel < 4)
        {
            throw new InvalidException("Từ Trúc Cơ trở lên mới được cống hiến");
        }

        if (times <= 0)
        {
            throw new InvalidException("Số lượt cống hiến không hợp lệ");
        }

        if (times > Config.MaxContributionTimesPerRequest)
        {
            throw new InvalidException(
                $"Chỉ được cống hiến tối đa {Config.MaxContributionTimesPerRequest} lượt mỗi lần");
        }

        var amountPerTime = Config.ContributionMoneyVipPerTime;
        if (amountPerTime <= 0)
        {
            throw new InvalidException("Cấu hình cống hiến không hợp lệ");
        }

        var clan = reference.Clan;
        var clanId = clan.Id;
        var charId = charData.Id;
        var gate = CreationLocks.AddOrUpdate(
            clanId,
            _ => new ClanCreationGate { RefCount = 1 },
            (_, existing) =>
            {
                Interlocked.Increment(ref existing.RefCount);
                return existing;
            });

        await gate.Semaphore.WaitAsync();
        try
        {
            var member = clan.Members.FirstOrDefault(x => x.CharId == charId);
            if (member == null)
            {
                throw new InvalidException("Chưa gia nhập tông môn");
            }

            var now = DateTime.UtcNow;
            // Lượt miễn phí thuộc về nhân vật, không thuộc về bản ghi membership. Nếu chỉ lưu
            // ở ClanMember, rời tông môn sẽ xóa dấu này và gia nhập lại sẽ nhận lượt mới.
            var previousFreeContributionAt = charData.LastFreeClanContributionAt;
            var hasFreeContribution = previousFreeContributionAt.Date == now.Date;
            var freeTimes = hasFreeContribution ? 0 : 1;
            if (freeTimes > times)
            {
                freeTimes = times;
            }

            var paidTimes = times - freeTimes;
            int cost;
            long amount;
            try
            {
                cost = checked(paidTimes * amountPerTime);
                amount = checked((long)times * amountPerTime);
            }
            catch (OverflowException)
            {
                throw new InvalidException("Giá trị cống hiến vượt giới hạn");
            }

            var remainingMoneyVip = charData.MoneyVip - cost;
            if (remainingMoneyVip < 0)
            {
                throw new InvalidException("Không đủ tiên ngọc");
            }

            long newFunds;
            long newContribPoints;
            long newTotalContribPoints;
            try
            {
                newFunds = checked(clan.Funds + amount);
                newContribPoints = checked(member.ContribPoints + amount);
                newTotalContribPoints = checked(member.TotalContribPoints + amount);
            }
            catch (OverflowException)
            {
                throw new InvalidException("Tài nguyên tông môn đã đạt giới hạn");
            }

            var lastFreeContributionAt = freeTimes > 0
                ? now
                : previousFreeContributionAt;

            await ExecuteClanTransactionAsync(async db =>
            {
                var alreadyCommitted = await db.ClanMembers.AsNoTracking().AnyAsync(x =>
                                           x.ClanId == clanId && x.CharId == charId &&
                                           x.ContribPoints == newContribPoints &&
                                           x.TotalContribPoints == newTotalContribPoints &&
                                           x.LastFreeContributionAt == lastFreeContributionAt) &&
                                       await db.Clans.AsNoTracking()
                                           .AnyAsync(x => x.Id == clanId && x.Funds == newFunds) &&
                                       await db.Characters.AsNoTracking().AnyAsync(x =>
                                           x.Id == charId &&
                                           x.MoneyVip == remainingMoneyVip &&
                                           x.LastFreeClanContributionAt == lastFreeContributionAt);
                if (alreadyCommitted) return true;

                var updatedMember = await db.ClanMembers
                    .Where(x => x.ClanId == clanId && x.CharId == charId &&
                                x.ContribPoints == member.ContribPoints &&
                                x.TotalContribPoints == member.TotalContribPoints &&
                                x.LastFreeContributionAt == member.LastFreeContributionAt)
                    .ExecuteUpdateAsync(x => x
                        .SetProperty(m => m.ContribPoints, newContribPoints)
                        .SetProperty(m => m.TotalContribPoints, newTotalContribPoints)
                        .SetProperty(m => m.LastFreeContributionAt, lastFreeContributionAt));
                if (updatedMember != 1)
                    throw new InvalidException("Dữ liệu cống hiến đã thay đổi, vui lòng thử lại");

                var updatedClan = await db.Clans
                    .Where(x => x.Id == clanId && x.Funds == clan.Funds)
                    .ExecuteUpdateAsync(x => x.SetProperty(c => c.Funds, newFunds));
                if (updatedClan != 1)
                    throw new InvalidException("Dữ liệu tông môn đã thay đổi, vui lòng thử lại");

                var updatedChar = await db.Characters
                    .Where(x => x.Id == charId &&
                                x.MoneyVip == charData.MoneyVip &&
                                x.MoneyVip >= cost &&
                                x.LastFreeClanContributionAt == previousFreeContributionAt)
                    .ExecuteUpdateAsync(x => x
                        .SetProperty(c => c.MoneyVip, remainingMoneyVip)
                        .SetProperty(c => c.LastFreeClanContributionAt, lastFreeContributionAt));
                if (updatedChar != 1)
                    throw new InvalidException("Lượt miễn phí hoặc tiên ngọc đã thay đổi, vui lòng thử lại");

                if (cost > 0)
                {
                    await db.SpendingMoneyVips.AddAsync(new SpendingMoneyVip
                    {
                        CharId = charId,
                        SpendFor = SpendFor.ClanContribution,
                        MoneyVip = cost,
                        Time = now
                    });
                    await db.SaveChangesAsync();
                }

                return true;
            });

            charData.SetMoneyVip(remainingMoneyVip);
            charData.SetLastFreeClanContributionAt(lastFreeContributionAt);
            clan.Funds = newFunds;
            member.ContribPoints = newContribPoints;
            member.TotalContribPoints = newTotalContribPoints;
            member.LastFreeContributionAt = lastFreeContributionAt;

            var contribution = new ClanContribution
            {
                ClanId = clanId,
                CharId = charId,
                Times = times,
                FreeTimes = freeTimes,
                PaidTimes = paidTimes,
                Amount = amount,
                ClanFunds = newFunds,
                MemberContribPoints = newContribPoints,
                MemberTotalContribPoints = newTotalContribPoints,
                LastFreeContributionAt = lastFreeContributionAt,
                FreeContributionTimesRemaining = 0
            };

            if (Observers.TryGetValue(clanId, out var observer))
            {
                observer.NotifyContribution(contribution);
            }

            return contribution;
        }
        finally
        {
            gate.Semaphore.Release();

            if (Interlocked.Decrement(ref gate.RefCount) == 0)
            {
                ((ICollection<KeyValuePair<int, ClanCreationGate>>)CreationLocks)
                    .Remove(new KeyValuePair<int, ClanCreationGate>(clanId, gate));
            }
        }
    }

    public static async Task<ClanMember> LeaveClan(ClanReference reference, CharacterData charData)
    {
        var charId = charData.Id;
        if (charId <= 0)
        {
            throw new InvalidException("Thành viên không hợp lệ");
        }

        var clan = reference.Clan;
        var member = clan.Members.FirstOrDefault(x => x.CharId == charId);
        if (member == null)
        {
            throw new InvalidException("Chưa gia nhập tông môn");
        }

        if ((member.Role == ClanRole.Leader || clan.LeaderCharId == charId) && clan.MemberCount > 1)
        {
            throw new InvalidException("Tông chủ không thể rời tông môn khi còn thành viên");
        }

        var result = await RemoveMember(reference, member, ClanRemoveMemberReason.Leave);
        // Chỉ tính cooldown khi TỰ rời (không áp dụng khi bị kick, vì KickMember không đi qua đây).
        charData.SetLastLeaveClanAt(DateTime.UtcNow);
        return result;
    }

    private static async Task<ClanMember> RemoveMember(
        ClanReference reference,
        ClanMember member,
        ClanRemoveMemberReason reason)
    {
        var clan = reference.Clan;
        var clanId = clan.Id;
        var charId = member.CharId;
        var deleteClan = member.Role == ClanRole.Leader || clan.LeaderCharId == charId;
        var gate = CreationLocks.AddOrUpdate(
            clanId,
            _ => new ClanCreationGate { RefCount = 1 },
            (_, existing) =>
            {
                Interlocked.Increment(ref existing.RefCount);
                return existing;
            });

        await gate.Semaphore.WaitAsync();
        try
        {
            if (!clan.Members.Any(x => x.CharId == charId))
            {
                throw new InvalidException("Thành viên không tồn tại");
            }

            var returnedShopItems = clan.ShopItems
                .Where(x => deleteClan || x.OwnerCharId == charId)
                .ToList();
            await ExecuteClanTransactionAsync(async db =>
            {
                var persistedShopItems = await db.ClanShopItems
                    .Where(x => x.ClanId == clanId && (deleteClan || x.OwnerCharId == charId))
                    .ToListAsync();

                if (deleteClan)
                {
                    if (!await db.Clans.AnyAsync(x => x.Id == clanId))
                        return true;

                    ReturnClanShopItems(db, persistedShopItems);
                    await db.ClanShopItems.Where(x => x.ClanId == clanId).ExecuteDeleteAsync();
                    await db.ClanJoinRequests.Where(x => x.ClanId == clanId).ExecuteDeleteAsync();
                    await db.ClanMembers.Where(x => x.ClanId == clanId).ExecuteDeleteAsync();
                    var deletedClan = await db.Clans.Where(x => x.Id == clanId).ExecuteDeleteAsync();
                    if (deletedClan != 1)
                        throw new InvalidException("Tông môn không tồn tại");
                }
                else
                {
                    if (!await db.ClanMembers.AnyAsync(x => x.ClanId == clanId && x.CharId == charId))
                        return true;

                    ReturnClanShopItems(db, persistedShopItems);
                    await db.ClanShopItems
                        .Where(x => x.ClanId == clanId && x.OwnerCharId == charId)
                        .ExecuteDeleteAsync();
                    var deletedMember = await db.ClanMembers
                        .Where(x => x.ClanId == clanId && x.CharId == charId)
                        .ExecuteDeleteAsync();
                    if (deletedMember != 1)
                        throw new InvalidException("Thành viên không tồn tại");

                    var affected = await db.Clans
                        .Where(x => x.Id == clanId && x.MemberCount > 0)
                        .ExecuteUpdateAsync(x => x.SetProperty(c => c.MemberCount, c => c.MemberCount - 1));
                    if (affected != 1)
                        throw new InvalidException("Tông môn không tồn tại");
                }

                if (persistedShopItems.Count > 0) await db.SaveChangesAsync();
                return true;
            });

            if (deleteClan)
            {
                PublishDeleted(clanId);
            }
            else
            {
                clan.Members.RemoveAll(x => x.CharId == charId);
                foreach (var shopItem in returnedShopItems)
                {
                    clan.ShopItems.RemoveAll(x => x.Id == shopItem.Id);
                }

                clan.MemberCount = Math.Max(0, clan.MemberCount - 1);
                MemberClanIds.TryRemove(charId, out _);
                TMC.WarManager.OnMemberRemoved(clanId, charId);
                if (Observers.TryGetValue(clanId, out var observer))
                {
                    foreach (var shopItem in returnedShopItems)
                    {
                        observer.SendAll(MessageTag.ClanShopItemRemoved, new ClanShopItemRemoved
                        {
                            ClanId = clanId,
                            ShopItemId = shopItem.Id
                        });
                    }

                    observer.NotifyRemoveMember(charId, reason);
                }
            }

            return member;
        }
        finally
        {
            gate.Semaphore.Release();

            if (Interlocked.Decrement(ref gate.RefCount) == 0)
            {
                ((ICollection<KeyValuePair<int, ClanCreationGate>>)CreationLocks)
                    .Remove(new KeyValuePair<int, ClanCreationGate>(clanId, gate));
            }
        }
    }

    public static async Task<(ClanObserver observer, ClanReference reference)> WatchClan(
        int clanId,
        IClanObserverState state)
    {
        var reference = await FindReference(clanId);
        var observer = FindOrCreateObserver(reference.Clan);
        reference.AddState(state);
        observer.AddState(state);
        MemberClanIds[state.CharId] = clanId;
        return (observer, reference);
    }

    public static void ForgetMember(int charId)
    {
        MemberClanIds.TryRemove(charId, out _);
    }

    public static void RememberMember(int charId, int clanId)
    {
        MemberClanIds[charId] = clanId;
    }

    private static bool IsUniqueRole(ClanRole role)
    {
        return role is ClanRole.ViceLeader or ClanRole.Elder;
    }

    private static void ValidateUniqueRole(Models.Clan clan, ClanRole role, int targetCharId)
    {
        if (!IsUniqueRole(role)) return;

        if (clan.Members.Any(x => x.Role == role && x.CharId != targetCharId))
            throw new InvalidException($"Tông môn chỉ được có 1 {GetRoleName(role)}");
    }

    private static string GetRoleName(ClanRole role)
    {
        return role switch
        {
            ClanRole.ViceLeader => "Phó Tộc Chủ",
            ClanRole.Elder => "Trưởng Lão",
            ClanRole.Leader => "Tộc Chủ",
            _ => "Đệ tử"
        };
    }

    private static ClanMapNode ToMapNode(Models.Clan clan)
    {
        return new ClanMapNode
        {
            ClanId = clan.Id,
            Name = clan.Name,
            NodeId = clan.NodeId,
            ImageUrl = clan.ImageUrl
        };
    }

    private static void PublishClanMapNodeChanged(Models.Clan clan)
    {
        if (clan.NodeId <= 0) return;
        TMC.WarManager.UpsertClan(clan);

        var changed = new ClanMapNodeChanged { Node = ToMapNode(clan) };
        foreach (var client in TcpServer.Instance.GetClientsOnline())
        {
            if (!client.IsCharacterSelected()) continue;
            client.MemSend(MessageTag.ClanMapNodeChanged, changed);
        }
    }

    public static bool TryGetCharacterClanSnapshot(
        int charId,
        out int clanId,
        out string clanName,
        out string? clanImageUrl,
        out int clanLevel,
        out long hon,
        out long maxHon,
        out ClanRole clanRole,
        out ClanInheritanceEffect[] inheritanceEffects)
    {
        clanId = 0;
        clanName = string.Empty;
        clanImageUrl = null;
        clanLevel = 0;
        hon = 0;
        maxHon = 0;
        clanRole = ClanRole.Member;
        inheritanceEffects = [];

        if (!MemberClanIds.TryGetValue(charId, out var cachedClanId) ||
            !References.TryGetValue(cachedClanId, out var reference))
        {
            return false;
        }

        var clan = reference.Clan;
        var member = clan.Members.FirstOrDefault(x => x.CharId == charId);
        if (member == null)
        {
            return false;
        }

        clanId = clan.Id;
        clanName = clan.Name;
        clanImageUrl = clan.ImageUrl;
        clanLevel = clan.Level;
        hon = clan.Hon;
        maxHon = clan.MaxHon;
        clanRole = member.Role;
        inheritanceEffects = clan.InheritanceEffects.ToArray();
        return true;
    }

    public static void PublishDeleted(int clanId)
    {
        TMC.WarManager.OnClanDeleted(clanId);
        if (Observers.TryRemove(clanId, out var observer))
        {
            observer.OnClanDeleted();
        }

        References.TryRemove(clanId, out _);
        foreach (var item in MemberClanIds.Where(x => x.Value == clanId).ToArray())
        {
            MemberClanIds.TryRemove(item.Key, out _);
        }
    }

    public static void OnRemoveState(ClanObserver observer, IClanObserverState state)
    {
        if (observer.HasNoStates())
        {
            Observers.TryRemove(observer.Clan.Id, out _);
        }
    }

    public static void OnRemoveState(ClanReference reference, IClanObserverState state)
    {
        if (reference.HasNoStates())
        {
            ((ICollection<KeyValuePair<int, ClanReference>>)References)
                .Remove(new KeyValuePair<int, ClanReference>(reference.Clan.Id, reference));
        }
    }

    private static async Task<ClanReference> FindReference(int clanId, AppDbContext db)
    {
        var gate = CreationLocks.AddOrUpdate(
            clanId,
            _ => new ClanCreationGate { RefCount = 1 },
            (_, existing) =>
            {
                Interlocked.Increment(ref existing.RefCount);
                return existing;
            });

        await gate.Semaphore.WaitAsync();

        try
        {
            if (References.TryGetValue(clanId, out var reference))
            {
                return reference;
            }

            var clan = await LoadClan(db, clanId, includeJoinRequests: true, includeShopItems: true);
            if (clan == null)
            {
                throw new InvalidException("Tông môn không tồn tại");
            }

            foreach (var member in clan.Members)
            {
                MemberClanIds[member.CharId] = clan.Id;
            }

            clan.RefreshInheritanceViews();

            reference = new ClanReference(clan);
            References.TryAdd(clanId, reference);
            return reference;
        }
        finally
        {
            gate.Semaphore.Release();

            if (Interlocked.Decrement(ref gate.RefCount) == 0)
            {
                ((ICollection<KeyValuePair<int, ClanCreationGate>>)CreationLocks)
                    .Remove(new KeyValuePair<int, ClanCreationGate>(clanId, gate));
            }
        }
    }

    private static async Task<Models.Clan?> LoadClan(
        AppDbContext db,
        int clanId,
        bool includeJoinRequests,
        bool includeShopItems)
    {
        var clan = await db.Clans
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == clanId);
        if (clan == null)
        {
            return null;
        }

        var members = await db.ClanMembers
            .AsNoTracking()
            .Where(x => x.ClanId == clanId)
            .Include(x => x.CharData)
            .ThenInclude(x => x.Appearance)
            .Include(x => x.CharData)
            .ThenInclude(x => x.Progression)
            .OrderByDescending(x => x.Role)
            .ThenBy(x => x.JoinAt)
            .ToListAsync();
        clan.Members = members;

        clan.JoinRequests = includeJoinRequests
            ? await db.ClanJoinRequests
                .AsNoTracking()
                .Where(x => x.ClanId == clanId)
                .Include(x => x.CharData)
                .ThenInclude(x => x.Appearance)
                .Include(x => x.CharData)
                .ThenInclude(x => x.Progression)
                .OrderBy(x => x.Created)
                .ToListAsync()
            : new List<ClanJoinRequest>();

        clan.ShopItems = includeShopItems
            ? await db.ClanShopItems
                .AsNoTracking()
                .Where(x => x.ClanId == clanId)
                .OrderBy(x => x.CreatedAt)
                .ToListAsync()
            : new List<ClanShopItem>();

        clan.InheritanceEffects = await db.ClanInheritanceEffects
            .AsNoTracking()
            .Where(x => x.ClanId == clanId)
            .OrderBy(x => x.Trigger)
            .ThenBy(x => x.SlotIndex)
            .ToListAsync();
        clan.RefreshInheritanceViews();
        return clan;
    }

    private static ClanObserver FindOrCreateObserver(Models.Clan clan)
    {
        if (Observers.TryGetValue(clan.Id, out var observer))
        {
            return observer;
        }

        observer = new ClanObserver(clan);
        Observers.TryAdd(clan.Id, observer);
        return observer;
    }

    private static async Task<CharacterData?> LoadCharForClan(AppDbContext db, int charId)
    {
        return await db.Characters
            .AsNoTracking()
            .Include(x => x.Appearance)
            .Include(x => x.Progression)
            .FirstOrDefaultAsync(x => x.Id == charId);
    }

    private static void ReturnClanShopItems(AppDbContext db, IEnumerable<ClanShopItem> shopItems)
    {
        var items = shopItems.ToArray();
        var nextFurnaceIds = new Dictionary<int, int>();
        foreach (var shopItem in items.Where(x => x.ItemType == ItemType.Furnace))
        {
            if (!nextFurnaceIds.TryGetValue(shopItem.OwnerCharId, out var nextSelfId))
            {
                nextSelfId = db.Furnaces
                    .Where(x => x.CharId == shopItem.OwnerCharId)
                    .Select(x => (int)x.SelfId)
                    .DefaultIfEmpty(0)
                    .Max() + 1;
            }

            if (nextSelfId > ushort.MaxValue)
            {
                throw new InvalidException("Số lượng đan lô trong túi vượt giới hạn");
            }

            db.Furnaces.Add(new FurnaceData
            {
                CharId = shopItem.OwnerCharId,
                SelfId = (ushort)nextSelfId,
                Id = shopItem.ItemId,
                Durability = shopItem.ItemDurability > 0 ? shopItem.ItemDurability : (sbyte)100
            });
            nextFurnaceIds[shopItem.OwnerCharId] = nextSelfId + 1;
        }

        foreach (var group in items
                     .Where(x => x.ItemType != ItemType.Furnace)
                     .GroupBy(x => new { x.OwnerCharId, x.ItemId, x.ItemType }))
        {
            var total = group.Sum(x => (long)x.Count);
            if (total <= 0 || total > int.MaxValue)
            {
                throw new InvalidException("Số lượng vật phẩm kho tông môn không hợp lệ");
            }

            var item = db.Items.SingleOrDefault(x => x.CharId == group.Key.OwnerCharId &&
                                                     x.Id == group.Key.ItemId && x.Type == group.Key.ItemType);
            if (item == null)
            {
                db.Items.Add(new ItemRef(group.Key.ItemId, group.Key.ItemType, (int)total)
                {
                    CharId = group.Key.OwnerCharId
                });
            }
            else if ((long)item.Count + total > int.MaxValue)
            {
                throw new InvalidException("Số lượng vật phẩm trong túi vượt giới hạn");
            }
            else
            {
                item.Count += (int)total;
            }
        }
    }

    private static string NormalizeClanName(string name)
    {
        return string.Join(" ", (name ?? string.Empty)
            .Trim()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    public static async Task Handle(Order order, AppDbContext db)
    {
        var request = JsonConvert.DeserializeObject<CreateClanRequest>(order.Data ?? string.Empty);
        if (request == null)
        {
            throw new InvalidException("Dữ liệu tạo tông môn không hợp lệ");
        }

        var charId = order.CharId;
        var charData = TcpServer.Instance
            .GetCharsOnline()
            .FirstOrDefault(x => x.Id == charId);
        request.LeaderCharId = charId;
        request.CreateBy = CreateClanBy.Vnd;
        await ValidateCreateClanRequest(request, charData);

        var reference = await CreateClan(
            charId,
            request.Name,
            request.NodeId,
            request.MaxMembers,
            request.Description,
            request.Announcement,
            request.ImageUrl,
            charData,
            moneyVipBonus: Config.CreateClanVndBonusMoneyVip,
            transactionWork: async transactionDb =>
            {
                var persistedOrder = await transactionDb.Orders
                                         .Include(x => x.Purchase)
                                         .FirstOrDefaultAsync(x => x.OrderId == order.OrderId)
                                     ?? throw new InvalidException("Đơn thanh toán không tồn tại");

                if (persistedOrder.Status == OrderStatus.Complete)
                {
                    if (persistedOrder.Purchase == null)
                        throw new InvalidException("Dữ liệu thanh toán tông môn không hợp lệ");
                    return;
                }

                if (persistedOrder.Status != OrderStatus.Processing ||
                    persistedOrder.OrderType != OrderType.CreateClan ||
                    persistedOrder.CharId != charId)
                    throw new InvalidException("Trạng thái đơn thanh toán không hợp lệ");

                persistedOrder.Purchase = new IapPurchase
                {
                    CharId = charId,
                    AmountSnapshot = persistedOrder.Amount,
                    Store = Store.Web,
                    PurchaseTokenHash = Array.Empty<byte>()
                };
                persistedOrder.Status = OrderStatus.Complete;
            });

        // Publish to sockets/cache only after the retriable database unit committed.
        order.Status = OrderStatus.Complete;
        if (charData != null)
        {
            var clanHandler = charData.Client?.GetHandler<ClanHandler>();
            if (clanHandler != null) await clanHandler.OnClanCreated(reference.Clan);
        }
        else if (reference.HasNoStates())
        {
            ((ICollection<KeyValuePair<int, ClanReference>>)References)
                .Remove(new KeyValuePair<int, ClanReference>(reference.Clan.Id, reference));
        }
    }
}
