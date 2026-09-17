using System.Collections.Generic;
using MemoryPack;
using MTS.Game;
using MTS.Models;
using MTS.Game.LuyenKhi;

namespace MTS.Network.Models;
[MemoryPackable]
public partial class ClanConfig
{
    // Giá tạo tông môn nếu trả bằng Tiên Ngọc (MoneyVip).
    public int CreateClanMoneyVipPrice { get; set; }
    // Giá tạo tông môn nếu trả bằng VNĐ (nạp thật).
    public int CreateClanVndPrice { get; set; }
    // Tiên Ngọc thưởng thêm khi tạo tông môn bằng VNĐ.
    public int CreateClanVndBonusMoneyVip { get; set; }
    // Số slot thành viên ban đầu khi mới lập tông môn.
    public int CreateMaxMember { get; set; }
    // Cứ mỗi bao nhiêu cấp tông môn thì mở thêm slot thành viên.
    public int MemberSlotLevelInterval { get; set; } = 1;
    // Mỗi lần đạt mốc cấp trên thì cộng thêm bao nhiêu slot thành viên.
    public int MemberSlotsPerInterval { get; set; } = 5;
    // Cấp giới hạn Hồn khởi tạo cho tông môn mới (dùng để tính MaxHon ban đầu).
    public int CreateClanHonLimitLevel { get; set; } = 1;
    // Chi phí Hồn Tinh cơ bản để luyện điểm Hồn ở bậc đầu tiên (xem GetSoulEssenceCostForHonPoint).
    public long SoulEssenceHonCostBase { get; set; } = 1_000;
    // Cứ mỗi bao nhiêu điểm Hồn thì chi phí luyện 1 điểm Hồn tăng thêm 1 bậc.
    public int SoulEssenceHonCostTierSize { get; set; } = 50;
    // Mỗi bậc thì chi phí luyện 1 điểm Hồn tăng thêm bao nhiêu Hồn Tinh so với bậc trước.
    public long SoulEssenceHonCostGrowthPerTier { get; set; } = 1_000;
    // Số Hồn Tinh cộng thêm mỗi khi thành viên giết quái.
    public long MonsterKillSoulEssenceReward { get; set; } = 1;
    // Trần Hồn tối đa theo từng cấp giới hạn; index 0 là cấp giới hạn 1.
    public long[] MaxHonByLimitLevel { get; set; } = [10];
    // Trần Hồn Tinh tối đa theo từng cấp giới hạn; index 0 là cấp giới hạn 1.
    public long[] MaxSoulEssenceByLimitLevel { get; set; } = [1_000];
    // Chi phí Quỹ tông môn (Funds) để mở lên từng cấp giới hạn hồn tiếp theo; index 0 ứng với cấp giới hạn 1 (không dùng vì là cấp khởi tạo).
    public long[] UpgradeHonLimitFundCostByLevel { get; set; } = [0];
    // Số Tiên Ngọc quy đổi mỗi lần đóng góp.
    public int ContributionMoneyVipPerTime { get; set; }
    // Số lần đóng góp tối đa cho phép trong một request.
    public int MaxContributionTimesPerRequest { get; set; }
    // Số lượng vật phẩm tối đa mua/thêm trong một request.
    public int MaxShopQuantityPerRequest { get; set; } = 100;
    // Map vai trò (ClanRole) sang quyền hạn (ClanPermission) tương ứng.
    public Dictionary<ClanRole, ClanPermission> RolePermissions { get; set; } = new();
    // Số ô truyền thừa cơ bản ở cấp 1.
    public int BaseInheritanceSlots { get; set; } = 1;
    // Số ô truyền thừa cộng thêm mỗi cấp tông môn.
    public int InheritanceSlotsPerLevel { get; set; } = 1;
    // Trần số ô truyền thừa tối đa mỗi cột (StartTurn/EndTurn).
    public int MaxInheritanceSlotsPerColumn { get; set; } = 10;
    // Phí Tiên Ngọc để đổi hiệu ứng trong một ô truyền thừa đã có sẵn.
    public int ChangeInheritanceMoneyVipCost { get; set; } = 2_000;
    // Giá trị hiệu ứng truyền thừa cơ bản ở cấp 1.
    public int InheritanceBaseValue { get; set; } = 1;
    // Giá trị hiệu ứng truyền thừa cộng thêm mỗi cấp tông môn.
    public int InheritanceValuePerClanLevel { get; set; } = 1;
    // Danh sách EffectType được phép chọn cho ô truyền thừa.
    public EffectType[] InheritanceBuffWhitelist { get; set; } = [];
    // Mốc EXP tổng cần để lên từng cấp; index 0 là cấp 1 và phải bằng 0, index 1 là cấp 2...
    public long[] ClanLevelRequiredTotalExp { get; set; } = [];
    // Chỉ cho tạo tông môn bằng VNĐ khi cổng thanh toán web đang bật.
    public bool EnableCreateClanByVnd => GameConfig.EnableWebPay;
    // Số giờ phải chờ sau khi TỰ RỜI tông môn mới được gửi đơn xin gia nhập tông môn khác.
    // Không áp dụng khi bị kick khỏi tông môn.
    [MemoryPackIgnore]
    public int LeaveClanCooldownHours { get; set; } = 24;
    // Số giờ phải chờ trước khi được phong LÊN Phó Tộc Chủ/Trưởng Lão lần nữa, tính riêng theo
    // từng chức vụ (slot) của tông môn. Không áp dụng khi giáng chức về Member.
    [MemoryPackIgnore]
    public int RoleAssignCooldownHours { get; set; } = 24;
}
