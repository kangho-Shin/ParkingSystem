using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Parking.Contracts;

namespace Parking.EdgeService
{
    [ApiController]
    [Route("api/v1")]
    public sealed class EdgeController : ControllerBase
    {
        private readonly EdgeEventService _service;
        private readonly LocalConfigurationStore _configurationStore;
        private readonly IConfiguration _configuration;
        private readonly GatewayClient _gatewayClient;
        private readonly PaymentRelayService _paymentRelayService;

        public EdgeController(
            EdgeEventService service,
            LocalConfigurationStore configurationStore,
            IConfiguration configuration,
            GatewayClient gatewayClient,
            PaymentRelayService paymentRelayService)
        {
            _service = service;
            _configurationStore = configurationStore;
            _configuration = configuration;
            _gatewayClient = gatewayClient;
            _paymentRelayService = paymentRelayService;
        }

        [HttpGet("local/config")]
        public async Task<IActionResult> GetConfigurationAsync(CancellationToken cancellationToken)
        {
            long siteId = _configuration.GetValue<long>("Edge:SiteId");
            SiteConfiguration? result = await _configurationStore.GetAsync(siteId, cancellationToken);
            return result is null ? NotFound() : Ok(result);
        }

        [HttpPost("edge/events")]
        public async Task<IActionResult> EntryAsync(
            [FromBody] FieldEventRequest request, CancellationToken cancellationToken) =>
            Ok(await _service.AcceptEntryAsync(request, cancellationToken));

        [HttpPost("edge/exits")]
        public async Task<IActionResult> ExitAsync(
            [FromBody] ExitEventRequest request, CancellationToken cancellationToken) =>
            Ok(await _service.AcceptExitAsync(request, cancellationToken));

        [HttpPost("local/fees/quote")]
        public async Task<IActionResult> QuoteFeeAsync(
            [FromBody] JToken request,
            CancellationToken cancellationToken)
        {
            try
            {
                HttpRelayResponse response = await _gatewayClient.RelayFeeQuoteAsync(
                    request.ToString(Newtonsoft.Json.Formatting.None),
                    cancellationToken);
                return new ContentResult
                {
                    StatusCode = response.StatusCode,
                    ContentType = "application/json; charset=utf-8",
                    Content = response.Content
                };
            }
            catch (HttpRequestException)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
            catch (TaskCanceledException)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
        }

        [HttpPost("local/payments/complete")]
        public async Task<IActionResult> CompletePaymentAsync(
            [FromBody] JToken request,
            CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(request["PaymentId"]?.ToString(), out Guid paymentId) ||
                paymentId == Guid.Empty)
                return BadRequest();

            HttpRelayResponse response = await _paymentRelayService.CompleteAsync(
                paymentId,
                request.ToString(Newtonsoft.Json.Formatting.None),
                cancellationToken);
            return ToContentResult(response);
        }

        private static async Task<IActionResult> RelayAsync(
            Func<Task<HttpRelayResponse>> action)
        {
            try
            {
                HttpRelayResponse response = await action();
                return ToContentResult(response);
            }
            catch (HttpRequestException)
            {
                return new StatusCodeResult(StatusCodes.Status503ServiceUnavailable);
            }
            catch (TaskCanceledException)
            {
                return new StatusCodeResult(StatusCodes.Status503ServiceUnavailable);
            }
        }

        private static ContentResult ToContentResult(HttpRelayResponse response) => new()
        {
            StatusCode = response.StatusCode,
            ContentType = "application/json; charset=utf-8",
            Content = response.Content
        };
    }
}
