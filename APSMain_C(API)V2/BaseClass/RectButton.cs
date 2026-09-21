using APSMain;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APSMain.BaseClass
{
    public class RectButton : Button
    {
        private bool isHovered = false;
        public Color BorderColor { get; set; }
        public int   BorderThickness { get; set; } = 4;
        public Image? IconImage { get; set; }
        public int IconSize { get; set; } = 48;
        public bool isHC { get; set; }

        public RectButton()
        {
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 4;
            TabStop = true;
            TextAlign = ContentAlignment.MiddleCenter;
            DoubleBuffered = true;
        }

        private static bool InDesigner(IComponent c)
     => LicenseManager.UsageMode == LicenseUsageMode.Designtime
        || (c is Control ctrl && (ctrl.Site?.DesignMode ?? false));

        protected override void WndProc(ref Message m)
        {
            const int WM_NCHITTEST = 0x0084;
            const int HTCLIENT = 1;

            // ★ 디자이너에선 기본 동작 유지(선택/리사이즈 가능)
            if (m.Msg == WM_NCHITTEST && !InDesigner(this)) {
                m.Result = (IntPtr)HTCLIENT;
                return;
            }
            base.WndProc(ref m);
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            base.OnPaint(pevent);

            Graphics g = pevent.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            
            var cur = PointToClient(Cursor.Position);
            bool inside = ClientRectangle.Contains(cur);

            Color fillColor;
            if (inside && MouseButtons == MouseButtons.Left)
                fillColor = isHC ? Color.AliceBlue : Color.SkyBlue;
            else if (inside)
                fillColor = isHC ? Color.Gray : Color.FromArgb(14, 165, 233);
            else if (isHovered)
                fillColor = isHC ? BackColor : Color.FromArgb(14, 165, 233);
            else if (Focused)
                fillColor = Color.Gray;
            else
                fillColor = BackColor;

            using (Brush b = new SolidBrush(fillColor)) {
                g.FillRectangle(b, 0, 0, Width, Height);
            }

            if (isHovered || Focused) {
                if (APSConfig.isContrast) {
                    BorderColor = Color.Yellow;
                }
                else {
                    BorderColor = Color.Red;
                }
            }
            else {
                BorderColor = Color.White;  //FromArgb(255, 255, 255);
            }
            // 테두리 (기존과 동일: FlatAppearance 값 사용)
            using (Pen pen = new Pen(BorderColor, BorderThickness)) {
                g.DrawRectangle(pen, 2, 2, Width - 5, Height - 5);
            }

            // 아이콘
            if (IconImage != null) {
                int txtH = !string.IsNullOrEmpty(Text) ? Font.Height + 10 : 0;
                int availH = Height - txtH - 10;
                int availW = Width - 32;

                float scale = Math.Min((float)availW / IconImage.Width, (float)availH / IconImage.Height);
                int drawW = (int)(IconImage.Width * scale);
                int drawH = (int)(IconImage.Height * scale);

                int iconX = (Width - drawW) / 2;
                int iconY = (Height - txtH - drawH) / 2 + 4;

                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.DrawImage(IconImage, iconX, iconY, drawW, drawH);
            }

            // 텍스트
            if (!string.IsNullOrEmpty(Text)) {
                int txtH = Font.Height + 6;
                var textRect = new Rectangle(0, 55, Width, txtH);
                TextRenderer.DrawText(g, Text, Font, textRect, ForeColor,
                                       TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            }

            if (Focused)
                ControlPaint.DrawFocusRectangle(g, ClientRectangle);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            // 사각형: 별도 Region 필요 없음
            Region = new Region(ClientRectangle);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            bool nowHovered = ClientRectangle.Contains(e.Location);
            if (nowHovered != isHovered) {
                isHovered = nowHovered;
                Invalidate();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (isHovered) {
                isHovered = false;
                Invalidate();
            }
        }
    }
}
