using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using MTS.Data;
using MTS.Game;
using MTS.Game.ItemProvider;
using MTS.Game.Resources;
using MTS.Localization;
using MTS.Network.Models;
using Network;
using Network.Extensions;

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
    public int MoneyVip { get; set; } = 0;
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
                        .SetProperty(x => x.MoneyVip, MoneyVip));
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

        var cost = CalculateCostTuLuyen() * time.Days / 30;
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

        var buffExp = GetTotalValueBuffWithMultiplier(percentTuChat, 100, percentTamCanh);
        var exp = Progression.CurrentExp + buffExp;
        var levelUpdated = Progression.SetCurrentExp(exp);
        Stats.SetLinhThach(remaining);
        if (levelUpdated == null)
        {
            exp = Progression.CurrentExp;
            return new TuLuyenResult()
            {
                Success = true,
                CurrentExp = exp,
                RemainingMoney = remaining,
            };
        }

        return new TuLuyenResult()
        {
            Success = true,
            LevelUpdated = levelUpdated,
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

            var time = TimeSpan.FromDays(cp.baseTimeLearn);
            var provider = ItemProviderFactory.GetProvider<SkillProvider>(type, InventoryManager);
            var price = provider.GetPriceItemBuy(id);
            var money = Stats.LinhThach;
            var remaining = money - price / 2;
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

            return new LinhNgoResult()
            {
                Success = true,
                Id = id,
                Type = type,
                RemainingMoney = remaining,
                NewTuoiTac = newTuoiTac,
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

            var time = TimeSpan.FromDays(tt.baseTimeLearn);
            var provider = ItemProviderFactory.GetProvider<SkillProvider>(type, InventoryManager);
            var price = provider.GetPriceItemBuy(id);
            var money = Stats.LinhThach;
            var remaining = money - price / 2;
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

            return new LinhNgoResult()
            {
                Success = true,
                Id = id,
                Type = type,
                RemainingMoney = remaining,
                NewTuoiTac = newTuoiTac,
            };
        }

        throw new Exception(Strings.NotSupportItemType);
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
}