using Microsoft.AspNetCore.Mvc;
using Parking.Central.Data;
using Parking.Contracts;

namespace Parking.Api.Features.Search;

[ApiController]
[Route("api/v1/parking/sessions")]
public sealed class ParkingCorrectionController : ControllerBase
{
    private readonly IParkingCorrectionRepository _repository;
    public ParkingCorrectionController(IParkingCorrectionRepository repository) => _repository = repository;

    [HttpPut("{parkingSessionId:long}/car-number")]
    public async Task<IActionResult> CorrectAsync(long parkingSessionId, [FromBody] CorrectCarNumberRequest request, CancellationToken cancellationToken)
    {
        if (parkingSessionId <= 0 || string.IsNullOrWhiteSpace(request.CarNumber)) return BadRequest();
        CorrectCarNumberResponse result = await _repository.CorrectCarNumberAsync(parkingSessionId, request.CarNumber, cancellationToken);
        return result.Updated ? Ok(result) : NotFound(result);
    }
}
