using Microsoft.EntityFrameworkCore;
using MTS.Exceptions;
using MTS.Network;
using MTS.Network.Models;

namespace MTS.Game.Clan;

public static partial class ClanManager
{
    public static async Task<ClanMemberRoleChanged> SetMemberRole(
        ClanReference reference,
        int actorCharId,
        int targetCharId,
        Models.ClanRole role)
    {
        if (targetCharId <= 0 || !Enum.IsDefined(role))
            throw new InvalidException("Thành viên hoặc chức vụ không hợp lệ");

        var clan = reference.Clan;
        var clanId = clan.Id;
        var gate = AcquireGate(clanId);
        await gate.Semaphore.WaitAsync();
        try
        {
            var actor = clan.Members.FirstOrDefault(member => member.CharId == actorCharId)
                ?? throw new InvalidException("Chưa gia nhập tông môn");
            if (!actor.HasPermission(Models.ClanPermission.AssignRole))
                throw new InvalidException("Không có quyền cấp chức vụ");

            var target = clan.Members.FirstOrDefault(member => member.CharId == targetCharId)
                ?? throw new InvalidException("Thành viên không tồn tại");
            if (target.CharId == actor.CharId)
                throw new InvalidException("Không thể tự thay đổi chức vụ của mình");
            if (target.Role >= actor.Role)
                throw new InvalidException("Không thể thay đổi chức vụ của thành viên ngang hoặc cao hơn");
            if (role >= actor.Role)
                throw new InvalidException("Chỉ được cấp chức vụ thấp hơn chức vụ của bạn");
            if (target.Role == role)
                throw new InvalidException("Thành viên đã có chức vụ này");
            ValidateUniqueRole(clan, role, targetCharId);
            var now = DateTime.UtcNow;
            DateTime? lastRoleAssignedAt = role switch
            {
                Models.ClanRole.ViceLeader => clan.LastViceLeaderAssignedAt,
                Models.ClanRole.Elder => clan.LastElderAssignedAt,
                _ => null
            };
            if (lastRoleAssignedAt != null)
            {
                if (Config.RoleAssignCooldownHours < 0)
                    throw new InvalidException("Cấu hình cooldown phong chức không hợp lệ");

                var readyAt = lastRoleAssignedAt.Value.AddHours(Config.RoleAssignCooldownHours);
                if (now < readyAt)
                {
                    var remainingHours = Math.Ceiling((readyAt - now).TotalHours);
                    throw new InvalidException(
                        $"Chức vụ {GetRoleName(role)} vừa được phong gần đây, cần chờ thêm {remainingHours} giờ nữa mới được phong lại");
                }
            }

            var oldRole = target.Role;
            await using var db = await Program.CreateDbAsync();
            if (IsUniqueRole(role) && await db.ClanMembers.AnyAsync(member =>
                    member.ClanId == clanId && member.Role == role && member.CharId != targetCharId))
                throw new InvalidException($"Tông môn chỉ được có 1 {GetRoleName(role)}");

            var affected = await db.ClanMembers
                .Where(member => member.ClanId == clanId && member.CharId == targetCharId && member.Role == oldRole)
                .ExecuteUpdateAsync(update => update.SetProperty(member => member.Role, role));
            if (affected != 1 && !await db.ClanMembers.AsNoTracking().AnyAsync(member =>
                    member.ClanId == clanId && member.CharId == targetCharId && member.Role == role))
                throw new InvalidException("Dữ liệu thành viên đã thay đổi, vui lòng thử lại");

            if (role == Models.ClanRole.ViceLeader)
            {
                await db.Clans.Where(x => x.Id == clanId)
                    .ExecuteUpdateAsync(update => update.SetProperty(c => c.LastViceLeaderAssignedAt, now));
                clan.LastViceLeaderAssignedAt = now;
            }
            else if (role == Models.ClanRole.Elder)
            {
                await db.Clans.Where(x => x.Id == clanId)
                    .ExecuteUpdateAsync(update => update.SetProperty(c => c.LastElderAssignedAt, now));
                clan.LastElderAssignedAt = now;
            }

            target.Role = role;
            var change = new ClanMemberRoleChanged
            {
                ClanId = clanId,
                CharId = targetCharId,
                Role = role
            };
            if (Observers.TryGetValue(clanId, out var observer))
                observer.SendAll(MessageTag.ClanMemberRoleChanged, change);

            return change;
        }
        finally
        {
            ReleaseGate(clanId, gate);
        }
    }
}
