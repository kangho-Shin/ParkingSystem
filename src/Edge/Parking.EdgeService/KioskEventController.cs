using Microsoft.AspNetCore.Mvc;
using Parking.Contracts;

namespace Parking.EdgeService;

[ApiController]
[Route("api/v1/local/kiosks")]
public sealed class KioskEventController : ControllerBase
{
    private readonly IKioskExitCoordinator _coordinator;
    private readonly IEdgeDeviceResolver _resolver;
    public KioskEventController(IKioskExitCoordinator coordinator, IEdgeDeviceResolver resolver)
    {
        _coordinator = coordinator;
        _resolver = resolver;
    }

    [HttpPost("events/{eventId:guid}/complete")]
    public async Task<IActionResult> CompleteAsync(
        Guid eventId,
        [FromBody] KioskDeviceIdentity identity,
        CancellationToken cancellationToken)
    {
        if (eventId == Guid.Empty || identity.Sitenum <= 0 ||
            identity.Groupnum <= 0 || identity.Devicenum <= 0)
            return BadRequest();
        try
        {
            ParkingDevice kiosk = await _resolver.ResolveAsync(
                new EdgeDeviceIdentity(
                    identity.Sitenum, identity.Groupnum, identity.Devicenum, "KIOSK"),
                cancellationToken);
            FieldEventResponse? response = await _coordinator.CompleteAsync(
                kiosk.DeviceId, eventId, cancellationToken);
            return response is null ? NotFound() : Ok(response);
        }
        catch (DeviceIdentityException)
        {
            return NotFound();
        }
    }
}
