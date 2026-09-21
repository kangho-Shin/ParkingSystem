using Parking.Central.Data;
using Parking.Contracts;

namespace Parking.Api.Features.Entries
{
    public sealed class CreateEntryHandler
    {
        private readonly IParkingEventRepository _repository;
        private readonly IPeriodVehicleRepository _periodRepository;

        public CreateEntryHandler(
            IParkingEventRepository repository,
            IPeriodVehicleRepository periodRepository)
        {
            _repository = repository;
            _periodRepository = periodRepository;
        }

        public async Task<FieldEventResponse> HandleAsync(
            FieldEventRequest request,
            CancellationToken cancellationToken)
        {
            PeriodMember? member = await _periodRepository.FindMemberAsync(
                request.SiteId,
                request.Groupnum,
                request.CarNumber,
                request.InDateTime,
                cancellationToken);
            return member is null
                ? await _repository.SaveEntryAsync(request, cancellationToken)
                : await _periodRepository.SaveEntryAsync(request, member, cancellationToken);
        }
    }
}
