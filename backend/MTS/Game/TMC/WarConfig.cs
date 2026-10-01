using MTS.Models;

namespace MTS.Game.TMC;

/// <summary>
/// Toàn bộ số liệu cân bằng của Tông môn chiến. Các hàm đánh dấu [TẠM] chỉ là giá trị chạy thử;
/// chủ dự án thay công thức chính thức tại đây mà không cần đổi schema hay message.
/// </summary>
public static class WarConfig
{
    /// <summary>Thời gian đi một cạnh (giây), dùng chung cho người và rối. Gửi cho client trong snapshot.</summary>
    public const float SecondsPerEdge = 60f;

    /// <summary>Thời gian chờ client vào trận (Ready) trước khi bên chưa sẵn sàng bị xử thua.</summary>
    public static readonly TimeSpan ReadyTimeout = TimeSpan.FromSeconds(30);

    /// <summary>Thời gian chờ báo cáo thứ hai của trận PvP sau khi đã có báo cáo đầu tiên.</summary>
    public static readonly TimeSpan ReportTimeout = TimeSpan.FromSeconds(30);

    /// <summary>Trận kéo dài quá mức này bị server kết thúc hoà (dọn trận treo).</summary>
    public static readonly TimeSpan BattleHardTimeout = TimeSpan.FromMinutes(15);

    // ---------------- Điều kiện tham chiến ----------------

    /// <summary>Cảnh giới cao nhất từng đạt (Progression.MaxLevel) tối thiểu để vào Tông môn chiến (4 = Trúc Cơ sơ kỳ).</summary>
    public const int MinMaxLevel = 4;

    public const string MinLevelMessage = "Yêu cầu đạt Trúc Cơ sơ kỳ trở lên mới được tham gia Tông môn chiến.";

    // ---------------- Giờ mở cửa ----------------

    /// <summary>Bật giờ mở cửa hằng ngày. false = mở cả ngày.</summary>
    public const bool ScheduleEnabled = true;

    /// <summary>Giờ mở cửa hằng ngày (giờ Việt Nam, UTC+7).</summary>
    public static readonly TimeSpan OpenTime = new(19, 0, 0);

    /// <summary>
    /// Giờ đóng cửa hằng ngày (giờ Việt Nam). Tới giờ: mọi trận đang đánh kết thúc hoà, người đang đi dừng ở node an toàn,
    /// tất cả bị đưa ra khỏi scene chiến. Lãnh thổ, HP, rối giữ nguyên. OpenTime &gt; CloseTime nghĩa là mở qua đêm.
    /// </summary>
    public static readonly TimeSpan CloseTime = new(23, 0, 0);

    public static bool IsOpen(DateTime utcNow)
    {
        if (!ScheduleEnabled) return true;
        var time = (utcNow + Utils.Utils.Utc7).TimeOfDay;
        return OpenTime <= CloseTime
            ? time >= OpenTime && time < CloseTime
            : time >= OpenTime || time < CloseTime;
    }

    public static string ClosedMessage =>
        $"Tông môn chiến chỉ mở từ {OpenTime:hh\\:mm} đến {CloseTime:hh\\:mm} hằng ngày.";

    // ---------------- HP người chơi ----------------

    /// <summary>HP chiến khi nhân vật tham chiến lần đầu: đầy.</summary>
    public static int GetInitialPlayerHp(CharacterDataView character) => GetPlayerMaxHp(character);

    /// <summary>
    /// HP chiến tối đa = Sinh Mệnh của nhân vật bên ngoài (CharacterData.Stats.SinhMenh), đồng bộ mỗi khi vào scene,
    /// xin snapshot và vào trận. HP chiến hiện tại vẫn lưu riêng (WarParticipant.Hp), không đụng Stats.Hp.
    /// </summary>
    public static int GetPlayerMaxHp(CharacterDataView character) => Math.Max(1, character.SinhMenh);

    /// <summary>[TẠM] HP chiến hồi mỗi phút của người chơi (1%/phút).</summary>
    public static float GetPlayerRegenPerMinute(int maxHp) => maxHp * 0.01f;

    // ---------------- Rối ----------------

    /// <summary>Số rối tối đa = level tông (không trần).</summary>
    public static int GetMaxPuppetCount(int clanLevel) => Math.Max(1, clanLevel);

    /// <summary>Level tối đa của rối: 3 level mỗi cấp tông (tông cấp 1 → lv3, cấp 2 → lv6...), trần 15 (Hóa Thần hậu kỳ).</summary>
    public static int GetMaxPuppetLevel(int clanLevel) => Math.Clamp(clanLevel * 3, 1, 15);

    /// <summary>Cảnh giới của rối theo level (3 level mỗi cảnh giới, giống người chơi).</summary>
    public static WarRealm GetPuppetRealm(int level) => (WarRealm)Math.Clamp((level - 1) / 3, 0, (int)WarRealm.HoaThan);

    /// <summary>Level khởi tạo của rối mới.</summary>
    public static int GetNewPuppetLevel(int clanLevel) => 1;

    /// <summary>
    /// HP tối đa của rối = (level rối × 10)% HP chiến tối đa của người tạo/nâng cấp tại thời điểm đó.
    /// </summary>
    public static int GetPuppetMaxHp(int puppetLevel, int creatorWarHpMax) =>
        Math.Max(1, (int)((long)creatorWarHpMax * puppetLevel * 10 / 100));

