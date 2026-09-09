using System.Data;
using System.Data.Common;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MTS.Data;
using MTS.Models;
using Newtonsoft.Json;
using Network.Models.Clans;

namespace MTS.Services.Clans;

public sealed partial class ClanService(IDbContextFactory<AppDbContext> factory, IOptions<ClanOptions> options, IClanWorldCatalog world)
{
    private readonly ClanOptions settings = options.Value;
    private static void Require(bool condition, string code)
    {
        if (!condition) throw new ClanRuleException(code);
    }

    public static string NormalizeName(string value) =>
        Regex.Replace((value ?? "").Normalize(NormalizationForm.FormC).Trim(), @"\s+", " ").ToUpperInvariant();

    public ClanResponse Execute(IClanActor actor, ClanCommandRequest request)
    {
        lock (actor.SyncRoot)
        {
            try
            {
                Require(actor.CharId > 0, "CharacterRequired");
                Require(request.RequestId != Guid.Empty, "RequestIdRequired");
                Require(Enum.IsDefined(request.Command), "InvalidCommand");
                var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(request))));
                // Capture once under CharacterLock. No mutation of live stats until commit is known.
                var walletBefore = actor.MoneyVip;
                int? walletAfter = null;
                var commitAttempted = false;
                using var strategyDb = factory.CreateDbContext();
                var strategy = strategyDb.Database.CreateExecutionStrategy();
                ClanResponse result;
                try
                {
                    result = strategy.Execute(() =>
                    {
                        using var db = factory.CreateDbContext();
                        using var transaction = db.Database.BeginTransaction(IsolationLevel.Serializable);
                        var existing = db.ClanCommandReceipts.AsNoTracking()
                            .SingleOrDefault(x => x.ActorCharId == actor.CharId && x.RequestId == request.RequestId);
                        if (existing != null) return Replay(existing, hash);
                        commitAttempted = false;
                        walletAfter = null;
                        var now = DateTime.UtcNow;
                        var clan = Apply(db, actor, request, now, walletBefore, ref walletAfter);
                        db.SaveChanges(); // obtains generated ClanId, still inside the transaction
                        var response = new ClanResponse { RequestId = request.RequestId, Success = true,
                            Code = "Ok", ClanId = clan.Id, Version = clan.Version };
                        db.ClanAuditLogs.Add(new ClanAuditLog { ClanId = clan.Id, ActorCharId = actor.CharId,
                            TargetCharId = request.TargetCharId > 0 ? request.TargetCharId : null,
                            Action = request.Command.ToString(), RequestId = request.RequestId, CreatedAt = now,
                            Data = JsonConvert.SerializeObject(new { request.Role, request.Amount, request.Approve }) });
                        db.ClanOutboxEvents.Add(new ClanOutboxEvent { EventId = Guid.NewGuid(), ClanId = clan.Id,
                            TargetCharId = request.TargetCharId > 0 ? request.TargetCharId : actor.CharId,
                            ClanVersion = clan.Version, EventType = request.Command.ToString(), CreatedAt = now, NextAttemptAt = now });
                        db.ClanCommandReceipts.Add(new ClanCommandReceipt { ActorCharId = actor.CharId,
                            RequestId = request.RequestId, CommandType = request.Command.ToString(), PayloadHash = hash,
                            ResultPayload = JsonConvert.SerializeObject(response), CreatedAt = now });
                        db.SaveChanges();
                        commitAttempted = true;
                        transaction.Commit();
                        return response;
                    });
                }
                catch (Exception ex) when (ex is not ClanRuleException)
                {
                    // A connection may fail after COMMIT reached MySQL. Resolve using the durable receipt.
                    var receipt = ResolveReceipt(actor.CharId, request.RequestId, commitAttempted);
                    if (receipt != null) result = Replay(receipt, hash);
                    else if (ex is DbUpdateException) throw new ClanRuleException("ConflictRetry");
                    else throw;
                }
                if (walletAfter.HasValue) actor.MoneyVip = walletAfter.Value;
                // A replay returns today's wallet, never an old balance from the saved receipt.
                result.MoneyVip = actor.MoneyVip;
                return result;
            }
            catch (ClanRuleException ex)
            {
                return new ClanResponse { RequestId = request.RequestId, Code = ex.Code };
            }
        }
    }

    private ClanCommandReceipt? ResolveReceipt(int charId, Guid requestId, bool commitAttempted)
    {
        while (true)
        {
            try
            {
                using var verify = factory.CreateDbContext();
                return verify.Database.CreateExecutionStrategy().Execute(() =>
                {
                    // A locking read waits for an in-flight INSERT/COMMIT of this receipt.
                    using var tx = verify.Database.BeginTransaction(IsolationLevel.Serializable);
                    var receipt = verify.ClanCommandReceipts.AsNoTracking()
                        .SingleOrDefault(x => x.ActorCharId == charId && x.RequestId == requestId);
                    tx.Commit();
                    return receipt;
                });
            }
            catch (Exception ex) when (commitAttempted && ex is DbException or TimeoutException or Microsoft.EntityFrameworkCore.Storage.RetryLimitExceededException)
            {
                // Do not release CharacterLock to autosave with an unresolved money transaction.
                // This character waits for DB recovery; unrelated characters remain independent.
                Serilog.Log.Warning(ex, "Waiting to resolve Clan commit for {CharId}/{RequestId}", charId, requestId);
                Thread.Sleep(TimeSpan.FromSeconds(5));
            }
        }
    }

    private static ClanResponse Replay(ClanCommandReceipt receipt, string hash)
    {
        Require(receipt.PayloadHash == hash, "RequestIdReused");
        return JsonConvert.DeserializeObject<ClanResponse>(receipt.ResultPayload)!;
    }

    private Clan Apply(AppDbContext db, IClanActor actor, ClanCommandRequest r, DateTime now,
        int walletBefore, ref int? walletAfter)
    {
        if (r.Command == ClanCommandType.Create)
        {
            Require(world.Contains(r.CastleNodeId), "InvalidCastleNode");
            Require(r.CreationPaymentMethod is 0 or 1, "InvalidPaymentMethod");
            CanJoin(db, actor.CharId, now);
            var name = Regex.Replace((r.Name ?? "").Normalize(NormalizationForm.FormC).Trim(), @"\s+", " ");
            Require(name.Length is >= 2 and <= 64 && !name.Any(char.IsControl), "InvalidName");
            var normalized = NormalizeName(name);
            Require(!db.Clans.Any(x => x.NormalizedName == normalized), "NameTaken");
            ValidateSettings(r);
            var created = new Clan { CastleNodeId = r.CastleNodeId, Name = name, NormalizedName = normalized, EmblemId = r.EmblemId,
                Description = r.Description, Announcement = r.Announcement, LeaderCharId = actor.CharId,
                MaxMembers = settings.InitialCapacity, MemberCount = 1, JoinPolicy = (ClanJoinPolicy)r.JoinPolicy,
                MinLevel = r.MinLevel, CreatedAt = now, UpdatedAt = now };
            db.Clans.Add(created);
            db.SaveChanges();
            db.ClanMembers.Add(new ClanMember { CharId = actor.CharId, ClanId = created.Id, JoinedAt = now });
            CancelOtherApplications(db, actor.CharId, now);
            if (r.CreationPaymentMethod == 0)
            {
                Require(settings.CreationMoneyVipCost > 0, "CreationPriceNotConfigured");
                Debit(db, actor.CharId, walletBefore, settings.CreationMoneyVipCost, ref walletAfter);
                Ledger(db, created.Id, actor.CharId, r.RequestId, ClanLedgerAccount.CharacterMoneyVip,
                    -settings.CreationMoneyVipCost, walletAfter!.Value, "Create", now);
            }
            else
            {
                var payment = db.ClanCreationPayments.SingleOrDefault(x => x.OrderId == r.PaymentOrderId && x.CharId == actor.CharId);
                Require(payment?.Status == ClanCreationPaymentStatus.Paid, "PaidCreationRequired");
                payment!.Status = ClanCreationPaymentStatus.Consumed;
                payment.ConsumedByClanId = created.Id;
                payment.Version++;
            }
            return created;
        }

        var clan = db.Clans.SingleOrDefault(x => x.Id == r.ClanId);
        Require(clan is { Status: ClanStatus.Active }, "ClanNotFound");
        Require(r.ExpectedVersion <= 0 || r.ExpectedVersion == clan!.Version, "StaleVersion");
        var member = db.ClanMembers.SingleOrDefault(x => x.CharId == actor.CharId && x.ClanId == r.ClanId);
        switch (r.Command)
        {
            case ClanCommandType.Apply:
                CanJoin(db, actor.CharId, now);
                Require(actor.Level >= clan!.MinLevel, "LevelTooLow");
                Require(clan.JoinPolicy != ClanJoinPolicy.Closed, "RecruitmentClosed");
                Require(clan.MemberCount < clan.MaxMembers, "ClanFull");
                if (clan.JoinPolicy == ClanJoinPolicy.Open)
                {
                    AddMember(db, clan, actor.CharId, now);
                    break;
                }
                Require((r.Message?.Length ?? 0) <= 256, "MessageTooLong");
                var application = db.ClanApplications.SingleOrDefault(x => x.ClanId == clan.Id && x.CharId == actor.CharId);
                Require(application == null || application.Status != ClanApplicationStatus.Pending || application.ExpiresAt <= now,
                    "AlreadyApplied");
                Require(db.ClanApplications.Count(x => x.CharId == actor.CharId && x.Status == ClanApplicationStatus.Pending && x.ExpiresAt > now)
                    < settings.MaxPendingApplications, "TooManyApplications");
                if (application == null)
                {
                    application = new ClanApplication { ClanId = clan.Id, CharId = actor.CharId };
                    db.ClanApplications.Add(application);
                }
                application.Message = r.Message ?? "";
                application.Status = ClanApplicationStatus.Pending;
                application.CreatedAt = now;
                application.ExpiresAt = now.AddHours(settings.ApplicationLifetimeHours);
                application.ReviewedByCharId = null;
                application.ReviewedAt = null;
                application.Version++;
                break;
            case ClanCommandType.CancelApplication:
                var cancel = db.ClanApplications.SingleOrDefault(x => x.ClanId == clan!.Id && x.CharId == actor.CharId);
                Require(cancel is { Status: ClanApplicationStatus.Pending }, "ApplicationNotFound");
                cancel!.Status = ClanApplicationStatus.Cancelled;
                cancel.ReviewedAt = now;
                cancel.Version++;
                break;
            case ClanCommandType.ReviewApplication:
                Permission(clan!, member, ClanPermission.ReviewApplications);
                var pending = db.ClanApplications.SingleOrDefault(x => x.ClanId == clan!.Id && x.CharId == r.TargetCharId);
                Require(pending is { Status: ClanApplicationStatus.Pending } && pending.ExpiresAt > now, "ApplicationExpired");
                if (r.Approve)
                {
                    CanJoin(db, r.TargetCharId, now);
                    Require(clan!.JoinPolicy != ClanJoinPolicy.Closed, "RecruitmentClosed");
                    Require(db.Progressions.Any(x => x.CharId == r.TargetCharId && x.Level >= clan.MinLevel), "LevelTooLow");
                    AddMember(db, clan, r.TargetCharId, now);
                }
                pending!.Status = r.Approve ? ClanApplicationStatus.Approved : ClanApplicationStatus.Rejected;
                pending.ReviewedByCharId = actor.CharId;
                pending.ReviewedAt = now;
                pending.Version++;
                break;
            case ClanCommandType.Leave:
                Require(member != null, "NotMember");
                Require(clan!.LeaderCharId != actor.CharId, "TransferLeadershipFirst");
                RemoveMember(db, clan, member!, ClanLeaveReason.Leave, now);
                break;
            case ClanCommandType.Kick:
                Permission(clan!, member, ClanPermission.KickMembers);
                var kicked = TargetMember(db, clan!, r.TargetCharId);
                Require(r.TargetCharId != actor.CharId && clan!.LeaderCharId != r.TargetCharId &&
                    EffectiveRole(clan, member!) > kicked.Role, "RoleHierarchy");
                RemoveMember(db, clan!, kicked, ClanLeaveReason.Kick, now);
                break;
            case ClanCommandType.ChangeRole:
                Permission(clan!, member, ClanPermission.ChangeRoles);
                Require(r.Role >= 0 && r.Role <= (int)ClanRole.Deputy, "InvalidRole");
                var target = TargetMember(db, clan!, r.TargetCharId);
                Require(target.CharId != clan!.LeaderCharId, "UseTransferLeadership");
                target.Role = (ClanRole)r.Role;
                target.Version++;
                break;
            case ClanCommandType.TransferLeadership:
                Permission(clan!, member, ClanPermission.TransferLeadership);
                Require(r.TargetCharId != actor.CharId, "InvalidTarget");
                var leader = TargetMember(db, clan!, r.TargetCharId);
                member!.Role = ClanRole.Deputy;
                member.Version++;
                leader.Role = ClanRole.Member;
                leader.Version++;
                clan!.LeaderCharId = leader.CharId;
                break;
            case ClanCommandType.UpdateSettings:
                Permission(clan!, member, ClanPermission.EditSettings);
                ValidateSettings(r);
                clan!.Description = r.Description;
                clan.Announcement = r.Announcement;
                clan.EmblemId = r.EmblemId;
                clan.JoinPolicy = (ClanJoinPolicy)r.JoinPolicy;
                clan.MinLevel = r.MinLevel;
                break;
            case ClanCommandType.Donate:
                Require(member != null, "NotMember");
                Require(r.Amount == 0 && r.DonationCount is 1 or 10, "InvalidDonationCount");
                Require(!r.FreeDonation || r.DonationCount == 1, "InvalidDonationCount");
                Require(settings.DonationUnitMoneyVip > 0, "DonationPriceNotConfigured");
                var periodId = PeriodId(now);
                var counterName = r.FreeDonation ? "FreeDonation" : "PaidDonationCount";
                var counter = db.ClanMemberPeriodStats.SingleOrDefault(x => x.CharId == actor.CharId &&
                    x.ScopeId == 0 && x.PeriodId == periodId && x.CounterType == counterName);
                if (counter == null)
                {
                    counter = new ClanMemberPeriodStats { CharId = actor.CharId, PeriodId = periodId, CounterType = counterName };
                    db.ClanMemberPeriodStats.Add(counter);
                }
                Require(!r.FreeDonation || counter.Value == 0, "FreeDonationUsed");
                var donated = checked(settings.DonationUnitMoneyVip * r.DonationCount);
                if (!r.FreeDonation)
                {
                    Debit(db, actor.CharId, walletBefore, donated, ref walletAfter);
                    Ledger(db, clan!.Id, actor.CharId, r.RequestId, ClanLedgerAccount.CharacterMoneyVip, -donated, walletAfter!.Value, "Donate", now);
                }
                counter.Value = checked(counter.Value + r.DonationCount);
                counter.Version++;
                clan!.JadeFunds = checked(clan.JadeFunds + donated);
                member!.AvailableContribution = checked(member.AvailableContribution + donated);
                member.TotalContribution = checked(member.TotalContribution + donated);
                member.Version++;
                Ledger(db, clan.Id, actor.CharId, r.RequestId, ClanLedgerAccount.JadeFunds, donated, clan.JadeFunds, "Donate", now);
                Ledger(db, clan.Id, actor.CharId, r.RequestId, ClanLedgerAccount.Contribution, donated, member.AvailableContribution, "Donate", now);
                break;
            case ClanCommandType.UnlockInheritance:
                Permission(clan!, member, ClanPermission.EditSettings);
                var unlock = Inheritance(r.InheritanceId);
                Require(clan!.Level >= unlock.MinClanLevel, "ClanLevelTooLow");
                Require(!db.ClanInheritances.Any(x => x.ClanId == clan.Id && x.InheritanceId == unlock.Id), "InheritanceAlreadyUnlocked");
                Require(clan.JadeFunds >= unlock.UnlockCost, "InsufficientClanFunds");
                clan.JadeFunds -= unlock.UnlockCost;
                db.ClanInheritances.Add(new ClanInheritance { ClanId = clan.Id, InheritanceId = unlock.Id, UnlockedByCharId = actor.CharId, UnlockedAt = now });
                Ledger(db, clan.Id, actor.CharId, r.RequestId, ClanLedgerAccount.JadeFunds, -unlock.UnlockCost, clan.JadeFunds, "UnlockInheritance", now);
                break;
            case ClanCommandType.LearnInheritance:
                Require(member != null, "NotMember");
                var learn = Inheritance(r.InheritanceId);
                Require(db.ClanInheritances.Any(x => x.ClanId == clan!.Id && x.InheritanceId == learn.Id), "InheritanceLocked");
                Require(!db.ClanMemberInheritances.Any(x => x.CharId == actor.CharId && x.ClanId == clan!.Id && x.InheritanceId == learn.Id), "InheritanceAlreadyLearned");
                Require(member!.AvailableContribution >= learn.LearnCost, "InsufficientContribution");
                member.AvailableContribution -= learn.LearnCost;
                member.ActiveInheritanceId = learn.Id;
                member.Version++;
                db.ClanMemberInheritances.Add(new ClanMemberInheritance { CharId = actor.CharId, ClanId = clan!.Id, InheritanceId = learn.Id, LearnedAt = now });
                Ledger(db, clan!.Id, actor.CharId, r.RequestId, ClanLedgerAccount.Contribution, -learn.LearnCost, member.AvailableContribution, "LearnInheritance", now);
                break;
            case ClanCommandType.SelectInheritance:
                Require(member != null, "NotMember");
                if (r.InheritanceId != 0)
                {
                    Inheritance(r.InheritanceId);
                    Require(db.ClanMemberInheritances.Any(x => x.CharId == actor.CharId && x.ClanId == clan!.Id && x.InheritanceId == r.InheritanceId), "InheritanceNotLearned");
                }
                member!.ActiveInheritanceId = r.InheritanceId == 0 ? null : r.InheritanceId;
                member.Version++;
                break;
            case ClanCommandType.Dissolve:
                Permission(clan!, member, ClanPermission.Dissolve);
                Require(clan!.MemberCount == 1, "MembersMustLeaveFirst");
                Require(clan.Funds == 0 && clan.JadeFunds == 0, "ClanHasFunds");
                RemoveMember(db, clan, member!, ClanLeaveReason.Dissolved, now);
                clan.Status = ClanStatus.Dissolved;
                clan.DissolvedAt = now;
                foreach (var item in db.ClanApplications.Where(x => x.ClanId == clan.Id && x.Status == ClanApplicationStatus.Pending))
                {
                    item.Status = ClanApplicationStatus.Cancelled;
                    item.ReviewedAt = now;
                    item.Version++;
                }
                break;
            default: throw new ClanRuleException("InvalidCommand");
        }
        clan!.Version++;
        clan.UpdatedAt = now;
        return clan;
    }

    private int PeriodId(DateTime now)
    {
        var day = now.AddHours(settings.CalendarUtcOffsetHours);
        return day.Year * 10000 + day.Month * 100 + day.Day;
    }
    private ClanInheritanceDefinition Inheritance(int id) => settings.Inheritances.FirstOrDefault(x => x.Id == id)
        ?? throw new ClanRuleException("InheritanceNotFound");

    private static void Debit(AppDbContext db, int charId, int currentBalance, int amount, ref int? after)
    {
        Require(currentBalance >= amount, "InsufficientFunds");
        var balance = currentBalance - amount;
        // Only this column is written. Live RAM is authoritative in the current single-session server.
        Require(db.Characters.Where(x => x.Id == charId)
            .ExecuteUpdate(s => s.SetProperty(x => x.MoneyVip, balance)) == 1, "CharacterRequired");
        after = balance;
    }

    private static void Ledger(AppDbContext db, int clanId, int actorId, Guid requestId, ClanLedgerAccount account,
        long delta, long balance, string reason, DateTime now) => db.ClanLedgerEntries.Add(new ClanLedgerEntry {
            ClanId = clanId, ActorCharId = actorId, TransactionId = requestId, RequestId = requestId,
            AccountType = account, Delta = delta, BalanceAfter = balance, Reason = reason, CreatedAt = now });

    private static void ValidateSettings(ClanCommandRequest r)
    {
        Require(r.Description != null && r.Description.Length <= 512 && r.Announcement != null && r.Announcement.Length <= 1024,
            "TextTooLong");
        Require(r.EmblemId >= 0 && r.MinLevel >= 0 && r.MinLevel <= 10000 && r.JoinPolicy is >= 0 and <= 2, "InvalidSettings");
    }

    private static ClanRole EffectiveRole(Clan clan, ClanMember member) =>
        clan.LeaderCharId == member.CharId ? ClanRole.Leader : member.Role;

    private static void Permission(Clan clan, ClanMember? member, ClanPermission permission)
    {
        Require(member != null, "NotMember");
        Require((ClanRolePermissions.GetPermissions(EffectiveRole(clan, member!)) & permission) == permission, "Forbidden");
    }

    private static ClanMember TargetMember(AppDbContext db, Clan clan, int charId) =>
        db.ClanMembers.SingleOrDefault(x => x.ClanId == clan.Id && x.CharId == charId) ?? throw new ClanRuleException("TargetNotMember");

    private static void CanJoin(AppDbContext db, int charId, DateTime now)
    {
        Require(!db.ClanMembers.Any(x => x.CharId == charId), "AlreadyMember");
        var state = db.ClanMembershipStates.SingleOrDefault(x => x.CharId == charId);
        Require(state?.NextJoinAt == null || state.NextJoinAt <= now, "JoinCooldown");
        if (state == null) db.ClanMembershipStates.Add(new ClanMembershipState { CharId = charId });
        else state.Version++;
    }

    private static void AddMember(AppDbContext db, Clan clan, int charId, DateTime now)
    {
        Require(clan.MemberCount < clan.MaxMembers, "ClanFull");
        db.ClanMembers.Add(new ClanMember { ClanId = clan.Id, CharId = charId, JoinedAt = now });
        clan.MemberCount++;
        CancelOtherApplications(db, charId, now);
    }

    private static void CancelOtherApplications(AppDbContext db, int charId, DateTime now)
    {
        foreach (var application in db.ClanApplications.Where(x => x.CharId == charId && x.Status == ClanApplicationStatus.Pending))
        {
            application.Status = ClanApplicationStatus.Cancelled;
            application.ReviewedAt = now;
            application.Version++;
        }
    }

    private void RemoveMember(AppDbContext db, Clan clan, ClanMember member, ClanLeaveReason reason, DateTime now)
    {
        db.ClanMembers.Remove(member);
        clan.MemberCount--;
        var state = db.ClanMembershipStates.SingleOrDefault(x => x.CharId == member.CharId);
        if (state == null)
        {
            state = new ClanMembershipState { CharId = member.CharId };
            db.ClanMembershipStates.Add(state);
        }
        state.LastClanId = clan.Id;
        state.LeftAt = now;
        state.NextJoinAt = now.AddHours(settings.JoinCooldownHours);
        state.LeaveReason = reason;
        state.Version++;
    }
}
