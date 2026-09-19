using Parking.Contracts;

namespace Parking.EdgeService;

public interface IDisplayBoardOutput
{
    Task SendAsync(
        LprRecognition recognition,
        FieldEventResponse response,
        CancellationToken cancellationToken);
}
