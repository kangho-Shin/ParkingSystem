namespace Parking.FeeEngine;

public sealed record ParkingSettlementResult(
    long PayableAmount,
    bool IsPrepayGrace);

public static class ParkingSettlementCalculator
{
    public static ParkingSettlementResult Calculate(
        long finalFee,
        long paidAmount,
        DateTimeOffset? lastPaydate,
        DateTimeOffset exitAt,
        int prepayGraceTime)
    {
        bool isPrepayGrace = paidAmount > 0 &&
            lastPaydate.HasValue &&
            prepayGraceTime > 0 &&
            exitAt <= lastPaydate.Value.AddMinutes(prepayGraceTime);

        long payableAmount = isPrepayGrace
            ? 0
            : Math.Max(finalFee - paidAmount, 0);

        return new ParkingSettlementResult(payableAmount, isPrepayGrace);
    }
}
