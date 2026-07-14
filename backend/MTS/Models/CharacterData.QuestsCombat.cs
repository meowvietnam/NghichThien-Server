using Microsoft.EntityFrameworkCore;
using MTS.Data;
using MTS.Game;
using MTS.Game.ItemProvider;
using MTS.Game.LuyenDan;
using MTS.Game.Resources;
using MTS.Game.Social;
using MTS.Localization;
using MTS.Network.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using MemoryPack;
using MTS.IAP;
using MTS.Game.Combat;
using MTS.Network;

namespace MTS.Models;

public partial class CharacterData
{
    public JoinMonPhaiResult? JoinMonPhai(EMonPhai request)
    {
        if (QuestManager.CurrentMainQuest <= 0)
        {
            throw new Exception(Strings.NotCompleteFirstMainQuest);
        }

        if (MonPhai != EMonPhai.None)
        {
            throw new Exception(Strings.MonPhaiExisted);
        }

        SetMonPhai(request);
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
            throw new Exception(Strings.HackerMainQuest);
        }

        int trueID = QuestManager.CurrentMainQuest + 1;
        var questRS = GameConfig.MainQuestManagerModel.MainQuestResource.FirstOrDefault(x => x.Id == trueID);
        if (questRS == null)
        {
            throw new Exception(Strings.MainQuestResourceNotExist);
        }

        if (Progression.Level < questRS.levelRequire)
        {
            throw new Exception(Strings.HackerMainQuest);
        }

        int linhThach = questRS.rewardRef.Money;
        int moneyVip = questRS.rewardRef.MoneyVip;
        var itemRefs = questRS.rewardRef.ItemRefs;
        if (linhThach > 0)
        {
            Stats.SetLinhThach(Stats.LinhThach + linhThach);
        }

        if (moneyVip > 0)
        {
            SetMoneyVip(moneyVip + MoneyVip);
        }

        if (itemRefs.Count > 0)
        {
            InventoryManager.AddToInventory(itemRefs);
        }

