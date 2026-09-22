using Dapper;
using MySqlConnector;
using Parking.Contracts;

namespace Parking.Central.Data;

public sealed class ParkingCorrectionRepository : IParkingCorrectionRepository
{
    private readonly string _connectionString;
    public ParkingCorrectionRepository(string connectionString) => _connectionString = connectionString;

    public async Task<CorrectCarNumberResponse> CorrectCarNumberAsync(long parkingSessionId, string carNumber, CancellationToken cancellationToken)
    {
        string normalized = carNumber.Trim();
        const string sql = "UPDATE tparkinfo SET carnum=@CarNumber WHERE xindex=@ParkingSessionId AND outflag<>'O';";
        await using MySqlConnection connection = new(_connectionString);
        int affected = await connection.ExecuteAsync(new CommandDefinition(sql, new { ParkingSessionId = parkingSessionId, CarNumber = normalized }, cancellationToken: cancellationToken));
        return affected == 1
            ? new(parkingSessionId, normalized, true, "CAR_NUMBER_UPDATED", "차량번호를 수정했습니다.")
            : new(parkingSessionId, normalized, false, "OPEN_SESSION_NOT_FOUND", "수정할 미출차 차량이 없습니다.");
    }
}
