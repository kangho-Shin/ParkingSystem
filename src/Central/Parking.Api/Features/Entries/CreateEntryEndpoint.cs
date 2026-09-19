using Microsoft.AspNetCore.Mvc;
using Parking.Central.Data;
using Parking.Contracts;

namespace Parking.Api.Features.Entries
{
    [ApiController]
    [Route("api/v1/parking/entries")]
    public sealed class CreateEntryController : ControllerBase
    {
        private readonly CreateEntryHandler _handler;
        private readonly IParkingLaneDirectionValidator _laneValidator;
        public CreateEntryController(
            CreateEntryHandler handler,
            IParkingLaneDirectionValidator laneValidator)
        {
            _handler = handler;
            _laneValidator = laneValidator;
        }

        [HttpPost]
        public async Task<IActionResult> CreateAsync(
            [FromBody] FieldEventRequest request,
            CancellationToken cancellationToken)
        {
            if (request.EventId == Guid.Empty)
                return BadRequest(new { ResultCode = "INVALID_EVENT_ID", Message = "EventId가 필요합니다." });

            if (request.EventType != ParkingEventType.Entry)
                return BadRequest(new
                {
                    ResultCode = "INVALID_EVENT_TYPE",
                    Message = "입차 요청의 EventType은 Entry여야 합니다."
                });

            if (request.SiteId <= 0 || request.Groupnum <= 0 ||
                request.LaneId <= 0 || request.DeviceId <= 0)
                return BadRequest(new
                {
                    ResultCode = "INVALID_DEVICE_IDENTITY",
                    Message = "현장·차로·장비 번호가 올바르지 않습니다."
                });

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
                    Message = "입차 차로·장비·방향 설정이 일치하지 않습니다."
                });

            return Ok(await _handler.HandleAsync(request, cancellationToken));
        }
    }
}
