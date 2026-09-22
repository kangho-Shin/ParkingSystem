using Parking.Contracts;

namespace Parking.EdgeService;

public sealed class KioskExitCoordinator : IKioskExitCoordinator
{
    private readonly LocalConfigurationStore _configurationStore;
    private readonly KioskNotificationService _notifications;
    private readonly KioskPendingRepository _pending;
    private readonly EdgeEventService _events;
    private readonly GatewayClient _gateway;
    private readonly SqliteOutboxRepository _outbox;
    private readonly EdgeMonitoringRepository _monitoring;
    private readonly IDisplayBoardOutput _display;

    public KioskExitCoordinator(
        LocalConfigurationStore configurationStore,
        KioskNotificationService notifications,
        KioskPendingRepository pending,
        EdgeEventService events,
        GatewayClient gateway,
        SqliteOutboxRepository outbox,
        EdgeMonitoringRepository monitoring,
        IDisplayBoardOutput display)
    {
        _configurationStore = configurationStore;
        _notifications = notifications;
        _pending = pending;
        _events = events;
        _gateway = gateway;
        _outbox = outbox;
        _monitoring = monitoring;
        _display = display;
    }

    public async Task<FieldEventResponse> RouteAsync(
        LprRecognition recognition, CancellationToken cancellationToken)
    {
        SiteConfiguration? configuration = await _configurationStore.GetAsync(
            recognition.SiteId, cancellationToken);
        ParkingDevice? kiosk = configuration is null ? null :
            DeviceLinkSelector.FindTargets(configuration, recognition.DeviceId, "KIOSK", "KIOSK")
                .FirstOrDefault();
        if (kiosk is null)
        {
            FieldEventResponse standalone = await _events.AcceptExitAsync(
                ToExitRequest(recognition), cancellationToken);
            await _display.SendAsync(recognition, standalone, cancellationToken);
            return standalone;
        }

        bool notified = await _notifications.EnqueueAndNotifyAsync(
            kiosk.DeviceId, recognition, cancellationToken);
        if (notified)
            return new FieldEventResponse(
                recognition.EventId, true, null, "KIOSK_NOTIFIED",
                "무인정산기로 전송했습니다.", false);

        KioskOfflinePolicy policy = configuration is null
            ? KioskOfflinePolicy.Open
            : KioskOfflinePolicyResolver.Resolve(configuration, recognition.Groupnum);
        if (policy == KioskOfflinePolicy.Block)
            return new FieldEventResponse(
                recognition.EventId, false, null, "KIOSK_OFFLINE_BLOCKED",
                "무인정산기 연결 대기 중입니다.", false);

        OfflineKioskExitRequest offlineRequest = new(
            recognition.EventId, recognition.SiteId, recognition.Groupnum,
            recognition.LaneId, recognition.DeviceId, recognition.CarNumber,
            recognition.RecognizedAt, recognition.ImageFileName);
        await _outbox.EnqueueOfflineKioskExitAsync(offlineRequest, cancellationToken);
        if (!await _pending.TryBeginOfflineOpenAsync(recognition.EventId, cancellationToken))
            return new FieldEventResponse(
                recognition.EventId, false, null, "KIOSK_EXIT_ALREADY_PROCESSING",
                "출차 처리 중입니다.", false);
        ExitEventRequest exitRequest = ToExitRequest(recognition);
        await _monitoring.RecordExitAsync(
            exitRequest,
            new FieldEventResponse(
                recognition.EventId, false, null, "PENDING", "중앙 전송 대기 중입니다.", false),
            EdgeDeliveryState.Pending,
            cancellationToken);

        FieldEventResponse opened;
        try
        {
            opened = await _gateway.SendOfflineKioskExitAsync(offlineRequest, cancellationToken);
            if (opened.EventId != recognition.EventId)
                throw new InvalidOperationException("응답 EventId가 다릅니다.");
            await _outbox.MarkCompletedAsync(recognition.EventId, cancellationToken);
            if (opened.OpenBarrier != true)
                opened = opened with
                {
                    Accepted = true,
                    ResultCode = "KIOSK_OFFLINE_OPEN_LOCAL",
                    DisplayMessage = "무인 연결 장애로 출차합니다.",
                    OpenBarrier = true
                };
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            opened = new FieldEventResponse(
                recognition.EventId, true, null, "KIOSK_OFFLINE_OPEN_PENDING",
                "무인 연결 장애로 출차합니다.", true);
        }
        await _monitoring.RecordExitAsync(
            exitRequest,
            opened,
            opened.ResultCode == "KIOSK_OFFLINE_OPEN_PENDING"
                ? EdgeDeliveryState.Pending
                : EdgeDeliveryState.Completed,
            cancellationToken);
        await _pending.MarkOfflineOpenedAsync(recognition.EventId, cancellationToken);
        await _display.SendAsync(recognition, opened, cancellationToken);
        return opened;
    }

