using Microsoft.AspNetCore.Mvc;
using Parking.Central.Data;
using Parking.Contracts;

namespace Parking.Api.Features.Exits
{
    [ApiController]
    [Route("api/v1/parking")]
    public sealed class ExitController : ControllerBase
    {
        private readonly IParkingExitRepository _repository;
        public ExitController(IParkingExitRepository repository) { _repository = repository; }

        [HttpGet("open")]
        public async Task<IActionResult> FindOpenAsync(
            [FromQuery] long siteId,
            [FromQuery] string carNumber,
            CancellationToken cancellationToken)
        {
            OpenParkingSessionResponse? session =
                await _repository.FindOpenAsync(siteId, carNumber, cancellationToken);
            return session is null ? NotFound() : Ok(session);
        }

        [HttpPost("exits")]
        public async Task<IActionResult> ExitAsync(
            [FromBody] ExitEventRequest request,
            CancellationToken cancellationToken)
        {
            if (request.EventId == Guid.Empty || request.SiteId <= 0 ||
                request.LaneId <= 0 || request.DeviceId <= 0)
                return BadRequest();

            return Ok(await _repository.SaveExitAsync(request, cancellationToken));
        }
    }
}
