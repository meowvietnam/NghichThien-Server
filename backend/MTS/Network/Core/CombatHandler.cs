using MTS.Exceptions;
using Microsoft.EntityFrameworkCore;
using MTS.Game;
using MTS.Game.Combat;
using MTS.Localization;
using MTS.Models;
using MTS.Network.Models;
using MTS.Services;
using Network;
using Network.Extensions;
using Serilog;

namespace MTS.Network.Core
{
    public class CombatHandler : IClientHandler
    {
        private Client client;

        private static MessageTag[] handler =
        [
            MessageTag.ResultCombat, MessageTag.LoadCharCombat,
            MessageTag.LoadCharCombatArena, MessageTag.LoadRankRequest,
            MessageTag.ClaimRankRewardRequest,
            MessageTag.WatchAdsCombat
        ];

        private readonly object combatLock = new();
        private CombatSession? combatSession;

        public CombatHandler(Client client)
        {
            this.client = client;
            client.OnLogout += OnLogout;
            client.OnOtherClientLogin += OnOtherClientLogin;
            client.OnReconnectedSuccess += OnReconnectedSuccess;
            client.OnRemoveDisconnectedSession += OnRemoveDisconnectedSession;
        }


        public MessageTag[] HandleTag => handler;

        private void EnsureInCombat()
        {
            if (combatSession == null)
            {
                throw new InvalidException(Strings.CombatSessionNotFound);
            }

            if (combatSession.Status != CombatStatus.InCombat)
            {
                throw new InvalidException(Strings.CombatSessionInvalid);
            }
        }

        public void Handle(Message<MessageTag> message)
        {
            var tag = message.Tag;
            var reader = message.Reader;
            if (tag == MessageTag.ResultCombat)
            {
                var request = reader.Read<ResultCombat>();
                ResultCombatResult result = null;
                try
                {
                    EnsureInCombat();
                    lock (client.CharacterLock)
                    {
                        var charData = client.CharacterData;
                        result = charData.ResultCombat(request);
                        EndCombat(result.Reward);
                    }
                }
                catch (InvalidException e)
                {
                    result = new ResultCombatResult()
                    {
                        Success = false,
                        Message = e.Message
                    };
                }
                finally
                {
                    client.Send(MessageTag.ResultCombatResult, result);
                }
            }

            else if (tag == MessageTag.LoadCharCombat)
            {
                LoadCharCombatResult result = null;
                try
                {
                    lock (client.CharacterLock)
                    {
                        var charData = client.CharacterData;
                        EnsureNoCombatSession();
                        result = new LoadCharCombatResult()
                        {
                            Success = true,
                            Client = new CharacterSnapshot(charData)
                        };
                        charData.EquipmentManager.ClearDanCombat();
                        StartCombat(ETypeCombat.AllMaps, charData.Progression.Level);
                    }
                }
                catch (InvalidException e)
                {
                    result = new LoadCharCombatResult()
                    {
                        Success = false,
                        Message = e.Message
                    };
                }
                finally
                {
                    client.Send(MessageTag.LoadCharCombatResult, result);
                }
            }
            else if (tag == MessageTag.LoadCharCombatArena)
            {
                try
                {
                    lock (client.CharacterLock)
                    {
                        var charData = client.CharacterData;
                        EnsureNoCombatSession();
                        var (state, _) = GetMatchMakingState(charData);
                        if (state == MatchMakingState.Ads && !charData.IsNoAds)
                        {
                            var adsHandler = client.GetHandler<AdsHandler>();
                            ShowAds ads = new ArenaAds("LoadCharCombatArena", "Xem quảng cáo để tham gia đấu trường",
                                client);
                            adsHandler.SendAds(ads);
                            return;
                        }

                        SendLoadCharCombatArena();
                    }
                }
                catch (InvalidException e)
                {
                    var result = new LoadCharCombatResult()
                    {
                        Success = false,
                        Message = e.Message
                    };
                    client.Send(MessageTag.LoadCharCombatArenaResult, result);
                }
            }
            else if (tag == MessageTag.LoadRankRequest)
            {
                var result = LoadRank();
                client.MemSend(MessageTag.LoadRankRequest, result);
            }
            else if (tag == MessageTag.ClaimRankRewardRequest)
            {
                var id = reader.Read<long>();
                ClaimRankRewardResult result = null;
                try
                {
                    lock (client.CharacterLock)
                    {
                        var charData = client.CharacterData;
                        var moneyVip = charData.ArenaManager.Claim(id).GetAwaiter().GetResult();
                        result = new ClaimRankRewardResult()
                        {
                            Success = true,
                            Id = id,
                            MoneyVip = moneyVip,
                        };
                    }
                }
                catch (InvalidException e)
                {
                    result = new ClaimRankRewardResult()
                    {
                        Success = false,
                        Message = e.Message
                    };
                }
                finally
                {
                    client.MemSend(MessageTag.ClaimRankRewardRequest, result);
                }
            }
            else if (tag == MessageTag.WatchAdsCombat)
            {
                try
                {
                    lock (client.CharacterLock)
                    {
                        if (combatSession == null)
                        {
                            throw new InvalidException(Strings.CombatSessionNotFound);
                        }

                        if (!combatSession.CanClaim())
                        {
                            throw new InvalidException(Strings.CombatAdRewardNotAvailable);
                        }

                        if (client.CharacterData.IsNoAds)
                        {
                            ClaimCombatAds();
                        }
                        else
                        {
                            var adsHandler = client.GetHandler<AdsHandler>();
                            var ads = new RewardCombatAds("RewardCombatAds", "Xem quảng cáo để nhận thưởng x2", client);
                            adsHandler.SendAds(ads);
                        }
                    }
                }
                catch (InvalidException e)
                {
                    client.MemSend(MessageTag.WatchAdsCombatResult, new WatchAdsCombatResult()
                    {
                        Success = false,
                        Message = e.Message
                    });
                }
            }
        }

