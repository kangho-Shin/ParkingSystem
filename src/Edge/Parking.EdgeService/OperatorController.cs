using Microsoft.AspNetCore.Mvc;
using Parking.Contracts;

namespace Parking.EdgeService;

[ApiController]
[Route("api/v1/local/operator")]
public sealed class OperatorController : ControllerBase
{
    private readonly IDisplayBoardOutput _displayBoardOutput;
    public OperatorController(IDisplayBoardOutput displayBoardOutput) => _displayBoardOutput = displayBoardOutput;

    [HttpPost("barrier/open")]
    public async Task<IActionResult> OpenBarrierAsync([FromBody] OperatorBarrierRequest request, CancellationToken cancellationToken)
    {
        if (request.SiteId <= 0 || request.LaneId <= 0 || request.DeviceId <= 0) return BadRequest();
        LprRecognition recognition = new(
            Guid.NewGuid(), request.SiteId, 1, request.DeviceId, request.LaneId,
            ParkingEventType.Exit, DateTimeOffset.Now, request.CarNumber.Trim(), "");
        await _displayBoardOutput.SendFromDeviceAsync(
            request.DeviceId,
            recognition,
            new FieldEventResponse(Guid.NewGuid(), true, null, "OPERATOR_OPEN", "관리자 수동개방", true),
            cancellationToken);
        return Ok(new { Accepted = true, ResultCode = "OPERATOR_OPEN" });
    }
}
