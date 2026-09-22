using System.Security.Cryptography;
using System.Text;
using Dapper;
using MySqlConnector;
using Parking.Contracts;

namespace Parking.Central.Data;

public sealed class SiteConfigurationRepository : ISiteConfigurationRepository
{
    private readonly string _connectionString;

    public SiteConfigurationRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<SiteConfiguration?> GetAsync(
        long siteId,
        CancellationToken cancellationToken)
    {
        await using MySqlConnection connection = new(_connectionString);
        ParkingSite? site = await connection.QueryFirstOrDefaultAsync<ParkingSite>(
            new CommandDefinition("""
                SELECT sitenum SiteId,parkname SiteName,useflag Enabled
                FROM tparkings
                WHERE sitenum=@SiteId
                ORDER BY groupnum LIMIT 1;
                """,
                new { SiteId = siteId },
                cancellationToken: cancellationToken));
        if (site is null)
            return null;

        IReadOnlyList<ParkingLane> lanes =
            (await connection.QueryAsync<ParkingLane>(new CommandDefinition("""
                SELECT laneid LaneId,sitenum SiteId,groupnum GroupNumber,
                       lanename LaneName,direction Direction,useflag Enabled
                FROM tlaneinfo
                WHERE sitenum=@SiteId
                ORDER BY groupnum,laneid;
                """,
                new { SiteId = siteId },
                cancellationToken: cancellationToken))).AsList();

        IReadOnlyList<ParkingDevice> devices =
            (await connection.QueryAsync<ParkingDevice>(new CommandDefinition("""
                SELECT deviceid DeviceId,sitenum SiteId,laneid LaneId,
                       devicenum DeviceNumber,
                       CASE devicetype
                           WHEN 1 THEN 'OPERATOR'
                           WHEN 2 THEN 'KIOSK'
                           WHEN 3 THEN 'LPR'
                           WHEN 4 THEN 'TICKET'
                           WHEN 5 THEN 'PDA'
                           WHEN 6 THEN 'LDM'
                           WHEN 7 THEN 'GATE'
                           WHEN 8 THEN 'EDGE'
                           ELSE 'OTHER'
                       END DeviceType,
                       devicename DeviceName,ip IpAddress,useflag Enabled,port Port
                FROM tdeviceinfo
                WHERE sitenum=@SiteId
                ORDER BY devicenum;
                """,
                new { SiteId = siteId },
                cancellationToken: cancellationToken))).AsList();

        IReadOnlyList<ParkingDeviceLink> links =
            (await connection.QueryAsync<ParkingDeviceLink>(new CommandDefinition("""
                SELECT sitenum SiteId,sourcedeviceid SourceDeviceId,
                       targetdeviceid TargetDeviceId,linktype LinkType,
                       useflag Enabled
                FROM tdevicelink
                WHERE sitenum=@SiteId
                ORDER BY sourcedeviceid,targetdeviceid;
                """,
                new { SiteId = siteId },
                cancellationToken: cancellationToken))).AsList();

        IReadOnlyList<OperationVariableRow> variableRows =
            (await connection.QueryAsync<OperationVariableRow>(new CommandDefinition("""
                SELECT groupnum Groupnum,cmdtype CommandType,val Value,opt `Option`
                FROM tparkvariable
                WHERE sitenum=@SiteId AND useflag=1
                ORDER BY groupnum,cmdtype;
                """,
                new { SiteId = siteId },
                cancellationToken: cancellationToken))).AsList();
        IReadOnlyList<ParkingOperationVariable> variables = variableRows
            .Select(x => new ParkingOperationVariable(
                x.Groupnum,
                x.CommandType,
                UsesOption(x.CommandType) ? x.Option : x.Value))
            .ToList();

        return new SiteConfiguration(site, lanes, devices, links, variables);
    }

    public async Task<bool> SaveSiteAsync(
        ParkingSite site,
        CancellationToken cancellationToken)
    {
        await using MySqlConnection connection = new(_connectionString);
        bool exists = await connection.ExecuteScalarAsync<bool>(new CommandDefinition("""
            SELECT EXISTS(
                SELECT 1 FROM tparkings WHERE sitenum=@SiteId
            );
            """, site, cancellationToken: cancellationToken));
        if (!exists)
            return false;
        await connection.ExecuteAsync(new CommandDefinition("""
            UPDATE tparkings
            SET parkname=@SiteName,useflag=@Enabled
            WHERE sitenum=@SiteId;
            """, site, cancellationToken: cancellationToken));
        return true;
    }

