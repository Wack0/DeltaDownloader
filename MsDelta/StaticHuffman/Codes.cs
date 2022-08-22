using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MsDelta.StaticHuffman
{
    internal class Codes
    {
        internal readonly uint m_IndexNumber;
        internal readonly byte[] m_Lengths;
        internal readonly uint[] m_Codes;
        internal readonly byte m_MaxLength;

        private static void CheckParams(uint IndexNumber, byte MaxLength)
        {
            if (IndexNumber == 0 || IndexNumber > 0x10000 || MaxLength == 0 || MaxLength > 0x1F) throw new IndexOutOfRangeException();
        }

        internal static void ResetLengths(uint IndexNumber, byte MaxLength, Span<byte> Lengths, bool SkipUnused)
        {
            CheckParams(IndexNumber, MaxLength);

            if (SkipUnused)
            {
                if (IndexNumber == 0) return;
                Lengths.ArraySet<byte>(0, 0, IndexNumber);
                return;
            }

            if (IndexNumber >= 3)
            {
                byte Value = (byte)(BitScanner.BitScanReverse((int)IndexNumber - 1) + 1);
                byte Value2 = Value;
                Value2--;
                if (Value >= MaxLength) throw new IndexOutOfRangeException();
                uint Offset = 0;
                if ((1 << Value) != IndexNumber)
                {
                    Offset = (uint)(1 << Value) - IndexNumber;
                    Lengths.ArraySet(Value2, 0, Offset);
                }
                if (IndexNumber > Offset)
                    Lengths.ArraySet(Value, Offset, IndexNumber - Offset);
                return;
            }

            if (MaxLength == 0) throw new IndexOutOfRangeException();

            if (IndexNumber != 0) Lengths.ArraySet<byte>(1, 0, IndexNumber);
        }

        internal Codes(uint IndexNumber, byte MaxLength, bool SkipUnused)
        {
            CheckParams(IndexNumber, MaxLength);
            m_IndexNumber = IndexNumber;
            m_MaxLength = MaxLength;
            m_Lengths = new byte[IndexNumber];
            m_Codes = new uint[IndexNumber];
            ResetLengths(IndexNumber, MaxLength, m_Lengths, SkipUnused);
            CalculateCodes();
        }

        private uint[] InitBlock()
        {
            var totalLength = m_MaxLength + 1;
            var lengthsCounts = new uint[totalLength];
            for (uint i = 0; i < m_IndexNumber; i++)
            {
                Debug.Assert(m_Lengths[i] <= m_MaxLength);
                lengthsCounts[m_Lengths[i]]++;
            }

            // Check lengthsCounts
            for (uint bitVal = 2, i = 1; i <= m_MaxLength; bitVal = 2 * (bitVal - lengthsCounts[i]), i++)
            {
                if (lengthsCounts[i] > bitVal) throw new InvalidOperationException();
            }

            var Block = new uint[totalLength];
            uint count = 0;
            for (uint i = 0; i <= m_MaxLength; i++)
            {
                Block[totalLength - i - 1] = count;
                count = (count + lengthsCounts[totalLength - i - 1]) >> 1;
            }

            return Block;
        }

        private void CalculateCodes()
        {
            var Block = InitBlock();

            for (uint i = 0; i < m_IndexNumber; i++)
            {
                var length = m_Lengths[i];
                if (length == 0)
                {
                    m_Codes[i] = 0;
                    continue;
                }

                var oldBlock = Block[length];
                Block[length]++;
                uint newCode = 0;
                for (uint l = 0; l < length; l++)
                {
                    byte isOdd = (byte)(oldBlock & 1);
                    oldBlock >>= 1;
                    newCode <<= 1;
                    newCode |= isOdd;
                }
                m_Codes[i] = newCode;
            }
        }

        internal void SetLengths(uint indexNumber, ReadOnlySpan<byte> lengths)
        {
            if (m_IndexNumber == 0 || indexNumber != m_IndexNumber) throw new IndexOutOfRangeException();
            for (uint i = 0; i < m_IndexNumber; i++) m_Lengths[i] = lengths[(int)i];
            CalculateCodes();
        }
    }
}
