using System.Text;
using Dapper;
using MySqlConnector;
using Parking.Contracts;

namespace Parking.Central.Data;

public sealed class ParkingManagementRepository : IParkingManagementRepository
{
    private readonly string _connectionString;

    public ParkingManagementRepository(string connectionString) =>
        _connectionString = connectionString;

    public Task<PagedParkingResult<ParkingManagementItem>> SearchEntriesAsync(
        ParkingManagementQuery query,
        CancellationToken cancellationToken)
    {
        DynamicParameters parameters = Parameters(query);
        string generalWhere = EntryWhere(query, "p", "d", parameters);
        string periodWhere = EntryWhere(query, "p", "d", parameters);
        string union = $"""
            SELECT 1 SessionType,p.xindex ParkingSessionId,p.sitenum SiteId,
                   p.groupnum Groupnum,p.carnum CarNumber,p.cartype CarType,
                   p.outflag Status,p.inlaneid InLaneId,p.indevicenum InDeviceNumber,
                   COALESCE(d.devicename,'') InDeviceName,p.indate InDateTime,
                   NULL PaidAt,NULL OutLaneId,NULL OutDeviceNumber,
                   NULL ProcessDeviceName,NULL OutDateTime,p.parktime ParkingMinutes,
                   NULL OriginalFee,NULL DiscountFee,NULL PaidFee,p.manual IsManual,
                   p.inimage InImage,NULL OutImage,p.indate SortDate
            FROM tparkinfo p
            LEFT JOIN tdeviceinfo d
              ON d.sitenum=p.sitenum AND d.groupnum=p.groupnum
             AND d.devicenum=p.indevicenum
            WHERE p.outflag='I'{generalWhere}
            UNION ALL
            SELECT 2 SessionType,p.xindex ParkingSessionId,p.sitenum SiteId,
                   p.groupnum Groupnum,p.carnum CarNumber,p.cartype CarType,
                   p.outflag Status,p.inlaneid InLaneId,p.indevicenum InDeviceNumber,
                   COALESCE(d.devicename,'') InDeviceName,p.indate InDateTime,
                   NULL PaidAt,NULL OutLaneId,NULL OutDeviceNumber,
                   NULL ProcessDeviceName,NULL OutDateTime,p.parktime ParkingMinutes,
                   NULL OriginalFee,NULL DiscountFee,NULL PaidFee,p.manual IsManual,
                   p.inimage InImage,NULL OutImage,p.indate SortDate
            FROM tperiodinout p
            LEFT JOIN tdeviceinfo d
              ON d.sitenum=p.sitenum AND d.groupnum=p.groupnum
             AND d.devicenum=p.indevicenum
            WHERE p.outflag='I'{periodWhere}
            """;
        return SearchAsync(union, query, parameters, cancellationToken);
    }

