using APSMain;
using System.Drawing.Drawing2D;

namespace APSMain.BaseClass
{
    public class RoundButton : Button
    {
        private bool isHovered = false;
        public Color BorderColor { get; set; }
        public int BorderThickness { get; set; } = 6;
        public Image? IconImage { get; set; }
        public int IconSize { get; set; } = 48;  // 원하는 아이콘 크기, 필요시 자동계산
        public bool isHC { get; set; }

        public RoundButton()
        {
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            TabStop = true;
            //BackColor = fillColor = Color.FromArgb(16, 26, 46);
            //ForeColor = Color.White;
            TextAlign = ContentAlignment.MiddleCenter;
            DoubleBuffered = true;
            //SetStyle(ControlStyles.StandardClick | ControlStyles.StandardDoubleClick, true);
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_NCHITTEST = 0x0084;
            const int HTCLIENT = 1;

            if (m.Msg == WM_NCHITTEST) {
                m.Result = (IntPtr)HTCLIENT; // ★ 여기 한 줄로 모든 마우스가 아래 앱으로
                return;
            }

            base.WndProc(ref m);
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            base.OnPaint(pevent);

            Graphics g = pevent.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Color fillColor;
            
            // OnPaint 내부, fillColor 결정 부분 교체
            var cur = PointToClient(Cursor.Position);
            bool inside = Region?.IsVisible(cur) == true;

            if (inside && MouseButtons == MouseButtons.Left)
                fillColor = isHC ? Color.AliceBlue : Color.SkyBlue;   // 클릭 시
            else if (inside)
                fillColor = isHC ? Color.Gray : Color.LightBlue;       // 마우스 오버
            else if (isHovered)
                fillColor = isHC ? BackColor : Color.LightGray;
            else if (Focused)
                fillColor = Color.Gray;
            else
                fillColor = BackColor;

            using (Brush b = new SolidBrush(fillColor))
            using (GraphicsPath path = new GraphicsPath()) {
                path.AddEllipse(0, 0, Width, Height);
                g.FillPath(b, path);
            }

            // 테두리
            using (Pen pen = new Pen(FlatAppearance.BorderColor, FlatAppearance.BorderSize)) {
                g.DrawEllipse(pen, 0, 0, Width - 1, Height - 1);
            }

            // 아이콘 그리기
            if (IconImage != null) {
                int txtH = !string.IsNullOrEmpty(Text) ? Font.Height + 10 : 0;
                int availH = Height - txtH - 24;
                int availW = Width - 32;

                // 아이콘 크기 계산 (비율 유지, 최대 40~45% 영역)
                float scale = Math.Min((float)availW / IconImage.Width, (float)availH / IconImage.Height);
                int drawW = (int)(IconImage.Width * scale);
                int drawH = (int)(IconImage.Height * scale);

                int iconX = (Width - drawW) / 2;
                int iconY = (Height - txtH - drawH) / 2 + 4; // 살짝 위로 올림

                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.DrawImage(IconImage, iconX, iconY, drawW, drawH);
            }

            // 텍스트
            if (!string.IsNullOrEmpty(Text)) {
                int txtH = Font.Height + 6;
                var textRect = new Rectangle(0, 50, Width, txtH);
                TextRenderer.DrawText(g, Text, Font, textRect, ForeColor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            }

            if (Focused)
                ControlPaint.DrawFocusRectangle(g, ClientRectangle);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            using (GraphicsPath path = new GraphicsPath()) {
                path.AddEllipse(0, 0, Width, Height);
                Region = new Region(path);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            bool nowHovered = Region?.IsVisible(e.Location) == true;

            if (nowHovered != isHovered) {
                isHovered = nowHovered;
                Invalidate(); // 다시 그리기
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);

            // 마우스가 벗어나면 Hover 해제
            if (isHovered) {
                isHovered = false;
                Invalidate();
            }
        }
    }
}