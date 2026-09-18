using Parking.Contracts;

namespace Parking.Central.Data;

public interface IParkingEventRepository
{
    Task<FieldEventResponse> SaveEntryAsync(FieldEventRequest request, CancellationToken cancellationToken);
}