    public Task<PagedParkingResult<ParkingManagementItem>> SearchExitsAsync(
        ParkingManagementQuery query,
        CancellationToken cancellationToken)
    {
        DynamicParameters parameters = Parameters(query);
        string generalWhere = ExitWhere(query, "p", "pd", parameters, true);
        string periodWhere = ExitWhere(query, "p", "pd", parameters, false);
        string union = $"""
            SELECT 1 SessionType,p.xindex ParkingSessionId,p.sitenum SiteId,
                   p.groupnum Groupnum,p.carnum CarNumber,p.cartype CarType,
                   p.outflag Status,p.inlaneid InLaneId,p.indevicenum InDeviceNumber,
                   COALESCE(ind.devicename,'') InDeviceName,p.indate InDateTime,
                   p.paydate PaidAt,p.outlaneid OutLaneId,
                   CASE WHEN p.outflag='X' THEN pay.devicenum ELSE p.outdevicenum END OutDeviceNumber,
                   pd.devicename ProcessDeviceName,p.outdate OutDateTime,
                   p.parktime ParkingMinutes,p.parkfee OriginalFee,
                   p.discountfee DiscountFee,p.paidfee PaidFee,p.manual IsManual,
                   p.inimage InImage,p.outimage OutImage,
                   CASE WHEN p.outflag='X' THEN p.paydate ELSE p.outdate END SortDate
            FROM tparkinfo p
            LEFT JOIN tdeviceinfo ind
              ON ind.sitenum=p.sitenum AND ind.groupnum=p.groupnum
             AND ind.devicenum=p.indevicenum
            LEFT JOIN tbcardinfo pay
              ON pay.xindex=(SELECT MAX(c2.xindex) FROM tbcardinfo c2 WHERE c2.pindex=p.xindex)
            LEFT JOIN tdeviceinfo pd
              ON pd.sitenum=p.sitenum AND pd.groupnum=p.groupnum
             AND pd.devicenum=CASE WHEN p.outflag='X' THEN pay.devicenum ELSE p.outdevicenum END
            WHERE p.outflag IN ('X','O'){generalWhere}
            UNION ALL
            SELECT 2 SessionType,p.xindex ParkingSessionId,p.sitenum SiteId,
                   p.groupnum Groupnum,p.carnum CarNumber,p.cartype CarType,
                   p.outflag Status,p.inlaneid InLaneId,p.indevicenum InDeviceNumber,
                   COALESCE(ind.devicename,'') InDeviceName,p.indate InDateTime,
                   NULL PaidAt,p.outlaneid OutLaneId,p.outdevicenum OutDeviceNumber,
                   pd.devicename ProcessDeviceName,p.outdate OutDateTime,
                   p.parktime ParkingMinutes,NULL OriginalFee,NULL DiscountFee,
                   NULL PaidFee,p.manual IsManual,p.inimage InImage,p.outimage OutImage,
                   p.outdate SortDate
            FROM tperiodinout p
            LEFT JOIN tdeviceinfo ind
              ON ind.sitenum=p.sitenum AND ind.groupnum=p.groupnum
             AND ind.devicenum=p.indevicenum
            LEFT JOIN tdeviceinfo pd
              ON pd.sitenum=p.sitenum AND pd.groupnum=p.groupnum
             AND pd.devicenum=p.outdevicenum
            WHERE p.outflag='O'{periodWhere}
            """;
        return SearchAsync(union, query, parameters, cancellationToken);
    }

