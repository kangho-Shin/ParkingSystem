using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics.Printing.PrintTicket;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace APSMain.Tcpip
{
    public enum GateCmd
    {
        DATA=0,
        GATEOPEN,
        GATECLOSE,
        GATEOPENLOCK,
        GATEUNLOCK,
        GATERESET,
        GATEDETRESET,
        DETECTORRESET
    }

    public class LDMDisplayManager
    {
        public LDMDisplayClient[] _displays = new LDMDisplayClient[4];

        public event Action<int, byte[]>? MessageReceived;
        public event Action<int>? Disconnected;
        private static readonly byte[] GATE_OPEN       = { 0x02, 0xFF, 0xC1, 0x30, 0x35, 0x39, 0x03 };
        private static readonly byte[] GATE_RESET      = { 0x02, 0xFF, 0xC5, 0x30, 0x30, 0x38, 0x03 };
        private static readonly byte[] PANEL_RESET     = { 0x02, 0xFF, 0xAA, 0x52, 0x45, 0x53, 0x54, 0x41, 0x52, 0x54, 0x20, 0x20, 0x03 };
        private static readonly byte[] GATE_OPEN_LOCK  = { 0x02, 0xFF, 0xC1, 0x39, 0x39, 0x03 };
        private static readonly byte[] GATE_CLOSE      = { 0x02, 0xFF, 0xC2, 0x30, 0x30, 0x03 };
        private static readonly byte[] GATE_STATE      = { 0x02, 0xFF, 0xC4, 0x30, 0x34, 0x03 };
        private static readonly byte[] GATE_UNLOCK     = { 0x02, 0xFF, 0xC3, 0x30, 0x35, 0x03 };
        private static readonly byte[] DETECTOR_RESET  = { 0x02, 0xFF, 0xC9, 0x30, 0x30, 0x03 };
        private static readonly byte[] GATE_DET_RESET  = { 0x02, 0xFF, 0xC6, 0x30, 0x30, 0x03 };
        private static readonly byte[] LDM_RESET       = { 0x02, 0xFF, 0xA5, 0x30, 0x68, 0x03 };

        private static readonly string FFF = $"{(char)Constants.ASCII_DLE}{(char)(0xFF - 0x80)}";
        private static readonly Encoding Ksc949 = Encoding.GetEncoding(949);

        private readonly System.Threading.Timer?[] _reconnectTimers = new System.Threading.Timer?[4];

        public LDMDisplayManager(string[] ips, int[] port)
        {
            for ( int i = 0 ; i < 4 ; i++ )
            {
                if ( !string.IsNullOrEmpty(ips[i]) )
                { 
                    _displays[i] = new LDMDisplayClient(ips[i], port[i]);
                    Console.WriteLine($"LDM IP {ips[i]} Conecting....");
                    int idx = i;
                    _displays[i].MessageReceived += data => MessageReceived?.Invoke(idx, data);
                    _displays[i].Onconnected += (ip) =>
                    {
                        Console.WriteLine($"LDM IP {ip} Conect OK");
                    };
                    _displays[i].Disconnected += (ip) =>
                    {
                        Console.WriteLine($"LDM IP {ip} DisConected");
                        Disconnected?.Invoke(idx);
                        ReconnectAsync(idx);
                    };
                    _displays[i].SocketConnect();
                }
            }
        }

        private void ReconnectAsync(int index)
        {
            if (index < 0 || index >= _displays.Length)
                return;

            if (_displays[index] == null)
                return;

            if (_reconnectTimers[index] != null)
                return;

            Console.WriteLine($"[LDM {_displays[index]._ip}] 재접속 대기...");

            _reconnectTimers[index] = new System.Threading.Timer(_ =>
            {
                _reconnectTimers[index]?.Dispose();
                _reconnectTimers[index] = null;

                if (_displays[index] != null && !_displays[index].IsConnected) {
                    Console.WriteLine($"[LDM {_displays[index]._ip}] 재접속 시도...");
                    _displays[index].SocketConnect();
                }

            }, null, 3000, Timeout.Infinite);
        }

        public void ConnectAllAsync()
        {
            foreach (var d in _displays)
                d.SocketConnect();
        }

        public void SendData(int index, byte[] data)
        {
            if (index < 0 || index >= _displays.Length)
                return;

            var client = _displays[index];

            if (client == null) {
                Console.WriteLine($"LDM[{index}] client null");
                return;
            }

            client.SendPacket(data);
        }

        public void ConvertCheckSumLDMPacket(byte[] LDMBuff, int Length)
        {
            int ii, xLength;
            byte XOR = 0x00;
            for (ii = 0; ii < Length; ii++)
            {
                if (LDMBuff[ii] == 0x79)
                    LDMBuff[ii] = 0xFF;
            }
            xLength = Length - 2;
            for (ii = 0; ii < xLength; ii++)
            {
                XOR ^= LDMBuff[ii];
            }
            LDMBuff[xLength] = XOR;
        }

        private int strldmlen(string ldmText)
        {
            int i, xLen, slen, delcnt;

            xLen = ldmText.Length;
            slen = delcnt = 0;
            for (i = 0, delcnt = 0; i < xLen; i++) {
                if (ldmText[i] == '^')
                    delcnt++;
                slen += ldmText[i] > 127 ? 2 : 1;
            }

            return slen - delcnt * 2;
        }

        public void LDMDisplayOneLineSend(int ldmNum,string ldmText, char memory, int xLDMLine,  int rotate, int timeView)
        {
            Span<byte> buffer = stackalloc byte[512];
            byte[] LDMPacket;
            int offset = 0;

            buffer[offset++] = Constants.ASCII_STX;
            buffer[offset++] = 0xFF;
            buffer[offset++] = Constants.ASCII_ESC;
            buffer[offset++] = (byte)memory;
            buffer[offset++] = (byte)(0x30 + timeView);
            buffer[offset++] = (byte)'M';
            buffer[offset++] = (byte)'0';
             buffer[offset++] = (byte)(0x30+xLDMLine);
            if (strldmlen(ldmText) > 12)
                buffer[offset++] = (byte)'1';
            else
                buffer[offset++] = (byte)'0';

            byte[] bytes = Encoding.GetEncoding("ks_c_5601").GetBytes(ldmText);
            bytes.CopyTo(buffer[offset..]);
            offset += bytes.Length;
            buffer[offset++] = 0xff;
            buffer[offset++] = 0x20;
            buffer[offset++] = Constants.ASCII_ETX;
            LDMPacket = buffer[..offset].ToArray();
            for (int i = 0; i < LDMPacket.Length; i++)
            {
                if (LDMPacket[i] == '^')
                    LDMPacket[i] = 0x10;
            }
            ConvertCheckSumLDMPacket(LDMPacket, LDMPacket.Length);
            try
            {
                SendData(ldmNum, LDMPacket);
            }
            catch (Exception ex)
            {
                Console.WriteLine("LDM{0}  : {1}", ldmNum, ex.Message);
            }
        }

        public void LDMDisplayDataSend(int ldmNum,string ldmText1, string ldmText2, char memory, int rotate, int timeView)
        {
            Span<byte> buffer = stackalloc byte[512];
            byte[] LDMPacket;
            int offset = 0;

            buffer[offset++] = Constants.ASCII_STX;
            buffer[offset++] = 0xFF;
            buffer[offset++] = Constants.ASCII_ESC;
            buffer[offset++] = (byte)memory;
            buffer[offset++] = (byte)(0x30 + timeView);
            buffer[offset++] = (byte)'M';
            buffer[offset++] = (byte)'0';
            buffer[offset++] = (byte)'1';

            if (strldmlen(ldmText1) > 12)
                buffer[offset++] = (byte)'1';
            else
                buffer[offset++] = (byte)'0';

            var bytes = Encoding.GetEncoding("ks_c_5601").GetBytes(ldmText1);
            bytes.CopyTo(buffer[offset..]);
            offset += bytes.Length;
            buffer[offset++] = 0xff;

            buffer[offset++] = (byte)'2';
            if (strldmlen(ldmText2) > 12)
                buffer[offset++] = (byte)'1';
            else
                buffer[offset++] = (byte)'0';

            bytes = Encoding.GetEncoding("ks_c_5601").GetBytes(ldmText2);
            bytes.CopyTo(buffer[offset..]);
            offset += bytes.Length;
            buffer[offset++] = 0xff;
            buffer[offset++] = 0x20;
            buffer[offset++] = Constants.ASCII_ETX;
            LDMPacket = buffer[..offset].ToArray();
            for (int i = 0; i < LDMPacket.Length; i++)
            {
                if (LDMPacket[i] == '^')
                    LDMPacket[i] = 0x10;
            }

            ConvertCheckSumLDMPacket(LDMPacket, LDMPacket.Length);

            try
            {
                SendData(ldmNum, LDMPacket);
            }
            catch (Exception ex)
            {
                Console.WriteLine("LDM{0}  : {1}", ldmNum, ex.Message);
            }
        }

        private byte[] ConvertAsciiLDMPacket(byte[] calPacket, int length)
        {
            int ii, kk;

            byte[] buff = new byte[length];
            Array.Clear(buff, 0, length);
            for (ii = 0, kk = 0; ii < length; ii++)  //'^'
            {
                if (calPacket[ii] == Constants.ASCII_DLE)
                {
                    buff[kk++] = (byte)(calPacket[ii + 1] + 0x80);
                    ii++;
                }
                else if (calPacket[ii] == '^')
                {
                    buff[kk++] = 0x10;
                }
                else
                {
                    buff[kk++] = calPacket[ii];
                }
            }
            byte[] LDMPacket = new byte[kk];
            Array.Copy(buff, LDMPacket, kk);
            ConvertCheckSumLDMPacket(LDMPacket, LDMPacket.Length);
            return LDMPacket;
        }

        public void GateCommand(int ldmNum, GateCmd cmd)
        {
            Console.WriteLine($"GATE[{ldmNum}] : {cmd}");
            try
            {
                switch (cmd)
                {
                    case GateCmd.GATEOPEN :
                        SendData(ldmNum, GATE_OPEN);
                        break;
                    case GateCmd.GATECLOSE :
                        SendData(ldmNum, GATE_CLOSE);
                        break;
                    case GateCmd.GATEOPENLOCK :
                        SendData(ldmNum, GATE_OPEN_LOCK);
                        break;
                    case GateCmd.GATEUNLOCK :
                        SendData(ldmNum, GATE_UNLOCK);
                        break;
                    case GateCmd.GATERESET :
                        SendData(ldmNum, GATE_RESET);
                        break;
                    case GateCmd.GATEDETRESET :
                        SendData(ldmNum, GATE_DET_RESET);
                        break;
                    case GateCmd.DETECTORRESET :
                        SendData(ldmNum, DETECTOR_RESET);
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("LDM{0}  : {1}", ldmNum, ex.Message);
            }
        }

        public void LDMReset(int ldmNum)
        {
            SendData(ldmNum, LDM_RESET);
        }
    }
}
