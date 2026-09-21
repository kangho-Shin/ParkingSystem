using APSMain.Comm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APSMain.Smatro
{
    public static class SmartroSendHelper
    {
        public static byte[] MakePacket(byte cmdMsg, SMPAYDATA payData, SMDEVICEINFO? infoset = null)
        {
            string terminalId = APSConfig.SMTERMID ?? "";

            switch (cmdMsg) {
                case Constants.CMD_TX_TREMCHACK:
                    return SmartroPacketBuilder.MakeCheck(terminalId);

                case Constants.CMD_TX_WAITNG:
                    return SmartroPacketBuilder.MakeWait(terminalId);

                case Constants.CMD_TX_UID:
                    return SmartroPacketBuilder.MakeUid(terminalId);

                case Constants.CMD_TX_LAST_DATA:
                    return SmartroPacketBuilder.MakeLast(terminalId);

                case Constants.CMD_TX_PAY:
                    return SmartroPacketBuilder.MakeApproval(terminalId, payData.nMoney);
                case Constants.CMD_TX_ADD_INFO:
                    return SmartroPacketBuilder.MakeAddInfoApproval(terminalId, payData);

                case Constants.CMD_TX_PAYCANCEL:
                    return MakeCancel(terminalId, payData);

                case Constants.CMD_TX_INFO_READ_V2:
                    return SmartroPacketBuilder.MakeDeviceInfo(terminalId);

                case Constants.CMD_TX_INFO_SET_V2:
                    if (infoset == null) { return Array.Empty<byte>(); }
                    return SmartroPacketBuilder.MakeInfoSetV2(terminalId, infoset);
                default:
                    return Array.Empty<byte>();
            }
        }

        private static byte[] MakeCancel(string terminalId, SMPAYDATA payData)
        {
            SmartroCancelRequest req = new SmartroCancelRequest
            {
                CancelType = GetCancelType(payData.cancleCode),
                TradeType = GetTradeType(payData.cancleCode),
                Amount = payData.nMoney,
                Tax = payData.nTax,
                ServiceCharge = payData.nService,
                Installment = payData.halbu.ToString("D2"),
                SignFlag = "1",
                ApprovalNo = payData.approVal ?? "",
                SaleDate = payData.dealDate ?? "",
                SaleTime = payData.dealNum ?? ""
            };

            return SmartroPacketBuilder.MakeCancel(terminalId, req);
        }

        private static string GetCancelType(int cancleCode)
        {
            if (cancleCode == 0)
                return "1";
            if (cancleCode == 1)
                return "2";
            if (cancleCode == 9)
                return "4";
            if (cancleCode == 10)
                return "5";
            if (cancleCode == 11)
                return "6";

            return "3";
        }

        private static string GetTradeType(int cancleCode)
        {
            if (cancleCode == 8)
                return "8";
            if (cancleCode == 7)
                return "6";
            if (cancleCode == 6)
                return "5";
            if (cancleCode == 5)
                return "4";
            if (cancleCode == 4)
                return "3";
            if (cancleCode == 3 || cancleCode == 9 || cancleCode == 10)
                return "2";

            return "1";
        }
    }
}
