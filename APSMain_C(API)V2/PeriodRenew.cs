using APSMain.BaseClass;
using APSMain.DbModels;
using APSMain.TTSLib;
using Dapper;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace APSMain
{
    public partial class PeriodRenew : Form, ISubFormResult<string>, IActiveForm
    {
        public ZoomWrapper? _zoomWrap;
        private ContrastToggler _contrast => APSConfig.Contrast;
        string _carNumber = string.Empty;   // 차량 번호 저장
        public Action<Form, Keys, int, int>? RouteArrow { get; set; }
        public FormResult Result { get; private set; } = FormResult.FormCancel;
        public string ResultData { get; private set; } = "";

        private bool _isHanMode = false;
        private readonly CheonJiInAutomata _automata = new CheonJiInAutomata();
        private Button? _lastHanButton = null;
        private DateTime _lastHanButtonTime = DateTime.MinValue;

        private const int HanCycleMs = 1000;
        private MainForm? _mainForm;
        Tperiodmember? member = null;
        private bool searchok = false;

        private int lastIndex = 0;
        private bool _parentZoom = false;

        public PeriodRenew()
        {
            InitializeComponent();

            _mainForm = APSConfig.mainForm;
            if (_mainForm != null) {
                RouteArrow = _mainForm.ProcessArrow;
            }

            _parentZoom = APSConfig.isZoomed;
        }

        private void PeriodRenew_Load(object sender, EventArgs e)
        {
            _zoomWrap = new ZoomWrapper(pZoomContent);

            SetInputMode(true);

            btnOk.Enabled = false;
            btnPayment.Enabled = false;
            if (APSConfig.isContrast) {
                btnOk.Image = Properties.Resources.confirm_gray;
                btnOk.BackColor = SystemColors.Control;
                btnOk.ForeColor = SystemColors.ControlText;
            }

            panInfo.Paint += (s, e) =>
            {
                DateTime now = DateTime.Now;
                DateTime nextMonthEnd;

                var g = e.Graphics;
                Font font = new Font("맑은 고딕", 34, FontStyle.Bold, GraphicsUnit.Pixel);
                var rect = panInfo.ClientRectangle;

                if (searchok) {
                    TextRenderer.DrawText(g, "차량번호", font, new Rectangle(10, 10, 180, 50), Color.Blue, TextFormatFlags.VerticalCenter);
                    TextRenderer.DrawText(g, "종료일", font, new Rectangle(10, 58, 180, 50), Color.Blue, TextFormatFlags.VerticalCenter);
                    TextRenderer.DrawText(g, "연장일", font, new Rectangle(10, 106, 180, 50), Color.Blue, TextFormatFlags.VerticalCenter);
                    TextRenderer.DrawText(g, "상태", font, new Rectangle(10, 154, 180, 50), Color.Blue, TextFormatFlags.VerticalCenter);
                    TextRenderer.DrawText(g, "월주차비", font, new Rectangle(10, 202, 180, 50), Color.Blue, TextFormatFlags.VerticalCenter);
                    TextRenderer.DrawText(g, "결제금액", font, new Rectangle(10, 250, 180, 50), Color.Blue, TextFormatFlags.VerticalCenter);

                    TextRenderer.DrawText(g, ":", font, new Rectangle(170, 10, 40, 50), Color.Blue, TextFormatFlags.VerticalCenter);
                    TextRenderer.DrawText(g, ":", font, new Rectangle(170, 58, 40, 50), Color.Blue, TextFormatFlags.VerticalCenter);
                    TextRenderer.DrawText(g, ":", font, new Rectangle(170, 106, 40, 50), Color.Blue, TextFormatFlags.VerticalCenter);
                    TextRenderer.DrawText(g, ":", font, new Rectangle(170, 154, 40, 50), Color.Blue, TextFormatFlags.VerticalCenter);
                    TextRenderer.DrawText(g, ":", font, new Rectangle(170, 202, 40, 50), Color.Blue, TextFormatFlags.VerticalCenter);
                    TextRenderer.DrawText(g, ":", font, new Rectangle(170, 250, 40, 50), Color.Blue, TextFormatFlags.VerticalCenter);

                    if (member != null) {
                        TextRenderer.DrawText(g, member.Carnum1, font, new Rectangle(200, 10, 400, 50), Color.Black, TextFormatFlags.VerticalCenter);
                        string edate = member.Enddate.ToString("yyyy-MM-dd");
                        nextMonthEnd = new DateTime(now.Year, now.Month, 1).AddMonths(2).AddDays(-1);
                        string ndate = nextMonthEnd.ToString("yyyy-MM-dd");
                        string price = $"{member.Parkprice} 원";
                        TextRenderer.DrawText(g, edate, font, new Rectangle(200, 58, 400, 50), Color.Black, TextFormatFlags.VerticalCenter);
                        TextRenderer.DrawText(g, ndate, font, new Rectangle(200, 106, 400, 50), Color.Black, TextFormatFlags.VerticalCenter);
                        TextRenderer.DrawText(g, "연장가능", font, new Rectangle(200, 154, 400, 50), Color.Red, TextFormatFlags.VerticalCenter);
                        TextRenderer.DrawText(g, price, font, new Rectangle(200, 202, 400, 50), Color.Black, TextFormatFlags.VerticalCenter);
                        TextRenderer.DrawText(g, price, font, new Rectangle(200, 250, 400, 50), Color.Black, TextFormatFlags.VerticalCenter);
                    }

                }
                else {
                    //TextRenderer.DrawText(g, "전체 차량번호 입력 후", font, new Rectangle(10, 106, 495, 50), Color.Black, TextFormatFlags.HorizontalCenter);
                    //TextRenderer.DrawText(g, "[차량조회]를 눌러주세요", font, new Rectangle(10, 154, 495, 50), Color.Black, TextFormatFlags.HorizontalCenter);

                    string t1_1 = "전체 ";
                    string t1_2 = "차량번호";
                    string t1_3 = " 입력 후";

                    Size s1 = TextRenderer.MeasureText(g, t1_1 + t1_2 + t1_3, font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding);
                    int startX1 = 10 + (495 - s1.Width) / 2;
                    int y1 = 106;

                    Size s1_1 = TextRenderer.MeasureText(g, t1_1, font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding);
                    Size s1_2 = TextRenderer.MeasureText(g, t1_2, font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding);

                    TextRenderer.DrawText(g, t1_1, font, new Point(startX1, y1), Color.Black, TextFormatFlags.NoPadding);
                    TextRenderer.DrawText(g, t1_2, font, new Point(startX1 + s1_1.Width, y1), Color.Blue, TextFormatFlags.NoPadding);
                    TextRenderer.DrawText(g, t1_3, font, new Point(startX1 + s1_1.Width + s1_2.Width, y1), Color.Black, TextFormatFlags.NoPadding);

                    string t2_1 = "[차량조회]";
                    string t2_2 = "를 눌러주세요";

                    Size s2 = TextRenderer.MeasureText(g, t2_1 + t2_2, font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding);
                    int startX2 = 10 + (495 - s2.Width) / 2;
                    int y2 = 154;

                    Size s2_1 = TextRenderer.MeasureText(g, t2_1, font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding);

                    TextRenderer.DrawText(g, t2_1, font, new Point(startX2, y2), Color.Blue, TextFormatFlags.NoPadding);
                    TextRenderer.DrawText(g, t2_2, font, new Point(startX2 + s2_1.Width, y2), Color.Black, TextFormatFlags.NoPadding);

                }

            };
        }

        private void KeyButton_Click(object sender, EventArgs e)
        {
            if (sender is not Button btn)
                return;

            string key = btn.Tag?.ToString() ?? string.Empty;
            if (string.IsNullOrEmpty(key))
                return;

            if (key == "DEL") {
                ResetHanCycle();

                if (_isHanMode) {
                    _automata.Backspace();
                    txtCarNum.Text = _automata.GetText();
                }
                else {
                    _automata.Backspace();
                    txtCarNum.Text = _automata.GetText();
                }

                txtCarNum.SelectionStart = txtCarNum.Text.Length;
                return;
            }

            if (_isHanMode) {
                if (key == "ㆍ" || key == "ㅡ" || key == "ㅣ") {
                    ResetHanCycle();
                    _automata.InputVowel(key[0]);
                }
                else {
                    bool isSameCycle = _lastHanButton == btn && (DateTime.Now - _lastHanButtonTime).TotalMilliseconds <= HanCycleMs;

                    string hanKey = GetCycleConsonant(btn);

                    _automata.InputConsonant(hanKey, isSameCycle);

                    _lastHanButton = btn;
                    _lastHanButtonTime = DateTime.Now;
                }

                txtCarNum.Text = _automata.GetText();
            }
            else {
                ResetHanCycle();
                _automata.AppendText(key);
                txtCarNum.Text = _automata.GetText();
            }

            txtCarNum.SelectionStart = txtCarNum.Text.Length;
        }

        private void UpdateDisplay()
        {
            if (_isHanMode)
                txtCarNum.Text = _automata.GetText();

            txtCarNum.SelectionStart = txtCarNum.Text.Length;
        }

        private void SetInputMode(bool hanMode)
        {
            _isHanMode = hanMode;

            if (!_isHanMode)
                _automata.Commit();

            btnMode.Text = _isHanMode ? "숫자 (*)" : "한글 (*)";

            btnNum1.Text = _isHanMode ? "ㄱㅋ" : "1";
            btnNum2.Text = _isHanMode ? "ㄴ" : "2";
            btnNum3.Text = _isHanMode ? "ㄷㅌ" : "3";
            btnNum4.Text = _isHanMode ? "ㄹ" : "4";
            btnNum5.Text = _isHanMode ? "ㅁ" : "5";
            btnNum6.Text = _isHanMode ? "ㅂㅍ" : "6";
            btnNum7.Text = _isHanMode ? "ㅅㅎ" : "7";
            btnNum8.Text = _isHanMode ? "ㅇ" : "8";
            btnNum9.Text = _isHanMode ? "ㅈㅊ" : "9";


            btnNum1.Tag = _isHanMode ? "HAN" : "1";
            btnNum2.Tag = _isHanMode ? "HAN" : "2";
            btnNum3.Tag = _isHanMode ? "HAN" : "3";
            btnNum4.Tag = _isHanMode ? "HAN" : "4";
            btnNum5.Tag = _isHanMode ? "HAN" : "5";
            btnNum6.Tag = _isHanMode ? "HAN" : "6";
            btnNum7.Tag = _isHanMode ? "HAN" : "7";
            btnNum8.Tag = _isHanMode ? "HAN" : "8";
            btnNum9.Tag = _isHanMode ? "HAN" : "9";

            if (_isHanMode) {
                btnNum0.Text = "";
                btnNum0.Tag = "";
            }
            else {
                btnNum0.Text = "0";
                btnNum0.Tag = "0";
            }
            btnDot.Visible = _isHanMode;
            btnEu.Visible = _isHanMode;
            btnI.Visible = _isHanMode;

            UpdateDisplay();
        }

        private void ToggleInputMode()
        {
            UpdateDisplay();
            SetInputMode(!_isHanMode);
        }

        private void btnMode_Click(object sender, EventArgs e)
        {
            ToggleInputMode();
        }

        private string GetHanKey(Button btn)
        {
            string text = btn.Text.Replace(" ", "");
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            if (text.Length <= 1)
                return text;

            DateTime now = DateTime.Now;

            if (_lastHanButton == btn && (now - _lastHanButtonTime).TotalMilliseconds <= HanCycleMs) {
                _lastHanButtonTime = now;

                return text.Length switch
                {
                    2 => text[1].ToString(),
                    3 => text[1].ToString(),
                    _ => text[1].ToString()
                };
            }

            _lastHanButton = btn;
            _lastHanButtonTime = now;
            return text[0].ToString();
        }

        private void ResetHanCycle()
        {
            _lastHanButton = null;
            _lastHanButtonTime = DateTime.MinValue;
        }

        private string GetCycleConsonant(Button btn)
        {
            string text = btn.Text.Replace(" ", "");
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            DateTime now = DateTime.Now;

            if (_lastHanButton == btn &&
                (now - _lastHanButtonTime).TotalMilliseconds <= HanCycleMs) {
                _lastHanButtonTime = now;

                return text.Length >= 2 ? text[1].ToString() : text[0].ToString();
            }

            _lastHanButton = btn;
            _lastHanButtonTime = now;
            return text[0].ToString();
        }

        private void btnHome_Click(object sender, EventArgs e)
        {
            Result = FormResult.FormHome;
            _mainForm!.PlaySoundFile("btnHome1.mp3", AudioRouteState.Dual, 1);
            BeginInvoke((Action)(() => this.Close()));
        }

        private void btnPre_Click(object sender, EventArgs e)
        {
            Result = FormResult.FormPre;
            APSConfig.isZoomed = _parentZoom;
            _mainForm!.PlaySoundFile("btnPre1.mp3", AudioRouteState.Dual, 1);
            BeginInvoke((Action)(() => this.Close()));
        }

        private void btnOk_Click(object sender, EventArgs e)
        {

        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            //using IDbConnection db = new MySqlConnection(_mainForm!._connstr);

            //string query = "select * from tperiodmember where carnum1=@carnum";
            //member = db.QueryFirstOrDefault<Tperiodmember>(query, new { carnum = txtCarNum.Text });

            //if (member != null) {
            //    if (member.Enddate.Month == DateTime.Now.Month) {
            //        btnPayment.Enabled = true;
            //        //btnOk.Enabled = true;
            //        if (APSConfig.isContrast) {
            //            btnOk.Image = Properties.Resources.confirm_white;
            //            btnOk.BackColor = Color.FromArgb(17, 17, 17);
            //            btnOk.ForeColor = Color.White;
            //        }
            //        searchok = true; // 서울1가1001
            //        panInfo.Invalidate();
            //        //lastIndex = GetEndTabIndex();
            //        btnOk.Focus();
            //    }
            //    else {// "기간연장을 할 수 없습니다.\n관리실에 문의 부탁드립니다.",
            //        UiHelpers.ShowMessage(this, 14, true);
            //    }
            //}
            //else {
            //    _mainForm?.PlaySoundFile("error.mp3", AudioRouteState.Dual, 1);
            //    UiHelpers.ShowMessage(this, 0, true);
            //    Task.Delay(1000).Wait();
            //    _mainForm?.PlaySoundFile("incar01.mp3", AudioRouteState.Dual);
            //}
        }

        private void btnPayment_Click(object sender, EventArgs e)
        {

        }

        #region Process Contrast
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            var key = keyData & Keys.KeyCode;
            if (key is Keys.Left or Keys.Right) {
                if (this.ActiveControl is TextBoxBase or ComboBox)
                    return base.ProcessCmdKey(ref msg, keyData);

                RouteArrow?.Invoke(this, key, 0, lastIndex);
                return true;
            }
            else if (keyData is Keys.F) {           // ⏹️ 홈
                BeginInvoke((Action)(() => OnGoHome()));
                return true;
            }
            else if (keyData is Keys.Escape) {      // ✖️ 이전
                BeginInvoke((Action)(() => OnCancel()));
                return true;
            }
            else if (keyData is Keys.Enter) { // 🔘⏺️
                if (this.ActiveControl is IButtonControl btn) {
                    btn.PerformClick();
                    return true;
                }
            }

            return base.ProcessCmdKey(ref msg, keyData);
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
        #endregion

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

        public void ZoomMovedQuard(int pos)
        {
            _zoomWrap?.SnapToQuadrant(pos);
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
            BeginInvoke((Action)(() => this.Close()));
        }

        public void GiveFocus()
        {
            this.Focus();

            btnOk.Enabled = true;
            if (APSConfig.isContrast) {
                btnOk.Image = Properties.Resources.confirm_white;
                btnOk.BackColor = Color.FromArgb(17, 17, 17);
                btnOk.ForeColor = Color.White;
            }
            lastIndex = GetEndTabIndex();
            btnOk.Focus();
        }

        public void OnGoHome()
        {
            Result = FormResult.FormHome;
            _mainForm!.PlaySoundFile("btnHome1.mp3", AudioRouteState.Dual, 1);
            BeginInvoke((Action)(() => this.Close()));
        }

        public void DoActiveButton()
        {
            this.BeginInvoke((Action)(() =>
            {
                if (btnOk.CanFocus)
                    btnOk.Focus();
            }));
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
        #endregion

        private void PeriodRenew_Shown(object sender, EventArgs e)
        {
            ApplyFocusStyleToAllButtons(this, thick: 6);

            _contrast.Register(this);   // 루트(폼) 등록
            _contrast.SaveBase(this);   // 현재 상태 스냅샷 저장

            if (APSConfig.isContrast)   // 전역 모드가 이미 ON이라면 즉시 적용
                _contrast.ApplyHighContrast(this);

            APSConfig.ContrastChanged += OnContrastChanged;   // 전역 신호 구독

            if (APSConfig.isZoomed) {
                APSConfig.isZoomed = false;
                ZoomInOut();
            }
            GiveFocus();
        }
    }
}
