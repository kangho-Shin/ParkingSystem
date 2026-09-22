using Parking.Contracts;

namespace Parking.EdgeService;

public interface IDisplayBoardOutput
{
    Task SendClockAsync(
        long siteId,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task SendClockFromDeviceAsync(
        long sourceDeviceId,
        long siteId,
        DateTimeOffset now,
        CancellationToken cancellationToken) =>
        SendClockAsync(siteId, now, cancellationToken);

    Task SendAsync(
        LprRecognition recognition,
        FieldEventResponse response,
        CancellationToken cancellationToken);

    Task SendFromDeviceAsync(
        long sourceDeviceId,
        LprRecognition recognition,
        FieldEventResponse response,
        CancellationToken cancellationToken,
        int displaySeconds = 11) =>
        SendAsync(recognition, response, cancellationToken);
}
