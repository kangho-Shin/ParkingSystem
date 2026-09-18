using Parking.Central.Data;
using Parking.Contracts;

namespace Parking.Api.Features.Entries
{
    public sealed class CreateEntryHandler
    {
        private readonly IParkingEventRepository _repository;

        public CreateEntryHandler(IParkingEventRepository repository)
        {
            _repository = repository;
        }

        public Task<FieldEventResponse> HandleAsync(
            FieldEventRequest request,
            CancellationToken cancellationToken)
        {
            return _repository.SaveEntryAsync(request, cancellationToken);
        }
    }
}