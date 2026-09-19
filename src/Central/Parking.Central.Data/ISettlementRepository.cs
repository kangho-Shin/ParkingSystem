namespace Parking.Central.Data;

public sealed record SettlementData(
    IReadOnlyList<int> DiscountKeys,
    long PaidAmount,
    DateTimeOffset? LastPaydate);

public interface ISettlementRepository
{
    Task<SettlementData> GetAsync(
        long parkingSessionId,
        CancellationToken cancellationToken);
}