    /// <summary>
    /// Độn tốc (TocDoBay) của rối = (level rối × 10)% độn tốc của người tạo/nâng cấp tại thời điểm đó.
    /// </summary>
    public static int GetPuppetDonToc(int puppetLevel, int creatorDonToc) =>
        Math.Max(0, (int)((long)creatorDonToc * puppetLevel * 10 / 100));

    /// <summary>[TẠM] HP rối hồi mỗi phút, cùng nhịp với người chơi.</summary>
    public static float GetPuppetRegenPerMinute(int maxHp) => maxHp * 0.01f;

    /// <summary>Độ bền của rối: số lần được phép chết (HP về 0). Hết độ bền thì rối bị xoá. Nâng cấp hồi đầy.</summary>
    public const int PuppetMaxDurability = 5;

    /// <summary>Giá tạo một rối, tính bằng tiên ngọc.</summary>
    public const long CreatePuppetMoneyVip = 5000;

    /// <summary>Giá nâng rối lên <paramref name="targetLevel"/>, tính bằng tiên ngọc: 500 × level cần lên.</summary>
    public static long GetUpgradePuppetMoneyVip(int targetLevel) => 500L * targetLevel;

    /// <summary>
    /// Trả bằng điểm cống hiến thì giá gấp đôi tiên ngọc. Giống mở giới hạn luyện hồn: trừ điểm cống hiến trước,
    /// thiếu bao nhiêu điểm thì bù bằng tiên ngọc (2 điểm = 1 tiên ngọc, làm tròn lên).
    /// </summary>
    public const int ContribPointsPerMoneyVip = 2;

    /// <summary>Sinh thành phần ngẫu nhiên cho rối mới; kết quả được lưu vào Puppet.RandomSeed.</summary>
    public static int RollPuppetSeed() => Random.Shared.Next(0, 1_000_000);

    // ---------------- Thưởng hằng ngày ----------------

    /// <summary>Tiên ngọc mỗi ngày cho mỗi node thường (điểm) tông đang giữ.</summary>
    public const int NodeDailyMoneyVip = 100;

    /// <summary>Tiên ngọc mỗi ngày cho mỗi thành (node Stronghold, không gồm thành chính) tông đang giữ.</summary>
    public const int StrongholdDailyMoneyVip = 500;

    /// <summary>Tổng tiên ngọc/ngày của tông theo lãnh thổ đang giữ.</summary>
    public static long GetClanDailyMoneyVip(int strongholdCount, int nodeCount) =>
        (long)strongholdCount * StrongholdDailyMoneyVip + (long)nodeCount * NodeDailyMoneyVip;

    /// <summary>
    /// Phần trăm tổng tiên ngọc/ngày của tông dành cho CẢ NHÓM chức vụ, chia đều cho các thành viên đủ điều kiện
    /// trong nhóm: Tông chủ 50%, Phó tông chủ 20%, Trưởng lão 10%, Thành viên 20% (tổng 100%).
    /// </summary>
    public static int GetRewardPercent(ClanRole role) => role switch
    {
        ClanRole.Leader => 50,
        ClanRole.ViceLeader => 20,
        ClanRole.Elder => 10,
        _ => 20
    };

    /// <summary>Tiên ngọc/ngày một người nhận: phần của nhóm chức vụ chia đều cho <paramref name="groupSize"/> người.</summary>
    public static long GetMemberDailyMoneyVip(long clanDailyMoneyVip, ClanRole role, int groupSize) =>
        clanDailyMoneyVip * GetRewardPercent(role) / 100 / Math.Max(1, groupSize);

    /// <summary>Mốc bắt đầu ngày (UTC) chứa <paramref name="utcNow"/>: 00:00 giờ Việt Nam (UTC+7).</summary>
    public static DateTime GetDayStart(DateTime utcNow)
    {
        var local = utcNow + Utils.Utils.Utc7;
        return DateTime.SpecifyKind(local.Date - Utils.Utils.Utc7, DateTimeKind.Utc);
    }

    public static DateTime GetNextDailySettlementAt(DateTime utcNow) => GetDayStart(utcNow).AddDays(1);

    // ---------------- Mùa (sự kiện 2 tuần) ----------------

    /// <summary>Độ dài một mùa. Hết mùa: node thường về trung lập, người và rối về thành chính, HP hồi đầy.</summary>
    public static readonly TimeSpan SeasonLength = TimeSpan.FromDays(14);

    /// <summary>Mốc gốc của lịch mùa: 00:00 thứ Hai 28/09/2026 giờ Việt Nam. Các mùa nối tiếp nhau mỗi 14 ngày.</summary>
    public static readonly DateTime SeasonAnchor = new(2026, 9, 27, 17, 0, 0, DateTimeKind.Utc);

    public static DateTime GetSeasonStart(DateTime utcNow)
    {
        var elapsed = utcNow - SeasonAnchor;
        var index = (long)Math.Floor(elapsed.TotalDays / SeasonLength.TotalDays);
        return SeasonAnchor.AddDays(index * SeasonLength.TotalDays);
    }

    public static DateTime GetSeasonEndAt(DateTime utcNow) => GetSeasonStart(utcNow) + SeasonLength;
}

/// <summary>Các thông tin nhân vật mà công thức HP chiến được phép đọc (giữ giao diện hàm ổn định).</summary>
public readonly record struct CharacterDataView(int CharId, int Level, int SinhMenh);
