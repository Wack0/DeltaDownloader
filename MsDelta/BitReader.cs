using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MsDelta
{
    public class BitReader
    {
        private ReadOnlySpan<byte> BufferImpl => new ReadOnlySpan<byte>(m_Array, m_Offset, m_Array.Length - m_Offset);
        private ReadOnlySpan<byte> BufferCurrent => new ReadOnlySpan<byte>(m_Array, m_Offset + m_Current, m_End - m_Current);

        private ReadOnlySpan<byte> BufferEnd => new ReadOnlySpan<byte>(m_Array, m_Offset + m_End, m_Array.Length - m_Offset - m_End);

        public int CurrentOffset => m_Offset + m_LastSeekOffset;
        public bool AtEnd => m_End == m_Current;

        private byte UseByte()
        {
            var ret = BufferCurrent[0];
            m_Current++;
            m_LastSeekOffset++;
            return ret;
        }

        private uint UseDword()
        {
            uint ret = UseByte();
            ret |= (uint)UseByte() << 8;
            ret |= (uint)UseByte() << 16;
            ret |= (uint)UseByte() << 24;
            return ret;
        }

        private readonly byte[] m_Array;
        int m_Offset;
        int m_LastSeekOffset;
        int m_Current;
        int m_End;
        uint m_ExtraBits;
        internal ulong m_ShiftRegister { get; private set; }
        uint m_ValidLength;
        uint m_BitPadding;

        public BitReader(byte[] buffer) : this(new ArraySegment<byte>(buffer)) { }

        public BitReader(byte[] buffer, int offset) : this(new ArraySegment<byte>(buffer, offset, buffer.Length - offset)) { }
        public BitReader(ArraySegment<byte> buffer)
        {
            m_ExtraBits = 0;
            m_ShiftRegister = 0;
            m_ValidLength = 0;
            m_Current = 0;
            m_End = 0;
            m_Array = buffer.Array;
            if (m_Array.Length == 0) throw new IndexOutOfRangeException();
            m_Offset = buffer.Offset;
            m_LastSeekOffset = 0;
            m_BitPadding = (uint) (BufferImpl[0] & 0b111);
            if (BufferImpl.Length == 1 && m_BitPadding > 5) throw new InvalidDataException();
            Seek(0);
            Read(3);
        }

        public void Seek(int offset)
        {
            int remaining = BufferImpl.Length - offset;
            int unknown = (-offset) & 0b11;
            int realRemaining = Math.Min(unknown, remaining);

            m_ShiftRegister = 0;
            m_ValidLength = 0;
            m_LastSeekOffset = offset;
            switch (realRemaining)
            {
                case 3:
                    m_ShiftRegister |= (uint)BufferImpl[offset + 2] << 16;
                    goto case 2;
                case 2:
                    m_ShiftRegister |= (uint)BufferImpl[offset + 1] << 8;
                    goto case 1;
                case 1:
                    m_ShiftRegister |= BufferImpl[offset + 0];
                    break;
            }


            if (realRemaining == remaining)
            {
                m_Current = 0;
                m_End = 0;
                m_ValidLength = (8 * (uint)realRemaining) - m_BitPadding;
                m_ExtraBits = 0;
                return;
            }

            int diff = remaining - unknown;
            m_Current = offset + unknown;
            m_ValidLength = 8 * (uint)unknown;
            int diff2 = (diff - 1) & ~0b11;
            m_ExtraBits = 8 * (uint)(diff - diff2) - m_BitPadding;
            m_End = 4 * (diff2 >> 2) + unknown + offset;
            Consume(0);
        }

        private void CheckLength(int length)
        {
            if ((uint)length > m_ValidLength) throw new IndexOutOfRangeException();
        }

        public void Consume(int length)
        {
            CheckLength(length);
            InternalConsume(length);
        }

        private void InternalConsume(int length)
        {
            m_ShiftRegister >>= length;
            m_ValidLength -= (uint)length;
            if (m_ValidLength >= 32) return;

            if (m_End != m_Current)
            {
                var used = UseDword();
                m_ShiftRegister |= (ulong)used << (byte)m_ValidLength;
                m_ValidLength += 32;
                return;
            }
            var extra = (m_ExtraBits + 7) >> 3;

            ulong nextShift = 0;
            switch (extra)
            {
                case 4:
                    nextShift |= (ulong)BufferEnd[3] << 24;
                    goto case 3;
                case 3:
                    nextShift |= (ulong)BufferEnd[2] << 16;
                    goto case 2;
                case 2:
                    nextShift |= (ulong)BufferEnd[1] << 8;
                    goto case 1;
                case 1:
                    nextShift |= BufferEnd[0];
                    break;
            }

            m_ShiftRegister |= nextShift << (int)m_ValidLength;
            m_ValidLength += m_ExtraBits;
            m_ExtraBits = 0;
        }

        public uint Read(int length)
        {
            CheckLength(length);

            uint ret = 0;
            if (length != 0)
            {
                ulong val = ~1ul;
                val <<= (length - 1);
                ret = (uint)(m_ShiftRegister & ~val);
            }
            InternalConsume(length);
            return ret;
        }

        public int ReadNibble() => (int)Read(4);
        public int ReadByte() => (int)Read(8);

        public int ReadNumber(uint baseBitsNumber)
        {
            if (m_ShiftRegister == 0) throw new InvalidOperationException();
            var LowestSetBit = BitScanner.BitScanForward((int)baseBitsNumber);
            if ((uint)(31 - LowestSetBit) < 8) throw new IndexOutOfRangeException();
            var val = LowestSetBit + 8;
            Consume(LowestSetBit + 1);
            return (int)Read(val) | (1 << val);
        }

        public ulong ReadInt()
        {
            var NibbleCount = BitScanner.BitScanForward((int)m_ShiftRegister | 0x10000);
            if (NibbleCount == 16) throw new InvalidOperationException();
            NibbleCount++;
            Consume(NibbleCount);
            if (NibbleCount < 8) return (ulong)Read(4 * NibbleCount);
            ulong ret = (ulong)Read(32);
            ret |= ((ulong)Read(4 * (NibbleCount - 8)) << 32);
            return ret;
        }

        public ulong Read64(int length)
        {
            if (length <= 32) return (ulong)Read(length);
            ulong ret = (ulong)Read(32);
            ret |= ((ulong)Read(length - 32) << 32);
            return ret;
        }

        private int GetCurrentOffsetIntoBuffer()
        {
            if (m_Current != m_End || m_ExtraBits != 0)
            {
                return
                    BufferImpl.Length - ((m_End - m_Current) & ~3)
                    - (int)((m_ExtraBits + m_BitPadding) >> 3)
                    - (int)(m_ValidLength >> 3);
            }
            else
            {
                return BufferImpl.Length - (int)(m_ValidLength >> 3);
            }
        }

        public byte[] ReadBuffer()
        {
            var length64 = ReadInt();
            if (length64 > int.MaxValue) throw new InvalidDataException();
            var length = (int)length64;
            m_ValidLength &= ~7u;
            var BufferOffset = GetCurrentOffsetIntoBuffer();

            byte[] ret = new byte[length];
            Buffer.BlockCopy(m_Array, m_Offset + BufferOffset, ret, 0, length);
            Seek(BufferOffset + length);
            return ret;
        }

        /// <summary>
        /// Reads exactly 32 bits.
        /// </summary>
        /// <returns></returns>
        public uint ReadU32() => (uint)Read(32);
        /// <summary>
        /// Reads exactly 1 bit.
        /// </summary>
        /// <returns></returns>
        public bool ReadBool() => Read(1) != 0;
    }
}
