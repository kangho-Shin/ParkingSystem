using Parking.Contracts;

namespace Parking.Central.Data;

public interface IPeriodMemberManagementRepository
{
    Task<IReadOnlyList<PeriodMemberDetail>> SearchAsync(
        long siteId,
        int? groupnum,
        string? carNumber,
        CancellationToken cancellationToken);

    Task<PeriodMemberDetail?> GetAsync(long memberId, CancellationToken cancellationToken);
    Task<long> CreateAsync(PeriodMemberSaveRequest request, CancellationToken cancellationToken);
    Task<bool> UpdateAsync(long memberId, PeriodMemberSaveRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(long memberId, CancellationToken cancellationToken);
}
