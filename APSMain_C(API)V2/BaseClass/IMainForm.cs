using APSMain.BaseClass;
using APSMain.Comm;
using APSMain.DbModels;
using APSMain.Tcpip;
using APSMain.TTSLib;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Dapper;
using NAudio.Wave;
using Windows.Media.MediaProperties;
using APSMain.Smatro;

namespace APSMain.BaseClass
{
    public interface IMainForm
    {
        // 스마트로 전광판
        public event Action<int, SmartroPacket, SmartroApprovalResponse?>? CardCalculateEvent;
        public event Action<int, SmartroPacket, SmartroApprovalResponse?>? CardCancleEvent;
        public event Action<int, SmartroPacket>? CardSetupEvent;

        void SmatroWaiting();
        void MakePacketData(byte CMDMSG, SMPAYDATA payData,SMDEVICEINFO? info=null);

        SMDEVICEINFO SMDeviceInfo { get; set; }

        // 음성/사운드
        void PlaySoundFile(string mpname, AudioRouteState state, int Priority = 0);
        void PlayTextSpeech(string msg, AudioRouteState playType, int Priority = 0);
        void PlaySsmlSpeech(string msg, AudioRouteState playType, int Priority = 0);
        void AutoSoundPlay();

        // UI/메뉴 동작
        void ProcessArrow(Form form, Keys key, int startIndex, int endIndex);
        void ResetEndTabIndex();
        void MainFormRecover();
        void ParentFormRecover(bool zoomMode);

        // 전광판/주차
        public LDMDisplayManager? _displays { get; set; }
        void LDMTextDisplay(int ldmNum, int type, int dtime, string ltext1, string ltext2);
        void ParkCalFormDisplay();

        // 통신
        void UDPSendPacket(req_cmd_code PacketComm, byte[] MSG, int nLength, string desip);
        void SendPacketData(req_cmd_code PacketComm, STUDPDATA PacketData, int Size);

        void InvokeUI(Action action);
        void lelMessageChange(int xindex);
    }
}
