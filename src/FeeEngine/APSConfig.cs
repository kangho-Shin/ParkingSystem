using Dapper;
using Newtonsoft.Json;
using System.Data;
using WebApplication_PMS.Models;

namespace WebApplication_PMS
{
    enum DiscountType
    {
        None,          // 할인 없음
        TimeMinute,    // 시간할인 (분 단위)
        Amount,        // 금액 할인
        FixedAmount,   // 고정 금액 정산
        Percent,       // 퍼센트 할인
        Compound       // 복수 할인
    }

    public class ParkingTime
    {
        public DateTime Date { get; set; }           // 해당 날짜
        public int dayMinutes { get; set; }          // 주간 주차 시간 (분)
        public int nightMinutes { get; set; }        // 야간 주차 시간 (분)
        public int Fee { get; set; }                 // 요금 계산 후 저장 가능
    }

    public class ParkFeeStep
    {
        public int UnitMinutes { get; set; }        // 과금 단위 시간
        public int FeePerUnit { get; set; }         // 단위당 요금
        public int MaxCount { get; set; } = 0;      // 최대 적용 횟수 (0은 무제한)
    }

    public class ConfigInitRequest
    {
        public int Sitenum { get; set; }
        public int Groupnum { get; set; }
    }

    public class APSDTOConfig
    {
        [JsonProperty("parkfee")]
        public List<Tparkfee> FeeRules { get; set; } = new();

        [JsonProperty("holiday")]
        public List<Tholiday> Holidays { get; set; } = new();

        [JsonProperty("discount")]
        public List<Tdiscount> Discounts { get; set; } = new();

        [JsonProperty("variable")]
        public List<Tparkvariable> VariableOptions { get; set; } = new();
    }

    public static class APSConfig
    {
        public static List<Tparkfee> FeeRules { get; set; } = new();
        public static List<DateTime> Holidays { get; set; } = new();
        public static List<Tdiscount> Discounts { get; set; } = new();
        public static List<ParkFeeStep> FeeSteps { get; set; } = new();
        public static List<Tparkvariable> VariableOptions { get; set; } = new();
        public static Dictionary<DayOfWeek, (TimeSpan Start, TimeSpan End)> DayTimeRanges { get; set; } = new();

        public static int MaxDailyFee { get; set; }
        public static int GraceTime { get; set; }
        public static int PrepayGraceTime { get; set; }
        public static int ServiceTime { get; set; }
        public static int TimeDiscountApplyType { get; set; }
        public static int ExcludeWeekend { get; set; }
        public static int ExcludeHoliday { get; set; }

        public static void Reload(IDbConnection db)
        {
            // 요금 규칙
            FeeRules = db.Query<Tparkfee>("SELECT * FROM Tparkfee").ToList();

            // 할인 규칙
            Discounts = db.Query<Tdiscount>("SELECT * FROM Tdiscount").ToList();

            // 공휴일
            Holidays = db.Query<DateTime>("SELECT hdate FROM Tholiday").ToList();

            // 설정값
            VariableOptions = db.Query<Tparkvariable>("SELECT * FROM Tparkvariable").ToList();

            // 정수값 옵션 (val 문자열을 int로 변환)
            MaxDailyFee = GetValInt("CMD_MAXDAILY_FEE",true);
            GraceTime = GetValInt("CMD_GRACE_TIME", true);
            PrepayGraceTime = GetValInt("CMD_PREPAY_GRACE", true);
            ServiceTime = GetValInt("CMD_SERVICE_TIME", true);
            TimeDiscountApplyType = 1; // 설정 테이블에 없음 (직접 지정)

            ExcludeWeekend = GetValInt("CMD_WEEKENDUSE",false);
            ExcludeHoliday = GetValInt("CMD_HOLIDAYUSE", false);

            // 요일별 주간 시간대 (일~토: CMD_OPTIME00 ~ CMD_OPTIME06)
            DayTimeRanges = new();
            for (int i = 0; i <= 6; i++)
            {
                string key = $"CMD_OPTIME0{i}";
                string? time = GetOptStr(key);

                if (!string.IsNullOrWhiteSpace(time) && time.Contains("~"))
                {
                    var parts = time.Split('~');
                    if (TimeSpan.TryParse(parts[0], out var start) && TimeSpan.TryParse(parts[1], out var end))
                    {
                        DayTimeRanges[(DayOfWeek)i] = (start, end);
                    }
                    else
                    {
                        DayTimeRanges[(DayOfWeek)i] = (TimeSpan.Zero, TimeSpan.Zero);
                    }
                }
                else
                {
                    DayTimeRanges[(DayOfWeek)i] = (TimeSpan.Zero, TimeSpan.Zero);
                }
            }

            // 요금 스텝은 하드코딩 유지 (또는 DB로 확장 가능)
            FeeSteps = new()
            {
                new ParkFeeStep { UnitMinutes = 30, FeePerUnit = 0,   MaxCount = 1 },
                new ParkFeeStep { UnitMinutes = 10, FeePerUnit = 200, MaxCount = 3 },
                new ParkFeeStep { UnitMinutes = 10, FeePerUnit = 300, MaxCount = 0 }
            };

            // 내부 유틸 함수들
            int GetValInt(string cmdtype,bool optflag)
            {
                var val = VariableOptions.FirstOrDefault(v => v.Cmd_type == cmdtype)?.Val;
                if( optflag == true)
                {
                    int nval = GetOptInt(cmdtype);
                    return nval;
                }
                return int.TryParse(val, out int result) ? result : 0;
            }

            string? GetValStr(string cmdtype)
            {
                return VariableOptions.FirstOrDefault(v => v.Cmd_type == cmdtype)?.Val;
            }

            int GetOptInt(string cmdtype)
            {
                var val = VariableOptions.FirstOrDefault(v => v.Cmd_type == cmdtype)?.Opt;
                return int.TryParse(val, out int result) ? result : 0;
            }

            string? GetOptStr(string cmdtype)
            {
                return VariableOptions.FirstOrDefault(v => v.Cmd_type == cmdtype)?.Opt;
            }
        }
    }
}
