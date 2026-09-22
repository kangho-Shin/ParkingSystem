using Dapper;
using MySqlConnector;

namespace Parking.Central.Data;

internal sealed class PaymentSummaryRow
{
    public long PaidAmount { get; set; }
    public DateTime? LastPaydate { get; set; }
}

public sealed class SettlementRepository : ISettlementRepository
{
    private readonly string _connectionString;

    public SettlementRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<SettlementData> GetAsync(
        long parkingSessionId,
        CancellationToken cancellationToken)
    {
        await using MySqlConnection connection = new(_connectionString);
        await connection.OpenAsync(cancellationToken);

        IEnumerable<int> discountKeys = await connection.QueryAsync<int>(
            new CommandDefinition("""
                SELECT DISTINCT discountkey
                FROM parking_session_discount
                WHERE parkindex=@ParkingSessionId
                ORDER BY discountkey;
                """,
                new { ParkingSessionId = parkingSessionId },
                cancellationToken: cancellationToken));

        PaymentSummaryRow payment = await connection.QuerySingleAsync<PaymentSummaryRow>(
            new CommandDefinition("""
                SELECT COALESCE(SUM(payamount),0) PaidAmount,
                       MAX(paydate) LastPaydate
                FROM payment
                WHERE parkindex=@ParkingSessionId;
                """,
                new { ParkingSessionId = parkingSessionId },
                cancellationToken: cancellationToken));

        DateTimeOffset? lastPaydate = payment.LastPaydate.HasValue
            ? ParkingLocalTime.FromDatabase(payment.LastPaydate.Value)
            : null;

        return new SettlementData(discountKeys.ToList(), payment.PaidAmount, lastPaydate);
    }
}
