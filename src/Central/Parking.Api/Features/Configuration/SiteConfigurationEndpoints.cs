using Microsoft.AspNetCore.Mvc;
using Parking.Central.Data;
using Parking.Contracts;

namespace Parking.Api.Features.Configuration
{
    [ApiController]
    [Route("api/v1/config")]
    public sealed class SiteConfigurationController : ControllerBase
    {
        private readonly ISiteConfigurationRepository _repository;
        public SiteConfigurationController(ISiteConfigurationRepository repository) { _repository = repository; }

        [HttpGet("sites/{siteId:long}")]
        public async Task<IActionResult> GetAsync(long siteId, CancellationToken cancellationToken)
        {
            SiteConfiguration? configuration = await _repository.GetAsync(siteId, cancellationToken);
            return configuration is null ? NotFound() : Ok(configuration);
        }

        [HttpPut("sites/{siteId:long}")]
        public async Task<IActionResult> SaveSiteAsync(
            long siteId, [FromBody] ParkingSite site, CancellationToken cancellationToken)
        {
            if (siteId != site.SiteId || siteId <= 0) return BadRequest();
            await _repository.SaveSiteAsync(site, cancellationToken);
            return NoContent();
        }

        [HttpPut("lanes/{laneId:long}")]
        public async Task<IActionResult> SaveLaneAsync(
            long laneId, [FromBody] ParkingLane lane, CancellationToken cancellationToken)
        {
            if (laneId != lane.LaneId || laneId <= 0 || lane.SiteId <= 0 || lane.GroupNumber <= 0)
                return BadRequest();
            await _repository.SaveLaneAsync(lane, cancellationToken);
            return NoContent();
        }

        [HttpPut("devices/{deviceId:long}")]
        public async Task<IActionResult> SaveDeviceAsync(
            long deviceId, [FromBody] ParkingDevice device, CancellationToken cancellationToken)
        {
            if (deviceId != device.DeviceId || deviceId <= 0 ||
                device.SiteId <= 0 || device.DeviceNumber <= 0 ||
                (device.Port is <= 0 or > 65535))
                return BadRequest();
            await _repository.SaveDeviceAsync(device, cancellationToken);
            return NoContent();
        }
    }
}
