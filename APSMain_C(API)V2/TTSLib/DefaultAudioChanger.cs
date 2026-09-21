using System;
using System.Runtime.InteropServices;

namespace APSMain.TTSLib
{
    [StructLayout(LayoutKind.Sequential)]
    public struct PROPERTYKEY
    {
        public Guid fmtid;
        public int pid;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PROPVARIANT
    {
        public ushort vt;
        public ushort wReserved1;
        public ushort wReserved2;
        public ushort wReserved3;
        public IntPtr p;
    }

    public static class DefaultAudioChanger
    {
        private const int CLSCTX_ALL = 23;

        [ComImport]
        [Guid("870af99c-171d-4f9e-af0d-e63df40c2bc9")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IPolicyConfig
        {
            int GetMixFormat(string pszDeviceName, out IntPtr ppFormat);
            int GetDeviceFormat(string pszDeviceName, int bDefault, out IntPtr ppFormat);
            int SetDeviceFormat(string pszDeviceName, IntPtr pEndpointFormat, IntPtr mixFormat);
            int GetProcessingPeriod(string pszDeviceName, int bDefault, out long hnsDefault, out long hnsMinimum);
            int SetProcessingPeriod(string pszDeviceName, long hns);
            int GetShareMode(string pszDeviceName, out IntPtr pMode);
            int SetShareMode(string pszDeviceName, IntPtr mode);
            int GetPropertyValue(string pszDeviceName, ref PROPERTYKEY key, out PROPVARIANT pv);
            int SetPropertyValue(string pszDeviceName, ref PROPERTYKEY key, ref PROPVARIANT pv);
            int SetDefaultEndpoint(string pszDeviceName, ERole role);   // ← 여기만 쓰면 됨
            int SetEndpointVisibility(string pszDeviceName, int bVisible);
        }

        [DllImport("ole32.dll")]
        private static extern int CoCreateInstance(
            [In] ref Guid clsid,
            [MarshalAs(UnmanagedType.IUnknown)] object inner,
            int context,
            [In] ref Guid uuid,
            [MarshalAs(UnmanagedType.IUnknown)] out object rReturnedComObject);

        private static readonly Guid CLSID_PolicyConfig = new("870af99c-171d-4f9e-af0d-e63df40c2bc9");
        private static readonly Guid IID_IPolicyConfig = new("f8679f50-850a-41cf-9c72-430f290290c8");

        public enum ERole
        {
            eConsole = 0,
            eMultimedia = 1,
            eCommunications = 2
        }

        public static bool SetDefaultDevice(string deviceId)
        {
            try {
                Guid clsid = CLSID_PolicyConfig;   // ← 복사본
                Guid iid = IID_IPolicyConfig;     // ← 복사본

                CoCreateInstance(ref clsid, null!, CLSCTX_ALL, ref iid, out object obj);
                var policy = (IPolicyConfig)obj;

                policy.SetDefaultEndpoint(deviceId, ERole.eConsole);
                policy.SetDefaultEndpoint(deviceId, ERole.eMultimedia);
                policy.SetDefaultEndpoint(deviceId, ERole.eCommunications);
                return true;
            }
            catch {
                return false;
            }
        }
    }
}
