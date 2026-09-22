using Dapper;
using MySqlConnector;
using Parking.Contracts;

namespace Parking.Central.Data;

public sealed class ParkingSearchRepository : IParkingSearchRepository
{
    private readonly string _connectionString;

    public ParkingSearchRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<IReadOnlyList<ParkingSearchCandidate>> SearchAsync(
        long siteId,
        int groupnum,
        string carNumber,
        CancellationToken cancellationToken)
    {
        string normalizedCarNumber = carNumber.Trim();
        string numberCondition = normalizedCarNumber.Length == 4
            ? "RIGHT(carnum,4)=@CarNumber"
            : "carnum=@CarNumber";
        string sql = $"""
            SELECT xindex ParkingSessionId, carnum CarNumber,
                   groupnum Groupnum, cartype CarType,
                   indate InDateTime, inimage InImage
            FROM parking_session
            WHERE sitenum=@SiteId AND groupnum=@Groupnum
              AND outflag<>'O' AND {numberCondition}
            ORDER BY indate DESC, xindex DESC;
            """;

        await using MySqlConnection connection = new(_connectionString);
        IEnumerable<ParkingSearchRow> rows = await connection.QueryAsync<ParkingSearchRow>(
            new CommandDefinition(
                sql,
                new { SiteId = siteId, Groupnum = groupnum, CarNumber = normalizedCarNumber },
                cancellationToken: cancellationToken));

        return rows.Select(row => new ParkingSearchCandidate(
            row.ParkingSessionId,
            row.CarNumber,
            row.Groupnum,
            row.CarType,
            ParkingLocalTime.FromDatabase(row.InDateTime),
            row.InImage)).ToList();
    }

    private sealed class ParkingSearchRow
    {
        public long ParkingSessionId { get; set; }
        public string CarNumber { get; set; } = "";
        public int Groupnum { get; set; }
        public int CarType { get; set; }
        public DateTime InDateTime { get; set; }
        public string? InImage { get; set; }
    }
}
