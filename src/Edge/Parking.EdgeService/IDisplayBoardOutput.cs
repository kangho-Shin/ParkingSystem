using Parking.Contracts;

namespace Parking.EdgeService;

public interface IDisplayBoardOutput
{
    Task SendClockAsync(
        long siteId,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task SendAsync(
        LprRecognition recognition,
        FieldEventResponse response,
        CancellationToken cancellationToken);

    Task SendFromDeviceAsync(
        long sourceDeviceId,
        LprRecognition recognition,
        FieldEventResponse response,
        CancellationToken cancellationToken) =>
        SendAsync(recognition, response, cancellationToken);
}
