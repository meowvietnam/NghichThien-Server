using System.Data;
using Microsoft.EntityFrameworkCore;
using MTS.Models;
using Network.Models.Clans;

namespace MTS.Services.Clans;

public sealed partial class ClanService
{
    public ClanResponse Query(int charId, ClanQueryRequest r)
    {
        try
        {
            Require(charId > 0, "CharacterRequired");
            Require(settings.IsValid(), "InvalidClanConfiguration");
            Require(Enum.IsDefined(r.Query) && r.AfterId >= 0 && r.RequestId != Guid.Empty, "InvalidQuery");
            using var strategyDb = factory.CreateDbContext();
            return strategyDb.Database.CreateExecutionStrategy().Execute(() =>
            {
                using var db = factory.CreateDbContext();
                // Permissions and returned rows belong to one consistent snapshot.
                using var tx = db.Database.BeginTransaction(IsolationLevel.RepeatableRead);
                var result = new ClanResponse { RequestId = r.RequestId, Success = true, Code = "Ok" };
                var limit = Math.Clamp(r.Limit, 1, 50);
                var now = DateTime.UtcNow;
                if (r.Query == ClanQueryType.Payments)
                {
                    var payments = db.ClanCreationPayments.AsNoTracking()
                        .Where(x => x.CharId == charId && (r.AfterId == 0 || x.OrderId < r.AfterId))
                        .OrderByDescending(x => x.OrderId).Take(limit + 1).ToArray();
                    result.Payments = payments.Take(limit).Select(ClanPaymentService.ToDto).ToArray();
                    result.NextCursor = payments.Length > limit ? result.Payments.Last().OrderId : 0;
                    return result;
                }
                if (r.Query == ClanQueryType.Search)
                {
                    Require((r.Search?.Length ?? 0) <= 64, "InvalidSearch");
                    var text = NormalizeName(r.Search ?? "");
                    var query = db.Clans.AsNoTracking().Where(x => x.Status == ClanStatus.Active);
                    if (text.Length > 0)
                    {
                        if (int.TryParse(text, out var id)) query = query.Where(x => x.Id == id);
                        else
                        {
                            Require(text.Length >= 2, "SearchTooShort");
                            query = query.Where(x => x.NormalizedName.StartsWith(text));
                        }
                    }
                    var rows = query.Where(x => x.Id > r.AfterId).OrderBy(x => x.Id).Take(limit + 1).ToArray();
                    result.Clans = rows.Take(limit).Select(Summary).ToArray();
                    result.NextCursor = rows.Length > limit ? result.Clans.Last().Id : 0;
                    return result;
                }
                if (r.Query == ClanQueryType.MyApplications)
                {
                    var rows = db.ClanApplications.AsNoTracking().Where(x => x.CharId == charId && x.Id > r.AfterId)
                        .OrderBy(x => x.Id).Take(limit + 1).ToArray();
                    result.Applications = rows.Take(limit).Select(x => Application(x, now)).ToArray();
                    result.NextCursor = rows.Length > limit ? result.Applications.Last().Id : 0;
                    return result;
                }
                var clanId = r.Query == ClanQueryType.Mine
                    ? db.ClanMembers.Where(x => x.CharId == charId).Select(x => (int?)x.ClanId).SingleOrDefault()
                    : r.ClanId;
                if (clanId == null && r.Query == ClanQueryType.Mine) return result;
                var clan = db.Clans.AsNoTracking().SingleOrDefault(x => x.Id == clanId && x.Status == ClanStatus.Active);
                Require(clan != null, "ClanNotFound");
                var member = db.ClanMembers.AsNoTracking().SingleOrDefault(x => x.ClanId == clanId && x.CharId == charId);
                result.ClanId = clan!.Id;
                result.Version = clan.Version;
                if (r.Query is ClanQueryType.Detail or ClanQueryType.Mine)
                {
                    result.Detail = new ClanDetailDto { Id = clan.Id, Name = clan.Name, EmblemId = clan.EmblemId,
                        Level = clan.Level, MemberCount = clan.MemberCount, MaxMembers = clan.MaxMembers,
                        JoinPolicy = (int)clan.JoinPolicy, MinLevel = clan.MinLevel, Description = clan.Description,
                        LeaderCharId = clan.LeaderCharId, Version = clan.Version,
                        Announcement = member == null ? "" : clan.Announcement,
                        Funds = member == null ? 0 : clan.Funds,
                        JadeFunds = member == null ? 0 : clan.JadeFunds,
                        CastleNodeId = member == null ? null : clan.CastleNodeId,
                        ActiveInheritanceId = member?.ActiveInheritanceId,
                        DonationUnitMoneyVip = member == null ? 0 : settings.DonationUnitMoneyVip,
                        FreeDonationAvailable = member != null && settings.DonationUnitMoneyVip > 0 &&
                            !db.ClanMemberPeriodStats.Any(x => x.CharId == charId && x.ScopeId == 0 &&
                                x.PeriodId == PeriodId(now) && x.CounterType == "FreeDonation" && x.Value > 0),
                        MyRole = member == null ? -1 : (int)EffectiveRole(clan, member),
                        MyContribution = member?.TotalContribution ?? 0,
                        MyAvailableContribution = member?.AvailableContribution ?? 0 };
                    return result;
                }
                Require(member != null, "NotMember");
                switch (r.Query)
                {
                    case ClanQueryType.Inheritances:
                        var unlocked = db.ClanInheritances.Where(x => x.ClanId == clan.Id).Select(x => x.InheritanceId).ToHashSet();
                        var learned = db.ClanMemberInheritances.Where(x => x.CharId == charId && x.ClanId == clan.Id)
                            .Select(x => x.InheritanceId).ToHashSet();
                        var definitions = settings.Inheritances.Where(x => x.Id > r.AfterId).OrderBy(x => x.Id).Take(limit + 1).ToArray();
                        result.Inheritances = definitions.Take(limit).Select(x => new ClanInheritanceDto {
                            Id = x.Id, Name = x.Name, Effect = x.Effect, Element = x.Element, Value = x.Value,
                            UnlockCost = x.UnlockCost, LearnCost = x.LearnCost, MinClanLevel = x.MinClanLevel,
                            Unlocked = unlocked.Contains(x.Id), Learned = learned.Contains(x.Id) }).ToArray();
                        result.NextCursor = definitions.Length > limit ? result.Inheritances.Last().Id : 0;
                        break;
                    case ClanQueryType.Members:
                        var members = (from m in db.ClanMembers.AsNoTracking()
                            join c in db.Characters.AsNoTracking() on m.CharId equals c.Id
                            where m.ClanId == clan.Id && m.CharId > r.AfterId
                            orderby m.CharId
                            select new ClanMemberDto { CharId = m.CharId, Name = c.Name,
                                Role = m.CharId == clan.LeaderCharId ? (int)ClanRole.Leader : (int)m.Role,
                                Contribution = m.TotalContribution, JoinedAt = m.JoinedAt }).Take(limit + 1).ToArray();
                        result.Members = members.Take(limit).ToArray();
                        result.NextCursor = members.Length > limit ? result.Members.Last().CharId : 0;
                        break;
                    case ClanQueryType.Applications:
                        Permission(clan, member, ClanPermission.ReviewApplications);
                        var applications = db.ClanApplications.AsNoTracking().Where(x => x.ClanId == clan.Id &&
                            x.Status == ClanApplicationStatus.Pending && x.ExpiresAt > now && x.Id > r.AfterId)
                            .OrderBy(x => x.Id).Take(limit + 1).ToArray();
                        result.Applications = applications.Take(limit).Select(x => Application(x, now)).ToArray();
                        result.NextCursor = applications.Length > limit ? result.Applications.Last().Id : 0;
                        break;
                    case ClanQueryType.Audit:
                        Permission(clan, member, ClanPermission.ReviewApplications);
                        var logs = db.ClanAuditLogs.AsNoTracking().Where(x => x.ClanId == clan.Id && (r.AfterId == 0 || x.Id < r.AfterId))
                            .OrderByDescending(x => x.Id).Take(limit + 1).Select(x => new ClanLogDto { Id = x.Id,
                                ActorCharId = x.ActorCharId, TargetCharId = x.TargetCharId, Action = x.Action, CreatedAt = x.CreatedAt }).ToArray();
                        result.Logs = logs.Take(limit).ToArray();
                        result.NextCursor = logs.Length > limit ? result.Logs.Last().Id : 0;
                        break;
                    case ClanQueryType.Ledger:
                        var ledger = db.ClanLedgerEntries.AsNoTracking().Where(x => x.ClanId == clan.Id &&
                            (x.AccountType == ClanLedgerAccount.Funds || x.AccountType == ClanLedgerAccount.JadeFunds || x.ActorCharId == charId) && (r.AfterId == 0 || x.Id < r.AfterId))
                            .OrderByDescending(x => x.Id).Take(limit + 1).Select(x => new ClanLogDto { Id = x.Id,
                                ActorCharId = x.ActorCharId, Action = x.Reason + ":" + x.AccountType,
                                Delta = x.Delta, BalanceAfter = x.BalanceAfter, CreatedAt = x.CreatedAt }).ToArray();
                        result.Logs = ledger.Take(limit).ToArray();
                        result.NextCursor = ledger.Length > limit ? result.Logs.Last().Id : 0;
                        break;
                    default: throw new ClanRuleException("InvalidQuery");
                }
                return result;
            });
        }
        catch (ClanRuleException ex)
        {
            return new ClanResponse { RequestId = r.RequestId, Code = ex.Code };
        }
    }

    private static ClanSummaryDto Summary(Clan c) => new() { Id = c.Id, Name = c.Name, EmblemId = c.EmblemId,
        Level = c.Level, MemberCount = c.MemberCount, MaxMembers = c.MaxMembers, JoinPolicy = (int)c.JoinPolicy, MinLevel = c.MinLevel };

    private static ClanApplicationDto Application(ClanApplication a, DateTime now) => new() { Id = a.Id,
        ClanId = a.ClanId, CharId = a.CharId, Message = a.Message, ExpiresAt = a.ExpiresAt,
        Status = (int)(a.Status == ClanApplicationStatus.Pending && a.ExpiresAt <= now ? ClanApplicationStatus.Expired : a.Status) };
}
