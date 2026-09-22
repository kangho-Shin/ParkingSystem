using Dapper;
using MySqlConnector;
using Parking.Contracts;

namespace Parking.Central.Data;

public sealed class PeriodMemberManagementRepository : IPeriodMemberManagementRepository
{
    private const string SelectColumns = """
        xindex MemberId,sitenum SiteId,groupnum Groupnum,
        devicenum DeviceNumber,cardno CardNumber,serialno SerialNo,
        periodtype PeriodType,groupcode GroupCode,carnum1 CarNumber1,
        cartype1 CarType1,carnum2 CarNumber2,cartype2 CarType2,name Name,
        tel Telephone,addr Address,companycode CompanyCode,deptcode DepartmentCode,
        startdate StartDate,enddate EndDate,serviceday ServiceDay,
        parktype ParkType,parktimecode ParkTimeCode,parkarea ParkArea,
        parkvalidday ParkValidDay,parklevel ParkLevel,parkprice ParkPrice,
        diskey DiscountKey,paytype PayType,useflag UseFlag,outflag OutFlag,
        mid ManagerId,mname ManagerName
        """;

    private readonly string _connectionString;

    public PeriodMemberManagementRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<IReadOnlyList<PeriodMemberDetail>> SearchAsync(
        long siteId,
        int? groupnum,
        string? carNumber,
        CancellationToken cancellationToken)
    {
        await using MySqlConnection connection = new(_connectionString);
        IEnumerable<PeriodMemberDetail> rows =
            await connection.QueryAsync<PeriodMemberDetail>(new CommandDefinition($"""
                SELECT {SelectColumns}
                FROM tperiodmember
                WHERE sitenum=@SiteId
                  AND (@Groupnum IS NULL OR groupnum=@Groupnum)
                  AND (@CarNumber IS NULL OR carnum1 LIKE CONCAT('%',@CarNumber,'%')
                       OR carnum2 LIKE CONCAT('%',@CarNumber,'%'))
                ORDER BY xindex DESC;
                """,
                new
                {
                    SiteId = siteId,
                    Groupnum = groupnum,
                    CarNumber = string.IsNullOrWhiteSpace(carNumber)
                        ? null
                        : carNumber.Trim()
                },
                cancellationToken: cancellationToken));
        return rows.AsList();
    }

    public async Task<PeriodMemberDetail?> GetAsync(
        long memberId,
        CancellationToken cancellationToken)
    {
        await using MySqlConnection connection = new(_connectionString);
        return await connection.QueryFirstOrDefaultAsync<PeriodMemberDetail>(
            new CommandDefinition($"""
                SELECT {SelectColumns}
                FROM tperiodmember
                WHERE xindex=@MemberId;
                """,
                new { MemberId = memberId },
                cancellationToken: cancellationToken));
    }

    public async Task<long> CreateAsync(
        PeriodMemberSaveRequest request,
        CancellationToken cancellationToken)
    {
        await using MySqlConnection connection = new(_connectionString);
        return await connection.ExecuteScalarAsync<long>(new CommandDefinition("""
            INSERT INTO tperiodmember
            (sitenum,groupnum,devicenum,cardno,serialno,periodtype,groupcode,
             carnum1,cartype1,carnum2,cartype2,name,tel,addr,companycode,deptcode,
             startdate,enddate,serviceday,parktype,parktimecode,parkarea,
             parkvalidday,parklevel,parkprice,diskey,paytype,useflag,outflag,mid,mname)
            VALUES
            (@SiteId,@Groupnum,@DeviceNumber,@CardNumber,@SerialNo,@PeriodType,@GroupCode,
             @CarNumber1,@CarType1,@CarNumber2,@CarType2,@Name,@Telephone,@Address,
             @CompanyCode,@DepartmentCode,@StartDate,@EndDate,@ServiceDay,@ParkType,
             @ParkTimeCode,@ParkArea,@ParkValidDay,@ParkLevel,@ParkPrice,@DiscountKey,
             @PayType,@UseFlag,@OutFlag,@ManagerId,@ManagerName);
            SELECT LAST_INSERT_ID();
            """,
            Normalize(request),
            cancellationToken: cancellationToken));
    }

    public async Task<bool> UpdateAsync(
        long memberId,
        PeriodMemberSaveRequest request,
        CancellationToken cancellationToken)
    {
        await using MySqlConnection connection = new(_connectionString);
        DynamicParameters parameters = new(Normalize(request));
        parameters.Add("MemberId", memberId);
        int affected = await connection.ExecuteAsync(new CommandDefinition("""
            UPDATE tperiodmember SET
                sitenum=@SiteId,groupnum=@Groupnum,devicenum=@DeviceNumber,
                cardno=@CardNumber,serialno=@SerialNo,periodtype=@PeriodType,
                groupcode=@GroupCode,carnum1=@CarNumber1,cartype1=@CarType1,
                carnum2=@CarNumber2,cartype2=@CarType2,name=@Name,tel=@Telephone,
                addr=@Address,companycode=@CompanyCode,deptcode=@DepartmentCode,
                startdate=@StartDate,enddate=@EndDate,serviceday=@ServiceDay,
                parktype=@ParkType,parktimecode=@ParkTimeCode,parkarea=@ParkArea,
                parkvalidday=@ParkValidDay,parklevel=@ParkLevel,parkprice=@ParkPrice,
                diskey=@DiscountKey,paytype=@PayType,useflag=@UseFlag,
                outflag=@OutFlag,mid=@ManagerId,mname=@ManagerName
            WHERE xindex=@MemberId;
            """,
            parameters,
            cancellationToken: cancellationToken));
        return affected > 0;
    }

    public async Task<bool> DeleteAsync(long memberId, CancellationToken cancellationToken)
    {
        await using MySqlConnection connection = new(_connectionString);
        int exists = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            "SELECT COUNT(*) FROM tperiodmember WHERE xindex=@MemberId;",
            new { MemberId = memberId },
            cancellationToken: cancellationToken));
        if (exists == 0)
            return false;
        await connection.ExecuteAsync(new CommandDefinition(
            "UPDATE tperiodmember SET useflag=0 WHERE xindex=@MemberId;",
            new { MemberId = memberId },
            cancellationToken: cancellationToken));
        return true;
    }

    private static object Normalize(PeriodMemberSaveRequest request) => new
    {
        request.SiteId,
        request.Groupnum,
        request.DeviceNumber,
        request.CardNumber,
        SerialNo = EmptyToNull(request.SerialNo),
        request.PeriodType,
        request.GroupCode,
        CarNumber1 = request.CarNumber1.Trim(),
        request.CarType1,
        CarNumber2 = EmptyToNull(request.CarNumber2),
        request.CarType2,
        Name = request.Name.Trim(),
        Telephone = EmptyToNull(request.Telephone),
        Address = EmptyToNull(request.Address),
        request.CompanyCode,
        request.DepartmentCode,
        request.StartDate,
        request.EndDate,
        request.ServiceDay,
        request.ParkType,
        request.ParkTimeCode,
        ParkArea = request.ParkArea.Trim(),
        ParkValidDay = request.ParkValidDay.Trim(),
        request.ParkLevel,
        request.ParkPrice,
        request.DiscountKey,
        request.PayType,
        request.UseFlag,
        OutFlag = string.IsNullOrWhiteSpace(request.OutFlag) ? "O" : request.OutFlag.Trim(),
        ManagerId = EmptyToNull(request.ManagerId),
        ManagerName = EmptyToNull(request.ManagerName)
    };

    private static string? EmptyToNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
