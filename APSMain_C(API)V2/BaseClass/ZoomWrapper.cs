using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APSMain.BaseClass
{
    public class ZoomWrapper
    {
        private const int DragThreshold = 20;
        private const int ClickSuppressMs = 150;

        private Control _zoomContent;
        private Point _downScreen;          // 화면 좌표 기준
        private Point _contentStart;        // 드래그 시작 시 컨텐츠 위치
        private bool _panning;              // 현재 드래그 중인지
        private long _suppressClickUntil;   // 드래그 직후 클릭 무시 시간

        public event Action<bool>? PanStateChanged; // true: 시작, false: 종료
        public bool IsZoomed => APSConfig.isZoomed;
        public bool IsPanning => _panning;
        public bool IsInClickSuppress => Environment.TickCount64 < _suppressClickUntil;

        private Point _dragStart;
        private Point _pendingLoc;
        private bool _hasPending;
        private const int ScreenWidth = 1060;
        private const int ScreenHeight = 795;
        private const int ScreenDWidth = 2120;
        private const int ScreenDHeight = 1590;

        public ZoomWrapper(Control zoomContent)
        {
            _zoomContent = zoomContent;

            AttachDragEvents(_zoomContent);

            _pendingLoc = new Point(0, 0);
        }

        public void ToggleZoom()
        {
            ZoomInOut(APSConfig.isZoomed);
        }

        public void ZoomInOut(bool zoomIn)
        {
            using (RedrawScope.Suspend(_zoomContent)) // ← 그리는 동안 화면 갱신 정지
            {
                //Console.WriteLine($"Dock         {_zoomContent.Dock}");
                //Console.WriteLine($"Anchor       {_zoomContent.Anchor}");

                //Console.WriteLine($"FullName     {_zoomContent.GetType().FullName}");
                //Console.WriteLine($"AutoSize     {_zoomContent.AutoSize}");
                //Console.WriteLine($"MinimumSize  {_zoomContent.MinimumSize}");
                //Console.WriteLine($"MaximumSize  {_zoomContent.MaximumSize}");

                if (zoomIn) {
                    _zoomContent.Size = new Size(ScreenWidth, ScreenHeight);
                    ResizeControls(_zoomContent, 0.5f);
                    _zoomContent.Location = new Point(0, 0);
                    APSConfig.isZoomed = false;
                }
                else {
                    _zoomContent.Size = new Size(ScreenDWidth, ScreenDHeight);
                    ResizeControls(_zoomContent, 2.0f);
                    _zoomContent.Location = new Point(0, 0); 
                    APSConfig.isZoomed = true;
                }
            }
        }

        private void ResizeControls(Control parent, float scale)
        {
            _zoomContent.SuspendLayout();

            foreach (Control ctrl in parent.Controls) {
                // 이미 UI 스레드에서 동기 실행이므로 Invoke 불필요
                ctrl.Left = (int)(ctrl.Left * scale);
                ctrl.Top = (int)(ctrl.Top * scale);
                ctrl.Width = (int)(ctrl.Width * scale);
                ctrl.Height = (int)(ctrl.Height * scale);
                ctrl.Font = new Font(ctrl.Font.FontFamily, ctrl.Font.Size * scale, ctrl.Font.Style);

                if (ctrl is Button btn && btn.Image != null) {
                    int w = (int)(btn.Image.Width * scale);
                    int h = (int)(btn.Image.Height * scale);

                    if (w > 0 && h > 0) {
                        btn.Image = new Bitmap(btn.Image, new Size(w, h));
                    }
                }
                // DataGridView면 컬럼/행/헤더도 같이 확대
                if (ctrl is DataGridView dgv) {
                    // 컬럼 폭
                    foreach (DataGridViewColumn col in dgv.Columns)
                        col.Width = (int)(col.Width * scale);

                    // 행 높이
                    foreach (DataGridViewRow row in dgv.Rows)
                        row.Height = (int)(row.Height * scale);

                    if (dgv.RowTemplate.Height > 0)
                        dgv.RowTemplate.Height = (int)(dgv.RowTemplate.Height * scale);

                    // 헤더 높이
                    if (dgv.ColumnHeadersHeight > 0)
                        dgv.ColumnHeadersHeight = (int)(dgv.ColumnHeadersHeight * scale);

                    // ★ 헤더 폰트를 DataGridView.Font로 맞추기
                    dgv.ColumnHeadersDefaultCellStyle.Font = dgv.Font;
                    foreach (DataGridViewColumn col in dgv.Columns) {
                        col.HeaderCell.Style.Font = dgv.Font;
                    }
                }

                //// 자식 컨트롤 재귀 확대
                //if (ctrl.HasChildren)
                //    ResizeControls(ctrl, scale);
            }

            _zoomContent.ResumeLayout(true);
            _zoomContent.Invalidate();
            _zoomContent.Update();
        }

        private void AttachDragEvents(Control parent)
        {
            parent.MouseDown += ZoomContent_MouseDown;
            parent.MouseMove += ZoomContent_MouseMove;   // ★ 추가
            parent.MouseUp += ZoomContent_MouseUp;

            foreach (Control ctrl in parent.Controls) {
                ctrl.MouseDown += ZoomContent_MouseDown;
                ctrl.MouseMove += ZoomContent_MouseMove; // ★ 추가
                ctrl.MouseUp += ZoomContent_MouseUp;
            }
        }

        public void DetachDragEvents(Control parent)
        {
            parent.MouseDown -= ZoomContent_MouseDown;
            parent.MouseMove -= ZoomContent_MouseMove;
            parent.MouseUp -= ZoomContent_MouseUp;

            foreach (Control ctrl in parent.Controls) {
                ctrl.MouseDown -= ZoomContent_MouseDown;
                ctrl.MouseMove -= ZoomContent_MouseMove;
                ctrl.MouseUp -= ZoomContent_MouseUp;
            }
        }

        private void ZoomContent_MouseDown(object? sender, MouseEventArgs e)
        {
            _dragStart = e.Location;
            _downScreen = Control.MousePosition;
            _contentStart = _zoomContent.Location;

        }

        private void ZoomContent_MouseMove(object? sender, MouseEventArgs e)
        {
            if (!APSConfig.isZoomed || e.Button != MouseButtons.Left)
                return;

            var cur = Control.MousePosition;
            int dx = cur.X - _downScreen.X;
            int dy = cur.Y - _downScreen.Y;
            if (!_panning && Math.Abs(dx) < 5 && Math.Abs(dy) < 5)
                return;
            if (!_panning && (Math.Abs(dx) >= DragThreshold || Math.Abs(dy) >= DragThreshold)) {
                _panning = true;
                try { _zoomContent.Capture = true; }
                catch { }
                PanStateChanged?.Invoke(true);
            }

            if (_panning) {
                // 실시간 이동 (부드러운 패닝)
                int x = _contentStart.X + dx;
                int y = _contentStart.Y + dy;
                /*
                int minX = -(ScreenDWidth - ScreenWidth);
                int minY = -(ScreenDHeight - ScreenHeight);
                int maxX = 0, maxY = 0;

                x = Math.Max(minX, Math.Min(x, maxX));
                y = Math.Max(minY, Math.Min(y, maxY));
                */
                var (minX, minY, maxX, maxY) = GetPanBounds();

                x = Math.Max(minX, Math.Min(x, maxX));
                y = Math.Max(minY, Math.Min(y, maxY));



                // ★ 잔상 최소화: 이전/현재 합집합만 무효화(+여유 1~2px)
                var parent = _zoomContent.Parent;
                if (parent != null) {
                    Rectangle prev = new Rectangle(_zoomContent.Location, _zoomContent.Size);
                    Rectangle next = new Rectangle(new Point(x, y), _zoomContent.Size);
                    Rectangle u = Rectangle.Union(prev, next);
                    u.Inflate(2, 2);
                    parent.Invalidate(u, false); // 자식까지 무효화 금지
                }
                _zoomContent.Location = new Point(x, y);
            }
        }

        private void ZoomContent_MouseUp(object? sender, MouseEventArgs e)
        {
            if (!APSConfig.isZoomed)
                return;

            if (_panning) {
                _panning = false;
                _suppressClickUntil = Environment.TickCount64 + ClickSuppressMs;
                PanStateChanged?.Invoke(false);
                try { _zoomContent.Capture = false; }
                catch { }
            }
            else {
                if (APSConfig.isZoomed) {
                    Point upPoint = e.Location;
                    int dx = upPoint.X - _dragStart.X;
                    int dy = upPoint.Y - _dragStart.Y;

                    // 현재 content 위치
                    int x = _zoomContent.Location.X;
                    int y = _zoomContent.Location.Y;

                    // 드래그 방향에 따라 200px씩 이동
                    if (dx < -10)
                        x -= 200;
                    else if (dx > 10)
                        x += 200;

                    if (dy < -10)
                        y -= 200;
                    else if (dy > 10)
                        y += 200;
                    int minX = -(ScreenDWidth - ScreenWidth);
                    int minY = -(ScreenDHeight - ScreenHeight);
                    int maxX = 0;
                    int maxY = 0;

                    x = Math.Max(minX, Math.Min(x, maxX));
                    y = Math.Max(minY, Math.Min(y, maxY));
                    _zoomContent.Location = new Point(x, y);

                }
            }
            if (_hasPending)
                ApplyPendingPan();

            if (APSConfig.activeForm != APSConfig.menuForm) {
                APSConfig.activeForm?.DoActiveButton();
            }
        }

        private void ApplyPendingPan()
        {
            if (!_hasPending)
                return;
            _hasPending = false;

            var parent = _zoomContent.Parent;
            Rectangle? union = null;
            if (parent != null) {
                Rectangle prev = new Rectangle(_zoomContent.Location, _zoomContent.Size);
                Rectangle next = new Rectangle(_pendingLoc, _zoomContent.Size);
                union = Rectangle.Union(prev, next);
            }

            // ★ 핵심: 이동 구간만 무효화 + 중간 그리기 차단
            using (RedrawScope.Suspend(parent ?? _zoomContent)) {
                if (union.HasValue && parent != null)
                    parent.Invalidate(union.Value, false);  // 자식까지 무효화 금지

                _zoomContent.Location = _pendingLoc;
            }
        }

        private void PanTimer_Tick(object? sender, EventArgs e)
        {
            if (_panning && _hasPending)
                ApplyPendingPan();
        }

        private static bool Alive(Control? c) => c != null && !c.IsDisposed && c.IsHandleCreated && c.Visible;

        private Size GetHostSize()
        {
            var p = _zoomContent.Parent;
            return (p != null && p.IsHandleCreated) ? p.ClientSize : new Size(ScreenWidth, ScreenHeight);
        }

        private Point GetViewport()
        {
            return new Point(-_zoomContent.Left, -_zoomContent.Top);
        }

        // 패닝 가능한 Location 범위(콘텐츠 Location 기준)
        private (int minX, int minY, int maxX, int maxY) GetPanBounds()
        {
            Size host = GetHostSize();
            Size content = _zoomContent.Size;

            // 콘텐츠가 더 크면 음수로 이동 가능, 작거나 같으면 (0,0) 고정
            int minX = Math.Min(0, host.Width - content.Width);
            int minY = Math.Min(0, host.Height - content.Height);
            int maxX = 0;
            int maxY = 0;
            return (minX, minY, maxX, maxY);
        }

        // 뷰포트 원점(콘텐츠 좌표)을 지정해서 스크롤
        public void SetViewport(int viewX, int viewY)
        {
            if (!APSConfig.isZoomed)
                return;

            var (minX, minY, maxX, maxY) = GetPanBounds();

            // 콘텐츠 Location = (-뷰포트원점)
            int locX = -viewX;
            int locY = -viewY;

            // 경계 클램프
            locX = Math.Max(minX, Math.Min(locX, maxX));
            locY = Math.Max(minY, Math.Min(locY, maxY));

            if (_zoomContent.Left == locX && _zoomContent.Top == locY)
                return;

            _zoomContent.Location = new Point(locX, locY);
            _zoomContent.Invalidate();
            _zoomContent.Update();
        }

        // 픽셀 단위로 뷰포트를 이동(+x=오른쪽, +y=아래)
        public void PanByPixels(int dx, int dy)
        {
            if (!APSConfig.isZoomed)
                return;

            Point vp = GetViewport();
            int nx = vp.X + dx;
            int ny = vp.Y + dy;

            SetViewport(nx, ny);
        }

        // 뷰포트 크기 비율로 이동(fx, fy가 ±0.25면 1/4만큼 이동)
        public void PanByFraction(float fx, float fy)
        {
            if (!APSConfig.isZoomed)
                return;

            Size host = GetHostSize(); // 뷰포트 크기 == Host 클라이언트 크기
            int dx = (int)MathF.Round(host.Width * fx);
            int dy = (int)MathF.Round(host.Height * fy);

            PanByPixels(dx, dy);
        }

        public void SnapToQuadrant(int idx)
        {
            if (!APSConfig.isZoomed)
                return;

            Size host = GetHostSize();          // 부모(뷰포트) 크기
            Size content = _zoomContent.Size;   // 현재 콘텐츠 크기(예: 2048x1536)

            int dx = Math.Max(0, content.Width - host.Width);   // 2048-1024=1024
            int dy = Math.Max(0, content.Height - host.Height);  // 1536-768=768

            int viewX = 0, viewY = 0;
            switch (idx) {
                case 1:
                    viewX = 0;
                    viewY = 0;
                    break;       // ↖
                case 2:
                    viewX = dx;
                    viewY = 0;
                    break;       // ↗
                case 3:
                    viewX = 0;
                    viewY = dy;
                    break;       // ↙
                case 4:
                    viewX = dx;
                    viewY = dy;
                    break;       // ↘
                default:
                    return;
            }
            SetViewport(viewX, viewY); // 내부에서 (-viewX,-viewY)로 Location 설정 + 클램프
        }
    }
}
