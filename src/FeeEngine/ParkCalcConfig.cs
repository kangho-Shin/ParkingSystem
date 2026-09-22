using System.Data;
using Dapper;

namespace Parking.FeeEngine;

public sealed class ParkCalcConfig
{
    public async Task<ParkingFeeConfiguration> LoadAsync(
        IDbConnection db,
        int sitenum,
        int groupnum,
        CancellationToken cancellationToken = default)
    {
        var parameter = new { Sitenum = sitenum, Groupnum = groupnum };

        List<Tparkfee> feeRules = (await db.QueryAsync<Tparkfee>(
            new CommandDefinition("""
                SELECT * FROM tparkfee
                WHERE sitenum=@Sitenum AND groupnum=@Groupnum AND useflag=1
                ORDER BY weektype,dayshift,cartype,feestep;
                """, parameter, cancellationToken: cancellationToken))).AsList();

        List<Tdiscount> discounts = (await db.QueryAsync<Tdiscount>(
            new CommandDefinition("""
                SELECT sitenum,groupnum,diskey `Key`,distype `Type`,disvalue `Value`
                FROM tdiscount
                WHERE sitenum=@Sitenum AND groupnum=@Groupnum AND useflag=1;
                """, parameter, cancellationToken: cancellationToken))).AsList();

        List<Tholiday> holidays = (await db.QueryAsync<Tholiday>(
            new CommandDefinition("""
                SELECT sitenum,groupnum,holiday Hdate
                FROM tholiday
                WHERE sitenum=@Sitenum AND groupnum=@Groupnum AND useflag=1;
                """, parameter, cancellationToken: cancellationToken))).AsList();

        List<Tparkvariable> variables = (await db.QueryAsync<Tparkvariable>(
            new CommandDefinition("""
                SELECT sitenum,groupnum,cmdtype,val,opt,msg
                FROM tparkvariable
                WHERE sitenum=@Sitenum AND groupnum=@Groupnum AND useflag=1;
                """, parameter, cancellationToken: cancellationToken))).AsList();

        return new ParkingFeeConfiguration
        {
            FeeRules = feeRules.Select(x => new ParkingFeeRule
            {
                WeekType = x.Weektype,
                CarType = x.Cartype,
                FeeStep = x.Feestep,
                UnitMinutes = x.Parktime ?? 0,
                FeePerUnit = x.Parkfee ?? 0,
                MaxCount = x.Maxcount ?? 0
            }).ToList(),
            Discounts = discounts.Select(x => new ParkingDiscount
            {
                Key = x.Key,
                Type = (DiscountType)x.Type,
                Value = x.Value ?? 0
            }).ToList(),
            Holidays = holidays.Select(x => x.Hdate.Date).ToList(),
            DayTimeRanges = CreateDayTimeRanges(variables),
            MaxDailyFee = GetInt(variables, "CMD_MAXDAILY_FEE", true),
            GraceTime = GetInt(variables, "CMD_GRACE_TIME", true),
            PrepayGraceTime = GetInt(variables, "CMD_PREPAY_GRACE", true),
            ServiceTime = GetInt(variables, "CMD_SERVICE_TIME", true),
            TimeDiscountApplyType = 1,
            ExcludeWeekend = GetInt(variables, "CMD_WEEKENDUSE", false) == 1,
            ExcludeHoliday = GetInt(variables, "CMD_HOLIDAYUSE", false) == 1
        };
    }

    private static Dictionary<DayOfWeek, DayTimeRange> CreateDayTimeRanges(
        IReadOnlyList<Tparkvariable> variables)
    {
        var result = new Dictionary<DayOfWeek, DayTimeRange>();

        for (int i = 0; i <= 6; i++)
        {
            string? value = variables.FirstOrDefault(
                x => x.Cmdtype == $"CMD_OPTIME0{i}")?.Opt;
            string[] parts = value?.Split('~') ?? Array.Empty<string>();

            result[(DayOfWeek)i] =
                parts.Length == 2 &&
                TimeSpan.TryParse(parts[0], out TimeSpan start) &&
                TimeSpan.TryParse(parts[1], out TimeSpan end)
                    ? new DayTimeRange(start, end)
                    : new DayTimeRange(TimeSpan.Zero, TimeSpan.Zero);
        }

        return result;
    }

    private static int GetInt(
        IReadOnlyList<Tparkvariable> variables,
        string commandType,
        bool useOption)
    {
        Tparkvariable? variable = variables.FirstOrDefault(x => x.Cmdtype == commandType);
        string? value = useOption ? variable?.Opt : variable?.Val;
        return int.TryParse(value, out int result) ? result : 0;
    }
}
