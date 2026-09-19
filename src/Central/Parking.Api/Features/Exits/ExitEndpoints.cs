using Microsoft.AspNetCore.Mvc;
using Parking.Central.Data;
using Parking.Contracts;
using Parking.Api.Features.Fees;
using Parking.FeeEngine;

namespace Parking.Api.Features.Exits
{
    [ApiController]
    [Route("api/v1/parking")]
    public sealed class ExitController : ControllerBase
    {
        private readonly IParkingExitRepository _repository;
        private readonly FeeCalculationService _feeService;
        private readonly ISettlementRepository _settlementRepository;

        public ExitController(
            IParkingExitRepository repository,
            FeeCalculationService feeService,
            ISettlementRepository settlementRepository)
        {
            _repository = repository;
            _feeService = feeService;
            _settlementRepository = settlementRepository;
        }

        [HttpGet("open")]
        public async Task<IActionResult> FindOpenAsync(
            [FromQuery] long siteId,
            [FromQuery] string carNumber,
            CancellationToken cancellationToken)
        {
            OpenParkingSessionResponse? session =
                await _repository.FindOpenAsync(siteId, carNumber, cancellationToken);
            return session is null ? NotFound() : Ok(session);
        }

        [HttpPost("exits")]
        public async Task<IActionResult> ExitAsync(
            [FromBody] ExitEventRequest request,
            CancellationToken cancellationToken)
        {
            if (request.EventId == Guid.Empty || request.SiteId <= 0 ||
                request.SiteId > int.MaxValue ||
                request.LaneId <= 0 || request.DeviceId <= 0 ||
                string.IsNullOrWhiteSpace(request.CarNumber) ||
                request.OccurredAt == default)
                return BadRequest();

            bool exitAllowed = false;
            OpenParkingSessionResponse? session = await _repository.FindOpenAsync(
                request.SiteId,
                request.CarNumber.Trim(),
                cancellationToken);

            if (session is not null && request.OccurredAt < session.EntryAt)
                return BadRequest();

            if (session is not null)
            {
                SettlementData settlement = await _settlementRepository.GetAsync(
                    session.ParkingSessionId,
                    cancellationToken);
                FeeCalculationResult calculation = await _feeService.CalculateSettlementAsync(
                    new CalculateParkingFeeRequest
                    {
                        Sitenum = checked((int)request.SiteId),
                        Groupnum = session.Groupnum,
                        EntryAt = session.EntryAt.ToOffset(request.OccurredAt.Offset).DateTime,
                        ExitAt = request.OccurredAt.DateTime,
                        CarType = session.CarType,
                        DiscountKeys = settlement.DiscountKeys.ToList()
                    },
                    cancellationToken);

                ParkingSettlementResult settlementResult = ParkingSettlementCalculator.Calculate(
                    calculation.Fee.FinalFee,
                    settlement.PaidAmount,
                    settlement.LastPaydate,
                    request.OccurredAt,
                    calculation.PrepayGraceTime);
                exitAllowed = settlementResult.PayableAmount == 0;
            }

            return Ok(await _repository.SaveExitAsync(
                request,
                exitAllowed,
                cancellationToken));
        }
    }
}
