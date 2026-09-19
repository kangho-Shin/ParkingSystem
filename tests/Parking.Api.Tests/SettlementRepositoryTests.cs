using Dapper;
using MySqlConnector;
using Parking.Central.Data;

namespace Parking.Api.Tests;

[Collection("Database")]
public sealed class SettlementRepositoryTests
{
    private static string ConnectionString =>
        Environment.GetEnvironmentVariable("PARKING_TEST_CONNECTION")
        ?? throw new InvalidOperationException("PARKING_TEST_CONNECTION 환경변수가 없습니다.");

    [Fact]
    public async Task 할인키와_기존결제금액을_주차건에서_조회한다()
    {
        await using MySqlConnection connection = new(ConnectionString);
        long parkingSessionId = await connection.ExecuteScalarAsync<long>("""
            INSERT INTO parking_session
            (sitenum,ineventid,carnum,groupnum,cartype,inlaneid,indate,status)
            VALUES (1,@EventId,@CarNumber,1,1,10,UTC_TIMESTAMP(6),'Paid');
            SELECT LAST_INSERT_ID();
            """, new
        {
            EventId = Guid.NewGuid().ToByteArray(),
            CarNumber = $"TEST{Guid.NewGuid():N}"[..20]
        });

        await connection.ExecuteAsync("""
            INSERT INTO parking_session_discount
            (parkindex,carnum,discountkey,source,sourceref,discounttype,
             discountvalue,sdate,applydate)
            VALUES
            (@ParkingSessionId,'TEST',10,'Test','A',1,30,UTC_TIMESTAMP(6),UTC_TIMESTAMP(6)),
            (@ParkingSessionId,'TEST',20,'Test','B',4,50,UTC_TIMESTAMP(6),UTC_TIMESTAMP(6));

            INSERT INTO payment
            (paymentid,parkindex,sitenum,originalfee,discountfee,payamount,
             paymethod,approvalnum,paydate)
            VALUES
            (@PaymentId1,@ParkingSessionId,1,1000,0,400,'Card','A',UTC_TIMESTAMP(6)),
            (@PaymentId2,@ParkingSessionId,1,1000,0,300,'Card','B',UTC_TIMESTAMP(6));
            """, new
        {
            ParkingSessionId = parkingSessionId,
            PaymentId1 = Guid.NewGuid().ToByteArray(),
            PaymentId2 = Guid.NewGuid().ToByteArray()
        });

        SettlementRepository repository = new(ConnectionString);
        SettlementData result = await repository.GetAsync(
            parkingSessionId,
            CancellationToken.None);

        Assert.Equal(new[] { 10, 20 }, result.DiscountKeys);
        Assert.Equal(700, result.PaidAmount);
        Assert.NotNull(result.LastPaydate);
    }
}
