using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APSMain.BaseClass
{
    public enum ButtonRole
    {
        Receipt,
        ParkingFee,
        SeasonPass
    }

    public class CTButton : Button
    { 
        public Color ActiveBack  { get; set; }
        public Color HoverBack   { get; set; }
        public Color PressedBack { get; set; }
        public Color BorderColor { get; set; } 
        private bool isHovered = false;
        private bool isPressed = false;
        public bool HighContrast { get; set; }
        public int BorderThickness { get; set; }
        public Image? IconImage { get; set; }
        public int IconSize { get; set; } = 128; 

        private ButtonRole _theme = ButtonRole.ParkingFee;

        public ButtonRole Theme
        {
            get => _theme;
            set
            {
                _theme = value;
                //ApplyTheme(_theme);
                //Invalidate(); // 바로 적용
            }
        }

        public CTButton()
        {
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 6;
            Font = new Font("맑은 고딕", 40, FontStyle.Bold);
            //ForeColor = Color.FromArgb(17, 17, 17);
            //BackColor = ActiveBack;
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

        public void SetHighContrast(bool on)
        {
            HighContrast = on;
            Invalidate();
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            isHovered = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            isHovered = false;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs mevent)
        {
            base.OnMouseDown(mevent);
            isPressed = true;
            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs mevent)
        {
            base.OnMouseUp(mevent);
            isPressed = false;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            //base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit; // ★ 부드럽게

            bool isHC = HighContrast || SystemInformation.HighContrast;

            Color baseBack = !ActiveBack.IsEmpty ? ActiveBack : BackColor;

            //Color baseBack = !ActiveBack.IsEmpty ? ActiveBack : BackColor;
            //Color pressedCol = !PressedBack.IsEmpty ? PressedBack : ControlPaint.Dark(baseBack);
            Color borderCol = !BorderColor.IsEmpty ? BorderColor : ControlPaint.DarkDark(baseBack);

            if (APSConfig.isContrast) {
                borderCol = Color.White;
            }
            

            //Color fillColor;
            //if (isHC)
            //    fillColor = isPressed ? pressedCol : (isHovered || Focused) ? Color.Gray : baseBack;
            //else
            //    fillColor = isPressed ? pressedCol : (isHovered || Focused) ? Color.FromArgb(46, 164, 230) : baseBack;

            // 눌렀을 때도 일단 같은 색 쓰게 강제
            Color pressedCol = baseBack;    // ★ 테스트용: 눌러도 색 안 바뀌게

            Color fillColor;
            if (isHC)
                fillColor = isPressed ? pressedCol : (isHovered || Focused) ? Color.Gray : baseBack;
            else
                fillColor = isPressed ? pressedCol : (isHovered || Focused) ? Color.FromArgb(46, 164, 230) : baseBack;

            using (var b = new SolidBrush(fillColor))
                g.FillRectangle(b, ClientRectangle);


            int totalHNormal = 0;

            int textH = (int)Math.Ceiling(Font.GetHeight(g)) + 6;
            totalHNormal = IconSize + 60;

            int topNormal = 30;
            // 아이콘
            if (IconImage != null) {
                int iconX = (Width - IconSize) / 2;
                int iconY = 30;

                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.DrawImage(IconImage, iconX, iconY, IconSize, IconSize);
                topNormal += IconSize+30;
            }

            // 텍스트
            if (Text.Length > 0) {
                string txt = Text.Replace(@"\n", "\n");
                int lineCount = txt.Split('\n').Length;

                // 한 줄 높이
                int lineH = (int)Math.Ceiling(Font.GetHeight(g)) + 6;
                int rectH = lineH * lineCount + 20;        // ★ 줄 수만큼 높이 확보

                int offsetY = (lineCount == 1) ? (topNormal + 40) : topNormal;

                var rect = new Rectangle(0, offsetY, Width, rectH);

                using (var sf = new StringFormat())
                using (var br = new SolidBrush(ForeColor)) {
                    sf.Alignment = StringAlignment.Center;
                    sf.LineAlignment = StringAlignment.Near;
                    sf.FormatFlags = StringFormatFlags.LineLimit;

                    // TextRenderingHint 설정을 따르는 GDI+ 방식
                    g.DrawString(txt, Font, br, rect, sf);   // ★ TextRenderer 대신 DrawString
                }
            }
            // 테두리
            Color bColor = (isHovered || Focused) ? Color.Red : borderCol;
            // 테두리
            if (APSConfig.isContrast) {
                bColor = (isHovered || Focused) ? Color.Yellow : borderCol;
            }
            using (var p = new Pen(bColor, BorderThickness))
                g.DrawRectangle(p, new Rectangle(3, 3, Width - 7, Height - 7));

            if (Focused)
                ControlPaint.DrawFocusRectangle(g, ClientRectangle);
        }

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            //_iconFont?.Dispose();
            //_iconFont = ResolveIconFont(IconSizePt);
        }

        protected override void Dispose(bool disposing)
        {
            //if (disposing)
            //    _iconFont?.Dispose();
            base.Dispose(disposing);
        }
    }
}

