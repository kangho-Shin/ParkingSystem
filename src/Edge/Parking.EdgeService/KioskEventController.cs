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

    [HttpPost("display")]
    public async Task<IActionResult> DisplayAsync(
        [FromBody] KioskDisplayRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Device is null || request.Device.Sitenum <= 0 || request.Device.Groupnum <= 0 ||
            request.Device.Devicenum <= 0 || string.IsNullOrWhiteSpace(request.CarNumber) ||
            string.IsNullOrWhiteSpace(request.DisplayMessage) ||
            request.DisplaySeconds is < 1 or > 100)
            return BadRequest();
        try
        {
            ParkingDevice kiosk = await _resolver.ResolveAsync(
                new EdgeDeviceIdentity(
                    request.Device.Sitenum, request.Device.Groupnum,
                    request.Device.Devicenum, "KIOSK"),
                cancellationToken);
            await _coordinator.DisplayAsync(
                kiosk.DeviceId, request.Device.Sitenum, request.Device.Groupnum,
                request.CarNumber, request.DisplayMessage,
                request.DisplaySeconds, cancellationToken);
            return Ok();
        }
        catch (DeviceIdentityException)
        {
            return NotFound();
        }
    }

    [HttpPost("manual-exit/complete")]
    public async Task<IActionResult> CompleteManualAsync(
        [FromBody] KioskManualExitRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Device is null || request.Device.Sitenum <= 0 ||
            request.Device.Groupnum <= 0 || request.Device.Devicenum <= 0 ||
            string.IsNullOrWhiteSpace(request.CarNumber))
            return BadRequest();
        try
        {
            ParkingDevice kiosk = await _resolver.ResolveAsync(
                new EdgeDeviceIdentity(
                    request.Device.Sitenum, request.Device.Groupnum,
                    request.Device.Devicenum, "KIOSK"), cancellationToken);
            FieldEventResponse? response = await _coordinator.CompleteManualAsync(
                kiosk.DeviceId, request.Device.Sitenum, request.Device.Groupnum,
                request.CarNumber, request.ExitAt, cancellationToken);
            return response is null ? NotFound() : Ok(response);
        }
        catch (DeviceIdentityException)
        {
            return NotFound();
        }
    }

    [HttpPost("display/reset")]
    public async Task<IActionResult> ResetDisplayAsync(
        [FromBody] KioskDeviceIdentity identity,
        CancellationToken cancellationToken)
    {
        if (identity.Sitenum <= 0 || identity.Groupnum <= 0 || identity.Devicenum <= 0)
            return BadRequest();
        try
        {
            ParkingDevice kiosk = await _resolver.ResolveAsync(
                new EdgeDeviceIdentity(
                    identity.Sitenum, identity.Groupnum, identity.Devicenum, "KIOSK"),
                cancellationToken);
            await _coordinator.ResetDisplayAsync(
                kiosk.DeviceId, identity.Sitenum, cancellationToken);
            return Ok();
        }
        catch (DeviceIdentityException)
        {
            return NotFound();
        }
    }
}
