using APSMain.BaseClass;
using APSMain.Models;
using APSMain.TTSLib;
using System.Data;
using System.Runtime.InteropServices;

namespace APSMain
{
    public partial class CarSelectForm : Form, ISubFormResult<string>, IActiveForm
    {
        public Action<Form, Keys, int, int>? RouteArrow { get; set; }
        private ZoomWrapper? _zoomWrap;
        private ContrastToggler _contrast => APSConfig.Contrast;
        public FormResult Result { get; private set; } = FormResult.FormCancel;
        public string ResultData { get; private set; } = "";
        public string _selectedCar = string.Empty;
        private MainForm? _mainForm;
        private int lastIndex = 0;

        private const int LVM_FIRST = 0x1000;
        private const int LVM_SETITEMHEIGHT = LVM_FIRST + 64;
        const int VERTICAL_PADDING = 30;
        private bool _parentZoom = false;
        private bool _periodtype = false;
        public Func<long, Task>? EdgeSelectionConfirmed { get; set; }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        public CarSelectForm(bool periodtype)
        {
            InitializeComponent();

            this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
            this.UpdateStyles();

            _mainForm = APSConfig.mainForm;
            if (_mainForm != null) {
                RouteArrow = _mainForm.ProcessArrow;
            }

            _periodtype = periodtype;
            _parentZoom = APSConfig.isZoomed;
        }

        private void CarSelectForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            APSConfig.ContrastChanged -= OnContrastChanged;
            if (_zoomWrap != null) {
                _zoomWrap.DetachDragEvents(pZoomContent);
            }
            //APSConfig.activeForm = null;
        }

        private void CarSelectForm_Load(object sender, EventArgs e)
        {
            //_mainForm?.NormalZoomMode();
            // 차량 목록 바인딩 (예: Parkinfos + Periodmembers)
            var cars = ParkCache.Parkins
                .Select(p => new CarEntryInfo { ParkingSessionId = p.Xindex, Carnum = p.Carnum!, InDate = new DateTime(p.Indate.Year, p.Indate.Month, p.Indate.Day, (int)p.Inhour, (int)p.Inmin, 0), Type = "일반I" })
                .Concat(ParkCache.Parkinfos
                .Select(p => new CarEntryInfo { ParkingSessionId = p.Xindex, Carnum = p.Carnum, InDate = new DateTime(p.Indate.Year, p.Indate.Month, p.Indate.Day, (int)p.Inhour, (int)p.Inmin, 0), Type = "일반X" }))
                .Concat(ParkCache.Periodmembers
                .Select(p => new CarEntryInfo { Carnum = p.Carnum1, InDate = p.Startdate, Type = "정기" }))
                .ToList();


            NormalizeHeader();

            // 💡 데이터 바인딩
            dgvCarList.Rows.Clear();
            foreach (var car in cars) {
                int rowIndex = dgvCarList.Rows.Add(
                    car.Carnum ?? "",
                    car.InDate.ToString("MM-dd HH:mm:ss") ?? "",
                    car.Type
                );
                dgvCarList.Rows[rowIndex].Tag = car.ParkingSessionId;
            }

            _zoomWrap = new ZoomWrapper(pZoomContent);

            _mainForm?.lelMessageChange(3);
            //string ttxtext = $"carfind{dgvCarList.Rows.Count:D2}.mp3";
            //_mainForm?.PlaySoundFile(ttxtext, AudioRouteState.Dual, 1);
            if (_mainForm?.CurrentState == AudioRouteState.UsbActive) {
                _mainForm?.PlaySoundFile("main13.mp3", AudioRouteState.UsbActive, 1);
            }
            else {
                _mainForm?.PlaySoundFile("main03.mp3", AudioRouteState.Idle, 1);
            }

            lastIndex = GetEndTabIndex();
        }

