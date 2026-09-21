using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APSMain.Smatro
{
    public static class SmartroPacketParser
    {
        private const int HEADER_SIZE = 35;
        private const byte STX = 0x02;
        private const byte ETX = 0x03;

        private static readonly Encoding _enc = Encoding.GetEncoding("ks_c_5601-1987");

        public static bool ParsePacket(byte[] packet, int length, out SmartroPacket result)
        {
            result = new SmartroPacket();

            if (packet == null || length < HEADER_SIZE + 2)
                return false;

            if (packet[0] != STX)
                return false;

            int bodyLength = packet[33] | (packet[34] << 8);
            int totalLength = HEADER_SIZE + bodyLength + 2;

            if (length != totalLength)
                return false;

            if (packet[HEADER_SIZE + bodyLength] != ETX)
                return false;

            byte recvBcc = packet[HEADER_SIZE + bodyLength + 1];
            byte calcBcc = CalcBcc(packet, 0, HEADER_SIZE + bodyLength);

#if RELEASE
            if (recvBcc != calcBcc)
                return false;
#endif

            result.TerminalId = GetText(packet, 1, 16);
            result.DateTimeText = GetText(packet, 17, 14);
            result.JobCode = packet[31];
            result.ResponseCode = packet[32];
            result.BodyLength = (ushort)bodyLength;

            if (bodyLength > 0) {
                result.Body = new byte[bodyLength];
                Buffer.BlockCopy(packet, HEADER_SIZE, result.Body, 0, bodyLength);
            }

            return true;
        }

        public static SmartroDeviceStatus ParseDeviceStatus(byte[] body)
        {
            return new SmartroDeviceStatus
            {
                CardModule = GetText(body, 0, 1),
                RfModule = GetText(body, 1, 1),
                VanServer = GetText(body, 2, 1),
                LinkServer = GetText(body, 3, 1)
            };
        }

        public static SmartroEventResponse ParseEvent(byte[] body)
        {
            return new SmartroEventResponse
            {
                EventCode = GetText(body, 0, 1)
            };
        }

        public static SmartroUidResponse ParseUid(byte[] body)
        {
            return new SmartroUidResponse
            {
                CardUid = GetText(body, 0, 10)
            };
        }

        public static SmartroApprovalResponse ParseApproval(byte[] body)
        {
            return new SmartroApprovalResponse
            {
                TradeType = GetText(body, 0, 1),
                MediaType = GetText(body, 1, 1),
                CardNo = GetText(body, 2, 20),
                Amount = ToInt(GetText(body, 22, 10)),
                Tax = ToInt(GetText(body, 32, 8)),
                ServiceCharge = ToInt(GetText(body, 40, 8)),
                Installment = GetText(body, 48, 2),
                ApprovalNo = GetText(body, 50, 12),
                SaleDate = GetText(body, 62, 8),
                SaleTime = GetText(body, 70, 6),
                TradeNo = GetText(body, 76, 12),
                MerchantNo = GetText(body, 88, 15),
                TerminalNo = GetText(body, 103, 14),
                IssuerInfo = GetText(body, 117, 20),
                AcquirerInfo = GetText(body, 137, 20)
            };
        }

        private static string GetText(byte[] data, int offset, int size)
        {
            if (data == null || data.Length < offset + size)
                return "";

            return _enc.GetString(data, offset, size).TrimEnd('\0', ' ');
        }

        private static int ToInt(string value)
        {
            return int.TryParse(value, out int ret) ? ret : 0;
        }

        private static byte CalcBcc(byte[] data, int start, int end)
        {
            byte bcc = 0;

            for (int i = start; i <= end; i++)
                bcc ^= data[i];

            return bcc;
        }
    }
}
