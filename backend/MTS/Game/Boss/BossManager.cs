using MTS.Exceptions;
using MemoryPack;
using Newtonsoft.Json;
using Serilog;
using MTS.Game.Combat;
using MTS.Network;
using MTS.Network.Models;
using MTS.Models;

namespace MTS.Game.Boss;

public class BossManager
{
    public const int MaxAttempts = 3;
    private readonly object bossLock = new();
    private readonly Dictionary<int, IBossFighter> players = new();

    private readonly HashSet<int> readyPlayers = new();

    // Hàng đợi tấn công: người chơi được đẩy vào khi tham gia, người ở đầu hàng là người được đánh.
    private readonly LinkedList<int> attackQueue = new();

    // Kept for the entire spawn, even after a player leaves or finishes combat.
    private readonly Dictionary<int, int> attempts = new();
    private readonly Dictionary<int, long> damageTotals = new();
    private bool isBossKilled;
    private bool isClosed;
    private readonly Action<int, Dictionary<int, int>, Dictionary<int, long>, int?>? save;
    public static BossManager? Instance { get; internal set; }
    public DateTimeOffset EndAt { get; private set; } = DateTimeOffset.MaxValue;
    private bool IsClosed => isClosed || DateTimeOffset.UtcNow >= EndAt;

    public long SpawnId { get; private set; }
    public int NodeId { get; private set; }
    public string ScheduleText { get; internal set; } = string.Empty;
    public BossData BossData { get; }
    public int Id => BossData.Character.Id;
    public CharacterSnapshot Character => BossData.Character;

    public BossManager(BossData bossData, Action<int, Dictionary<int, int>, Dictionary<int, long>, int?>? save = null)
    {
        BossData = bossData.DeepClone();
        this.save = save;
    }

    public BossManager(BossProgress progress, DateTimeOffset endAt)
        : this(MemoryPackSerializer.Deserialize<BossData>(progress.BossBytes)!)
    {
        SpawnId = progress.Id;
        NodeId = progress.NodeId;
        Character.Hp = progress.Hp;
        isBossKilled = progress.KillerId.HasValue;
        EndAt = endAt;
        attempts = JsonConvert.DeserializeObject<Dictionary<int, int>>(progress.AttemptsJson) ?? new();
        damageTotals = JsonConvert.DeserializeObject<Dictionary<int, long>>(progress.DamageJson) ?? new();
        save = (hp, used, damage, killer) => BossStore.Save(progress, hp, used, damage, killer);
    }

    public BossStatus GetStatus(int characterId)
    {
        lock (bossLock)
        {
            var damage = damageTotals.GetValueOrDefault(characterId);
            var chances = BossRewards.GetFurnaceChances(damage, Character.SinhMenh);
            return new BossStatus
            {
                Success = true, SpawnId = SpawnId, NodeId = NodeId, SkinId = BossData.SkinId,
                Boss = BossData.DeepClone().Character,
                IsAlive = !IsClosed && !isBossKilled && Character.Hp > 0,
                FightingCount = readyPlayers.Count(id => players.TryGetValue(id, out var p) && p.IsConnected),
                RemainingAttempts = Math.Max(0, MaxAttempts - attempts.GetValueOrDefault(characterId)),
                DamageDone = damage,
                TamPhamChance = chances.TamPham / 100f,
                TuPhamChance = chances.TuPham / 100f,
                NguPhamChance = chances.NguPham / 100f,
                LucPhamChance = chances.LucPham / 100f,
                ScheduleText = ScheduleText
            };
        }
    }

    public void Join(IBossFighter player, JoinBossRequest request)
    {
        lock (bossLock)
        {
            if (request == null || request.SpawnId != SpawnId || request.NodeId != NodeId ||
                player.CharData.Progression.IdCurrentNode != NodeId)
                throw new InvalidException("Bạn phải tới đúng vị trí của phiên boss này.");
            if (player.CharData.Stats.Hp <= 0)
                throw new InvalidException("Khí huyết đã cạn, hãy hồi phục trước khi tham gia.");
            OnPlayerJoin(player);
        }
    }

    public BossData GetBossData()
    {
        lock (bossLock)
        {
            return BossData.DeepClone();
        }
    }

