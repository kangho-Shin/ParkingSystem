using JPXLpr.Novitec;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JPXLpr
{
    public class LprCameraUnit
    {
        public readonly IPCameraAsync _camera;
        private readonly object _carLock = new object();

        private bool _isBusy { get; set; }

        public int CameraNo { get; set; }
        public string Ip { get; set; }

        public event EventHandler<LprCameraImageEventArgs>? ImageReceived;

        public LprCameraUnit(int cameraNo, string ip)
        {
            CameraNo = cameraNo;
            Ip = ip;

            _camera = new IPCameraAsync();
            _camera.ImageGrabbed += Camera_ImageGrabbed;
            _camera.ImageGrabFailed += Camera_ImageGrabFailed;
        }

        public bool Connect()
        {
            var ret1 = _camera.ConnectCommandPort(Ip);
            var ret2 = _camera.ConnectStreamPort(Ip, false);

            if (ret1 != IPCamError.OK) return false;
            if (ret2 != IPCamError.OK) return false;

            _camera.SetTriggerSource(false);
            _camera.SetTriggerMode(1, true);
            _camera.StartGrab();

            return true;
        }

        public void Disconnect()
        {
            _camera.StopGrab();
            _camera.DisconnectStreamPort();
            _camera.DisconnectCommandPort();
        }

        private void Camera_ImageGrabbed(object? sender, ImageGrabbedEventArgs e)
        {
            if (e.bitmap == null) return;

            try {
               // Bitmap bitmap = new Bitmap(e.bitmap);

                ImageReceived?.Invoke(this, new LprCameraImageEventArgs(CameraNo, e.bitmap));
            }
            catch {
                // 필요하면 로그만
            }
        }

        private void Camera_ImageGrabFailed(object? sender, ImageGrabbedEventArgs e)
        {
            // 필요하면 로그만
        }

        public void TriggerGrabAsync()
        {
            _camera.SetTriggerSource(true);   // SW Trigger
            _camera.SetTriggerMode(1, true);  // One-shot
            _camera.SetForcedTrigger();
        }
    }

}
