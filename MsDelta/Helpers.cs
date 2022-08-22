using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace MsDelta
{
    internal static class Helpers
    {
        internal static void ArraySet<T>(this T[] arr, T value, uint offset, uint length)
            where T : struct, IEquatable<T>
        {
            if (offset >= arr.LongLength) throw new IndexOutOfRangeException();
            length = (uint) Math.Min(length, arr.LongLength - offset);
            if (value.Equals(default(T)) && length > 76)
                Array.Clear(arr, (int)offset, (int)length);
            else
                for (uint i = 0; i < length; i++) arr[offset + i] = value;

        }

        internal static void ArraySet<T>(this Span<T> arr, T value, uint offset, uint length)
        {
            arr.Slice((int)offset, (int)length).Fill(value);
        }

        internal static Span<T> AsSpan<T, TBuffer>(ref TBuffer ptr)
            where T : unmanaged
            where TBuffer : unmanaged
        {
            unsafe
            {
                return new Span<T>(Unsafe.AsPointer(ref ptr), sizeof(TBuffer) / sizeof(T));
            }
        }
    }
}
