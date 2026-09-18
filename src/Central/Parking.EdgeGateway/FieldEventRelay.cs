using Parking.Contracts;

namespace Parking.EdgeGateway
{
    public sealed class FieldEventRelay
    {
        private readonly ParkingApiClient _apiClient;

        public FieldEventRelay(ParkingApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public Task<FieldEventResponse> RelayAsync(
            FieldEventRequest request,
            CancellationToken cancellationToken)
        {
            return _apiClient.SendAsync(request, cancellationToken);
        }
    }
}
