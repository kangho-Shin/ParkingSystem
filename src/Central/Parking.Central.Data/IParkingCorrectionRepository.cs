using Parking.Contracts;

namespace Parking.Central.Data;

public interface IParkingCorrectionRepository
{
    Task<CorrectCarNumberResponse> CorrectCarNumberAsync(long parkingSessionId, string carNumber, CancellationToken cancellationToken);
}
