using Microsoft.AspNetCore.Mvc;
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

        public EdgeController(
            EdgeEventService service,
            LocalConfigurationStore configurationStore,
            IConfiguration configuration)
        {
            _service = service;
            _configurationStore = configurationStore;
            _configuration = configuration;
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
    }
}
