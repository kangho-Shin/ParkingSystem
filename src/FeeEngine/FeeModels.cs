namespace Parking.FeeEngine;

public enum DiscountType
{
    None,
    TimeMinute,
    Amount,
    FixedAmount,
    Percent,
    Compound
}

public sealed class ParkingFeeRule
{
    public int WeekType { get; init; }
    public int CarType { get; init; }
    public int FeeStep { get; init; }
    public int UnitMinutes { get; init; }
    public int FeePerUnit { get; init; }
    public int MaxCount { get; init; }
}

public sealed class ParkingDiscount
{
    public int Key { get; init; }
    public DiscountType Type { get; init; }
    public int Value { get; init; }
}

public sealed class ParkingDayCharge
{
    public DateTime Date { get; init; }
    public int DayMinutes { get; init; }
    public int NightMinutes { get; init; }
    public int Fee { get; internal set; }
}

public sealed class ParkingFeeRequest
{
    public DateTime EntryAt { get; init; }
    public DateTime ExitAt { get; init; }
    public int CarType { get; init; }
    public IReadOnlyList<int> DiscountKeys { get; init; } = Array.Empty<int>();
}

public sealed class ParkingFeeResult
{
    public int OriginalFee { get; init; }
    public int FinalFee { get; init; }
    public int DiscountFee => OriginalFee - FinalFee;
    public int ParkingMinutes { get; init; }
    public IReadOnlyList<ParkingDayCharge> DailyCharges { get; init; } = Array.Empty<ParkingDayCharge>();
}
