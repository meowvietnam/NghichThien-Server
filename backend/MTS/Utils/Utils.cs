using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using NPinyin;

namespace MTS.Utils;

public static class Utils
{
    public static readonly TimeSpan Utc7 = TimeSpan.FromHours(7);

    public static readonly string[] DayOfWeeks =
        ["Chủ nhật", "Thứ hai", "Thứ ba", "Thứ tư", "Thứ năm", "Thứ sáu", "Thứ bảy"];

    public static string GetDayOfWeek(DayOfWeek dayOfWeek)
    {
        return DayOfWeeks[(int)dayOfWeek];
    }

    public static string RemoveVietnameseDiacritics(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return name;

        string normalized = name.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();

        foreach (char c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(c);
            }
        }

        return sb
            .ToString()
            .Normalize(NormalizationForm.FormC)
            .Replace("Đ", "D")
            .Replace("đ", "d");
    }
    public static string ChineseToPinyin(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return name;
        var pinyin = Pinyin.GetPinyin(name);
        return pinyin;
    }
    public static string GenerateSecureString(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var result = new char[length];

        using var rng = RandomNumberGenerator.Create();
        var buffer = new byte[length];
        rng.GetBytes(buffer);
        for (int i = 0; i < length; i++)
        {
            result[i] = chars[buffer[i] % chars.Length];
        }

        return new string(result);
    }
    public static string NormalizeSearchText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        text = text.Trim().ToLowerInvariant();

        var normalized = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();

        foreach (var c in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(c);

            if (category != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(c);
            }
        }
        return sb
            .ToString()
            .Normalize(NormalizationForm.FormC)
            .Replace('đ', 'd');
    }
    public static string FormatCount(long money)
    {
        return money.ToString("N0", CultureInfo.GetCultureInfo("vi-VN"));
    }

    public static void AddRange<T>(this HashSet<T> set, IEnumerable<T> values)
    {
        foreach (var value in values)
        {
            set.Add(value);
        }
    }

    public static byte[] HashSHA256(string input)
    {
        return SHA256.HashData(Encoding.UTF8.GetBytes(input));
    }

    public static DateTimeOffset GetNextPeriodicTime(
        DateTimeOffset startDate,
        DateTimeOffset now,
        TimeSpan interval)
    {
        now = now.ToOffset(startDate.Offset);
        if (now < startDate)
            return startDate;
        var intervalTicks = interval.Ticks;
        var elapsedTicks = (now - startDate).Ticks;
        var passedPeriods = elapsedTicks / intervalTicks;
        return startDate.AddTicks((passedPeriods + 1) * intervalTicks);
    }

    public static DateTimeOffset Now()
    {
        return DateTimeOffset.UtcNow.ToOffset(Utc7);
    }
    public static bool IsToday(DateTime dateTimeUtc)
    {
        var now = Now();
        var dateTime = new DateTimeOffset(DateTime.SpecifyKind(dateTimeUtc, DateTimeKind.Utc)).ToOffset(Utc7);
        return now.Date == dateTime.Date;
    }
    public static List<T[]> SplitArray<T>(T[] array, int chunkSize)
    {
        return array
            .Select((value, index) => new { value, index })
            .GroupBy(x => x.index / chunkSize)
            .Select(g => g.Select(x => x.value).ToArray())
            .ToList();
    }
}