        private void NormalizeHeader()
        {
            dgvCarList.RowTemplate.Height = 110;
            dgvCarList.AllowUserToAddRows = false;
            dgvCarList.RowHeadersVisible = false;
            dgvCarList.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvCarList.MultiSelect = false;

            dgvCarList.Columns.Clear();
            dgvCarList.Columns.Add("Carnum", "차량번호");
            dgvCarList.Columns.Add("InDate", "입차일시");
            dgvCarList.Columns.Add("Type", "유형");

            var centerStyle = new DataGridViewCellStyle
            {
                Alignment = DataGridViewContentAlignment.MiddleCenter,
                BackColor = Color.White,
                ForeColor = Color.Black
            };

            dgvCarList.Columns["Carnum"].Width = 360;
            dgvCarList.Columns["InDate"].Width = 380;
            dgvCarList.Columns["Type"].Width = 170;
            dgvCarList.Columns["Carnum"].DefaultCellStyle = centerStyle;
            dgvCarList.Columns["InDate"].DefaultCellStyle = centerStyle;
            dgvCarList.Columns["Type"].DefaultCellStyle = centerStyle;

            dgvCarList.EnableHeadersVisualStyles = false;
            dgvCarList.ColumnHeadersDefaultCellStyle.BackColor = Color.White;
            dgvCarList.ColumnHeadersDefaultCellStyle.ForeColor = Color.Black;
            dgvCarList.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.White;
            dgvCarList.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.Black;

            dgvCarList.DefaultCellStyle.SelectionBackColor = dgvCarList.DefaultCellStyle.BackColor;
            dgvCarList.DefaultCellStyle.SelectionForeColor = dgvCarList.DefaultCellStyle.ForeColor;

            // ★ 헤더 폰트를 DataGridView.Font에 맞춰 둠
            dgvCarList.ColumnHeadersDefaultCellStyle.Font = dgvCarList.Font;

            for (int i = 0; i < dgvCarList.Columns.Count; i++) {
                dgvCarList.Columns[i].HeaderCell.Style = new DataGridViewCellStyle(dgvCarList.ColumnHeadersDefaultCellStyle);
                dgvCarList.Columns[i].SortMode = DataGridViewColumnSortMode.NotSortable;
            }
        }


        #region IActionForm
        public void MoveTo(Point screenLocation)
        {
            this.Location = screenLocation;
        }

        private bool _zoomRequested = false;
        public string ZoomInOut()
        {
            if (!IsHandleCreated || !Visible)
                return "";

            if (InvokeRequired) // ★ 외부에서 불려도 동기로
                return (string)Invoke(new Func<string>(ZoomInOut));

            if (_zoomRequested)
                return "";
            _zoomRequested = true;

            try {
                _zoomWrap?.ToggleZoom();   // 동기 실행
                Invalidate();
                Update();                  // 즉시 갱신
                return _zoomWrap?.IsZoomed == true ? "축소" : "확대";
            }
            finally { _zoomRequested = false; }
        }

        public void OnConfirm()
        {
            if (this.ActiveControl is IButtonControl btn) {
                btn.PerformClick();
            }
        }

        public void OnCancel()
        {
            Result = FormResult.FormPre;
            APSConfig.isZoomed = _parentZoom;
            _mainForm!.PlaySoundFile("btnPre1.mp3", AudioRouteState.Dual, 1);
            BeginInvoke((Action)(() => this.Close()));  // 폼 닫기
        }

        public void GiveFocus()
        {
            if (!IsDisposed && dgvCarList.CanFocus) // DataGridView로 변경
                dgvCarList.Focus();

            btnOk.Enabled = true;
            if (APSConfig.isContrast) {
                btnOk.Image = Properties.Resources.confirm_white;
                btnOk.BackColor = Color.FromArgb(17, 17, 17);
                btnOk.ForeColor = Color.White;
            }
            lastIndex = GetEndTabIndex();
            btnOk.Focus();
        }

        public void ContrastCall()
        {
            _contrast.UpdateSnapshotValues(this);
        }

        public void OnContrastChanged(bool on)
        {
            if (IsDisposed)
                return;
            if (on)
                _contrast.ApplyHighContrast(this);
            else
                _contrast.RestoreBase(this);
        }
        #endregion


        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            var key = keyData & Keys.KeyCode;

