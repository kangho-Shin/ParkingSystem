using Parking.Contracts;

namespace Parking.Central.Data;

public interface IPaymentRepository
{
    Task<PaymentCompleteResponse> CompleteAsync(
        CompletePaymentRequest request,
        CancellationToken cancellationToken);
}
