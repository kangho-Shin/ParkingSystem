using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading;
using System.IO;

namespace JPXLpr.Novitec
{
    /// <summary>
    /// The IPCamera class with the callback functionality.
    /// </summary>
    public class IPCameraAsync : IPCamera
    {
        /// <summary>
        /// 
        /// </summary>
        public event EventHandler<ImageGrabbedEventArgs>? ImageGrabbed;

        /// <summary>
        /// 
        /// </summary>
        public event EventHandler<ImageGrabbedEventArgs>? ImageGrabFailed;

        private Thread m_grabThread;
        private bool m_keepGrab;

        //private Thread m_decodeThread;
        //private bool m_keepDecode;

        private AutoResetEvent m_startDecode = new AutoResetEvent(false);

        /// <summary>
        /// To receive callback, user needs to call this method.
        /// </summary>
        public void StartGrab()
        {
            if (m_keepGrab) return;

            m_grabThread = new Thread(GrabLoop);
            m_grabThread.IsBackground = true;
            m_keepGrab = true;
            m_grabThread.Start();

            //m_decodeThread = new Thread(DecodeLoop);
            //m_decodeThread.IsBackground = true;
            //m_keepDecode = true;
            //m_decodeThread.Start();
        }

        /// <summary>
        /// To stop receiving callback, user needs to call this method.
        /// </summary>
        public void StopGrab()
        {
            if (!m_keepGrab) return;

            m_keepGrab = false;
            m_grabThread.Join(1000);

            //m_keepDecode = false;
            //m_decodeThread.Join(1000);
        }

        /// <summary>
        /// byte[] m_jpegBuffer;
        /// </summary>
        /// <param name="threadParam"></param>

        //private void DecodeLoop(object threadParam)
        //{
        //    while (m_keepDecode) {
        //        if (m_startDecode.WaitOne(100)) {
        //            try {
        //                using (MemoryStream jpegStrm = new MemoryStream(m_jpegBuffer)) {
        //                    using (Image img = Image.FromStream(jpegStrm)) {
        //                        Bitmap bitmap = new Bitmap(img);

        //                        if (ImageGrabbed != null)
        //                            ImageGrabbed(this, new ImageGrabbedEventArgs(bitmap, null, IPCamError.OK));
        //                    }
        //                }
        //            }
        //            catch (Exception ex) {
        //                Console.WriteLine($"DecodeLoop { ex.ToString()}");
        //            }
        //        }
        //    }
        //}

        private void GrabLoop(object threadParam)
        {
            while (m_keepGrab)
            {
                IPCamError err;
                MetaInfo metainfo;

                if (m_isUDPStreaming) {
                    err = GetImage_UDP(1000, out metainfo);
                }
                else {
                    err = SendPing();
                    err = GetImage_TCP(1000, out metainfo);
                }

                if ( err == IPCamError.OK ) {
                    if ( ImageGrabbed != null ) {
                        Bitmap? bitmap = null;

                        if ( metainfo.Type == 1 ) { // JPG
                            bitmap = m_bitmap;
                        }
                        else { // YUV
                            bitmap = m_YUV2RGB_Converter.GetBitmap_YUVtoRGB_incremental();
                        }
                        ImageGrabbed(this, new ImageGrabbedEventArgs(bitmap, metainfo, IPCamError.OK));
                    }
                }
                else if (err != IPCamError.Timeout) {
                    if (ImageGrabFailed != null)
                        ImageGrabFailed(this, new ImageGrabbedEventArgs(null, null, err));
                }
            }
        }

        public bool CameraPing()
        {
            string model;
            IPCamError err = GetSerialNumber(out model);
            if ( err != IPCamError.OK ) {
                if (ImageGrabFailed != null)
                    ImageGrabFailed(this, new ImageGrabbedEventArgs(null, null, err));
            }
            return err == IPCamError.OK;
        }
    }


    public class ImageGrabbedEventArgs : EventArgs
    {
        public readonly Bitmap? bitmap;
        public readonly IPCamError error;
        public readonly MetaInfo? metaInfo;

        public ImageGrabbedEventArgs(Bitmap? _bitmap, MetaInfo? _metaInfo, IPCamError _error)
        {
            bitmap = _bitmap;
            error = _error;
            metaInfo = _metaInfo;
        }
    }
}