            int idx = dgvCarList.CurrentCell?.RowIndex ?? -1;

            if (key is Keys.Left or Keys.Right) {
                // 텍스트 입력 중에는 화살표를 건드리지 않으려면 다음 줄 해제:
                if (this.ActiveControl is TextBoxBase or ComboBox)
                    return base.ProcessCmdKey(ref msg, keyData);

                RouteArrow?.Invoke(this, key, 0, lastIndex);   // ← 항상 MainForm로 위임
                return true;               // 내가 처리했음
            }
            else if (keyData is Keys.F) {           // ⏹️ 홈
                BeginInvoke((Action)(() => OnGoHome()));
                return true;
            }
            else if (keyData is Keys.Escape) {      // ✖️ 이전
                BeginInvoke((Action)(() => OnCancel()));
                return true;
            }
            else if (keyData is Keys.Down) {
                if (idx < dgvCarList.Rows.Count - 1)
                    SelectIndex(idx + 1);
                else if (idx == -1 && dgvCarList.Rows.Count > 0) // 초기 선택이 없을 경우
                    SelectIndex(0);
                return true;
            }
            else if (keyData is Keys.Up) {
                if (idx > 0)
                    SelectIndex(idx - 1);
                return true;
            }
            else if (keyData is Keys.Enter) {
                if (this.ActiveControl is IButtonControl btn) {
                    btn.PerformClick();
                    return true;
                }
                //btnOk_Click(this, EventArgs.Empty);
                //return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private int GetEndTabIndex(bool buttonsOnly = true)
        {
            int max = -1;
            void Walk(Control p)
            {
                foreach (Control c in p.Controls) {
                    if (!c.Visible || !c.Enabled) { if (c.HasChildren) Walk(c); continue; }

                    bool isTarget = c.TabStop && (!buttonsOnly || c is Button);
                    if (isTarget)
                        max = Math.Max(max, c.TabIndex);

                    if (c.HasChildren)
                        Walk(c);
                }
            }
            Walk(this);
            return max;
        }


        void ApplyFocusStyleToAllButtons(Control root, int thick = 6)
        {
            foreach (Control c in root.Controls) {
                if (c is Button b)
                    EnhanceFocus(b, thick);
                if (c.HasChildren)
                    ApplyFocusStyleToAllButtons(c, thick);
            }
        }

        private void EnhanceFocus(Button b, int thick = 6)
        {
            b.FlatStyle = FlatStyle.Standard;          // Flat의 내부 여백 이슈 피함
            //b.UseVisualStyleBackColor = true;          // 테마 배경 유지 -> 텍스트영역 안정
            b.AutoSize = false;                        // 포커스 시 크기변동 방지
            b.AutoEllipsis = false;                    // "..." 잘림 방지
            b.UseCompatibleTextRendering = true;       // 한글 잘림/측정 이슈 완화
            b.TabStop = true;
            b.TextAlign = ContentAlignment.MiddleCenter;
            b.RightToLeft = RightToLeft.No;
            b.FlatAppearance.BorderSize = thick;
            b.FlatAppearance.BorderColor = Color.White; //.FromArgb(0x33, 0x33, 0x33); 

            // 포커스 변화 → 다시 그리기만
            b.GotFocus += (s, e) =>
            {
                b.FlatAppearance.BorderSize = thick;
                if (APSConfig.isContrast)
                    b.FlatAppearance.BorderColor = Color.Yellow; // Color.FromArgb(0xFF, 0xEB, 0x3B);
                else
                    b.FlatAppearance.BorderColor = Color.Red; //Color.FromArgb(0xFF, 0xEB, 0x3B);
                b.Invalidate();
            };
            b.LostFocus += (s, e) =>
            {
                b.FlatAppearance.BorderSize = thick;
                b.FlatAppearance.BorderColor = Color.White; //.FromArgb(0x33, 0x33, 0x33); 
                b.Invalidate();
            };

            // Paint에서만 선명한 테두리 오버레이
            b.Paint += (_, e) =>
            {
                thick = b.FlatAppearance.BorderSize;
                using var p = new System.Drawing.Pen(b.FlatAppearance.BorderColor, thick);

                p.Alignment = System.Drawing.Drawing2D.PenAlignment.Inset;

                var r = b.ClientRectangle;

                e.Graphics.DrawRectangle(p, r);
            };
        }

        private void SetListViewItemHeight(ListView listView, int height)
        {
            // 높이를 16비트 값으로 인코딩하여 lParam에 전달 (WPARAM/LPARAM은 높이/너비를 설정)
            // 32비트 환경에서는 wParam과 lParam이 동일한 값을 가집니다.
            // LVM_SETITEMHEIGHT 메시지는 항목의 높이와 너비를 인수로 받습니다.
            // 높이와 너비를 같은 값(height)으로 설정합니다.
            int heightParam = height; // 또는 (height << 16) | height;

            // Windows 메시지를 ListView 컨트롤에 보냅니다.
            SendMessage(listView.Handle, LVM_SETITEMHEIGHT, heightParam, 0);

            // 강제 높이 설정 후 ListView를 갱신합니다.
            listView.Invalidate();
        }

        private void lvCarList_DrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
        {
            e.DrawDefault = true;
        }

        private void lvCarList_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            if (!e.IsSelected)
                return;

            _selectedCar = e.Item?.SubItems[0].Text ?? "";

            string ttxtext = $"선택된 차량번호는 {_selectedCar}입니다 .본인의 차량을 방향키로 선택한 뒤 확인 버튼을 눌러주세요.";

            _mainForm?.PlayTextSpeech(ttxtext, AudioRouteState.UsbActive);

            //if (!e.IsSelected || _closing)
            //    return;

            //// 폼 종료 전에 필요한 값만 캡처
            //string car = e.Item?.SubItems[0].Text ?? "";

            //_closing = true;
            //BeginInvoke(new Action(() =>
            //{
            //    try {
            //        ResultData = car;
            //        APSConfig.activeForm = null;
            //        Result = DialogResult.OK;   // 모달일 때만 의미 있음
            //        Close();                    // 이벤트 반환 후 안전하게 종료
            //    }
            //    finally { _closing = false; }
            //}));

            btnOk.Enabled = true;
            if (APSConfig.isContrast) {
                btnOk.Image = Properties.Resources.confirm_white;
                btnOk.BackColor = Color.FromArgb(17, 17, 17);
                btnOk.ForeColor = Color.White;
            }
            lastIndex = GetEndTabIndex();
            btnOk.Focus();
        }

