using Microsoft.AspNetCore.Mvc;
using Parking.Contracts;

namespace Parking.EdgeService;

[ApiController]
[Route("api/v1/management")]
public sealed class ManagementController : ControllerBase
{
    private readonly EdgeManagementService _service;

    public ManagementController(
        EdgeManagementService service)
    {
        _service = service;
    }

    [HttpGet("status")]
    public async Task<ActionResult<EdgeServiceStatus>> GetStatusAsync(
        CancellationToken cancellationToken) =>
        Ok(await _service.GetStatusAsync(cancellationToken));

    [HttpGet("entries")]
    public async Task<ActionResult<IReadOnlyList<EdgeEntryItem>>> GetEntriesAsync(
        [FromQuery] int limit = 1000,
        CancellationToken cancellationToken = default)
    {
        if (!IsValidLimit(limit))
            return BadRequest();
        return Ok(await _service.GetEntriesAsync(limit, cancellationToken));
    }

    [HttpGet("activities")]
    public async Task<ActionResult<IReadOnlyList<EdgeActivityItem>>> GetActivitiesAsync(
        [FromQuery] int limit = 1000,
        CancellationToken cancellationToken = default)
    {
        if (!IsValidLimit(limit))
            return BadRequest();
        return Ok(await _service.GetActivitiesAsync(limit, cancellationToken));
    }

    [HttpGet("configuration")]
    public async Task<ActionResult<SiteConfiguration>> GetConfigurationAsync(
        CancellationToken cancellationToken)
    {
        SiteConfiguration? configuration =
            await _service.GetConfigurationAsync(cancellationToken);
        return configuration is null ? NotFound() : Ok(configuration);
    }

    private static bool IsValidLimit(int limit) => limit is >= 1 and <= 1000;

}
