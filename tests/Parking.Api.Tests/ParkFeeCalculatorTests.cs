using Parking.FeeEngine;

namespace Parking.Api.Tests;

public sealed class ParkFeeCalculatorTests
{
    [Fact]
    public void 사전정산_유예시간_안에는_추가요금이_없다()
    {
        DateTimeOffset paydate = new(2026, 9, 19, 10, 0, 0, TimeSpan.FromHours(9));

        ParkingSettlementResult result = ParkingSettlementCalculator.Calculate(
            1500,
            1000,
            paydate,
            paydate.AddMinutes(10),
            10);

        Assert.True(result.IsPrepayGrace);
        Assert.Equal(0, result.PayableAmount);
    }

    [Fact]
    public void 사전정산_유예시간이_지나면_기결제금액을_차감한다()
    {
        DateTimeOffset paydate = new(2026, 9, 19, 10, 0, 0, TimeSpan.FromHours(9));

        ParkingSettlementResult result = ParkingSettlementCalculator.Calculate(
            1500,
            1000,
            paydate,
            paydate.AddMinutes(11),
            10);

        Assert.False(result.IsPrepayGrace);
        Assert.Equal(500, result.PayableAmount);
    }

    [Fact]
    public void 회차시간_이내이면_요금은_0원이다()
    {
        ParkingFeeCalculator calculator = CreateCalculator(graceTime: 30, serviceTime: 0);
        DateTime entryAt = new(2026, 9, 21, 8, 0, 0);

        ParkingFeeResult result = calculator.Calculate(new ParkingFeeRequest
        {
            EntryAt = entryAt,
            ExitAt = entryAt.AddMinutes(30),
            CarType = 1
        });

        Assert.Equal(0, result.FinalFee);
        Assert.Equal(30, result.ParkingMinutes);
    }

    [Fact]
    public void 서비스시간은_요금계산시간에서_차감한다()
    {
        ParkingFeeCalculator calculator = CreateCalculator(graceTime: 0, serviceTime: 10);
        DateTime entryAt = new(2026, 9, 21, 8, 0, 0);

        ParkingFeeResult result = calculator.Calculate(new ParkingFeeRequest
        {
            EntryAt = entryAt,
            ExitAt = entryAt.AddMinutes(40),
            CarType = 1
        });

        Assert.Equal(300, result.FinalFee);
        Assert.Equal(30, result.ParkingMinutes);
    }

    private static ParkingFeeCalculator CreateCalculator(int graceTime, int serviceTime)
    {
        Dictionary<DayOfWeek, DayTimeRange> dayTimeRanges = Enum
            .GetValues<DayOfWeek>()
            .ToDictionary(
                day => day,
                _ => new DayTimeRange(TimeSpan.Zero, TimeSpan.FromHours(24)));

        return new ParkingFeeCalculator(new ParkingFeeConfiguration
        {
            GraceTime = graceTime,
            ServiceTime = serviceTime,
            FeeRules = new List<ParkingFeeRule>
            {
                new()
                {
                    WeekType = 1,
                    CarType = 1,
                    FeeStep = 1,
                    UnitMinutes = 10,
                    FeePerUnit = 100,
                    MaxCount = 0
                }
            },
            DayTimeRanges = dayTimeRanges
        });
    }
}
