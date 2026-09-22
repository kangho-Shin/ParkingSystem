using System.Net;
using System.Text;
using Dapper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using MySqlConnector;
using Newtonsoft.Json;
using Parking.Central.Data;
using Parking.Contracts;

namespace Parking.Api.Tests;

[Collection("Database")]
[Trait("Category", "DatabaseMutation")]
public sealed class ExitEndpointTests
{
    private const int TestGroupnum = 98;
    private const long TestExitLaneId = 98020;
    private const long TestExitDeviceId = 984002;

    private static string ConnectionString =>
        Environment.GetEnvironmentVariable("PARKING_RUNTIME_CONNECTION")
        ?? throw new InvalidOperationException("PARKING_RUNTIME_CONNECTION 환경변수가 없습니다.");

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

    [Fact]
    public async Task 입차_EventType으로_출차요청하면_거부한다()
    {
        await using TestApplication factory = new();
        HttpClient client = factory.CreateClient();
        ExitEventRequest request = new(
            Guid.NewGuid(), 9001, 9020, 4002, "12가3456", DateTimeOffset.UtcNow,
            2, 1, null, ParkingEventType.Entry);

        HttpResponseMessage response = await client.PostAsync(
            "/api/v1/parking/exits",
            new StringContent(
                JsonConvert.SerializeObject(request),
                Encoding.UTF8,
                "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static async Task<(string CarNumber, DateTime Paydate)> CreatePaidSessionAsync()
    {
        string carNumber = $"TEST{Guid.NewGuid():N}"[..20];
        DateTime paydate = DateTime.UtcNow;

        await using MySqlConnection connection = new(ConnectionString);
        await connection.ExecuteAsync("""
            INSERT INTO tparkings
            (sitenum,groupnum,parkname,parktype,sitekeyhash,useflag)
            VALUES(9001,@Groupnum,'출차시험','TEST',REPEAT('0',64),1)
            ON DUPLICATE KEY UPDATE useflag=1;

            INSERT INTO tlaneinfo
            (laneid,sitenum,groupnum,lanename,direction,useflag)
            VALUES(@LaneId,9001,@Groupnum,'출차시험차로','EXIT',1)
            ON DUPLICATE KEY UPDATE direction='EXIT',useflag=1;

            INSERT INTO tdeviceinfo
            (deviceid,sitenum,groupnum,laneid,devicenum,devicename,devicetype,useflag)
            VALUES(@DeviceId,9001,@Groupnum,@LaneId,402,'출차시험LPR',3,1)
            ON DUPLICATE KEY UPDATE groupnum=@Groupnum,laneid=@LaneId,useflag=1;

            INSERT INTO tparkingnum(sitenum,groupnum)
            VALUES(9001,@Groupnum)
            ON DUPLICATE KEY UPDATE groupnum=VALUES(groupnum);

            INSERT INTO tparkfee
            (sitenum,groupnum,weektype,dayshift,cartype,feestep,parktime,parkfee,maxcount)
            VALUES
            (9001,@Groupnum,1,0,1,1,10,100,0),
            (9001,@Groupnum,2,0,1,1,10,100,0)
            ON DUPLICATE KEY UPDATE
            parktime=VALUES(parktime),parkfee=VALUES(parkfee),maxcount=VALUES(maxcount);

            INSERT INTO tparkvariable(sitenum,groupnum,cmdtype,val,opt,msg)
            VALUES (9001,@Groupnum,'CMD_PREPAY_GRACE','0','10',NULL)
            ON DUPLICATE KEY UPDATE val=VALUES(val),opt=VALUES(opt),msg=VALUES(msg);
            """, new
            {
                Groupnum = TestGroupnum,
                LaneId = TestExitLaneId,
                DeviceId = TestExitDeviceId
            });
        long parkingSessionId = await connection.ExecuteScalarAsync<long>("""
            INSERT INTO tparkinfo
            (sitenum,ineventid,carnum,groupnum,cartype,inlaneid,indevicenum,indate,paydate,outflag)
            VALUES (9001,@EventId,@CarNumber,@Groupnum,1,9010,401,@EntryAt,@Paydate,'X');
            SELECT LAST_INSERT_ID();
            """, new
        {
            EventId = Guid.NewGuid().ToString("N"),
            CarNumber = carNumber,
            Groupnum = TestGroupnum,
            EntryAt = ParkingLocalTime.ToDatabase(
                new DateTimeOffset(paydate.AddMinutes(-120), TimeSpan.Zero)),
            Paydate = ParkingLocalTime.ToDatabase(
                new DateTimeOffset(paydate, TimeSpan.Zero))
        });

        await connection.ExecuteAsync("""
            INSERT INTO tbcardinfo
            (paymentid,pindex,sitenum,groupnum,devicenum,dealtype,money,
             acceptnum,dealdate)
            VALUES
            (@PaymentId,@ParkingSessionId,9001,@Groupnum,0,'APPROVE',1,'EXIT',@Paydate);
            """, new
        {
            PaymentId = Guid.NewGuid().ToString("N"),
            ParkingSessionId = parkingSessionId,
            Groupnum = TestGroupnum,
            Paydate = ParkingLocalTime.ToDatabase(
                new DateTimeOffset(paydate, TimeSpan.Zero))
        });

        return (carNumber, paydate);
    }

    private static async Task<FieldEventResponse> RequestExitAsync(
        string carNumber,
        DateTime outDateTime)
    {
        await using TestApplication factory = new();
        HttpClient client = factory.CreateClient();
        var request = new
        {
            EventId = Guid.NewGuid(),
            SiteId = 9001,
            LaneId = TestExitLaneId,
            DeviceId = TestExitDeviceId,
            CarNumber = carNumber,
            OutDateTime = new DateTimeOffset(outDateTime, TimeSpan.Zero),
            Groupnum = TestGroupnum,
            EventType = ParkingEventType.Exit
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
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IParkingLaneDirectionValidator>(
                    new AllowLaneDirectionValidator());
                services.AddSingleton<IPeriodVehicleRepository>(
                    new NoPeriodVehicleRepository());
            });
        }
    }

    private sealed class AllowLaneDirectionValidator : IParkingLaneDirectionValidator
    {
        public Task<bool> IsValidAsync(
            long siteId, int groupnum, long laneId, long deviceId,
            string eventType, CancellationToken cancellationToken) =>
            Task.FromResult(true);
    }

    private sealed class NoPeriodVehicleRepository : IPeriodVehicleRepository
    {
        public Task<PeriodMember?> FindMemberAsync(long siteId, int groupnum, string carNumber, DateTimeOffset at, CancellationToken cancellationToken) => Task.FromResult<PeriodMember?>(null);
        public Task<FieldEventResponse> SaveEntryAsync(FieldEventRequest request, PeriodMember member, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<OpenPeriodSession?> FindOpenAsync(long siteId, int groupnum, string carNumber, CancellationToken cancellationToken) => Task.FromResult<OpenPeriodSession?>(null);
        public Task<FieldEventResponse> SaveExitAsync(ExitEventRequest request, OpenPeriodSession session, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