        public void ClaimCombatAds()
        {
            combatSession.HasClaimedAdReward = true;
            var reward = combatSession.RewardSnapshot;
            var charData = client.CharacterData;
            charData.Claim(reward);
            client.MemSend(MessageTag.WatchAdsCombatResult, new WatchAdsCombatResult()
            {
                Success = true,
                Reward = reward,
            });
        }

        public void SendLoadCharCombatArena()
        {
            LoadCharCombatResult result = null;
            try
            {
                lock (client.CharacterLock)
                {
                    var charData = client.CharacterData;
                    EnsureNoCombatSession();
                    var (state, cost) = GetMatchMakingState(charData);
                    if (state == MatchMakingState.NotFree)
                    {
                        var remainingMoneyVip = charData.MoneyVip - cost;
                        if (remainingMoneyVip < 0)
                        {
                            throw new InvalidException(Strings.NotEnoughMoneyVip);
                        }

                        charData.SetMoneyVip(remainingMoneyVip);
                        charData.AddSpendMoneyVipOnline(cost, SpendFor.Arena);
                    }

                    var enemyData = LoadEnemyCombat().GetAwaiter().GetResult();
                    var enemy = enemyData == null ? null : new CharacterSnapshot(enemyData);
                    var arena = charData.ArenaManager;
                    arena.SetCurrentArenaCount(arena.CurrentArenaCount + 1);
                    result = new LoadCharCombatResult()
                    {
                        Success = true,
                        Client = new CharacterSnapshot(charData),
                        Enemy = enemy,
                        CurrentArenaCount = arena.CurrentArenaCount,
                        NewMoneyVip = charData.MoneyVip,
                    };
                    charData.EquipmentManager.ClearDanCombat();
                    var level = enemy != null ? enemy.Level : charData.Progression.Level;
                    StartCombat(ETypeCombat.DauTruong, level);
                }
            }
            catch (InvalidException e)
            {
                result = new LoadCharCombatResult()
                {
                    Success = false,
                    Message = e.Message
                };
            }
            finally
            {
                client.Send(MessageTag.LoadCharCombatArenaResult, result);
            }
        }

        private (MatchMakingState state, int cost) GetMatchMakingState(CharacterData charData)
        {
            var cost = 0;
            var matchMakingState = MatchMakingState.Free;
            var arena = charData.ArenaManager;
            var count = arena.CurrentArenaCount;
            switch (count)
            {
                case < 5:
                    matchMakingState = MatchMakingState.Free;
                    cost = 0;
                    break;
                case < 15:
                    matchMakingState = charData.IsNoAds ? MatchMakingState.Free : MatchMakingState.Ads;
                    cost = 0;
                    break;
                default:
                    matchMakingState = MatchMakingState.NotFree;
                    cost = GameConfig.MatchMakingCost;
                    break;
            }

            return (matchMakingState, cost);
        }

        private LoadRankResult LoadRank()
        {
            var nextReload = ArenaRankService.GetDelayToGrantRewards();
            var typeRankArenaWithValue = GameConfig.TypeRankArenaWithValue;
            var typeRankArenaWithReward = GameConfig.TypeRankArenaWithReward;
            MatchMakingState matchMakingState;
            int cost;
            lock (client.CharacterLock)
            {
                var charData = client.CharacterData;
                (matchMakingState, cost) = GetMatchMakingState(charData);
            }

            var result = new LoadRankResult()
            {
                Success = true,
                TypeRankArenaWithValue = typeRankArenaWithValue,
                TypeRankArenaWithReward = typeRankArenaWithReward,
                NextReload = nextReload,
                MatchMakingState = matchMakingState,
                Cost = cost,
            };
            return result;
        }

