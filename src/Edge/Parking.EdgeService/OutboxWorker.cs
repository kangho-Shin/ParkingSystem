using Newtonsoft.Json;
using Parking.Contracts;

namespace Parking.EdgeService
{
    public sealed class OutboxWorker : BackgroundService
    {
        private readonly SqliteOutboxRepository _outbox;
        private readonly GatewayClient _gatewayClient;
        private readonly EdgeMonitoringRepository? _monitoring;
        public OutboxWorker(
            SqliteOutboxRepository outbox,
            GatewayClient gatewayClient,
            EdgeMonitoringRepository? monitoring = null)
        {
            _outbox = outbox;
            _gatewayClient = gatewayClient;
            _monitoring = monitoring;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await ProcessOnceAsync(stoppingToken);
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }

        public async Task ProcessOnceAsync(CancellationToken cancellationToken)
        {
            IReadOnlyList<OutboxMessage> messages = await _outbox.GetPendingAsync(20, cancellationToken);
            foreach (OutboxMessage message in messages)
            {
                try
                {
                    if (message.EventType == "Payment")
                    {
                        CompletePaymentRequest request =
                            JsonConvert.DeserializeObject<CompletePaymentRequest>(message.PayloadJson)
                            ?? throw new InvalidOperationException("결제 Outbox 데이터를 읽지 못했습니다.");
                        HttpRelayResponse paymentResponse =
                            await _gatewayClient.RelayPaymentCompleteAsync(
                                message.PayloadJson,
                                cancellationToken);
                        if (paymentResponse.StatusCode >= 500)
                            throw new HttpRequestException("결제결과 중앙 전송에 실패했습니다.");
                        await _outbox.MarkCompletedAsync(message.EventId, cancellationToken);
                        if (_monitoring is not null)
                        {
                            EdgeDeliveryState state = paymentResponse.StatusCode < 400
                                ? EdgeDeliveryState.Completed
                                : EdgeDeliveryState.Failed;
                            await _monitoring.RecordPaymentAsync(
                                request,
                                state,
                                PaymentRelayService.ReadResultCode(paymentResponse.Content),
                                cancellationToken);
                        }
                        continue;
                    }

                    FieldEventResponse response;
                    if (message.EventType == "Exit")
                    {
                        ExitEventRequest request =
                            JsonConvert.DeserializeObject<ExitEventRequest>(message.PayloadJson)
                            ?? throw new InvalidOperationException("출차 Outbox 데이터를 읽지 못했습니다.");
                        response = await _gatewayClient.SendExitAsync(request, cancellationToken);
                        if (response.EventId != message.EventId)
                            throw new InvalidOperationException("응답 EventId가 다릅니다.");
                        if (_monitoring is not null)
                            await _monitoring.RecordExitAsync(
                                request, response, EdgeDeliveryState.Completed, cancellationToken);
                    }
                    else
                    {
                        FieldEventRequest request =
                            JsonConvert.DeserializeObject<FieldEventRequest>(message.PayloadJson)
                            ?? throw new InvalidOperationException("입차 Outbox 데이터를 읽지 못했습니다.");
                        response = await _gatewayClient.SendEntryAsync(request, cancellationToken);
                        if (response.EventId != message.EventId)
                            throw new InvalidOperationException("응답 EventId가 다릅니다.");
                        if (_monitoring is not null)
                            await _monitoring.CompleteEntryDeliveryAsync(
                                request.EventId, response, EdgeDeliveryState.Completed, cancellationToken);
                    }

                    await _outbox.MarkCompletedAsync(message.EventId, cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
                catch
                {
                    await _outbox.MarkFailedAsync(message.EventId, message.RetryCount, cancellationToken);
                    break;
                }
            }
        }
    }
}
