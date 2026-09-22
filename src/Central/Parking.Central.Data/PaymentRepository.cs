using System.Security.Cryptography;
using System.Text;
using Dapper;
using MySqlConnector;
using Parking.Contracts;

namespace Parking.Central.Data;

internal sealed class PaymentRow
{
    public string PaymentId { get; set; } = "";
    public long ParkingSessionId { get; set; }
    public long SiteId { get; set; }
    public string Fingerprint { get; set; } = "";
}

internal sealed class PaymentSessionRow
{
    public long SiteId { get; set; }
    public int Groupnum { get; set; }
    public string CarNumber { get; set; } = "";
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
                connection, transaction, request.PaymentId, cancellationToken);
            if (existingPayment is not null)
            {
                await transaction.CommitAsync(cancellationToken);
                return SamePayment(existingPayment, request)
                    ? Success(request.PaymentId, existingPayment.ParkingSessionId)
                    : Failure(request, "PAYMENT_ID_CONFLICT", "같은 결제번호의 내용이 다릅니다.");
            }

            PaymentSessionRow? session =
                await connection.QuerySingleOrDefaultAsync<PaymentSessionRow>(
                    new CommandDefinition("""
                        SELECT sitenum SiteId,groupnum Groupnum,carnum CarNumber,
                               outflag OutFlag
                        FROM tparkinfo
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
                connection, transaction, request.ParkingSessionId, cancellationToken);
            if (sessionPayment is not null)
            {
                await transaction.CommitAsync(cancellationToken);
                return sessionPayment.PaymentId == request.PaymentId.ToString("N")
                    ? Success(request.PaymentId, request.ParkingSessionId)
                    : Failure(
                        request,
                        "PARKING_SESSION_ALREADY_PAID",
                        "이미 결제된 주차내역입니다.");
            }

            int paymentType = PaymentType(request.PaymentMethod);
            int deviceNumber = await FindDeviceNumberAsync(
                connection,
                transaction,
                request.SiteId,
                session.Groupnum,
                request.TerminalId,
                cancellationToken);
            DateTime paidAt = ParkingLocalTime.ToDatabase(request.PaidAt);

            await connection.ExecuteAsync(new CommandDefinition("""
                INSERT INTO tbcardinfo
                (paymentid,pindex,sitenum,groupnum,devicenum,carnum,termid,
                 dealtype,credittype,money,rescode,msg,acceptnum,dealdate)
                VALUES
                (@PaymentId,@ParkingSessionId,@SiteId,@Groupnum,@DeviceNumber,
                 @CarNumber,@TerminalId,'APPROVE',@PaymentType,@PaidAmount,
                 '0000',@Fingerprint,@ApprovalNumber,@PaidAt);
                """,
                new
                {
                    PaymentId = request.PaymentId.ToString("N"),
                    request.ParkingSessionId,
                    request.SiteId,
                    session.Groupnum,
                    DeviceNumber = deviceNumber,
                    session.CarNumber,
                    TerminalId = EmptyToNull(request.TerminalId),
                    PaymentType = paymentType,
                    request.PaidAmount,
                    Fingerprint = PaymentFingerprint(request),
                    ApprovalNumber = request.ApprovalNumber.Trim(),
                    PaidAt = paidAt
                },
                transaction,
                cancellationToken: cancellationToken));

            await connection.ExecuteAsync(new CommandDefinition("""
                UPDATE tparkinfo
                SET outflag='X',paydate=@PaidAt,parkfee=@OriginalFee,
                    discountfee=@DiscountFee,payfee=@PaidAmount,
                    paidfee=(SELECT COALESCE(SUM(money),0)
                             FROM tbcardinfo WHERE pindex=@ParkingSessionId),
                    paytype=@PaymentType
                WHERE xindex=@ParkingSessionId;
                """,
                new
                {
                    request.ParkingSessionId,
                    PaidAt = paidAt,
                    request.OriginalFee,
                    request.DiscountFee,
                    request.PaidAmount,
                    PaymentType = paymentType
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
            SELECT c.paymentid PaymentId,c.pindex ParkingSessionId,
                   c.sitenum SiteId,c.msg Fingerprint
            FROM tbcardinfo c
            WHERE c.paymentid=@PaymentId;
            """,
            new { PaymentId = paymentId.ToString("N") },
            transaction,
            cancellationToken: cancellationToken));

    private static Task<PaymentRow?> FindBySessionIdAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        long parkingSessionId,
        CancellationToken cancellationToken) =>
        connection.QueryFirstOrDefaultAsync<PaymentRow>(new CommandDefinition("""
            SELECT paymentid PaymentId,pindex ParkingSessionId
            FROM tbcardinfo
            WHERE pindex=@ParkingSessionId AND dealtype='APPROVE' AND money>0
            ORDER BY xindex DESC LIMIT 1;
            """,
            new { ParkingSessionId = parkingSessionId },
            transaction,
            cancellationToken: cancellationToken));

    private static async Task<int> FindDeviceNumberAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        long siteId,
        int groupnum,
        string? terminalId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(terminalId))
            return 0;
        return await connection.QuerySingleOrDefaultAsync<int>(new CommandDefinition("""
            SELECT devicenum FROM tdeviceinfo
            WHERE sitenum=@SiteId AND groupnum=@Groupnum
              AND termid=@TerminalId AND useflag=1
            ORDER BY deviceid LIMIT 1;
            """,
            new { SiteId = siteId, Groupnum = groupnum, TerminalId = terminalId.Trim() },
            transaction,
            cancellationToken: cancellationToken));
    }

    private static bool SamePayment(PaymentRow row, CompletePaymentRequest request) =>
        row.ParkingSessionId == request.ParkingSessionId &&
        row.SiteId == request.SiteId &&
        row.Fingerprint == PaymentFingerprint(request);

    private static string PaymentFingerprint(CompletePaymentRequest request)
    {
        string canonical = string.Join("\u001f",
            request.ParkingSessionId,
            request.SiteId,
            request.OriginalFee,
            request.DiscountFee,
            request.PaidAmount,
            PaymentType(request.PaymentMethod),
            request.ApprovalNumber.Trim(),
            request.TerminalId?.Trim() ?? "",
            ParkingLocalTime.ToDatabase(request.PaidAt).ToString("yyyyMMddHHmmss"));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }

    private static int PaymentType(string value) =>
        int.TryParse(value, out int result)
            ? result
            : value.Equals("Card", StringComparison.OrdinalIgnoreCase) ? 1 : 0;

    private static string? EmptyToNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static PaymentCompleteResponse Success(Guid paymentId, long parkingSessionId) =>
        new(paymentId, parkingSessionId, true, "PAYMENT_COMPLETED", "결제가 완료되었습니다.", true);

    private static PaymentCompleteResponse Failure(
        CompletePaymentRequest request,
        string resultCode,
        string message) =>
        new(request.PaymentId, request.ParkingSessionId, false, resultCode, message, false);
}
