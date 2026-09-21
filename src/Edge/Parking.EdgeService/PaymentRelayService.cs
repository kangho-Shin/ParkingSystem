using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Parking.Contracts;

namespace Parking.EdgeService;

public sealed class PaymentRelayService
{
    private readonly SqliteOutboxRepository _outbox;
    private readonly GatewayClient _gatewayClient;
    private readonly EdgeMonitoringRepository? _monitoring;

    public PaymentRelayService(
        SqliteOutboxRepository outbox,
        GatewayClient gatewayClient,
        EdgeMonitoringRepository? monitoring = null)
    {
        _outbox = outbox;
        _gatewayClient = gatewayClient;
        _monitoring = monitoring;
    }

    public async Task<HttpRelayResponse> CompleteAsync(
        Guid paymentId,
        string payloadJson,
        CancellationToken cancellationToken)
    {
        await _outbox.EnqueuePaymentAsync(paymentId, payloadJson, cancellationToken);
        CompletePaymentRequest? request = JsonConvert.DeserializeObject<CompletePaymentRequest>(payloadJson);
        if (_monitoring is not null && request is not null)
            await _monitoring.RecordPaymentAsync(
                request, EdgeDeliveryState.Pending, "PENDING", cancellationToken);

        try
        {
            HttpRelayResponse response = await _gatewayClient.RelayPaymentCompleteAsync(
                payloadJson,
                cancellationToken);

            if (response.StatusCode >= 500)
                return Pending();

            await _outbox.MarkCompletedAsync(paymentId, cancellationToken);
            if (_monitoring is not null && request is not null)
            {
                EdgeDeliveryState state = response.StatusCode < 400
                    ? EdgeDeliveryState.Completed
                    : EdgeDeliveryState.Failed;
                await _monitoring.RecordPaymentAsync(
                    request,
                    state,
                    ReadResultCode(response.Content),
                    cancellationToken);
            }
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

    internal static string? ReadResultCode(string json)
    {
        try
        {
            return JObject.Parse(json)["ResultCode"]?.ToString();
        }
        catch (JsonReaderException)
        {
            return null;
        }
    }
}