    public async Task<bool> CorrectCarNumberAsync(
        long siteId,
        ParkingSessionType sessionType,
        long parkingSessionId,
        string carNumber,
        CancellationToken cancellationToken)
    {
        string table = sessionType switch
        {
            ParkingSessionType.General => "tparkinfo",
            ParkingSessionType.Period => "tperiodinout",
            _ => throw new ArgumentOutOfRangeException(nameof(sessionType))
        };
        string sql = $"UPDATE {table} SET carnum=@CarNumber " +
                     "WHERE xindex=@ParkingSessionId AND sitenum=@SiteId AND outflag='I';";
        await using MySqlConnection connection = new(_connectionString);
        int changed = await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new { SiteId = siteId, ParkingSessionId = parkingSessionId, CarNumber = carNumber.Trim() },
            cancellationToken: cancellationToken));
        return changed == 1;
    }

    private async Task<PagedParkingResult<ParkingManagementItem>> SearchAsync(
        string union,
        ParkingManagementQuery query,
        DynamicParameters parameters,
        CancellationToken cancellationToken)
    {
        int page = Math.Max(query.Page, 1);
        int pageSize = query.PageSize <= 0 ? 200 : Math.Min(query.PageSize, 500);
        parameters.Add("Offset", (page - 1) * pageSize);
        parameters.Add("PageSize", pageSize);
        string sql = $"""
            SELECT COUNT(*) FROM ({union}) total;
            SELECT * FROM ({union}) rowsource
            ORDER BY SortDate DESC,SessionType,ParkingSessionId DESC
            LIMIT @PageSize OFFSET @Offset;
            """;
        await using MySqlConnection connection = new(_connectionString);
        using SqlMapper.GridReader reader = await connection.QueryMultipleAsync(
            new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));
        long total = await reader.ReadSingleAsync<long>();
        IEnumerable<ParkingRow> rows = await reader.ReadAsync<ParkingRow>();
        return new PagedParkingResult<ParkingManagementItem>(
            rows.Select(ToItem).ToList(), page, pageSize, total);
    }

    private static DynamicParameters Parameters(ParkingManagementQuery query)
    {
        DynamicParameters parameters = new();
        parameters.Add("SiteId", query.SiteId);
        if (query.From.HasValue) parameters.Add("From", ParkingLocalTime.ToDatabase(query.From.Value));
        if (query.To.HasValue) parameters.Add("To", ParkingLocalTime.ToDatabase(query.To.Value));
        if (query.Groupnum.HasValue) parameters.Add("Groupnum", query.Groupnum.Value);
        if (query.DeviceId.HasValue) parameters.Add("DeviceId", query.DeviceId.Value);
        if (!string.IsNullOrWhiteSpace(query.CarNumber)) parameters.Add("CarNumber", query.CarNumber.Trim());
        if (!string.IsNullOrWhiteSpace(query.Status)) parameters.Add("Status", query.Status.Trim().ToUpperInvariant());
        return parameters;
    }

    private static string EntryWhere(
        ParkingManagementQuery query,
        string alias,
        string deviceAlias,
        DynamicParameters parameters)
    {
        StringBuilder sql = new($" AND {alias}.sitenum=@SiteId");
        AddCommon(sql, query, alias, deviceAlias, "indate", parameters);
        return sql.ToString();
    }

    private static string ExitWhere(
        ParkingManagementQuery query,
        string alias,
        string deviceAlias,
        DynamicParameters parameters,
        bool general)
    {
        string date = general
            ? $"CASE WHEN {alias}.outflag='X' THEN {alias}.paydate ELSE {alias}.outdate END"
            : $"{alias}.outdate";
        StringBuilder sql = new($" AND {alias}.sitenum=@SiteId");
        AddCommon(sql, query, alias, deviceAlias, date, parameters);
        if (!string.IsNullOrWhiteSpace(query.Status))
            sql.Append($" AND {alias}.outflag=@Status");
        return sql.ToString();
    }

    private static void AddCommon(
        StringBuilder sql,
        ParkingManagementQuery query,
        string alias,
        string deviceAlias,
        string dateExpression,
        DynamicParameters parameters)
    {
        if (query.From.HasValue) sql.Append($" AND {dateExpression}>=@From");
        if (query.To.HasValue) sql.Append($" AND {dateExpression}<=@To");
        if (query.Groupnum.HasValue) sql.Append($" AND {alias}.groupnum=@Groupnum");
        if (query.DeviceId.HasValue) sql.Append($" AND {deviceAlias}.deviceid=@DeviceId");
        if (!string.IsNullOrWhiteSpace(query.CarNumber))
            sql.Append(query.CarNumber.Trim().Length == 4
                ? $" AND RIGHT({alias}.carnum,4)=@CarNumber"
                : $" AND {alias}.carnum=@CarNumber");
    }

    private static ParkingManagementItem ToItem(ParkingRow row) => new(
        (ParkingSessionType)row.SessionType,
        row.ParkingSessionId,
        row.SiteId,
        row.Groupnum,
        row.CarNumber,
        row.CarType,
        row.Status,
        row.InLaneId,
        row.InDeviceNumber,
        row.InDeviceName,
        ParkingLocalTime.FromDatabase(row.InDateTime),
        row.PaidAt.HasValue ? ParkingLocalTime.FromDatabase(row.PaidAt.Value) : null,
        row.OutLaneId,
        row.OutDeviceNumber,
        row.ProcessDeviceName,
        row.OutDateTime.HasValue ? ParkingLocalTime.FromDatabase(row.OutDateTime.Value) : null,
        row.ParkingMinutes,
        row.OriginalFee,
        row.DiscountFee,
        row.PaidFee,
        row.IsManual != 0,
        row.InImage,
        row.OutImage);

    private sealed class ParkingRow
    {
        public int SessionType { get; set; }
        public long ParkingSessionId { get; set; }
        public long SiteId { get; set; }
        public int Groupnum { get; set; }
        public string CarNumber { get; set; } = "";
        public int CarType { get; set; }
        public string Status { get; set; } = "";
        public long InLaneId { get; set; }
        public int InDeviceNumber { get; set; }
        public string InDeviceName { get; set; } = "";
        public DateTime InDateTime { get; set; }
        public DateTime? PaidAt { get; set; }
        public long? OutLaneId { get; set; }
        public int? OutDeviceNumber { get; set; }
        public string? ProcessDeviceName { get; set; }
        public DateTime? OutDateTime { get; set; }
        public int ParkingMinutes { get; set; }
        public int? OriginalFee { get; set; }
        public int? DiscountFee { get; set; }
        public int? PaidFee { get; set; }
        public int IsManual { get; set; }
        public string? InImage { get; set; }
        public string? OutImage { get; set; }
    }
}
