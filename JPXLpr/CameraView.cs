using JPXLpr.HelpClass;
using JPXLpr.Novitec;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace JPXLpr
{
    public partial class CameraView : UserControl
    {
        public IPCameraAsync? _camera;
        private object _carLock = new object();
        private JPXLpr? _mainForm { get; set; }

        public bool _isBusy { get; set; }
        public int CameraNo { get; set; }
        public string? Ip { get; set; }
        public bool isVideoMode { get; set; } = false;
        public bool isRunning { get; set; } = false;

        public event EventHandler<LprCameraImageEventArgs>? ImageReceived;

        public event EventHandler<LprCameraImageEventArgs>? CameraError;

        public int _setExposure;
        public int _oldExposure { get; set; } = -1;
        public int _bracketMode { get; set; } = 0;
        public int _bracketCnt { get; set; } = 1;

        public CAMINFO? _CamInfo;
        public ExpSchedule? _expSchedule;

        private System.Threading.Timer? _triggerTimer;
        private System.Threading.Timer? _scheduleTimer;
        private System.Threading.Timer? _resetTimer;

        private int _preCount;
        private bool _preCapture = true;
        private int _monoCamera = -1;


        public CameraView()
        {
            InitializeComponent();
        }

        private void CameraView_Load(object sender, EventArgs e)
        {
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
                return;

        }

        public void SetCameraInfo(JPXLpr mainForm, CAMINFO camInfo, ExpSchedule expSchedule)
        {
            _mainForm = mainForm;
            _CamInfo = camInfo;
            _expSchedule = expSchedule;
        }

        private void InitTimers()
        {
            _preCount = 5;
            _preCapture = true;

            if( _triggerTimer != null ) {
                _triggerTimer.Dispose();
                _triggerTimer = null;
            }
            if (_scheduleTimer != null) {
                _scheduleTimer.Dispose();
                _scheduleTimer = null;
            }

            _triggerTimer = new System.Threading.Timer(TriggerTimer_Proc, null, 0, 1500);
            _scheduleTimer = new System.Threading.Timer(ScheduleTimer_Proc, null, 0, 1000);

            SetMessage($"자동조정중");
        }

        private void TriggerTimer_Proc(object? state)
        {
            if (!isRunning)
                return;

            if (_preCount <= 0)
                return;

            if (_camera != null) {
                _camera.SetTriggerSource(true);
                _camera.SetForcedTrigger();
                _camera.SetTriggerSource(false);
            }

            if ( --_preCount == 0 ) {
                _triggerTimer?.Dispose();
                _triggerTimer = null;

                _camera!.SetFilterSwitch(0);
                _camera!.SetMonochrome(1);
                pingCount = 598;
                SetMessage($"설정완료");
            }
        }

        private int pingCount = 0;

        private void ScheduleTimer_Proc(object? state)
        {
            if ( !isRunning ) {
                return;
            }
            if( pingCount++ >= 600 ) {
                if (_camera != null) {
                    _camera.CameraPing();
                }
                pingCount = 0;

                DateTime now = DateTime.Now;
                int xtick = now.Hour * 60 + now.Minute;
                int nMon = now.Month;

                ExpInfo expInfo = _expSchedule!.ExpVal[nMon];

                if (!_CamInfo!.schedule || _camera == null)
                    return;

                bool isDay = xtick >= expInfo.STick && xtick <= expInfo.ETick;
                int newExposure = isDay ? expInfo.ExpDayMin : expInfo.ExpNightMin;
                if (_setExposure != newExposure) {
                    SetMessage($"Expose : {_setExposure}-{newExposure}");
                    _setExposure = newExposure;

                    if (_oldExposure == -1)
                        _camera.SetExposure(_setExposure);
                    else
                        _camera.SetExposure(_oldExposure);
                }
/*
                if ( isDay ) {
                    if( _monoCamera != 0 ) {
                        _monoCamera = 0;
                        _camera.SetFilterSwitch(1);
                        _camera.SetMonochrome(_monoCamera);
                    }
                }
                else {
                    if ( _monoCamera != 1 ) {
                        _monoCamera = 1;
                        _camera.SetFilterSwitch(0);
                        _camera.SetMonochrome(_monoCamera);
                    }
                }
*/
                if ( _CamInfo.autoaec || _CamInfo.autoagc ) {
                    ALC xalc = new ALC();

                    xalc.enableAEC = _CamInfo.autoaec;
                    xalc.enableAGC = _CamInfo.autoagc;
                    xalc.enableAIC = false;

                    xalc.target = isDay ? _CamInfo.daytargetlum : _CamInfo.nighttargetlum;

                    xalc.minGain = (double)_CamInfo.agcmin;
                    xalc.maxGain = (double)_CamInfo.agcmax;

                    xalc.minExposure = isDay ? expInfo.ExpDayMin : expInfo.ExpNightMin;
                    xalc.maxExposure = isDay ? expInfo.ExpDayMax : expInfo.ExpNightMax;

                    _camera.SetALC(xalc);
                }
            }
        }

        private void ResetTimer_Proc(object? state)
        {
            _resetTimer?.Dispose();
            _resetTimer = null;

            Disconnect();

            StartCamera(Ip, CameraNo);
            SetMessage($"카메라{Ip} 재연결중");
        }

        public bool StartCamera(string ip, int camNo)
        {
            CameraNo = camNo;
            Ip = ip;

            _camera = new IPCameraAsync();
            _camera.ImageGrabbed += Camera_ImageGrabbed;
            _camera.ImageGrabFailed += Camera_ImageGrabFailed;
            SetMessage("카메라연결중");
            var ret1 = _camera.ConnectCommandPort(Ip);
            var ret2 = _camera.ConnectStreamPort(Ip, true); // true:UDP   false:tcp

            if ( ret1 != IPCamError.OK || ret2 != IPCamError.OK ) {
                if (_resetTimer == null) {
                    _resetTimer = new System.Threading.Timer(ResetTimer_Proc, null, 5000, Timeout.Infinite);
                }
                Console.WriteLine("Camera Socket Connected Error {0}", ip);
                return false;
            }

            Console.WriteLine("Camera Socket Connected Ok {0}", ip);
            _camera.SetBracketMode(false, 1);
            if (_CamInfo != null) {
                if (_CamInfo.triggermode == 0) {
                    _camera.SetTriggerMode(_CamInfo.triggermode, false);
                }
                else {
                    if (_CamInfo.triggerpolarity == 1) {
                        _camera.SetTriggerMode(_CamInfo.triggermode, true);
                    }
                    else {
                        _camera.SetTriggerMode(_CamInfo.triggermode, false);
                    }
                }
                _camera.SetFlash(_CamInfo.flashmode, _CamInfo.flashpolarity == 1 ? true : false);

                _camera.SetALCArea(_CamInfo.alcstartx, _CamInfo.alcstarty, _CamInfo.alcwidth, _CamInfo.alcheight);
            
                CameraParamSetting();
                _camera.SetTriggerSource(false);
                _camera.SetIris(_CamInfo.IrisVal);
            }
            // ; 0 : Free Run 1 : One Shot    2 : Mixed    3 : Pseudo
            _camera.SetTriggerMode(1, true);
            _camera.StartGrab();
            isRunning = true;

            InitTimers();
            return true;
        }

        private void StopTimers()
        {
            _triggerTimer?.Dispose();
            _triggerTimer = null;

            _scheduleTimer?.Dispose();
            _scheduleTimer = null;

            _resetTimer?.Dispose();
            _resetTimer = null;
        }

        public void Disconnect()
        {
            isRunning = false;

            StopTimers();

            if (_camera != null) {
                _camera.ImageGrabbed -= Camera_ImageGrabbed;
                _camera.ImageGrabFailed -= Camera_ImageGrabFailed;

                _camera.StopGrab();
                _camera.DisconnectStreamPort();
                _camera.DisconnectCommandPort();

                _camera = null;
            }

            if (picBox.Image != null) {
                picBox.Image.Dispose();
                picBox.Image = null;
            }
        }

        private void Camera_ImageGrabbed(object? sender, ImageGrabbedEventArgs e)
        {
            if ( e.bitmap == null ) return;

            if ( _preCount == 0 && _preCapture == true ) {
                _preCapture = false;
                return;
            }
            try {
                using (Bitmap src = new Bitmap(e.bitmap)) {
                    Bitmap viewBitmap = new Bitmap(src);
                    if (!_preCapture) {
                        Bitmap recogBitmap = new Bitmap(src);
                        ImageReceived?.Invoke(this, new LprCameraImageEventArgs(CameraNo, recogBitmap));
                    }
                    ShowCameraImage(viewBitmap);
                }
            }
            catch {
                // 필요하면 로그만
            }
        }

        private void ShowCameraImage(Bitmap bitmap)
        {
            if (InvokeRequired) {
                if (!IsDisposed && IsHandleCreated)
                    BeginInvoke(new Action(() => ShowCameraImage(bitmap)));
                else
                    bitmap.Dispose();

                return;
            }

            if (picBox == null || picBox.IsDisposed) {
                bitmap.Dispose();
                return;
            }

            Image oldImage = picBox.Image;
            picBox.Image = bitmap;
            oldImage?.Dispose();
        }

        private void Camera_ImageGrabFailed(object? sender, ImageGrabbedEventArgs e)
        {
            if (_resetTimer != null) return;
            Console.WriteLine(e.ToString());
            _resetTimer = new System.Threading.Timer(ResetTimer_Proc, null, 1000, Timeout.Infinite);
        }

        private void btnTrigger_Click(object sender, EventArgs e)
        {
            IPCamError err1, err2, err3;

            if (_camera != null) {
                err1 = _camera.SetTriggerSource(true);   // SW Trigger
                err2 = _camera.SetForcedTrigger();
                err3 = _camera.SetTriggerSource(false);
                Console.WriteLine($" {err1}-{err2}-{err3} ");
            }
        }

        private void btnSetup_Click(object sender, EventArgs e)
        {
            if (_mainForm!.isManagerMode == true) {
                AdvFeatureForm frm = new AdvFeatureForm(this, 0);

                frm.ShowDialog();
            }
        }


        public void VideoMode(bool mode)
        {
            if (mode) {
                _camera?.SetTriggerMode(0, false);
            }
            else {
                if (_CamInfo.triggerpolarity == 1) {
                    _camera?.SetTriggerMode(1, true);
                }
                else {
                    _camera?.SetTriggerMode(1, false);
                }
                Task.Delay(2000).Wait();
            }
            isVideoMode = mode;
            chVideo.Checked = mode;
        }

        public event EventHandler? ViewDoubleClicked;
        private void picBox_DoubleClick(object sender, EventArgs e)
        {
            ViewDoubleClicked?.Invoke(this, EventArgs.Empty);
        }

        public void CameraParamSetting()
        {
            DateTime now = DateTime.Now;

            int nMon = now.Month;
            int xtick = now.Hour * 60 + now.Minute;
            if (_camera == null) return;
            //_expSchedule
            _camera.SetTotalGain((double)_CamInfo.gain);
            ExpInfo expInfo = _expSchedule.ExpVal[nMon];
            int intVal = 63 - _CamInfo.jpegq;
            _camera.SetJPEGQuality(intVal);

            _camera.SetFrameRate(_CamInfo.framerate);

            if (_CamInfo.autoaec || _CamInfo.autoagc) {
                ALC xalc = new ALC();

                xalc.enableAEC = _CamInfo.autoaec;
                xalc.enableAGC = _CamInfo.autoagc;
                xalc.enableAIC = false;

                bool isDayTime = xtick >= expInfo.STick && xtick <= expInfo.ETick;

                xalc.target = isDayTime ? _CamInfo.daytargetlum : _CamInfo.nighttargetlum;

                xalc.minGain = (double)_CamInfo.agcmin;
                xalc.maxGain = (double)_CamInfo.agcmax;

                if (_CamInfo.schedule) {
                    if (isDayTime) {
                        _setExposure = expInfo.ExpDayMin;
                        _camera.SetExposure(_oldExposure == -1 ? _setExposure : _oldExposure);

                        xalc.minExposure = expInfo.ExpDayMin;
                        xalc.maxExposure = expInfo.ExpDayMax;
                    }
                    else {
                        _setExposure = expInfo.ExpNightMin;
                        _camera.SetExposure(_oldExposure == -1 ? _setExposure : _oldExposure);

                        xalc.minExposure = expInfo.ExpNightMin;
                        xalc.maxExposure = expInfo.ExpNightMax;
                    }
                }
                else {
                    xalc.minExposure = _CamInfo.aecmin;
                    xalc.maxExposure = _CamInfo.aecmax;
                }

                _camera.SetALC(xalc);
            }
            else {
                _camera.SetExposure(_CamInfo.exposuretime);
                _setExposure = _CamInfo.exposuretime;
            }

            if (_bracketMode == 1) {
                _camera.SetTriggerImageCount(_bracketCnt);
            }
            else {
                _camera.SetTriggerImageCount(1);
            }

            SetMessage("파라미터설정");
        }

        public void SetMessage(string carNumber)
        {
            if (IsDisposed || !IsHandleCreated)
                return;

            if (lblStatus.InvokeRequired) {
                BeginInvoke(new Action(() => SetMessage(carNumber)));
                return;
            }

            lblStatus.Text = carNumber;
        }

        private async void chVideo_CheckedChanged(object sender, EventArgs e)
        {
            if (chVideo.Checked) {
                isVideoMode = true;
                _camera?.SetTriggerMode(0, false);
            }
            else {
                
                if (_CamInfo.triggerpolarity == 1) {
                    _camera?.SetTriggerMode(1, true);
                }
                else {
                    _camera?.SetTriggerMode(1, false);
                }
                await Task.Delay(2000);
                isVideoMode = false;
            }
        }

        public void PerformButtonClick()
        {
            Console.Write($"[{CameraNo}] 호출");
            btnTrigger.PerformClick();
        }
    }
}
