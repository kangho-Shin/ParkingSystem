using Parking.Contracts;

namespace Parking.EdgeService
{
    public sealed class EdgeEventService
    {
        private readonly SqliteOutboxRepository _outbox;
        private readonly GatewayClient _gatewayClient;
        private readonly EdgeMonitoringRepository? _monitoring;

        public EdgeEventService(
            SqliteOutboxRepository outbox,
            GatewayClient gatewayClient,
            EdgeMonitoringRepository? monitoring = null)
        {
            _outbox = outbox;
            _gatewayClient = gatewayClient;
            _monitoring = monitoring;
        }

        public async Task<FieldEventResponse> AcceptEntryAsync(
            FieldEventRequest request,
            CancellationToken cancellationToken)
        {
            await _outbox.EnqueueEntryAsync(request, cancellationToken);
            if (_monitoring is not null)
                await _monitoring.RecordEntryAsync(request, cancellationToken);

            try
            {
                FieldEventResponse response =
                    await _gatewayClient.SendEntryAsync(request, cancellationToken);
                await _outbox.MarkCompletedAsync(request.EventId, cancellationToken);
                if (_monitoring is not null)
                    await _monitoring.CompleteEntryDeliveryAsync(
                        request.EventId,
                        response,
                        EdgeDeliveryState.Completed,
                        cancellationToken);
                return response;
            }
            catch (Exception exception) when (
                exception is HttpRequestException ||
                exception is TaskCanceledException)
            {
                FieldEventResponse response = new(
                    request.EventId,
                    true,
                    null,
                    "EDGE_OFFLINE_ENTRY",
                    "중앙 연결 대기 중입니다.",
                    true);
                if (_monitoring is not null)
                    await _monitoring.CompleteEntryDeliveryAsync(
                        request.EventId,
                        response,
                        EdgeDeliveryState.Pending,
                        cancellationToken);
                return response;
            }
        }

        public async Task<FieldEventResponse> AcceptExitAsync(
            ExitEventRequest request,
            CancellationToken cancellationToken)
        {
            await _outbox.EnqueueExitAsync(request, cancellationToken);
            if (_monitoring is not null)
            {
                await _monitoring.RecordExitAsync(
                    request,
                    new FieldEventResponse(
                        request.EventId, false, null, "PENDING", "중앙 전송 대기 중입니다.", false),
                    EdgeDeliveryState.Pending,
                    cancellationToken);
            }

            try
            {
                FieldEventResponse response =
                    await _gatewayClient.SendExitAsync(request, cancellationToken);
                await _outbox.MarkCompletedAsync(request.EventId, cancellationToken);
                if (_monitoring is not null)
                    await _monitoring.RecordExitAsync(
                        request, response, EdgeDeliveryState.Completed, cancellationToken);
                return response;
            }
            catch (Exception exception) when (
                exception is HttpRequestException ||
                exception is TaskCanceledException)
            {
                FieldEventResponse response = new(
                    request.EventId,
                    false,
                    null,
                    "CENTRAL_OFFLINE_EXIT_BLOCKED",
                    "중앙 연결 장애로 출차할 수 없습니다.",
                    false);
                if (_monitoring is not null)
                    await _monitoring.RecordExitAsync(
                        request, response, EdgeDeliveryState.Pending, cancellationToken);
                return response;
            }
        }
    }
}
