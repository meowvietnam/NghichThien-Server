using Microsoft.EntityFrameworkCore;
using MTS.Data;
using MTS.Game;
using MTS.Game.BaseSkill;
using MTS.Game.ItemProvider;
using MTS.Game.LuyenDan;
using MTS.Game.Resources;
using MTS.Game.Social;
using MTS.Localization;
using MTS.Network.Models;
using MTS.Utils;
using Network;
using Network.Extensions;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using static MTS.Game.LuyenDan.RecipeData;

namespace MTS.Models;

public class CharacterData : IMemorySerializable<CharacterData>
{
    [JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public int UserId { get; set; }

    [Key] public int Id { get; set; }
    public Gender Gender { get; set; }
    public string Name { get; set; }
    public EMonPhai MonPhai { get; private set; }
    public int MoneyVip { get; private set; } = 0;
    public bool IsInfiniteEnergy { get; private set; } = false;
    public bool IsNoAds { get; private set; } = false;
    public bool CanFly { get; private set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public CharacterStats Stats { get; set; } = new();
    public Progression Progression { get; set; } = new();
    public Appearance Appearance { get; set; } = new();
    public EquipmentManager EquipmentManager { get; set; } = new();
    public Social Social { get; set; } = new();
    public InventoryManager InventoryManager { get; set; } = new();
    public QuestManager QuestManager { get; set; } = new();

    [ForeignKey(nameof(UserId))]
    [JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public UserData User { get; set; }

    [NotMapped] private bool dirty;

    public void SetMonPhai(EMonPhai value)
    {
        if (MonPhai != value)
        {
            MonPhai = value;
            dirty = true;
        }
    }

    public void SetMoneyVip(int value)
    {
        if (MoneyVip != value)
        {
            MoneyVip = value;
            dirty = true;
        }
    }

    public void SetIsInfiniteEnergy(bool value)
    {
        if (IsInfiniteEnergy != value)
        {
            IsInfiniteEnergy = value;
            dirty = true;
        }
    }

    public void SetCanFly(bool value)
    {
        if (CanFly != value)
        {
            CanFly = value;
            dirty = true;
        }
    }

    public CharacterData()
    {
    }

    public CharacterData(CharacterData clone)
    {
        Clone1(clone);
    }

    private void Clone0(CharacterData clone)
    {
        MonPhai = clone.MonPhai;
        MoneyVip = clone.MoneyVip;
        IsInfiniteEnergy = clone.IsInfiniteEnergy;
        IsNoAds = clone.IsNoAds;
        CanFly = clone.CanFly;
        CreatedAt = clone.CreatedAt;
        dirty = false;
    }

    private void Clone1(CharacterData clone)
    {
        Id = clone.Id;
        Gender = clone.Gender;
        Name = clone.Name;
        Stats.CopyFrom(clone.Stats);
        InventoryManager.Clone(clone.InventoryManager);
        CreatedAt = clone.CreatedAt;
        Progression.CopyFrom(clone.Progression);
        Appearance.CopyFrom(clone.Appearance);
        Social.CopyFrom(clone.Social);
        EquipmentManager.CopyFrom(clone.EquipmentManager);
        QuestManager.CopyFrom(clone.QuestManager);
        Clone0(clone);
    }

    public int ByteSize()
    {
        return Id.ByteSize()
               + Gender.ByteSize()
               + Name.ByteSize()
               + Stats.ByteSize()
               + Progression.ByteSize()
               + Appearance.ByteSize()
               + EquipmentManager.ByteSize()
               + MonPhai.ByteSize()
               + MoneyVip.ByteSize()
               + IsInfiniteEnergy.ByteSize()
               + IsNoAds.ByteSize()
               + CanFly.ByteSize()
               + InventoryManager.ByteSize()
               + CreatedAt.ByteSize()
               + Social.ByteSize()
               + QuestManager.ByteSize();
    }

    public void Serialize(IWriter writer)
    {
        writer.Write(Id);
        writer.Write(Gender);
        writer.Write(Name);
        writer.Write(Stats);
        writer.Write(Progression);
        writer.Write(Appearance);
        writer.Write(EquipmentManager);
        writer.Write(MonPhai);
        writer.Write(MoneyVip);
        writer.Write(IsInfiniteEnergy);
        writer.Write(IsNoAds);
        writer.Write(CanFly);
        writer.Write(InventoryManager);
        writer.Write(CreatedAt);
        writer.Write(Social);
        writer.Write(QuestManager);
    }

    public void Deserialize(IReader reader, ref CharacterData value)
    {
        value.Id = reader.Read<int>();
        value.Gender = reader.Read<Gender>();
        value.Name = reader.Read();
        value.Stats = reader.Read<CharacterStats>();
        value.Progression = reader.Read<Progression>();
        value.Appearance = reader.Read<Appearance>();
        value.EquipmentManager = reader.Read<EquipmentManager>();
        value.MonPhai = reader.Read<EMonPhai>();
        value.MoneyVip = reader.Read<int>();
        value.IsInfiniteEnergy = reader.Read<bool>();
        value.IsNoAds = reader.Read<bool>();
        value.CanFly = reader.Read<bool>();
        value.InventoryManager = reader.Read<InventoryManager>();
        value.CreatedAt = reader.Read<DateTime>();
        value.Social = reader.Read<Social>();
        value.QuestManager = reader.Read<QuestManager>();
    }

    public bool IsInventoryDefault()
    {
        return InventoryManager.IsDefault();
    }

    public async Task SaveChanges(AppDbContext db)
    {
        if (dirty)
        {
            await db.Characters
                .Where(x => x.Id == Id)
                .ExecuteUpdateAsync(s =>
                    s.SetProperty(x => x.MonPhai, MonPhai)
                        .SetProperty(x => x.MoneyVip, MoneyVip)
                        .SetProperty(x => x.IsInfiniteEnergy, IsInfiniteEnergy)
                        .SetProperty(x => x.IsNoAds, IsNoAds)
                        .SetProperty(x => x.CanFly, CanFly));
            dirty = false;
        }

        if (!Stats.IsDefault())
        {
            await Stats.SaveChanges(db);
        }

        if (!Progression.IsDefault())
        {
            await Progression.SaveChanges(db);
        }

        if (!EquipmentManager.IsDefault())
        {
            await EquipmentManager.SaveChanges(db);
        }

        if (!Social.IsDefault())
        {
            await Social.SaveChanges(db);
        }

        if (!InventoryManager.IsDefault())
        {
            await InventoryManager.SaveChanges(db);
        }

        if (!QuestManager.IsDefault())
        {
            await QuestManager.SaveChanges(db);
        }
    }

    public LearnNgoDaoResult LearnNgoDao(int idNgoDao)
    {
        List<(NgoDaoResource, int)> FindResources(ND.NgoDao nd)
        {
            var ndResources = GameConfig.NgoDaoManager.NgoDaoResources;
            var result = new List<(NgoDaoResource, int)>();
            foreach (var ngoDaoResource in ndResources)
            {
                var findIndex = ngoDaoResource.NgoDaos.FindIndex(x => x.Any(y => y.Id == nd.Id));
                if (findIndex < 0)
                {
                    continue;
                }

                result.Add((ngoDaoResource, findIndex));
            }

            return result;
        }


        var owners = InventoryManager.NgoDaos;
        var camNgos = InventoryManager.CamNgos;
        var temp = new NgoDao()
        {
            Id = (ushort)idNgoDao,
        };
        if (owners.Contains(temp))
        {
            throw new Exception(Strings.HasLearnedNgoDao);
        }

        var ndcptt = GameConfig.NDCPTTManager;
        var ngoDao = ndcptt.NgoDaos.FirstOrDefault(x => x.Id == idNgoDao);
        if (ngoDao == null)
        {
            throw new Exception(Strings.NgoDaoNotExist);
        }

        var ngoDaoDiem = Progression.NgoDaoDiem;
        if (ngoDaoDiem < ngoDao.Require)
        {
            throw new Exception(Strings.NotEnoughNgoDaoPoints);
        }

        var res = FindResources(ngoDao);
        if (res.Count == 0)
        {
            throw new Exception(Strings.NgoDaoResourceNotExist);
        }

        foreach (var (resource, index) in res)
        {
            var camNgoRequire = NgoDaoResource.CamNgoRequire[index];
            var camNgoTmp = new CamNgo()
            {
                Id = resource.ETypeSkill,
            };
            if (!camNgos.TryGetValue(camNgoTmp, out var camNgo) || camNgo.Value < camNgoRequire)
            {
                throw new Exception(Strings.NotEnoughCamNgoPoints);
            }

            if (index > 0)
            {
                var any = resource.NgoDaos[index - 1].Any(x =>
                {
                    var tmp = new NgoDao()
                    {
                        Id = (ushort)x.Id,
                    };
                    return owners.Contains(tmp);
                });
                if (!any)
                {
                    throw new Exception(Strings.PreviousBranchRequiresAtLeastOneNgoDao);
                }
            }
        }

        var ndOwner = new NgoDao()
        {
            Id = (ushort)idNgoDao,
        };
        var newNgoDaoDiem = ngoDaoDiem - ngoDao.Require;
        Progression.SetNgoDaoDiem(newNgoDaoDiem);
        var buffStat = ngoDao.Buff(this);
        InventoryManager.Add(ndOwner);
        return new LearnNgoDaoResult()
        {
            Success = true,
            Buff = buffStat,
            NgoDao = ndOwner,
            NewNgoDaoDiem = newNgoDaoDiem,
        };
    }

    public EquipThanThongResult EquipThanThong(EquipThanThong request)
    {
        var thanThongOwner = new ThanThong()
        {
            Id = request.Id,
        };
        if (!InventoryManager.ThanThongOwners.Contains(thanThongOwner))
        {
            throw new Exception(Strings.ThanThongNotOwned);
        }

        var thanThong = EquipmentManager.EquipThanThong(request);
        return new EquipThanThongResult()
        {
            Success = true,
            ThanThong = thanThong,
        };
    }

    public UnequipThanThongResult UnequipThanThong(ushort id)
    {
        if (!EquipmentManager.UnequipThanThong(id))
        {
            throw new Exception(Strings.ThanThongNotOwned);
        }

        return new UnequipThanThongResult()
        {
            Success = true,
            Id = id
        };
    }

    public EquipCongPhapResult EquipCongPhap(EquipCongPhap request)
    {
        var id = request.Id;
        var slot = request.Slot;
        var cpRef = new CongPhap()
        {
            Id = id,
        };
        if (!InventoryManager.CongPhapOwners.TryGetValue(cpRef, out var congPhapOwner))
        {
            throw new Exception(Strings.CongPhapNotOwned);
        }

        var value = EquipmentManager.GetSlotCP(slot);
        if (value != ushort.MaxValue)
        {
            throw new Exception(Strings.SlotCongPhongNotEmpty);
        }

        EquipmentManager.SetSlotCP(slot, id);
        congPhapOwner.GetCongPhap().EquipCongPhap(this, congPhapOwner.Level);
        return new EquipCongPhapResult()
        {
            Success = true,
            Slot = slot,
            Id = id,
        };
    }

    public UnequipCongPhapResult UnequipCongPhap(ETypeSlotCP slot)
    {
        var value = EquipmentManager.GetSlotCP(slot);
        if (value == ushort.MaxValue)
        {
            throw new Exception(Strings.SlotCongPhapEmpty);
        }

        EquipmentManager.SetSlotCP(slot, ushort.MaxValue);
        var cpRef = new CongPhap()
        {
            Id = value,
        };
        if (!InventoryManager.CongPhapOwners.TryGetValue(cpRef, out var congPhapOwner))
        {
            throw new Exception(Strings.CongPhapNotOwned);
        }

        congPhapOwner.GetCongPhap().UnEquipCongPhap(this, congPhapOwner.Level);
        return new UnequipCongPhapResult()
        {
            Success = true,
            Slot = slot,
        };
    }

    private int CalculateCostTuLuyen()
    {
        var level = Progression.Level;
        var r = 0.2f;
        var baseCost = 100f;
        var m = 5f;
        if (level < 1) return 0;
        var steadyGrowth = Math.Pow(1 + r, level - 1);
        var jumpFactor = Math.Pow(m, Math.Floor((level - 1) / 3.0f));
        var finalCost = (baseCost * steadyGrowth) * jumpFactor;
        return (int)Math.Round(finalCost);
    }

    private (string name, int nextMilestone, int index) GetTamCanhValue()
    {
        var currentPoint = Stats.TamCanh;
        var tamCanh = GameConfig.tamCanh;
        var tamCanhRequireEnd = GameConfig.tamCanhRequireEnd;
        if (currentPoint < 0)
            return (tamCanh[0], 0, 0);

        for (int i = 0; i < tamCanhRequireEnd.Length; i++)
        {
            if (currentPoint >= tamCanhRequireEnd[i] &&
                (i == tamCanhRequireEnd.Length - 1 || currentPoint < tamCanhRequireEnd[i + 1]))
            {
                int nextLevel = (i + 1 < tamCanhRequireEnd.Length)
                    ? tamCanhRequireEnd[i + 1]
                    : currentPoint;
                return (tamCanh[i + 1], nextLevel, i + 1);
            }
        }

        return (tamCanh[^1], currentPoint, tamCanh.Length - 1);
    }

    private int GetPercentTuChat()
    {
        return Stats.TuChat;
    }

    private int GetPercentTamCanh()
    {
        var level = Progression.Level;
        var getTamCanh = GetTamCanhValue();
        var daiCanhGioi = TT.ThanThong.GetDaiCanhGioi(level);
        var percentTamCanh = 0;
        if (getTamCanh.index > daiCanhGioi)
        {
            percentTamCanh = 50;
        }
        else if (getTamCanh.index < daiCanhGioi)
        {
            percentTamCanh = -50;
        }

        return percentTamCanh;
    }

    public TuLuyenResult TuLuyen(TimeSpan time)
    {
        var chuTu1Id = EquipmentManager.ChuTu1Used;
        if (chuTu1Id == ushort.MaxValue)
        {
            throw new Exception(Strings.MainCultivationRequiredForRetreat);
        }

        var months = time.Days / 30;
        var cost = CalculateCostTuLuyen() * months;
        var money = Stats.LinhThach;
        var remaining = money - cost;
        if (remaining < 0)
        {
            throw new Exception(Strings.NotEnoughMoney);
        }

        var level = Progression.Level;
        var thoNguyen = TimeSpan.FromDays(Stats.ThoNguyen * 365);
        var tuoiTac = Stats.TuoiTac;
        var p1 = time.Ticks / thoNguyen.Ticks * 100;
        var p2 = time.Ticks / tuoiTac.Ticks * 100;
        if (p1 > 10 || p2 > 10)
        {
            throw new Exception(Strings.ExceedLimit);
        }

        var chuTu1 = GameConfig.NDCPTTManager.CongPhaps.FirstOrDefault(x => x.Id == chuTu1Id);
        var tocDocTuLuyen = chuTu1?.GetTocDoTuLuyen(level) ?? 0;
        var percentTuChat = GetPercentTuChat();
        var percentTamCanh = GetPercentTamCanh();

        int GetTotalValueBuffWithMultiplier(params float[] percent)
        {
            float total = tocDocTuLuyen;
            foreach (var item in percent)
            {
                total *= (1 + item / 100f);
            }

            return (int)total;
        }

        var newTamCanh = Stats.TamCanh - months * 2;
        var buffExp = GetTotalValueBuffWithMultiplier(percentTuChat, 100, percentTamCanh);
        var exp = Progression.CurrentExp + buffExp;
        var levelUpdated = Progression.SetCurrentExp(exp);
        Stats.SetLinhThach(remaining);
        Stats.SetTamCanh(newTamCanh);
        if (levelUpdated == null)
        {
            exp = Progression.CurrentExp;
            return new TuLuyenResult()
            {
                Success = true,
                CurrentExp = exp,
                NewTamCanh = newTamCanh,
                RemainingMoney = remaining,
            };
        }

        return new TuLuyenResult()
        {
            Success = true,
            HasLevelUpdated = true,
            LevelUpdated = levelUpdated,
            NewTamCanh = newTamCanh,
            RemainingMoney = remaining,
        };
    }

    public LinhNgoResult LinhNgo(LinhNgo request)
    {
        var type = request.Type;
        var id = (ushort)request.Id;
        var itemRef = new ItemRef()
        {
            Id = id,
            Type = type,
        };
        var camNgoList = new List<CamNgo>();
        if (type == ItemType.CongPhap)
        {
            if (!InventoryManager.CongPhaps.TryGetValue(itemRef, out var cpRef) || cpRef.Count < 1)
            {
                throw new Exception(Strings.CongPhapNotOwned);
            }

            var cpOwnerRef = new CongPhap()
            {
                Id = id,
            };
            var congPhapOwners = InventoryManager.CongPhapOwners;
            if (congPhapOwners.Contains(cpOwnerRef))
            {
                throw new Exception(Strings.CongPhapOwned);
            }

            var cp = GameConfig.NDCPTTManager.CongPhaps.FirstOrDefault(x => x.Id == id);
            if (cp == null)
            {
                throw new Exception(Strings.CongPhapNotFound);
            }

            double totalDays = (double)cp.baseTimeLearn / Stats.NgoTinh;
            var time = TimeSpan.FromDays(totalDays);
            var provider = ItemProviderFactory.GetProvider<SkillProvider>(type, InventoryManager);
            var price = provider.GetPriceItemBuy(id);
            var money = Stats.LinhThach;
            var remaining = money - price;
            if (remaining < 0)
            {
                throw new Exception(Strings.NotEnoughMoney);
            }

            var congPhap = new CongPhap()
            {
                Id = id,
                Level = 1
            };
            var newTuoiTac = Stats.TuoiTac + time;
            InventoryManager.Add(congPhap);
            Stats.SetLinhThach(remaining);
            Stats.SetTuoiTac(newTuoiTac);
            cpRef.Count -= 1;
            if (cpRef.Count < 1)
            {
                InventoryManager.Remove(cpRef);
            }
            else
            {
                InventoryManager.Update(cpRef);
            }

            var newNgoDaoDiem = InventoryManager.UpdateNgoDaoWithSkill(cp, 50, ref camNgoList, 1);
            return new LinhNgoResult()
            {
                Success = true,
                Id = id,
                Type = type,
                RemainingMoney = remaining,
                NewTuoiTac = newTuoiTac,
                CamNgos = camNgoList,
                NewNgoDaoDiem = newNgoDaoDiem,
            };
        }

        if (type == ItemType.ThanThong)
        {
            if (!InventoryManager.ThanThongs.TryGetValue(itemRef, out var ttRef) || ttRef.Count < 1)
            {
                throw new Exception(Strings.CongPhapNotOwned);
            }

            var ttOwnerRef = new ThanThong()
            {
                Id = id,
            };
            var thanThongOwners = InventoryManager.ThanThongOwners;
            if (thanThongOwners.Contains(ttOwnerRef))
            {
                throw new Exception(Strings.ThanThongOwned);
            }

            var tt = GameConfig.NDCPTTManager.ThanThongs.FirstOrDefault(x => x.Id == id);
            if (tt == null)
            {
                throw new Exception(Strings.ThanThongNotFound);
            }

            double totalDays = (double)tt.baseTimeLearn / Stats.NgoTinh;

            var time = TimeSpan.FromDays(totalDays);
            var provider = ItemProviderFactory.GetProvider<SkillProvider>(type, InventoryManager);
            var price = provider.GetPriceItemBuy(id);
            var money = Stats.LinhThach;
            var remaining = money - price;
            if (remaining < 0)
            {
                throw new Exception(Strings.NotEnoughMoney);
            }

            var thanThong = new ThanThong()
            {
                Id = id,
            };
            var newTuoiTac = Stats.TuoiTac + time;
            InventoryManager.Add(thanThong);
            Stats.SetLinhThach(remaining);
            Stats.SetTuoiTac(newTuoiTac);
            ttRef.Count -= 1;
            if (ttRef.Count < 1)
            {
                InventoryManager.Remove(ttRef);
            }
            else
            {
                InventoryManager.Update(ttRef);
            }

            var newNgoDaoDiem = InventoryManager.UpdateNgoDaoWithSkill(tt, 25, ref camNgoList, 1);

            return new LinhNgoResult()
            {
                Success = true,
                Id = id,
                Type = type,
                RemainingMoney = remaining,
                NewTuoiTac = newTuoiTac,
                CamNgos = camNgoList,
                NewNgoDaoDiem = newNgoDaoDiem,
            };
        }

        throw new Exception(Strings.NotSupportItemType);
    }

    public DotPhaCongPhapResult? DotPhaCongPhap(int request)
    {
        var id = (ushort)request;
        var itemRef = new ItemRef()
        {
            Id = id,
            Type = ItemType.CongPhap,
        };
        var camNgoList = new List<CamNgo>();
        var cpOwnerRef = new CongPhap()
        {
            Id = id,
        };
        int require = 1;

        var congPhapOwners = InventoryManager.CongPhapOwners;
        if (congPhapOwners.TryGetValue(cpOwnerRef, out var cpOwner))
        {
            require = cpOwner.Level + 1;
        }
        else
        {
            throw new Exception(Strings.CongPhapNotOwned);
        }

        if (!InventoryManager.CongPhaps.TryGetValue(itemRef, out var cpRef) || cpRef.Count < require)
        {
            throw new Exception(Strings.NotEnoughCongPhap);
        }


        var cp = GameConfig.NDCPTTManager.CongPhaps.FirstOrDefault(x => x.Id == id);
        if (cp == null)
        {
            throw new Exception(Strings.CongPhapNotFound);
        }

        double totalDays = (double)(cp.baseTimeLearn * require) / Stats.NgoTinh;
        var time = TimeSpan.FromDays(totalDays);
        var provider = ItemProviderFactory.GetProvider<SkillProvider>(ItemType.CongPhap, InventoryManager);
        var price = provider.GetPriceItemBuy(id) * require;
        var money = Stats.LinhThach;
        var remaining = money - price;
        if (remaining < 0)
        {
            throw new Exception(Strings.NotEnoughMoney);
        }


        var newTuoiTac = Stats.TuoiTac + time;
        CongPhap cpOwnerNewValue = new CongPhap()
        {
            Id = cpOwner.Id,
            Level = (ushort)(cpOwner.Level + 1),
        };
        InventoryManager.Update(cpOwner, cpOwnerNewValue);
        Stats.SetLinhThach(remaining);
        Stats.SetTuoiTac(newTuoiTac);
        cpRef.Count -= (ushort)require;
        if (cpRef.Count < 1)
        {
            InventoryManager.Remove(cpRef);
        }
        else
        {
            InventoryManager.Update(cpRef);
        }

        var newNgoDaoDiem = InventoryManager.UpdateNgoDaoWithSkill(cp, 50 * cpOwner.Level, ref camNgoList, 1);
        return new DotPhaCongPhapResult()
        {
            Success = true,
            Id = id,
            RemainingMoney = remaining,
            NewTuoiTac = newTuoiTac,
            CamNgos = camNgoList,
            NewNgoDaoDiem = newNgoDaoDiem,
        };
    }

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

        int moneyVip = Random.Shared.Next(5, 20);
        foreach (var item in sideQuestComplete.ListIdNpc)
        {
            // add tăng mối quan hệ với npc này thêm 10 điểm 
            //id npc thì a check là npc đó có level có nằm trong khoảng min là level player -1 => max là level player + 2 không vì e đảm bảo chỉ random ra lv npc trong khoảng này
            var npcRef = GameConfig.NpcDatas.FirstOrDefault(x => x.CharStats.Id == item);
            if(npcRef == null)
            {
                throw new Exception(Strings.NpcNotExist);

            }
            int levelMax = Progression.Level + 2;
            int levelMin = Progression.Level - 1;
            if (Progression.Level > levelMax || Progression.Level<levelMin)
            {
                throw new Exception(Strings.HackerLevelNpc);

            }
            //SetRelationShip
            Social.RelationshipAddValue((ushort)item,10);
        }

        int newCountSideQuest = QuestManager.CurrentCountSideQuest + 1;
        QuestManager.SetCurrentCountSideQuest(newCountSideQuest);
        SetMoneyVip(MoneyVip + moneyVip);
        return new CompleteSideQuestResult()
        {
            Success = true,
            MoneyVipRandom = moneyVip,
            CurrentSideQuest = newCountSideQuest,
        };
    }


    public WinCombatResult? WinCombat(WinCombat request)
    {
        foreach (var item in request.ListIdThanThong)
        {
            if (!this.EquipmentManager.ThanThongEquips.Any(x => x.Id == item))
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


        int baseAddCamNgo = 5;
        List<CamNgo> camNgoAdd = new List<CamNgo>();
        var newNgoDaoDiem = Progression.NgoDaoDiem;
        foreach (var item in thanThongDict)
        {
            newNgoDaoDiem = InventoryManager.UpdateNgoDaoWithSkill(item.Key, baseAddCamNgo, ref camNgoAdd, item.Value);
        }

        int newHp = Stats.Hp;
        RewardSnapshot newReward = new RewardSnapshot();
        if (request.TypeCombat == Game.Combat.ETypeCombat.AllMaps || request.TypeCombat == Game.Combat.ETypeCombat.Boss)
        {
            if (request.CurrentHPUser > Stats.SinhMenh * 5)
            {
                throw new Exception(Strings.HackerHP);
            }

            if (request.TypeCombat == Game.Combat.ETypeCombat.AllMaps)
            {
                if (request.CurrentLevelEnemy > Progression.Level + 2)
                {
                    throw new Exception(Strings.HackerLevelEnemyCombat);
                }
            }
            else if (request.TypeCombat == Game.Combat.ETypeCombat.Boss)
            {
                // Tính toán level Boss hợp lệ dựa trên level của Player
                int validBossLevel = ((Progression.Level - 1) / 3 + 1) * 3 + 1;

                // Nếu level của con Boss trong trận đấu khác với level hợp lệ -> Chắc chắn hack
                if (request.CurrentLevelEnemy != validBossLevel)
                {
                    throw new Exception(Strings.HackerLevelBossCombat);
                }
            }

            newHp = request.CurrentHPUser;
            Stats.SetHp(request.CurrentHPUser);

            int linhThach = Random.Shared.Next(5 * request.CurrentLevelEnemy, 50 * request.CurrentLevelEnemy);
            int monsterLevel = request.CurrentLevelEnemy;
            var listItemRef = GenerateMonsterRewards(monsterLevel);
            InventoryManager.AddToInventory(listItemRef);
            newReward = new RewardSnapshot
            {
                Money = linhThach,
                MoneyVip = 0,
                ItemRefs = listItemRef
            };
        }


        return new WinCombatResult()
        {
            Success = true,
            NewHP = newHp,
            NewCamNgo = camNgoAdd.ToArray(),
            Reward = newReward,
            NewNgoDaoDiem = newNgoDaoDiem,
        };
    }
    public TangQuaNpcResult? TangQuaNpc(TangQuaNpc request)
    {
        var npcRef = GameConfig.NpcDatas.FirstOrDefault(x => x.CharStats.Id == request.IdChar);
        if (npcRef == null)
        {
            Console.WriteLine("npc k tồn tại");

            throw new Exception(Strings.NpcNotExist);

        }
        var value = ItemProviderFactory.GetProvider(request.ItemRef.Type,InventoryManager).ValueTangQuaNPCWithItem(request.ItemRef.Id, npcRef.CharStats.Progression.Level);
        if(value<0)
        {
            Console.WriteLine("value add âm");

            throw new Exception();

        }
        int valueAdd = value * request.ItemRef.Count;
        bool isHasItem = false;
        if(request.ItemRef.Type == ItemType.Herb)
        {
            var Herb = InventoryManager.Herbs.FirstOrDefault(x => x.Id == request.ItemRef.Id);
            if (Herb != null && Herb.Count >= request.ItemRef.Count)
            {
                isHasItem = true;
            }    

        }
        else if (request.ItemRef.Type == ItemType.Recipe)
        {
            var Recipe = InventoryManager.Recipes.FirstOrDefault(x => x.Id == request.ItemRef.Id);
            if (Recipe != null && Recipe.Count >= request.ItemRef.Count)
            {
                isHasItem = true;
            }

        }
        else if (request.ItemRef.Type == ItemType.LinhDan)
        {
            var LinhDan = InventoryManager.LinhDans.FirstOrDefault(x => x.Id == request.ItemRef.Id);
            if (LinhDan != null && LinhDan.Count >= request.ItemRef.Count)
            {
                isHasItem = true;
            }

        }
        else if (request.ItemRef.Type == ItemType.Material)
        {
            var Material = InventoryManager.Materials.FirstOrDefault(x => x.Id == request.ItemRef.Id);
            if (Material != null && Material.Count >= request.ItemRef.Count)
            {
                isHasItem = true;
            }

        }
        else if (request.ItemRef.Type == ItemType.ThanThong)
        {
            var thanThong = InventoryManager.ThanThongs.FirstOrDefault(x => x.Id == request.ItemRef.Id);
            if (thanThong != null && thanThong.Count >= request.ItemRef.Count)
            {
                isHasItem = true;
            }

        }
        else if (request.ItemRef.Type == ItemType.CongPhap)
        {
            var congPhap = InventoryManager.CongPhaps.FirstOrDefault(x=>x.Id == request.ItemRef.Id);
            if (congPhap != null && congPhap.Count >= request.ItemRef.Count) 
            {
                isHasItem = true;
            }

        }
        else if (request.ItemRef.Type == ItemType.Furnace)
        {
            if (InventoryManager.Furnaces.FirstOrDefault(x=>x.SelfId == request.ItemRef.Id) != null) 
            {
                isHasItem = true;
            }

        }
        else if (request.ItemRef.Type == ItemType.VuKhi)
        {
            if (InventoryManager.VuKhis.FirstOrDefault(x => x.SelfId == request.ItemRef.Id) != null)
            {
                isHasItem = true;
            }

        }
        else if (request.ItemRef.Type == ItemType.PhongCu)
        {
            if (InventoryManager.PhongCus.FirstOrDefault(x => x.SelfId == request.ItemRef.Id) != null)
            {
                isHasItem = true;
            }

        }
        else if (request.ItemRef.Type == ItemType.TrangSuc)
        {
            if (InventoryManager.TrangSucs.FirstOrDefault(x => x.SelfId == request.ItemRef.Id) != null)
            {
                isHasItem = true;
            }

        }
        if (!isHasItem)
        {
            Console.WriteLine("HackerTangQuaNPC khi k có đủ đồ");

            throw new Exception(Strings.HackerTangQuaNPC);

        }
        if(request.ItemRef.Type == ItemType.Furnace)
        {
            InventoryManager.RemoveFurnace((ushort)request.ItemRef.Id);

        }
        else if(request.ItemRef.Type == ItemType.VuKhi || request.ItemRef.Type == ItemType.PhongCu || request.ItemRef.Type == ItemType.TrangSuc)
        {
            InventoryManager.RemoveEquipment((ushort)request.ItemRef.Id);

        }
        else
        {
            InventoryManager.Add(request.ItemRef,-request.ItemRef.Count);

        }

        int newValueRelationship = Social.GetRelationship((ushort)request.IdChar).Points + valueAdd;
        Social.RelationshipAddValue((ushort)request.IdChar, valueAdd);
        return new TangQuaNpcResult()
        {
            Success = true,
            IdChar = request.IdChar,
            NewValueRelationship = newValueRelationship,
        };
    }
    public ThinhGiaoKyNangResult? ThinhGiaoKyNang(ThinhGiaoKyNang request)
    {
        var npcRef = GameConfig.NpcDatas.FirstOrDefault(x => x.CharStats.Id == request.IdChar);
        if (npcRef == null)
        {
            throw new Exception(Strings.NpcNotExist);

        }
        int currentRelationshipValue = 0;
        var npcCurrent = Social.Relationships.FirstOrDefault(x => x.Npc == request.IdChar);
        var currentRelationType = ERelationShip.None;
        (List<int>, List<int>, List<int>) currentSkillCategorize = new();
        ItemRef itemRefSkill = new();
        var requireRelationShip = 0;

        if (npcCurrent != null)
        {
            currentRelationshipValue = npcCurrent.Points;
        }

        if (request.TypeSkill == ItemType.ThanThong)
        {
            currentRelationType = GameConfig.GetERelationShip(currentRelationshipValue, true);
            currentSkillCategorize = npcRef.GetCategorizeThinhGiaoThanThong();
            itemRefSkill =  new ItemRef
                    {
                        Type = ItemType.ThanThong,
                        Id = (ushort)request.IdSkill,
                        Count = 1
                    };
            requireRelationShip = GameConfig.relationShipRequireThanThong[currentRelationType];
            
        }    
        else if(request.TypeSkill == ItemType.CongPhap)
        {
             currentRelationType = GameConfig.GetERelationShip(currentRelationshipValue, true);
            currentSkillCategorize = npcRef.GetCategorizeThinhGiaoCongPhap();
            itemRefSkill = new ItemRef
            {
                Type = ItemType.CongPhap,
                Id = (ushort)request.IdSkill,
                Count = 1
            };
            requireRelationShip = GameConfig.relationShipRequireCongPhap[currentRelationType];

        }
        else
        {
            throw new Exception(Strings.NotSupportItemType);
        }
        int newValueRelationship = Social.GetRelationship((ushort)request.IdChar).Points - requireRelationShip;

        if (currentRelationType != ERelationShip.None)
        {
             
            if(currentRelationType == ERelationShip.Quen)
            {
                if(!currentSkillCategorize.Item1.Contains(request.IdSkill))
                {
                    throw new Exception(Strings.HackerSkillNotExistOnNpc);

                }
               
            }    
            else if (currentRelationType == ERelationShip.BanBe)
            {

                if (!currentSkillCategorize.Item2.Contains(request.IdSkill))
                {
                    throw new Exception(Strings.HackerSkillNotExistOnNpc);

                }
            }   
            else if(currentRelationType == ERelationShip.ThanThiet)
            {
                if (!currentSkillCategorize.Item3.Contains(request.IdSkill))
                {
                    throw new Exception(Strings.HackerSkillNotExistOnNpc);

                }
            }
            InventoryManager.AddToInventory(new List<ItemRef> {itemRefSkill});
            Social.RelationshipAddValue((ushort)request.IdChar,-requireRelationShip);
            // giảm độ hảo cảm của npc đó với user giảm đi requireRelationShip

        }
        else
        {
            throw new Exception();

        }



        return new ThinhGiaoKyNangResult()
        {
            Success = true,
            IdChar = request.IdChar,
            ItemRefSkill = itemRefSkill,
            NewValueRelationship = newValueRelationship,
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

    public EquipEquipmentResult? EquipEquipment(EquipEquipment request)
    {
        bool isUnequip = request.Id == ushort.MaxValue;

        switch (request.TypeEquipment)
        {
            case Game.LuyenKhi.EquipmentType.Weapon:
                // Nếu không phải tháo đồ, check xem trong hòm có món này không
                if (!isUnequip && !this.InventoryManager.VuKhis.Any(x => x.SelfId == request.Id))
                    throw new Exception(Strings.HackerEquipment);

                this.EquipmentManager.EquipVuKhi(request.Id);
                break;

            case Game.LuyenKhi.EquipmentType.Armor:
                if (!isUnequip && !this.InventoryManager.PhongCus.Any(x => x.SelfId == request.Id))
                    throw new Exception(Strings.HackerEquipment);

                this.EquipmentManager.EquipPhongCu(request.Id);
                break;

            case Game.LuyenKhi.EquipmentType.Jewelry:
                if (!isUnequip && !this.InventoryManager.TrangSucs.Any(x => x.SelfId == request.Id))
                    throw new Exception(Strings.HackerEquipment);

                this.EquipmentManager.EquipTrangSuc(request.Id); // Đã sửa thành EquipTrangSuc chuẩn bài!
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(request.TypeEquipment), "Loại trang bị không hợp lệ!");
        }

        return new EquipEquipmentResult()
        {
            Success = true,
            Id = request.Id,
            TypeEquipment = request.TypeEquipment,
        };
    }
}