        private void SelectIndex(int i)
        {
            if (i < 0 || i >= dgvCarList.Rows.Count) // DataGridView로 변경
                return;

            dgvCarList.ClearSelection();
            dgvCarList.Rows[i].Selected = true;

            // CurrentCell 설정 (포커스 이동)
            // 0번째 셀에 포커스를 맞춥니다.
            dgvCarList.CurrentCell = dgvCarList.Rows[i].Cells[0];

            // 스크롤 (EnsureVisible 역할)
            dgvCarList.FirstDisplayedScrollingRowIndex = i;

            _selectedCar = dgvCarList.Rows[i].Cells[0].Value?.ToString() ?? ""; // DataGridView에서 값 가져오기

            if (!string.IsNullOrEmpty(_selectedCar)) {
                string ttxtext = $"선택된 차량번호는 {_selectedCar}입니다 .본인 차량이 맞으면 확인 버튼을 눌러주세요.";
                _mainForm?.PlayTextSpeech(ttxtext, AudioRouteState.UsbActive, 1);
            }
            btnOk.Enabled = true;
            if (APSConfig.isContrast) {
                btnOk.Image = Properties.Resources.confirm_white;
                btnOk.BackColor = Color.FromArgb(17, 17, 17);
                btnOk.ForeColor = Color.White;
            }
            lastIndex = GetEndTabIndex();
            btnOk.Focus();
        }

