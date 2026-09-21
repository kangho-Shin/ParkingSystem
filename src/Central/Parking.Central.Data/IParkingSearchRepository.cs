using Parking.Contracts;

namespace Parking.Central.Data;

public interface IParkingSearchRepository
{
    Task<IReadOnlyList<ParkingSearchCandidate>> SearchAsync(
        long siteId,
        int groupnum,
        string carNumber,
        CancellationToken cancellationToken);
}
