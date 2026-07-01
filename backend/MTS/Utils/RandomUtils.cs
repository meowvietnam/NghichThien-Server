namespace MTS.Utils;

public static class RandomUtils
{
    public static T Random<T>(this List<T> list)
    {
        var index = System.Random.Shared.Next(list.Count);
        return list[index];
    }
    public static bool RandomChance(double percent)
    {
        if (percent <= 0) return false;
        if (percent >= 100) return true;
        return System.Random.Shared.NextDouble() * 100 < percent;
    }
    public static T Random<T>(this T[] array)
    {
        var index = System.Random.Shared.Next(array.Length);
        return array[index];
    }
}