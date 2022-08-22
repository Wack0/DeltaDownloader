using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace MsDelta.StaticHuffman
{
    internal class DecoderTable
    {
        struct PrefixEntry
        {
            internal const int SIZE = sizeof(uint) * 2;
            internal uint indexMask;
            internal uint decodeBase;
        }

        [StructLayout(LayoutKind.Explicit, Pack = 1, Size = PrefixEntry.SIZE * 32)]
        private struct PrefixEntryBuffer { }

        private PrefixEntryBuffer m_Buffer = default;
        Span<PrefixEntry> m_PrefixTable => Helpers.AsSpan<PrefixEntry, PrefixEntryBuffer>(ref m_Buffer);

        ushort[] m_DecodeEntry;

        internal DecoderTable(Codes codes)
        {
            if (codes == null) throw new ArgumentNullException(nameof(codes));

            Span<uint> indexWidth = stackalloc uint[32];
            indexWidth.Fill(0);

            uint PrefixTableLength = 0;
            var expectedIndex = 0u;
            if (codes.m_IndexNumber != 0)
            {
                expectedIndex = codes.m_IndexNumber;
                for (uint i = 0; i < codes.m_IndexNumber; i++)
                {
                    if (codes.m_Lengths[i] == 0) continue;
                    if (codes.m_Codes[i] == 0)
                    {
                        if (codes.m_IndexNumber != expectedIndex) throw new IndexOutOfRangeException();
                        expectedIndex = i;
                    } else
                    {
                        var LowestSetBit = (byte) BitScanner.BitScanForward((int)codes.m_Codes[i]);
                        if (codes.m_Lengths[i] <= LowestSetBit) throw new IndexOutOfRangeException();
                        if (LowestSetBit >= 32) throw new IndexOutOfRangeException();
                        var Width = (uint)codes.m_Lengths[i] - LowestSetBit - 1;
                        PrefixTableLength = Math.Max(PrefixTableLength, LowestSetBit);
                        indexWidth[LowestSetBit] = Math.Max(indexWidth[LowestSetBit], Width);
                    }
                }
            }

            uint EntryLength = 1;
            for (int i = 0; i <= PrefixTableLength; i++) EntryLength += 1u << (byte)indexWidth[i];

            m_DecodeEntry = new ushort[EntryLength];

            uint decodeBase = 0u;
            for (int i = 0; i <= (int)PrefixTableLength; i++)
            {
                var width = (byte)indexWidth[i];
                m_PrefixTable[i].decodeBase = decodeBase;
                decodeBase += 1u << width;
                m_PrefixTable[i].indexMask = (1u << width) - 1u;
            }

            for (int i = (int)PrefixTableLength + 1; i < 32; i++)
            {
                m_PrefixTable[i].decodeBase = decodeBase;
                m_PrefixTable[i].indexMask = 0;
            }

            decodeBase++;

            m_DecodeEntry.ArraySet<ushort>(0, 0, decodeBase);

            if (codes.m_IndexNumber != 0)
            {
                for (uint i = 0; i < codes.m_IndexNumber; i++)
                {
                    var code = codes.m_Codes[i];
                    if (code == 0) continue;
                    var LowestSetBit = (byte)BitScanner.BitScanForward((int)code);
                    var decodeOff = m_PrefixTable[LowestSetBit].decodeBase + (code >> (LowestSetBit + 1));
                    var bitOff = codes.m_Lengths[i] - LowestSetBit - 1;
                    var incOff = 1u << bitOff;
                    var len = 1u << (byte)(indexWidth[LowestSetBit] - bitOff);

                    for (uint itLen = 0; itLen < len; itLen++, decodeOff += incOff)
                    {
                        m_DecodeEntry[decodeOff] = (ushort)i;
                    }
                }
            }
            if (codes.m_IndexNumber > expectedIndex) m_DecodeEntry[decodeBase - 1] = (ushort)expectedIndex;
        }

        internal uint Decode(uint code)
        {
            code |= 0x80000000;
            var LowestSetBit = (byte)BitScanner.BitScanForward((int)code);
            ref var Prefix = ref m_PrefixTable[LowestSetBit];
            return m_DecodeEntry[Prefix.decodeBase + (Prefix.indexMask & (code >> (LowestSetBit + 1)))];
        }

        internal uint ReadDecode(BitReader reader, Codes codes)
        {
            var ret = Decode((uint)reader.m_ShiftRegister);
            reader.Consume(codes.m_Lengths[ret]);
            return ret;
        }
    }
}
