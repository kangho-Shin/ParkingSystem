using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace JPXLpr.Novitec
{
    public class NovaeyeWrapper : IDisposable
    {
        private const string DllName = "novaeye.dll";
        private IntPtr _hdnl;
        private bool _disposed = false;

        public NovaeyeWrapper()
        {
            _hdnl = IntPtr.Zero;
        }

        public int Create()
        {
            if (_hdnl != IntPtr.Zero)
                return 0;

            _hdnl = nvt_alpr_create();

            if (_hdnl == IntPtr.Zero)
                return -1;

            return 0;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (_hdnl != IntPtr.Zero) {
                nvt_alpr_destroy(_hdnl);
                _hdnl = IntPtr.Zero;
            }

            _disposed = true;
        }

        ~NovaeyeWrapper()
        {
            Dispose(false);
        }

        // Structures
        [StructLayout(LayoutKind.Sequential)]
        public struct ROI
        {
            public int x;
            public int y;
            public int width;
            public int height;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct NativeALPRResult
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
            public byte[] text;
            public ROI area;
            public float confidence;
        }

        public struct ALPRResult
        {
            public string text;
            public ROI area;
            public float confidence;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ALPRConfig
        {
            public int mode;
            public ROI area;
            public int max_detection;
        }

        // P/Invoke function declarations
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr nvt_alpr_create();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern void nvt_alpr_destroy(IntPtr alpr);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int nvt_alpr_initialize(
            IntPtr alpr,
            [MarshalAs(UnmanagedType.LPStr)] string filePath,
            IntPtr config
        );

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int nvt_alpr_recognize_from_file(
            IntPtr alpr,
            [MarshalAs(UnmanagedType.LPStr)] string imagePath,
            ref int numPlates
        );

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int nvt_alpr_recognize_from_memory(
            IntPtr alpr,
            byte[] imageBinary,
            int binarySize,
            ref int numPlates
        );

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int nvt_alpr_retrieve_result(
            IntPtr alpr,
            int resultIndex,
            ref NativeALPRResult result
        );

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int nvt_alpr_get_roi(
            IntPtr alpr,
            ref ROI roi
        );

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int nvt_alpr_set_roi(
            IntPtr alpr,
            ref ROI roi
        );

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        public int Initialize(string? filePath = null, ALPRConfig? config = null)
        {
            if (_hdnl == IntPtr.Zero) {
                int retCreate = Create();

                if (retCreate != 0)
                    return retCreate;
            }

            if (filePath != null && !System.IO.File.Exists(filePath))
                return -2;

            IntPtr configPtr = IntPtr.Zero;

            try {
                if (config.HasValue) {
                    configPtr = Marshal.AllocHGlobal(Marshal.SizeOf<ALPRConfig>());
                    Marshal.StructureToPtr(config.Value, configPtr, false);
                }

                return nvt_alpr_initialize(_hdnl, filePath, configPtr);
            }
            finally {
                if (configPtr != IntPtr.Zero)
                    Marshal.FreeHGlobal(configPtr);
            }
        }

        public int Recognize(string imagePath, ref int numPlates)
        {
            if (_hdnl == IntPtr.Zero)
                return -1;
            return nvt_alpr_recognize_from_file(_hdnl, imagePath, ref numPlates);
        }

        public int Recognize(byte[] imageBinary, int binarySize, ref int numPlates)
        {
            if (_hdnl == IntPtr.Zero)
                return -1;

            return nvt_alpr_recognize_from_memory(_hdnl, imageBinary, binarySize, ref numPlates);
        }

        public int RetrieveResult(int resultIndex, ref ALPRResult result)
        {
            if (_hdnl == IntPtr.Zero)
                return -1;

            NativeALPRResult nativeResult = new NativeALPRResult();

            int ret = nvt_alpr_retrieve_result(_hdnl, resultIndex, ref nativeResult);

            //Convert UTF8 byte array to string(UTF16)
            result.text = System.Text.Encoding.UTF8.GetString(nativeResult.text).TrimEnd('\0');
            result.area = nativeResult.area;
            result.confidence = nativeResult.confidence;

            return ret;
        }

        public int GetROI(ref ROI roi)
        {
            if (_hdnl == IntPtr.Zero)
                return -1;

            return nvt_alpr_get_roi(_hdnl, ref roi);
        }

        public int SetROI(ROI roi)
        {
            if (_hdnl == IntPtr.Zero)
                return -1;
            return nvt_alpr_set_roi(_hdnl, ref roi);
        }
    }
}
