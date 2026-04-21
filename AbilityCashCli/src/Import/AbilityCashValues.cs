namespace AbilityCashCli.Import;

public static class AbilityCashValues
{
    public const long MoneyMultiplier = 10_000;
    public const int QuantityOne = 10_000;
    public const int DaySeconds = 86_400;
    public const string RecurrenceEmpty = "{}";

    public static string BuildSourceComment(string filename, Type importerType) =>
        $"{Path.GetFileName(filename)} | {importerType.Name} v{AppInfo.Version}";

    public static byte[] NewGuidBytes() => Guid.NewGuid().ToByteArray();

    public static int NowUnix() => (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    public static int StartOfDayUnix(DateTime date)
    {
        var d = DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);
        return (int)new DateTimeOffset(d).ToUnixTimeSeconds();
    }

    public static int ToUnix(DateTime dt)
    {
        var d = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
        return (int)new DateTimeOffset(d).ToUnixTimeSeconds();
    }

    public static long ToStoredAmount(decimal amount) =>
        (long)Math.Round(amount * MoneyMultiplier, 0, MidpointRounding.AwayFromZero);
}
