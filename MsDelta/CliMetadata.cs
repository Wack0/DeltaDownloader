using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MsDelta
{
    public class CliMetadata
    {
        public readonly bool m_Valid = false;
        public readonly uint
            m_StartOffset, m_Size, m_BaseRva, m_StreamsNumber, m_StreamHeadersOffset, m_StringsStreamOffset, m_StringsStreamSize, m_USStreamOffset, m_USStreamSize,
            m_BlobStreamOffset, m_BlobStreamSize, m_GuidStreamOffset, m_GuidStreamSize, m_TablesStreamOffset, m_TablesStreamSize;
        public readonly bool m_LongStringsStream, m_LongGuidStream, m_LongBlobStream;
        public readonly ulong m_ValidTables;

        [StructLayout(LayoutKind.Explicit, Pack = 1, Size = sizeof(uint) * 64)]
        private struct RowsNumberBuffer { }

        private RowsNumberBuffer m_Buffer = default;
        Span<uint> m_RowsNumber => Helpers.AsSpan<uint, RowsNumberBuffer>(ref m_Buffer);

        public CliMetadata(BitReader reader)
        {
            m_Valid = reader.ReadBool();
            if (!m_Valid) return;

            m_StartOffset = reader.ReadU32();
            m_Size = reader.ReadU32();
            m_BaseRva = reader.ReadU32();
            m_StreamsNumber = reader.ReadU32();
            m_StreamHeadersOffset = reader.ReadU32();
            m_StringsStreamOffset = reader.ReadU32();
            m_StringsStreamSize = reader.ReadU32();
            m_USStreamOffset = reader.ReadU32();
            m_USStreamSize = reader.ReadU32();
            m_BlobStreamOffset = reader.ReadU32();
            m_BlobStreamSize = reader.ReadU32();
            m_GuidStreamOffset = reader.ReadU32();
            m_GuidStreamSize = reader.ReadU32();
            m_TablesStreamOffset = reader.ReadU32();
            m_TablesStreamSize = reader.ReadU32();

            m_LongStringsStream = reader.ReadBool();
            m_LongGuidStream = reader.ReadBool();
            m_LongBlobStream = reader.ReadBool();

            m_ValidTables = reader.Read64(64);

            var tables = m_ValidTables;

            for (int i = 0; i < 64; i++, tables >>= 1)
            {
                uint rowNumber = 0;
                if ((tables & 1) != 0)
                {
                    rowNumber = reader.ReadU32();
                }
                m_RowsNumber[i] = rowNumber;
            }
        }
    }
}
