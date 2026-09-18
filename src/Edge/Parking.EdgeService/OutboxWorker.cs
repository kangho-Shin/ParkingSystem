using System.Text.Json;
using Parking.Contracts;

namespace Parking.EdgeService
{
    public sealed class OutboxWorker : BackgroundService
    {
        private readonly SqliteOutboxRepository _outbox;
        private readonly GatewayClient _gatewayClient;

        public OutboxWorker(SqliteOutboxRepository outbox, GatewayClient gatewayClient)
        {
            _outbox = outbox;
            _gatewayClient = gatewayClient;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                IReadOnlyList<OutboxMessage> messages = await _outbox.GetPendingAsync(20, stoppingToken);
                foreach (OutboxMessage message in messages)
                {
                    try
                    {
                        FieldEventRequest request = JsonSerializer.Deserialize<FieldEventRequest>(message.PayloadJson)
                            ?? throw new InvalidOperationException("Outbox 데이터를 읽지 못했습니다.");
                        FieldEventResponse response = await _gatewayClient.SendAsync(request, stoppingToken);
                        if (response.EventId != message.EventId) throw new InvalidOperationException("응답 EventId가 다릅니다.");
                        await _outbox.MarkCompletedAsync(message.EventId, stoppingToken);
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
                    catch
                    {
                        await _outbox.MarkFailedAsync(message.EventId, message.RetryCount, stoppingToken);
                        break;
                    }
                }
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }
    }
}
