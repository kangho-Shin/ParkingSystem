using APSMain.BaseClass;
using APSMain.TTSLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace APSMain
{
    public partial class PayMsgForm : Form
    {
        private System.Threading.Timer? _autoCloseTimer = null;
        public event Action? AutoClose;
        private IMainForm? _mainForm;

        private string[] pyMsg = {
            "신용카드결제가 완료 되었습니다.\n카드를 받아 주신 후\n홈 또는 출력 버튼을 눌러 주세요.",
            "카드를 인식 중입니다.\n카드를 빼지 말아 주세요.",
            "승인 처리 중입니다.\n잠시만 기다려 주세요.",
            "출차 유예시간 {10}분 내\n출차해 주세요. 감사합니다.",
            "결제 오류입니다.\n신용카드를 확인해 주십시요",
            "신용카드 방향을 확인 하신 후 \n다시 투입구에 넣어 주세요."
        };

        public PayMsgForm()
        {
            InitializeComponent();

            DoubleBuffered = true;
            FormBorderStyle = FormBorderStyle.None;

            _mainForm = APSConfig.ScreenMode == 15 ? APSConfig.mainForm15 : APSConfig.mainForm;
        }

        private void PayMsgFrom_FormClosed(object sender, FormClosedEventArgs e)
        {
            _autoCloseTimer?.Dispose();
            _autoCloseTimer = null;

            AutoClose?.Invoke();

        }
        private void PayMsgFrom_Load(object sender, EventArgs e)
        {
            BringToFront();

            if (APSConfig.isContrast == true) {
                this.BackgroundImage = Properties.Resources.payCancel1;
                lblMsg1.ForeColor = Color.White;
                lblMsg1.BackColor = Color.Black;
                lblMsg2.ForeColor = Color.White;
                lblMsg2.BackColor = Color.Black;
                lblMsg3.ForeColor = Color.White;
                lblMsg3.BackColor = Color.Black;
            }
        }

        private void CenterOverOwner()
        {
            Rectangle target;

            if (Owner != null && Owner.Visible && !Owner.IsDisposed) {
                var topLeft = Owner.PointToScreen(Point.Empty);
                target = new Rectangle(topLeft, Owner.Size);
            }
            else {
                target = Screen.FromPoint(Cursor.Position).WorkingArea; // 폴백
            }

            int x = target.Left + (target.Width - Width) / 2;
            int y = target.Top + (target.Height - Height) / 2;
            Location = new Point(x, y);
        }

        private void PayMsgFrom_Shown(object sender, EventArgs e)
        {
            CenterOverOwner();  // Owner 기준 중앙 정렬
        }

        public void ShowMessage(int nIndex)
        {
            if (nIndex < 0 || nIndex >= pyMsg.Length)
                return;
            string[] msg = pyMsg[nIndex].Split('\n');

            if( nIndex <= 2 ) {
                if (_mainForm != null) {
                    _mainForm?.PlaySoundFile($"Comfirm.mp3", AudioRouteState.Idle, 1);
                }
            }
            else {
                if (_mainForm != null) {
                    _mainForm?.PlaySoundFile($"Error.mp3", AudioRouteState.Idle, 1);
                }
            }
            Task.Delay(500).Wait();
             string mpname = $"credit{nIndex:D2}.mp3";
            if (_mainForm != null) {
                _mainForm?.PlaySoundFile(mpname, AudioRouteState.UsbActive, 0);
            }
            if (InvokeRequired) {
                BeginInvoke(new Action(() =>
                {
                    lblMsg1.Text = (msg.Length > 0) ? msg[0] : string.Empty;
                    lblMsg2.Text = (msg.Length > 1) ? msg[1] : string.Empty;
                    lblMsg3.Text = (msg.Length > 2) ? msg[2] : string.Empty;
                    if (!Visible)
                        Show(Owner);
                    BringToFront();
                }));
                return;
            }

            lblMsg1.Text = (msg.Length > 0) ? msg[0] : string.Empty;
            lblMsg2.Text = (msg.Length > 1) ? msg[1] : string.Empty;
            lblMsg3.Text = (msg.Length > 2) ? msg[2] : string.Empty;
            BringToFront();
        }

        public void MessageCover(string msg1, string msg2,string msg3)
        {
            if (InvokeRequired) {
                BeginInvoke(new Action(() => { lblMsg1.Text = msg1; lblMsg2.Text = msg2; if (!Visible) Show(Owner); BringToFront(); }));
            }
            else {
                lblMsg1.Text = msg1;
                lblMsg2.Text = msg2;
                lblMsg3.Text = msg3;
                if (!Visible)
                    Show(Owner);
                BringToFront();
            }
        }

        public void StartPayAutoClose(int ms)
        {
            _autoCloseTimer?.Dispose();  // 재사용 시 기존 타이머 정리
            _autoCloseTimer = new System.Threading.Timer(_ =>
            {
                try {
                    if (!IsDisposed && IsHandleCreated)
                        BeginInvoke(new Action(Close));  // UI 스레드에서 닫기
                }
                finally {
                    _autoCloseTimer?.Dispose();
                    _autoCloseTimer = null;
                }
            }, null, ms, Timeout.Infinite);  // 원샷
        }
    }
}
