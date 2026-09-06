using MTS.Exceptions;
using MTS.Game;
using MTS.Game.BaseSkill;
using MTS.Game.LuyenDan;
using MTS.Localization;
using MTS.Network.Models;
using MTS.Game.Combat;
using MTS.ND;
using MTS.Services;
using Serilog;

namespace MTS.Models;

public partial class CharacterData
{
    public JoinMonPhaiResult? JoinMonPhai(EMonPhai request)
    {
        if (QuestManager.CurrentMainQuest <= 0)
        {
            throw new InvalidException(Strings.NotCompleteFirstMainQuest);
        }

        if (MonPhai != EMonPhai.None)
        {
            throw new InvalidException(Strings.MonPhaiExisted);
        }

        SetMonPhai(request);
        ShopManager.ShopSkill.Reload(new ReloadShopRequest()
        {
            ShopType = EShopType.Skill,
            MonPhai = MonPhai
        });
        return new JoinMonPhaiResult()
        {
            Success = true,
            TypeMonPhai = request,
        };
    }

    public CompleteMainQuestResult? CompleteMainQuest(int mainQuestComplete)
    {
        if (mainQuestComplete != QuestManager.CurrentMainQuest)
        {
            throw new InvalidException(Strings.HackerMainQuest);
        }

        int trueID = QuestManager.CurrentMainQuest + 1;
        var questRS = GameConfig.MainQuestManagerModel.MainQuestResource.FirstOrDefault(x => x.Id == trueID);
        if (questRS == null)
        {
            throw new InvalidException(Strings.MainQuestResourceNotExist);
        }

        if (Progression.Level < questRS.levelRequire)
        {
            throw new InvalidException(Strings.HackerMainQuest);
        }

        Claim(questRS.rewardRef);
        int newMainQuest = QuestManager.CurrentMainQuest + 1;
        QuestManager.SetCurrentMainQuest(newMainQuest);
        return new CompleteMainQuestResult()
        {
            Success = true,
            CurrentMainQuest = newMainQuest,
            Money = Stats.LinhThach,
            MoneyVip = MoneyVip,
            RewardItems = questRS.rewardRef.RewardItems
        };
    }

    public CompleteSideQuestResult? CompleteSideQuest(CompleteSideQuest sideQuestComplete)
    {
        if (QuestManager.CurrentCountSideQuest >= 10)
        {
            throw new InvalidException(Strings.HackerSideQuest);
        }

        foreach (var item in sideQuestComplete.ListIdNpc)
        {
            var npcRef = GameConfig.NpcDatas.FirstOrDefault(x => x.CharStats.Id == item);
            if (npcRef == null)
            {
                throw new InvalidException(Strings.NpcNotExist);
            }

            int levelMax = Progression.Level + 2;
            int levelMin = Progression.Level - 1;
            if (Progression.Level > levelMax || Progression.Level < levelMin)
            {
                throw new InvalidException(Strings.HackerLevelNpc);
            }

            Social.RelationshipAddValue((ushort)item, 10);
        }

        int moneyVip = Random.Shared.Next(5, 20);
        int linhThach = Random.Shared.Next(20 * Progression.Level, 100 * Progression.Level);

        int newCountSideQuest = QuestManager.CurrentCountSideQuest + 1;
        QuestManager.SetCurrentCountSideQuest(newCountSideQuest);
        int newTamCanh = Stats.TamCanh + Progression.Level;
        Stats.SetTamCanh(newTamCanh);
        SetMoneyVip(MoneyVip + moneyVip);
        return new CompleteSideQuestResult()
        {
            Success = true,
            MoneyVipRandom = moneyVip,
            CurrentSideQuest = newCountSideQuest,
            LinhThachRandom = linhThach,
            NewTamCanh = Stats.TamCanh,
        };
    }


