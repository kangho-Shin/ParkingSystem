using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JPXLpr.Novitec
{
    public class LprWorkItem
    {
        public int CameraNo { get; set; }
        public Bitmap? Bitmap { get; set; }
    }

    public class LprCameraResultEventArgs : EventArgs
    {
        public int CameraNo { get; private set; }
        public Bitmap? Bitmap { get; private set; }
        public string CarNum { get; private set; }
        public int Result { get; private set; }

        public LprCameraResultEventArgs(int cameraNo, Bitmap? bitmap, string carNum, int result)
        {
            CameraNo = cameraNo;
            Bitmap = bitmap;
            CarNum = carNum;
            Result = result;
        }
    }

    public class LprCameraImageEventArgs : EventArgs
    {
        public int CameraNo { get; private set; }
        public Bitmap Bitmap { get; private set; }

        public LprCameraImageEventArgs(int cameraNo, Bitmap bitmap)
        {
            CameraNo = cameraNo;
            Bitmap = bitmap;
        }
    }
}
