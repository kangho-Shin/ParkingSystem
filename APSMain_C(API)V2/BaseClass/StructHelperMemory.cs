using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace APSMain.BaseClass
{
    public static class StructHelperMemory
    {
        /// <summary>
        /// 구조체를 Memory<byte>로 직렬화합니다.
        /// </summary>
        public static Memory<byte> StructToMemory<T>(T str) where T : struct
        {
            int size = Marshal.SizeOf<T>();
            byte[] buffer = new byte[size];

            var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            try
            {
                IntPtr ptr = handle.AddrOfPinnedObject();
                Marshal.StructureToPtr(str, ptr, false);
            }
            finally
            {
                handle.Free();
            }

            return new Memory<byte>(buffer);
        }

        public static unsafe void PutPayload<T>(byte* dest, int len, in T src) where T : unmanaged
        {
            int n = Unsafe.SizeOf<T>();
            if (n > len)
                throw new ArgumentOutOfRangeException();
            MemoryMarshal.Write(new Span<byte>(dest, len), in Unsafe.AsRef(in src));
        }

        /// <summary>
        /// PutXdata(ref udata, in accept);
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dst"></param>
        /// <param name="src"></param>
        /// <param name="clearRest"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static unsafe void PutXdata<T>(ref STUDPDATA dst, in T src, bool clearRest = true) where T : unmanaged
        {
            int n = Unsafe.SizeOf<T>();
            if (n > 1024)
                throw new ArgumentOutOfRangeException(nameof(T));

            fixed (byte* p = dst.xdata) {
                var span = new Span<byte>(p, 1024);
                if (clearRest)
                    span.Clear();
                Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(span), src);
            }
        }

        /*
            var mem = StructHelperMemory.StructToMemory(PacketData);
            mem.Span.CopyTo(sBuf); // Span<byte> → 기존 버퍼로 복사

            var restored = StructHelperMemory.MemoryToStruct<PACKET_DATA>(mem);
         */

        /// <summary>
        /// Memory<byte>를 구조체로 역직렬화합니다.
        /// </summary>
        public static T MemoryToStruct<T>(ReadOnlyMemory<byte> memory) where T : struct
        {
            int size = Marshal.SizeOf<T>();
            if (memory.Length < size)
                throw new ArgumentException($"입력 메모리가 구조체 크기({size})보다 작습니다.");

            var handle = GCHandle.Alloc(memory.ToArray(), GCHandleType.Pinned); // 또는 memory.Span.ToArray()
            try
            {
                IntPtr ptr = handle.AddrOfPinnedObject();
                return Marshal.PtrToStructure<T>(ptr)!;
            }
            finally
            {
                handle.Free();
            }
        }

        ///*
        //    int size = Marshal.SizeOf<PACKET_DATA>();
        //    Memory<byte> buffer = new byte[size];

        //    StructHelperMemoryEx.WriteStructToMemory(PacketData, buffer);
        // */
        //public static void WriteStructToMemory<T>(T structure, Memory<byte> target) where T : struct
        //{
        //    int size = Marshal.SizeOf<T>();
        //    if (target.Length < size)
        //        throw new ArgumentException($"버퍼 크기({target.Length})가 구조체 크기({size})보다 작습니다.");

        //    using var handle = target.Pin();
        //    Marshal.StructureToPtr(structure, (nint)handle.Pointer, false);
        //}

        ///*
        //    int size = Marshal.SizeOf<PACKET_DATA>();
        //    Memory<byte> buffer = new byte[size];

        //    StructHelperMemoryEx.WriteStructToMemory(PacketData, buffer);
        // */
        ///// <summary>
        ///// Memory<byte> 또는 Span<byte>에서 구조체를 읽어옵니다.
        ///// </summary>
        //public static T ReadStructFromMemory<T>(ReadOnlyMemory<byte> source) where T : struct
        //{
        //    int size = Marshal.SizeOf<T>();
        //    if (source.Length < size)
        //        throw new ArgumentException($"버퍼 크기({source.Length})가 구조체 크기({size})보다 작습니다.");

        //    using var handle = source.Pin();
        //    return Marshal.PtrToStructure<T>((nint)handle.Pointer)!;
        //}

        /// <summary>
        /// 구조체의 크기를 반환합니다.
        /// </summary>
        public static int GetSize<T>() where T : struct
        {
            return Marshal.SizeOf<T>();
        }
    }
}
