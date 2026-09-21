using Parking.Contracts;

namespace Parking.EdgeService;

public interface IDisplayBoardOutput
{
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