    public void Close()
    {
        lock (bossLock)
        {
            if (isClosed) return;
            isClosed = true;
            if (!isBossKilled) SendToReadyPlayers(MessageTag.BossEnded, false);
        }
    }

    public void OnPlayerJoin(IBossFighter player)
    {
        lock (bossLock)
        {
            if (IsClosed || isBossKilled || Character.Hp <= 0)
                throw new InvalidException("Boss đã bị tiêu diệt.");
            if (attempts.GetValueOrDefault(player.Id) >= MaxAttempts)
                throw new InvalidException("Bạn đã dùng hết 3 lượt đánh boss này.");
            if (IsCurrentPlayer(player)) return;
            if (!players.TryAdd(player.Id, player))
                throw new InvalidException("Nhân vật đang đánh boss.");
            attackQueue.AddLast(player.Id);
        }
    }

    public bool OnPlayerReady(IBossFighter player)
    {
        lock (bossLock)
        {
            if (!IsCurrentPlayer(player) || readyPlayers.Contains(player.Id))
                return false;
            if (IsClosed || isBossKilled)
            {
                readyPlayers.Add(player.Id);
                Send(player, MessageTag.BossEnded, isBossKilled);
                return true;
            }

            var used = attempts.GetValueOrDefault(player.Id);
            if (used >= MaxAttempts) return false;
            var nextAttempts = new Dictionary<int, int>(attempts) { [player.Id] = used + 1 };
            save?.Invoke(Character.Hp, nextAttempts, damageTotals, null);
            attempts[player.Id] = used + 1;
            readyPlayers.Add(player.Id);
            Send(player, MessageTag.BossHp, Character.Hp);
            Send(player, MessageTag.SyncStartTurn, player.Id);
            GrantFirstJoinReward(player);
            return true;
        }
    }

    private static void GrantFirstJoinReward(IBossFighter player)
    {
        try
        {
            lock (player.MainLock)
            {
                var now = DateTime.UtcNow;
                var last = player.CharData.LastDailyBossRewardAt;
                if (last != default && last.AddHours(7).Date == now.AddHours(7).Date) return;
                var reward = BossRewards.CreateFirstJoinReward();
                foreach (var item in reward.Rewards)
                {
                    item.ClaimReward(player.CharData, null).GetAwaiter().GetResult();
                }
                player.CharData.SetLastDailyBossRewardAt(now);
                Send(player, MessageTag.FirstDailyBossReward, reward);
            }
        }
        catch (Exception e)
        {
            Log.Warning(e, "Cannot grant first boss join reward to {CharId}", player.Id);
        }
    }

    public void SyncPlayer(IBossFighter player)
    {
        lock (bossLock)
        {
            if (!IsCurrentPlayer(player) || !readyPlayers.Contains(player.Id)) return;
            Send(player, MessageTag.BossHp, Character.Hp);
            if (isBossKilled || IsClosed) Send(player, MessageTag.BossEnded, isBossKilled);
        }
    }

    private bool IsCurrentPlayer(IBossFighter player)
    {
        return players.TryGetValue(player.Id, out var current) && ReferenceEquals(current, player);
    }

    public void TakeDame(IBossFighter attacker, SyncDame model)
    {
        lock (bossLock)
        {
            if (IsClosed || isBossKilled || !IsCurrentPlayer(attacker) || !readyPlayers.Contains(attacker.Id)) return;
            if (model == null || model.CharSendDame != attacker.Id || model.CharTakeDame != Id) return;
            if (model.Datas == null || model.Datas.Count is 0 or > 100) return;
            // Trust combat calculations, but reject negative or unreasonable damage packets.
            if (model.Datas.Any(x => x == null || x.Dame is < 0 or > 1_000_000 ||
                                     x.DameHoTrao is < 0 or > 1_000_000)) return;
            var damage = model.Datas.Sum(x => (long)x.Dame);
            if (damage <= 0) return;
            var effectiveDamage = Math.Min(damage, Character.Hp);
            var hp = Character.Hp - (int)effectiveDamage;
            var nextDamageTotals = new Dictionary<int, long>(damageTotals)
            {
                [attacker.Id] = damageTotals.GetValueOrDefault(attacker.Id) + effectiveDamage
            };
            save?.Invoke(hp, attempts, nextDamageTotals, hp == 0 ? attacker.Id : null);
            damageTotals[attacker.Id] = nextDamageTotals[attacker.Id];
            Character.Hp = hp;
            if (Character.Hp == 0)
            {
                isBossKilled = true;
            }

            foreach (var player in players.Values.ToArray())
            {
                if (!ReferenceEquals(player, attacker) && readyPlayers.Contains(player.Id))
                {
                    Send(player, MessageTag.SyncDame, model);
                }
            }

            if (isBossKilled) SendToReadyPlayers(MessageTag.BossEnded, true);
        }
    }

