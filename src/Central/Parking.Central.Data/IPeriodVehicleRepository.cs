using Parking.Contracts;

namespace Parking.Central.Data;

public interface IPeriodVehicleRepository
{
    Task<PeriodMember?> FindMemberAsync(
        long siteId,
        int groupnum,
        string carNumber,
        DateTimeOffset at,
        CancellationToken cancellationToken);

    Task<FieldEventResponse> SaveEntryAsync(
        FieldEventRequest request,
        PeriodMember member,
        CancellationToken cancellationToken);

    Task<OpenPeriodSession?> FindOpenAsync(
        long siteId,
        int groupnum,
        string carNumber,
        CancellationToken cancellationToken);

    Task<FieldEventResponse> SaveExitAsync(
        ExitEventRequest request,
        OpenPeriodSession session,
        CancellationToken cancellationToken);
}
