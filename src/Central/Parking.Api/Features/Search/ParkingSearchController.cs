using Microsoft.AspNetCore.Mvc;
using Parking.Central.Data;
using Parking.Contracts;

namespace Parking.Api.Features.Search;

[ApiController]
[Route("api/v1/parking/search")]
public sealed class ParkingSearchController : ControllerBase
{
    private readonly IParkingSearchRepository _repository;

    public ParkingSearchController(IParkingSearchRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<IActionResult> SearchAsync(
        [FromQuery] long siteId,
        [FromQuery] int groupnum,
        [FromQuery] string carNumber,
        [FromQuery] DateTimeOffset exitAt,
        CancellationToken cancellationToken)
    {
        if (siteId <= 0 || groupnum <= 0 || string.IsNullOrWhiteSpace(carNumber) ||
            exitAt == default)
            return BadRequest();

        IReadOnlyList<ParkingSearchCandidate> candidates = await _repository.SearchAsync(
            siteId,
            groupnum,
            carNumber,
            cancellationToken);

        if (candidates.Count == 0)
            return NotFound();

        return Ok(new ParkingSearchResponse(candidates));
    }
}
