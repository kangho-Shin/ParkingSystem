using Microsoft.AspNetCore.Mvc;
using Parking.Central.Data;
using Parking.Contracts;

namespace Parking.Api.Features.Period;

[ApiController]
[Route("api/v1/period")]
public sealed class PeriodVehicleController : ControllerBase
{
    private readonly IPeriodVehicleRepository _repository;

    public PeriodVehicleController(IPeriodVehicleRepository repository)
    {
        _repository = repository;
    }

    [HttpGet("members/search")]
    public async Task<IActionResult> FindMemberAsync(
        [FromQuery] long siteId,
        [FromQuery] int groupnum,
        [FromQuery] string carNumber,
        [FromQuery] DateTimeOffset? at,
        CancellationToken cancellationToken)
    {
        if (siteId <= 0 || groupnum <= 0 || string.IsNullOrWhiteSpace(carNumber))
            return BadRequest();

        PeriodMember? result = await _repository.FindMemberAsync(
            siteId,
            groupnum,
            carNumber,
            at ?? DateTimeOffset.Now,
            cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("open")]
    public async Task<IActionResult> FindOpenAsync(
        [FromQuery] long siteId,
        [FromQuery] int groupnum,
        [FromQuery] string carNumber,
        CancellationToken cancellationToken)
    {
        if (siteId <= 0 || groupnum <= 0 || string.IsNullOrWhiteSpace(carNumber))
            return BadRequest();

        OpenPeriodSession? result = await _repository.FindOpenAsync(
            siteId,
            groupnum,
            carNumber,
            cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}
