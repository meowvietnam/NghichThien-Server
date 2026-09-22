namespace MTS.Game.Boss;

public class BossOptions
{
    // Vietnam time (UTC+7). One boss per selected day and time window.
    public DayOfWeek[] DaysOfWeek { get; set; } = [];
    public TimeOnly StartTime { get; set; } = new(12, 0);
    public TimeOnly EndTime { get; set; } = new(13, 0);
    public bool KeepUntilNextSpawn { get; set; }

    public static BossOptions Default => new() { DaysOfWeek = Enum.GetValues<DayOfWeek>() };

    public string GetScheduleText()
    {
        var days = (DaysOfWeek ?? []).Distinct().OrderBy(day => ((int)day + 6) % 7).ToArray();
        var useShortDays = days.Length >= 5;
        var dayText = days.Length == 7 ? "Hằng ngày" : string.Join(", ", days.Select(day => day switch
        {
            DayOfWeek.Sunday => "CN",
            _ => useShortDays ? $"T{((int)day + 1)}" : $"Thứ {((int)day + 1)}"
        }));
        var timeText = KeepUntilNextSpawn
            ? $"từ {StartTime:HH:mm} đến phiên kế tiếp"
            : $"{StartTime:HH:mm}–{EndTime:HH:mm}";
        return $"Lịch Boss (giờ VN): {dayText}, {timeText}";
    }
}
