using System.Net;
using System.Text;
using Dapper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using MySqlConnector;
using Newtonsoft.Json;
using Parking.Contracts;

namespace Parking.Api.Tests;

[Collection("Database")]
public sealed class ExitEndpointTests
{
    private static string ConnectionString =>
        Environment.GetEnvironmentVariable("PARKING_TEST_CONNECTION")
        ?? throw new InvalidOperationException("PARKING_TEST_CONNECTION 환경변수가 없습니다.");

    [Fact]
    public async Task 사전정산_유예시간_이내이면_출차를_허용한다()
    {
        (string carNumber, DateTime paydate) = await CreatePaidSessionAsync();

        FieldEventResponse result = await RequestExitAsync(
            carNumber,
            paydate.AddMinutes(5));

        Assert.True(result.Accepted);
        Assert.True(result.OpenBarrier);
        Assert.Equal("EXIT_ACCEPTED", result.ResultCode);
    }

    [Fact]
    public async Task 사전정산_유예시간_초과후_추가요금이_있으면_출차를_거부한다()
    {
        (string carNumber, DateTime paydate) = await CreatePaidSessionAsync();

        FieldEventResponse result = await RequestExitAsync(
            carNumber,
            paydate.AddMinutes(11));

        Assert.False(result.Accepted);
        Assert.False(result.OpenBarrier);
        Assert.Equal("PAYMENT_REQUIRED", result.ResultCode);
    }

    private static async Task<(string CarNumber, DateTime Paydate)> CreatePaidSessionAsync()
    {
        string carNumber = $"TEST{Guid.NewGuid():N}"[..20];
        DateTime paydate = DateTime.UtcNow;

        await using MySqlConnection connection = new(ConnectionString);
        long parkingSessionId = await connection.ExecuteScalarAsync<long>("""
            INSERT INTO parking_session
            (sitenum,ineventid,carnum,groupnum,cartype,inlaneid,indate,paydate,status)
            VALUES (1,@EventId,@CarNumber,1,1,10,@EntryAt,@Paydate,'Paid');
            SELECT LAST_INSERT_ID();
            """, new
        {
            EventId = Guid.NewGuid().ToByteArray(),
            CarNumber = carNumber,
            EntryAt = paydate.AddMinutes(-120),
            Paydate = paydate
        });

        await connection.ExecuteAsync("""
            INSERT INTO payment
            (paymentid,parkindex,sitenum,originalfee,discountfee,payamount,
             paymethod,approvalnum,paydate)
            VALUES
            (@PaymentId,@ParkingSessionId,1,2400,0,2400,'Card','EXIT',@Paydate);
            """, new
        {
            PaymentId = Guid.NewGuid().ToByteArray(),
            ParkingSessionId = parkingSessionId,
            Paydate = paydate
        });

        return (carNumber, paydate);
    }

    private static async Task<FieldEventResponse> RequestExitAsync(
        string carNumber,
        DateTime occurredAt)
    {
        await using TestApplication factory = new();
        HttpClient client = factory.CreateClient();
        var request = new
        {
            EventId = Guid.NewGuid(),
            SiteId = 1,
            LaneId = 20,
            DeviceId = 201,
            CarNumber = carNumber,
            OccurredAt = new DateTimeOffset(occurredAt, TimeSpan.Zero)
        };

        HttpResponseMessage response = await client.PostAsync(
            "/api/v1/parking/exits",
            new StringContent(
                JsonConvert.SerializeObject(request),
                Encoding.UTF8,
                "application/json"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return JsonConvert.DeserializeObject<FieldEventResponse>(
            await response.Content.ReadAsStringAsync())
            ?? throw new InvalidOperationException("출차 응답이 없습니다.");
    }

    private sealed class TestApplication : WebApplicationFactory<global::Parking.Api.Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("ConnectionStrings:ParkingDatabase", ConnectionString);
        }
    }
}
