using Dapper;
using MySqlConnector;
using Parking.Central.Data;
using Parking.Contracts;

namespace Parking.Api.Tests;

[Collection("Database")]
[Trait("Category", "DatabaseMutation")]
public sealed class ParkingManagementRepositoryTests
{
    private const long SiteId = 990101;
    private const long GeneralOpenId = 99010101;
    private const long PeriodOpenId = 99010101;
    private readonly string _connectionString =
        Environment.GetEnvironmentVariable("PARKING_RUNTIME_CONNECTION")
        ?? throw new InvalidOperationException("PARKING_RUNTIME_CONNECTION 환경변수가 없습니다.");

    [Fact]
    public async Task 입차조회는_일반과_등록의_I만_합친다()
    {
        await SeedAsync();
        try
        {
            ParkingManagementRepository repository = new(_connectionString);
            PagedParkingResult<ParkingManagementItem> result =
                await repository.SearchEntriesAsync(
                    new ParkingManagementQuery(SiteId, CarNumber: "1234"),
                    CancellationToken.None);

            Assert.Equal(2, result.TotalCount);
            Assert.Contains(result.Items, x => x.SessionType == ParkingSessionType.General);
            Assert.Contains(result.Items, x => x.SessionType == ParkingSessionType.Period);
            Assert.All(result.Items, x => Assert.Equal("I", x.Status));
        }
        finally { await ClearAsync(); }
    }

    [Fact]
    public async Task 출차조회는_일반_XO와_등록_O를_합친다()
    {
        await SeedAsync();
        try
        {
            ParkingManagementRepository repository = new(_connectionString);
            PagedParkingResult<ParkingManagementItem> result =
                await repository.SearchExitsAsync(new ParkingManagementQuery(
                    SiteId,
                    new DateTimeOffset(2026, 9, 20, 0, 0, 0, TimeSpan.FromHours(9)),
                    new DateTimeOffset(2026, 9, 24, 0, 0, 0, TimeSpan.FromHours(9))),
                    CancellationToken.None);

            Assert.Equal(3, result.TotalCount);
            ParkingManagementItem settled = Assert.Single(result.Items, x => x.Status == "X");
            Assert.Equal(201, settled.OutDeviceNumber);
            ParkingManagementItem period = Assert.Single(result.Items,
                x => x.SessionType == ParkingSessionType.Period);
            Assert.Null(period.OriginalFee);
            Assert.Null(period.PaidFee);
        }
        finally { await ClearAsync(); }
    }

    [Fact]
    public async Task 같은xindex라도_세션구분에_따라_I차량번호만_수정한다()
    {
        await SeedAsync();
        try
        {
            ParkingManagementRepository repository = new(_connectionString);
            bool changed = await repository.CorrectCarNumberAsync(
                SiteId, ParkingSessionType.Period, PeriodOpenId, "99가9999",
                CancellationToken.None);

            Assert.True(changed);
            await using MySqlConnection connection = new(_connectionString);
            Assert.Equal("11가1234", await connection.QuerySingleAsync<string>(
                "SELECT carnum FROM tparkinfo WHERE xindex=@Id;", new { Id = GeneralOpenId }));
            Assert.Equal("99가9999", await connection.QuerySingleAsync<string>(
                "SELECT carnum FROM tperiodinout WHERE xindex=@Id;", new { Id = PeriodOpenId }));
            Assert.False(await repository.CorrectCarNumberAsync(
                SiteId, ParkingSessionType.General, 99010102, "88나8888",
                CancellationToken.None));
        }
        finally { await ClearAsync(); }
    }

