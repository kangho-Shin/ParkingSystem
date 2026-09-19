using Microsoft.AspNetCore.Mvc;
using Parking.Central.Data;
using Parking.Api.Features.Fees;
using Parking.Contracts;

namespace Parking.Api.Features.Search;

[ApiController]
[Route("api/v1/parking/search")]
public sealed class ParkingSearchController : ControllerBase
{
    private readonly IParkingSearchRepository _repository;
    private readonly ParkingQuoteService _quoteService;

    public ParkingSearchController(
        IParkingSearchRepository repository,
        ParkingQuoteService quoteService)
    {
        _repository = repository;
        _quoteService = quoteService;
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

        if (candidates.Count == 1)
        {
            try
            {
                QuoteParkingFeeResponse? quote = await _quoteService.QuoteBySessionIdAsync(
                    candidates[0].ParkingSessionId,
                    exitAt,
                    cancellationToken);
                return quote is null ? NotFound() : Ok(quote);
            }
            catch (ArgumentException)
            {
                return BadRequest();
            }
        }

        return Ok(new ParkingSearchResponse(candidates));
    }
}