    public Task SaveLaneAsync(ParkingLane lane, CancellationToken cancellationToken) =>
        ExecuteAsync("""
            INSERT INTO tparkings
            (sitenum,groupnum,parkname,parktype,addr,tel,boss,sitekeyhash,useflag)
            SELECT sitenum,@GroupNumber,parkname,parktype,addr,tel,boss,sitekeyhash,useflag
            FROM tparkings
            WHERE sitenum=@SiteId
            ORDER BY groupnum LIMIT 1
            ON DUPLICATE KEY UPDATE groupnum=VALUES(groupnum);

            INSERT INTO tlaneinfo
            (laneid,sitenum,groupnum,lanename,direction,useflag)
            VALUES
            (@LaneId,@SiteId,@GroupNumber,@LaneName,UPPER(@Direction),@Enabled)
            ON DUPLICATE KEY UPDATE sitenum=@SiteId,groupnum=@GroupNumber,
                lanename=@LaneName,direction=UPPER(@Direction),useflag=@Enabled;
            """, lane, cancellationToken);

    public async Task SaveDeviceAsync(
        ParkingDevice device,
        CancellationToken cancellationToken)
    {
        await using MySqlConnection connection = new(_connectionString);
        int groupnum = await ResolveDeviceGroupAsync(
            connection, null, device.SiteId, device.LaneId, device.DeviceId,
            cancellationToken);
        int deviceType = DeviceTypeCode(device.DeviceType);
        await connection.ExecuteAsync(new CommandDefinition("""
            INSERT INTO tdeviceinfo
            (deviceid,sitenum,groupnum,laneid,devicenum,devicetype,
             devicename,ip,port,useflag)
            VALUES
            (@DeviceId,@SiteId,@Groupnum,@LaneId,@DeviceNumber,@DeviceType,
             @DeviceName,@IpAddress,@Port,@Enabled)
            ON DUPLICATE KEY UPDATE sitenum=@SiteId,groupnum=@Groupnum,
                laneid=@LaneId,devicenum=@DeviceNumber,devicetype=@DeviceType,
                devicename=@DeviceName,ip=@IpAddress,port=@Port,useflag=@Enabled;
            """,
            new
            {
                device.DeviceId,
                device.SiteId,
                Groupnum = groupnum,
                device.LaneId,
                device.DeviceNumber,
                DeviceType = deviceType,
                device.DeviceName,
                device.IpAddress,
                device.Port,
                device.Enabled
            },
            cancellationToken: cancellationToken));
        if (deviceType == 8)
        {
            await connection.ExecuteAsync(new CommandDefinition("""
                INSERT INTO tparksync(sitenum,deviceid,configversion,appliedversion,status)
                VALUES(@SiteId,@DeviceId,0,0,'WAIT')
                ON DUPLICATE KEY UPDATE deviceid=VALUES(deviceid);
                """,
                new { device.SiteId, device.DeviceId },
                cancellationToken: cancellationToken));
        }
    }

    public Task SaveDeviceLinkAsync(
        ParkingDeviceLink link,
        CancellationToken cancellationToken) =>
        ExecuteAsync("""
            INSERT INTO tdevicelink
            (sitenum,groupnum,sourcedeviceid,targetdeviceid,linktype,useflag)
            SELECT @SiteId,d.groupnum,@SourceDeviceId,@TargetDeviceId,
                   UPPER(@LinkType),@Enabled
            FROM tdeviceinfo d
            WHERE d.deviceid=@SourceDeviceId AND d.sitenum=@SiteId
            ON DUPLICATE KEY UPDATE groupnum=VALUES(groupnum),useflag=@Enabled;
            """, link, cancellationToken);

