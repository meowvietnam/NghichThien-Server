using Microsoft.EntityFrameworkCore;
using MTS.Game;
using MTS.Game.Combat;
using MTS.Localization;
using MTS.Models;
using MTS.Network.Models;
using MTS.Services;
using Network;
using Network.Extensions;

namespace MTS.Network.Core
{
    public class CombatHandler : IClientHandler
    {
        private Client client;

        private static MessageTag[] handler =
        [
            MessageTag.ResultCombat, MessageTag.LoadCharCombat,
            MessageTag.LoadCharCombatArena, MessageTag.LoadRankRequest,
            MessageTag.ClaimRankRewardRequest
        ];

        private CombatSession? combatSession;
        public CombatHandler(Client client)
        {
            this.client = client;
        }

        public MessageTag[] HandleTag => handler;

        public void Handle(Message<MessageTag> message)
        {
            var tag = message.Tag;
            var reader = message.Reader;
            var charData = client.CharacterData;
            if (tag == MessageTag.ResultCombat)
            {
                var request = reader.Read<ResultCombat>();
                ResultCombatResult result = null;
                try
                {
                    lock (charData)
                    {
                        result = charData.ResultCombat(request);
                        EndCombat();
                    }
                }
                catch (Exception e)
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
                    if (combatSession != null)
                    {
                        throw new(string.Empty);
                    }
                    lock (charData)
                    {
                        result = new LoadCharCombatResult()
                        {
                            Success = true,
                            Client = new CharacterSnapshot(charData)
                        };
                        charData.EquipmentManager.ClearDanCombat();
                        StartCombat(ETypeCombat.AllMaps, charData.Progression.Level);
                    }
                }
                catch (Exception e)
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
                    if (combatSession != null)
                    {
                        throw new(string.Empty);
                    }
                    lock (charData)
                    {
                        var (state, _) = GetMatchMakingState();
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
                catch (Exception e)
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
                    lock (charData)
                    {
                        var moneyVip = charData.ArenaManager.Claim(id).GetAwaiter().GetResult();
                        result = new ClaimRankRewardResult()
                        {
                            Success = true,
                            Id = id,
                            MoneyVip = moneyVip,
                        };
                    }
                }
                catch (Exception e)
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
        }

        public void SendLoadCharCombatArena()
        {
            LoadCharCombatResult result = null;
            var charData = client.CharacterData;
            try
            {
                lock (charData)
                {
                    var (state, cost) = GetMatchMakingState();
                    if (state == MatchMakingState.NotFree)
                    {
                        var remainingMoneyVip = charData.MoneyVip - cost;
                        if (remainingMoneyVip < 0)
                        {
                            throw new Exception(Strings.NotEnoughMoneyVip);
                        }
                        charData.SetMoneyVip(remainingMoneyVip);
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
            catch (Exception e)
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
        private (MatchMakingState state, int cost) GetMatchMakingState()
        {
            var cost = 0;
            var matchMakingState = MatchMakingState.Free;
            var charData = client.CharacterData;
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
            var (matchMakingState, cost) = GetMatchMakingState();
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
            var charData = client.CharacterData;
            var point = charData.ArenaManager.Points;
            var level = charData.Progression.Level;
            var charId = charData.Id;
            var min = point - 500;
            var max = point + 500;
            await using var db = await Program.CreateDbAsync();
            var matchPointPlayers =
                await db.ArenaManagers
                    .Where(x => x.Points >= min && x.Points <= max && x.CharId != charId && x.CharData.Progression.Level >= level)
                    .OrderBy(_ => EF.Functions.Random())
                    .Select(x => x.CharId)
                    .Take(10)
                    .ToListAsync();
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
                    .FirstOrDefaultAsync(x => matchPointPlayers.Contains(x.CharId));
            if (matchPlayerProgression == null)
            {
                return null;
            }

            await matchPlayerProgression.CharacterData.InventoryManager.LoadEquipments(db);
            return matchPlayerProgression.CharacterData;
        }

        private void StartCombat(ETypeCombat type, int level)
        {
            combatSession = new CombatSession()
            {
                Type = type,
                Level = level,
            };
        }

        private void EndCombat()
        {
            combatSession = null;
        }
        public void OnDisconnect(IProtocol<MessageTag> protocol)
        {
            var charData = client.CharacterData;
            if (combatSession == null)
            {
                return;
            }
            lock (charData)
            {
                try
                {
                    var stats = charData.Stats;
                    var result = new ResultCombat()
                    {
                        CurrentHPUser = stats.Hp,
                        CurrentLevelEnemy = combatSession.Level,
                        CurrentThanThuc = stats.ThanThuc,
                        CurrentThoNguyen = stats.ThoNguyen,
                        ListIdThanThong = new(),
                        TypeCombat = combatSession.Type,
                        TypeResultCombat = ETypeResultCombat.LoseByRun
                    };
                    charData.ResultCombat(result);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
                finally
                {
                    EndCombat();
                }
            }
        }

        public void OnEnter()
        {
        }

        public void OnLeave()
        {
        }
    }
}
