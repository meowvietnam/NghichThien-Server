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
        var cpRef = new CongPhap()
        {
            Id = value,
        };
        if (!InventoryManager.CongPhapOwners.TryGetValue(cpRef, out var congPhapOwner))
        {
            throw new Exception(Strings.CongPhapNotOwned);
        }

        congPhapOwner.GetCongPhap().UnEquipCongPhap(this, congPhapOwner.Level);
        EquipmentManager.SetSlotCP(slot, ushort.MaxValue);
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
        var newTamCanh = Stats.TamCanh - months;
        if (newTamCanh < CharacterStats.GetMinTamCanh())
        {
            throw new Exception(Strings.TamCanhBelowMinimum);
        }
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

        var percentAddHp = 0.1 * months;
        percentAddHp = Math.Clamp(percentAddHp, 0, 1);
        var buffExp = GetTotalValueBuffWithMultiplier(percentTuChat, 100, percentTamCanh) * months;
        if (Stats.Ability.HasFlag(Ability.TienThienDaoThe))
        {
            buffExp += (int)MathF.Ceiling(buffExp * 0.1f);
        }

        if (Stats.Ability.HasFlag(Ability.HuyenAmTuyetMach))
        {
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
}
