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

namespace MTS.Models;

public partial class CharacterData
{
    public EquipEquipmentResult? EquipEquipment(EquipEquipment request)
    {
        bool isUnequip = request.Id == ushort.MaxValue;

        switch (request.TypeEquipment)
        {
            case Game.LuyenKhi.EquipmentType.Weapon:
                // Nếu không phải tháo đồ, check xem trong hòm có món này không
                if (!isUnequip && !InventoryManager.VuKhis.Any(x => x.SelfId == request.Id))
                    throw new InvalidException(Strings.HackerEquipment);

                EquipmentManager.EquipVuKhi(request.Id);
                break;

            case Game.LuyenKhi.EquipmentType.Armor:
                if (!isUnequip && !InventoryManager.PhongCus.Any(x => x.SelfId == request.Id))
                    throw new InvalidException(Strings.HackerEquipment);

                EquipmentManager.EquipPhongCu(request.Id);
                break;

            case Game.LuyenKhi.EquipmentType.Jewelry:
                if (!isUnequip && !InventoryManager.TrangSucs.Any(x => x.SelfId == request.Id))
                    throw new InvalidException(Strings.HackerEquipment);

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
}