    public ResultCombatResult? ResultCombat(ResultCombat request)
    {
        request.ListIdThanThong
            .RemoveAll(item => !EquipmentManager.ThanThongEquips.Any(x => x.Id == item));

        var idCounts = request.ListIdThanThong
            .GroupBy(id => id)
            .ToDictionary(g => g.Key, g => g.Count());

        var thanThongDict = GameConfig.NDCPTTManager.ThanThongs
            .Where(x => idCounts.ContainsKey(x.Id))
            .ToDictionary(
                x => x,
                x => idCounts[x.Id]
            );
        var enemyLevel = request.CurrentLevelEnemy;
        var newTamCanh = Stats.TamCanh;
        int baseAddCamNgo = 5;
        List<CamNgo> camNgoAdd = new List<CamNgo>();
        var camNgo = new CamNgo()
        {
            CharId = Id,
            Id = ETypeSkill.The,
            Value = 10 * enemyLevel
        };
        var camNgos = InventoryManager.CamNgos;
        if (camNgos.TryGetValue(camNgo, out var ownerCamNgo))
        {
            camNgo.Value += ownerCamNgo.Value;
        }
        InventoryManager.AddOrUpdate(camNgo);
        camNgoAdd.Add(camNgo);
        var newNgoDaoDiem = Progression.NgoDaoDiem;
        foreach (var item in thanThongDict)
        {
            newNgoDaoDiem = InventoryManager.UpdateNgoDaoWithSkill(item.Key, baseAddCamNgo, ref camNgoAdd, item.Value);
        }

        int newHp = Stats.Hp;
        int newPointRank = ArenaManager.Points;
        RewardSnapshot newReward = new RewardSnapshot();
        if (request.TypeCombat == ETypeCombat.AllMaps || request.TypeCombat == ETypeCombat.Boss)
        {
            if (request.CurrentHPUser > Stats.SinhMenh * 5)
            {
                throw new InvalidException(Strings.HackerHP);
            }

            if (request.CurrentThanThuc > Stats.ThanThuc)
            {
                throw new InvalidException(Strings.HackerThanThucEndCombat);
            }

            if (request.CurrentThoNguyen > Stats.ThoNguyen)
            {
                throw new InvalidException(Strings.HackerThoNguyenEndCombat);
            }

            if (request.TypeCombat == ETypeCombat.AllMaps)
            {
                if (enemyLevel > Progression.Level + 2)
                {
                    throw new InvalidException(Strings.HackerLevelEnemyCombat);
                }
            }
            else if (request.TypeCombat == ETypeCombat.Boss)
            {
                int validBossLevel = ((Progression.Level - 1) / 3 + 1) * 3;

                if (enemyLevel != validBossLevel)
                {
                    throw new InvalidException(Strings.HackerLevelBossCombat);
                }
            }

            newHp = request.CurrentHPUser;

            if (request.TypeResultCombat == ETypeResultCombat.Win)
            {
                int linhThach = Random.Shared.Next(5 * enemyLevel, 50 * enemyLevel);
                int monsterLevel = enemyLevel;
                var listItemRef = GenerateMonsterRewards(monsterLevel);
                listItemRef = ConsolidateItemRefs(listItemRef);
                var rewardItems = InventoryManager.AddToInventory(listItemRef);
                var itemEvents = GenerateItemEvents();
                if (itemEvents != null)
                {
                    var inventory = EventManager.Inventory;
                    foreach (var itemEvent in itemEvents)
                    {
                        inventory.Add(itemEvent);
                    }
                }
                newReward = new RewardSnapshot
                {
                    Money = linhThach,
                    MoneyVip = 0,
                    RewardItems = rewardItems,
                    ItemRefs = listItemRef,
                    itemEvents = itemEvents
                };
            }
            else if (request.TypeResultCombat == ETypeResultCombat.LoseByRun)
            {
                if (request.CurrentHPUser < Stats.SinhMenh)
                {
                    newHp = request.CurrentHPUser / 2;
                }
                else
                {
                    newHp = Stats.SinhMenh / 2;
                }
            }

            newHp = Math.Clamp(newHp, 0, Stats.SinhMenh);
            Stats.SetHp(newHp);
            Stats.SetThoNguyen(request.CurrentThoNguyen);
            Stats.SetThanThuc(request.CurrentThanThuc);
        }
        else if (request.TypeCombat == ETypeCombat.DauTruong)
        {
            if (request.TypeResultCombat == ETypeResultCombat.Win)
            {
                newPointRank += 25;
                ArenaManager.SetPoints(newPointRank);
            }
            else
            {
                if (ArenaManager.Points > 400)
                {
                    newPointRank -= 15;
                    ArenaManager.SetPoints(newPointRank);
                }
            }
        }

        var loseTypes = new[]
            { ETypeResultCombat.LoseByDied, ETypeResultCombat.LoseByRun, ETypeResultCombat.LoseByTime };
        if (loseTypes.Contains(request.TypeResultCombat))
        {
            var min = CharacterStats.GetMinTamCanh();
            var decrease = (int)Math.Round((newTamCanh - min) * 0.1f);
            newTamCanh = Math.Max(min, newTamCanh - decrease);
        }

        if (request.TypeResultCombat == ETypeResultCombat.Win && request.TypeCombat == ETypeCombat.AllMaps)
        {
            var myLevel = Progression.Level;
            var deltaLevel = enemyLevel - myLevel;
            var addPoints = deltaLevel switch
            {
                <= 0 => 1,
                > 0 => 3,
            };
            var newPoints = MonsterKill.Points + addPoints;
            MonsterKill.SetPoints(newPoints);
        }

        Stats.SetTamCanh(newTamCanh);

        return new ResultCombatResult()
        {
            Success = true,
            NewHP = newHp,
            NewCamNgo = camNgoAdd.ToArray(),
            Reward = newReward,
            NewNgoDaoDiem = newNgoDaoDiem,
            TypeResultCombat = request.TypeResultCombat,
            NewPointRank = newPointRank,
            NewTamCanh = Stats.TamCanh,
        };
    }
    public ResultCombatResult? ResultCombatPvp(ResultCombat request)
    {
        request.ListIdThanThong
            .RemoveAll(item => !EquipmentManager.ThanThongEquips.Any(x => x.Id == item));
        var enemyLevel = request.CurrentLevelEnemy;
        int baseAddCamNgo = 5;
        List<CamNgo> camNgoAdd = new List<CamNgo>();
        var camNgo = new CamNgo()
        {
            CharId = Id,
            Id = ETypeSkill.The,
            Value = 10 * enemyLevel
        };
        var camNgos = InventoryManager.CamNgos;
        if (camNgos.TryGetValue(camNgo, out var ownerCamNgo))
        {
            camNgo.Value += ownerCamNgo.Value;
        }
        InventoryManager.AddOrUpdate(camNgo);
        camNgoAdd.Add(camNgo);
        int newHp = Stats.Hp;
        RewardSnapshot newReward = new RewardSnapshot();
        if (request.TypeCombat != ETypeCombat.LuanBanRealTime)
        {
            if (request.CurrentHPUser > Stats.SinhMenh * 5)
            {
                throw new InvalidException(Strings.HackerHP);
            }

            if (request.CurrentThanThuc > Stats.ThanThuc)
            {
                throw new InvalidException(Strings.HackerThanThucEndCombat);
            }

            if (request.CurrentThoNguyen > Stats.ThoNguyen)
            {
                throw new InvalidException(Strings.HackerThoNguyenEndCombat);
            }

            newHp = request.CurrentHPUser;

            if (request.TypeResultCombat == ETypeResultCombat.Win)
            {
                int linhThach = Random.Shared.Next(5 * enemyLevel, 50 * enemyLevel);
                int monsterLevel = enemyLevel;
                var listItemRef = GenerateMonsterRewards(monsterLevel);
                listItemRef = ConsolidateItemRefs(listItemRef);
                var rewardItems = InventoryManager.AddToInventory(listItemRef);
                var itemEvents = GenerateItemEvents();
                if (itemEvents != null)
                {
                    var inventory = EventManager.Inventory;
                    foreach (var itemEvent in itemEvents)
                    {
                        inventory.Add(itemEvent);
                    }
                }

                linhThach = 0;
                rewardItems = new();
                itemEvents = new();
                listItemRef = new();
                
                newReward = new RewardSnapshot
                {
                    Money = linhThach,
                    MoneyVip = 0,
                    RewardItems = rewardItems,
                    ItemRefs = listItemRef,
                    itemEvents = itemEvents
                };
            }
            else if (request.TypeResultCombat == ETypeResultCombat.LoseByRun)
            {
                if (request.CurrentHPUser < Stats.SinhMenh)
                {
                    newHp = request.CurrentHPUser / 2;
                }
                else
                {
                    newHp = Stats.SinhMenh / 2;
                }
            }
            newHp = Math.Clamp(newHp, 0, Stats.SinhMenh);
            Stats.SetHp(newHp);
            Stats.SetThoNguyen(request.CurrentThoNguyen);
            Stats.SetThanThuc(request.CurrentThanThuc);
        }
        return new ResultCombatResult()
        {
            Success = true,
            NewHP = newHp,
            NewCamNgo = camNgoAdd.ToArray(),
            Reward = newReward,
            NewNgoDaoDiem = Progression.NgoDaoDiem,
            TypeResultCombat = request.TypeResultCombat,
            NewPointRank = ArenaManager.Points,
            NewTamCanh = Stats.TamCanh,
        };
    }
    private List<ItemEvent> GenerateItemEvents()
    {
        return EventService.GenerateItemEvents();
    }
    public List<ItemRef> GenerateMonsterRewards(int monsterLevel)
    {
        var itemRefs = new List<ItemRef>();

        int currentRealm = (monsterLevel - 1) / 3;
        int indexInRealm = (monsterLevel - 1) % 3;

        double normalMutationRate = 0.2;
        double superMutationRate = 0.1;
        normalMutationRate *= (indexInRealm + 1);
        superMutationRate *= (indexInRealm + 1);

        double totalMutationRate = normalMutationRate + superMutationRate;
        int rewardCount = Random.Shared.Next(3, 9);

        // VÒNG LẶP: Giờ đây mỗi slot là một lần xúc xắc độc lập hoàn toàn!
        for (int i = 0; i < rewardCount; i++)
        {
            // Khởi tạo lại cấp bậc gốc cho từng slot quay riêng biệt
            int slotMaxTierIndex = currentRealm;

            // Lắc xúc xắc đột biến CHỈ cho riêng slot này
            double rate = Random.Shared.NextDouble();
            if (rate <= superMutationRate)
            {
                slotMaxTierIndex += 2; // Đột biến cực đại slot i
            }
            else if (rate <= totalMutationRate)
            {
                slotMaxTierIndex += 1; // Đột biến thường slot i
            }

            if (slotMaxTierIndex > 5) slotMaxTierIndex = 5;

            // Tính trọng số động riêng cho slot này dựa trên trần phẩm cấp vừa quay được
            var tierWeights = CalculateTierWeights(monsterLevel, slotMaxTierIndex);
            Tier selectedTier = RollRandomTier(tierWeights);

            // 4. Quyết định loại Item rơi ra (50% Material, 50% Herb)
            ItemType selectedType = Random.Shared.NextDouble() < 0.5 ? ItemType.Material : ItemType.Herb;
            int selectedId = 0;

            if (selectedType == ItemType.Material)
            {
                var validMaterials = GameConfig.LuyenKhiResources.Materials.Where(m => m.Tier == selectedTier).ToList();
                if (validMaterials.Count == 0)
                {
                    for (int tierIndex = 0; tierIndex <= 5; tierIndex++)
                    {
                        validMaterials = GameConfig.LuyenKhiResources.Materials.Where(m => (int)m.Tier == tierIndex)
                            .ToList();
                        if (validMaterials.Count > 0) break;
                    }
                }

                if (validMaterials.Count > 0)
                    selectedId = validMaterials[Random.Shared.Next(0, validMaterials.Count)].Id;
                else
                    selectedId = 1001;
            }
            else
            {
                var validHerbs = GameConfig.LuyenDanResources.HerbDatas.Where(h => h.Tier == selectedTier).ToList();
                if (validHerbs.Count == 0)
                {
                    for (int tierIndex = 0; tierIndex <= 5; tierIndex++)
                    {
                        validHerbs = GameConfig.LuyenDanResources.HerbDatas.Where(h => (int)h.Tier == tierIndex)
                            .ToList();
                        if (validHerbs.Count > 0) break;
                    }
                }

                if (validHerbs.Count > 0)
                    selectedId = validHerbs[Random.Shared.Next(0, validHerbs.Count)].Id;
                else
                    selectedId = 2001;
            }


            itemRefs.Add(new ItemRef()
            {
                Id = (ushort)selectedId,
                Type = selectedType,
                Count = 1,
            });
        }

        return itemRefs;
    }

