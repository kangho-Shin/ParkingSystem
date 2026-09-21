using System.Globalization;
using Dapper;
using Microsoft.Data.Sqlite;
using Parking.Contracts;

namespace Parking.EdgeService;

public sealed class EdgeMonitoringRepository
{
    private readonly string _connectionString;

    public EdgeMonitoringRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            CREATE TABLE IF NOT EXISTS monitor_entry (
                entry_event_id TEXT NOT NULL PRIMARY KEY,
                parking_session_id INTEGER NULL,
                site_id INTEGER NOT NULL,
                groupnum INTEGER NOT NULL,
                lane_id INTEGER NOT NULL,
                device_id INTEGER NOT NULL,
                car_number TEXT NOT NULL,
                in_at_utc TEXT NOT NULL,
                in_image TEXT NULL,
                delivery_state INTEGER NOT NULL,
                result_code TEXT NULL);
            CREATE INDEX IF NOT EXISTS ix_monitor_entry_session
                ON monitor_entry(parking_session_id);
            CREATE INDEX IF NOT EXISTS ix_monitor_entry_vehicle
                ON monitor_entry(site_id,groupnum,car_number,in_at_utc);

            CREATE TABLE IF NOT EXISTS monitor_activity (
                activity_id TEXT NOT NULL,
                activity_type INTEGER NOT NULL,
                parking_session_id INTEGER NULL,
                site_id INTEGER NOT NULL,
                groupnum INTEGER NOT NULL,
                lane_id INTEGER NULL,
                device_id INTEGER NULL,
                car_number TEXT NOT NULL,
                occurred_at_utc TEXT NOT NULL,
                in_image TEXT NULL,
                out_image TEXT NULL,
                original_fee INTEGER NULL,
                discount_fee INTEGER NULL,
                paid_amount INTEGER NULL,
                payment_method TEXT NULL,
                open_barrier INTEGER NULL,
                delivery_state INTEGER NOT NULL,
                result_code TEXT NULL,
                PRIMARY KEY(activity_id,activity_type));
            CREATE INDEX IF NOT EXISTS ix_monitor_activity_occurred
                ON monitor_activity(occurred_at_utc DESC);
            """;

        await using SqliteConnection connection = new(_connectionString);
        await connection.ExecuteAsync(new CommandDefinition(sql, cancellationToken: cancellationToken));
    }

    public async Task RecordEntryAsync(
        FieldEventRequest request,
        CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT OR IGNORE INTO monitor_entry
            (entry_event_id,parking_session_id,site_id,groupnum,lane_id,device_id,
             car_number,in_at_utc,in_image,delivery_state,result_code)
            VALUES
            (@EventId,NULL,@SiteId,@Groupnum,@LaneId,@DeviceId,@CarNumber,@InAt,
             @InImage,@DeliveryState,NULL);
            """;

