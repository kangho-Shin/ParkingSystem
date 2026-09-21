using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APSMain.BaseClass
{
    public enum req_cmd_code
    {
        // 정보 저장 요청.
        APS_CMD_ACCEPT,             // 0
        APS_CMD_COMPANY,
        APS_CMD_CREDITCARD,
        APS_CMD_COUPONINFO,
        APS_CMD_DAYCLOSECAL,
        APS_CMD_DEPARTMENT,
        APS_CMD_DEVICEALARM,
        APS_CMD_DEVICELIST,
        APS_CMD_DEVICESTATE,
        APS_CMD_GATEINFO,
        APS_CMD_LOGON,              //10
        APS_CMD_MANAGER,
        APS_CMD_PARKIN,
        APS_CMD_PARKOUT,
        APS_CMD_PARKINFO,
        APS_CMD_PARKNUM,
        APS_CMD_PERIODACCOUNT,
        APS_CMD_PERIODIN,
        APS_CMD_PERIODINOUT,
        APS_CMD_PERIODMEMBER,
        APS_CMD_PERIODPARKTIME,     // 20
        APS_CMD_PING,
        APS_CMD_TMONEYCARD,
        APS_CMD_TMONEYERRLOG,
        APS_CMD_LPRINFO,
        APS_CMD_IOCARINFO,
        APS_CMD_PAGING,
        APS_CMD_OCSMEMBER,
        APS_CMD_OCSSALE,
        APS_CMD_SALECARDISSUE,
        APS_CMD_TKICCCREDIT,            // 30
        APS_CMD_OPCLOSECAL,
        APS_CMD_PERIODTMEMBER,
        APS_CMD_WEBREQ,
        APS_CMD_WEBCARNUM,
        APS_CMD_WEBCANCEL,
        APS_CMD_WEBCAMERA,
        APS_CMD_WEBSALEKEY,
        APS_CMD_LOGDATA,
        APS_CMD_MAIN_KICC,
        APS_CMD_MAIN_CONFIRM,
        APS_CMD_NOCARNUM,               // 40
        APS_CMD_ERROR,
        APS_CMD_CONFIG,
        APS_CMD_ALARM,
        APS_CMD_TIME,
        APS_CMD_NOCAR,
        APS_CMD_DEVICETYPE,
        APS_CMD_RERECEIPT,
        APS_CMD_CDCANCEL,
        APS_CMDdbCHECK,
        APS_COM_EMERGENCY,
        APS_CMD_WEBDISREQ,
        APS_CMD_PARKCALS,
        APS_CMD_PARKCALSTOP,
        APS_CMD_MAX
    };
}
