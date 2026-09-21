using APSMain.BaseClass;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Windows.Media.Core;

namespace APSMain
{
    public partial class MessageForm : Form
    {
        private System.Threading.Timer? _autoCloseTimer = null;
        public event Action? AutoClose;

        private string _message = "";

        public string[] _messages = {
            "조회된 차량이 없습니다.\n차량번호를 다시 한번\n확인해 주십시요.",
            "무료 차량 입니다.\n안녕히 가십시오.",
            "사전 정산 차량 입니다.\n안녕히 가십시오.",
            "정기 차량입니다.\n안녕히 가십시오.",
            "일반차량 입니다.\n안녕히 가십시오.",
            "카카오 주차 차량 입니다.\n안녕히 가십시오.",
            "SKT T맵 주차 차량입니다.\n안녕히 가십시오.",
            "긴급 차량입니다.\n안녕히 가십시오.",
            "미인식 차량입니다.\n차량번호를 입력해 주십시오.",
            "",
            "정기차량에 대한 결제내용이\n없습니다.\n다시 확인해 주십시요.", // 10번
            "승인 처리 중입니다.\n잠시만 기다려 주세요.",
            "관리자를 호출중 입니다.\n잠시만 기다려 주세요.",
            "할인권 방향을 확인하신 후\n다시 넣어주세요.\n",
            "인식된 차량의 정보가\n서버에 없습니다.\n차량번호를 입력 하시거나\n관리자에게 문의 하십시오.",
            "",
            "",
            "",
            "",
            "",
            "",    //20 번
            "",
            "",
            "",
            "",
            "",
            "",
            ""
        };

        public MessageForm()
        {
            InitializeComponent();

            DoubleBuffered = true;
            FormBorderStyle = FormBorderStyle.None;
        }

        private void MessageForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            _autoCloseTimer?.Dispose();
            _autoCloseTimer = null;

            AutoClose?.Invoke();

        }
        private void MessageForm_Load(object sender, EventArgs e)
        {
            BringToFront();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            using var sf = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };

            e.Graphics.DrawString(
                _message ?? "",
                Font,
                Brushes.White,
                ClientRectangle,
                sf);
        }

        public void ShowMessage(string msg)
        {
            if (InvokeRequired) {
                BeginInvoke(new Action(() =>
                {
                    _message = msg;
                    Invalidate();
                    if (!Visible)
                        Show(Owner);
                    BringToFront();
                }));
                return;
            }

            _message = msg;
            Invalidate();
            if (!Visible)
                Show(Owner);
            BringToFront();
        }

        public void ShowMessage(int index)
        {
            if (index < 0 || index >= _messages.Length)
                return;

            ShowMessage(_messages[index]);
        }

        private void CenterOverOwner()
        {
            Rectangle target;

            if (Owner != null && Owner.Visible && !Owner.IsDisposed) {
                var topLeft = Owner.PointToScreen(Point.Empty);
                target = new Rectangle(topLeft, Owner.Size);
            }
            else {
                target = Screen.FromPoint(Cursor.Position).WorkingArea;
            }

            int x = target.Left + (target.Width - Width) / 2;
            int y = target.Top + (target.Height - Height) / 2;
            Location = new Point(x, y);
        }

        private void MessageForm_Shown(object sender, EventArgs e)
        {
            CenterOverOwner();
        }

        public void StartAutoClose(int ms)
        {
            _autoCloseTimer?.Dispose();

            _autoCloseTimer = new System.Threading.Timer(_ =>
            {
                try {
                    if (!IsDisposed && IsHandleCreated)
                        BeginInvoke(new Action(Close));
                }
                finally {
                    _autoCloseTimer?.Dispose();
                    _autoCloseTimer = null;
                }
            }, null, ms, Timeout.Infinite);
        }
    }
}