    public async Task<bool> ValidateSiteKeyAsync(
        long siteId,
        string siteKey,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(siteKey))
            return false;
        await using MySqlConnection connection = new(_connectionString);
        string? stored = await connection.QueryFirstOrDefaultAsync<string>(
            new CommandDefinition("""
                SELECT sitekeyhash FROM tparkings
                WHERE sitenum=@SiteId AND useflag=1
                ORDER BY groupnum LIMIT 1;
                """,
                new { SiteId = siteId },
                cancellationToken: cancellationToken));
        if (stored is null || stored.Length != 64)
            return false;
        string supplied = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(siteKey)));
        return CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(stored.ToUpperInvariant()),
            Encoding.ASCII.GetBytes(supplied));
    }

    public async Task<VersionedSiteConfiguration?> GetVersionedAsync(
        long siteId,
        CancellationToken cancellationToken)
    {
        SiteConfiguration? configuration = await GetAsync(siteId, cancellationToken);
        if (configuration is null)
            return null;
        await using MySqlConnection connection = new(_connectionString);
        SyncRow? state = await connection.QuerySingleOrDefaultAsync<SyncRow>(
            new CommandDefinition("""
                SELECT MAX(configversion) Version,MAX(moddate) UpdatedAt
                FROM tparksync
                WHERE sitenum=@SiteId;
                """,
                new { SiteId = siteId },
                cancellationToken: cancellationToken));
        return new VersionedSiteConfiguration(
            configuration,
            state?.Version ?? 0,
            state?.UpdatedAt is null
                ? DateTimeOffset.MinValue
                : ParkingLocalTime.FromDatabase(state.UpdatedAt.Value).ToUniversalTime());
    }

    public async Task<bool> SaveVersionedAsync(
        VersionedSiteConfiguration value,
        CancellationToken cancellationToken)
    {
        long siteId = value.Configuration.Site.SiteId;
        await using MySqlConnection connection = new(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using MySqlTransaction transaction =
            await connection.BeginTransactionAsync(cancellationToken);

        long current = await connection.QuerySingleOrDefaultAsync<long>(
            new CommandDefinition("""
                SELECT COALESCE(MAX(configversion),0)
                FROM tparksync
                WHERE sitenum=@SiteId FOR UPDATE;
                """,
                new { SiteId = siteId },
                transaction,
                cancellationToken: cancellationToken));
        if (value.Version <= current)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        await connection.ExecuteAsync(new CommandDefinition("""
            UPDATE tparkings SET parkname=@SiteName,useflag=@Enabled
            WHERE sitenum=@SiteId;
            UPDATE tlaneinfo SET useflag=0 WHERE sitenum=@SiteId;
            UPDATE tdeviceinfo SET useflag=0 WHERE sitenum=@SiteId;
            UPDATE tdevicelink SET useflag=0 WHERE sitenum=@SiteId;
            DELETE FROM tparkvariable WHERE sitenum=@SiteId;
            """,
            value.Configuration.Site,
            transaction,
            cancellationToken: cancellationToken));

        foreach (ParkingLane lane in value.Configuration.Lanes)
        {
            await connection.ExecuteAsync(new CommandDefinition("""
                INSERT INTO tparkings
                (sitenum,groupnum,parkname,parktype,addr,tel,boss,sitekeyhash,useflag)
                SELECT sitenum,@GroupNumber,parkname,parktype,addr,tel,boss,
                       sitekeyhash,useflag
                FROM tparkings
                WHERE sitenum=@SiteId
                ORDER BY groupnum LIMIT 1
                ON DUPLICATE KEY UPDATE groupnum=VALUES(groupnum);

                INSERT INTO tlaneinfo
                (laneid,sitenum,groupnum,lanename,direction,useflag)
                VALUES
                (@LaneId,@SiteId,@GroupNumber,@LaneName,UPPER(@Direction),@Enabled)
                ON DUPLICATE KEY UPDATE sitenum=@SiteId,groupnum=@GroupNumber,
                    lanename=@LaneName,direction=UPPER(@Direction),useflag=@Enabled;
                """,
                lane,
                transaction,
                cancellationToken: cancellationToken));
        }

        foreach (ParkingDevice device in value.Configuration.Devices)
        {
            int groupnum = await ResolveDeviceGroupAsync(
                connection, transaction, device.SiteId, device.LaneId,
                device.DeviceId, cancellationToken);
            await connection.ExecuteAsync(new CommandDefinition("""
                INSERT INTO tdeviceinfo
                (deviceid,sitenum,groupnum,laneid,devicenum,devicetype,
                 devicename,ip,port,useflag)
                VALUES
                (@DeviceId,@SiteId,@Groupnum,@LaneId,@DeviceNumber,@DeviceType,
                 @DeviceName,@IpAddress,@Port,@Enabled)
                ON DUPLICATE KEY UPDATE sitenum=@SiteId,groupnum=@Groupnum,
                    laneid=@LaneId,devicenum=@DeviceNumber,devicetype=@DeviceType,
                    devicename=@DeviceName,ip=@IpAddress,port=@Port,useflag=@Enabled;
                """,
                new
                {
                    device.DeviceId,
                    device.SiteId,
                    Groupnum = groupnum,
                    device.LaneId,
                    device.DeviceNumber,
                    DeviceType = DeviceTypeCode(device.DeviceType),
                    device.DeviceName,
                    device.IpAddress,
                    device.Port,
                    device.Enabled
                },
                transaction,
                cancellationToken: cancellationToken));
        }

        foreach (ParkingDeviceLink link in value.Configuration.DeviceLinks ?? [])
        {
            await connection.ExecuteAsync(new CommandDefinition("""
                INSERT INTO tdevicelink
                (sitenum,groupnum,sourcedeviceid,targetdeviceid,linktype,useflag)
                SELECT @SiteId,d.groupnum,@SourceDeviceId,@TargetDeviceId,
                       UPPER(@LinkType),@Enabled
                FROM tdeviceinfo d
                WHERE d.deviceid=@SourceDeviceId AND d.sitenum=@SiteId
                ON DUPLICATE KEY UPDATE groupnum=VALUES(groupnum),useflag=@Enabled;
                """,
                link,
                transaction,
                cancellationToken: cancellationToken));
        }

        foreach (ParkingOperationVariable variable in
                 value.Configuration.OperationVariables ?? [])
        {
            bool usesOption = UsesOption(variable.CommandType);
            await connection.ExecuteAsync(new CommandDefinition("""
                INSERT INTO tparkvariable(sitenum,groupnum,cmdtype,val,opt,useflag)
                VALUES(@SiteId,@Groupnum,@CommandType,@Value,@Option,1);
                """,
                new
                {
                    SiteId = siteId,
                    variable.Groupnum,
                    variable.CommandType,
                    Value = usesOption ? "0" : variable.Value,
                    Option = usesOption ? variable.Value : null
                },
                transaction,
                cancellationToken: cancellationToken));
        }

        await connection.ExecuteAsync(new CommandDefinition("""
            INSERT INTO tparksync
            (sitenum,deviceid,configversion,appliedversion,status,moddate)
            SELECT @SiteId,deviceid,@Version,0,'WAIT',@UpdatedAt
            FROM tdeviceinfo
            WHERE sitenum=@SiteId AND devicetype=8 AND useflag=1
            ON DUPLICATE KEY UPDATE configversion=@Version,status='WAIT',
                                    moddate=@UpdatedAt;
            """,
            new
            {
                SiteId = siteId,
                value.Version,
                UpdatedAt = ParkingLocalTime.ToDatabase(value.UpdatedAtUtc)
            },
            transaction,
            cancellationToken: cancellationToken));
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public Task TouchVersionAsync(long siteId, CancellationToken cancellationToken) =>
        ExecuteAsync("""
            UPDATE tparksync
            SET configversion=configversion+1,moddate=CURRENT_TIMESTAMP
            WHERE sitenum=@SiteId;
            """, new { SiteId = siteId }, cancellationToken);

    private async Task ExecuteAsync(
        string sql,
        object value,
        CancellationToken cancellationToken)
    {
        await using MySqlConnection connection = new(_connectionString);
        await connection.ExecuteAsync(new CommandDefinition(
            sql, value, cancellationToken: cancellationToken));
    }

    private static async Task<int> ResolveDeviceGroupAsync(
        MySqlConnection connection,
        MySqlTransaction? transaction,
        long siteId,
        long? laneId,
        long deviceId,
        CancellationToken cancellationToken) =>
        await connection.QuerySingleAsync<int>(new CommandDefinition("""
            SELECT COALESCE(
                (SELECT groupnum FROM tlaneinfo
                 WHERE sitenum=@SiteId AND laneid=@LaneId LIMIT 1),
                (SELECT groupnum FROM tdeviceinfo
                 WHERE sitenum=@SiteId AND deviceid=@DeviceId LIMIT 1),
                (SELECT MIN(groupnum) FROM tparkings WHERE sitenum=@SiteId)) Groupnum;
            """,
            new { SiteId = siteId, LaneId = laneId, DeviceId = deviceId },
            transaction,
            cancellationToken: cancellationToken));

    private static int DeviceTypeCode(string value) => value.ToUpperInvariant() switch
    {
        "OPERATOR" => 1,
        "KIOSK" => 2,
        "LPR" => 3,
        "TICKET" => 4,
        "PDA" => 5,
        "LDM" => 6,
        "GATE" => 7,
        "EDGE" => 8,
        _ => 0
    };

    private static bool UsesOption(string commandType) =>
        commandType.StartsWith("CMD_OPTIME", StringComparison.OrdinalIgnoreCase) ||
        commandType.Equals("CMD_MAXDAILY_FEE", StringComparison.OrdinalIgnoreCase) ||
        commandType.Equals("CMD_GRACE_TIME", StringComparison.OrdinalIgnoreCase) ||
        commandType.Equals("CMD_PREPAY_GRACE", StringComparison.OrdinalIgnoreCase) ||
        commandType.Equals("CMD_SERVICE_TIME", StringComparison.OrdinalIgnoreCase) ||
        commandType.Equals("CMD_DUPLICATE_ENTRY_TIME", StringComparison.OrdinalIgnoreCase);

    private sealed class SyncRow
    {
        public long Version { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    private sealed class OperationVariableRow
    {
        public int Groupnum { get; set; }
        public string CommandType { get; set; } = "";
        public string? Value { get; set; }
        public string? Option { get; set; }
    }
}
