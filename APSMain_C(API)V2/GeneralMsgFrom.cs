using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Text;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace APSMain
{
    public partial class GeneralMsgFrom : Form
    {
        private System.Threading.Timer? _autoCloseTimer;

        public event Action? AutoClose;
        private int _dragLastY = 0;
        private bool _dragging = false;

        public GeneralMsgFrom()
        {
            InitializeComponent();

            BackColor = Color.Black;
            ForeColor = Color.White;
            DoubleBuffered = true;
        }

        private void PayMsgForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            _autoCloseTimer?.Dispose();
            _autoCloseTimer = null;

            AutoClose?.Invoke();
        }

        private void PayMsgForm_Load(object sender, EventArgs e)
        {
            StartPosition = FormStartPosition.Manual;

            // MenuForm을 오너로 등록하면 Z-Order/포커스가 안정적
            if (APSConfig.menuForm != null && !APSConfig.menuForm.IsDisposed)
                APSConfig.menuForm.AddOwnedForm(this);

            CenterOverMenu();   // 최초 위치

            string filePath = @"./Helps/설명1.rtf";
            if (File.Exists(filePath)) {
                rtb.LoadFile(filePath, RichTextBoxStreamType.RichText);
            }
            else {
                rtb.Text = "설명 파일이 없습니다.";
            }

            rtb.SelectionChanged += (s, e) => {
                rtb.SelectionLength = 0;
            };

            rtb.MouseDown += (s, e) =>
            {
                rtb.DeselectAll();
                if (e.Button == MouseButtons.Left) {
                    _dragLastY = e.Y;
                    _dragging = true;
                    rtb.Cursor = Cursors.Hand;
                }
            };
            rtb.MouseMove += (s, e) =>
            {
                if (_dragging) {
                    int dy = e.Y - _dragLastY;
                    _dragLastY = e.Y;

                    // 현재 첫 줄 인덱스 계산
                    int firstLine = rtb.GetLineFromCharIndex(rtb.GetCharIndexFromPosition(new Point(1, 1)));
                    int linesToMove = -(dy / 24); // 민감도(24=폰트높이)

                    int targetLine = Math.Max(0, firstLine + linesToMove);

                    // 원하는 줄로 스크롤
                    int charIdx = rtb.GetFirstCharIndexFromLine(targetLine);
                    if (charIdx >= 0) {
                        rtb.SelectionStart = charIdx;
                        rtb.ScrollToCaret();
                    }
                }
            };
            rtb.MouseUp += (s, e) =>
            {
                _dragging = false;
                rtb.Cursor = Cursors.Default;
            };
        }

        public void CenterOverMenu()
        {
            // 대상 영역: MenuForm의 스크린 좌표 사각형
            Rectangle target;
            var menu = APSConfig.menuForm;

            if (menu != null && menu.Visible && !menu.IsDisposed) {
                var topLeft = menu.PointToScreen(Point.Empty);
                target = new Rectangle(topLeft, menu.Size);
            }
            else {
                // 폴백: 현재 커서가 있는 모니터의 작업영역 중앙
                target = Screen.FromPoint(Cursor.Position).WorkingArea;
            }

            int x = target.Left + (target.Width - Width) / 2;
            int y = target.Top + (target.Height - Height) / 2;

            // 목표 영역 밖으로 튀지 않도록 클램프(옵션)
            var work = Screen.FromRectangle(target).WorkingArea;
            x = Math.Max(work.Left, Math.Min(x, work.Right - Width));
            y = Math.Max(work.Top, Math.Min(y, work.Bottom - Height));

            Location = new Point(x, y);
        }


        private void PayMsgForm_Shown(object sender, EventArgs e)
        {
            CenterOverMenu();   // 글자/오토레이아웃 후 한 번 더 보정

 //           StartAutoClose(3000);   // 3초
        }

        public void StartAutoClose(int ms)
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
