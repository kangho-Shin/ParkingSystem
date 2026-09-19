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

        public ExitController(
            IParkingExitRepository repository,
            FeeCalculationService feeService)
        {
            _repository = repository;
            _feeService = feeService;
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
                request.Groupnum <= 0 || request.CarType <= 0 ||
                string.IsNullOrWhiteSpace(request.CarNumber) ||
                request.OccurredAt == default)
                return BadRequest();

            bool isFreeExit = false;
            OpenParkingSessionResponse? session = await _repository.FindOpenAsync(
                request.SiteId,
                request.CarNumber.Trim(),
                cancellationToken);

            if (session is not null && request.OccurredAt < session.EntryAt)
                return BadRequest();

            if (session is not null && session.Status != "Paid")
            {
                ParkingFeeResult fee = await _feeService.CalculateAsync(
                    new CalculateParkingFeeRequest
                    {
                        Sitenum = checked((int)request.SiteId),
                        Groupnum = request.Groupnum,
                        EntryAt = session.EntryAt.ToOffset(request.OccurredAt.Offset).DateTime,
                        ExitAt = request.OccurredAt.DateTime,
                        CarType = request.CarType,
                        DiscountKeys = request.DiscountKeys ?? new List<int>()
                    },
                    cancellationToken);

                isFreeExit = fee.FinalFee == 0;
            }

            return Ok(await _repository.SaveExitAsync(
                request,
                isFreeExit,
                cancellationToken));
        }
    }
}
