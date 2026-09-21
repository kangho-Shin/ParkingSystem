using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace APSMain.BaseClass
{
    public partial class MagnifyForm : Form
    {
        private readonly string _text;
        private readonly Font _font;
        private readonly int _autoHideMs;
        private System.Windows.Forms.Timer? _autoTimer;

        public MagnifyForm(string text, Font baseFont, float scale)
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            TopMost = true;
            ShowInTaskbar = false;
            BackColor = Color.LightYellow;
            DoubleBuffered = true;

            _text = text.Replace("\\n", "").Replace("\r", "").Replace("\n", " ");
            _font = new Font(baseFont.FontFamily, baseFont.Size * scale, FontStyle.Bold);

            // ★ 자동 사라짐 기본값(원하면 값만 바꾸면 됨)
            _autoHideMs = 1000;

            var sz = MeasureTextSize(_text, _font);
            ClientSize = new Size((int)sz.Width, (int)sz.Height);
        }

        public void MagnifyForm_Load(object sender, EventArgs e)
        {
            this.Disposed += (_, __) => _font.Dispose();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var rect = ClientRectangle;
            TextRenderer.DrawText(
                e.Graphics, _text, _font, rect, Color.Black,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine
            );
        }

        private static Size MeasureTextSize(string text, Font font)
        {
            // TextRenderer 기준으로 실제 표시와 일치
            return TextRenderer.MeasureText(text, font, new Size(int.MaxValue, int.MaxValue),
                TextFormatFlags.SingleLine);
        }

        public void ShowNear(Control target)
        {
            if (!IsAlive(target))
                return;

            // ListView/복잡 컨트롤은 컬럼 클릭 직후 핸들이 흔들려서 한 틱 지연
            if (target is ListView || target.RecreatingHandle) {
                target.BeginInvoke(new Action(() => ShowNearSafe(target)));
            }
            else {
                ShowNearSafe(target);
            }
        }

        private void ShowNearSafe(Control target)
        {
            if (!IsAlive(target))
                return;

            Rectangle rScreen;
            var wa = Screen.FromHandle(target.Handle).WorkingArea;

            if (target is ListView lv) {
                // ★ 리스트뷰: 리스트 "왼쪽 정렬" + 히트된 항목(첫 컬럼) 중앙 Y에 맞춰 배치
                var cur = Control.MousePosition;
                var pt = lv.PointToClient(cur);
                var hit = lv.HitTest(pt);

                Rectangle r;
                if (hit?.Item != null) {
                    // 첫 컬럼(Label) 기준
                    r = hit.Item.GetBounds(ItemBoundsPortion.Label);
                }
                else {
                    // 히트 실패 시 커서 근처
                    r = new Rectangle(pt, new Size(1, 1));
                }

                int listLeftXScreen = lv.PointToScreen(Point.Empty).X;
                int itemCenterYScreen = lv.PointToScreen(new Point(r.Left + r.Width / 2, r.Top + r.Height / 2)).Y;

                int x = listLeftXScreen;
                int y = itemCenterYScreen - this.Height / 2;

                // 화면 클램프
                x = Math.Max(wa.Left, Math.Min(x, wa.Right - this.Width));
                y = Math.Max(wa.Top, Math.Min(y, wa.Bottom - this.Height));

                this.Location = new Point(x, y);
            }
            else {
                // 일반 컨트롤: 기존 “왼쪽-아래” 맞춤 로직 유지
                Rectangle rClient = target.ClientRectangle;
                try {
                    rScreen = target.RectangleToScreen(rClient); // 안전한 좌표 변환
                }
                catch (ObjectDisposedException) { return; }

                var screenPos = target.PointToScreen(Point.Empty);
                var belowLeft = target.PointToScreen(new Point(0, target.Height));
                int x = belowLeft.X;
                int y = belowLeft.Y - this.Height;

                if (y < wa.Top) {
                    y = screenPos.Y + target.Height + 10;
                }

                if (x + this.Width > wa.Right) {
                    x = wa.Right - this.Width - 10;
                    if (x < wa.Left)
                        x = wa.Left;
                }

                this.Location = new Point(x, y);
            }

            // 오너 지정: 부모 폼 닫히면 같이 정리
            var owner = target.FindForm();
            if (IsAlive(owner))
                Show(owner!);
            else
                Show();

            // 타겟이 파괴되면 팝업 자동 종료
            target.HandleDestroyed -= Target_HandleDestroyed;
            target.HandleDestroyed += Target_HandleDestroyed;
            target.Disposed -= Target_Disposed;
            target.Disposed += Target_Disposed;

            // ★ 자동 사라짐 시작
            StartAutoHide();
        }

        private void StartAutoHide()
        {
            if (_autoHideMs <= 0)
                return;

            _autoTimer?.Stop();
            _autoTimer?.Dispose();
            _autoTimer = new System.Windows.Forms.Timer { Interval = _autoHideMs };
            _autoTimer.Tick += (s, e) =>
            {
                _autoTimer?.Stop();
                _autoTimer?.Dispose();
                _autoTimer = null;
                SafeClose();
            };
            _autoTimer.Start();
        }

        private static bool IsAlive(Control? c) =>
            c != null && !c.IsDisposed && c.IsHandleCreated && c.Visible;

        private void Target_HandleDestroyed(object? s, EventArgs e) => SafeClose();
        private void Target_Disposed(object? s, EventArgs e) => SafeClose();

        private void SafeClose()
        {
            if (IsHandleCreated)
                BeginInvoke(new Action(() => { try { Close(); } catch { } }));
        }
    }
}
