using Parking.Contracts;

namespace Parking.EdgeService
{
    public sealed class EdgeEventService
    {
        private readonly SqliteOutboxRepository _outbox;
        private readonly GatewayClient _gatewayClient;

        public EdgeEventService(SqliteOutboxRepository outbox, GatewayClient gatewayClient)
        {
            _outbox = outbox;
            _gatewayClient = gatewayClient;
        }

        public async Task<FieldEventResponse> AcceptEntryAsync(
            FieldEventRequest request,
            CancellationToken cancellationToken)
        {
            await _outbox.EnqueueEntryAsync(request, cancellationToken);

            try
            {
                FieldEventResponse response =
                    await _gatewayClient.SendEntryAsync(request, cancellationToken);
                await _outbox.MarkCompletedAsync(request.EventId, cancellationToken);
                return response;
            }
            catch (Exception exception) when (
                exception is HttpRequestException ||
                exception is TaskCanceledException)
            {
                return new FieldEventResponse(
                    request.EventId,
                    true,
                    null,
                    "EDGE_OFFLINE_ENTRY",
                    "중앙 연결 대기 중입니다.",
                    true);
            }
        }

        public async Task<FieldEventResponse> AcceptExitAsync(
            ExitEventRequest request,
            CancellationToken cancellationToken)
        {
            await _outbox.EnqueueExitAsync(request, cancellationToken);

            try
            {
                FieldEventResponse response =
                    await _gatewayClient.SendExitAsync(request, cancellationToken);
                await _outbox.MarkCompletedAsync(request.EventId, cancellationToken);
                return response;
            }
            catch (Exception exception) when (
                exception is HttpRequestException ||
                exception is TaskCanceledException)
            {
                return new FieldEventResponse(
                    request.EventId,
                    false,
                    null,
                    "CENTRAL_OFFLINE_EXIT_BLOCKED",
                    "중앙 연결 장애로 출차할 수 없습니다.",
                    false);
            }
        }
    }
}
