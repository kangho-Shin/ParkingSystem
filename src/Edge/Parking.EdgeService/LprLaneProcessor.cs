using Parking.Contracts;

namespace Parking.EdgeService;

public sealed class LprLaneProcessor
{
    private readonly long _siteId;
    private readonly LprFileNameParser _parser;
    private readonly LocalConfigurationStore _configurationStore;
    private readonly EdgeEventService _eventService;
    private readonly ILogger<LprLaneProcessor> _logger;

    public LprLaneProcessor(
        IConfiguration configuration,
        LprFileNameParser parser,
        LocalConfigurationStore configurationStore,
        EdgeEventService eventService,
        ILogger<LprLaneProcessor> logger)
    {
        _siteId = configuration.GetValue<long>("Edge:SiteId");
        _parser = parser;
        _configurationStore = configurationStore;
        _eventService = eventService;
        _logger = logger;
    }

    public async Task<LprProcessResult> ProcessAsync(
        string fileName,
        CancellationToken cancellationToken)
    {
        LprParseResult parsed = _parser.Parse(fileName);
        if (!parsed.Success)
            return Reject(parsed.EventId, parsed.ErrorCode ?? "INVALID_FILE_NAME");

        LprRecognition recognition = parsed.Recognition!;
        if (recognition.SiteId != _siteId)
            return Reject(recognition.EventId, "INVALID_SITE");

        SiteConfiguration? configuration =
            await _configurationStore.GetAsync(_siteId, cancellationToken);
        if (configuration is null)
            return Reject(recognition.EventId, "CONFIG_NOT_READY");
        if (!configuration.Site.Enabled || configuration.Site.SiteId != recognition.SiteId)
            return Reject(recognition.EventId, "INVALID_SITE");

        ParkingLane? lane = configuration.Lanes.FirstOrDefault(
            value => value.LaneId == recognition.LaneId);
        if (lane is null || !lane.Enabled ||
            lane.SiteId != recognition.SiteId ||
            lane.GroupNumber != recognition.Groupnum)
            return Reject(recognition.EventId, "INVALID_LANE");

        ParkingDevice? device = configuration.Devices.FirstOrDefault(
            value => value.DeviceId == recognition.DeviceId);
        if (device is null || !device.Enabled ||
            device.SiteId != recognition.SiteId ||
            device.LaneId != recognition.LaneId ||
            !string.Equals(device.DeviceType, "LPR", StringComparison.OrdinalIgnoreCase))
            return Reject(recognition.EventId, "INVALID_DEVICE");

        if (!string.Equals(lane.Direction, recognition.Direction, StringComparison.OrdinalIgnoreCase))
            return Reject(recognition.EventId, "DIRECTION_MISMATCH");

        try
        {
            FieldEventResponse response;
            if (recognition.Direction == ParkingEventType.Entry)
            {
                response = await _eventService.AcceptEntryAsync(
                    new FieldEventRequest(
                        recognition.EventId,
                        recognition.SiteId,
                        recognition.LaneId,
                        recognition.DeviceId,
                        recognition.CarNumber,
                        recognition.RecognizedAt,
                        recognition.Groupnum,
                        ParkingEventType.Entry,
                        recognition.ImageFileName),
                    cancellationToken);
            }
            else
            {
                response = await _eventService.AcceptExitAsync(
                    new ExitEventRequest(
                        recognition.EventId,
                        recognition.SiteId,
                        recognition.LaneId,
                        recognition.DeviceId,
                        recognition.CarNumber,
                        recognition.RecognizedAt,
                        recognition.Groupnum,
                        EventType: ParkingEventType.Exit,
                        OutImage: recognition.ImageFileName),
                    cancellationToken);
            }

            return new LprProcessResult(true, recognition.EventId, null, response);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "LPR 차로 처리 실패: EventId={EventId}, FileName={FileName}",
                recognition.EventId,
                fileName);
            return Reject(recognition.EventId, "PROCESSING_ERROR");
        }
    }

    private static LprProcessResult Reject(Guid? eventId, string errorCode) =>
        new(false, eventId, errorCode, null);
}
