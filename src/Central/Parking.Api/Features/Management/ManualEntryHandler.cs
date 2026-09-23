using Parking.Central.Data;
using Parking.Contracts;

namespace Parking.Api.Features.Management;

public sealed class ManualEntryHandler
{
    private readonly IParkingEventRepository _repository;
    private readonly IPeriodVehicleRepository _periodRepository;

    public ManualEntryHandler(
        IParkingEventRepository repository,
        IPeriodVehicleRepository periodRepository)
    {
        _repository = repository;
        _periodRepository = periodRepository;
    }

    public async Task<ManualEntryResponse> HandleAsync(
        ManualEntryRequest request,
        CancellationToken cancellationToken)
    {
        FieldEventRequest entry = new(
            Guid.NewGuid(),
            request.SiteId,
            request.LaneId,
            request.DeviceId,
            request.CarNumber.Trim(),
            request.InDateTime,
            request.Groupnum,
            ParkingEventType.Entry,
            null,
            request.CarType,
            true);
        PeriodMember? member = await _periodRepository.FindMemberAsync(
            request.SiteId,
            request.Groupnum,
            request.CarNumber,
            request.InDateTime,
            cancellationToken);
        ParkingSessionType type = member is null
            ? ParkingSessionType.General
            : ParkingSessionType.Period;
        FieldEventResponse result = member is null
            ? await _repository.SaveEntryAsync(entry, cancellationToken)
            : await _periodRepository.SaveEntryAsync(entry, member, cancellationToken);
        return new ManualEntryResponse(
            type,
            result.ParkingSessionId,
            result.Accepted,
            result.ResultCode,
            result.DisplayMessage);
    }
}
