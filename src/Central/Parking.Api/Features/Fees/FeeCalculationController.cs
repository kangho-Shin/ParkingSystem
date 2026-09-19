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
        private readonly IParkingExitRepository _parkingRepository;
        private readonly ISettlementRepository _settlementRepository;

        public FeeCalculationController(
            FeeCalculationService service,
            IParkingExitRepository parkingRepository,
            ISettlementRepository settlementRepository)
        {
            _service = service;
            _parkingRepository = parkingRepository;
            _settlementRepository = settlementRepository;
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

            OpenParkingSessionResponse? session = await _parkingRepository.FindOpenAsync(
                request.Sitenum,
                request.CarNumber.Trim(),
                cancellationToken);

            if (session is null)
                return NotFound();

            SettlementData settlement = await _settlementRepository.GetAsync(
                session.ParkingSessionId,
                cancellationToken);

            var calculationRequest = new CalculateParkingFeeRequest
            {
                Sitenum = request.Sitenum,
                Groupnum = session.Groupnum,
                EntryAt = session.EntryAt.ToOffset(request.ExitAt.Offset).DateTime,
                ExitAt = request.ExitAt.DateTime,
                CarType = session.CarType,
                DiscountKeys = settlement.DiscountKeys.ToList()
            };

            if (!IsValid(calculationRequest))
                return BadRequest();

            FeeCalculationResult calculation = await _service.CalculateSettlementAsync(
                calculationRequest,
                cancellationToken);
            ParkingSettlementResult settlementResult = ParkingSettlementCalculator.Calculate(
                calculation.Fee.FinalFee,
                settlement.PaidAmount,
                settlement.LastPaydate,
                request.ExitAt,
                calculation.PrepayGraceTime);
            return Ok(new QuoteParkingFeeResponse(
                session.ParkingSessionId,
                session.CarNumber,
                session.EntryAt,
                request.ExitAt,
                calculation.Fee,
                settlement.PaidAmount,
                settlementResult.PayableAmount,
                settlementResult.IsPrepayGrace));
        }

        private static bool IsValid(CalculateParkingFeeRequest request) =>
            request.Sitenum > 0 &&
            request.Groupnum > 0 &&
            request.EntryAt != default &&
            request.ExitAt > request.EntryAt &&
            request.CarType > 0;
    }
}
