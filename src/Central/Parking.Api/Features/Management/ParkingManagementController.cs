using Microsoft.AspNetCore.Mvc;
using Parking.Central.Data;
using Parking.Contracts;

namespace Parking.Api.Features.Management;

[ApiController]
[Route("api/v1/management/parking")]
public sealed class ParkingManagementController : ControllerBase
{
    private readonly IParkingManagementRepository _repository;
    private readonly ISiteConfigurationRepository _siteRepository;
    private readonly IParkingLaneDirectionValidator _laneValidator;
    private readonly ManualEntryHandler _manualEntryHandler;

    public ParkingManagementController(
        IParkingManagementRepository repository,
        ISiteConfigurationRepository siteRepository,
        IParkingLaneDirectionValidator laneValidator,
        ManualEntryHandler manualEntryHandler)
    {
        _repository = repository;
        _siteRepository = siteRepository;
        _laneValidator = laneValidator;
        _manualEntryHandler = manualEntryHandler;
    }

    [HttpGet("entries")]
    public async Task<IActionResult> GetEntriesAsync(
        [FromQuery] long siteId,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] int? groupnum,
        [FromQuery] long? deviceId,
        [FromQuery] string? carNumber,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 200,
        CancellationToken cancellationToken = default)
    {
        if (!await IsAuthorizedAsync(siteId, cancellationToken)) return Unauthorized();
        if (!ValidPage(page, pageSize) || !ValidOptionalRange(from, to))
            return BadRequest(new { Message = "조회 조건이 올바르지 않습니다." });
        return Ok(await _repository.SearchEntriesAsync(new ParkingManagementQuery(
            siteId, from, to, groupnum, deviceId, carNumber, null, page, pageSize),
            cancellationToken));
    }

    [HttpGet("exits")]
    public async Task<IActionResult> GetExitsAsync(
        [FromQuery] long siteId,
        [FromQuery] DateTimeOffset from,
        [FromQuery] DateTimeOffset to,
        [FromQuery] string? status,
        [FromQuery] int? groupnum,
        [FromQuery] long? deviceId,
        [FromQuery] string? carNumber,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 200,
        CancellationToken cancellationToken = default)
    {
        if (!await IsAuthorizedAsync(siteId, cancellationToken)) return Unauthorized();
        if (!ValidPage(page, pageSize) || to < from || to - from > TimeSpan.FromDays(31) ||
            status is not null && status is not "X" and not "O")
            return BadRequest(new { Message = "조회 조건이 올바르지 않습니다." });
        return Ok(await _repository.SearchExitsAsync(new ParkingManagementQuery(
            siteId, from, to, groupnum, deviceId, carNumber, status, page, pageSize),
            cancellationToken));
    }

    [HttpPost("manual-entries")]
    public async Task<IActionResult> CreateManualEntryAsync(
        [FromBody] ManualEntryRequest request,
        CancellationToken cancellationToken)
    {
        if (!await IsAuthorizedAsync(request.SiteId, cancellationToken)) return Unauthorized();
        if (request.SiteId <= 0 || request.Groupnum <= 0 || request.LaneId <= 0 ||
            request.DeviceId <= 0 || string.IsNullOrWhiteSpace(request.CarNumber) ||
            request.InDateTime == default || request.CarType is < 1 or > 3)
            return BadRequest(new { Message = "수동입차 입력값이 올바르지 않습니다." });
        if (!await _laneValidator.IsValidAsync(
                request.SiteId, request.Groupnum, request.LaneId, request.DeviceId,
                ParkingEventType.Entry, cancellationToken))
            return BadRequest(new { Message = "입차 차로와 장치를 확인하세요." });
        ManualEntryResponse response = await _manualEntryHandler.HandleAsync(
            request, cancellationToken);
        return response.Accepted ? Ok(response) : Conflict(response);
    }

    [HttpPut("sessions/{sessionType}/{parkingSessionId:long}/car-number")]
    public async Task<IActionResult> CorrectCarNumberAsync(
        ParkingSessionType sessionType,
        long parkingSessionId,
        [FromBody] ManagementCarNumberRequest request,
        CancellationToken cancellationToken)
    {
        if (!await IsAuthorizedAsync(request.SiteId, cancellationToken)) return Unauthorized();
        if (parkingSessionId <= 0 || string.IsNullOrWhiteSpace(request.CarNumber))
            return BadRequest(new { Message = "차량번호가 필요합니다." });
        bool changed = await _repository.CorrectCarNumberAsync(
            request.SiteId, sessionType, parkingSessionId,
            request.CarNumber, cancellationToken);
        return changed
            ? Ok(new { Updated = true, request.CarNumber })
            : NotFound(new { Updated = false, Message = "현재 입차차량이 아닙니다." });
    }

    private Task<bool> IsAuthorizedAsync(long siteId, CancellationToken cancellationToken) =>
        siteId > 0
            ? _siteRepository.ValidateSiteKeyAsync(
                siteId, Request.Headers["X-Site-Key"].ToString(), cancellationToken)
            : Task.FromResult(false);

    private static bool ValidPage(int page, int pageSize) =>
        page >= 1 && pageSize is >= 1 and <= 500;

    private static bool ValidOptionalRange(DateTimeOffset? from, DateTimeOffset? to) =>
        !from.HasValue || !to.HasValue || to.Value >= from.Value;
}
