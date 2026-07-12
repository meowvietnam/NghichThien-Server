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
    public int CurrentFreeAdsMoneyVipCount { get; private set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public CharacterStats Stats { get; set; } = new();
    public Progression Progression { get; set; } = new();
    public Appearance Appearance { get; set; } = new();
    public EquipmentManager EquipmentManager { get; set; } = new();
    public Social Social { get; set; } = new();
    public InventoryManager InventoryManager { get; set; } = new();
    public QuestManager QuestManager { get; set; } = new();
    public ArenaManager ArenaManager { get; set; } = new();
    public ShopManager ShopManager { get; set; } = new();

    [ForeignKey(nameof(UserId))]
    [JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    [MemoryPackIgnore]
    public UserData User { get; set; }


    [JsonIgnore] [Newtonsoft.Json.JsonIgnore] [MemoryPackIgnore] [NotMapped]
    private bool dirty;

    [JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    [MemoryPackIgnore]
    [NotMapped]
    public Client Client { get; set; }

    [JsonIgnore] [Newtonsoft.Json.JsonIgnore] [MemoryPackIgnore] [NotMapped]
    private List<SpendingMoneyVip> addedSpendingMoneyVips = new();

    public void SetMonPhai(EMonPhai value)
    {
        if (MonPhai != value)
        {
            MonPhai = value;
            dirty = true;
        }
    }

    public void SetNoAds(bool value)
    {
        if (IsNoAds != value)
        {
            IsNoAds = value;
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

    public void SetCurrentFreeAdsMoneyVipCount(int value)
    {
        if (CurrentFreeAdsMoneyVipCount != value)
        {
            CurrentFreeAdsMoneyVipCount = value;
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
        CurrentFreeAdsMoneyVipCount = clone.CurrentFreeAdsMoneyVipCount;
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
        ArenaManager.CopyFrom(clone.ArenaManager);
        ShopManager.CopyFrom(clone.ShopManager);
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
               + CurrentFreeAdsMoneyVipCount.ByteSize()
               + InventoryManager.ByteSize()
               + CreatedAt.ByteSize()
               + Social.ByteSize()
               + QuestManager.ByteSize()
               + ArenaManager.ByteSize()
               + ShopManager.ByteSize();
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
        writer.Write(CurrentFreeAdsMoneyVipCount);
        writer.Write(InventoryManager);
        writer.Write(CreatedAt);
        writer.Write(Social);
        writer.Write(QuestManager);
        writer.Write(ArenaManager);
        writer.Write(ShopManager);
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
        value.CurrentFreeAdsMoneyVipCount = reader.Read<int>();
        value.InventoryManager = reader.Read<InventoryManager>();
        value.CreatedAt = reader.Read<DateTime>();
        value.Social = reader.Read<Social>();
        value.QuestManager = reader.Read<QuestManager>();
        value.ArenaManager = reader.Read<ArenaManager>();
        value.ShopManager = reader.Read<ShopManager>();
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
                        .SetProperty(x => x.CanFly, CanFly)
                        .SetProperty(x => x.CurrentFreeAdsMoneyVipCount, CurrentFreeAdsMoneyVipCount));
            dirty = false;
        }

        lock (addedSpendingMoneyVips)
        {
            if (addedSpendingMoneyVips.Count > 0)
            {
                db.SpendingMoneyVips.AddRange(addedSpendingMoneyVips);
                addedSpendingMoneyVips.Clear();
            }
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

        if (!ArenaManager.IsDefault())
        {
            await ArenaManager.SaveChanges(db);
        }

        if (!ShopManager.IsDefault())
        {
            await ShopManager.SaveChanges(db);
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
        if (time < TimeSpan.FromDays(30))
        {
            throw new Exception(Strings.RetreatDurationInvalid);
        }

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
        var percentAddHp = 0.1 * months;
        percentAddHp = Math.Clamp(percentAddHp, 0, 1);
        var buffExp = GetTotalValueBuffWithMultiplier(percentTuChat, 100, percentTamCanh);
        if (Stats.Ability.HasFlag(Ability.TienThienDaoThe))
        {
            // Nhân với 0.1f, làm tròn lên thành số nguyên, rồi cộng vào buffExp
            buffExp += (int)MathF.Ceiling(buffExp * 0.1f);
        }

        if (Stats.Ability.HasFlag(Ability.HuyenAmTuyetMach))
        {
            // Nhân với 0.1f, làm tròn lên thành số nguyên, rồi trừ đi từ buffExp
            buffExp -= (int)MathF.Ceiling(buffExp * 0.1f);
        }

        var oldExp = Progression.CurrentExp;
        var oldHp = Stats.Hp;
        var exp = oldExp + buffExp;
        Progression.SetCurrentExp(exp);
        var newHp = (int)Math.Ceiling(oldHp + Stats.SinhMenh * percentAddHp);
        newHp = Math.Clamp(newHp, 0, Stats.SinhMenh);
        Stats.SetLinhThach(remaining);
        Stats.SetTamCanh(newTamCanh);
        Stats.SetHp(newHp);
        return new TuLuyenResult()
        {
            Success = true,
            CurrentExp = Progression.CurrentExp,
            NewTamCanh = newTamCanh,
            RemainingMoney = remaining,
            NewHp = newHp,
            DeltaExp = buffExp,
            DeltaHp = newHp - oldHp
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
                if(ArenaManager.Points>400)
                {
                    newPointRank -= 15;

                    ArenaManager.SetPoints(newPointRank);
                }

            }

        }

        return new ResultCombatResult()
        {
            Success = true,
            NewHP = newHp,
            NewCamNgo = camNgoAdd.ToArray(),
            Reward = newReward,
            NewNgoDaoDiem = newNgoDaoDiem,
            TypeResultCombat = request.TypeResultCombat,
            NewPointRank = newPointRank
        };
    }

    public GachaResult? Gacha(GachaModel request)
    {
        RewardSnapshot reward = new RewardSnapshot();
        List<ItemRef> listItemRef = new List<ItemRef>();

        int require = GameConfig.PageGachaWithRequireValue[request.TypeGacha];
        int gachaCount = 1;
        if (request.IsGacha10)
        {
            require *= 10;
            gachaCount = 10;
        }

        if (this.MoneyVip < require)
        {
            Console.WriteLine("chó hack moneyvip");
            throw new Exception(Strings.NotEnoughMoney);
        }

        int newTienNgoc = this.MoneyVip - require;

        for (int i = 0; i < gachaCount; i++)
        {
            if (request.TypeGacha == Game.Gacha.ETypeGacha.ThanThong)
            {
                var item = RollThanThong();
                if (item != null)
                    listItemRef.Add(item);
            }
            else if (request.TypeGacha == Game.Gacha.ETypeGacha.CongPhap)
            {
                var item = RollCongPhap();
                if (item != null)
                    listItemRef.Add(item);
            }
            else if (request.TypeGacha == Game.Gacha.ETypeGacha.DanDuoc)
            {
                var item = RollDanDuoc();
                if (item != null)
                    listItemRef.Add(item);
            }
            else if (request.TypeGacha == Game.Gacha.ETypeGacha.DuocLieu)
            {
                var item = RollDuocLieu();
                if (item != null)
                    listItemRef.Add(item);
            }
            else if (request.TypeGacha == Game.Gacha.ETypeGacha.VatLieu)
            {
                var item = RollVatLieu();
                if (item != null)
                    listItemRef.Add(item);
            }
        }

        listItemRef = ConsolidateItemRefs(listItemRef);
        this.InventoryManager.AddToInventory(listItemRef);
        reward.ItemRefs = listItemRef;
        this.SetMoneyVip(newTienNgoc);
        AddSpendMoneyVip(require, SpendFor.Gacha);
        return new GachaResult()
        {
            Success = true,
            Reward = reward,
            NewTienNgoc = newTienNgoc,
        };
    }

    private List<ItemRef> ConsolidateItemRefs(List<ItemRef> items)
    {
        var consolidated = new Dictionary<(ItemType Type, ushort Id), ItemRef>();

        foreach (var item in items)
        {
            var key = (item.Type, item.Id);
            if (consolidated.TryGetValue(key, out var existingItem))
            {
                existingItem.Count = (ushort)(existingItem.Count + item.Count);
            }
            else
            {
                consolidated[key] = new ItemRef(item.Id, item.Type, item.Count);
            }
        }

        return consolidated.Values.ToList();
    }

    private ItemRef RollThanThong()
    {
        // DiaGiaiHa: 6%, DiaGiaiTrung: 5%, DiaGiaiThuong: 4% (total 15%)
        // ThienGiaiHa: 3%, ThienGiaiTrung: 2%, ThienGiaiThuong: 1% (total 6%)
        // Remaining 79% distributed among NhanGiaiHa, NhanGiaiTrung, NhanGiaiThuong
        // Constraint: NhanGiaiThuong < NhanGiaiTrung < NhanGiaiHa
        // NhanGiaiHa: 31.6%, NhanGiaiTrung: 26.35%, NhanGiaiThuong: 21.05% (6:5:4 ratio)

        double roll = Random.Shared.NextDouble() * 100;
        Game.BaseSkill.ETierSkill tier;

        if (roll < 6) tier = Game.BaseSkill.ETierSkill.DiaGiaiHa;
        else if (roll < 11) tier = Game.BaseSkill.ETierSkill.DiaGiaiTrung;
        else if (roll < 15) tier = Game.BaseSkill.ETierSkill.DiaGiaiThuong;
        else if (roll < 18) tier = Game.BaseSkill.ETierSkill.ThienGiaiHa;
        else if (roll < 20) tier = Game.BaseSkill.ETierSkill.ThienGiaiTrung;
        else if (roll < 21) tier = Game.BaseSkill.ETierSkill.ThienGiaiThuong;
        else if (roll < 52.6) tier = Game.BaseSkill.ETierSkill.NhanGiaiHa;
        else if (roll < 78.95) tier = Game.BaseSkill.ETierSkill.NhanGiaiTrung;
        else tier = Game.BaseSkill.ETierSkill.NhanGiaiThuong;

        // Try to get an item from the rolled tier, with fallback to higher tiers
        var tierOrder = new[]
        {
            tier, Game.BaseSkill.ETierSkill.NhanGiaiHa, Game.BaseSkill.ETierSkill.NhanGiaiTrung,
            Game.BaseSkill.ETierSkill.NhanGiaiThuong
        };
        foreach (var checkTier in tierOrder)
        {
            var validThanThongs = GameConfig.NDCPTTManager.ThanThongs
                .Where(x => x.eTierSkill == checkTier).ToList();

            if (validThanThongs.Count > 0)
            {
                var selected = validThanThongs[Random.Shared.Next(validThanThongs.Count)];
                return new ItemRef((ushort)selected.Id, ItemType.ThanThong, 1);
            }
        }

        return null;
    }

    private ItemRef RollCongPhap()
    {
        // DiaGiaiHa: 6%, DiaGiaiTrung: 5%, DiaGiaiThuong: 4% (total 15%)
        // ThienGiaiHa: 3%, ThienGiaiTrung: 2%, ThienGiaiThuong: 1% (total 6%)
        // Remaining 79% distributed among NhanGiaiHa, NhanGiaiTrung, NhanGiaiThuong
        // Constraint: NhanGiaiThuong < NhanGiaiTrung < NhanGiaiHa
        // NhanGiaiHa: 31.6%, NhanGiaiTrung: 26.35%, NhanGiaiThuong: 21.05% (6:5:4 ratio)

        double roll = Random.Shared.NextDouble() * 100;
        Game.BaseSkill.ETierSkill tier;

        if (roll < 6) tier = Game.BaseSkill.ETierSkill.DiaGiaiHa;
        else if (roll < 11) tier = Game.BaseSkill.ETierSkill.DiaGiaiTrung;
        else if (roll < 15) tier = Game.BaseSkill.ETierSkill.DiaGiaiThuong;
        else if (roll < 18) tier = Game.BaseSkill.ETierSkill.ThienGiaiHa;
        else if (roll < 20) tier = Game.BaseSkill.ETierSkill.ThienGiaiTrung;
        else if (roll < 21) tier = Game.BaseSkill.ETierSkill.ThienGiaiThuong;
        else if (roll < 52.6) tier = Game.BaseSkill.ETierSkill.NhanGiaiHa;
        else if (roll < 78.95) tier = Game.BaseSkill.ETierSkill.NhanGiaiTrung;
        else tier = Game.BaseSkill.ETierSkill.NhanGiaiThuong;

        // Try to get an item from the rolled tier, with fallback to higher tiers
        var tierOrder = new[]
        {
            tier, Game.BaseSkill.ETierSkill.NhanGiaiHa, Game.BaseSkill.ETierSkill.NhanGiaiTrung,
            Game.BaseSkill.ETierSkill.NhanGiaiThuong
        };
        foreach (var checkTier in tierOrder)
        {
            var validCongPhaps = GameConfig.NDCPTTManager.CongPhaps
                .Where(x => x.eTierSkill == checkTier).ToList();

            if (validCongPhaps.Count > 0)
            {
                var selected = validCongPhaps[Random.Shared.Next(validCongPhaps.Count)];
                return new ItemRef((ushort)selected.Id, ItemType.CongPhap, 1);
            }
        }

        return null;
    }

    private ItemRef RollDanDuoc()
    {
        // LinhDan: 5% Lục, 7% Ngũ, 10% Tứ
        // Recipe: 2% Lục, 5% Ngũ, 7% Tứ
        // Furnace: 1% Lục, 3% Ngũ, 5% Tứ
        // Constraint: Tâm < Nhị < Nhất (always), LinhDan > Recipe > Furnace (always)

        double roll = Random.Shared.NextDouble() * 100;
        ItemType type;
        MTS.Game.LuyenDan.Tier tier;

        // First determine tier - now extends to NhatPham and NhiPham
        // Constraint: Tâm < Nhị < Nhất with ratio 4:5:6
        if (roll < 8) tier = MTS.Game.LuyenDan.Tier.LucPham; // 5% + 2% + 1% = 8%
        else if (roll < 23) tier = MTS.Game.LuyenDan.Tier.NguPham; // 7% + 5% + 3% = 15%, cumulative 23%
        else if (roll < 42) tier = MTS.Game.LuyenDan.Tier.TuPham; // 10% + 7% + 5% = 22%, cumulative 42%
        else if (roll < 56.33) tier = MTS.Game.LuyenDan.Tier.TamPham; // 14.33% for Tam (4 out of 15)
        else if (roll < 74.33) tier = MTS.Game.LuyenDan.Tier.NhiPham; // 18% for Nhi (5 out of 15)
        else tier = MTS.Game.LuyenDan.Tier.NhatPham; // 25.67% for Nhat (6 out of 15)

        // Now determine which type based on the tier
        if (tier == MTS.Game.LuyenDan.Tier.LucPham)
        {
            // LinhDan: 5%, Recipe: 2%, Furnace: 1% -> LinhDan: 62.5%, Recipe: 25%, Furnace: 12.5%
            double typeRoll = Random.Shared.NextDouble() * 100;
            if (typeRoll < 62.5) type = ItemType.LinhDan;
            else if (typeRoll < 87.5) type = ItemType.Recipe;
            else type = ItemType.Furnace;
        }
        else if (tier == MTS.Game.LuyenDan.Tier.NguPham)
        {
            // LinhDan: 7%, Recipe: 5%, Furnace: 3% -> LinhDan: 46.67%, Recipe: 33.33%, Furnace: 20%
            double typeRoll = Random.Shared.NextDouble() * 100;
            if (typeRoll < 46.67) type = ItemType.LinhDan;
            else if (typeRoll < 80) type = ItemType.Recipe;
            else type = ItemType.Furnace;
        }
        else if (tier == MTS.Game.LuyenDan.Tier.TuPham)
        {
            // LinhDan: 10%, Recipe: 7%, Furnace: 5% -> LinhDan: 45.45%, Recipe: 31.82%, Furnace: 22.73%
            double typeRoll = Random.Shared.NextDouble() * 100;
            if (typeRoll < 45.45) type = ItemType.LinhDan;
            else if (typeRoll < 77.27) type = ItemType.Recipe;
            else type = ItemType.Furnace;
        }
        else // TamPham, NhiPham, NhatPham with constraint: Tâm < Nhị < Nhất, LinhDan > Recipe > Furnace
        {
            // Distribute with ratio LinhDan:Recipe:Furnace = 3:2:1
            double typeRoll = Random.Shared.NextDouble() * 100;
            if (typeRoll < 50) type = ItemType.LinhDan;
            else if (typeRoll < 83.33) type = ItemType.Recipe;
            else type = ItemType.Furnace;
        }

        return RollItemByTypeAndTier(type, tier);
    }

    private ItemRef RollDuocLieu()
    {
        // Herb: 2% Lục, 5% Ngũ, 10% Tứ, then random in Tâm, Nhị, Nhất
        // Recipe: Only 2% Nhị and 5% Nhất (no drops in Lục, Ngũ, Tứ, Tâm)
        // Furnace: Only 2% Nhị and 5% Nhất (no drops in Lục, Ngũ, Tứ, Tâm)
        // Constraint: Recipe < Furnace < Herb (always)

        double roll = Random.Shared.NextDouble() * 100;
        ItemType type;
        MTS.Game.LuyenDan.Tier tier;

        // First determine tier - now extends to NhatPham and NhiPham
        // Lục, Ngũ, Tứ only drop Herb. Tâm drops only Herb. Nhị and Nhất can drop Recipe and Furnace
        if (roll < 2) tier = MTS.Game.LuyenDan.Tier.LucPham; // 2% Herb
        else if (roll < 7) tier = MTS.Game.LuyenDan.Tier.NguPham; // 5% Herb, cumulative 7%
        else if (roll < 17) tier = MTS.Game.LuyenDan.Tier.TuPham; // 10% Herb, cumulative 17%
        else if (roll < 31.33) tier = MTS.Game.LuyenDan.Tier.TamPham; // 14.33% Herb (4 out of 15)
        else if (roll < 49.33)
            tier = MTS.Game.LuyenDan.Tier.NhiPham; // 18% for Nhi (5 out of 15) - includes Recipe 2% + Furnace 2%
        else tier = MTS.Game.LuyenDan.Tier.NhatPham; // 50.67% for Nhat (6 out of 15) - includes Recipe 5% + Furnace 5%

        // Now determine which type based on the tier
        if (tier == MTS.Game.LuyenDan.Tier.LucPham)
        {
            // Lục: 100% Herb
            type = ItemType.Herb;
        }
        else if (tier == MTS.Game.LuyenDan.Tier.NguPham)
        {
            // Ngũ: 100% Herb
            type = ItemType.Herb;
        }
        else if (tier == MTS.Game.LuyenDan.Tier.TuPham)
        {
            // Tứ: 100% Herb
            type = ItemType.Herb;
        }
        else if (tier == MTS.Game.LuyenDan.Tier.TamPham)
        {
            // Tâm: 100% Herb
            type = ItemType.Herb;
        }
        else if (tier == MTS.Game.LuyenDan.Tier.NhiPham)
        {
            // Nhị: Herb (14%), Recipe (2%), Furnace (2%) -> Herb: 77.78%, Recipe: 11.11%, Furnace: 11.11%
            double typeRoll = Random.Shared.NextDouble() * 100;
            if (typeRoll < 77.78) type = ItemType.Herb;
            else if (typeRoll < 88.89) type = ItemType.Recipe;
            else type = ItemType.Furnace;
        }
        else // NhatPham
        {
            // Nhất: Herb (40.67%), Recipe (5%), Furnace (5%) -> Herb: 80.26%, Recipe: 9.87%, Furnace: 9.87%
            double typeRoll = Random.Shared.NextDouble() * 100;
            if (typeRoll < 80.26) type = ItemType.Herb;
            else if (typeRoll < 90.13) type = ItemType.Recipe;
            else type = ItemType.Furnace;
        }

        return RollItemByTypeAndTier(type, tier);
    }

    private ItemRef RollVatLieu()
    {
        // Material: 2% Lục, 5% Ngũ, 10% Tứ (total 17%)
        // Remaining 83% distributed among Tâm, Nhị, Nhất with constraint: Tâm < Nhị < Nhất

        double roll = Random.Shared.NextDouble() * 100;
        MTS.Game.LuyenDan.Tier tier;

        if (roll < 2) tier = MTS.Game.LuyenDan.Tier.LucPham; // 2%
        else if (roll < 7) tier = MTS.Game.LuyenDan.Tier.NguPham; // 5%, cumulative 7%
        else if (roll < 17) tier = MTS.Game.LuyenDan.Tier.TuPham; // 10%, cumulative 17%
        else if (roll < 31.33) tier = MTS.Game.LuyenDan.Tier.TamPham; // 14.33% for Tam (4 out of 15)
        else if (roll < 49.33) tier = MTS.Game.LuyenDan.Tier.NhiPham; // 18% for Nhi (5 out of 15)
        else tier = MTS.Game.LuyenDan.Tier.NhatPham; // 50.67% for Nhat (6 out of 15)

        return RollItemByTypeAndTier(ItemType.Material, tier);
    }


    private ItemRef RollItemByTypeAndTier(ItemType type, MTS.Game.LuyenDan.Tier tier)
    {
        // Fallback tier order - try higher tiers if current tier has no items
        var allTiers = new[]
        {
            MTS.Game.LuyenDan.Tier.NhatPham, MTS.Game.LuyenDan.Tier.NhiPham, MTS.Game.LuyenDan.Tier.TamPham,
            MTS.Game.LuyenDan.Tier.TuPham, MTS.Game.LuyenDan.Tier.NguPham, MTS.Game.LuyenDan.Tier.LucPham
        };
        var tierIndex = System.Array.IndexOf(allTiers, tier);
        var tierOrder = allTiers.Skip(tierIndex).ToArray();

        foreach (var checkTier in tierOrder)
        {
            if (type == ItemType.LinhDan)
            {
                var validItems = GameConfig.LuyenDanResources.LinhDans
                    .Where(x => x.Tier == checkTier).ToList();
                if (validItems.Count > 0)
                {
                    var selected = validItems[Random.Shared.Next(validItems.Count)];
                    return new ItemRef((ushort)selected.Id, ItemType.LinhDan, 1);
                }
            }
            else if (type == ItemType.Herb)
            {
                var validItems = GameConfig.LuyenDanResources.HerbDatas
                    .Where(x => x.Tier == checkTier).ToList();
                if (validItems.Count > 0)
                {
                    var selected = validItems[Random.Shared.Next(validItems.Count)];
                    return new ItemRef((ushort)selected.Id, ItemType.Herb, 1);
                }
            }
            else if (type == ItemType.Recipe)
            {
                var validItems = GameConfig.LuyenDanResources.Recipes
                    .Where(x => x.Tier == checkTier).ToList();
                if (validItems.Count > 0)
                {
                    var selected = validItems[Random.Shared.Next(validItems.Count)];
                    return new ItemRef((ushort)selected.Id, ItemType.Recipe, 1);
                }
            }
            else if (type == ItemType.Furnace)
            {
                var validItems = GameConfig.LuyenDanResources.TempFurnaceDatas
                    .Where(x => x.Tier == checkTier).ToList();
                if (validItems.Count > 0)
                {
                    var selected = validItems[Random.Shared.Next(validItems.Count)];
                    return new ItemRef((ushort)selected.Id, ItemType.Furnace, 1);
                }
            }
            else if (type == ItemType.Material)
            {
                var validItems = GameConfig.LuyenKhiResources.Materials
                    .Where(x => x.Tier == checkTier).ToList();
                if (validItems.Count > 0)
                {
                    var selected = validItems[Random.Shared.Next(validItems.Count)];
                    return new ItemRef((ushort)selected.Id, ItemType.Material, 1);
                }
            }
        }

        return null;
    }

    public TangQuaNpcResult? TangQuaNpc(TangQuaNpc request)
    {
        var itemRef = request.ItemRef;
        if (itemRef.Count == 0)
        {
            throw new Exception(Strings.GiftItemQuantityInvalid);
        }

        var isUniqueItem = itemRef.Type is ItemType.Furnace
            or ItemType.VuKhi
            or ItemType.PhongCu
            or ItemType.TrangSuc;
        if (isUniqueItem && itemRef.Count != 1)
        {
            throw new Exception(Strings.GiftItemQuantityInvalid);
        }

        var npcRef = GameConfig.NpcDatas.FirstOrDefault(x => x.CharStats.Id == request.IdChar);
        if (npcRef == null)
        {
            Console.WriteLine("npc k tồn tại");

            throw new Exception(Strings.NpcNotExist);
        }

        var value = ItemProviderFactory.GetProvider(request.ItemRef.Type, InventoryManager)
            .ValueTangQuaNPCWithItem(request.ItemRef.Id, npcRef.CharStats.Progression.Level);
        if (value.Item1 < 0)
        {
            Console.WriteLine("value add âm");

            throw new Exception(Strings.NpcGiftValueInvalid);
        }

        int valueAdd = value.Item1 * request.ItemRef.Count * value.Item2 * 2;
        bool isHasItem = false;
        if (request.ItemRef.Type == ItemType.Herb)
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
            var congPhap = InventoryManager.CongPhaps.FirstOrDefault(x => x.Id == request.ItemRef.Id);
            if (congPhap != null && congPhap.Count >= request.ItemRef.Count)
            {
                isHasItem = true;
            }
        }
        else if (request.ItemRef.Type == ItemType.Furnace)
        {
            if (InventoryManager.Furnaces.FirstOrDefault(x => x.SelfId == request.ItemRef.Id) != null)
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

        ushort vuKhiEquip = EquipmentManager.VuKhi;
        ushort phongCuEquip = EquipmentManager.PhongCu;
        ushort trangSucEquip = EquipmentManager.TrangSuc;

        if (request.ItemRef.Type == ItemType.VuKhi && request.ItemRef.Id == vuKhiEquip)
        {
            Console.WriteLine("HackerTangQuaNPC tặng quà khi đang trang bị vũ khí");
            throw new Exception(Strings.HackerTangQuaNPC);
        }
        else if (request.ItemRef.Type == ItemType.PhongCu && request.ItemRef.Id == phongCuEquip)
        {
            Console.WriteLine("HackerTangQuaNPC tặng quà khi đang trang bị vũ khí");
            throw new Exception(Strings.HackerTangQuaNPC);
        }
        else if (request.ItemRef.Type == ItemType.TrangSuc && request.ItemRef.Id == trangSucEquip)
        {
            Console.WriteLine("HackerTangQuaNPC tặng quà khi đang trang bị vũ khí");
            throw new Exception(Strings.HackerTangQuaNPC);
        }

        if (request.ItemRef.Type == ItemType.Furnace)
        {
            InventoryManager.RemoveFurnace((ushort)request.ItemRef.Id);
        }
        else if (request.ItemRef.Type == ItemType.VuKhi || request.ItemRef.Type == ItemType.PhongCu ||
                 request.ItemRef.Type == ItemType.TrangSuc)
        {
            InventoryManager.RemoveEquipment((ushort)request.ItemRef.Id);
        }
        else
        {
            InventoryManager.Add(request.ItemRef, -request.ItemRef.Count);
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
            Console.WriteLine("npc k tồn tại");
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

        ERelationShip GetIndexSkillThinhGiao()
        {
            ERelationShip indexThinhGiao = ERelationShip.Quen;

            if (currentSkillCategorize.Item1.Contains(request.IdSkill))
            {
                indexThinhGiao = ERelationShip.Quen;
            }
            else if (currentSkillCategorize.Item2.Contains(request.IdSkill))
            {
                indexThinhGiao = ERelationShip.BanBe;
            }
            else if (currentSkillCategorize.Item3.Contains(request.IdSkill))
            {
                indexThinhGiao = ERelationShip.ThanThiet;
            }

            return indexThinhGiao;
        }

        if (request.TypeSkill == ItemType.ThanThong)
        {
            currentRelationType = GameConfig.GetERelationShip(currentRelationshipValue, true);
            currentSkillCategorize = npcRef.GetCategorizeThinhGiaoThanThong();
            itemRefSkill = new ItemRef
            {
                Type = ItemType.ThanThong,
                Id = (ushort)request.IdSkill,
                Count = 1
            };
            requireRelationShip = GameConfig.relationShipRequireThanThong[GetIndexSkillThinhGiao()];
        }
        else if (request.TypeSkill == ItemType.CongPhap)
        {
            currentRelationType = GameConfig.GetERelationShip(currentRelationshipValue, true);
            currentSkillCategorize = npcRef.GetCategorizeThinhGiaoCongPhap();
            itemRefSkill = new ItemRef
            {
                Type = ItemType.CongPhap,
                Id = (ushort)request.IdSkill,
                Count = 1
            };
            requireRelationShip = GameConfig.relationShipRequireCongPhap[GetIndexSkillThinhGiao()];
        }
        else
        {
            Console.WriteLine("NotSupportItemType");

            throw new Exception(Strings.NotSupportItemType);
        }

        int newValueRelationship = Social.GetRelationship((ushort)request.IdChar).Points - requireRelationShip;

        if (currentRelationType != ERelationShip.None)
        {
            List<int> baseSkillsCheck = new List<int>();
            if ((int)currentRelationType >= (int)ERelationShip.Quen)
            {
                baseSkillsCheck.AddRange(currentSkillCategorize.Item1);
            }

            if ((int)currentRelationType >= (int)ERelationShip.BanBe)
            {
                baseSkillsCheck.AddRange(currentSkillCategorize.Item2);
            }

            if ((int)currentRelationType >= (int)ERelationShip.ThanThiet)
            {
                baseSkillsCheck.AddRange(currentSkillCategorize.Item3);
            }

            if (!baseSkillsCheck.Contains(request.IdSkill))
            {
                Console.WriteLine("HackerSkillNotExistOnNpc");

                throw new Exception(Strings.HackerSkillNotExistOnNpc);
            }

            InventoryManager.AddToInventory(new List<ItemRef> { itemRefSkill });
            Social.RelationshipAddValue((ushort)request.IdChar, -requireRelationShip);
            // giảm độ hảo cảm của npc đó với user giảm đi requireRelationShip
        }
        else
        {
            Console.WriteLine("sai thiết kế client");

            throw new Exception(Strings.NpcSkillRequestInvalid);
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
                if (!isUnequip && !InventoryManager.VuKhis.Any(x => x.SelfId == request.Id))
                    throw new Exception(Strings.HackerEquipment);

                EquipmentManager.EquipVuKhi(request.Id);
                break;

            case Game.LuyenKhi.EquipmentType.Armor:
                if (!isUnequip && !InventoryManager.PhongCus.Any(x => x.SelfId == request.Id))
                    throw new Exception(Strings.HackerEquipment);

                EquipmentManager.EquipPhongCu(request.Id);
                break;

            case Game.LuyenKhi.EquipmentType.Jewelry:
                if (!isUnequip && !InventoryManager.TrangSucs.Any(x => x.SelfId == request.Id))
                    throw new Exception(Strings.HackerEquipment);

                EquipmentManager.EquipTrangSuc(request.Id); // Đã sửa thành EquipTrangSuc chuẩn bài!
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

    public async Task<IapResult> IapPurchase(IapModel request)
    {
        var store = request.Store;
        var productId = request.ProductId;
        var purchaseToken = request.PurchaseToken;
        var purchaseTokenHash = Utils.Utils.HashSHA256(purchaseToken);
        await using var db = await Program.CreateDbAsync();

        var hasPurchased = await db.IapPurchases
            .AnyAsync(x => x.Store == store
                           && x.PurchaseTokenHash == purchaseTokenHash);
        if (hasPurchased)
        {
            throw new Exception(Strings.IapPurchaseAlreadyProcessed);
        }

        var product = await db.IapProducts
            .FirstAsync(x => x.Store == store
                             && x.StoreProductId == productId
                             && x.IsActive);
        var cancelToken = CancellationToken.None;
        if (store == Store.GooglePlay)
        {
            var verify = await GooglePlayIap.VerifyAsync(productId, purchaseToken, cancelToken);
            if (!verify.IsValid)
            {
                throw new Exception(verify.Error);
            }

            var productType = product.ProductType;
            if (productType == IapProductType.Consumable)
            {
                await GooglePlayIap.ConsumeAsync(productId, purchaseToken, cancelToken);
            }
            else if (productType == IapProductType.NonConsumable)
            {
                await GooglePlayIap.AcknowledgeAsync(productId, purchaseToken, cancelToken);
            }

            var purchase = new IapPurchase()
            {
                CharId = Id,
                ProductId = product.Id,
                Store = store,
                PurchaseTokenHash = purchaseTokenHash,
            };
            var reward = product.Reward;
            reward.Execute(this);
            reward.Edit(purchase);
            await db.IapPurchases.AddAsync(purchase, cancelToken);
            await db.SaveChangesAsync(cancelToken);
            return new IapResult()
            {
                Success = true,
                Store = store,
                ProductId = productId,
                Reward = product.Reward,
            };
        }

        throw new Exception($"{Strings.IapStoreNotSupported}: {store}");
    }

    public async Task<List<OwnerIapProduct>> GetProducts()
    {
        await using var db = await Program.CreateDbAsync();
        var iapProducts = await db.IapProducts.Where(x => x.IsActive).ToArrayAsync();
        var ownerProducts = iapProducts
            .Select(x =>
            {
                var hasPurchased = db.IapPurchases
                    .Any(p => p.CharId == Id && p.Store == x.Store && p.ProductId == x.Id);
                return new OwnerIapProduct()
                {
                    Product = x,
                    HasPurchased = hasPurchased
                };
            }).ToList();
        return ownerProducts;
    }

    public ExchangeMoneyVipResult ExchangeMoneyVipForLinhThach(int amount)
    {
        if (amount <= 0 || amount > MoneyVip)
        {
            throw new Exception(Strings.MoneyVipInvalid);
        }

        var linhThach = Stats.LinhThach + amount * 100;
        Stats.SetLinhThach(linhThach);
        var moneyVip = MoneyVip - amount;
        SetMoneyVip(moneyVip);
        return new ExchangeMoneyVipResult()
        {
            Success = true,
            LinhThach = linhThach,
            MoneyVip = moneyVip
        };
    }

    public void BuffDan(ItemRef linhDanRef)
    {
        var id = linhDanRef.Id;
        var count = linhDanRef.Count;

        if (count == 0)
        {
            throw new Exception(Strings.LinhDanQuantityInvalid);
        }
        var linhDan = GameConfig.LuyenDanResources.LinhDans.FirstOrDefault(x => x.Id == id);
        if (linhDan == null)
        {
            throw new Exception(Strings.LinhDanNotFound);
        }

        if (linhDan.ETypeDan == ETypeDan.DanCombat)
        {
            throw new Exception(Strings.LinhDanCombatBuffInvalid);
        }
        var buffType = linhDan.EBuffType;
        if (buffType != EDanBuffType.None)
        {
            bool isTuViType = buffType switch
            {
                EDanBuffType.TuVi => true,
                EDanBuffType.TuViVaDanDocDuoiTrucCo => true,
                EDanBuffType.TuViVaDanDocDuoiKetDan => true,
                EDanBuffType.TuViVaDanDocDuoiNguyenAnh => true,
                EDanBuffType.TuViVaDanDocDuoiHoaThan => true,
                _ => false
            };
            if (isTuViType && Stats.Ability.HasFlag(Ability.TienThienDiMach))
            {
                throw new Exception(Strings.HasFlagTienThienDiMach);
            }
            Stats.Buff(linhDan, count);
            Stats.ClampHp();
        }
    }
    public UseDanResult UseDan(UseDanRequest request)
    {
        var id = request.Id;
        var count = request.Count;

        if (count == 0)
        {
            throw new Exception(Strings.LinhDanQuantityInvalid);
        }

        var linhDan = GameConfig.LuyenDanResources.LinhDans.FirstOrDefault(x => x.Id == id);
        if (linhDan == null)
        {
            throw new Exception(Strings.LinhDanNotFound);
        }

        var type = linhDan.ETypeDan;
        var level = Progression.Level;
        var danCombatUseds = EquipmentManager.DanCombatUseds;
        if (type == ETypeDan.DanCombat)
        {
            var danCombatRef = new DanCombatUsed()
            {
                Id = id
            };
            if (danCombatUseds.TryGetValue(danCombatRef, out var used))
            {
                if (used.Count + count > level)
                {
                    throw new Exception(Strings.DanCombatLimitExceeded);
                }
            }
        }

        var linhDans = InventoryManager.LinhDans;
        var linhDanRef = new ItemRef()
        {
            Id = id,
            Type = ItemType.LinhDan
        };
        if (!linhDans.TryGetValue(linhDanRef, out var item))
        {
            throw new Exception(Strings.InventoryItemNotFound);
        }

        var remaining = item.Count - count;
        if (remaining < 0)
        {
            throw new Exception(Strings.InventoryItemQuantityInvalid);
        }

        if (type == ETypeDan.DanCombat)
        {
            var danCombatRef = new DanCombatUsed()
            {
                Id = id
            };
            if (danCombatUseds.TryGetValue(danCombatRef, out var used))
            {
                used.Count += count;
                EquipmentManager.Update(used);
            }
            else
            {
                danCombatRef.Count = count;
                EquipmentManager.Add(danCombatRef);
            }
        }

        InventoryManager.Decrease(new ItemRef()
        {
            Type = ItemType.LinhDan,
            Id = id,
            Count = count,
        });
        var buffType = linhDan.EBuffType;
        StatsSnapshot? snapshot = null;

        if (buffType != EDanBuffType.None)
        {
            bool isTuViType = buffType switch
            {
                EDanBuffType.TuVi => true,
                EDanBuffType.TuViVaDanDocDuoiTrucCo => true,
                EDanBuffType.TuViVaDanDocDuoiKetDan => true,
                EDanBuffType.TuViVaDanDocDuoiNguyenAnh => true,
                EDanBuffType.TuViVaDanDocDuoiHoaThan => true,
                _ => false
            };

            if (isTuViType && Stats.Ability.HasFlag(Ability.TienThienDiMach))
            {
                throw new Exception(Strings.HasFlagTienThienDiMach);
            }

            Stats.Buff(linhDan, count);
            Stats.ClampHp();
            snapshot = new StatsSnapshot(Stats);
        }

        var result = new UseDanResult()
        {
            Success = true,
            Stats = snapshot,
        };
        return result;
    }

    private bool IsOnline()
    {
        return Client != null;
    }

    public async void AddSpendMoneyVip(long moneyVip, SpendFor spendFor)
    {
        var spendingMoneyVip = new SpendingMoneyVip
        {
            CharId = Id,
            SpendFor = spendFor,
            MoneyVip = moneyVip,
        };
        if (IsOnline())
        {
            addedSpendingMoneyVips.Add(spendingMoneyVip);
        }
        else
        {
            await using var db = await Program.CreateDbAsync();
            await db.SpendingMoneyVips.AddAsync(spendingMoneyVip);
            await db.SaveChangesAsync();
        }
    }

    public async Task<long> GetTongNap()
    {
        await using var db = await Program.CreateDbAsync();
        return await db.IapPurchases
            .Where(x => x.CharId == Id)
            .SumAsync(x => x.MoneyVipSnapshot);
    }

    public async Task<long> GetTongTieu()
    {
        await using var db = await Program.CreateDbAsync();
        return await db.SpendingMoneyVips
            .Where(x => x.CharId == Id)
            .SumAsync(x => x.MoneyVip);
    }
}
