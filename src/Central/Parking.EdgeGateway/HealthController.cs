using Microsoft.AspNetCore.Mvc;
using Parking.Contracts;

namespace Parking.EdgeGateway;

[ApiController]
[Route("health")]
public sealed class HealthController : ControllerBase
{
    private readonly ParkingApiClient _parkingApiClient;

    public HealthController(ParkingApiClient parkingApiClient)
    {
        _parkingApiClient = parkingApiClient;
    }

    [HttpGet]
    public async Task<ActionResult<GatewayHealthResponse>> GetAsync(
        CancellationToken cancellationToken)
    {
        bool centralConnected;
        try
        {
            centralConnected = await _parkingApiClient.CheckHealthAsync(cancellationToken);
        }
        catch (Exception exception) when (
            exception is HttpRequestException ||
            exception is TaskCanceledException)
        {
            centralConnected = false;
        }

        return Ok(new GatewayHealthResponse(true, centralConnected));
    }
}
