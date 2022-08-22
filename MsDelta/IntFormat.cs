using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MsDelta
{
    public class IntFormat
    {
        private StaticHuffman.Codes m_Codes;
        private StaticHuffman.DecoderTable m_DecoderTable;

        [StructLayout(LayoutKind.Explicit, Pack = 1, Size = sizeof(uint) * 252)]
        private struct WeightsBuffer { }

        private WeightsBuffer m_Buffer = default;
        Span<uint> m_Weights => Helpers.AsSpan<uint, WeightsBuffer>(ref m_Buffer);

        public IntFormat()
        {
            m_Codes = new StaticHuffman.Codes(0xFC, 0x10, false);
            m_DecoderTable = new StaticHuffman.DecoderTable(m_Codes);
            m_Weights.Fill(0);
        }

        public IntFormat(BitReader bitReader) : this()
        {
            var length1 = bitReader.ReadByte();
            if (length1 >= 0x7F) throw new InvalidDataException();
            var length2 = bitReader.ReadByte();
            if (length2 >= 0x7F) throw new InvalidDataException();
            var length3 = bitReader.ReadByte();
            if (0xFC - length2 - length1 < length3) throw new InvalidDataException();

            Span<byte> lengths = stackalloc byte[252];

            int item = 0;
            for (int i = 0; length1 > i; i++)
                {
                    item = bitReader.ReadNibble() + 1;
                    if (item > 0x10) throw new InvalidDataException();
                    lengths[i] = (byte)item;
                }

            for (int i = 0; length2 > i; i++)
                {
                    item = bitReader.ReadNibble() + 1;
                    if (item > 0x10) throw new InvalidDataException();
                    lengths[i + 0x7E] = (byte)item;
                }

            var entry = bitReader.ReadNibble() + 1;
            if (entry > 0x10) throw new InvalidDataException();
            var entry8 = (byte)entry;
            for (int i = length1; i < 0x7E; i++)
            {
                var isZero = length3 == 0;
                length3--;
                if (isZero)
                {
                    entry8--;
                    length3 = 0xFC - i - length2;
                }
                lengths[i] = entry8;
            }
            for (int i = length2; i < 0x7E; i++)
            {
                var isZero = length3 == 0;
                length3--;
                if (isZero)
                {
                    entry8--;
                    length3 = 0x7E - i;
                }
                lengths[i + 0x7E] = entry8;
            }

            m_Codes.SetLengths(0xFC, lengths);
            m_DecoderTable = new StaticHuffman.DecoderTable(m_Codes);
            m_Weights.Fill(0);
        }

        public ulong ReadNumber(BitReader bitReader)
        {
            var decoded = m_DecoderTable.ReadDecode(bitReader, m_Codes);

            var overSize = decoded >= 0x7E;

            if (overSize) decoded -= 0x7E;

            ulong decoded64 = decoded;

            if (decoded >= 4)
            {
                var bitLen = (decoded >> 1) - 1;
                var bitZero = (long)(decoded & 1) + 2;
                var nextBits = bitReader.Read64((int)bitLen);
                bitZero <<= (int)bitLen;
                decoded64 = (ulong)((long)nextBits | bitZero);
            }

            if (overSize) decoded64 = ~decoded64;
            return decoded64;
        }


    }
}