        await using SqliteConnection connection = new(_connectionString);
        await connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            EventId = request.EventId.ToString("D"),
            request.SiteId,
            request.Groupnum,
            request.LaneId,
            request.DeviceId,
            request.CarNumber,
            InAt = request.InDateTime.ToUniversalTime().ToString("O"),
            request.InImage,
            DeliveryState = (int)EdgeDeliveryState.Pending
        }, cancellationToken: cancellationToken));
    }

    public async Task CompleteEntryDeliveryAsync(
        Guid eventId,
        FieldEventResponse response,
        EdgeDeliveryState deliveryState,
        CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE monitor_entry SET
                parking_session_id=COALESCE(@ParkingSessionId,parking_session_id),
                delivery_state=@DeliveryState,
                result_code=@ResultCode
            WHERE entry_event_id=@EventId;
            """;

        await using SqliteConnection connection = new(_connectionString);
        await connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            EventId = eventId.ToString("D"),
            response.ParkingSessionId,
            DeliveryState = (int)deliveryState,
            response.ResultCode
        }, cancellationToken: cancellationToken));
    }

    public async Task RecordPaymentAsync(
        CompletePaymentRequest request,
        EdgeDeliveryState deliveryState,
        string? resultCode,
        CancellationToken cancellationToken)
    {
        const string selectEntry = """
            SELECT site_id SiteId,groupnum Groupnum,car_number CarNumber,in_image InImage
            FROM monitor_entry
            WHERE parking_session_id=@ParkingSessionId
            ORDER BY in_at_utc DESC LIMIT 1;
            """;
        const string insertActivity = """
            INSERT INTO monitor_activity
            (activity_id,activity_type,parking_session_id,site_id,groupnum,lane_id,
             device_id,car_number,occurred_at_utc,in_image,out_image,original_fee,
             discount_fee,paid_amount,payment_method,open_barrier,delivery_state,result_code)
            VALUES
            (@ActivityId,@ActivityType,@ParkingSessionId,@SiteId,@Groupnum,NULL,NULL,
             @CarNumber,@OccurredAt,@InImage,NULL,@OriginalFee,@DiscountFee,@PaidAmount,
             @PaymentMethod,NULL,@DeliveryState,@ResultCode)
            ON CONFLICT(activity_id,activity_type) DO UPDATE SET
                delivery_state=@DeliveryState,result_code=@ResultCode;
            """;

        await using SqliteConnection connection = new(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using SqliteTransaction transaction = connection.BeginTransaction();
        EntryLookupRow? entry = await connection.QuerySingleOrDefaultAsync<EntryLookupRow>(
            new CommandDefinition(
                selectEntry,
                new { request.ParkingSessionId },
                transaction,
                cancellationToken: cancellationToken));

        await connection.ExecuteAsync(new CommandDefinition(insertActivity, new
        {
            ActivityId = request.PaymentId.ToString("D"),
            ActivityType = (int)EdgeActivityType.Payment,
            request.ParkingSessionId,
            request.SiteId,
            Groupnum = entry?.Groupnum ?? 0,
            CarNumber = entry?.CarNumber ?? "",
            OccurredAt = request.PaidAt.ToUniversalTime().ToString("O"),
            InImage = entry?.InImage,
            request.OriginalFee,
            request.DiscountFee,
            request.PaidAmount,
            request.PaymentMethod,
            DeliveryState = (int)deliveryState,
            ResultCode = resultCode
        }, transaction, cancellationToken: cancellationToken));
        await TrimActivitiesAsync(connection, transaction, cancellationToken);
        transaction.Commit();
    }

    public async Task RecordExitAsync(
        ExitEventRequest request,
        FieldEventResponse response,
        EdgeDeliveryState deliveryState,
        CancellationToken cancellationToken)
    {
        const string selectEntry = """
            SELECT entry_event_id EntryEventId,parking_session_id ParkingSessionId,
                   in_image InImage
            FROM monitor_entry
            WHERE parking_session_id=@ParkingSessionId
               OR (site_id=@SiteId AND groupnum=@Groupnum AND car_number=@CarNumber)
            ORDER BY CASE WHEN parking_session_id=@ParkingSessionId THEN 0 ELSE 1 END,
                     in_at_utc DESC LIMIT 1;
            """;
        const string insertActivity = """
            INSERT INTO monitor_activity
            (activity_id,activity_type,parking_session_id,site_id,groupnum,lane_id,
             device_id,car_number,occurred_at_utc,in_image,out_image,original_fee,
             discount_fee,paid_amount,payment_method,open_barrier,delivery_state,result_code)
            VALUES
            (@ActivityId,@ActivityType,@ParkingSessionId,@SiteId,@Groupnum,@LaneId,
             @DeviceId,@CarNumber,@OccurredAt,@InImage,@OutImage,NULL,NULL,NULL,NULL,
             @OpenBarrier,@DeliveryState,@ResultCode)
            ON CONFLICT(activity_id,activity_type) DO UPDATE SET
                parking_session_id=COALESCE(@ParkingSessionId,parking_session_id),
                open_barrier=@OpenBarrier,delivery_state=@DeliveryState,result_code=@ResultCode;
            """;

        await using SqliteConnection connection = new(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using SqliteTransaction transaction = connection.BeginTransaction();
        ExitEntryLookupRow? entry = await connection.QuerySingleOrDefaultAsync<ExitEntryLookupRow>(
            new CommandDefinition(selectEntry, new
            {
                response.ParkingSessionId,
                request.SiteId,
                request.Groupnum,
                request.CarNumber
            }, transaction, cancellationToken: cancellationToken));

        await connection.ExecuteAsync(new CommandDefinition(insertActivity, new
        {
            ActivityId = request.EventId.ToString("D"),
            ActivityType = (int)EdgeActivityType.Exit,
            ParkingSessionId = response.ParkingSessionId ?? entry?.ParkingSessionId,
            request.SiteId,
            request.Groupnum,
            request.LaneId,
            request.DeviceId,
            request.CarNumber,
            OccurredAt = request.OutDateTime.ToUniversalTime().ToString("O"),
            InImage = entry?.InImage,
            request.OutImage,
            response.OpenBarrier,
            DeliveryState = (int)deliveryState,
            response.ResultCode
        }, transaction, cancellationToken: cancellationToken));

        if (response.OpenBarrier && entry is not null)
        {
            await connection.ExecuteAsync(new CommandDefinition(
                "DELETE FROM monitor_entry WHERE entry_event_id=@EntryEventId;",
                new { entry.EntryEventId },
                transaction,
                cancellationToken: cancellationToken));
        }

        await TrimActivitiesAsync(connection, transaction, cancellationToken);
        transaction.Commit();
    }

    public async Task<IReadOnlyList<EdgeEntryItem>> GetEntriesAsync(
        int limit,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT entry_event_id EventId,parking_session_id ParkingSessionId,
                   site_id SiteId,groupnum Groupnum,lane_id LaneId,device_id DeviceId,
                   car_number CarNumber,in_at_utc InAt,in_image InImage,
                   delivery_state DeliveryState,result_code ResultCode
            FROM monitor_entry
            ORDER BY in_at_utc DESC LIMIT @Limit;
            """;
        await using SqliteConnection connection = new(_connectionString);
        IEnumerable<EntryRow> rows = await connection.QueryAsync<EntryRow>(
            new CommandDefinition(sql, new { Limit = limit }, cancellationToken: cancellationToken));
        return rows.Select(ToEntryItem).ToList();
    }

    public async Task<IReadOnlyList<EdgeActivityItem>> GetActivitiesAsync(
        int limit,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT activity_id ActivityId,activity_type ActivityType,
                   parking_session_id ParkingSessionId,site_id SiteId,groupnum Groupnum,
                   lane_id LaneId,device_id DeviceId,car_number CarNumber,
                   occurred_at_utc OccurredAt,in_image InImage,out_image OutImage,
                   original_fee OriginalFee,discount_fee DiscountFee,paid_amount PaidAmount,
                   payment_method PaymentMethod,open_barrier OpenBarrier,
                   delivery_state DeliveryState,result_code ResultCode
            FROM monitor_activity
            ORDER BY occurred_at_utc DESC,rowid DESC LIMIT @Limit;
            """;
        await using SqliteConnection connection = new(_connectionString);
        IEnumerable<ActivityRow> rows = await connection.QueryAsync<ActivityRow>(
            new CommandDefinition(sql, new { Limit = limit }, cancellationToken: cancellationToken));
        return rows.Select(ToActivityItem).ToList();
    }

    private static async Task TrimActivitiesAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        CancellationToken cancellationToken)
    {
        await connection.ExecuteAsync(new CommandDefinition("""
            DELETE FROM monitor_activity
            WHERE rowid IN (
                SELECT rowid FROM monitor_activity
                ORDER BY occurred_at_utc DESC,rowid DESC
                LIMIT -1 OFFSET 1000);
            """, transaction: transaction, cancellationToken: cancellationToken));
    }

    private static EdgeEntryItem ToEntryItem(EntryRow row) => new(
        Guid.Parse(row.EventId), row.ParkingSessionId, row.SiteId, checked((int)row.Groupnum),
        row.LaneId, row.DeviceId, row.CarNumber, ParseTime(row.InAt), row.InImage,
        (EdgeDeliveryState)checked((int)row.DeliveryState), row.ResultCode);

    private static EdgeActivityItem ToActivityItem(ActivityRow row) => new(
        Guid.Parse(row.ActivityId), (EdgeActivityType)checked((int)row.ActivityType),
        row.ParkingSessionId, row.SiteId, checked((int)row.Groupnum), row.LaneId,
        row.DeviceId, row.CarNumber, ParseTime(row.OccurredAt), row.InImage, row.OutImage,
        row.OriginalFee, row.DiscountFee, row.PaidAmount, row.PaymentMethod,
        row.OpenBarrier is null ? null : row.OpenBarrier != 0,
        (EdgeDeliveryState)checked((int)row.DeliveryState), row.ResultCode);

    private static DateTimeOffset ParseTime(string value) =>
        DateTimeOffset.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

    private sealed record EntryLookupRow(long SiteId, long Groupnum, string CarNumber, string? InImage);
    private sealed record ExitEntryLookupRow(string EntryEventId, long? ParkingSessionId, string? InImage);
    private sealed record EntryRow(
        string EventId, long? ParkingSessionId, long SiteId, long Groupnum, long LaneId,
        long DeviceId, string CarNumber, string InAt, string? InImage, long DeliveryState,
        string? ResultCode);
    private sealed record ActivityRow(
        string ActivityId, long ActivityType, long? ParkingSessionId, long SiteId,
        long Groupnum, long? LaneId, long? DeviceId, string CarNumber, string OccurredAt,
        string? InImage, string? OutImage, long? OriginalFee, long? DiscountFee,
        long? PaidAmount, string? PaymentMethod, long? OpenBarrier, long DeliveryState,
        string? ResultCode);
}
