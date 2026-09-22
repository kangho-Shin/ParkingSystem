using Dapper;
using MySqlConnector;
using Parking.Central.Data;

namespace Parking.Api.Tests;

[Collection("Database")]
[Trait("Category", "DatabaseMutation")]
public sealed class SettlementRepositoryTests
{
    private static string ConnectionString =>
        Environment.GetEnvironmentVariable("PARKING_RUNTIME_CONNECTION")
        ?? throw new InvalidOperationException("PARKING_RUNTIME_CONNECTION 환경변수가 없습니다.");

    [Fact]
    public async Task 할인키와_기존결제금액을_주차건에서_조회한다()
    {
        await using MySqlConnection connection = new(ConnectionString);
        long parkingSessionId = await connection.ExecuteScalarAsync<long>("""
            INSERT INTO tparkinfo
            (sitenum,ineventid,carnum,groupnum,cartype,inlaneid,indevicenum,indate,outflag)
            VALUES (9001,@EventId,@CarNumber,2,1,9010,401,UTC_TIMESTAMP(),'X');
            SELECT LAST_INSERT_ID();
            """, new
        {
            EventId = Guid.NewGuid().ToString("N"),
            CarNumber = $"TEST{Guid.NewGuid():N}"[..20]
        });

        await connection.ExecuteAsync("""
            INSERT INTO tdiscountinfo
            (discountid,pindex,sitenum,groupnum,carnum,diskey,distype,
             disvalue,source,sourceref,indate,disdate)
            VALUES
            (@DiscountId1,@ParkingSessionId,9001,2,'TEST',10,1,30,'Test','A',UTC_TIMESTAMP(),UTC_TIMESTAMP()),
            (@DiscountId2,@ParkingSessionId,9001,2,'TEST',20,4,50,'Test','B',UTC_TIMESTAMP(),UTC_TIMESTAMP());

            INSERT INTO tbcardinfo
            (paymentid,pindex,sitenum,groupnum,devicenum,dealtype,money,
             acceptnum,dealdate)
            VALUES
            (@PaymentId1,@ParkingSessionId,9001,2,0,'APPROVE',400,'A',UTC_TIMESTAMP()),
            (@PaymentId2,@ParkingSessionId,9001,2,0,'APPROVE',300,'B',UTC_TIMESTAMP());
            """, new
        {
            ParkingSessionId = parkingSessionId,
            DiscountId1 = Guid.NewGuid().ToString("N"),
            DiscountId2 = Guid.NewGuid().ToString("N"),
            PaymentId1 = Guid.NewGuid().ToString("N"),
            PaymentId2 = Guid.NewGuid().ToString("N")
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
