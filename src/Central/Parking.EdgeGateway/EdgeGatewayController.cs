using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Parking.Contracts;

namespace Parking.EdgeGateway
{
    [ApiController]
    [Route("api/v1/edge")]
    public sealed class EdgeGatewayController : ControllerBase
    {
        private readonly FieldEventRelay _relay;
        private readonly ParkingApiClient _client;

        public EdgeGatewayController(FieldEventRelay relay, ParkingApiClient client)
        {
            _relay = relay;
            _client = client;
        }

        [HttpPost("events")]
        public Task<IActionResult> EntryAsync(
            [FromBody] FieldEventRequest request, CancellationToken cancellationToken) =>
            RelayAsync(() => _relay.RelayAsync(request, cancellationToken));

        [HttpPost("exits")]
        public Task<IActionResult> ExitAsync(
            [FromBody] ExitEventRequest request, CancellationToken cancellationToken) =>
            RelayAsync(() => _relay.RelayExitAsync(request, cancellationToken));

        [HttpGet("config/sites/{siteId:long}")]
        public async Task<IActionResult> GetConfigurationAsync(
            long siteId, CancellationToken cancellationToken)
        {
            try { return Ok(await _client.GetSiteConfigurationAsync(siteId, cancellationToken)); }
            catch (HttpRequestException) { return StatusCode(StatusCodes.Status503ServiceUnavailable); }
            catch (TaskCanceledException) { return StatusCode(StatusCodes.Status503ServiceUnavailable); }
        }

        [HttpPost("fees/quote")]
        public async Task<IActionResult> QuoteFeeAsync(
            [FromBody] JToken request,
            CancellationToken cancellationToken)
        {
            try
            {
                HttpRelayResponse response = await _client.RelayFeeQuoteAsync(
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

        [HttpGet("parking/search")]
        public Task<IActionResult> SearchParkingAsync(CancellationToken cancellationToken) =>
            RelayHttpAsync(() => _client.RelayParkingSearchAsync(
                Request.QueryString.Value ?? "",
                cancellationToken));

        [HttpPost("fees/quote/session")]
        public Task<IActionResult> QuoteSessionFeeAsync(
            [FromBody] JToken request,
            CancellationToken cancellationToken) =>
            RelayHttpAsync(() => _client.RelaySessionFeeQuoteAsync(
                request.ToString(Newtonsoft.Json.Formatting.None),
                cancellationToken));

        [HttpPost("payments/complete")]
        public Task<IActionResult> CompletePaymentAsync(
            [FromBody] JToken request,
            CancellationToken cancellationToken) =>
            RelayHttpAsync(() => _client.RelayPaymentCompleteAsync(
                request.ToString(Newtonsoft.Json.Formatting.None),
                cancellationToken));

        private async Task<IActionResult> RelayAsync(Func<Task<FieldEventResponse>> action)
        {
            try { return Ok(await action()); }
            catch (HttpRequestException) { return StatusCode(StatusCodes.Status503ServiceUnavailable); }
            catch (TaskCanceledException) { return StatusCode(StatusCodes.Status503ServiceUnavailable); }
        }

        private static async Task<IActionResult> RelayHttpAsync(
            Func<Task<HttpRelayResponse>> action)
        {
            try
            {
                HttpRelayResponse response = await action();
                return new ContentResult
                {
                    StatusCode = response.StatusCode,
                    ContentType = "application/json; charset=utf-8",
                    Content = response.Content
                };
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
    }
}
