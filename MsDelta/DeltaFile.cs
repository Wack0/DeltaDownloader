using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MsDelta
{
    public class DeltaFile
    {
        public DateTime FileTime { get; }
        public ulong Version { get; }
        public FileTypeCode Code { get; }
        public DeltaFlags Flags { get; }
        public ulong TargetSize { get; }
        public CryptAlg HashAlgorithm { get; }
        public byte[] Hash { get; }
        public PreProcess FileTypeHeader { get; }

        private static bool HasSignatureAtOffset(byte[] bytes, int offset)
        {
            return bytes[offset + 0] == (byte)'P' && bytes[offset + 1] == (byte)'A' && bytes[offset + 2] == (byte)'3' && bytes[offset + 3] == (byte)'0';
        }

        public static int FindOffsetOfDelta(byte[] bytes, int startOffset = 0)
        {
            while (!HasSignatureAtOffset(bytes, startOffset))
            {
                startOffset += 4;
                if (startOffset >= bytes.Length) throw new InvalidDataException();
            }
            return startOffset;
        }

        public DeltaFile(byte[] bytes) : this(bytes, FindOffsetOfDelta(bytes))
        {

        }

        public DeltaFile(byte[] bytes, int startOffset)
        {
            if (!HasSignatureAtOffset(bytes, startOffset)) throw new InvalidDataException();
            int offset = startOffset + 4;
            long ft64 = 0;
            using (var br = new BinaryReader(new MemoryStream(bytes, offset, bytes.Length - offset)))
            {
                ft64 = br.ReadInt64();
            }
            offset += sizeof(ulong);
            FileTime = DateTime.FromFileTimeUtc(ft64);
            var reader = new BitReader(bytes, offset);
            // header
            Version = reader.ReadInt();
            Code = (FileTypeCode)reader.ReadInt();
            Flags = (DeltaFlags)reader.ReadInt();
            TargetSize = reader.ReadInt();
            HashAlgorithm = (CryptAlg)reader.ReadInt();
            Hash = reader.ReadBuffer();

            // buffers
            var preProcessBuffer = reader.ReadBuffer();
            var patchBuffer = reader.ReadBuffer();
            Debug.Assert(reader.AtEnd);

            FileTypeHeader = new PreProcess(Code, preProcessBuffer);
        }
    }
}
