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
[Trait("Category", "DatabaseMutation")]
public sealed class FeeQuoteEndpointTests
{
    private static string ConnectionString =>
        Environment.GetEnvironmentVariable("PARKING_RUNTIME_CONNECTION")
        ?? throw new InvalidOperationException("PARKING_RUNTIME_CONNECTION 환경변수가 없습니다.");

    [Fact]
    public async Task 차량번호_정산조회는_세션할인_기결제_유예시간을_자동적용한다()
    {
        string carNumber = $"TEST{Guid.NewGuid():N}"[..20];
        DateTime paydate = DateTime.UtcNow;
        long parkingSessionId;

        await using (MySqlConnection connection = new(ConnectionString))
        {
            await connection.ExecuteAsync("""
                INSERT INTO tparkvariable(sitenum,groupnum,cmdtype,val,opt,msg)
                VALUES (9001,2,'CMD_PREPAY_GRACE','0','10',NULL)
                ON DUPLICATE KEY UPDATE val=VALUES(val),opt=VALUES(opt),msg=VALUES(msg);

                INSERT INTO tdiscount(sitenum,groupnum,diskey,distype,disvalue,title)
                VALUES (9001,2,10,4,50,'50원 고정')
                ON DUPLICATE KEY UPDATE distype=VALUES(distype),disvalue=VALUES(disvalue);
                """);

            parkingSessionId = await connection.ExecuteScalarAsync<long>("""
                INSERT INTO tparkinfo
                (sitenum,ineventid,carnum,groupnum,cartype,inlaneid,indevicenum,indate,paydate,outflag)
                VALUES (9001,@EventId,@CarNumber,2,1,9010,401,@EntryAt,@Paydate,'X');
                SELECT LAST_INSERT_ID();
                """, new
            {
                EventId = Guid.NewGuid().ToString("N"),
                CarNumber = carNumber,
                EntryAt = Parking.Central.Data.ParkingLocalTime.ToDatabase(
                    new DateTimeOffset(paydate.AddHours(-2), TimeSpan.Zero)),
                Paydate = Parking.Central.Data.ParkingLocalTime.ToDatabase(
                    new DateTimeOffset(paydate, TimeSpan.Zero))
            });

            await connection.ExecuteAsync("""
                INSERT INTO tdiscountinfo
                (discountid,pindex,sitenum,groupnum,carnum,diskey,distype,
                 disvalue,source,sourceref,indate,disdate)
                VALUES
                (@DiscountId,@ParkingSessionId,9001,2,@CarNumber,10,4,50,'Test','QUOTE',@Paydate,@Paydate);

                INSERT INTO tbcardinfo
                (paymentid,pindex,sitenum,groupnum,devicenum,dealtype,money,
                 acceptnum,dealdate)
                VALUES
                (@PaymentId,@ParkingSessionId,9001,2,0,'APPROVE',500,'QUOTE',@Paydate);
                """, new
            {
                ParkingSessionId = parkingSessionId,
                CarNumber = carNumber,
                Paydate = Parking.Central.Data.ParkingLocalTime.ToDatabase(
                    new DateTimeOffset(paydate, TimeSpan.Zero)),
                DiscountId = Guid.NewGuid().ToString("N"),
                PaymentId = Guid.NewGuid().ToString("N")
            });
        }

        await using TestApplication factory = new();
        HttpClient client = factory.CreateClient();
        var request = new
        {
            Sitenum = 9001,
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
        exitAt = exitAt.AddTicks(-(exitAt.Ticks % TimeSpan.TicksPerSecond));
        long parkingSessionId;
        await using (MySqlConnection connection = new(ConnectionString))
        {
            parkingSessionId = await connection.ExecuteScalarAsync<long>("""
                INSERT INTO tparkinfo
                (sitenum,ineventid,carnum,groupnum,cartype,inlaneid,indevicenum,indate,outflag)
                VALUES (9001,@EventId,@CarNumber,99,1,9010,401,@EntryAt,'I');
                SELECT LAST_INSERT_ID();
                """, new
            {
                EventId = Guid.NewGuid().ToString("N"),
                CarNumber = $"FREE{Guid.NewGuid():N}"[..20],
                EntryAt = Parking.Central.Data.ParkingLocalTime.ToDatabase(
                    new DateTimeOffset(exitAt.AddMinutes(-10), TimeSpan.Zero))
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
        FeeStorageRow stored = await verifyConnection.QuerySingleAsync<FeeStorageRow>("""
            SELECT outflag OutFlag,parktime ParkTime,parkfee ParkFee,
                   discountfee DiscountFee,payfee PayFee
            FROM tparkinfo WHERE xindex=@ParkingSessionId;
            """, new { ParkingSessionId = parkingSessionId });
        Assert.Equal("X", stored.OutFlag);
        Assert.Equal(10, stored.ParkTime);
        Assert.Equal(0, stored.ParkFee);
        Assert.Equal(0, stored.DiscountFee);
        Assert.Equal(0, stored.PayFee);
    }

    private sealed class TestApplication : WebApplicationFactory<global::Parking.Api.Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("ConnectionStrings:ParkingDatabase", ConnectionString);
        }
    }

    private sealed class FeeStorageRow
    {
        public string OutFlag { get; set; } = "";
        public int ParkTime { get; set; }
        public long ParkFee { get; set; }
        public long DiscountFee { get; set; }
        public long PayFee { get; set; }
    }
}
