using APSMain.BaseClass;
using APSMain.Tcpip;
using APSMain.TTSLib;

namespace APSMain
{
    public partial class LDMShow : Form
    {
        private IMainForm? _mainForm=null;

        public LDMShow()
        {
            _mainForm = APSConfig.ScreenMode == 15 ? APSConfig.mainForm15 : APSConfig.mainForm;

            InitializeComponent();
        }

        private void LDMShow_Load(object sender, EventArgs e)
        {
            Dictionary<int, string> items = new Dictionary<int, string>();

            if (_mainForm != null) {
                if (_mainForm._displays != null) {
                    for (int i = 0; i < 4; i++) {
                        if (_mainForm._displays._displays[i] != null) {
                            items.Add(i, _mainForm._displays._displays[i]._ip);
                        }
                    }
                    try {
                        cbLDMList.DataSource = new BindingSource(items, null);
                        cbLDMList.DisplayMember = "Value";   // 표시될 항목
                        cbLDMList.ValueMember = "Key";       // 내부값
                    }
                    catch { }
                }
            }
            ldmText.Text = "^W오늘도 즐거운 하루 되세요.~~~~~~~~~";
        }

        private void btnSendData_Click(object sender, EventArgs e)
        {
            int line, shift;
            char memory = 'I';

            line = shift = 0;

            if (cbLDMList.SelectedIndex >= 0) {
                if (cbLDMList.SelectedItem != null) {
                    int ldmNum = ((KeyValuePair<int, string>)cbLDMList.SelectedItem).Key;

                    if (rdLine1.Checked)
                        line = 1;
                    else if (rdLine2.Checked)
                        line = 2;

                    if (rdMemory1.Checked)
                        memory = 'E';
                    else if (rdMemory2.Checked)
                        memory = 'I';

                    if (rdShift1.Checked)
                        shift = 0;
                    else if (rdShift2.Checked)
                        shift = 1;

                    if ( _mainForm != null && _mainForm._displays != null) {
                        _mainForm._displays.LDMDisplayOneLineSend(ldmNum, ldmText.Text, memory, line, shift, 7);
                    }
                }
            }

        }

        private void btnCancle_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnGateOpen_Click(object sender, EventArgs e)
        {
            GateControl(GateCmd.GATEOPEN);
        }

        private void btnGateClose_Click(object sender, EventArgs e)
        {
            GateControl(GateCmd.GATECLOSE);
        }

        private void btnDetReset_Click(object sender, EventArgs e)
        {
            GateControl(GateCmd.GATEDETRESET);
        }

        private void btnGateReset_Click(object sender, EventArgs e)
        {
            GateControl(GateCmd.GATERESET);
        }

        private void GateControl(GateCmd cmd)
        {
            if (cbLDMList.SelectedIndex >= 0) {
                if (cbLDMList.SelectedItem != null) {
                    int ldmNum = ((KeyValuePair<int, string>)cbLDMList.SelectedItem).Key;

                    if ( _mainForm != null && _mainForm._displays != null) {
                        _mainForm._displays.GateCommand(ldmNum, cmd);
                    }
                }
            }
        }
    }
}