        private void CarSelectForm_Shown(object sender, EventArgs e)
        {
            ApplyFocusStyleToAllButtons(this, thick: 6);

            _contrast.Register(this);   // 루트(폼) 등록
            _contrast.SaveBase(this);   // 현재 상태 스냅샷 저장

            if (APSConfig.isContrast)   // 전역 모드가 이미 ON이라면 즉시 적용
                _contrast.ApplyHighContrast(this);

            APSConfig.ContrastChanged += OnContrastChanged;   // 전역 신호 구독

            if (dgvCarList.Rows.Count > 0) { // DataGridView로 변경
                                             // DataGridView는 Rows[i]를 사용
                dgvCarList.Rows[0].Selected = true;
                dgvCarList.CurrentCell = dgvCarList.Rows[0].Cells[0]; // CurrentCell로 포커스 설정
            }

            if (APSConfig.isZoomed) {
                APSConfig.isZoomed = false;
                ZoomInOut();
            }
            GiveFocus();
        }

        public void ParkCalFormDisplay(string carNum)
        {
            int ldmNum = _mainForm?._OLDMNum ?? 0;

            if (ParkCache.Parkins != null && ParkCache.Parkins.Count > 0) {
                var Parkin = ParkCache.Parkins.Where(t => string.Equals(t.Carnum, carNum)).FirstOrDefault();
                if (Parkin != null) {
                    var Parkinfo = new Tparkinfo
                    {
                        Sitenum = Parkin.Sitenum,
                        Groupnum = Parkin.Groupnum,
                        Carnum = Parkin.Carnum!,
                        Indate = Parkin.Indate,
                        Inhour = Parkin.Inhour,
                        Inmin = Parkin.Inmin,
                        Inimage = Parkin.Inimage,
                        Indevicenum = Parkin.Indevicenum,
                        Outflag = 73, // 입차 상태
                                      // 나머지도 채울 수 있으면 채움
                    };
                    HandleExitCarDetected(Parkinfo, null, ldmNum);
                    //TTSWrapper.SpeakText($"차량 번호 {Parkin.Carnum} 를 선택하여 정산을 합니다.", 0);

                }
            }
            else if (ParkCache.Parkinfos != null && ParkCache.Parkinfos.Count > 0) {
                var Parkinfo = ParkCache.Parkinfos.Where(t => t.Carnum.Equals(carNum)).FirstOrDefault();
                if (Parkinfo != null) {
                    HandleExitCarDetected(Parkinfo, null, ldmNum);
                    //TTSWrapper.SpeakText($"차량 번호 {Parkinfo.Carnum} 를 선택하여 정산을 합니다.", 0);
                }
            }
            else if (ParkCache.Periodmembers != null && ParkCache.Periodmembers.Count > 0) {
                var Periodmember = ParkCache.Periodmembers.Where(t => t.Carnum1.Equals(carNum)).FirstOrDefault();
                if (Periodmember != null) {
                    //TTSWrapper.SpeakText($"차량 번호 {Periodmember.Carnum} 를 선택하여 정산을 합니다.", 0);
                    HandleExitCarDetected(null, Periodmember, ldmNum);
                }
            }
        }

