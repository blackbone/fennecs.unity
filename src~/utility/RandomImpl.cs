namespace fennecs.utility;

public static class RandomImpl
{
    private static Random shared;

    public static int Next() => shared.Next();
    public static int Next(int maxValue) => shared.Next(maxValue);
    public static int Next(int minValue, int maxValue) => shared.Next(minValue, maxValue);
}