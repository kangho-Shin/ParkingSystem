using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace JPXLpr.NGis
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct RECT
    {
        public int left;
        public int top;
        public int right;
        public int bottom;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct CD_IMG
    {
        public nint img;     // 그림 (0x00이면 Black 이다)
        public int bpl;     // BytePerLine
        public int bpp;     // BitsPerPixel
        public int xsize;   // XPixel Size
        public int ysize;   // YPixel Size
    }

    public struct LPRPARAM
    {
        public int nMinWidth;
        public int nMinHeight;
        public int nMaxWidth;
        public int nMaxHeight;
        public int nRecRatio;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public struct MESH_RESULT
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] CharStr;  // 인식결과 문자
        public int Ratio;       // 문자의 신뢰도(범위: 0 ~ 100 %)					
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct LPRBOX
    {
        public int nLeft;      // 상단 x 좌표
        public int nTop;       // 상단 y 좌표
        public int nWidth;     // 이미지 넓이
        public int nHeight;    // 이미지 높이
    }
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public struct LPRRES
    {
        public int nLpType;                // 번호판유형 (5 가지 번호판유형)
                                           // 1   : (서울12 가1234)
                                           // 2   : (서울3 가1234)
                                           // 3   : (12가 1234)
                                           // 4   : (가로 1줄짜리 번호판, 현재 경찰차)
                                           // 5   : (주황색 특장차 번호판)
                                           // , ArraySubType = UnmanagedType.Struct
        public int nTpRatio;               // 전체 번호판 인식의 신뢰도(범위: 0 ~ 100 %)  							
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 9)]
        public MESH_RESULT[] mrResult;    // 각 문자(번호판의 문자)의 인식결과 (9가지 문자값)
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
        public LPRBOX[] bxPos;            // 각 문자의 위치, 마지막 1개는 번호판 위치
    }

    public class NGisCar
    {
        [DllImport("NgisCar.dll")]
        public extern static int NgisCarOpen();
        [DllImport("NgisCar.dll")]
        public extern static int NgisCarClose();
        [DllImport("NgisCar.dll", CallingConvention = CallingConvention.StdCall)]
        public extern static int NgisCarOcr(nint pCarImage, int file_option, ref RECT pr, ref LPRPARAM pParam, ref LPRRES pRes, int x_degree, int y_degree, int x_ratio, int y_ratio);

        private bool _isOpen;

        public long Open()
        {
            int ret = NgisCarOpen();
            _isOpen = ret == 0;
            return ret;
        }

        public long Close()
        {
            _isOpen = false;
            return NgisCarClose();
        }

        public int FileRecognition(string inimage, ref string carNum)
        {
            if (!_isOpen)
                return -100;

            int j, k, nCharCnt, result;
            RECT rect = new RECT();
            nint sptr = Marshal.StringToHGlobalAnsi(inimage);
            LPRPARAM jParam = new LPRPARAM();
            LPRRES RegCar = new LPRRES();
            byte[] szResult = new byte[100];
            byte[] stCarNum = new byte[100];

            RegCar.mrResult = new MESH_RESULT[9];
            for (int i = 0; i < 9; i++) {
                RegCar.mrResult[i].CharStr = new byte[8];
            }
            RegCar.bxPos = new LPRBOX[10];

            nCharCnt = result = -1;
            result = NgisCarOcr(sptr, 0, ref rect, ref jParam, ref RegCar, 0, 0, 0, 0);
            Array.Clear(szResult, 0, 100);
            for (j = 0, nCharCnt = -1; j < 9; j++) { //0~9 까지 돌자.
                nCharCnt++;
                szResult[nCharCnt] = RegCar.mrResult[j].CharStr[0]; //한글자 넣고...
                if ((RegCar.mrResult[j].CharStr[0] & 0x80) != 0x00) { //한글이면 한바이트 더 넣는다.
                    nCharCnt++;
                    szResult[nCharCnt] = RegCar.mrResult[j].CharStr[1];
                }
            }
            Array.Clear(stCarNum, 0, 100);
            for (j = 0, k = 0; j <= nCharCnt; j++) {
                if (szResult[j] != 0x20) stCarNum[k++] = szResult[j];
            }

            carNum = Encoding.Default.GetString(stCarNum).TrimEnd('\0');

            return result;
        }

        public int ImageRecognition(Bitmap bitmap, ref string carNum)
        {
            if (!_isOpen)
                return -100;

            if (bitmap == null)
                return -1;

            Bitmap? workBitmap = null;
            BitmapData? data = null;

            try {
                workBitmap = new Bitmap(bitmap.Width, bitmap.Height, PixelFormat.Format24bppRgb);

                using (Graphics g = Graphics.FromImage(workBitmap)) {
                    g.DrawImage(bitmap, 0, 0, bitmap.Width, bitmap.Height);
                }

                Rectangle rectImg = new Rectangle(0, 0, workBitmap.Width, workBitmap.Height);

                data = workBitmap.LockBits(
                    rectImg,
                    ImageLockMode.ReadOnly,
                    PixelFormat.Format24bppRgb);

                CD_IMG cdImg = new CD_IMG();
                cdImg.img = data.Scan0;
                cdImg.bpl = data.Stride;
                cdImg.bpp = 24;
                cdImg.xsize = workBitmap.Width;
                cdImg.ysize = workBitmap.Height;

                return ImageRecognition(cdImg, ref carNum);
            }
            finally {
                if (data != null && workBitmap != null)
                    workBitmap.UnlockBits(data);

                if (workBitmap != null)
                    workBitmap.Dispose();
            }
        }
        public int ImageRecognition(CD_IMG inimg, ref string carNum)
        {
            int j, k, nCharCnt, result;
            LPRPARAM jParam = new LPRPARAM();
            LPRRES RegCar = new LPRRES();
            byte[] szResult = new byte[100];
            byte[] stCarNum = new byte[100];

            nCharCnt = result = -1;
            nint xpointer = Marshal.AllocHGlobal(Marshal.SizeOf(inimg));
            Marshal.StructureToPtr(inimg, xpointer, false);

            RECT rect;
            rect.left = 10;
            rect.top = 10;
            rect.right = inimg.xsize - 10;
            rect.bottom = inimg.ysize - 10;
            result = NgisCarOcr(xpointer, -2, ref rect, ref jParam, ref RegCar, 0, 0, 0, 0);
            if (result != 0) {
                result = NgisCarOcr(xpointer, -2, ref rect, ref jParam, ref RegCar, 500, -1500, 110, 100);
            }
            Array.Clear(szResult, 0, 100);
            for (j = 0, nCharCnt = -1; j < 9; j++) {   //0~9 까지 돌자.
                nCharCnt++;
                szResult[nCharCnt] = RegCar.mrResult[j].CharStr[0]; //한글자 넣고...
                if ((RegCar.mrResult[j].CharStr[0] & 0x80) != 0x00) {  //한글이면 한바이트 더 넣는다.
                    nCharCnt++;
                    szResult[nCharCnt] = RegCar.mrResult[j].CharStr[1];
                }
            }
            Array.Clear(stCarNum, 0, 100);
            for (j = 0, k = 0; j < nCharCnt; j++) {
                if (szResult[j] != 0x20) stCarNum[k++] = szResult[j];
            }
            carNum = Encoding.Default.GetString(stCarNum).TrimEnd('\0');

            Marshal.FreeHGlobal(xpointer);
            return result;
        }
    }
}
