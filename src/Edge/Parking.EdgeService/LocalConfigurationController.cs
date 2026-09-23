using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Parking.Contracts;

namespace Parking.EdgeService;

[ApiController]
[Route("api/v1/local")]
public sealed class LocalConfigurationController : ControllerBase
{
    private readonly LocalBootstrapStore _bootstrap;
    private readonly LocalConfigurationService _configuration;

    public LocalConfigurationController(
        LocalBootstrapStore bootstrap,
        LocalConfigurationService configuration)
    {
        _bootstrap = bootstrap;
        _configuration = configuration;
    }

    [HttpGet("setup")]
    public async Task<IActionResult> GetSetupAsync(CancellationToken token)
    {
        EdgeBootstrapSettings? value = await _bootstrap.GetAsync(token);
        return value is null ? NotFound() : Ok(ToResponse(value));
    }

    [HttpGet("central-connection")]
    public async Task<IActionResult> GetCentralConnectionAsync(CancellationToken token)
    {
        System.Net.IPAddress? remote = HttpContext.Connection.RemoteIpAddress;
        if (remote is null || !System.Net.IPAddress.IsLoopback(remote))
            return Forbid();
        EdgeBootstrapSettings? value = await _bootstrap.GetAsync(token);
        return value is null
            ? NotFound()
            : Ok(new CentralConnectionResponse(
                value.SiteId, value.CentralServerUrl, value.ParkingApiUrl, value.SiteAuthKey));
    }

    [HttpPut("setup")]
    public async Task<IActionResult> SaveSetupAsync(
        [FromBody] EdgeSetupRequest request,
        CancellationToken token)
    {
        try
        {
            EdgeBootstrapSettings value = new(
                request.SiteId,
                request.CentralServerUrl,
                request.ParkingApiUrl,
                request.ImageServerUrl,
                request.ImageWatchPath,
                request.SiteAuthKey,
                DateTimeOffset.UtcNow);
            await _bootstrap.SaveAsync(value, token);
            return Ok(ToResponse(value));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { Message = exception.Message });
        }
    }

    [HttpGet("config")]
    public async Task<IActionResult> GetConfigurationAsync(CancellationToken token)
    {
        try
        {
            SiteConfiguration? value = await _configuration.GetAsync(token);
            return value is null ? NotFound() : Ok(value);
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    [HttpPut("config/site")]
    public Task<IActionResult> SaveSiteAsync([FromBody] ParkingSite value, CancellationToken token) =>
        ExecuteAsync(() => _configuration.SaveSiteAsync(value, token));

    [HttpPut("config/lanes/{laneId:long}")]
    public Task<IActionResult> SaveLaneAsync(long laneId, [FromBody] ParkingLane value, CancellationToken token) =>
        laneId == value.LaneId ? ExecuteAsync(() => _configuration.SaveLaneAsync(value, token)) : Task.FromResult<IActionResult>(BadRequest());

    [HttpDelete("config/lanes/{laneId:long}")]
    public Task<IActionResult> DeleteLaneAsync(long laneId, CancellationToken token) =>
        ExecuteAsync(() => _configuration.DeleteLaneAsync(laneId, token));

    [HttpPut("config/devices/{deviceId:long}")]
    public Task<IActionResult> SaveDeviceAsync(long deviceId, [FromBody] ParkingDevice value, CancellationToken token) =>
        deviceId == value.DeviceId ? ExecuteAsync(() => _configuration.SaveDeviceAsync(value, token)) : Task.FromResult<IActionResult>(BadRequest());

    [HttpDelete("config/devices/{deviceId:long}")]
    public Task<IActionResult> DeleteDeviceAsync(long deviceId, CancellationToken token) =>
        ExecuteAsync(() => _configuration.DeleteDeviceAsync(deviceId, token));

    [HttpPut("config/device-links")]
    public Task<IActionResult> SaveDeviceLinkAsync([FromBody] ParkingDeviceLink value, CancellationToken token) =>
        ExecuteAsync(() => _configuration.SaveDeviceLinkAsync(value, token));

    [HttpDelete("config/device-links")]
    public Task<IActionResult> DeleteDeviceLinkAsync(
        [FromQuery] long sourceDeviceId,
        [FromQuery] long targetDeviceId,
        [FromQuery] string linkType,
        CancellationToken token) => ExecuteAsync(() => _configuration.DeleteDeviceLinkAsync(
            sourceDeviceId, targetDeviceId, linkType, token));

    [HttpPut("config/variables")]
    public Task<IActionResult> SaveVariableAsync([FromBody] ParkingOperationVariable value, CancellationToken token) =>
        ExecuteAsync(() => _configuration.SaveOperationVariableAsync(value, token));

    [HttpDelete("config/variables")]
    public Task<IActionResult> DeleteVariableAsync(
        [FromQuery] int groupnum,
        [FromQuery] string commandType,
        CancellationToken token) => ExecuteAsync(() => _configuration.DeleteOperationVariableAsync(
            groupnum, commandType, token));

    private async Task<IActionResult> ExecuteAsync(Func<Task> action)
    {
        try { await action(); return NoContent(); }
        catch (SqliteException exception) when (exception.SqliteErrorCode == 19)
        { return Conflict(new { Message = "중복되거나 연결할 수 없는 설정입니다." }); }
        catch (InvalidOperationException exception)
        { return BadRequest(new { Message = exception.Message }); }
        catch (ArgumentException exception)
        { return BadRequest(new { Message = exception.Message }); }
    }

    private static EdgeSetupResponse ToResponse(EdgeBootstrapSettings value) => new(
        value.SiteId,
        value.CentralServerUrl,
        value.ParkingApiUrl,
        value.ImageServerUrl,
        value.ImageWatchPath,
        value.UpdatedAtUtc);
}