    public async Task<FieldEventResponse?> CompleteAsync(
        long kioskDeviceId, Guid eventId, CancellationToken cancellationToken)
    {
        PendingKioskEvent? pending = await _pending.GetAsync(eventId, cancellationToken);
        if (pending is null || pending.KioskDeviceId != kioskDeviceId) return null;
        if (!await _pending.TryBeginCompletionAsync(eventId, cancellationToken)) return null;
        try
        {
            FieldEventResponse response = await _events.AcceptExitAsync(
                ToExitRequest(pending.Recognition), cancellationToken);
            await _display.SendFromDeviceAsync(
                kioskDeviceId, pending.Recognition, response, cancellationToken);
            await _pending.MarkCompletedAsync(eventId, cancellationToken);
            return response;
        }
        catch
        {
            await _pending.ReleaseCompletionAsync(eventId, CancellationToken.None);
            throw;
        }
    }

    public Task DisplayAsync(
        long kioskDeviceId,
        long siteId,
        int groupnum,
        string carNumber,
        string displayMessage,
        int displaySeconds,
        CancellationToken cancellationToken)
    {
        LprRecognition recognition = new(
            Guid.NewGuid(), siteId, groupnum, kioskDeviceId, 0,
            "Exit", DateTimeOffset.Now, carNumber, "");
        FieldEventResponse response = new(
            recognition.EventId, true, null,
            "KIOSK_SETTLEMENT_COMPLETED", displayMessage, false);
        return _display.SendFromDeviceAsync(
            kioskDeviceId, recognition, response, cancellationToken, displaySeconds);
    }

    public async Task<FieldEventResponse?> CompleteManualAsync(
        long kioskDeviceId,
        long siteId,
        int groupnum,
        string carNumber,
        DateTimeOffset exitAt,
        CancellationToken cancellationToken)
    {
        SiteConfiguration? configuration = await _configurationStore.GetAsync(
            siteId, cancellationToken);
        ParkingDevice? lpr = configuration is null
            ? null
            : DeviceLinkSelector.FindSource(configuration, kioskDeviceId, "KIOSK", "LPR");
        ParkingLane? lane = lpr?.LaneId is null || configuration is null
            ? null
            : configuration.Lanes.FirstOrDefault(x =>
                x.Enabled && x.LaneId == lpr.LaneId && x.GroupNumber == groupnum &&
                string.Equals(x.Direction, "Exit", StringComparison.OrdinalIgnoreCase));
        if (lpr is null || lane is null) return null;

        LprRecognition recognition = new(
            Guid.NewGuid(), siteId, groupnum, lpr.DeviceId, lane.LaneId,
            "Exit", exitAt, carNumber, "");
        FieldEventResponse response = await _events.AcceptExitAsync(
            ToExitRequest(recognition), cancellationToken);
        if (!response.Accepted) return response;

        FieldEventResponse displayResponse = response with
        {
            DisplayMessage = "정산 완료되었습니다."
        };
        await _display.SendFromDeviceAsync(
            kioskDeviceId, recognition, displayResponse, cancellationToken);
        return response;
    }

    private static ExitEventRequest ToExitRequest(LprRecognition recognition) => new(
        recognition.EventId, recognition.SiteId, recognition.LaneId,
        recognition.DeviceId, recognition.CarNumber, recognition.RecognizedAt,
        recognition.Groupnum, EventType: ParkingEventType.Exit,
        OutImage: recognition.ImageFileName);
}
