namespace Parking.EdgeManager.Core;

public static class ParkingManagementQueryPolicy
{
    public static int NormalizePage(int page) => Math.Max(page, 1);

    public static int NormalizePageSize(int pageSize) =>
        pageSize <= 0 ? 200 : Math.Min(pageSize, 500);

    public static void ValidateExitRange(DateTimeOffset from, DateTimeOffset to)
    {
        if (to < from || to - from > TimeSpan.FromDays(31))
            throw new ArgumentOutOfRangeException(nameof(to));
    }
}
