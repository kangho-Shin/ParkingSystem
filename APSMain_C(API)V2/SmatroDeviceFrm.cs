using APSMain.BaseClass;
using APSMain.Comm;
using APSMain.DbModels;
using APSMain.Smatro;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using NAudio.CoreAudioApi;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace APSMain
{
    public partial class SmatroDeviceFrm : Form
    {
        public SMPAYDATA _smPayData = new SMPAYDATA();
        public SMDEVICEINFO _sminfoset = new SMDEVICEINFO();
        private IMainForm? _mainForm = null;

        public SmatroDeviceFrm()
        {
            InitializeComponent();

            _mainForm = APSConfig.ScreenMode == 15 ? APSConfig.mainForm15 : APSConfig.mainForm;
            if (_mainForm != null) {
                _mainForm.CardSetupEvent += CardSetupEvent;
            }
        }

        private void SmatroDeviceFrm_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (_mainForm != null) {
                _mainForm.CardSetupEvent -= CardSetupEvent;
            }
        }

        private void SmatroDeviceFrm_Load(object sender, EventArgs e)
        {
            txtCrdId.Text = APSConfig.SMTERMID;
            cbDeviceType.SelectedIndex = 0;
            cbDhcp.SelectedIndex = 0;
            cbSamSlot1.SelectedIndex = 6;
            cbSamSlot2.SelectedIndex = 6;
            cbSamSlot3.SelectedIndex = 6;
            cbSamSlot4.SelectedIndex = 6;
        }

        public void CardSetupEvent(int msgType, SmartroPacket packet)
        {
            if (packet.Body == null || packet.Body.Length <= 0)
                return;

            if (packet.JobCode != (byte)'y')
                return;

            byte[] xbody = packet.Body;

            for (int i = 0; i < xbody.Length; i++) {
                if (xbody[i] == 0xFF)
                    xbody[i] = 0x00;
            }

            BeginInvoke(() =>
            {
                int rxLen = 0;
                int index;
                string txtMsg;

                txtCrdId.Text = GetAsciiText(xbody, ref rxLen, 16);
                txtCrdIp.Text = GetAsciiText(xbody, ref rxLen, 16);
                txtCrdPort.Text = GetAsciiText(xbody, ref rxLen, 16);

                txtPreId.Text = GetAsciiText(xbody, ref rxLen, 16);
                txtPreIp.Text = GetAsciiText(xbody, ref rxLen, 16);
                txtPrePort.Text = GetAsciiText(xbody, ref rxLen, 16);

                txtKeyIp.Text = GetAsciiText(xbody, ref rxLen, 16);
                txtKeyPort.Text = GetAsciiText(xbody, ref rxLen, 16);

                txtAirIp.Text = GetAsciiText(xbody, ref rxLen, 16);
                txtAirPort.Text = GetAsciiText(xbody, ref rxLen, 16);

                txtMsg = GetSamType(xbody[rxLen++]);
                index = cbSamSlot1.FindStringExact(txtMsg);
                if (index >= 0)
                    cbSamSlot1.SelectedIndex = index;

                txtMsg = GetSamType(xbody[rxLen++]);
                index = cbSamSlot2.FindStringExact(txtMsg);
                if (index >= 0)
                    cbSamSlot2.SelectedIndex = index;

                txtMsg = GetSamType(xbody[rxLen++]);
                index = cbSamSlot3.FindStringExact(txtMsg);
                if (index >= 0)
                    cbSamSlot3.SelectedIndex = index;

                txtMsg = GetSamType(xbody[rxLen++]);
                index = cbSamSlot4.FindStringExact(txtMsg);
                if (index >= 0)
                    cbSamSlot4.SelectedIndex = index;

                index = ToComboIndex(xbody[rxLen++]);
                if (index >= 0 && index < cbDeviceType.Items.Count)
                    cbDeviceType.SelectedIndex = index;

                // 장치 IP 16 + 장치 PORT 16
                rxLen += 32;

                index = ToComboIndex(xbody[rxLen++]);
                if (index >= 0 && index < cbDhcp.Items.Count)
                    cbDhcp.SelectedIndex = index;

                txtEntIp.Text = GetAsciiText(xbody, ref rxLen, 16);
                txtEntSaubnet.Text = GetAsciiText(xbody, ref rxLen, 16);
                txtEntGateway.Text = GetAsciiText(xbody, ref rxLen, 16);
            });
        }

        private string GetAsciiText(byte[] data, ref int offset, int size)
        {
            if (data.Length < offset + size)
                return "";

            string text = Encoding.ASCII.GetString(data, offset, size).Trim('\0', ' ');
            offset += size;
            return text;
        }

        private int ToComboIndex(byte value)
        {
            if (value == 0x00 || value == 0xFF)
                return 0;

            if (value >= '0' && value <= '9')
                return value - '0';

            return 0;
        }

        private string GetSamType(byte value)
        {
            if (value == '0')
                return "후불";
            if (value == '1')
                return "티머니";
            if (value == '2')
                return "이비";
            if (value == '3')
                return "한페이";
            if (value == '4')
                return "유페이";
            if (value == '5')
                return "마이비";

            return "";
        }

        private void btnCancle_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private async void btnReadInfo_Click(object sender, EventArgs e)
        {
            if (_mainForm != null) {
                _mainForm.MakePacketData(Constants.CMD_TX_INFO_READ_V2, _smPayData);
                await Task.Delay(150);
            }
        }

        private async void btnSet_Click(object sender, EventArgs e)
        {
            _sminfoset.s_Crd_Id = txtCrdId.Text;
            _sminfoset.s_Crd_Ip = txtCrdIp.Text;
            _sminfoset.s_Crd_Port = txtCrdPort.Text;
            _sminfoset.s_Pre_Id = txtPreId.Text;
            _sminfoset.s_Pre_Ip = txtPreIp.Text;
            _sminfoset.s_Pre_Port = txtPrePort.Text;
            _sminfoset.s_Key_Ip = txtKeyIp.Text;
            _sminfoset.s_Key_Port = txtKeyPort.Text;
            _sminfoset.s_Air_Ip = txtAirIp.Text;
            _sminfoset.s_Air_Port = txtAirPort.Text;
            _sminfoset.i_Sam_Slot1 = cbSamSlot1.SelectedIndex;
            _sminfoset.i_Sam_Slot2 = cbSamSlot2.SelectedIndex;
            _sminfoset.i_Sam_Slot3 = cbSamSlot3.SelectedIndex;
            ;
            _sminfoset.i_Sam_Slot4 = cbSamSlot4.SelectedIndex;
            _sminfoset.i_Device_Type = cbDeviceType.SelectedIndex;
            //_sminfoset.i_Lcd_Value;
            //_sminfoset.i_Sound_Value;
            //_sminfoset.i_Touch_Value;
            //_sminfoset.b_Setup_Load;
            _sminfoset.i_Ent_Dhcp = cbDhcp.SelectedIndex;
            _sminfoset.s_Ent_Device_Ip = txtEntIp.Text;
            _sminfoset.s_Ent_Subnet = txtEntSaubnet.Text;
            _sminfoset.s_Ent_Gatway = txtEntGateway.Text;
            //_sminfoset.s_Dev_Port;
            //_sminfoset.s_Dev_Ip;

            if (_mainForm != null) {
                _mainForm.SMDeviceInfo.Copy(_sminfoset);
                _mainForm.MakePacketData(Constants.CMD_TX_INFO_SET_V2, _smPayData, _sminfoset);
                await Task.Delay(150);
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {

        }

        private void btnReset_Click(object sender, EventArgs e)
        {
            if (_mainForm != null) {
                _mainForm.SMDeviceInfo = _sminfoset;
                _mainForm.MakePacketData(Constants.CMD_TX_RESET, _smPayData);
            }
        }
    }
}
