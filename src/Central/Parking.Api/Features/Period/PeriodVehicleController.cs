using Microsoft.AspNetCore.Mvc;
using Parking.Central.Data;
using Parking.Contracts;

namespace Parking.Api.Features.Period;

[ApiController]
[Route("api/v1/period")]
public sealed class PeriodVehicleController : ControllerBase
{
    private readonly IPeriodVehicleRepository _repository;
    private readonly IPeriodMemberManagementRepository _memberRepository;

    public PeriodVehicleController(
        IPeriodVehicleRepository repository,
        IPeriodMemberManagementRepository memberRepository)
    {
        _repository = repository;
        _memberRepository = memberRepository;
    }

    [HttpGet("members/search")]
    public async Task<IActionResult> FindMemberAsync(
        [FromQuery] long siteId,
        [FromQuery] int groupnum,
        [FromQuery] string carNumber,
        [FromQuery] DateTimeOffset? at,
        CancellationToken cancellationToken)
    {
        if (siteId <= 0 || groupnum <= 0 || string.IsNullOrWhiteSpace(carNumber))
            return BadRequest();

        PeriodMember? result = await _repository.FindMemberAsync(
            siteId,
            groupnum,
            carNumber,
            at ?? DateTimeOffset.Now,
            cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("open")]
    public async Task<IActionResult> FindOpenAsync(
        [FromQuery] long siteId,
        [FromQuery] int groupnum,
        [FromQuery] string carNumber,
        CancellationToken cancellationToken)
    {
        if (siteId <= 0 || groupnum <= 0 || string.IsNullOrWhiteSpace(carNumber))
            return BadRequest();

        OpenPeriodSession? result = await _repository.FindOpenAsync(
            siteId,
            groupnum,
            carNumber,
            cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("members")]
    public async Task<IActionResult> SearchMembersAsync(
        [FromQuery] long siteId,
        [FromQuery] int? groupnum,
        [FromQuery] string? carNumber,
        CancellationToken cancellationToken)
    {
        if (siteId <= 0 || groupnum <= 0)
            return BadRequest();

        IReadOnlyList<PeriodMemberDetail> result = await _memberRepository.SearchAsync(
            siteId, groupnum, carNumber, cancellationToken);
        return Ok(result);
    }

    [HttpGet("members/{memberId:long}")]
    public async Task<IActionResult> GetMemberAsync(
        long memberId,
        CancellationToken cancellationToken)
    {
        if (memberId <= 0)
            return BadRequest();

        PeriodMemberDetail? result = await _memberRepository.GetAsync(memberId, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("members")]
    public async Task<IActionResult> CreateMemberAsync(
        [FromBody] PeriodMemberSaveRequest request,
        CancellationToken cancellationToken)
    {
        if (!IsValid(request))
            return BadRequest();

        long memberId = await _memberRepository.CreateAsync(request, cancellationToken);
        PeriodMemberDetail? result = await _memberRepository.GetAsync(memberId, cancellationToken);
        return Created($"/api/v1/period/members/{memberId}", result);
    }

    [HttpPut("members/{memberId:long}")]
    public async Task<IActionResult> UpdateMemberAsync(
        long memberId,
        [FromBody] PeriodMemberSaveRequest request,
        CancellationToken cancellationToken)
    {
        if (memberId <= 0 || !IsValid(request))
            return BadRequest();

        bool updated = await _memberRepository.UpdateAsync(memberId, request, cancellationToken);
        return updated ? NoContent() : NotFound();
    }

    [HttpDelete("members/{memberId:long}")]
    public async Task<IActionResult> DeleteMemberAsync(
        long memberId,
        CancellationToken cancellationToken)
    {
        if (memberId <= 0)
            return BadRequest();

        bool deleted = await _memberRepository.DeleteAsync(memberId, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    private static bool IsValid(PeriodMemberSaveRequest request) =>
        request.SiteId > 0 &&
        request.Groupnum > 0 &&
        !string.IsNullOrWhiteSpace(request.Name) &&
        !string.IsNullOrWhiteSpace(request.CarNumber1) &&
        request.CarType1 is >= 1 and <= 3 &&
        request.StartDate != default &&
        request.EndDate >= request.StartDate &&
        request.ParkArea is { Length: 7 } &&
        request.ParkValidDay is { Length: 7 } &&
        request.OutFlag is "I" or "O";
}
