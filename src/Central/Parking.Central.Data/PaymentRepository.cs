using Dapper;
using MySqlConnector;
using Parking.Contracts;

namespace Parking.Central.Data;

internal sealed class PaymentRow
{
    public byte[] PaymentId { get; set; } = Array.Empty<byte>();
    public long ParkingSessionId { get; set; }
    public long SiteId { get; set; }
    public long OriginalFee { get; set; }
    public long DiscountFee { get; set; }
    public long PaidAmount { get; set; }
    public string PaymentMethod { get; set; } = "";
    public string ApprovalNumber { get; set; } = "";
    public string? TerminalId { get; set; }
    public DateTime PaidAt { get; set; }
}

internal sealed class PaymentSessionRow
{
    public long SiteId { get; set; }
    public string OutFlag { get; set; } = "";
}

public sealed class PaymentRepository : IPaymentRepository
{
    private readonly string _connectionString;

    public PaymentRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<PaymentCompleteResponse> CompleteAsync(
        CompletePaymentRequest request,
        CancellationToken cancellationToken)
    {
        await using MySqlConnection connection = new(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using MySqlTransaction transaction =
            await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            PaymentRow? existingPayment = await FindByPaymentIdAsync(
                connection,
                transaction,
                request.PaymentId,
                cancellationToken);

            if (existingPayment is not null)
            {
                await transaction.CommitAsync(cancellationToken);
                return SamePayment(existingPayment, request)
                    ? Success(request.PaymentId, existingPayment.ParkingSessionId)
                    : Failure(request, "PAYMENT_ID_CONFLICT", "같은 결제번호의 내용이 다릅니다.");
            }

            PaymentSessionRow? session = await connection.QuerySingleOrDefaultAsync<PaymentSessionRow>(
                new CommandDefinition("""
                    SELECT sitenum SiteId, outflag OutFlag
                    FROM parking_session
                    WHERE xindex=@ParkingSessionId
                    FOR UPDATE;
                    """,
                    new { request.ParkingSessionId },
                    transaction,
                    cancellationToken: cancellationToken));

            if (session is null || session.SiteId != request.SiteId)
            {
                await transaction.CommitAsync(cancellationToken);
                return Failure(request, "PARKING_SESSION_NOT_FOUND", "주차내역이 없습니다.");
            }

            if (session.OutFlag == "O")
            {
                await transaction.CommitAsync(cancellationToken);
                return Failure(request, "PARKING_SESSION_EXITED", "이미 출차된 주차내역입니다.");
            }

            PaymentRow? sessionPayment = await FindBySessionIdAsync(
                connection,
                transaction,
                request.ParkingSessionId,
                cancellationToken);

            if (sessionPayment is not null)
            {
                await transaction.CommitAsync(cancellationToken);
                return sessionPayment.PaymentId.AsSpan().SequenceEqual(request.PaymentId.ToByteArray())
                    ? Success(request.PaymentId, request.ParkingSessionId)
                    : Failure(request, "PARKING_SESSION_ALREADY_PAID", "이미 결제된 주차내역입니다.");
            }

            await connection.ExecuteAsync(new CommandDefinition("""
                INSERT INTO payment
                (paymentid,parkindex,sitenum,originalfee,discountfee,
                 payamount,paymethod,approvalnum,terminalid,paydate)
                VALUES
                (@PaymentId,@ParkingSessionId,@SiteId,@OriginalFee,@DiscountFee,
                 @PaidAmount,@PaymentMethod,@ApprovalNumber,@TerminalId,@PaidAtLocal);
                """,
                new
                {
                    PaymentId = request.PaymentId.ToByteArray(),
                    request.ParkingSessionId,
                    request.SiteId,
                    request.OriginalFee,
                    request.DiscountFee,
                    request.PaidAmount,
                    PaymentMethod = request.PaymentMethod.Trim(),
                    ApprovalNumber = request.ApprovalNumber.Trim(),
                    TerminalId = string.IsNullOrWhiteSpace(request.TerminalId)
                        ? null
                        : request.TerminalId.Trim(),
                    PaidAtLocal = ParkingLocalTime.ToDatabase(request.PaidAt)
                },
                transaction,
                cancellationToken: cancellationToken));

            await connection.ExecuteAsync(new CommandDefinition("""
                UPDATE parking_session
                SET outflag='X', paydate=@PaidAtLocal
                WHERE xindex=@ParkingSessionId;
                """,
                new
                {
                    request.ParkingSessionId,
                    PaidAtLocal = ParkingLocalTime.ToDatabase(request.PaidAt)
                },
                transaction,
                cancellationToken: cancellationToken));

            await transaction.CommitAsync(cancellationToken);
            return Success(request.PaymentId, request.ParkingSessionId);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static Task<PaymentRow?> FindByPaymentIdAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        Guid paymentId,
        CancellationToken cancellationToken) =>
        connection.QuerySingleOrDefaultAsync<PaymentRow>(new CommandDefinition("""
            SELECT paymentid PaymentId, parkindex ParkingSessionId, sitenum SiteId,
                   originalfee OriginalFee, discountfee DiscountFee, payamount PaidAmount,
                   paymethod PaymentMethod, approvalnum ApprovalNumber,
                   terminalid TerminalId, paydate PaidAt
            FROM payment WHERE paymentid=@PaymentId;
            """,
            new { PaymentId = paymentId.ToByteArray() },
            transaction,
            cancellationToken: cancellationToken));

    private static Task<PaymentRow?> FindBySessionIdAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        long parkingSessionId,
        CancellationToken cancellationToken) =>
        connection.QuerySingleOrDefaultAsync<PaymentRow>(new CommandDefinition("""
            SELECT paymentid PaymentId, parkindex ParkingSessionId
            FROM payment WHERE parkindex=@ParkingSessionId;
            """,
            new { ParkingSessionId = parkingSessionId },
            transaction,
            cancellationToken: cancellationToken));

    private static bool SamePayment(PaymentRow row, CompletePaymentRequest request) =>
        row.ParkingSessionId == request.ParkingSessionId &&
        row.SiteId == request.SiteId &&
        row.OriginalFee == request.OriginalFee &&
        row.DiscountFee == request.DiscountFee &&
        row.PaidAmount == request.PaidAmount &&
        row.PaymentMethod == request.PaymentMethod.Trim() &&
        row.ApprovalNumber == request.ApprovalNumber.Trim() &&
        (row.TerminalId ?? "") == (request.TerminalId?.Trim() ?? "") &&
        row.PaidAt == ParkingLocalTime.ToDatabase(request.PaidAt);

    private static PaymentCompleteResponse Success(Guid paymentId, long parkingSessionId) =>
        new(paymentId, parkingSessionId, true, "PAYMENT_COMPLETED", "결제가 완료되었습니다.", true);

    private static PaymentCompleteResponse Failure(
        CompletePaymentRequest request,
        string resultCode,
        string message) =>
        new(request.PaymentId, request.ParkingSessionId, false, resultCode, message, false);
}