        private async void btnOk_Click(object sender, EventArgs e)
        {
            var selectedRow = dgvCarList.SelectedRows.Count > 0 ? dgvCarList.SelectedRows[0] : null;

            if (selectedRow == null)
                return;

            _selectedCar = selectedRow.Cells[0].Value?.ToString() ?? ""; // 0번째 셀의 값

            if (string.IsNullOrEmpty(_selectedCar)) {
                return; // 선택 없음
            }

            try {
                ResultData = _selectedCar;
                if (EdgeSelectionConfirmed != null) {
                    btnOk.Enabled = false;
                    if (selectedRow.Tag is not long parkingSessionId || parkingSessionId <= 0)
                        throw new InvalidOperationException("선택한 차량의 주차 세션 정보가 없습니다.");
                    try { await EdgeSelectionConfirmed(parkingSessionId); }
                    catch (Exception ex) { MessageBox.Show(this, $"요금 조회 오류: {ex.Message}"); }
                    finally { if (!IsDisposed) btnOk.Enabled = true; }
                    return;
                }
                ParkCalFormDisplay(_selectedCar);
                // ...
            }
            catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }
        }

        private void HandleExitCarDetected(Tparkinfo? carInfo, Tperiodmember? tmember, int ldmIndex)
        {
            if (this.InvokeRequired) {
                this.Invoke(new Action(() => ShowOrResetCalForm(carInfo, tmember, ldmIndex)));
            }
            else {
                ShowOrResetCalForm(carInfo, tmember, ldmIndex);
            }
        }

        private void ShowOrResetCalForm(Tparkinfo? carInfo, Tperiodmember? tmember, int ldmIndex)
        {
            ParkCalForm calForm = new ParkCalForm(ldmIndex, _periodtype);

            _mainForm!._xparkinfo.CopyFrom(carInfo!);
            _mainForm!._xperiodmember.CopyFrom(tmember!);
            calForm._parkinfo = _mainForm!._xparkinfo;
            calForm._periodmember = _mainForm!._xperiodmember;

            UiHost.ShowActiveForm<string>(
                       this,
                       calForm,
                       (result, data) =>
                       {
                           Result = result;
                           if (Result == FormResult.FormHome) {
                               //BeginInvoke((Action)(() => OnGoHome()));
                               //OnGoHome();
                               if (Result == FormResult.FormHome) {
                                   var host = APSConfig.FormHost;
                                   if (host != null && host.IsHandleCreated && !host.IsDisposed) {
                                       host.BeginInvoke((Action)(() => OnGoHome()));
                                   }
                                   else {
                                       OnGoHome();
                                   }
                                   return;
                               }
                           }
                       }
            );
        }

        private void btnHome_Click(object sender, EventArgs e)
        {
            Result = FormResult.FormHome;
            ResultData = "";
            _mainForm!.PlaySoundFile("btnHome1.mp3", AudioRouteState.Dual, 1);
            BeginInvoke((Action)(() => this.Close()));
        }

        private void btnPre_Click(object sender, EventArgs e)
        {
            Result = FormResult.FormPre;
            APSConfig.isZoomed = _parentZoom;
            ResultData = "";
            _mainForm!.PlaySoundFile("btnPre1.mp3", AudioRouteState.Dual, 1);
            BeginInvoke((Action)(() => this.Close()));
        }

        public void ZoomMovedQuard(int pos)
        {
            _zoomWrap?.SnapToQuadrant(pos);
        }

        private void btn_MouseDown(object sender, MouseEventArgs e)
        {
            Button? btn = sender as Button;

            if (btn != null) {
                if (btn.Name.Equals("btnHome")) {
                    btn.Image = Properties.Resources.homepress;
                }
                else if (btn.Name.Equals("btnPre")) {
                    btn.Image = Properties.Resources.prevpress;
                }
                else if (btn.Name.Equals("btnOk")) {
                    btn.Image = Properties.Resources.confirmpress;
                }
            }
        }

        private void btn_MouseUp(object sender, MouseEventArgs e)
        {
            Button? btn = sender as Button;

            if (btn != null) {
                if (btn.Name.Equals("btnHome")) {
                    btn.Image = Properties.Resources.home;
                }
                else if (btn.Name.Equals("btnPre")) {
                    btn.Image = Properties.Resources.prev;
                }
                else if (btn.Name.Equals("btnOk")) {
                    btn.Image = Properties.Resources.confirm;
                }
            }
        }

        //private void dgvCarList_RowPostPaint(object sender, DataGridViewRowPostPaintEventArgs e)
        //{
        //    var dgv = sender as DataGridView;
        //    if (dgv.Rows[e.RowIndex].Selected) {
        //        using (var pen = new Pen(Color.Yellow, 3)) {
        //            Rectangle rect = e.RowBounds;

        //            // ★ 헤더 아래쪽부터만 그리기 (헤더 높이 보정)
        //            rect.Y = Math.Max(rect.Y, dgv.ColumnHeadersHeight);
        //            rect.Height = Math.Min(rect.Height, dgv.DisplayRectangle.Bottom - rect.Y) - 2;
        //            rect.Width -= 2;

        //            e.Graphics.DrawRectangle(pen, rect);
        //        }
        //    }
        //}

        private void dgvCarList_Paint(object sender, PaintEventArgs e)
        {/*
            var dgv = (DataGridView)sender;
            var clip = Rectangle.Intersect(dgv.DisplayRectangle, e.ClipRectangle);

            Color myPenColor = APSConfig.isContrast ? Color.Yellow : Color.Red;

            const int penWidth = 5;   // 기존 두께
            const int vMarginPx = 16;   // 위/아래 여백(줄 간격 벌리기용)

            using (var pen = new Pen(myPenColor, penWidth)) {
                pen.Alignment = System.Drawing.Drawing2D.PenAlignment.Inset;

                foreach (DataGridViewRow row in dgv.SelectedRows) {
                    if (!row.Displayed)
                        continue;

                    var r = dgv.GetRowDisplayRectangle(row.Index, true);
                    r = Rectangle.Intersect(r, clip);

                    if (dgv.ColumnHeadersVisible && r.Y < dgv.ColumnHeadersHeight) {
                        int dy = dgv.ColumnHeadersHeight - r.Y;
                        r.Y += dy;
                        r.Height -= dy;
                    }

                    // 위/아래를 잘라서 줄 간 테두리가 겹치지 않게 함
                    if (r.Width > 2 && r.Height > vMarginPx * 2 + 2) {
                        r.Y += vMarginPx;
                        r.Height -= vMarginPx * 2;

                        e.Graphics.DrawRectangle(pen, r);
                    }
                }
            }*/
        }

        private void dgvCarList_SystemColorsChanged(object sender, EventArgs e)
        {
            dgvCarList.Invalidate();

            BeginInvoke((Action)(() => btnOk.Focus()));
        }

        public void OnGoHome()
        {
            Result = FormResult.FormHome;
            _mainForm!.PlaySoundFile("btnHome1.mp3", AudioRouteState.Dual, 1);
            BeginInvoke((Action)(() => this.Close()));
        }

        private void dgvCarList_SelectionChanged(object sender, EventArgs e)
        {
            btnOk.Enabled = true;
            if (APSConfig.isContrast) {
                btnOk.Image = Properties.Resources.confirm_white;
                btnOk.BackColor = Color.FromArgb(17, 17, 17);
                btnOk.ForeColor = Color.White;
            }
            lastIndex = GetEndTabIndex();
            btnOk.Focus();
        }

        private void dgvCarList_RowPostPaint(object sender, DataGridViewRowPostPaintEventArgs e)
        {
            var dgv = (DataGridView)sender;
            var row = dgv.Rows[e.RowIndex];

            if (!row.Selected)
                return;

            // 선택 색은 이미 빼놨으니 테두리만 강조
            Color myPenColor = APSConfig.isContrast ? Color.Yellow : Color.Red;

            const int penWidth = 5;
            int vMarginPx = 14;   // 위/아래 잘라낼 여백
            if (APSConfig.isZoomed) {
                vMarginPx = 28;
            }
            using (var pen = new Pen(myPenColor, penWidth)) {
                pen.Alignment = System.Drawing.Drawing2D.PenAlignment.Inset;

                // 행 영역
                Rectangle r = e.RowBounds;

                // 헤더 아래쪽부터만 그리기
                if (dgv.ColumnHeadersVisible && r.Y < dgv.ColumnHeadersHeight) {
                    int dy = dgv.ColumnHeadersHeight - r.Y;
                    r.Y += dy;
                    r.Height -= dy;
                }

                // 위/아래 줄여서 라인 간 간격 확보
                if (r.Width > 2 && r.Height > vMarginPx * 2 + 2) {
                    r.Y += vMarginPx;
                    r.Height -= vMarginPx * 2;

                    e.Graphics.DrawRectangle(pen, r);
                }
            }
        }

        public void DoActiveButton()
        {
            this.BeginInvoke((Action)(() =>
            {
                if (btnOk.CanFocus)
                    btnOk.Focus();
            }));
        }


    }
}
