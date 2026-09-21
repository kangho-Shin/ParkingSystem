using System;
using System.Runtime.InteropServices;

namespace APSMain.BaseClass
{
    public enum device_type : int
    {
        DEVICE_TYPE_MANNED = 1,
        DEVICE_TYPE_APS,
        DEVICE_TYPE_PDA,
        DEVICE_TYPE_LPR,
        DEVICE_TYPE_TICKETER,
        DEVICE_TYPE_RF,
        DEVICE_TYPE_KIOSK,
        DEVICE_TYPE_ETC,
        DEVICE_TYPE_MAX
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct STDEVINFO
    {
        public fixed byte devname[40];
        public int devnum;
        public int devtype;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct STUDPHADER
    {
        public ushort btID;       // 0xC0C0
        public ushort wClientID;  // 요금계산기 번호
        public ushort wtCmd;      // 커맨드
        public ushort nSize;      // payload size
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public unsafe struct STUDPDATA
    {
        public ushort devicenum;
        public fixed byte srcip[20];
        public fixed byte desip[20];
        public fixed byte xdata[1024];
    }

    //	[Serializable]
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public unsafe struct STUDPPACKET
    {
        public ushort btID;
        public ushort wClientID;
        public ushort wtCmd;
        public ushort nSize;
        public fixed byte xPacket[1272];
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct STTACCEPT
    {
        public int xindex;
        public byte sitenum;
        public byte groupnum;
        public ushort devicenum;
        public byte devicetype;
        public byte managercode;

        public fixed byte managername[30];
        public fixed byte ip[20];
        public fixed byte accepttime[20];
        public byte accepttype;

        public fixed int uparkmoney[2];        // long → int (32bit)
        public fixed int uservicemoney[2];
        public int uchargemoney;
        public fixed int uincar[2];
        public fixed int uoutcar[2];
        public fixed int saveMoney[7];
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct STTLOGON
    {
        public int xindex;
        public byte sitenum;
        public byte groupnum;
        public ushort devicenum;
        public byte devicetype;
        public byte managercode;

        public fixed byte managerid[20];
        public fixed byte managerpass[20];
        public fixed byte managername[30];
        public fixed byte logdatetime[20];
        public byte logtype;
        public fixed byte ip[20];

        public fixed int uparkmoney[2];        // long → int (32bit)
        public fixed int uservicemoney[2];
        public int uchargemoney;
        public fixed int uincar[2];
        public fixed int uoutcar[2];
        public fixed int saveMoney[7];
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct STTDEVICELIST
    {
        public int xindex;
        public byte sitenum;
        public byte groupnum;
        public ushort devicenum;
        public fixed byte devicename[20];
        public byte devicetype;
        public fixed byte ip[20];
        public fixed byte note[100];
    }

    // TDEVICESTATE
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct STTDEVICESTATE
    {
        public int xindex;
        public byte sitenum;
        public byte groupnum;
        public ushort devicenum;
        public fixed byte devicename[20];
        public byte devicetype;
        public fixed byte ip[20];
        public int restmoney;
        public int income;
        public fixed byte connecttime[20];
        public fixed byte lasttime[20];
        public int restmoney0;
        public int restmoney1;
        public int restmoney2;
        public int restmoney3;
        public int restmoney4;
        public int restmoney5;
        public int errcode;
    }

    // TDEVICEALARM
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct STTDEVICEALARM
    {
        public int xindex;
        public byte sitenum;
        public byte groupnum;
        public ushort devicenum;
        public fixed byte alarmkind[40];
        public fixed byte alarmtime[20];
        public fixed byte repairtime[20];
        public fixed byte alarmmessage[100];
        public fixed byte repairmessage[100];
        public byte managercode;
        public fixed byte managername[30];
    }

    // TMANAGER
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct STTMANAGER
    {
        public int xindex;
        public byte sitenum;
        public byte groupnum;
        public ushort devicenum;
        public byte managercode;
        public fixed byte managerid[10];
        public fixed byte managerpw[10];
        public fixed byte managername[30];
        public fixed byte managertel[20];
        public fixed byte managerplace[30];
        public byte managerpower;
        public fixed byte workingend[16];
        public fixed byte workingstart[16];
    }

    // TGATEINFO
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct STTGATEINFO
    {
        public int xindex;
        public byte sitenum;
        public byte groupnum;
        public ushort devicenum;
        public fixed byte gatename[20];
        public fixed byte ip[20];
        public byte inouttype;
        public byte openclose;
        public fixed byte opdate[20];
        public byte ophour;
        public byte opmin;
        public byte managercode;
        public fixed byte managername[30];
        public byte manflag;
        public byte dendflag;
        public byte tendflag;
    }

    // TPARKNUM
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct STTPARKNUM
    {
        public int xindex;
        public byte sitenum;
        public byte groupnum;
        public ushort devicenum;
        public byte devicetype;
        public int parkinnum;
        public int parkoutnum;
        public int parkfullnum;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct STTPARKIN
    {
        public int xindex;
        public byte sitenum;
        public byte groupnum;
        public fixed byte ticketdata[30];
        public int ticketnum;
        public byte ticketcartype;
        public byte parkcartype;
        public fixed byte carnum[20];
        public ushort indevicenum;
        public fixed byte indate[14];
        public byte inhour;
        public byte inmin;
        public fixed byte inimage[80];
        public fixed byte parkonplace[20];
        public byte outflag;
        public byte managercode;
        public fixed byte managername[30];
        public fixed byte ip[20];
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct STTPARKINFO
    {
        public int xindex;
        public byte sitenum;
        public byte groupnum;
        public fixed byte ticketdata[30];
        public int ticketnum;
        public byte ticketcartype;
        public byte parkcartype;
        public fixed byte carnum[20];
        public ushort indevicenum;
        public fixed byte indate[14];
        public byte inhour;
        public byte inmin;
        public ushort outdevicenum;
        public fixed byte outdate[14];
        public byte outhour;
        public byte outmin;
        public fixed byte inimage[80];
        public fixed byte outimage[80];
        public int parktime;
        public byte parkcaltype;
        public byte salecaltype;
        public int parkmoney;
        public int salemoney;
        public int saletime;
        public int credittype;
        public fixed byte creditcardno[40];
        public fixed byte acceptno[20];
        public fixed byte accepttime[20];
        public int creditmoney;
        public int receiptnum;

        public fixed byte ocssalecode[20];
        public int ocssaletype;
        public int ocssaleval;
        public byte managercode;
        public fixed byte managername[30];
        public fixed byte saleimage[80];
        public int intick;          // long → int (32bit 기준)
        public int outtick;
        public byte manflag;
        public byte dendflag;
        public byte tendflag;
        public fixed byte denddate[14];
        public fixed byte tenddate[14];
        public byte outflag;
        public int cashMoney;       // long → int
        public int changemoney;
        public fixed int uparkmoney[2];
        public fixed int uservicemoney[2];
        public int uchargemoney;
        public fixed int uoutcar[2];
        public fixed int saveMoney[7];
        public fixed byte ip[20];
        public int echargefee;
        public int echargeamount;
        public int eunitprice;
        public int echargetime;
        public int eparktime;
        public int eoverfee;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct STTKICCSCREDITCARD
    {
        public byte sitenum;
        public byte groupnum;
        public int ticketnum;
        public ushort indevicenum;
        public ushort outdevicenum;

        public fixed byte terminalnum[9];
        public fixed byte rescode[5];
        public fixed byte issuecode[4];
        public fixed byte issueinum[5];
        public fixed byte acceptdate[20];
        public fixed byte vandealnum[13];
        public fixed byte acceptnum[10];
        public fixed byte issuename[15];
        public fixed byte entrynum[16];
        public fixed byte purchasename[15];
        public fixed byte szErrorMsg[65];
        public fixed byte notice[61];

        public byte dealctrlcode;
        public byte accountindex;
        public fixed byte dealindex[5];
        public byte displaycode;
        public fixed byte cardnum[21];

        public int money;            // long → int (32bit 기준)
        public int parktime;

        public byte dendflag;
        public byte tendflag;

        public fixed byte enddate[20];
        public fixed byte ip[20];
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct STTCREDITCARD
    {
        public int xindex;
        public byte sitenum;
        public byte groupnum;
        public int ticketnum;
        public ushort indevicenum;
        public ushort outdevicenum;

        public fixed byte acceptdate[14];
        public byte accepthour;
        public byte acceptmin;
        public fixed byte approvalcode[8];
        public fixed byte dealnum[14];
        public fixed byte dealdate[14];
        public fixed byte dealtime[8];
        public fixed byte issuecode[8];
        public fixed byte purchasecode[8];
        public fixed byte acceptnum[14];
        public fixed byte message1[18];
        public fixed byte message2[18];
        public fixed byte cardnum[18];
        public fixed byte valxindexdate[10];
        public fixed byte installment[4];
        public fixed byte money[11];
        public fixed byte entrynum[17];

        public byte quotationused;
        public byte acceptused;

        public fixed byte point1[14];
        public fixed byte point2[14];
        public fixed byte point3[14];
        public fixed byte point4[14];
        public fixed byte vandealnum[14];

        public byte isp;
        public byte mpimodule;
        public byte mpiused;
        public int parktime;

        public byte dendflag;
        public byte tendflag;
        public fixed byte enddate[14];
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct STTCOMPANY
    {
        public int xindex;
        public byte sitenum;
        public byte groupnum;
        public ushort devicenum;
        public byte companycode;

        public fixed byte companyname[40];
        public fixed byte pregxindexentname[30];
        public fixed byte companyaddress[100];
        public fixed byte companytel[20];
        public fixed byte companyfax[20];

        public byte managercode;
        public fixed byte managername[30];
        public fixed byte note[100];
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct STDEPARTMENT
    {
        public int xindex;
        public byte sitenum;
        public byte groupnum;
        public byte companycode;

        public fixed byte companyname[40];
        public int deptcode;
        public fixed byte deptname[30];
        public fixed byte depttel[20];
        public fixed byte deptfax[20];
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct STTPERIODIN
    {
        public int xindex;
        public byte sitenum;
        public byte groupnum;
        public int cardid;
        public fixed byte name[30];
        public fixed byte carnum[20];
        public fixed byte cartype[30];
        public ushort indevicenum;
        public fixed byte indate[14];
        public byte inhour;
        public byte inmin;
        public fixed byte inimage[80];
        public fixed byte enddate[14];
        public int intimetick;  // long → int
        public byte outflag;
        public fixed byte parkonplace[20];
        public byte managercode;
        public fixed byte managername[30];
        public fixed byte ip[20];
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct STTPERIODINOUT
    {
        public int xindex;
        public byte sitenum;
        public byte groupnum;
        public int cardid;
        public fixed byte name[30];
        public fixed byte carnum[20];
        public fixed byte cartype[30];
        public ushort indevicenum;
        public fixed byte indate[14];
        public byte inhour;
        public byte inmin;
        public fixed byte inimage[80];
        public ushort outdevicenum;
        public fixed byte outdate[14];
        public byte outhour;
        public byte outmin;
        public fixed byte outimage[80];
        public int parktime;
        public fixed byte enddate[14];
        public byte managercode;
        public fixed byte managername[30];
        public byte outflag;
        public fixed byte note[45];
        public fixed byte ip[20];
        public fixed byte parktimetime[20];
        public byte parktimecode;
        public int reserved1;
        public int reserved2;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct STTPERIODMEMBER
    {
        public int xindex;
        public byte sitenum;
        public byte groupnum;
        public ushort devicenum;
        public int cardid;
        public fixed byte serialno[20];
        public byte periodtype;
        public fixed byte name[30];
        public fixed byte telnum[20];
        public fixed byte groupcode[20];
        public fixed byte company1[30];
        public fixed byte company2[30];
        public fixed byte carnum1[20];
        public fixed byte cartype1[30];
        public fixed byte carnum2[20];
        public fixed byte cartype2[30];
        public fixed byte address[100];
        public byte parktype;
        public fixed byte recorddate[14];
        public fixed byte startdate[14];
        public fixed byte enddate[14];
        public byte parktimecode;
        public fixed byte parktimetime[14];
        public int parkprice;
        public fixed byte parkarea[10];
        public int parklevel;
        public fixed byte parkvalidday[10];
        public byte antiflag;
        public byte useflag;
        public byte serviceday;
        public byte managercode;
        public fixed byte managername[30];
        public fixed byte paytype[45];
        public byte outflag;
        public int intimetick;  // long → int
        public byte reserved1;
        public byte reserved2;
        public byte reserved3;
        public byte reserved4;
        public fixed byte note[50];
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct STTPERIODTMEMBER
    {
        public int xindex;
        public byte sitenum;
        public byte groupnum;
        public int cardid;
        public int ticketnum;
        public fixed byte carnum[20];
        public fixed byte name[20];
        public fixed byte telnum[20];
        public fixed byte recorddate[16];
        public fixed byte startdate[16];
        public fixed byte enddate[16];
        public byte saletype;
        public int saleval;
        public fixed byte visitplace[30];
        public fixed byte visitobject[45];
        public int pindex;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct STTPERIODACCOUNT
    {
        public int xindex;
        public byte sitenum;
        public byte groupnum;
        public ushort devicenum;
        public byte devicetype;
        public int cardid;
        public fixed byte name[30];
        public fixed byte carnum[20];
        public fixed byte company1[30];
        public fixed byte company2[30];
        public fixed byte recorddate[14];
        public fixed byte startdate[14];
        public byte starthour;
        public byte startmin;
        public fixed byte enddate[14];
        public byte endhour;
        public byte endmin;
        public int price;
        public byte managercode;
        public fixed byte managername[20];
        public byte printflag;
    }

    public class DisInfo
    {
        public int diskey;
        public int save;
        public int limit;
        public string title=string.Empty;
    }
}
