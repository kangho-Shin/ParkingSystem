using System.Globalization;

namespace Parking.LprStressTester;

public static class LprFileNameFactory
{
    public static string Create(LprStressOptions options, int deviceNumber, int sequence, DateTime localTime)
    {
        string carNumber = $"시험{deviceNumber:000}{sequence:000000}";
        return string.Create(CultureInfo.InvariantCulture,
            $"{options.SiteId}_{options.Groupnum:000}_{deviceNumber:000}_{options.LaneId}_Entry_{localTime:yyyyMMddHHmmssfff}_{carNumber}_{Guid.NewGuid():N}.jpg");
    }
}