    /// <summary>
    /// Hàm tính toán trọng số tỷ lệ động. Càng về sau càng mở khóa phẩm cao, nhưng tỷ lệ phẩm cao luôn thấp.
    /// </summary>
    private Dictionary<Tier, float> CalculateTierWeights(int level, int maxAllowedTierIndex)
    {
        var weights = new Dictionary<Tier, float>();

        for (int i = 0; i <= maxAllowedTierIndex; i++)
        {
            Tier t = (Tier)i;
            float weight = (float)Math.Pow(1, maxAllowedTierIndex - i);
            weights.Add(t, weight);
        }

        return weights;
    }

    /// <summary>
    /// Hàm chọn một Tier ngẫu nhiên dựa trên bảng trọng số phần trăm
    /// </summary>
    private Tier RollRandomTier(Dictionary<Tier, float> weights)
    {
        float totalWeight = weights.Values.Sum();
        float randomValue = (float)Random.Shared.NextDouble() * totalWeight;
        float currentSum = 0f;

        foreach (var pair in weights)
        {
            currentSum += pair.Value;
            if (randomValue <= currentSum)
            {
                return pair.Key;
            }
        }

        return weights.Keys.Last();
    }

    public void Claim(RewardSnapshot reward)
    {
        SetMoneyVip(reward.MoneyVip + MoneyVip);
        Stats.SetLinhThach(reward.Money + Stats.LinhThach);
        if (reward.ItemRefs != null)
        {
            var rewardItems = InventoryManager.AddToInventory(reward.ItemRefs);
            reward.RewardItems = rewardItems;
        }
        if (reward.itemEvents != null)
        {
            var items = reward.itemEvents;
            var inventory = EventManager.Inventory;
            foreach (var itemEvent in items)
            {
                inventory.Add(itemEvent);
            }
        }
    }
}