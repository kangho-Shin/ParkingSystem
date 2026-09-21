using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APSMain.BaseClass
{
    public class CenterLabel : Label
    {
        public int OffsetY { get; set; } = -3;

        public CenterLabel()
        {
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            using var br = new SolidBrush(this.BackColor);
            e.Graphics.FillRectangle(br, this.ClientRectangle);

            Rectangle rect = this.ClientRectangle;
            rect.Y += OffsetY;

            TextRenderer.DrawText(
                e.Graphics,
                this.Text,
                this.Font,
                rect,
                this.ForeColor,
                ConvertAlign(this.TextAlign) |
                TextFormatFlags.SingleLine |
                TextFormatFlags.EndEllipsis
            );
        }

        private TextFormatFlags ConvertAlign(ContentAlignment align)
        {
            return align switch
            {
                ContentAlignment.TopLeft => TextFormatFlags.Left | TextFormatFlags.Top,
                ContentAlignment.TopCenter => TextFormatFlags.HorizontalCenter | TextFormatFlags.Top,
                ContentAlignment.TopRight => TextFormatFlags.Right | TextFormatFlags.Top,

                ContentAlignment.MiddleLeft => TextFormatFlags.Left | TextFormatFlags.VerticalCenter,
                ContentAlignment.MiddleCenter => TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter,
                ContentAlignment.MiddleRight => TextFormatFlags.Right | TextFormatFlags.VerticalCenter,

                ContentAlignment.BottomLeft => TextFormatFlags.Left | TextFormatFlags.Bottom,
                ContentAlignment.BottomCenter => TextFormatFlags.HorizontalCenter | TextFormatFlags.Bottom,
                ContentAlignment.BottomRight => TextFormatFlags.Right | TextFormatFlags.Bottom,

                _ => TextFormatFlags.Left | TextFormatFlags.Top
            };
        }
    }

    public sealed class PanLabel : Panel
    {
        private ContentAlignment _textAlign = ContentAlignment.MiddleCenter;
        private int _textYOffset = 0; // 미세 보정(+아래/-위)

        public PanLabel()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);

            DoubleBuffered = true;
            BackColor = Color.Transparent;   // 패널 투명 합성(부모에 그려짐)
            AutoSize = false;                // 기본: 고정 크기
            Padding = new Padding(0);
        }
        /*
                public override string Text
                {
                    get => base.Text! ?? "";
                    set { if (base.Text != value) { base.Text = value; Invalidate(); } }
                }
        */
        public new string Text
        {
            get => base.Text ?? "";
            set
            {
                string newText = value ?? "";
                if (base.Text != newText) {
                    base.Text = newText;
                    Invalidate();
                }
            }
        }

        public ContentAlignment TextAlign
        {
            get => _textAlign;
            set { if (_textAlign != value) { _textAlign = value; Invalidate(); } }
        }

        /// <summary>텍스트 수직 미세 보정(px). +는 아래로, -는 위로.</summary>
        public int TextYOffset
        {
            get => _textYOffset;
            set { if (_textYOffset != value) { _textYOffset = value; Invalidate(); } }
        }

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            if (AutoSize)
                Invalidate();
            else
                Invalidate();
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            // Panel의 BackgroundImage/BackgroundImageLayout 처리를 그대로 사용
            base.OnPaintBackground(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (string.IsNullOrEmpty(Text))
                return;

            // 그릴 영역
            var rc = ClientRectangle;
            rc = Rectangle.Inflate(rc, -Padding.Left, -Padding.Top);
            rc.Width -= (Padding.Right - Padding.Left);
            rc.Height -= (Padding.Bottom - Padding.Top);

            if (_textYOffset != 0)
                rc.Offset(0, _textYOffset);

            TextRenderer.DrawText(
                e.Graphics,
                Text,
                Font,
                AlignRect(rc, _textAlign),
                Enabled ? ForeColor : SystemColors.GrayText,
                GetFlags(_textAlign)
            );
        }

        public override Size GetPreferredSize(Size proposedSize)
        {
            var text = string.IsNullOrEmpty(Text) ? " " : Text;
            var sz = TextRenderer.MeasureText(text, Font, new Size(int.MaxValue, int.MaxValue),
                                              TextFormatFlags.NoPadding);
            sz = new Size(sz.Width + Padding.Horizontal, sz.Height + Padding.Vertical);
            return sz;
        }

        private static TextFormatFlags GetFlags(ContentAlignment align)
        {
            var flags = TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding;

            switch (align) {
                case ContentAlignment.TopLeft:
                    flags |= TextFormatFlags.Top | TextFormatFlags.Left;
                    break;
                case ContentAlignment.TopCenter:
                    flags |= TextFormatFlags.Top | TextFormatFlags.HorizontalCenter;
                    break;
                case ContentAlignment.TopRight:
                    flags |= TextFormatFlags.Top | TextFormatFlags.Right;
                    break;
                case ContentAlignment.MiddleLeft:
                    flags |= TextFormatFlags.VerticalCenter | TextFormatFlags.Left;
                    break;
                case ContentAlignment.MiddleCenter:
                    flags |= TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter;
                    break;
                case ContentAlignment.MiddleRight:
                    flags |= TextFormatFlags.VerticalCenter | TextFormatFlags.Right;
                    break;
                case ContentAlignment.BottomLeft:
                    flags |= TextFormatFlags.Bottom | TextFormatFlags.Left;
                    break;
                case ContentAlignment.BottomCenter:
                    flags |= TextFormatFlags.Bottom | TextFormatFlags.HorizontalCenter;
                    break;
                case ContentAlignment.BottomRight:
                    flags |= TextFormatFlags.Bottom | TextFormatFlags.Right;
                    break;
            }
            return flags;
        }

        private static Rectangle AlignRect(Rectangle rc, ContentAlignment align)
        {
            // TextRenderer가 정렬 플래그를 처리하지만, NoPadding일 때 시각적 균형을 위해 1px 보정
            return Rectangle.FromLTRB(rc.Left + 1, rc.Top + 1, rc.Right + 1, rc.Bottom + 1);
        }
    }
}
