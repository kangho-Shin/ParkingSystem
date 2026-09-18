using Microsoft.AspNetCore.Mvc;
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

        private async Task<IActionResult> RelayAsync(Func<Task<FieldEventResponse>> action)
        {
            try { return Ok(await action()); }
            catch (HttpRequestException) { return StatusCode(StatusCodes.Status503ServiceUnavailable); }
            catch (TaskCanceledException) { return StatusCode(StatusCodes.Status503ServiceUnavailable); }
        }
    }
}
