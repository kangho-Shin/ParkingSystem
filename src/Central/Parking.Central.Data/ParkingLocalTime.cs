namespace Parking.Central.Data;

public static class ParkingLocalTime
{
    public static readonly TimeSpan KoreaOffset = TimeSpan.FromHours(9);

    public static DateTime ToDatabase(DateTimeOffset value)
    {
        DateTime local = value.ToOffset(KoreaOffset).DateTime;
        return new DateTime(
            local.Year, local.Month, local.Day,
            local.Hour, local.Minute, local.Second,
            DateTimeKind.Unspecified);
    }

    public static DateTimeOffset FromDatabase(DateTime value) =>
        new(DateTime.SpecifyKind(value, DateTimeKind.Unspecified), KoreaOffset);
}
