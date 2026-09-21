using Dapper;
using Microsoft.Data.Sqlite;
using Parking.Contracts;

namespace Parking.EdgeService;

public sealed class LocalConfigurationStore
{
    private readonly string _connectionString;
    public LocalConfigurationStore(string connectionString) => _connectionString = connectionString;

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            CREATE TABLE IF NOT EXISTS local_site(site_id INTEGER PRIMARY KEY,site_name TEXT NOT NULL,enabled INTEGER NOT NULL);
            CREATE TABLE IF NOT EXISTS local_lane(site_id INTEGER NOT NULL,lane_id INTEGER NOT NULL,groupnum INTEGER NOT NULL,lane_name TEXT NOT NULL,direction TEXT NOT NULL,enabled INTEGER NOT NULL,PRIMARY KEY(site_id,lane_id));
            CREATE TABLE IF NOT EXISTS local_device(site_id INTEGER NOT NULL,device_id INTEGER NOT NULL,lane_id INTEGER NULL,device_number INTEGER NOT NULL,device_type TEXT NOT NULL,device_name TEXT NOT NULL,ip_address TEXT NULL,port INTEGER NULL,enabled INTEGER NOT NULL,PRIMARY KEY(site_id,device_id),UNIQUE(site_id,device_number));
            CREATE TABLE IF NOT EXISTS local_device_link(site_id INTEGER NOT NULL,source_device_id INTEGER NOT NULL,target_device_id INTEGER NOT NULL,link_type TEXT NOT NULL,enabled INTEGER NOT NULL,PRIMARY KEY(site_id,source_device_id,target_device_id,link_type));
            CREATE TABLE IF NOT EXISTS local_operation_variable(site_id INTEGER NOT NULL,groupnum INTEGER NOT NULL,command_type TEXT NOT NULL,value TEXT NULL,PRIMARY KEY(site_id,groupnum,command_type));
            CREATE TABLE IF NOT EXISTS local_configuration_state(site_id INTEGER PRIMARY KEY,version INTEGER NOT NULL,updated_at_utc TEXT NOT NULL);
            """;
        await using SqliteConnection connection = new(_connectionString);
        await connection.ExecuteAsync(new CommandDefinition(sql, cancellationToken: cancellationToken));
    }

    public Task SaveAsync(SiteConfiguration value, CancellationToken token) => MutateAsync(value.Site.SiteId, async (c, t) =>
    {
        await UpsertAsync(c, t, "INSERT INTO local_site VALUES(@SiteId,@SiteName,@Enabled) ON CONFLICT(site_id) DO UPDATE SET site_name=@SiteName,enabled=@Enabled;", value.Site, token);
        await c.ExecuteAsync(new CommandDefinition("DELETE FROM local_device_link WHERE site_id=@SiteId; DELETE FROM local_operation_variable WHERE site_id=@SiteId; DELETE FROM local_device WHERE site_id=@SiteId; DELETE FROM local_lane WHERE site_id=@SiteId;", new { SiteId = value.Site.SiteId }, t, cancellationToken: token));
        foreach (ParkingLane x in value.Lanes) await SaveLaneCoreAsync(c, t, x, token);
        foreach (ParkingDevice x in value.Devices) await SaveDeviceCoreAsync(c, t, x, token);
        foreach (ParkingDeviceLink x in value.DeviceLinks ?? []) await SaveLinkCoreAsync(c, t, x, token);
        foreach (ParkingOperationVariable x in value.OperationVariables ?? []) await SaveVariableCoreAsync(c, t, value.Site.SiteId, x, token);
    }, token);

    public async Task<SiteConfiguration?> GetAsync(long siteId, CancellationToken token)
    {
        await using SqliteConnection c = new(_connectionString);
        SiteRow? site = await c.QuerySingleOrDefaultAsync<SiteRow>(new CommandDefinition("SELECT site_id SiteId,site_name SiteName,enabled Enabled FROM local_site WHERE site_id=@SiteId;", new { SiteId = siteId }, cancellationToken: token));
        if (site is null) return null;
        var laneRows = (await c.QueryAsync<LaneRow>(new CommandDefinition("SELECT lane_id LaneId,site_id SiteId,groupnum GroupNumber,lane_name LaneName,direction Direction,enabled Enabled FROM local_lane WHERE site_id=@SiteId ORDER BY groupnum,lane_id;", new { SiteId = siteId }, cancellationToken: token))).ToList();
        var deviceRows = (await c.QueryAsync<DeviceRow>(new CommandDefinition("SELECT device_id DeviceId,site_id SiteId,lane_id LaneId,device_number DeviceNumber,device_type DeviceType,device_name DeviceName,ip_address IpAddress,enabled Enabled,port Port FROM local_device WHERE site_id=@SiteId ORDER BY device_number;", new { SiteId = siteId }, cancellationToken: token))).ToList();
        var linkRows = (await c.QueryAsync<LinkRow>(new CommandDefinition("SELECT site_id SiteId,source_device_id SourceDeviceId,target_device_id TargetDeviceId,link_type LinkType,enabled Enabled FROM local_device_link WHERE site_id=@SiteId ORDER BY source_device_id,target_device_id;", new { SiteId = siteId }, cancellationToken: token))).ToList();
        var variableRows = (await c.QueryAsync<VariableRow>(new CommandDefinition("SELECT groupnum Groupnum,command_type CommandType,value Value FROM local_operation_variable WHERE site_id=@SiteId ORDER BY groupnum,command_type;", new { SiteId = siteId }, cancellationToken: token))).ToList();
        return new SiteConfiguration(
            new ParkingSite(site.SiteId, site.SiteName, site.Enabled != 0),
            laneRows.Select(x => new ParkingLane(x.LaneId, x.SiteId, checked((int)x.GroupNumber), x.LaneName, x.Direction, x.Enabled != 0)).ToList(),
            deviceRows.Select(x => new ParkingDevice(x.DeviceId, x.SiteId, x.LaneId, checked((int)x.DeviceNumber), x.DeviceType, x.DeviceName, x.IpAddress, x.Enabled != 0, x.Port is null ? null : checked((int)x.Port.Value))).ToList(),
            linkRows.Select(x => new ParkingDeviceLink(x.SiteId, x.SourceDeviceId, x.TargetDeviceId, x.LinkType, x.Enabled != 0)).ToList(),
            variableRows.Select(x => new ParkingOperationVariable(checked((int)x.Groupnum), x.CommandType, x.Value)).ToList());
    }

    public Task SaveSiteAsync(ParkingSite x, CancellationToken token) => MutateAsync(x.SiteId, (c, t) => UpsertAsync(c, t, "INSERT INTO local_site VALUES(@SiteId,@SiteName,@Enabled) ON CONFLICT(site_id) DO UPDATE SET site_name=@SiteName,enabled=@Enabled;", x, token), token);
    public Task SaveLaneAsync(ParkingLane x, CancellationToken token) => MutateAsync(x.SiteId, (c, t) => SaveLaneCoreAsync(c, t, x, token), token);
    public Task SaveDeviceAsync(ParkingDevice x, CancellationToken token) => MutateAsync(x.SiteId, (c, t) => SaveDeviceCoreAsync(c, t, x, token), token);
    public Task SaveDeviceLinkAsync(ParkingDeviceLink x, CancellationToken token) => MutateAsync(x.SiteId, (c, t) => SaveLinkCoreAsync(c, t, x, token), token);
    public Task SaveOperationVariableAsync(long siteId, ParkingOperationVariable x, CancellationToken token) => MutateAsync(siteId, (c, t) => SaveVariableCoreAsync(c, t, siteId, x, token), token);
    public Task DeleteLaneAsync(long siteId, long id, CancellationToken token) => DeleteAsync(siteId, "DELETE FROM local_lane WHERE site_id=@SiteId AND lane_id=@Id;", new { SiteId = siteId, Id = id }, token);
    public Task DeleteDeviceAsync(long siteId, long id, CancellationToken token) => DeleteAsync(siteId, "DELETE FROM local_device_link WHERE site_id=@SiteId AND (source_device_id=@Id OR target_device_id=@Id); DELETE FROM local_device WHERE site_id=@SiteId AND device_id=@Id;", new { SiteId = siteId, Id = id }, token);
    public Task DeleteDeviceLinkAsync(long siteId, long sourceId, long targetId, string type, CancellationToken token) => DeleteAsync(siteId, "DELETE FROM local_device_link WHERE site_id=@SiteId AND source_device_id=@SourceId AND target_device_id=@TargetId AND link_type=@Type;", new { SiteId = siteId, SourceId = sourceId, TargetId = targetId, Type = type }, token);
    public Task DeleteOperationVariableAsync(long siteId, int groupnum, string commandType, CancellationToken token) => DeleteAsync(siteId, "DELETE FROM local_operation_variable WHERE site_id=@SiteId AND groupnum=@Groupnum AND command_type=@CommandType;", new { SiteId = siteId, Groupnum = groupnum, CommandType = commandType }, token);

    public async Task<long> GetVersionAsync(long siteId, CancellationToken token)
    {
        await using SqliteConnection c = new(_connectionString);
        return await c.QuerySingleOrDefaultAsync<long>(new CommandDefinition("SELECT version FROM local_configuration_state WHERE site_id=@SiteId;", new { SiteId = siteId }, cancellationToken: token));
    }

    public async Task<VersionedSiteConfiguration?> GetVersionedAsync(long siteId, CancellationToken token)
    {
        SiteConfiguration? configuration = await GetAsync(siteId, token);
        if (configuration is null) return null;
        await using SqliteConnection c = new(_connectionString);
        StateRow? state = await c.QuerySingleOrDefaultAsync<StateRow>(new CommandDefinition(
            "SELECT version Version,updated_at_utc UpdatedAtUtc FROM local_configuration_state WHERE site_id=@SiteId;",
            new { SiteId = siteId }, cancellationToken: token));
        return new VersionedSiteConfiguration(
            configuration,
            state?.Version ?? 0,
            state is null ? DateTimeOffset.MinValue : DateTimeOffset.Parse(state.UpdatedAtUtc, System.Globalization.CultureInfo.InvariantCulture));
    }

    public async Task ApplyRemoteAsync(VersionedSiteConfiguration value, CancellationToken token)
    {
        long siteId = value.Configuration.Site.SiteId;
        SiteConfiguration? existing = await GetAsync(siteId, token);
        long current = await GetVersionAsync(siteId, token);
        if (existing is not null && value.Version <= current) return;
        await SaveAsync(value.Configuration, token);
        await SetStateAsync(siteId, value.Version, value.UpdatedAtUtc, token);
    }

    public Task SetStateAsync(long siteId, long version, DateTimeOffset updatedAtUtc, CancellationToken token) =>
        SetStateCoreAsync(siteId, version, updatedAtUtc, token);

    public async Task<DateTimeOffset?> GetSyncedAtAsync(CancellationToken token)
    {
        await using SqliteConnection c = new(_connectionString);
        string? value = await c.QuerySingleOrDefaultAsync<string>(new CommandDefinition("SELECT updated_at_utc FROM local_configuration_state ORDER BY updated_at_utc DESC LIMIT 1;", cancellationToken: token));
        return value is null ? null : DateTimeOffset.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
    }

    private Task DeleteAsync(long siteId, string sql, object args, CancellationToken token) => MutateAsync(siteId, async (c, t) => { await c.ExecuteAsync(new CommandDefinition(sql, args, t, cancellationToken: token)); }, token);

    private async Task MutateAsync(long siteId, Func<SqliteConnection, SqliteTransaction, Task> action, CancellationToken token)
    {
        if (siteId <= 0) throw new ArgumentOutOfRangeException(nameof(siteId));
        await using SqliteConnection c = new(_connectionString);
        await c.OpenAsync(token);
        await using SqliteTransaction t = c.BeginTransaction();
        await action(c, t);
        await c.ExecuteAsync(new CommandDefinition("INSERT INTO local_configuration_state VALUES(@SiteId,1,@Now) ON CONFLICT(site_id) DO UPDATE SET version=version+1,updated_at_utc=@Now;", new { SiteId = siteId, Now = DateTimeOffset.UtcNow.ToString("O") }, t, cancellationToken: token));
        await t.CommitAsync(token);
    }

    private static Task<int> UpsertAsync(SqliteConnection c, SqliteTransaction t, string sql, object args, CancellationToken token) => c.ExecuteAsync(new CommandDefinition(sql, args, t, cancellationToken: token));
    private static Task<int> SaveLaneCoreAsync(SqliteConnection c, SqliteTransaction t, ParkingLane x, CancellationToken token) => UpsertAsync(c, t, "INSERT INTO local_lane VALUES(@SiteId,@LaneId,@GroupNumber,@LaneName,@Direction,@Enabled) ON CONFLICT(site_id,lane_id) DO UPDATE SET groupnum=@GroupNumber,lane_name=@LaneName,direction=@Direction,enabled=@Enabled;", x, token);
    private static Task<int> SaveDeviceCoreAsync(SqliteConnection c, SqliteTransaction t, ParkingDevice x, CancellationToken token) => UpsertAsync(c, t, "INSERT INTO local_device(site_id,device_id,lane_id,device_number,device_type,device_name,ip_address,port,enabled) VALUES(@SiteId,@DeviceId,@LaneId,@DeviceNumber,@DeviceType,@DeviceName,@IpAddress,@Port,@Enabled) ON CONFLICT(site_id,device_id) DO UPDATE SET lane_id=@LaneId,device_number=@DeviceNumber,device_type=@DeviceType,device_name=@DeviceName,ip_address=@IpAddress,port=@Port,enabled=@Enabled;", x, token);
    private static Task<int> SaveLinkCoreAsync(SqliteConnection c, SqliteTransaction t, ParkingDeviceLink x, CancellationToken token) => UpsertAsync(c, t, "INSERT INTO local_device_link VALUES(@SiteId,@SourceDeviceId,@TargetDeviceId,@LinkType,@Enabled) ON CONFLICT(site_id,source_device_id,target_device_id,link_type) DO UPDATE SET enabled=@Enabled;", x, token);
    private static Task<int> SaveVariableCoreAsync(SqliteConnection c, SqliteTransaction t, long siteId, ParkingOperationVariable x, CancellationToken token) => UpsertAsync(c, t, "INSERT INTO local_operation_variable VALUES(@SiteId,@Groupnum,@CommandType,@Value) ON CONFLICT(site_id,groupnum,command_type) DO UPDATE SET value=@Value;", new { SiteId = siteId, x.Groupnum, x.CommandType, x.Value }, token);
    private async Task SetStateCoreAsync(long siteId, long version, DateTimeOffset updatedAtUtc, CancellationToken token)
    {
        await using SqliteConnection c = new(_connectionString);
        await c.ExecuteAsync(new CommandDefinition("INSERT INTO local_configuration_state VALUES(@SiteId,@Version,@UpdatedAt) ON CONFLICT(site_id) DO UPDATE SET version=@Version,updated_at_utc=@UpdatedAt;", new { SiteId = siteId, Version = version, UpdatedAt = updatedAtUtc.ToUniversalTime().ToString("O") }, cancellationToken: token));
    }
    private sealed record SiteRow(long SiteId, string SiteName, long Enabled);
    private sealed record LaneRow(long LaneId, long SiteId, long GroupNumber, string LaneName, string Direction, long Enabled);
    private sealed record DeviceRow(long DeviceId, long SiteId, long? LaneId, long DeviceNumber, string DeviceType, string DeviceName, string? IpAddress, long Enabled, long? Port);
    private sealed record LinkRow(long SiteId, long SourceDeviceId, long TargetDeviceId, string LinkType, long Enabled);
    private sealed record VariableRow(long Groupnum, string CommandType, string? Value);
    private sealed record StateRow(long Version, string UpdatedAtUtc);
}
