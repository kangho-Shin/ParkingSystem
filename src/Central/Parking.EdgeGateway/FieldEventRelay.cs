using Parking.Contracts;

namespace Parking.EdgeGateway
{
    public sealed class FieldEventRelay
    {
        private readonly ParkingApiClient _apiClient;
        public FieldEventRelay(ParkingApiClient apiClient) { _apiClient = apiClient; }

        public Task<FieldEventResponse> RelayAsync(
            FieldEventRequest request, CancellationToken cancellationToken) =>
            _apiClient.SendAsync(request, cancellationToken);

        public Task<FieldEventResponse> RelayExitAsync(
            ExitEventRequest request, CancellationToken cancellationToken) =>
            _apiClient.SendExitAsync(request, cancellationToken);
    }
}