        private async Task<CharacterData> LoadEnemyCombat()
        {
            int point;
            int level;
            int charId;
            lock (client.CharacterLock)
            {
                var charData = client.CharacterData;
                point = charData.ArenaManager.Points;
                level = charData.Progression.Level;
                charId = charData.Id;
            }

            var min = point - 500;
            var max = point + 500;
            await using var db = await Program.CreateDbAsync();

            var enemyCharId =
                await db.ArenaManagers
                    .Where(x => x.Points >= min && x.Points <= max && x.CharId != charId)
                    .OrderBy(x => x.CharData.Progression.Level >= level ? 0 : 1)
                    .ThenBy(x => Math.Abs(x.CharData.Progression.Level - level))
                    .ThenBy(_ => EF.Functions.Random())
                    .Select(x => x.CharId)
                    .FirstOrDefaultAsync();
            if (enemyCharId == 0)
            {
                return null;
            }

            var matchPlayerProgression =
                await db.Progressions
                    .AsNoTracking()
                    .Include(x => x.CharacterData)
                    .ThenInclude(x => x.EquipmentManager)
                    .ThenInclude(x => x.ThanThongEquips)
                    .Include(x => x.CharacterData)
                    .ThenInclude(x => x.EquipmentManager)
                    .ThenInclude(x => x.DanCombatUseds)
                    .Include(x => x.CharacterData)
                    .ThenInclude(x => x.InventoryManager)
                    .Include(x => x.CharacterData)
                    .ThenInclude(x => x.InventoryManager)
                    .ThenInclude(x => x.NgoDaos)
                    .Include(x => x.CharacterData)
                    .ThenInclude(x => x.Stats)
                    .Include(x => x.CharacterData)
                    .ThenInclude(x => x.Appearance)
                    .AsSplitQuery()
                    .FirstOrDefaultAsync(x => x.CharId == enemyCharId);
            if (matchPlayerProgression == null)
            {
                return null;
            }

            await matchPlayerProgression.CharacterData.InventoryManager.LoadEquipments(db);
            return matchPlayerProgression.CharacterData;
        }

        private void StartCombat(ETypeCombat type, int level)
        {
            lock (combatLock)
            {
                EnsureNoCombatSession();
                combatSession = new CombatSession()
                {
                    Status = CombatStatus.InCombat,
                    Type = type,
                    Level = level,
                };
            }
        }

        private void EnsureNoCombatSession()
        {
            lock (combatLock)
            {
                if (combatSession is { Status: CombatStatus.InCombat })
                {
                    throw new InvalidException(Strings.CombatAlreadyInProgress);
                }
            }
        }

        private void EndCombat(RewardSnapshot resultReward)
        {
            lock (combatLock)
            {
                combatSession.Status = CombatStatus.End;
                combatSession.RewardSnapshot = resultReward;
            }
        }

        private void FinalNow()
        {
            lock (client.CharacterLock)
            {
                var charData = client.CharacterData;
                if (charData == null)
                {
                    return;
                }

                CombatSession session;
                lock (combatLock)
                {
                    if (combatSession == null)
                    {
                        return;
                    }

                    session = combatSession;
                }

                if (session == null)
                {
                    return;
                }

                if (session.Status == CombatStatus.End)
                {
                    return;
                }

                try
                {
                    var stats = charData.Stats;
                    var result = new ResultCombat()
                    {
                        CurrentHPUser = stats.Hp,
                        CurrentLevelEnemy = session.Level,
                        CurrentThanThuc = stats.ThanThuc,
                        CurrentThoNguyen = stats.ThoNguyen,
                        ListIdThanThong = new(),
                        TypeCombat = session.Type,
                        TypeResultCombat = ETypeResultCombat.LoseByRun
                    };
                    charData.ResultCombat(result);
                }
                catch (InvalidException e)
                {
                    Log.Warning(e,
                        "Failed to resolve combat on disconnect; ClientId={ClientId}, CharacterId={CharacterId}, CombatType={CombatType}, CombatLevel={CombatLevel}",
                        client.Id, charData.Id, session.Type, session.Level);
                }
                finally
                {
                    EndCombat(null);
                    client.SaveChanges().GetAwaiter().GetResult();
                }
            }
        }

        private void OnOtherClientLogin()
        {
        }

        private void OnLogout()
        {
            FinalNow();
        }

        private void OnReconnectedSuccess()
        {
        }

        private void OnRemoveDisconnectedSession()
        {
            if (!client.IsOnline())
            {
                FinalNow();
            }
        }

        public void OnDisconnect(IProtocol<MessageTag> protocol)
        {
        }

        public void OnEnter()
        {
        }

        public void OnLeave()
        {
            client.OnLogout -= OnLogout;
            client.OnOtherClientLogin -= OnOtherClientLogin;
            client.OnReconnectedSuccess -= OnReconnectedSuccess;
            client.OnRemoveDisconnectedSession -= OnRemoveDisconnectedSession;
        }
    }
}