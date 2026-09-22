using APSMain.Api.Response;
using APSMain.BaseClass;
using APSMain.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APSMain
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
        public int Feestep { get; set; }
        public int DayShift { get; set; }
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
        public static int Sitenum { get; set; } = 1;
        public static int Groupnum { get; set; } = 1;
        public static int ScreenMode { get; set; } = 24;

        public static string? ParkingName { get; set; } = "";
        public static string? ParkingAddress { get; set; } = "";
        public static string? ParkingPhone { get; set; } = "";
        public static string? BusinessNumber { get; set; } = "";
        public static string? OwnerName { get; set; } = "";

        public static bool HOSTUSE { get; set; } = false;
        public static string? HOSTIP { get; set; }
        public static int HOSTPORT { get; set; }
        public static int VANTYPE { get; set; }
        public static int KICCCANCELTIME { get; set; }

        public static bool BARRIERFREE { get; set; }
        public static bool JungkiMode { get; set; }

        public static int ViolationFee { get; set; }

        public static bool NoCarOutGate { get; set; }

        public static int APSNUM { get; set; }    
        public static string? APSNAME { get; set; }
        public static string? APSIP { get; set; }
        public static int APSMODE { get; set; }
        public static int LprBaseNum { get; set; }

        public static int AutoCancleTime1 { get; set; } = 120;
        public static int AutoCancleTime2 { get; set; } = 120;
        public static int AutoCancleTime3 { get; set; } = 240;
        public static int AutoCancleTime4 { get; set; } = 240;
        public static int AutoCancleTime5 { get; set; } = 60;

        public static int MainTicketNum { get; set; } = 1;
        
        public static int ReceiptCount { get; set; } = 1;

        public static int TICKETERASE { get; set; } = 0; // 티켓 삭제 여부 (0: 삭제 안함, 1: 삭제함)
        public static List<Tparkfee> FeeRules { get; set; } = new();
        public static List<Tholiday> Holidays { get; set; } = new();
        public static List<Tdiscounttable> Discounts { get; set; } = new();
        public static List<ParkFeeStep> FeeSteps { get; set; } = new();

        public static List<Tparkvariable> VariableOptions { get; set; } = new();

        public static Dictionary<DayOfWeek, (TimeSpan Start, TimeSpan End)> DayTimeRanges { get; set; } = new()
        {
            { DayOfWeek.Sunday,    (TimeSpan.FromHours(0), TimeSpan.FromHours(24)) },
            { DayOfWeek.Monday,    (TimeSpan.FromHours(0), TimeSpan.FromHours(24)) },
            { DayOfWeek.Tuesday,   (TimeSpan.FromHours(0), TimeSpan.FromHours(24)) },
            { DayOfWeek.Wednesday, (TimeSpan.FromHours(0), TimeSpan.FromHours(24)) },
            { DayOfWeek.Thursday,  (TimeSpan.FromHours(0), TimeSpan.FromHours(24)) },
            { DayOfWeek.Friday,    (TimeSpan.FromHours(0), TimeSpan.FromHours(24)) },
            { DayOfWeek.Saturday,  (TimeSpan.FromHours(0), TimeSpan.FromHours(24)) }
        };

        public static int MaxDailyFee { get; set; } = 8000;        // 하루 최대 요금 (예: 10,000원)   
        public static int GraceTime { get; set; }                   // CMD_GRACE_TIME
        public static int PrepayGraceTime { get; set; }             // CMD_PREPAY_GRACE
        public static int ServiceTime { get; set; }                 // CMD_SERVICE_TIME
        public static int IRestriction { get; set; }                 // CMD_SERVICE_TIME
        public static int JRestriction { get; set; }                 // CMD_SERVICE_TIME
        public static int TimeDiscountApplyType { get; set; } = 1; //  1: 사전 시간 할인, 2: 사후 시간 할인

        public static int ExcludeWeekend { get; set; }              // 주말 제외 여부
        public static int ExcludeHoliday { get; set; }              // 공휴일 제외 여부

        public static string? PrintPort { get; set; }
        public static int PrintSpeed { get; set; }
        public static int PrintPaper { get; set; }

        public static string? MainPort { get; set; }
        public static int MainSpeed { get; set; }

        public static string? TDPort { get; set; }
        public static int TDSpeed { get; set; }

        public static string? CDPort { get; set; }
        public static int CDSpeed { get; set; }
        public static string? SMTERMID { get; set; }

        public static bool LprXUse { get; set; } = false;
        public static string? LprXIp { get; set; }
        public static int LprXPort { get; set; }

        public static bool LprUse { get; set; } = false;
        public static int LprPort { get; set; }

        //public static string? FtpIp { get; set; }
        //public static string? FtpId { get; set; }
        //public static string? FtpPass { get; set; }

        public static Panel?     FormHost { get; set; } = null!;
        public static List<Form> FormStack { get; } = new();   // ★ 추가
        public static MainForm?   mainForm { get; set; } = null!;
        public static MainForm15? mainForm15 { get; set; } = null!;

        public static MenuForm?  menuForm { get; set; } = null!;
        public static MenuForm15? menuForm15 { get; set; } = null!;
        public static IActiveForm?  activeForm { get; set; } = null!;

        public static int menuFormY { get; set; } = 100;

        public static bool isZoomed { get; set; } = false;

        public static bool isContrast { get; set; } = false;

        public static event Action<bool>? ContrastChanged; // on/off 방송

        public static readonly ContrastToggler Contrast = new ContrastToggler();

        public static void ToggleContrast()
        {
            isContrast = !isContrast;
            ContrastChanged?.Invoke(isContrast);
        }

        public static bool wheelChar = false;
        public static int SoundRepeatTime = 120;
        public static string MAINTERMID = "KIOSK1114915545";
        public static string SMPGID = "pcuc00012m";

        public static bool debugmode { get; set; } = false;
        public static int DebugX { get; set; }
        public static int DebugY { get; set; }
        public static bool cutmode { get; set; } = false;
    }


    public class CarParkRequest
    {
        public int Sitenum { get; set; }
        public int Groupnum { get; set; }
        public int CalType { get; set; }
        public string? CarNum { get; set; }
        public bool isPeriodCar { get; set; } = false; // 기간제 차량 여부
    }

    public static class ParkCache
    {
        public static List<Tparkin> Parkins { get; set; } = new();
        public static List<Tparkinfo> Parkinfos { get; set; } = new();
        public static List<Tperiodmember> Periodmembers { get; set; } = new();
        public static List<Tperiodin> Periodins { get; set; } = new();
        public static List<Tperiodinout> Periodinouts { get; set; } = new();
        public static List<DisInfo> DisKeys { get; set; } = new();
        public static List<Tdisperson> Dispersions { get; set; } = new();
        public static List<Tdiscountinfo> Discountinfos { get; set; } = new ();
        public static List<Tbcardinfo> Bcardinfos { get; set; } = new();

        public static List<CarCalcItem> Cars { get; set; } = new();

        public static void Clear()
        {
            Parkins.Clear();
            Parkinfos.Clear();
            Periodmembers.Clear();
            Periodins.Clear();
            Periodinouts.Clear();
            DisKeys.Clear();
            Dispersions.Clear();
            Discountinfos.Clear();
            Bcardinfos.Clear();
            Cars.Clear();
        }

        public static Tparkin? GetinCar(string carnum)
            => Parkins.FirstOrDefault(p => p.Carnum == carnum);

        public static Tparkinfo? GetParkCar(string carnum)
            => Parkinfos.FirstOrDefault(p => p.Carnum == carnum);

        public static Tperiodmember? GetPeriodInfoCar(string carnum)
            => Periodmembers.FirstOrDefault(p => p.Carnum1 == carnum);

        public static Tperiodin? GetPeriodInCar(string carnum)
            => Periodins.FirstOrDefault(p => p.Carnum == carnum);

        public static Tperiodinout? GetPeriodInoutCar(string carnum)
            => Periodinouts.FirstOrDefault(p => p.Carnum == carnum);

        public static DisInfo? GetDiskey(int diskey)
           => DisKeys.FirstOrDefault(p => p.diskey == diskey);

        public static bool IsSingleMemberCarInCache()
        {
            int count = Periodmembers.Count +
                        Periodins.Count +
                        Periodinouts.Count;
            return count == 1;
        }

        public static int GetmemberCarMatchCount()
        {
            int count = Periodmembers.Count +
                        Periodins.Count +
                        Periodinouts.Count;
            return count;
        }

        public static bool IsSingleCarInCache()
        {
            int count = Parkins.Count +
                        Parkinfos.Count;
            return count == 1;
        }

        public static int GetCarMatchCount()
        {
            int count = Parkins.Count + Periodmembers.Count + Parkinfos.Count;
            return count;
        }
    }

    public static class Constants
    {
        public const byte ASCII_NONE= 0x00;
        public const byte ASCII_STX = 0x02;
        public const byte ASCII_ETX = 0x03;
        public const byte ASCII_ACK = 0x06;
        public const byte ASCII_NAK = 0x15;
        public const byte ASCII_DLE = 0x10;
        public const byte ASCII_BCC = 0x18;
        public const byte ASCII_ESC = 0x1B; 
        public const byte ASCII_DONE = 0x88;

        public const byte CMD_TX_TREMCHACK = (byte)'A';
        public const byte CMD_TX_PAY = (byte)'B';
        public const byte CMD_TX_PAYCANCEL = (byte)'C';
        public const byte CMD_TX_SEARCH = (byte)'D';
        public const byte CMD_TX_WAITNG = (byte)'E';
        public const byte CMD_TX_UID = (byte)'F';
        public const byte CMD_TX_ADD_INFO = (byte)'G';
        public const byte CMD_TX_RETURN = (byte)'H';
        public const byte CMD_TX_INFO_SET = (byte)'I';
        public const byte CMD_TX_INFO_READ = (byte)'J';
        public const byte CMD_TX_INFO_WRITING = (byte)'K';
        public const byte CMD_TX_LAST_DATA = (byte)'L';
        public const byte CMD_TX_IC_CARD_CHECK = (byte)'M';
        public const byte CMD_TX_TRAFFIC_CHECK = (byte)'N';
        public const byte CMD_TX_QR = (byte)'Q';
        public const byte CMD_TX_RESET = (byte)'R';
        public const byte CMD_TX_SET_UP = (byte)'S';
        public const byte CMD_TX_TRANS_PAY = (byte)'T';
        public const byte CMD_TX_VER_CHECK = (byte)'V';
        public const byte CMD_TX_QR_CHECK = (byte)'W';
        public const byte CMD_TX_INFO_SET_V2 = (byte)'X';
        public const byte CMD_TX_INFO_READ_V2 = (byte)'Y';
        public const byte CMD_TX_DISCOUNT = (byte)'Z';

        public const byte CMD_RX_TREMCHACK = (byte)'a';
        public const byte CMD_RX_PAY = (byte)'b';
        public const byte CMD_RX_PAYCANCEL = (byte)'c';
        public const byte CMD_RX_SEARCH = (byte)'d';
        public const byte CMD_RX_WAITNG = (byte)'e';
        public const byte CMD_RX_UID = (byte)'f';
        public const byte CMD_RX_ADD_INFO = (byte)'g';
        public const byte CMD_RX_RETURN = (byte)'h';
        public const byte CMD_RX_INFO_SET = (byte)'i';
        public const byte CMD_RX_INFO_READ = (byte)'j';
        public const byte CMD_RX_INFO_WRITING = (byte)'k';
        public const byte CMD_RX_LAST_DATA = (byte)'l';
        public const byte CMD_RX_IC_CARD_CHECK = (byte)'m';
        public const byte CMD_RX_TRAFFIC_CHECK = (byte)'n';
        public const byte CMD_RX_QR = (byte)'q';
        public const byte CMD_RX_RESET = (byte)'r';
        public const byte CMD_RX_SET_UP = (byte)'s';
        public const byte CMD_RX_TRANS_PAY = (byte)'t';
        public const byte CMD_RX_VER_CHECK = (byte)'v';
        public const byte CMD_RX_QR_CHECK = (byte)'w';
        public const byte CMD_RX_INFO_SET_V2 = (byte)'x';
        public const byte CMD_RX_INFO_READ_V2 = (byte)'y';
        public const byte CMD_RX_DISCOUNT = (byte)'z';
    }

}