        int newMainQuest = QuestManager.CurrentMainQuest + 1;
        QuestManager.SetCurrentMainQuest(newMainQuest);
        return new CompleteMainQuestResult()
        {
            Success = true,
            CurrentMainQuest = newMainQuest,
        };
    }

    public CompleteSideQuestResult? CompleteSideQuest(CompleteSideQuest sideQuestComplete)
    {
        if (QuestManager.CurrentCountSideQuest >= 10)
        {
            throw new Exception(Strings.HackerSideQuest);
        }

        foreach (var item in sideQuestComplete.ListIdNpc)
        {
            var npcRef = GameConfig.NpcDatas.FirstOrDefault(x => x.CharStats.Id == item);
            if (npcRef == null)
            {
                throw new Exception(Strings.NpcNotExist);
            }

            int levelMax = Progression.Level + 2;
            int levelMin = Progression.Level - 1;
            if (Progression.Level > levelMax || Progression.Level < levelMin)
            {
                throw new Exception(Strings.HackerLevelNpc);
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
        };
    }


    public ResultCombatResult? ResultCombat(ResultCombat request)
    {
        foreach (var item in request.ListIdThanThong)
        {
            if (!EquipmentManager.ThanThongEquips.Any(x => x.Id == item))
            {
                throw new Exception(Strings.HackerSkillCombat);
            }
        }

        var idCounts = request.ListIdThanThong
            .GroupBy(id => id)
            .ToDictionary(g => g.Key, g => g.Count());

        var thanThongDict = GameConfig.NDCPTTManager.ThanThongs
            .Where(x => idCounts.ContainsKey(x.Id))
            .ToDictionary(
                x => x,
                x => idCounts[x.Id]
            );

        var newTamCanh = Stats.TamCanh;
        int baseAddCamNgo = 5;
        List<CamNgo> camNgoAdd = new List<CamNgo>();
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
                throw new Exception(Strings.HackerHP);
            }

            if (request.CurrentThanThuc > Stats.ThanThuc)
            {
                throw new Exception(Strings.HackerThanThucEndCombat);
            }

            if (request.CurrentThoNguyen > Stats.ThoNguyen)
            {
                throw new Exception(Strings.HackerThoNguyenEndCombat);
            }

            if (request.TypeCombat == ETypeCombat.AllMaps)
            {
                if (request.CurrentLevelEnemy > Progression.Level + 2)
                {
                    throw new Exception(Strings.HackerLevelEnemyCombat);
                }
            }
            else if (request.TypeCombat == ETypeCombat.Boss)
            {
                int validBossLevel = ((Progression.Level - 1) / 3 + 1) * 3;

                if (request.CurrentLevelEnemy != validBossLevel)
                {
                    throw new Exception(Strings.HackerLevelBossCombat);
                }
            }

            newHp = request.CurrentHPUser;

            if (request.TypeResultCombat == ETypeResultCombat.Win)
            {
                int linhThach = Random.Shared.Next(5 * request.CurrentLevelEnemy, 50 * request.CurrentLevelEnemy);
                int monsterLevel = request.CurrentLevelEnemy;
                var listItemRef = GenerateMonsterRewards(monsterLevel);
                listItemRef = ConsolidateItemRefs(listItemRef);
                InventoryManager.AddToInventory(listItemRef);
                newReward = new RewardSnapshot
                {
                    Money = linhThach,
                    MoneyVip = 0,
                    ItemRefs = listItemRef
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
            NewTamCanh = newTamCanh,
        };
    }

    public List<ItemRef> GenerateMonsterRewards(int monsterLevel)
    {
        var itemRefs = new List<ItemRef>();

        // 1. Xác định Cảnh giới gốc (0: Luyện Khí, 1: Trúc Cơ, 2: Kim Đan, 3: Nguyên Anh, 4: Hóa Thần, 5: Trên Hóa Thần)
        int currentRealm = (monsterLevel - 1) / 3;
        int currentTierIndex = currentRealm; // Mặc định Luyện Khí = Nhất Phẩm (0), Trúc Cơ = Nhị Phẩm (1)...

        // 2. Tính năng Bảo Hiểm / Đột Biến: Nếu ở max level của đại cảnh giới (3, 6, 9, 12, 15), sửa tỷ lệ thành 1% (0.01)
        bool isMaxLevelOfRealm = (monsterLevel % 3 == 0);
        if (isMaxLevelOfRealm && Random.Shared.NextDouble() <= 0.01)
        {
            currentTierIndex++; // Nhảy vọt lên 1 phẩm cấp cao hơn
        }

        // Khống chế không vượt quá Lục Phẩm (Index 5 trong enum Tier)
        if (currentTierIndex > 5) currentTierIndex = 5;

        // 3. Tính toán phân bổ tỷ lệ động dựa trên level cụ thể (Tính 1 lần dùng chung cho cả 5 item)
        var tierWeights = CalculateTierWeights(monsterLevel, currentTierIndex);

        // VÒNG LẶP: Sinh ra đúng 3 loại item phần thưởng độc lập
        for (int i = 0; i < 3; i++)
        {
            // Mỗi item sẽ quay xúc xắc chọn Tier riêng biệt dựa trên Trọng Số (Weights)
            Tier selectedTier = RollRandomTier(tierWeights);

            // 4. Quyết định loại Item rơi ra (50% Material, 50% Herb)
            ItemType selectedType = Random.Shared.NextDouble() < 0.5 ? ItemType.Material : ItemType.Herb;
            int selectedId = 0;

            if (selectedType == ItemType.Material)
            {
                var validMaterials = GameConfig.LuyenKhiResources.Materials
                    .Where(m => m.Tier == selectedTier).ToList();

                // Nếu không tìm thấy vật phẩm đúng Tier (ví dụ thiếu Nhất Phẩm)
                if (validMaterials.Count == 0)
                {
                    // Duyệt tăng dần từ Nhất Phẩm (0) đến Lục Phẩm (5) để tìm phẩm cấp thay thế gần nhất
                    for (int tierIndex = 0; tierIndex <= 5; tierIndex++)
                    {
                        validMaterials = GameConfig.LuyenKhiResources.Materials
                            .Where(m => (int)m.Tier == tierIndex).ToList();

                        if (validMaterials.Count > 0) break; // Tìm thấy danh sách có hàng là dừng lại liền
                    }
                }

                // Tiến hành bốc item
                if (validMaterials.Count > 0)
                    selectedId = validMaterials[Random.Shared.Next(0, validMaterials.Count)].Id;
                else
                    selectedId = 1001; // ID bảo hiểm cuối cùng nếu cả database trống rỗng
            }
            else
            {
                var validHerbs = GameConfig.LuyenDanResources.HerbDatas
                    .Where(h => h.Tier == selectedTier).ToList();

                // Nếu không tìm thấy Thảo dược đúng Tier
                if (validHerbs.Count == 0)
                {
                    // Duyệt tăng dần từ Nhất Phẩm đến Lục Phẩm để tìm danh sách có dữ liệu gần nhất
                    for (int tierIndex = 0; tierIndex <= 5; tierIndex++)
                    {
                        validHerbs = GameConfig.LuyenDanResources.HerbDatas
                            .Where(h => (int)h.Tier == tierIndex).ToList();

                        if (validHerbs.Count > 0) break;
                    }
                }

                // Tiến hành bốc item
                if (validHerbs.Count > 0)
                    selectedId = validHerbs[Random.Shared.Next(0, validHerbs.Count)].Id;
                else
                    selectedId = 2001; // ID thảo dược rác bảo hiểm cuối cùng
            }

            // 5. Random số lượng độc lập cho từng item (Count từ 1 đến 3)
            int rewardCount = Random.Shared.Next(1, 4);

            // Nạp item này vào danh sách kết quả
            itemRefs.Add(new ItemRef()
            {
                Id = (ushort)selectedId,
                Type = selectedType,
                Count = (ushort)rewardCount
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
            float weight = (float)Math.Pow(3.5, maxAllowedTierIndex - i);
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
}