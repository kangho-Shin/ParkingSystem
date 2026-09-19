using Microsoft.AspNetCore.Mvc;
using Parking.Central.Data;
using Parking.Contracts;
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

    public sealed class QuoteParkingFeeRequest
    {
        public int Sitenum { get; set; }
        public int Groupnum { get; set; }
        public string CarNumber { get; set; } = "";
        public DateTimeOffset ExitAt { get; set; }
        public int CarType { get; set; }
        public List<int> DiscountKeys { get; set; } = new();
    }

    public sealed record QuoteParkingFeeResponse(
        long ParkingSessionId,
        string CarNumber,
        DateTimeOffset EntryAt,
        DateTimeOffset ExitAt,
        ParkingFeeResult Fee);

    [ApiController]
    [Route("api/v1/fees")]
    public sealed class FeeCalculationController : ControllerBase
    {
        private readonly FeeCalculationService _service;
        private readonly IParkingExitRepository _parkingRepository;

        public FeeCalculationController(
            FeeCalculationService service,
            IParkingExitRepository parkingRepository)
        {
            _service = service;
            _parkingRepository = parkingRepository;
        }

        [HttpPost("calculate")]
        public async Task<IActionResult> CalculateAsync(
            [FromBody] CalculateParkingFeeRequest request,
            CancellationToken cancellationToken)
        {
            if (!IsValid(request))
                return BadRequest();

            ParkingFeeResult result = await _service.CalculateAsync(request, cancellationToken);
            return Ok(result);
        }

        [HttpPost("quote")]
        public async Task<IActionResult> QuoteAsync(
            [FromBody] QuoteParkingFeeRequest request,
            CancellationToken cancellationToken)
        {
            if (request.Sitenum <= 0 || request.Groupnum <= 0 ||
                string.IsNullOrWhiteSpace(request.CarNumber) ||
                request.ExitAt == default || request.CarType <= 0)
                return BadRequest();

            OpenParkingSessionResponse? session = await _parkingRepository.FindOpenAsync(
                request.Sitenum,
                request.CarNumber.Trim(),
                cancellationToken);

            if (session is null)
                return NotFound();

            var calculationRequest = new CalculateParkingFeeRequest
            {
                Sitenum = request.Sitenum,
                Groupnum = request.Groupnum,
                EntryAt = session.EntryAt.ToOffset(request.ExitAt.Offset).DateTime,
                ExitAt = request.ExitAt.DateTime,
                CarType = request.CarType,
                DiscountKeys = request.DiscountKeys
            };

            if (!IsValid(calculationRequest))
                return BadRequest();

            ParkingFeeResult fee = await _service.CalculateAsync(calculationRequest, cancellationToken);
            return Ok(new QuoteParkingFeeResponse(
                session.ParkingSessionId,
                session.CarNumber,
                session.EntryAt,
                request.ExitAt,
                fee));
        }

        private static bool IsValid(CalculateParkingFeeRequest request) =>
            request.Sitenum > 0 &&
            request.Groupnum > 0 &&
            request.EntryAt != default &&
            request.ExitAt > request.EntryAt &&
            request.CarType > 0;
    }
}