    private void SendToReadyPlayers<T>(MessageTag tag, T model)
    {
        foreach (var player in players.Values.ToArray())
        {
            if (!readyPlayers.Contains(player.Id)) continue;
            Send(player, tag, model);
        }
    }

    private static void Send<T>(IBossFighter player, MessageTag tag, T model)
    {
        try
        {
            player.MemSend(tag, model);
        }
        catch (Exception e)
        {
            Log.Warning(e, "Cannot send boss state to {CharId}", player.Id);
        }
    }

    public void SyncEndTurnBoss(IBossFighter player, SyncEndTurnBoss model)
    {
        lock (bossLock)
        {
            if (!IsCurrentPlayer(player) || !readyPlayers.Contains(player.Id) ||
                model == null || model.CharId != player.Id) return;
            var isHeadOfQueue = attackQueue.First?.Value == player.Id;
            model.IsKietSuc = model.IsKietSuc || !isHeadOfQueue || isBossKilled || IsClosed;
            player.MemSend(MessageTag.SyncEndTurnBoss, model);
        }
    }

    public ResultCombatResult OnResultCombat(IBossFighter player, ResultCombat result)
    {
        lock (bossLock)
        {
            if (!IsCurrentPlayer(player) || !readyPlayers.Contains(player.Id))
                throw new InvalidException("Bạn chưa bắt đầu lượt đánh boss.");
            lock (player.MainLock)
            {
                var stats = player.CharData.Stats;
                if (result == null || result.TypeCombat != ETypeCombat.Boss ||
                    !Enum.IsDefined(result.TypeResultCombat) || result.TypeResultCombat == ETypeResultCombat.None ||
                    result.CurrentLevelEnemy != Character.Level ||
                    result.CurrentHPUser < 0 || result.CurrentHPUser > (long)stats.SinhMenh * 5 ||
                    result.CurrentThanThuc < 0 || result.CurrentThanThuc > stats.ThanThuc ||
                    result.CurrentThoNguyen < 0 || result.CurrentThoNguyen > stats.ThoNguyen ||
                    (result.TypeResultCombat == ETypeResultCombat.Win && !isBossKilled))
                    throw new InvalidException("Kết quả đánh boss không hợp lệ.");

                stats.SetHp(Math.Clamp(result.CurrentHPUser, 0, stats.SinhMenh));
                stats.SetThanThuc(result.CurrentThanThuc);
                stats.SetThoNguyen(result.CurrentThoNguyen);
                readyPlayers.Remove(player.Id);
                players.Remove(player.Id);
                attackQueue.Remove(player.Id);
                // Do not call the normal combat reward path for world bosses.
                return new ResultCombatResult
                {
                    Success = true,
                    NewHP = stats.Hp,
                    NewTamCanh = stats.TamCanh,
                    NewNgoDaoDiem = player.CharData.Progression.NgoDaoDiem,
                    TypeResultCombat = result.TypeResultCombat
                };
            }
        }
    }

    public IReadOnlyDictionary<int, long> GetDamageTotals()
    {
        lock (bossLock) return new Dictionary<int, long>(damageTotals);
    }

    public int GetPlayerCount()
    {
        lock (bossLock) return readyPlayers.Count(id => players.TryGetValue(id, out var p) && p.IsConnected);
    }

    public void OnPlayerLeave(IBossFighter player)
    {
        lock (bossLock)
        {
            if (!IsCurrentPlayer(player)) return;
            players.Remove(player.Id);
            readyPlayers.Remove(player.Id);
            attackQueue.Remove(player.Id);
        }
    }
}
