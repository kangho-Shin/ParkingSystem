using Microsoft.AspNetCore.Mvc;
using Parking.FeeEngine;

namespace Parking.Api.Features.Fees
{
    public sealed class CalculateParkingFeeRequest
    {
        public int Sitenum { get; set; }
        public int Groupnum { get; set; }
        public DateTime EntryAt { get; set; }
        public DateTime ExitAt { get; set; }
        public int CarType { get; set; }
        public List<int> DiscountKeys { get; set; } = new();
    }

    [ApiController]
    [Route("api/v1/fees")]
    public sealed class FeeCalculationController : ControllerBase
    {
        private readonly FeeCalculationService _service;
        public FeeCalculationController(FeeCalculationService service) { _service = service; }

        [HttpPost("calculate")]
        public async Task<IActionResult> CalculateAsync(
            [FromBody] CalculateParkingFeeRequest request,
            CancellationToken cancellationToken)
        {
            if (request.Sitenum <= 0 || request.Groupnum <= 0 ||
                request.EntryAt == default || request.ExitAt <= request.EntryAt ||
                request.CarType <= 0)
                return BadRequest();

            ParkingFeeResult result = await _service.CalculateAsync(request, cancellationToken);
            return Ok(result);
        }
    }
}
