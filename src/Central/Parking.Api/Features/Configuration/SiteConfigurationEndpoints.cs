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

        [HttpGet("sites/{siteId:long}/versioned")]
        public async Task<IActionResult> GetVersionedAsync(
            long siteId,
            CancellationToken cancellationToken)
        {
            if (!await IsAuthorizedAsync(siteId, cancellationToken)) return Unauthorized();
            VersionedSiteConfiguration? value =
                await _repository.GetVersionedAsync(siteId, cancellationToken);
            return value is null ? NotFound() : Ok(value);
        }

        [HttpPut("sites/{siteId:long}/versioned")]
        public async Task<IActionResult> SaveVersionedAsync(
            long siteId,
            [FromBody] VersionedSiteConfiguration value,
            CancellationToken cancellationToken)
        {
            if (siteId <= 0 || siteId != value.Configuration.Site.SiteId) return BadRequest();
            if (!await IsAuthorizedAsync(siteId, cancellationToken)) return Unauthorized();
            bool applied = await _repository.SaveVersionedAsync(value, cancellationToken);
            if (applied) return NoContent();
            return Conflict(await _repository.GetVersionedAsync(siteId, cancellationToken));
        }

        [HttpPut("sites/{siteId:long}")]
        public async Task<IActionResult> SaveSiteAsync(
            long siteId, [FromBody] ParkingSite site, CancellationToken cancellationToken)
        {
            if (siteId != site.SiteId || siteId <= 0) return BadRequest();
            await _repository.SaveSiteAsync(site, cancellationToken);
            await _repository.TouchVersionAsync(siteId, cancellationToken);
            return NoContent();
        }

        [HttpPut("lanes/{laneId:long}")]
        public async Task<IActionResult> SaveLaneAsync(
            long laneId, [FromBody] ParkingLane lane, CancellationToken cancellationToken)
        {
            if (laneId != lane.LaneId || laneId <= 0 || lane.SiteId <= 0 || lane.GroupNumber <= 0)
                return BadRequest();
            await _repository.SaveLaneAsync(lane, cancellationToken);
            await _repository.TouchVersionAsync(lane.SiteId, cancellationToken);
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
            await _repository.TouchVersionAsync(device.SiteId, cancellationToken);
            return NoContent();
        }

        [HttpPut("device-links/{sourceDeviceId:long}/{targetDeviceId:long}")]
        public async Task<IActionResult> SaveDeviceLinkAsync(
            long sourceDeviceId,
            long targetDeviceId,
            [FromBody] ParkingDeviceLink link,
            CancellationToken cancellationToken)
        {
            if (sourceDeviceId != link.SourceDeviceId ||
                targetDeviceId != link.TargetDeviceId ||
                link.SiteId <= 0 || sourceDeviceId <= 0 || targetDeviceId <= 0 ||
                sourceDeviceId == targetDeviceId ||
                (link.LinkType != "KIOSK" && link.LinkType != "LDM"))
                return BadRequest();
            SiteConfiguration? configuration =
                await _repository.GetAsync(link.SiteId, cancellationToken);
            ParkingDevice? source = configuration?.Devices.FirstOrDefault(
                device => device.DeviceId == sourceDeviceId);
            ParkingDevice? target = configuration?.Devices.FirstOrDefault(
                device => device.DeviceId == targetDeviceId);
            if (source is null || target is null ||
                source.SiteId != link.SiteId || target.SiteId != link.SiteId ||
                !string.Equals(target.DeviceType, link.LinkType, StringComparison.OrdinalIgnoreCase))
                return BadRequest();
            await _repository.SaveDeviceLinkAsync(link, cancellationToken);
            await _repository.TouchVersionAsync(link.SiteId, cancellationToken);
            return NoContent();
        }

        private Task<bool> IsAuthorizedAsync(long siteId, CancellationToken cancellationToken)
        {
            string siteKey = Request.Headers["X-Site-Key"].ToString();
            return _repository.ValidateSiteKeyAsync(siteId, siteKey, cancellationToken);
        }
    }
}
