using Microsoft.AspNetCore.Mvc;
using Parking.Central.Data;
using Parking.Contracts;

namespace Parking.Api.Features.Payments;

[ApiController]
[Route("api/v1/payments")]
public sealed class PaymentController : ControllerBase
{
    private readonly IPaymentRepository _repository;

    public PaymentController(IPaymentRepository repository)
    {
        _repository = repository;
    }

    [HttpPost("complete")]
    public async Task<IActionResult> CompleteAsync(
        [FromBody] CompletePaymentRequest request,
        CancellationToken cancellationToken)
    {
        if (!IsValid(request))
            return BadRequest();

        PaymentCompleteResponse result = await _repository.CompleteAsync(
            request,
            cancellationToken);

        if (result.Accepted)
            return Ok(result);

        return result.ResultCode == "PARKING_SESSION_NOT_FOUND"
            ? NotFound(result)
            : Conflict(result);
    }

    private static bool IsValid(CompletePaymentRequest request) =>
        request.PaymentId != Guid.Empty &&
        request.ParkingSessionId > 0 &&
        request.SiteId > 0 &&
        request.OriginalFee >= 0 &&
        request.DiscountFee >= 0 &&
        request.DiscountFee <= request.OriginalFee &&
        request.PaidAmount == request.OriginalFee - request.DiscountFee &&
        !string.IsNullOrWhiteSpace(request.PaymentMethod) &&
        !string.IsNullOrWhiteSpace(request.ApprovalNumber) &&
        request.PaidAt != default;
}
