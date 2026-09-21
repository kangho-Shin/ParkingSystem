using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APSMain.Smatro
{
    public static class SmartroPacketBuilder
    {
        private const int HEADER_SIZE = 35;
        private const byte STX = 0x02;
        private const byte ETX = 0x03;

        private static readonly Encoding _enc = Encoding.GetEncoding("ks_c_5601-1987");

        public static byte[] MakeCheck(string terminalId)
        {
            return MakePacket(terminalId, (byte)'A', Array.Empty<byte>());
        }

        public static byte[] MakeWait(string terminalId)
        {
            return MakePacket(terminalId, (byte)'E', Array.Empty<byte>());
        }

        public static byte[] MakeUid(string terminalId)
        {
            return MakePacket(terminalId, (byte)'F', Array.Empty<byte>());
        }

        public static byte[] MakeLast(string terminalId)
        {
            return MakePacket(terminalId, (byte)'L', Array.Empty<byte>());
        }

        public static byte[] MakeDeviceInfo(string terminalId)
        {
            return MakePacket(terminalId, (byte)'Y', Array.Empty<byte>());
        }

        public static byte[] MakeApproval(string terminalId, int amount)
        {
            byte[] body = _enc.GetBytes(
                "1" +
                amount.ToString("D10") +
                "00000000" +
                "00000000" +
                "00" +
                "1"
            );

            return MakePacket(terminalId, (byte)'B', body);
        }

        public static byte[] MakeAddInfoApproval(string terminalId, SMPAYDATA payData)
        {
            byte[] body = new byte[339];
            int pos = 0;

            WriteText(body, ref pos, "1", 1);                         // 거래구분
            WriteText(body, ref pos, payData.nMoney.ToString("D10"), 10);
            WriteText(body, ref pos, payData.nTax.ToString("D8"), 8);
            WriteText(body, ref pos, payData.nService.ToString("D8"), 8);
            WriteText(body, ref pos, payData.halbu.ToString("D2"), 2);

            WriteText(body, ref pos, MakeCarNoText(payData.carNum), 30); // 차량번호
            WriteText(body, ref pos, "", 20);                            // 전화번호
            WriteText(body, ref pos, "", 40);                            // 이메일
            WriteText(body, ref pos, "", 20);                            // 구매자 연락처
            WriteText(body, ref pos, payData.prodMsg ?? "", 50);          // 상품명
            WriteText(body, ref pos, "", 100);                           // 주소
            WriteText(body, ref pos, "", 50);                            // 메시지

            return MakePacket(terminalId, (byte)'G', body);
        }

        private static string MakeCarNoText(string? carNum)
        {
            return "CARNO" + (carNum ?? "");
        }

        public static byte[] MakeCancel(string terminalId, SmartroCancelRequest req)
        {
            byte[] body = _enc.GetBytes(
                req.CancelType.PadRight(1).Substring(0, 1) +
                req.TradeType.PadRight(1).Substring(0, 1) +
                req.Amount.ToString("D10") +
                req.Tax.ToString("D8") +
                req.ServiceCharge.ToString("D8") +
                req.Installment.PadLeft(2, '0').Substring(0, 2) +
                req.SignFlag.PadRight(1).Substring(0, 1) +
                req.ApprovalNo.PadRight(12).Substring(0, 12) +
                req.SaleDate.PadRight(8).Substring(0, 8) +
                req.SaleTime.PadRight(6).Substring(0, 6)
            );

            return MakePacket(terminalId, (byte)'C', body);
        }

        private static byte[] MakePacket(string terminalId, byte jobCode, byte[] body)
        {
            byte[] packet = new byte[HEADER_SIZE + body.Length + 2];
            int pos = 0;

            packet[pos++] = STX;

            WriteText(packet, ref pos, terminalId, 16);
            WriteText(packet, ref pos, DateTime.Now.ToString("yyyyMMddHHmmss"), 14);

            packet[pos++] = jobCode;
            packet[pos++] = 0x00;

            packet[pos++] = (byte)(body.Length & 0xFF);
            packet[pos++] = (byte)((body.Length >> 8) & 0xFF);

            if (body.Length > 0) {
                Buffer.BlockCopy(body, 0, packet, pos, body.Length);
                pos += body.Length;
            }

            packet[pos++] = ETX;
            packet[pos] = CalcBcc(packet, 0, pos);

            return packet;
        }

        private static void WriteText(byte[] dest, ref int pos, string text, int size)
        {
            Array.Fill(dest, (byte)0x20, pos, size);

            byte[] src = _enc.GetBytes(text ?? "");
            int copyLen = Math.Min(src.Length, size);

            Buffer.BlockCopy(src, 0, dest, pos, copyLen);
            pos += size;
        }

        private static byte CalcBcc(byte[] data, int start, int end)
        {
            byte bcc = 0;

            for (int i = start; i <= end; i++)
                bcc ^= data[i];

            return bcc;
        }

        public static byte[] MakeInfoSetV2(string terminalId, SMDEVICEINFO? info)
        {
            byte[] body = new byte[246];
            int pos = 0;

            if (info == null) { return Array.Empty<byte>(); }
            WriteText(body, ref pos, info.s_Crd_Id ?? "", 16);
            WriteText(body, ref pos, info.s_Crd_Ip ?? "", 16);
            WriteText(body, ref pos, info.s_Crd_Port ?? "", 16);

            WriteText(body, ref pos, info.s_Pre_Id ?? "", 16);
            WriteText(body, ref pos, info.s_Pre_Ip ?? "", 16);
            WriteText(body, ref pos, info.s_Pre_Port ?? "", 16);

            WriteText(body, ref pos, info.s_Key_Ip ?? "", 16);
            WriteText(body, ref pos, info.s_Key_Port ?? "", 16);

            WriteText(body, ref pos, info.s_Air_Ip ?? "", 16);
            WriteText(body, ref pos, info.s_Air_Port ?? "", 16);

            body[pos++] = ToDigitByte(info.i_Sam_Slot1);
            body[pos++] = ToDigitByte(info.i_Sam_Slot2);
            body[pos++] = ToDigitByte(info.i_Sam_Slot3);
            body[pos++] = ToDigitByte(info.i_Sam_Slot4);

            body[pos++] = ToDigitByte(info.i_Device_Type);

            WriteText(body, ref pos, info.s_Dev_Ip ?? "", 16);
            WriteText(body, ref pos, info.s_Dev_Port ?? "", 16);

            body[pos++] = ToDigitByte(info.i_Ent_Dhcp);

            WriteText(body, ref pos, info.s_Ent_Device_Ip ?? "", 16);
            WriteText(body, ref pos, info.s_Ent_Subnet ?? "", 16);
            WriteText(body, ref pos, info.s_Ent_Gatway ?? "", 16);

            return MakePacket(terminalId, (byte)'X', body);
        }

        private static byte ToDigitByte(int value)
        {
            if (value < 0) { value = 0; }

            if (value > 9) { value = 9; }

            return (byte)('0' + value);
        }
    }
}
