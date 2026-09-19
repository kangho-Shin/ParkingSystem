using Parking.Contracts;

namespace Parking.EdgeService;

public sealed class PaymentRelayService
{
    private readonly SqliteOutboxRepository _outbox;
    private readonly GatewayClient _gatewayClient;

    public PaymentRelayService(
        SqliteOutboxRepository outbox,
        GatewayClient gatewayClient)
    {
        _outbox = outbox;
        _gatewayClient = gatewayClient;
    }

    public async Task<HttpRelayResponse> CompleteAsync(
        Guid paymentId,
        string payloadJson,
        CancellationToken cancellationToken)
    {
        await _outbox.EnqueuePaymentAsync(paymentId, payloadJson, cancellationToken);

        try
        {
            HttpRelayResponse response = await _gatewayClient.RelayPaymentCompleteAsync(
                payloadJson,
                cancellationToken);

            if (response.StatusCode >= 500)
                return Pending();

            await _outbox.MarkCompletedAsync(paymentId, cancellationToken);
            return response;
        }
        catch (Exception exception) when (
            exception is HttpRequestException ||
            exception is TaskCanceledException)
        {
            return Pending();
        }
    }

    private static HttpRelayResponse Pending() => new(
        StatusCodes.Status202Accepted,
        "{\"Accepted\":true,\"ResultCode\":\"PAYMENT_PENDING_SYNC\",\"Message\":\"결제결과 중앙 전송 대기 중입니다.\"}");
}
