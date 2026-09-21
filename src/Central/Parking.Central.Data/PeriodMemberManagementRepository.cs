using Dapper;
using MySqlConnector;
using Parking.Contracts;

namespace Parking.Central.Data;

public sealed class PeriodMemberManagementRepository : IPeriodMemberManagementRepository
{
    private const string SelectColumns = """
        xindex MemberId, sitenum SiteId, groupnum Groupnum, devicenum DeviceId,
        cardid CardId, serialno SerialNo, periodtype PeriodType, name Name,
        telnum TelNumber, groupcode GroupCode, company1 Company1, company2 Company2,
        carnum1 CarNumber1, cartype1 CarType1, carnum2 CarNumber2, cartype2 CarType2,
        address Address, parktype ParkType, recorddate RecordDate, startdate StartDate,
        enddate EndDate, parktimecode ParkTimeCode, parktimetime ParkTimeTime,
        parkprice ParkPrice, parkarea ParkArea, parklevel ParkLevel,
        parkvalidday ParkValidDay, antiflag AntiFlag, useflag UseFlag,
        serviceday ServiceDay, managercode ManagerCode, managername ManagerName,
        paytype PayType, outflag OutFlag, intimetick InTimeTick,
        reserved1 Reserved1, reserved2 Reserved2, reserved3 Reserved3,
        reserved4 Reserved4, note Note
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
        IEnumerable<PeriodMemberDetail> rows = await connection.QueryAsync<PeriodMemberDetail>(
            new CommandDefinition($"""
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
                    CarNumber = string.IsNullOrWhiteSpace(carNumber) ? null : carNumber.Trim()
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
            (sitenum,groupnum,devicenum,cardid,serialno,periodtype,name,telnum,
             groupcode,company1,company2,carnum1,cartype1,carnum2,cartype2,address,
             parktype,recorddate,startdate,enddate,parktimecode,parktimetime,parkprice,
             parkarea,parklevel,parkvalidday,antiflag,useflag,serviceday,managercode,
             managername,paytype,outflag,intimetick,reserved1,reserved2,reserved3,
             reserved4,note)
            VALUES
            (@SiteId,@Groupnum,@DeviceId,@CardId,@SerialNo,@PeriodType,@Name,@TelNumber,
             @GroupCode,@Company1,@Company2,@CarNumber1,@CarType1,@CarNumber2,@CarType2,
             @Address,@ParkType,@RecordDate,@StartDate,@EndDate,@ParkTimeCode,@ParkTimeTime,
             @ParkPrice,@ParkArea,@ParkLevel,@ParkValidDay,@AntiFlag,@UseFlag,@ServiceDay,
             @ManagerCode,@ManagerName,@PayType,@OutFlag,@InTimeTick,@Reserved1,@Reserved2,
             @Reserved3,@Reserved4,@Note);
            SELECT LAST_INSERT_ID();
            """, Normalize(request), cancellationToken: cancellationToken));
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
                sitenum=@SiteId, groupnum=@Groupnum, devicenum=@DeviceId,
                cardid=@CardId, serialno=@SerialNo, periodtype=@PeriodType, name=@Name,
                telnum=@TelNumber, groupcode=@GroupCode, company1=@Company1,
                company2=@Company2, carnum1=@CarNumber1, cartype1=@CarType1,
                carnum2=@CarNumber2, cartype2=@CarType2, address=@Address,
                parktype=@ParkType, recorddate=@RecordDate, startdate=@StartDate,
                enddate=@EndDate, parktimecode=@ParkTimeCode, parktimetime=@ParkTimeTime,
                parkprice=@ParkPrice, parkarea=@ParkArea, parklevel=@ParkLevel,
                parkvalidday=@ParkValidDay, antiflag=@AntiFlag, useflag=@UseFlag,
                serviceday=@ServiceDay, managercode=@ManagerCode,
                managername=@ManagerName, paytype=@PayType, outflag=@OutFlag,
                intimetick=@InTimeTick, reserved1=@Reserved1, reserved2=@Reserved2,
                reserved3=@Reserved3, reserved4=@Reserved4, note=@Note
            WHERE xindex=@MemberId;
            """, parameters,
            cancellationToken: cancellationToken));
        return affected > 0;
    }

    public async Task<bool> DeleteAsync(long memberId, CancellationToken cancellationToken)
    {
        await using MySqlConnection connection = new(_connectionString);
        int affected = await connection.ExecuteAsync(new CommandDefinition(
            "DELETE FROM tperiodmember WHERE xindex=@MemberId;",
            new { MemberId = memberId },
            cancellationToken: cancellationToken));
        return affected > 0;
    }

    private static object Normalize(PeriodMemberSaveRequest request) => new
    {
        request.SiteId,
        request.Groupnum,
        request.DeviceId,
        request.CardId,
        request.SerialNo,
        request.PeriodType,
        request.Name,
        request.TelNumber,
        request.GroupCode,
        Company1 = request.Company1.Trim(),
        Company2 = request.Company2.Trim(),
        CarNumber1 = request.CarNumber1.Trim(),
        CarType1 = request.CarType1.Trim(),
        CarNumber2 = request.CarNumber2.Trim(),
        CarType2 = request.CarType2.Trim(),
        Address = request.Address.Trim(),
        request.ParkType,
        request.RecordDate,
        request.StartDate,
        request.EndDate,
        request.ParkTimeCode,
        request.ParkTimeTime,
        request.ParkPrice,
        request.ParkArea,
        request.ParkLevel,
        request.ParkValidDay,
        request.AntiFlag,
        request.UseFlag,
        request.ServiceDay,
        request.ManagerCode,
        request.ManagerName,
        request.PayType,
        OutFlag = string.IsNullOrWhiteSpace(request.OutFlag) ? "I" : request.OutFlag.Trim(),
        request.InTimeTick,
        request.Reserved1,
        request.Reserved2,
        request.Reserved3,
        request.Reserved4,
        request.Note
    };
}
