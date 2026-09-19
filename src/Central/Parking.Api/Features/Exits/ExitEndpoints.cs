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
        private readonly IParkingLaneDirectionValidator _laneValidator;

        public ExitController(
            IParkingExitRepository repository,
            FeeCalculationService feeService,
            ISettlementRepository settlementRepository,
            IParkingLaneDirectionValidator laneValidator)
        {
            _repository = repository;
            _feeService = feeService;
            _settlementRepository = settlementRepository;
            _laneValidator = laneValidator;
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
            if (request.EventType != ParkingEventType.Exit)
                return BadRequest(new
                {
                    ResultCode = "INVALID_EVENT_TYPE",
                    Message = "출차 요청의 EventType은 Exit여야 합니다."
                });

            if (request.EventId == Guid.Empty || request.SiteId <= 0 ||
                request.SiteId > int.MaxValue ||
                request.Groupnum <= 0 ||
                request.LaneId <= 0 || request.DeviceId <= 0 ||
                string.IsNullOrWhiteSpace(request.CarNumber) ||
                request.OutDateTime == default)
                return BadRequest();

            if (!await _laneValidator.IsValidAsync(
                    request.SiteId,
                    request.Groupnum,
                    request.LaneId,
                    request.DeviceId,
                    request.EventType,
                    cancellationToken))
                return BadRequest(new
                {
                    ResultCode = "INVALID_LANE_DIRECTION",
                    Message = "출차 차로·장비·방향 설정이 일치하지 않습니다."
                });

            bool exitAllowed = false;
            OpenParkingSessionResponse? session = await _repository.FindOpenAsync(
                request.SiteId,
                request.CarNumber.Trim(),
                cancellationToken);

            if (session is not null && request.OutDateTime < session.EntryAt)
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
                        EntryAt = session.EntryAt.ToOffset(request.OutDateTime.Offset).DateTime,
                        ExitAt = request.OutDateTime.DateTime,
                        CarType = session.CarType,
                        DiscountKeys = settlement.DiscountKeys.ToList()
                    },
                    cancellationToken);

                ParkingSettlementResult settlementResult = ParkingSettlementCalculator.Calculate(
                    calculation.Fee.FinalFee,
                    settlement.PaidAmount,
                    settlement.LastPaydate,
                    request.OutDateTime,
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