    private async Task SeedAsync()
    {
        await ClearAsync();
        await using MySqlConnection connection = new(_connectionString);
        await connection.ExecuteAsync("""
            INSERT INTO tparkings(sitenum,groupnum,parkname,parktype,sitekeyhash,useflag)
            VALUES(@SiteId,2,'차량관리시험','TEST',REPEAT('0',64),1);
            INSERT INTO tlaneinfo(laneid,sitenum,groupnum,lanename,direction,useflag) VALUES
            (991010,@SiteId,2,'입차','ENTRY',1),(991020,@SiteId,2,'출차','EXIT',1);
            INSERT INTO tdeviceinfo
            (deviceid,sitenum,groupnum,laneid,devicenum,devicename,devicetype,useflag) VALUES
            (994011,@SiteId,2,991010,401,'입차LPR',3,1),
            (994012,@SiteId,2,991020,402,'출차LPR',3,1),
            (994013,@SiteId,2,991020,201,'출구무인',2,1);
            INSERT INTO tparkinfo
            (xindex,sitenum,groupnum,ineventid,carnum,cartype,inlaneid,indevicenum,indate,inimage,outflag,manual) VALUES
            (@GeneralOpenId,@SiteId,2,'99010100000000000000000000000001','11가1234',1,991010,401,'2026-09-20 10:00:00','in1.jpg','I',0),
            (99010102,@SiteId,2,'99010100000000000000000000000002','22나1234',1,991010,401,'2026-09-20 11:00:00','in2.jpg','X',0),
            (99010103,@SiteId,2,'99010100000000000000000000000003','33다5678',2,991010,401,'2026-09-20 12:00:00','in3.jpg','O',0);
            UPDATE tparkinfo SET paydate='2026-09-22 12:00:00',parkfee=1000,discountfee=100,payfee=900,paidfee=900 WHERE xindex=99010102;
            UPDATE tparkinfo SET outlaneid=991020,outdevicenum=402,outdate='2026-09-23 12:00:00',outimage='out3.jpg',parktime=2880 WHERE xindex=99010103;
            INSERT INTO tbcardinfo
            (paymentid,pindex,sitenum,groupnum,devicenum,carnum,dealtype,credittype,money,dealdate)
            VALUES('99010100000000000000000000000011',99010102,@SiteId,2,201,'22나1234','APPROVE',1,900,'2026-09-22 12:00:00');
            INSERT INTO tperiodmember
            (xindex,sitenum,groupnum,carnum1,cartype1,name,startdate,enddate,parkarea,useflag,outflag) VALUES
            (99010111,@SiteId,2,'44라1234',1,'등록시험','2026-01-01','2026-12-31','0100000',1,'I'),
            (99010112,@SiteId,2,'55마9876',1,'등록출차','2026-01-01','2026-12-31','0100000',1,'O');
            INSERT INTO tperiodinout
            (xindex,periodindex,sitenum,groupnum,ineventid,carnum,cartype,inlaneid,indevicenum,indate,inimage,outflag,manual) VALUES
            (@PeriodOpenId,99010111,@SiteId,2,'99010100000000000000000000000021','44라1234',1,991010,401,'2026-09-21 10:00:00','pin1.jpg','I',0),
            (99010104,99010112,@SiteId,2,'99010100000000000000000000000022','55마9876',1,991010,401,'2026-09-21 11:00:00','pin2.jpg','O',0);
            UPDATE tperiodinout SET outlaneid=991020,outdevicenum=402,outdate='2026-09-23 13:00:00',outimage='pout2.jpg',parktime=3000 WHERE xindex=99010104;
            """, new { SiteId, GeneralOpenId, PeriodOpenId });
    }

    private async Task ClearAsync()
    {
        await using MySqlConnection connection = new(_connectionString);
        await connection.ExecuteAsync("""
            DELETE FROM tbcardinfo WHERE sitenum=@SiteId;
            DELETE FROM tperiodinout WHERE sitenum=@SiteId;
            DELETE FROM tperiodmember WHERE sitenum=@SiteId;
            DELETE FROM tdiscountinfo WHERE sitenum=@SiteId;
            DELETE FROM tparkinfo WHERE sitenum=@SiteId;
            DELETE FROM tdeviceinfo WHERE sitenum=@SiteId;
            DELETE FROM tlaneinfo WHERE sitenum=@SiteId;
            DELETE FROM tparkings WHERE sitenum=@SiteId;
            """, new { SiteId });
    }
}
