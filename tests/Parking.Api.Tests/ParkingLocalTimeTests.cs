using Parking.Central.Data;

namespace Parking.Api.Tests;

public sealed class ParkingLocalTimeTests
{
    [Fact]
    public void UTC시간은_한국시간_초단위로_변환한다()
    {
        DateTimeOffset utc = new(2026, 9, 22, 0, 23, 6, 922, TimeSpan.Zero);

        DateTime stored = ParkingLocalTime.ToDatabase(utc);

        Assert.Equal(new DateTime(2026, 9, 22, 9, 23, 6), stored);
        Assert.Equal(0, stored.Millisecond);
        Assert.Equal(DateTimeKind.Unspecified, stored.Kind);
    }

    [Fact]
    public void DB한국시간은_UTC9를_포함해_반환한다()
    {
        DateTime stored = new(2026, 9, 22, 9, 23, 6);

        DateTimeOffset result = ParkingLocalTime.FromDatabase(stored);

        Assert.Equal(TimeSpan.FromHours(9), result.Offset);
        Assert.Equal("2026-09-22T09:23:06.0000000+09:00", result.ToString("O"));
    }
}
