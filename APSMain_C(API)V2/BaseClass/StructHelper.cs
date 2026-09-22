using APSMain.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace APSMain.BaseClass
{
    public static class StructHelper
    {

        /// <summary>
        /// 구조체를 바이트 배열로 변환합니다.
        /// </summary>
        public static byte[] XStructToBytes<T>(T str) where T : struct
        {
            int size = Marshal.SizeOf<T>();
            byte[] arr = new byte[size];
            IntPtr ptr = Marshal.AllocHGlobal(size);
            try {
                Marshal.StructureToPtr(str, ptr, false); 
                Marshal.Copy(ptr, arr, 0, size);
            }
            finally {
                Marshal.FreeHGlobal(ptr);
            }
            return arr;
        }

        /// <summary>
        /// 바이트 배열을 구조체로 변환합니다.
        /// </summary>
        public static T XBytesToStruct<T>(byte[] arr) where T : struct
        {
            int size = Marshal.SizeOf<T>();
            if (arr.Length < size)
                throw new ArgumentException($"입력 배열이 구조체 크기({size})보다 작습니다.");

            IntPtr ptr = Marshal.AllocHGlobal(size);
            try {
                Marshal.Copy(arr, 0, ptr, size);
                return Marshal.PtrToStructure<T>(ptr)!;
            }
            finally {
                Marshal.FreeHGlobal(ptr);
            }
        }

        /// <summary>
        /// 구조체를 바이트 배열로 변환합니다.
        /// </summary>
        public static byte[] StructToBytes<T>(in T v) where T : unmanaged
        {
            var buf = new byte[Unsafe.SizeOf<T>()];
            T tmp = v;                                 // ← 로컬 복사
            MemoryMarshal.Write(buf, in tmp);         // 경고 없음, 복사 1회
            return buf;
        }

        /// <summary>
        /// Span 바이트 배열을 구조체로 변환합니다.
        /// </summary>
        public static T BytesToStruct<T>(ReadOnlySpan<byte> src) where T : unmanaged
        {
            if (src.Length < Unsafe.SizeOf<T>())
                throw new ArgumentException("buffer too small");
            return MemoryMarshal.Read<T>(src);
        }

        /// <summary>
        /// (옵션) struct -> Span<byte>에 직접 쓰기(중간 배열 없음)
        /// </summary>
        public static int StructToSpan<T>(in T v, Span<byte> dest) where T : unmanaged
        {
            int n = Unsafe.SizeOf<T>();
            if (dest.Length < n)
                throw new ArgumentException("buffer too small");
            MemoryMarshal.Write(dest, in Unsafe.AsRef(in v));
            return n;
        }

        /// <summary>
        /// 구조체를 바이트 배열로 변환합니다.
        /// </summary>
        public static byte[] ToBytes<T>(in T v) where T : unmanaged
        {
            var buf = new byte[Unsafe.SizeOf<T>()];
            MemoryMarshal.Write(buf, in Unsafe.AsRef(in v)); // 복사 1회
            return buf;
        }

        /// <summary>
        /// byte[]/Span 배열을 구조체로 변환합니다.
        /// </summary>
        public static T FromBytes<T>(ReadOnlySpan<byte> src) where T : unmanaged
        {
            if (src.Length < Unsafe.SizeOf<T>())
                throw new ArgumentException("buffer too small");
            return MemoryMarshal.Read<T>(src);
        }

        // 어떤 struct든: 필드명 + 길이 → 해당 fixed byte 필드의 Span<byte> (unsafe 불필요)
        public static Span<byte> FieldSpan<T>(ref T dst, string fieldName, int len) where T : struct
        {
            nint off = (nint)Marshal.OffsetOf<T>(fieldName);
            ref byte baseRef = ref Unsafe.As<T, byte>(ref dst);
            ref byte fieldRef = ref Unsafe.AddByteOffset(ref baseRef, off);
            return MemoryMarshal.CreateSpan(ref fieldRef, len);
        }

        public static string ReadString<T>(ref T src, string fieldName, int len, Encoding? enc = null) where T : struct
        {
            var span = StructHelper.FieldSpan(ref src, fieldName, len);
            int n = span.IndexOf((byte)0);
            if (n < 0)
                n = len;
            return (enc ?? Encoding.ASCII).GetString(span[..n]);
        }

        /// <summary>
        /// WriteString(ref udata, nameof(STUDPDATA.srcip), 20, "10.0.0.7");
        /// ASCII/KS5601 등 문자열 쓰기(남는 바이트 0, NUL 종료 옵션)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dst"></param>
        /// <param name="fieldName"></param>
        /// <param name="len"></param>
        /// <param name="s"></param>
        /// <param name="enc"></param>
        /// <param name="nulTerm"></param>
        public static void WriteString<T>(ref T dst, string fieldName, int len, ReadOnlySpan<char> s,
                           Encoding? enc = null, bool nulTerm = true) where T : struct
        {
            var span = FieldSpan(ref dst, fieldName, len);
            span.Clear();
            int n = (enc ?? Encoding.ASCII).GetBytes(s, span);
            if (nulTerm && n < len)
                span[n] = 0;
        }

        /// <summary>
        /// WriteUnmanaged(ref udata, nameof(STUDPDATA.xdata), 1024, in accept);
        /// 다른 구조체 payload 쓰기(블리터블만/복사 1회)
        /// </summary>
        /// <typeparam name="TDst"></typeparam>
        /// <typeparam name="TPayload"></typeparam>
        /// <param name="dst"></param>
        /// <param name="fieldName"></param>
        /// <param name="len"></param>
        /// <param name="payload"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static void WriteUnmanaged<TDst, TPayload>(ref TDst dst, string fieldName, int len, in TPayload payload)
                           where TDst : struct where TPayload : unmanaged
        {
            if (Unsafe.SizeOf<TPayload>() > len)
                throw new ArgumentOutOfRangeException(nameof(payload));
            var span = FieldSpan(ref dst, fieldName, len);
            Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(span), payload);
        }

        /// <summary>
        /// WriteBytes(ref UPDData, nameof(STUDPDATA.xdata), 1024, MSG.AsSpan(0, Math.Min(nLength, 1024)));
        /// </summary>
        /// <typeparam name="TDst"></typeparam>
        /// <param name="dst"></param>
        /// <param name="fieldName"></param>
        /// <param name="fieldLen"></param>
        /// <param name="src"></param>
        public static void WriteBytes<TDst>(ref TDst dst, string fieldName, int fieldLen, ReadOnlySpan<byte> src) where TDst : struct
        {
            nint off = (nint)Marshal.OffsetOf<TDst>(fieldName);
            ref byte baseRef = ref Unsafe.As<TDst, byte>(ref dst);
            ref byte fieldRef = ref Unsafe.AddByteOffset(ref baseRef, off);
            var dest = MemoryMarshal.CreateSpan(ref fieldRef, fieldLen);
            dest.Clear();
            src[..Math.Min(src.Length, fieldLen)].CopyTo(dest);
        }

        /// <summary>
        /// 구조체의 크기를 구합니다.
        /// </summary>
        public static int GetSize<T>() where T : struct
        {
            return Marshal.SizeOf<T>();
        }

        public static unsafe void CopyTparkinfoToStruct(Tparkinfo src, ref STTPARKINFO dst)
        {
            dst.xindex = src.Xindex;
            dst.sitenum = (byte)(src.Sitenum ?? 0);
            dst.groupnum = (byte)(src.Groupnum ?? 0);

            StructHelper.WriteString<STTPARKINFO>(ref dst, nameof(STTPARKINFO.ticketdata), 30, src.Ticketdata);

            dst.ticketnum = src.Ticketnum ?? 0;
            dst.ticketcartype = (byte)(src.Ticketcartype ?? 0);
            dst.parkcartype = (byte)(src.Parkcartype ?? 0);

            StructHelper.WriteString<STTPARKINFO>(ref dst, nameof(STTPARKINFO.carnum), 20, src.Carnum, Encoding.GetEncoding(51949));

            dst.indevicenum = (ushort)(src.Indevicenum ?? 0);

            StructHelper.WriteString<STTPARKINFO>(ref dst, nameof(STTPARKINFO.indate), 14, src.Indate.ToString("yyyy-MM-dd"));
            dst.inhour = (byte)src.Inhour;
            dst.inmin = (byte)src.Inmin;

            dst.outdevicenum = (ushort)(src.Outdevicenum ?? 0);
            StructHelper.WriteString<STTPARKINFO>(ref dst, nameof(STTPARKINFO.outdate), 14, src.Outdate.ToString("yyyy-MM-dd"));
            dst.outhour = (byte)(src.Outhour ?? 0);
            dst.outmin = (byte)(src.Outmin ?? 0);

            StructHelper.WriteString<STTPARKINFO>(ref dst, nameof(STTPARKINFO.inimage), 80, src.Inimage, Encoding.GetEncoding(51949));
            StructHelper.WriteString<STTPARKINFO>(ref dst, nameof(STTPARKINFO.outimage), 80, src.Outimage, Encoding.GetEncoding(51949));

            dst.parktime = src.Parktime;
            dst.parkcaltype = (byte)(src.Parkcaltype);
            dst.parkmoney = src.Parkmoney;
            dst.salemoney = src.Salemoney;
            dst.saletime = src.Saletime;

            dst.credittype = src.Credittype;
            StructHelper.WriteString<STTPARKINFO>(ref dst, nameof(STTPARKINFO.creditcardno), 40, src.Creditcardno);
            StructHelper.WriteString<STTPARKINFO>(ref dst, nameof(STTPARKINFO.acceptno), 20, src.Acceptno);

            if (src.Accepttime.HasValue)
                StructHelper.WriteString<STTPARKINFO>(ref dst, nameof(STTPARKINFO.accepttime), 20, src.Accepttime.Value.ToString("yyyyMMddHHmmss"));

            dst.creditmoney = src.Creditmoney;
            dst.receiptnum = src.Receiptnum;

            StructHelper.WriteString<STTPARKINFO>(ref dst, nameof(STTPARKINFO.ocssalecode), 20, src.Ocssalecode);
            dst.ocssaletype = src.Ocssaletype;
            dst.ocssaleval = src.Ocssaleval;

            dst.managercode = (byte)(src.Managercode ?? 0);
            StructHelper.WriteString<STTPARKINFO>(ref dst, nameof(STTPARKINFO.managername), 30, src.Managername, Encoding.GetEncoding(51949));
            StructHelper.WriteString<STTPARKINFO>(ref dst, nameof(STTPARKINFO.saleimage), 80, src.Saleimage, Encoding.GetEncoding(51949));

            dst.intick = src.Intick;
            dst.outtick = src.Outtick;

            dst.manflag = (byte)(src.Manflag);
            dst.dendflag = (byte)(src.Dendflag);
            dst.tendflag = (byte)(src.Tendflag);

            if (src.Denddate.HasValue)
                StructHelper.WriteString<STTPARKINFO>(ref dst, nameof(STTPARKINFO.denddate), 14, src.Denddate.Value.ToString("yyyyMMddHHmmss"));

            if (src.Tenddate.HasValue)
                StructHelper.WriteString<STTPARKINFO>(ref dst, nameof(STTPARKINFO.tenddate), 14, src.Tenddate.Value.ToString("yyyyMMddHHmmss"));

            dst.outflag = (byte)src.Outflag;
        }


        public static unsafe void CopyTperiodinoutToStruct(Tperiodinout src, ref STTPERIODINOUT dst)
        {
            dst.xindex = src.Xindex;
            dst.sitenum = (byte)(src.Sitenum ?? 0);
            dst.groupnum = (byte)(src.Groupnum ?? 0);

            StructHelper.WriteString<STTPERIODINOUT>(ref dst, nameof(STTPERIODINOUT.carnum), 20, src.Carnum, Encoding.GetEncoding(51949));

            dst.indevicenum = (ushort)(src.Indevicenum ?? 0);

            StructHelper.WriteString<STTPERIODINOUT>(ref dst, nameof(STTPERIODINOUT.indate), 14, src.Indate.ToString("yyyy-MM-dd"));
            dst.inhour = (byte)(src.Inhour ?? 0);
            dst.inmin = (byte)(src.Inmin ?? 0);

            dst.outdevicenum = (ushort)(src.Outdevicenum ?? 0);
            StructHelper.WriteString<STTPERIODINOUT>(ref dst, nameof(STTPERIODINOUT.outdate), 14, src.Outdate.ToString("yyyy-MM-dd"));
            dst.outhour = (byte)(src.Outhour ?? 0);
            dst.outmin = (byte)(src.Outmin ?? 0);

            StructHelper.WriteString<STTPERIODINOUT>(ref dst, nameof(STTPERIODINOUT.inimage), 80, src.Inimage, Encoding.GetEncoding(51949));
            StructHelper.WriteString<STTPERIODINOUT>(ref dst, nameof(STTPERIODINOUT.outimage), 80, src.Outimage, Encoding.GetEncoding(51949));

            dst.parktime = src.Parktime ?? 0;

            dst.outflag = (byte)(src.Outflag ?? 79);
        }
    }
}



/*
        // IP 채우기
        Fx.WriteString(ref udata, nameof(STUDPDATA.srcip), 20, "10.0.0.7");
        Fx.WriteString(ref udata, nameof(STUDPDATA.desip), 20, "129.168.0.1");

        // KS5601 이름
        var ks5601 = Encoding.GetEncoding(51949);
        Fx.WriteString(ref accept, nameof(STTACCEPT.managername), 30, "홍길동", ks5601);

        // payload → xdata
        Fx.WriteUnmanaged(ref udata, nameof(STUDPDATA.xdata), 1024, in accept);
*/