namespace MTS.Game.TMC;

/// <summary>
/// Toàn bộ số liệu cân bằng của Tông môn chiến. Các hàm đánh dấu [TẠM] chỉ là giá trị chạy thử;
/// chủ dự án thay công thức chính thức tại đây mà không cần đổi schema hay message.
/// </summary>
public static class WarConfig
{
    /// <summary>Thời gian đi một cạnh (giây), dùng chung cho người và rối. Gửi cho client trong snapshot.</summary>
    public const float SecondsPerEdge = 0.75f;

    /// <summary>Thời gian chờ client vào trận (Ready) trước khi bên chưa sẵn sàng bị xử thua.</summary>
    public static readonly TimeSpan ReadyTimeout = TimeSpan.FromSeconds(30);

    /// <summary>Thời gian chờ báo cáo thứ hai của trận PvP sau khi đã có báo cáo đầu tiên.</summary>
    public static readonly TimeSpan ReportTimeout = TimeSpan.FromSeconds(30);

    /// <summary>Trận kéo dài quá mức này bị server kết thúc hoà (dọn trận treo).</summary>
    public static readonly TimeSpan BattleHardTimeout = TimeSpan.FromMinutes(15);

    // ---------------- HP người chơi ----------------

    /// <summary>[TẠM] HP chiến khi nhân vật tham chiến lần đầu.</summary>
    public static int GetInitialPlayerHp(CharacterDataView character) => GetPlayerMaxHp(character);

    /// <summary>[TẠM] HP chiến tối đa của người chơi.</summary>
    public static int GetPlayerMaxHp(CharacterDataView character) => 1000;

    /// <summary>[TẠM] HP chiến hồi mỗi phút của người chơi (1%/phút).</summary>
    public static float GetPlayerRegenPerMinute(int maxHp) => maxHp * 0.01f;

    // ---------------- Rối ----------------

    /// <summary>[TẠM] Số rối tối đa theo cấp tông: 1 rối mỗi cấp, tối đa 5.</summary>
    public static int GetMaxPuppetCount(int clanLevel) => Math.Clamp(clanLevel, 1, 5);

    /// <summary>[TẠM] Level tối đa của rối theo cấp tông (3 level mỗi cấp tông, trần Hóa Thần hậu kỳ = 15).</summary>
    public static int GetMaxPuppetLevel(int clanLevel) => Math.Clamp(clanLevel * 3, 1, 15);

    /// <summary>Cảnh giới của rối theo level (3 level mỗi cảnh giới, giống người chơi).</summary>
    public static WarRealm GetPuppetRealm(int level) => (WarRealm)Math.Clamp((level - 1) / 3, 0, (int)WarRealm.HoaThan);

    /// <summary>[TẠM] Level khởi tạo của rối mới.</summary>
    public static int GetNewPuppetLevel(int clanLevel) => 1;

    /// <summary>
    /// [TẠM] HP tối đa của rối. <paramref name="randomSeed"/> là thành phần ngẫu nhiên đã lưu bền khi tạo rối;
    /// giá trị tạm chưa dùng tới nó.
    /// </summary>
    public static int GetPuppetMaxHp(int puppetLevel, int clanLevel, int randomSeed) => 1000;

    /// <summary>[TẠM] HP rối hồi mỗi phút, cùng nhịp với người chơi.</summary>
    public static float GetPuppetRegenPerMinute(int maxHp) => maxHp * 0.01f;

    /// <summary>[TẠM] Chi phí tạo rối (quỹ tông). 0 = miễn phí.</summary>
    public static long GetCreatePuppetCost(int clanLevel, int currentCount) => 0;

    /// <summary>[TẠM] Chi phí nâng rối từ <paramref name="fromLevel"/> lên level kế. 0 = miễn phí.</summary>
    public static long GetUpgradePuppetCost(int fromLevel) => 0;

    /// <summary>Sinh thành phần ngẫu nhiên cho rối mới; kết quả được lưu vào Puppet.RandomSeed.</summary>
    public static int RollPuppetSeed() => Random.Shared.Next(0, 1_000_000);
}

/// <summary>Các thông tin nhân vật mà công thức HP chiến được phép đọc (giữ giao diện hàm ổn định).</summary>
public readonly record struct CharacterDataView(int CharId, int Level, int SinhMenh);
