namespace Parking.FeeEngine;

public sealed class ParkingFeeConfiguration
{
    public IReadOnlyList<ParkingFeeRule> FeeRules { get; init; } = Array.Empty<ParkingFeeRule>();
    public IReadOnlyList<ParkingDiscount> Discounts { get; init; } = Array.Empty<ParkingDiscount>();
    public IReadOnlyCollection<DateTime> Holidays { get; init; } = Array.Empty<DateTime>();
    public IReadOnlyDictionary<DayOfWeek, DayTimeRange> DayTimeRanges { get; init; } =
        new Dictionary<DayOfWeek, DayTimeRange>();
    public int MaxDailyFee { get; init; }
    public int GraceTime { get; init; }
    public int PrepayGraceTime { get; init; }
    public int ServiceTime { get; init; }
    public int TimeDiscountApplyType { get; init; } = 1;
    public bool ExcludeWeekend { get; init; }
    public bool ExcludeHoliday { get; init; }
}

public readonly record struct DayTimeRange(TimeSpan Start, TimeSpan End);
