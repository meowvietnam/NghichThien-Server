using Microsoft.EntityFrameworkCore;
using MTS.Exceptions;
using MTS.Models;
using MTS.Network;
using MTS.Network.Models;
using Serilog;

namespace MTS.Game.Clan;

public static partial class ClanManager
{
    public static async Task<ClanJoinRequest> RequestJoinClan(int clanId, CharacterData charData)
    {
        var charId = charData.Id;
        if (MemberClanIds.ContainsKey(charId))
        {
            throw new InvalidException("Đã gia nhập tông môn");
        }

        var reference = await FindReference(clanId);
        var clan = reference.Clan;
        var gate = AcquireGate(clanId);
        await gate.Semaphore.WaitAsync();
        try
        {
            if (MemberClanIds.ContainsKey(charId) || clan.Members.Any(x => x.CharId == charId))
            {
                throw new InvalidException("Đã gia nhập tông môn");
            }

            if (clan.MemberCount >= clan.MaxMembers)
            {
                throw new InvalidException("Tông môn đã đầy");
            }

            if (clan.JoinRequests.Any(x => x.CharId == charId))
            {
                throw new InvalidException("Đã gửi đơn xin gia nhập tông môn này");
            }

            var request = new ClanJoinRequest
            {
                ClanId = clanId,
                CharId = charId
            };

            await using var db = await Program.CreateDbAsync();
            await db.ClanJoinRequests.AddAsync(request);
            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                await using var verifyDb = await Program.CreateDbAsync();
                if (!await verifyDb.ClanJoinRequests.AsNoTracking()
                        .AnyAsync(x => x.ClanId == clanId && x.CharId == charId))
                    throw;
            }

            request.CharData = charData;
            clan.JoinRequests.Add(request);
            NotifyClanJoinRequestAdded(clanId, request);

            return request;
        }
        finally
        {
            ReleaseGate(clanId, gate);
        }
    }

    // Caller must verify the actor has ClanPermission.AddMember.
    public static async Task<ClanMember> ApproveJoinRequest(ClanReference reference, int charId)
    {
        var member = await AddMember(reference, charId);
        NotifyClanJoinRequestRemoved(reference.Clan.Id, charId);
        return member;
    }

    // Caller must verify the actor has ClanPermission.AddMember.
    public static async Task<int> RejectJoinRequest(ClanReference reference, int charId)
    {
        if (charId <= 0)
        {
            throw new InvalidException("Thành viên không hợp lệ");
        }

        var clan = reference.Clan;
        var clanId = clan.Id;
        var gate = AcquireGate(clanId);
        await gate.Semaphore.WaitAsync();
        try
        {
            if (!clan.JoinRequests.Any(x => x.CharId == charId))
            {
                throw new InvalidException("Không tìm thấy đơn xin gia nhập");
            }

            await using var db = await Program.CreateDbAsync();
            var deleted = await db.ClanJoinRequests
                .Where(x => x.ClanId == clanId && x.CharId == charId)
                .ExecuteDeleteAsync();
            if (deleted == 0 && await db.ClanJoinRequests.AsNoTracking()
                    .AnyAsync(x => x.ClanId == clanId && x.CharId == charId))
            {
                throw new InvalidException("Không tìm thấy đơn xin gia nhập");
            }

            clan.JoinRequests.RemoveAll(x => x.CharId == charId);
            NotifyClanJoinRequestRemoved(clanId, charId);
            return charId;
        }
        finally
        {
            ReleaseGate(clanId, gate);
        }
    }

    private static void NotifyClanJoinRequestAdded(int clanId, ClanJoinRequest request)
    {
        if (!Observers.TryGetValue(clanId, out var observer)) return;
        try
        {
            observer.SendAll(MessageTag.ClanJoinRequestAdded, new ClanJoinRequestAdded { Request = request });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to notify clan join request added for {ClanId}/{CharId}", clanId, request.CharId);
        }
    }

    private static void NotifyClanJoinRequestRemoved(int clanId, int charId)
    {
        if (!Observers.TryGetValue(clanId, out var observer)) return;
        try
        {
            observer.SendAll(MessageTag.ClanJoinRequestRemoved, new ClanJoinRequestRemoved
            {
                ClanId = clanId,
                CharId = charId
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to notify clan join request removed for {ClanId}/{CharId}", clanId, charId);
        }
    }
}
