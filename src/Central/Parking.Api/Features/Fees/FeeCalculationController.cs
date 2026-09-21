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
        public string CarNumber { get; set; } = "";
        public DateTimeOffset ExitAt { get; set; }
    }

    public sealed class QuoteParkingSessionRequest
    {
        public long ParkingSessionId { get; set; }
        public DateTimeOffset ExitAt { get; set; }
        public List<int> DiscountKeys { get; set; } = new();
    }

    public sealed record QuoteParkingFeeResponse(
        long ParkingSessionId,
        string CarNumber,
        DateTimeOffset EntryAt,
        DateTimeOffset ExitAt,
        ParkingFeeResult Fee,
        long PreviousPaidAmount,
        long PayableAmount,
        bool IsPrepayGrace);

    [ApiController]
    [Route("api/v1/fees")]
    public sealed class FeeCalculationController : ControllerBase
    {
        private readonly FeeCalculationService _service;
        private readonly ParkingQuoteService _quoteService;

        public FeeCalculationController(
            FeeCalculationService service,
            ParkingQuoteService quoteService)
        {
            _service = service;
            _quoteService = quoteService;
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
            if (request.Sitenum <= 0 || string.IsNullOrWhiteSpace(request.CarNumber) ||
                request.ExitAt == default)
                return BadRequest();

            try
            {
                QuoteParkingFeeResponse? result = await _quoteService.QuoteByCarNumberAsync(
                    request.Sitenum,
                    request.CarNumber.Trim(),
                    request.ExitAt,
                    cancellationToken);
                return result is null ? NotFound() : Ok(result);
            }
            catch (ArgumentException)
            {
                return BadRequest();
            }
        }

        [HttpPost("quote/session")]
        public async Task<IActionResult> QuoteSessionAsync(
            [FromBody] QuoteParkingSessionRequest request,
            CancellationToken cancellationToken)
        {
            if (request.ParkingSessionId <= 0 || request.ExitAt == default)
                return BadRequest();

            try
            {
                QuoteParkingFeeResponse? result = await _quoteService.QuoteBySessionIdAsync(
                    request.ParkingSessionId,
                    request.ExitAt,
                    request.DiscountKeys,
                    cancellationToken);
                return result is null ? NotFound() : Ok(result);
            }
            catch (ArgumentException)
            {
                return BadRequest();
            }
        }

        private static bool IsValid(CalculateParkingFeeRequest request) =>
            request.Sitenum > 0 &&
            request.Groupnum > 0 &&
            request.EntryAt != default &&
            request.ExitAt > request.EntryAt &&
            request.CarType > 0;
    }
}
