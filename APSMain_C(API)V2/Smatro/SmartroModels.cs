using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APSMain.Smatro
{
    public class CardTransInfo
    {
        public int Sitenum { get; set; }
        public int Groupnum { get; set; }
        public string TicketData { get; set; } = "";
        public int OutDeviceNum { get; set; }
        public string TermId { get; set; } = "";
        public string PosId { get; set; } = "";
        public string ResCode { get; set; } = "";
        public string DealNum { get; set; } = "";
        public string DealDate { get; set; } = "";
        public string DealTime { get; set; } = "";
        public string CardName { get; set; } = "";
        public string CardId { get; set; } = "";
        public string BranchNum { get; set; } = "";
        public string AcceptNum { get; set; } = "";
        public string SamId { get; set; } = "";
        public string SamDealNum { get; set; } = "";
        public string ReceiptNum { get; set; } = "";
        public int AcceptType { get; set; }
        public int DealType { get; set; }
        public int Money { get; set; }
        public int Tax { get; set; }
        public int Service { get; set; }
        public int Installment { get; set; }
        public int ParkTime { get; set; }
        public int DendFlag { get; set; }
        public int TendFlag { get; set; }
        public string EndDate { get; set; } = "";
        public int Pindex { get; set; }

        public void CopyFrom(CardTransInfo? src)
        {
            if (src == null) { return; }
            Sitenum = src.Sitenum;
            Groupnum = src.Groupnum;
            TicketData = src.TicketData;
            OutDeviceNum = src.OutDeviceNum;
            TermId = src.TermId;
            PosId = src.PosId;
            ResCode = src.ResCode;
            DealNum = src.DealNum;
            DealDate = src.DealDate;
            DealTime = src.DealTime;
            CardName = src.CardName;
            CardId = src.CardId;
            BranchNum = src.BranchNum;
            AcceptNum = src.AcceptNum;
            SamId = src.SamId;
            SamDealNum = src.SamDealNum;
            ReceiptNum = src.ReceiptNum;
            AcceptType = src.AcceptType;
            DealType = src.DealType;
            Money = src.Money;
            Tax = src.Tax;
            Service = src.Service;
            Installment = src.Installment;
            ParkTime = src.ParkTime;
            DendFlag = src.DendFlag;
            TendFlag = src.TendFlag;
            EndDate = src.EndDate;
            Pindex = src.Pindex;
        }

        public void Clear()
        {
            Sitenum = 0;
            Groupnum = 0;
            TicketData = string.Empty;
            OutDeviceNum = 0;
            TermId = string.Empty;
            PosId = string.Empty;
            ResCode = string.Empty;
            DealNum = string.Empty;
            DealDate = string.Empty;
            DealTime = string.Empty;
            CardName = string.Empty;
            CardId = string.Empty;
            BranchNum = string.Empty;
            AcceptNum = string.Empty;
            SamId = string.Empty;
            SamDealNum = string.Empty;
            ReceiptNum = string.Empty;
            AcceptType = 0;
            DealType = 0;
            Money = 0;
            Tax = 0;
            Service = 0;
            Installment = 0;
            ParkTime = 0;
            DendFlag = 0;
            TendFlag = 0;
            EndDate = string.Empty;
            Pindex = 0;
        }
    }

    public class SMPAYDATA
    {
        public int appCode { get; set; }
        public int cancleCode { get; set; }
        public int nMoney { get; set; }
        public int nTax { get; set; }
        public int nService { get; set; }
        public int halbu { get; set; }
        public string carNum { get; set; } = "";
        public string prodMsg { get; set; } = "";
        public string approVal { get; set; } = "";
        public string dealDate { get; set; } = "";
        public string dealNum { get; set; } = "";
        public string termid { get; set; } = "";
        public string qrCode { get; set; } = "";

        public void Init()
        {
            appCode = 0;
            cancleCode = 0;
            nMoney = 0;
            nTax = 0;
            nService = 0;
            halbu = 0;
            carNum = "";
            prodMsg = "";
            approVal = "";
            dealDate = "";
            dealNum = "";
            termid = "";
            qrCode = "";
        }
    }

    public class SMDEVICEINFO
    {
        public string? s_Crd_Id;
        public string? s_Crd_Ip;
        public string? s_Crd_Port;
        public string? s_Pre_Id;
        public string? s_Pre_Ip;
        public string? s_Pre_Port;
        public string? s_Key_Ip;
        public string? s_Key_Port;
        public string? s_Air_Ip;
        public string? s_Air_Port;
        public int i_Sam_Slot1;
        public int i_Sam_Slot2;
        public int i_Sam_Slot3;
        public int i_Sam_Slot4;
        public int i_Device_Type;
        public int i_Lcd_Value;
        public int i_Sound_Value;
        public int i_Touch_Value;
        public bool b_Setup_Load;
        public int i_Ent_Dhcp;
        public string? s_Ent_Device_Ip;
        public string? s_Ent_Subnet;
        public string? s_Ent_Gatway;
        public string? s_Dev_Port;
        public string? s_Dev_Ip;

        public void Copy(SMDEVICEINFO indev)
        {
            s_Crd_Id = indev.s_Crd_Id;
            s_Crd_Ip  = indev.s_Crd_Ip;
            s_Crd_Port  = indev.s_Crd_Port;
            s_Pre_Id  = indev.s_Pre_Id;
            s_Pre_Ip  = indev.s_Pre_Ip;
            s_Pre_Port  = indev.s_Pre_Port;
            s_Key_Ip  = indev.s_Key_Ip;
            s_Key_Port  = indev.s_Key_Port;
            s_Air_Ip  = indev.s_Air_Ip;
            s_Air_Port  = indev.s_Air_Port;
            i_Sam_Slot1  = indev.i_Sam_Slot1;
            i_Sam_Slot2  = indev.i_Sam_Slot2;
            i_Sam_Slot3  = indev.i_Sam_Slot3;
            i_Sam_Slot4  = indev.i_Sam_Slot4;
            i_Device_Type  = indev.i_Device_Type;
            i_Lcd_Value  = indev.i_Lcd_Value;
            i_Sound_Value  = indev.i_Sound_Value;
            i_Touch_Value  = indev.i_Touch_Value;
            b_Setup_Load  = indev.b_Setup_Load;
            i_Ent_Dhcp  = indev.i_Ent_Dhcp;
            s_Ent_Device_Ip  = indev.s_Ent_Device_Ip;
            s_Ent_Subnet  = indev.s_Ent_Subnet;
            s_Ent_Gatway  = indev.s_Ent_Gatway;
            s_Dev_Port  = indev.s_Dev_Port;
            s_Dev_Ip  = indev.s_Dev_Ip;
        }
    }

    public class SmartroCancelRequest
    {
        public string CancelType { get; set; } = "1";
        public string TradeType { get; set; } = "1";
        public int Amount { get; set; }
        public int Tax { get; set; }
        public int ServiceCharge { get; set; }
        public string Installment { get; set; } = "00";
        public string SignFlag { get; set; } = "1";
        public string ApprovalNo { get; set; } = "";
        public string SaleDate { get; set; } = "";
        public string SaleTime { get; set; } = "";
    }

    public class SmartroApprovalResponse
    {
        public string TradeType { get; set; } = "";
        public string MediaType { get; set; } = "";
        public string CardNo { get; set; } = "";
        public int Amount { get; set; }
        public int Tax { get; set; }
        public int ServiceCharge { get; set; }
        public string Installment { get; set; } = "";
        public string ApprovalNo { get; set; } = "";
        public string SaleDate { get; set; } = "";
        public string SaleTime { get; set; } = "";
        public string TradeNo { get; set; } = "";
        public string MerchantNo { get; set; } = "";
        public string TerminalNo { get; set; } = "";
        public string IssuerInfo { get; set; } = "";
        public string AcquirerInfo { get; set; } = "";

        public bool IsSuccess => TradeType != "X";
    }

    public class SmartroDeviceStatus
    {
        public string CardModule { get; set; } = "";
        public string RfModule { get; set; } = "";
        public string VanServer { get; set; } = "";
        public string LinkServer { get; set; } = "";
    }

    public class SmartroEventResponse
    {
        public string EventCode { get; set; } = "";

        public bool IsMsCard => EventCode == "M";
        public bool IsRfCard => EventCode == "R";
        public bool IsIcInsert => EventCode == "I";
        public bool IsIcRemove => EventCode == "O";
        public bool IsFallback => EventCode == "F";
        public bool IsBarcode => EventCode == "Q";
    }

    public class SmartroUidResponse
    {
        public string CardUid { get; set; } = "";
    }
}
