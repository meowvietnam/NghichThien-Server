using MTS.Exceptions;
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
using Serilog;

namespace MTS.Models;

public partial class CharacterData
{
    public TangQuaNpcResult? TangQuaNpc(TangQuaNpc request)
    {
        var itemRef = request.ItemRef;
        if (itemRef.Count == 0)
        {
            throw new InvalidException(Strings.GiftItemQuantityInvalid);
        }

        var isUniqueItem = itemRef.Type is ItemType.Furnace
            or ItemType.VuKhi
            or ItemType.PhongCu
            or ItemType.TrangSuc;
        if (isUniqueItem && itemRef.Count != 1)
        {
            throw new InvalidException(Strings.GiftItemQuantityInvalid);
        }

        var npcRef = GameConfig.NpcDatas.FirstOrDefault(x => x.CharStats.Id == request.IdChar);
        if (npcRef == null)
        {
            Log.Warning("NPC gift rejected: NPC not found; CharacterId={CharacterId}, NpcId={NpcId}",
                Id, request.IdChar);
            throw new InvalidException(Strings.NpcNotExist);
        }

        var value = ItemProviderFactory.GetProvider(request.ItemRef.Type, InventoryManager)
            .ValueTangQuaNPCWithItem(request.ItemRef.Id, npcRef.CharStats.Progression.Level);
        if (value.Item1 < 0)
        {
            Log.Warning("NPC gift rejected: invalid gift value; CharacterId={CharacterId}, NpcId={NpcId}, ItemType={ItemType}, ItemId={ItemId}, Count={Count}, Value={Value}",
                Id, request.IdChar, request.ItemRef.Type, request.ItemRef.Id, request.ItemRef.Count, value.Item1);
            throw new InvalidException(Strings.NpcGiftValueInvalid);
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
            Log.Warning("NPC gift rejected: item not owned or not enough quantity; CharacterId={CharacterId}, NpcId={NpcId}, ItemType={ItemType}, ItemId={ItemId}, Count={Count}",
                Id, request.IdChar, request.ItemRef.Type, request.ItemRef.Id, request.ItemRef.Count);
            throw new InvalidException(Strings.HackerTangQuaNPC);
        }

        ushort vuKhiEquip = EquipmentManager.VuKhi;
        ushort phongCuEquip = EquipmentManager.PhongCu;
        ushort trangSucEquip = EquipmentManager.TrangSuc;

        if (request.ItemRef.Type == ItemType.VuKhi && request.ItemRef.Id == vuKhiEquip || request.ItemRef.Type == ItemType.PhongCu && request.ItemRef.Id == phongCuEquip || request.ItemRef.Type == ItemType.TrangSuc && request.ItemRef.Id == trangSucEquip)
        {
            Log.Warning("NPC gift rejected: equipped item cannot be gifted; CharacterId={CharacterId}, NpcId={NpcId}, ItemType={ItemType}, ItemId={ItemId}",
                Id, request.IdChar, request.ItemRef.Type, request.ItemRef.Id);
            throw new InvalidException(Strings.HackerTangQuaNPC);
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
            Log.Warning("NPC skill request rejected: NPC not found; CharacterId={CharacterId}, NpcId={NpcId}, SkillType={SkillType}, SkillId={SkillId}",
                Id, request.IdChar, request.TypeSkill, request.IdSkill);
            throw new InvalidException(Strings.NpcNotExist);
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
            Log.Warning("NPC skill request rejected: unsupported skill item type; CharacterId={CharacterId}, NpcId={NpcId}, SkillType={SkillType}, SkillId={SkillId}",
                Id, request.IdChar, request.TypeSkill, request.IdSkill);
            throw new InvalidException(Strings.NotSupportItemType);
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
                Log.Warning("NPC skill request rejected: skill not available on NPC; CharacterId={CharacterId}, NpcId={NpcId}, SkillType={SkillType}, SkillId={SkillId}, Relationship={Relationship}",
                    Id, request.IdChar, request.TypeSkill, request.IdSkill, currentRelationType);
                throw new InvalidException(Strings.HackerSkillNotExistOnNpc);
            }

            InventoryManager.AddToInventory(new List<ItemRef> { itemRefSkill });
            Social.RelationshipAddValue((ushort)request.IdChar, -requireRelationShip);
            // giảm độ hảo cảm của npc đó với user giảm đi requireRelationShip
        }
        else
        {
            Log.Warning("NPC skill request rejected: relationship is not enough; CharacterId={CharacterId}, NpcId={NpcId}, SkillType={SkillType}, SkillId={SkillId}, RelationshipValue={RelationshipValue}",
                Id, request.IdChar, request.TypeSkill, request.IdSkill, currentRelationshipValue);
            throw new InvalidException(Strings.NpcSkillRequestInvalid);
        }


        return new ThinhGiaoKyNangResult()
        {
            Success = true,
            IdChar = request.IdChar,
            ItemRefSkill = itemRefSkill,
            NewValueRelationship = newValueRelationship,
        };
    }
}
