using System.Net;
using System.Text;
using Dapper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using MySqlConnector;
using Newtonsoft.Json;
using Parking.Api.Features.Fees;

namespace Parking.Api.Tests;

[Collection("Database")]
public sealed class FeeQuoteEndpointTests
{
    private static string ConnectionString =>
        Environment.GetEnvironmentVariable("PARKING_TEST_CONNECTION")
        ?? throw new InvalidOperationException("PARKING_TEST_CONNECTION 환경변수가 없습니다.");

    [Fact]
    public async Task 차량번호_정산조회는_세션할인_기결제_유예시간을_자동적용한다()
    {
        string carNumber = $"TEST{Guid.NewGuid():N}"[..20];
        DateTime paydate = DateTime.UtcNow;
        long parkingSessionId;

        await using (MySqlConnection connection = new(ConnectionString))
        {
            await connection.ExecuteAsync("""
                INSERT INTO tdiscount(sitenum,groupnum,`key`,type,value)
                VALUES (1,1,10,4,50)
                ON DUPLICATE KEY UPDATE type=VALUES(type),value=VALUES(value);
                """);

            parkingSessionId = await connection.ExecuteScalarAsync<long>("""
                INSERT INTO parking_session
                (sitenum,ineventid,carnum,groupnum,cartype,inlaneid,indate,paydate,outflag)
                VALUES (1,@EventId,@CarNumber,1,1,10,@EntryAt,@Paydate,'X');
                SELECT LAST_INSERT_ID();
                """, new
            {
                EventId = Guid.NewGuid().ToByteArray(),
                CarNumber = carNumber,
                EntryAt = paydate.AddHours(-2),
                Paydate = paydate
            });

            await connection.ExecuteAsync("""
                INSERT INTO parking_session_discount
                (parkindex,carnum,discountkey,source,sourceref,discounttype,
                 discountvalue,sdate,applydate)
                VALUES
                (@ParkingSessionId,@CarNumber,10,'Test','QUOTE',4,50,@Paydate,@Paydate);

                INSERT INTO payment
                (paymentid,parkindex,sitenum,originalfee,discountfee,payamount,
                 paymethod,approvalnum,paydate)
                VALUES
                (@PaymentId,@ParkingSessionId,1,1000,500,500,'Card','QUOTE',@Paydate);
                """, new
            {
                ParkingSessionId = parkingSessionId,
                CarNumber = carNumber,
                Paydate = paydate,
                PaymentId = Guid.NewGuid().ToByteArray()
            });
        }

        await using TestApplication factory = new();
        HttpClient client = factory.CreateClient();
        var request = new
        {
            Sitenum = 1,
            CarNumber = carNumber,
            ExitAt = new DateTimeOffset(paydate, TimeSpan.Zero).AddMinutes(5)
        };

        HttpResponseMessage response = await client.PostAsync(
            "/api/v1/fees/quote",
            new StringContent(
                JsonConvert.SerializeObject(request),
                Encoding.UTF8,
                "application/json"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        QuoteParkingFeeResponse? result = JsonConvert.DeserializeObject<QuoteParkingFeeResponse>(
            await response.Content.ReadAsStringAsync());
        Assert.NotNull(result);
        Assert.Equal(parkingSessionId, result.ParkingSessionId);
        Assert.Equal(500, result.PreviousPaidAmount);
        Assert.Equal(0, result.PayableAmount);
        Assert.True(result.IsPrepayGrace);
    }

    [Fact]
    public async Task 선택세션_0원견적은_정산완료_X로_변경한다()
    {
        DateTime exitAt = DateTime.UtcNow;
        long parkingSessionId;
        await using (MySqlConnection connection = new(ConnectionString))
        {
            parkingSessionId = await connection.ExecuteScalarAsync<long>("""
                INSERT INTO parking_session
                (sitenum,ineventid,carnum,groupnum,cartype,inlaneid,indate,outflag)
                VALUES (1,@EventId,@CarNumber,99,1,10,@EntryAt,'I');
                SELECT LAST_INSERT_ID();
                """, new
            {
                EventId = Guid.NewGuid().ToByteArray(),
                CarNumber = $"FREE{Guid.NewGuid():N}"[..20],
                EntryAt = exitAt.AddMinutes(-10)
            });
        }

        await using TestApplication factory = new();
        HttpClient client = factory.CreateClient();
        HttpResponseMessage response = await client.PostAsync(
            "/api/v1/fees/quote/session",
            new StringContent(
                JsonConvert.SerializeObject(new
                {
                    ParkingSessionId = parkingSessionId,
                    ExitAt = new DateTimeOffset(exitAt, TimeSpan.Zero)
                }),
                Encoding.UTF8,
                "application/json"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        QuoteParkingFeeResponse? result = JsonConvert.DeserializeObject<QuoteParkingFeeResponse>(
            await response.Content.ReadAsStringAsync());
        Assert.NotNull(result);
        Assert.Equal(parkingSessionId, result.ParkingSessionId);
        Assert.Equal(0, result.PayableAmount);

        await using MySqlConnection verifyConnection = new(ConnectionString);
        Assert.Equal("X", await verifyConnection.ExecuteScalarAsync<string>(
            "SELECT outflag FROM parking_session WHERE xindex=@ParkingSessionId;",
            new { ParkingSessionId = parkingSessionId }));
    }

    private sealed class TestApplication : WebApplicationFactory<global::Parking.Api.Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("ConnectionStrings:ParkingDatabase", ConnectionString);
        }
    }
}
