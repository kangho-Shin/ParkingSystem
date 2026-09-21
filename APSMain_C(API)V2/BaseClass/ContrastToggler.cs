using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace APSMain.BaseClass
{
    /// 폼(또는 패널) 하나에 1개씩 붙여 쓰는 고대비 토글러(심플 버전)
    /// - Shown에서 SaveBase(this) 한 번 호출
    /// - ApplyHighContrast(this) / RestoreBase(this)
    /// 

    // 최소 스텁(필요 시 기존 ListViewPainter와 유사하게 구현)
    sealed class DataGridViewPainter
    {
        public void OnCellPainting(object? sender, DataGridViewCellPaintingEventArgs e) 
        {
            var gv = (DataGridView)sender!;

            if (e.RowIndex == -1 && e.ColumnIndex >= 0) {
                e.Handled = true;
                e.PaintBackground(e.ClipBounds, true);

                // ★ 여기 폰트를 고정값 대신 스타일 기반으로
                var font = gv.ColumnHeadersDefaultCellStyle.Font ?? gv.Font;
                TextRenderer.DrawText( e.Graphics!,
                                       e.FormattedValue?.ToString() ?? "",
                                       font,
                                       e.CellBounds,
                                       gv.ColumnHeadersDefaultCellStyle.ForeColor,
                                       TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
        }
        public void OnRowPostPaint(object? sender, DataGridViewRowPostPaintEventArgs e) { }
        public void OnPaint(object? sender, PaintEventArgs e) { }
    }

    public sealed class ContrastToggler
    {
        // ===== 팔레트(원하면 바꿔 쓰세요) =====
        public Color BackColor { get; set; } = Color.Black;
        public Color ForeColor { get; set; } = Color.White;
        public Color ButtonBackColor { get; set; } = Color.Black;
        public Color ButtonForeColor { get; set; } = Color.White;

        private bool _hasSnapshot=false;
        // ===== 내부 스냅샷 =====
        private class SavedStyle
        {
            public Color Back, Fore;

            public bool HasBgImage;
            public Image? BgImage;
            public ImageLayout BgLayout;

            public Image? ControlImage;          // PictureBox
            public bool Visible;                 // 복구용

            public FlatStyle? FlatStyle;         // Button
            public ContentAlignment? TextAlign;  // Button/Label
            public bool UseVisualStyleBackColor; // Button
            public string? Text="";
            // ListView 전용
            public bool? LV_OwnerDraw;
            public bool? LV_GridLines;
            public bool? LV_FullRowSelect;
            public bool? LV_HideSelection;
            public View? LV_View;

            // ===== DataGridView 전용 =====
            public Color? Grid = null;           // GridColor
            public Color? SelBack = null;         // 선택 배경
            public Color? SelFore = null;         // 선택 전경
            public Color? BackHeader = null;      // 컬럼 헤더 배경
            public Color? ForeHeader = null;      // 컬럼 헤더 전경
            public Color? BackRowHeader = null;   // 행 헤더 배경
            public Color? ForeRowHeader = null;   // 행 헤더 전경

            public bool? DG_ReadOnly = null;
            public bool? DG_MultiSelect = null;
            public bool? DG_AllowUserToAddRows = null;
            public bool? DG_AllowUserToDeleteRows = null;
            public bool? DG_AllowUserToResizeRows = null;
            public bool? DG_RowHeadersVisible = null;
            public bool? DG_EnableHeadersVisualStyles = null;

            public DataGridViewSelectionMode? DG_SelectionMode = null;
            public DataGridViewEditMode? DG_EditMode = null;
            public DataGridViewCellBorderStyle? DG_CellBorderStyle = null;
        }

        private readonly Dictionary<Control, SavedStyle> _base = new();

        [System.Runtime.InteropServices.DllImport("uxtheme.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr hWnd, string? appName, string? idList);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        private const int LVM_SETEXTENDEDLISTVIEWSTYLE = 0x1036;
        private const int LVS_EX_DOUBLEBUFFER = 0x00010000;

        private static void EnableListViewDoubleBuffer(ListView lv)
        {
            // .NET 보호된 DoubleBuffered 속성 켜기(리플렉션)
            var pi = typeof(Control).GetProperty("DoubleBuffered",
                     System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            pi?.SetValue(lv, true, null);

            // Win32 확장 더블버퍼
            if ( lv.IsHandleCreated )
                SendMessage(lv.Handle, LVM_SETEXTENDEDLISTVIEWSTYLE,(IntPtr)LVS_EX_DOUBLEBUFFER, (IntPtr)LVS_EX_DOUBLEBUFFER);
        }

        private static void DisableTheme(ListView lv)
        {
            // 비주얼 스타일(테마) 비활성화 → 우리가 준 Back/Fore가 그대로 반영됨
            if (lv.IsHandleCreated)
                SetWindowTheme(lv.Handle, "", "");
        }

        // ===== 등록(동적 컨트롤 대응) & 초기 스냅샷 저장 =====
        public void Register(Control root)
        {
            root.Disposed += (_, __) => RemoveTree(root);  // 트리만 제거
            root.ControlAdded += (_, e) => CaptureTree(e.Control!);
            root.ControlRemoved += (_, e) => RemoveTree(e.Control!);
        }

        /// Shown 이후 한 번
        public void SaveBase(Control root)
        {
            CleanupDisposed();
            foreach (var c in Walk(root))
                CaptureOne(c);

            _hasSnapshot = true;   // ← 여기서 세팅
        }

        public void UpdateSnapshotValues(Control root)
        {
            // Walk(root): 네가 이미 쓰는 재귀 열거자 그대로 사용
            foreach (var c in Walk(root)) {
                if (_base.TryGetValue(c, out var s)) {
                    // Text 반영(스냅샷에 Text 필드가 있을 때)
                    s.Text = c.Text;

                    // PictureBox.Image 반영
                    if (c is PictureBox pb)
                        s.ControlImage = pb.Image;
                }
            }
        }

        // ===== 적용 / 복구 =====
        public void ApplyHighContrast(Control root)
        {
            if (!_hasSnapshot || _base.Count == 0) { SaveBase(root); _hasSnapshot = true; }
            root.SuspendLayout();
            foreach (var c in Walk(root)) {
                if (c is DataGridView lv) {
                    ApplyDataGridView(lv);
                    continue;
                }
                else if (c is Button btn) {
                    c.BackColor = ButtonBackColor;
                    c.ForeColor = ButtonForeColor;
                    if( c is RectButton) {
                        RectButton? rbtn = c as RectButton;
                        if (rbtn != null) {
                            rbtn.isHC = true;
                        }
                    }
                    if (c.Name.Equals("btnHome")) {
                        btn.Image = Properties.Resources.home_white;
                    }
                    else if (btn.Name.Equals("btnPre")) {
                        btn.Image = Properties.Resources.prev_white;
                    }
                    else if (btn.Name.Equals("btnPrint")) {
                        btn.Image = Properties.Resources.Receipt_white;
                    }
                    else if (btn.Name.Equals("btnOk")) {
                        if ( btn.Enabled ) {
                            btn.Image = Properties.Resources.confirm_white;
                            //btn.FlatStyle = FlatStyle.Flat;
                            //btn.ForeColor = Color.White;
                        }
                        else {
                           // btn.FlatStyle = FlatStyle.Standard;
                            btn.Image = Properties.Resources.confirm_gray;
                            btn.UseVisualStyleBackColor = false;
                            btn.BackColor = SystemColors.Control;
                            btn.ForeColor = SystemColors.ControlText;
                        }
                    }
                    if (btn.Name.Equals("btnLabel")) {
                        btn.FlatStyle = FlatStyle.Standard;
                        btn.BackColor = SystemColors.Control;
                        btn.ForeColor = SystemColors.ControlText;
                    }
                    else {
                        btn.FlatStyle = FlatStyle.Flat;
                        btn.UseVisualStyleBackColor = false;
                        btn.TextAlign = ContentAlignment.MiddleCenter;
                    }
                }
                else {
                    c.BackColor = BackColor;
                    c.ForeColor = ForeColor;
                    if (c is Label lab)
                        lab.TextAlign = ContentAlignment.MiddleCenter;
                }
                if (c is not PictureBox) {
                    c.BackgroundImage = null; // 배경이미지 제거
                }
                TrySetHighContrast(c, true); // CTButton 등 커스텀 훅(있을 때만)
            }

            root.ResumeLayout(true);
        }

        public void RestoreBase(Control root)
        {
            if (_base.Count == 0)
                return;
            root.SuspendLayout();

            foreach (var c in Walk(root)) {
                if (c.IsDisposed)
                    continue;
                if (!_base.TryGetValue(c, out var s))
                    continue;

                if (c is DataGridView lv) {
                    RestoreDataGridView(lv, s);
                    continue;
                }

                c.BackColor = s.Back;
                c.ForeColor = s.Fore;

                if (s.HasBgImage) {
                    c.BackgroundImage = s.BgImage;
                    c.BackgroundImageLayout = s.BgLayout;
                }
                else
                    c.BackgroundImage = null;

                switch (c) {
                    case PictureBox pb:
                        //pb.Image = s.ControlImage;
                        pb.Visible = s.Visible;
                        break;

                    case Button btn:
                        if (s.FlatStyle.HasValue)
                            btn.FlatStyle = s.FlatStyle.Value;
                        btn.UseVisualStyleBackColor = s.UseVisualStyleBackColor;
                        if (s.TextAlign.HasValue)
                            btn.TextAlign = s.TextAlign.Value;

                        if (btn.Name.Equals("btnHome")) {
                            btn.Image = Properties.Resources.home;
                        }
                        else if (btn.Name.Equals("btnPre")) {
                            btn.Image = Properties.Resources.prev;
                        }
                        else if (btn.Name.Equals("btnPrint")) {
                            btn.Image = Properties.Resources.Receipt;
                        }
                        else if (btn.Name.Equals("btnOk")) {
                            btn.Image = Properties.Resources.confirm;
                        }

                        break;

                    case Label l:
                        if (s.TextAlign.HasValue)
                            l.TextAlign = s.TextAlign.Value;
                        break;
                }

                TrySetHighContrast(c, false);
            }

            root.ResumeLayout(true);
        }

        // ===== 내부: 스냅샷 =====
        private void CaptureTree(Control node)
        {
            foreach (var c in Walk(node))
                CaptureOne(c);
        }

        private void RemoveTree(Control node)
        {
            foreach (var c in Walk(node))
                _base.Remove(c);
        }

        private void CleanupDisposed()
        {
            foreach (var k in _base.Keys.Where(x => x == null || x.IsDisposed).ToList())
                _base.Remove(k);
        }

        private void CaptureOne(Control c)
        {
            if (_base.ContainsKey(c))
                return;

            var s = new SavedStyle
            {
                Back = c.BackColor,
                Fore = c.ForeColor,

                HasBgImage = c.BackgroundImage != null,
                BgImage = c.BackgroundImage,
                BgLayout = c.BackgroundImageLayout,
                Text = c.Text,
                ControlImage = (c as PictureBox)?.Image,
                Visible = c.Visible,

                UseVisualStyleBackColor = (c as Button)?.UseVisualStyleBackColor ?? false,
            };

            if (c is Button b) { s.FlatStyle = b.FlatStyle; s.TextAlign = b.TextAlign; }
            else if (c is Label l) { s.TextAlign = l.TextAlign; }
            else if (c is ListView lv) {
                s.LV_OwnerDraw = lv.OwnerDraw;
                s.LV_GridLines = lv.GridLines;
                s.LV_FullRowSelect = lv.FullRowSelect;
                s.LV_HideSelection = lv.HideSelection;
                s.LV_View = lv.View;
            }

            _base[c] = s;
        }

        // ===== 내부: 순회 =====
        private static IEnumerable<Control> Walk(Control root)
        {
            var st = new Stack<Control>();
            st.Push(root);
            while (st.Count > 0) {
                var c = st.Pop();
                if (c == null)
                    continue;
                yield return c;
                foreach (Control ch in c.Controls)
                    st.Push(ch);
            }
        }

        // ===== ListView(완전 고대비: 헤더/행까지) =====
        private sealed class ListViewPainter
        {
            public Color RowBack, RowFore, HeaderBack, HeaderFore;
            public ListViewPainter(Color rowBack, Color rowFore, Color headerBack, Color headerFore)
            { RowBack = rowBack; RowFore = rowFore; HeaderBack = headerBack; HeaderFore = headerFore; }

            public void OnDrawColumnHeader(object? sender, DrawListViewColumnHeaderEventArgs e)
            {
                using var back = new SolidBrush(HeaderBack);
                e.Graphics.FillRectangle(back, e.Bounds);
                TextRenderer.DrawText(e.Graphics, e.Header!.Text, e.Font, e.Bounds, HeaderFore,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
                e.DrawDefault = false;
            }

            public void OnDrawItem(object? sender, DrawListViewItemEventArgs e)
            {
                var lv = (ListView)sender!;
                // Details 모드: 전체 가로폭(0~ClientWidth) 배경만 칠하고 텍스트는 그리지 않음
                if (lv.View == View.Details) {
                    var rowRect = new Rectangle(0, e.Bounds.Top, lv.ClientSize.Width, e.Bounds.Height);
                    using var back = new SolidBrush(RowBack);
                    e.Graphics.FillRectangle(back, rowRect);
                    e.DrawDefault = false; // 기본 그리기 금지
                    return;
                }

                // Details 외 모드: 항목 텍스트도 여기서
                using var back2 = new SolidBrush(RowBack);
                using var fore2 = new SolidBrush(RowFore);
                e.Graphics.FillRectangle(back2, e.Bounds);
                TextRenderer.DrawText(e.Graphics, e.Item.Text, e.Item.Font, e.Bounds, RowFore);
                e.DrawFocusRectangle();
                e.DrawDefault = false;
            }

            public void OnDrawSubItem(object? sender, DrawListViewSubItemEventArgs e)
            {
                using var back = new SolidBrush(RowBack);
                using var fore = new SolidBrush(RowFore);
                e.Graphics.FillRectangle(back, e.Bounds);
                TextRenderer.DrawText(e.Graphics, e.SubItem!.Text, e.SubItem.Font, e.Bounds, RowFore,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
                e.DrawDefault = false;
            }
        }

        private readonly Dictionary<ListView, ListViewPainter> _lvPainters = new();

        private void ApplyListView(ListView lv)
        {
            // 오너드로우 제거(이전 코드 있었다면 반드시 끔)
            if (_lvPainters.TryGetValue(lv, out var p)) {
                lv.DrawColumnHeader -= p.OnDrawColumnHeader;
                lv.DrawItem -= p.OnDrawItem;
                lv.DrawSubItem -= p.OnDrawSubItem;
                _lvPainters.Remove(lv);
            }
            lv.OwnerDraw = false;

            DisableTheme(lv);            // 테마 끄기
            EnableListViewDoubleBuffer(lv);

            // 색/스타일 강제
            lv.BackColor = BackColor;
            lv.ForeColor = ForeColor;
            lv.FullRowSelect = true;
            lv.GridLines = true;
            lv.HideSelection = false;

            // 아이템/서브아이템 색
            foreach (ListViewItem it in lv.Items) {
                it.UseItemStyleForSubItems = false;
                it.BackColor = BackColor;
                it.ForeColor = ForeColor;
                foreach (ListViewItem.ListViewSubItem si in it.SubItems) {
                    si.BackColor = BackColor;
                    si.ForeColor = ForeColor;
                }
            }

            lv.Invalidate(true);
        }

        private void RestoreListView(ListView lv, SavedStyle s)
        {
            // 오너드로우 해제
            if (_lvPainters.TryGetValue(lv, out var p)) {
                lv.DrawColumnHeader -= p.OnDrawColumnHeader;
                lv.DrawItem -= p.OnDrawItem;
                lv.DrawSubItem -= p.OnDrawSubItem;
                _lvPainters.Remove(lv);
            }

            // 원래 속성 복구
            if (s.LV_View.HasValue)
                lv.View = s.LV_View.Value;
            if (s.LV_OwnerDraw.HasValue)
                lv.OwnerDraw = s.LV_OwnerDraw.Value;
            if (s.LV_GridLines.HasValue)
                lv.GridLines = s.LV_GridLines.Value;
            if (s.LV_FullRowSelect.HasValue)
                lv.FullRowSelect = s.LV_FullRowSelect.Value;
            if (s.LV_HideSelection.HasValue)
                lv.HideSelection = s.LV_HideSelection.Value;

            // 색 복구
            lv.BackColor = s.Back;
            lv.ForeColor = s.Fore;

            foreach (ListViewItem it in lv.Items) {
                it.UseItemStyleForSubItems = true;
                it.BackColor = s.Back;
                it.ForeColor = s.Fore;
                foreach (ListViewItem.ListViewSubItem si in it.SubItems) { si.BackColor = s.Back; si.ForeColor = s.Fore; }
            }
        }


        private readonly Dictionary<DataGridView, DataGridViewPainter> _gvPainters = new();

        private void ApplyDataGridView(DataGridView gv)
        {
            // 기존 페인터 제거
            if (_gvPainters.TryGetValue(gv, out var p)) {
                gv.CellPainting -= p.OnCellPainting;
                gv.RowPostPaint -= p.OnRowPostPaint;
                gv.Paint -= p.OnPaint;
                _gvPainters.Remove(gv);
            }

            // 깜빡임/헤더테마
            var prop = typeof(DataGridView).GetProperty("DoubleBuffered",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            prop?.SetValue(gv, true, null);
            gv.EnableHeadersVisualStyles = false;

            // 동작(리스트뷰 유사)
            gv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            gv.MultiSelect = false;
            gv.ReadOnly = true;
            gv.AllowUserToAddRows = false;
            gv.AllowUserToDeleteRows = false;
            gv.AllowUserToResizeRows = false;
            gv.CellBorderStyle = DataGridViewCellBorderStyle.None;
            gv.RowHeadersVisible = false;
            gv.ShowCellToolTips = false;

            // 기본 색
            gv.BackgroundColor = BackColor;
            gv.GridColor = ForeColor;
            gv.DefaultCellStyle.BackColor = BackColor;
            gv.DefaultCellStyle.ForeColor = ForeColor;

            // 헤더 색
            //gv.ColumnHeadersDefaultCellStyle.BackColor = BackColor;
            //gv.ColumnHeadersDefaultCellStyle.ForeColor = ForeColor;
            gv.ColumnHeadersDefaultCellStyle.SelectionBackColor = gv.ColumnHeadersDefaultCellStyle.BackColor;
            gv.ColumnHeadersDefaultCellStyle.SelectionForeColor = gv.ColumnHeadersDefaultCellStyle.ForeColor;
            gv.RowHeadersDefaultCellStyle.BackColor = BackColor;
            gv.RowHeadersDefaultCellStyle.ForeColor = ForeColor;

            // ★ 선택 강조색(가시성 유지) — 필요시 값만 바꿔 쓰세요.
            var selBack = BackColor;///           Color.FromArgb(40, 110, 200);
            var selFore = Color.White;

            gv.DefaultCellStyle.SelectionBackColor = selBack;
            gv.DefaultCellStyle.SelectionForeColor = selFore;

            // 첫 컬럼 왜곡 방지
            if (gv.Columns.Count > 0) {
                gv.Columns[0].DefaultCellStyle.BackColor = gv.DefaultCellStyle.BackColor;
                gv.Columns[0].DefaultCellStyle.ForeColor = gv.DefaultCellStyle.ForeColor;
            }

            // 기존 행/셀 반영
            foreach (DataGridViewRow r in gv.Rows) {
                r.DefaultCellStyle.BackColor = BackColor;
                r.DefaultCellStyle.ForeColor = ForeColor;
                foreach (DataGridViewCell c in r.Cells) {
                    c.Style.BackColor = BackColor;
                    c.Style.ForeColor = ForeColor;
                }
            }
            gv.Invalidate(true);
        }

        private void RestoreDataGridView(DataGridView gv, SavedStyle s)
        {
            // 기존 페인터 제거
            if (_gvPainters.TryGetValue(gv, out var p)) {
                gv.CellPainting -= p.OnCellPainting;
                gv.RowPostPaint -= p.OnRowPostPaint;
                gv.Paint -= p.OnPaint;
                _gvPainters.Remove(gv);
            }

            // 동작 복구
            if (s.DG_ReadOnly.HasValue)
                gv.ReadOnly = s.DG_ReadOnly.Value;
            if (s.DG_EditMode.HasValue)
                gv.EditMode = s.DG_EditMode.Value;
            if (s.DG_SelectionMode.HasValue)
                gv.SelectionMode = s.DG_SelectionMode.Value;
            if (s.DG_MultiSelect.HasValue)
                gv.MultiSelect = s.DG_MultiSelect.Value;
            if (s.DG_AllowUserToAddRows.HasValue)
                gv.AllowUserToAddRows = s.DG_AllowUserToAddRows.Value;
            if (s.DG_AllowUserToDeleteRows.HasValue)
                gv.AllowUserToDeleteRows = s.DG_AllowUserToDeleteRows.Value;
            if (s.DG_AllowUserToResizeRows.HasValue)
                gv.AllowUserToResizeRows = s.DG_AllowUserToResizeRows.Value;
            if (s.DG_RowHeadersVisible.HasValue)
                gv.RowHeadersVisible = s.DG_RowHeadersVisible.Value;
            if (s.DG_CellBorderStyle.HasValue)
                gv.CellBorderStyle = s.DG_CellBorderStyle.Value;
            if (s.DG_EnableHeadersVisualStyles.HasValue)
                gv.EnableHeadersVisualStyles = s.DG_EnableHeadersVisualStyles.Value;
            gv.ShowCellToolTips = false;

            // 색 복구
            s.Back = Color.White;
            s.Fore = Color.Black;
            gv.BackgroundColor = s.Back;
            if (s.Grid.HasValue)
                gv.GridColor = s.Grid.Value;

            gv.DefaultCellStyle.BackColor = s.Back;
            gv.DefaultCellStyle.ForeColor = s.Fore;

            // ★ SavedStyle에 지정돼 있으면 그걸로, 없으면 Apply의 기본 강조색 유지
            var selBack = s.SelBack ?? Color.FromArgb(40, 110, 200);
            var selFore = s.SelFore ?? Color.White;

            gv.DefaultCellStyle.SelectionBackColor = selBack;
            gv.DefaultCellStyle.SelectionForeColor = selFore;

            gv.ColumnHeadersDefaultCellStyle.BackColor = s.BackHeader ?? s.Back;
            gv.ColumnHeadersDefaultCellStyle.ForeColor = s.ForeHeader ?? s.Fore;
            gv.ColumnHeadersDefaultCellStyle.SelectionBackColor = gv.ColumnHeadersDefaultCellStyle.BackColor;
            gv.ColumnHeadersDefaultCellStyle.SelectionForeColor = gv.ColumnHeadersDefaultCellStyle.ForeColor;

            gv.RowHeadersDefaultCellStyle.BackColor = s.BackRowHeader ?? s.Back;
            gv.RowHeadersDefaultCellStyle.ForeColor = s.ForeRowHeader ?? s.Fore;

            // 첫 컬럼 보호
            if (gv.Columns.Count > 0) {
                gv.Columns[0].DefaultCellStyle.BackColor = gv.DefaultCellStyle.BackColor;
                gv.Columns[0].DefaultCellStyle.ForeColor = gv.DefaultCellStyle.ForeColor;
            }

            // 기존 행/셀 반영
            foreach (DataGridViewRow r in gv.Rows) {
                r.DefaultCellStyle.BackColor = s.Back;
                r.DefaultCellStyle.ForeColor = s.Fore;
                foreach (DataGridViewCell c in r.Cells) {
                    c.Style.BackColor = s.Back;
                    c.Style.ForeColor = s.Fore;
                }
            }

            // 고대비에서도 동일 강조 유지
            if (SystemInformation.HighContrast) {
                gv.EnableHeadersVisualStyles = false;
                gv.DefaultCellStyle.SelectionBackColor = selBack;
                gv.DefaultCellStyle.SelectionForeColor = selFore;
                gv.ColumnHeadersDefaultCellStyle.SelectionBackColor = gv.ColumnHeadersDefaultCellStyle.BackColor;
                gv.ColumnHeadersDefaultCellStyle.SelectionForeColor = gv.ColumnHeadersDefaultCellStyle.ForeColor;
            }

            gv.Invalidate(true);
        }

        // ===== 커스텀 버튼(CTButton) 지원(있을 때만) =====
        private static void TrySetHighContrast(Control c, bool on)
        {
            var mi = c.GetType().GetMethod("SetHighContrast",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic,
                binder: null,
                types: new[] { typeof(bool) },
                modifiers: null);
            if (mi != null) { try { mi.Invoke(c, new object[] { on }); } catch { } }
        }
    }
}

