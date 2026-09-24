using MemoryPack;
using Microsoft.EntityFrameworkCore;
using MTS.Models;
using MTS.Data;
using MTS.Game.AllMaps;
using MTS.Network;
using Newtonsoft.Json;
using Serilog;

namespace MTS.Game.Boss;

public static class BossStore
{
    public static async Task<BossProgress> LoadOrCreate(long spawnId, BossData[] bosses)
    {
        await using var db = await Program.CreateDbAsync();
        var progress = await db.BossProgresses.AsNoTracking().SingleOrDefaultAsync(x => x.Id == spawnId);
        if (progress != null && progress.NodeId > 0) return progress;
        var clanNodes = await db.Clans.Select(x => x.NodeId).ToArrayAsync();
        var candidates = GetSpawnNodeIds(GameConfig.MapManager.Maps, clanNodes);
        if (candidates.Length == 0) throw new InvalidOperationException("Không có node hợp lệ để spawn boss.");
        var nodeId = candidates[Random.Shared.Next(candidates.Length)];
        if (progress != null)
        {
            progress.NodeId = nodeId;
            db.BossProgresses.Update(progress);
            await db.SaveChangesAsync();
            return progress;
        }
        var boss = bosses[Random.Shared.Next(bosses.Length)];
        progress = new BossProgress
        {
            Id = spawnId,
            NodeId = nodeId,
            BossBytes = MemoryPackSerializer.Serialize(boss),
            Hp = boss.Character.SinhMenh,
            ParticipantRewards = MemoryPackSerializer.Serialize(BossRewards.CreateParticipantRewards(boss)),
            KillerRewards = MemoryPackSerializer.Serialize(BossRewards.CreateKillerRewards(boss))
        };
        if (progress.Hp <= 0) throw new InvalidOperationException("Boss must have positive HP.");
        db.BossProgresses.Add(progress);
        await db.SaveChangesAsync();
        return progress;
    }

    public static int[] GetSpawnNodeIds(IEnumerable<Map> maps, IEnumerable<int> clanNodes)
    {
        var occupied = clanNodes.ToHashSet();
        return maps.Where(x => x != null && x.Id > 0 && x.MapType == MapType.Normal &&
                               x is not Stronghold && !occupied.Contains(x.Id))
            .Select(x => x.Id).Distinct().ToArray();
    }

    public static void Save(BossProgress progress, int hp, Dictionary<int, int> attempts,
        Dictionary<int, int> attemptDays, Dictionary<int, long> damageTotals, int? killerId)
    {
        using var db = Program.CreateDbAsync().GetAwaiter().GetResult();
        var row = db.BossProgresses.Single(x => x.Id == progress.Id);
        if (row.KillerId.HasValue)
            throw new InvalidOperationException("Boss was already killed.");
        row.Hp = hp;
        row.AttemptsJson = JsonConvert.SerializeObject(attempts);
        row.AttemptDaysJson = JsonConvert.SerializeObject(attemptDays);
        row.DamageJson = JsonConvert.SerializeObject(damageTotals);
        row.KillerId = killerId;
        if (killerId.HasValue)
        {
            // Attempts contains everyone who participated in this spawn, including departed players.
            SendRewardMails(db, row, attempts.Keys, damageTotals, killerId.Value);
        }
        else
        {
            db.SaveChanges();
        }
    }

    private static void SendRewardMails(AppDbContext db, BossProgress progress,
        IEnumerable<int> participantIds, IReadOnlyDictionary<int, long> damageTotals, int killerId)
    {
        var mails = CreateRewardMails(progress, participantIds, damageTotals, killerId);
        db.Mails.AddRange(mails);
        // Commit the boss death and all mail together, even for offline characters.
        db.SaveChanges();

        // Online lookup is only for the notification, never for choosing recipients.
        var onlineClients = TcpServer.Instance?.GetClientsOnline() ?? [];
        foreach (var mail in mails)
        {
            try
            {
                var client = onlineClients.FirstOrDefault(x => x.CharacterData?.Id == mail.CharId);
                client?.Send(MessageTag.NewMail, mail);
            }
            catch (Exception e)
            {
                Log.Warning(e, "Boss reward mail {MailId} saved but notification failed", mail.Id);
            }
        }
    }

    public static List<Mail> CreateRewardMails(BossProgress progress, IEnumerable<int> participants,
        IReadOnlyDictionary<int, long> damageTotals, int killerId)
    {
        var boss = MemoryPackSerializer.Deserialize<BossData>(progress.BossBytes)!;
        return participants.Distinct().Select(charId =>
        {
            var rewards = MemoryPackSerializer.Deserialize<List<IMailReward>>(progress.ParticipantRewards)!;
            // Phiên tạo trước khi cập nhật phần thưởng có thể vẫn lưu danh sách rỗng.
            if (rewards.Count == 0) rewards = BossRewards.CreateParticipantRewards(boss);
            rewards.AddRange(BossRewards.CreateDamageRewards(boss, damageTotals.GetValueOrDefault(charId)));
            if (charId == killerId)
            {
                var killerRewards = MemoryPackSerializer.Deserialize<List<IMailReward>>(progress.KillerRewards)!;
                rewards.AddRange(killerRewards.Count > 0 ? killerRewards : BossRewards.CreateKillerRewards(boss));
            }
            return new Mail
            {
                CharId = charId,
                Title = charId == killerId ? "Phần thưởng tiêu diệt Boss" : "Phần thưởng tham gia Boss",
                Content = "Boss đã bị tiêu diệt. Cảm ơn đạo hữu đã tham gia chiến đấu!",
                Rewards = rewards
            };
        }).ToList();
    }

}
