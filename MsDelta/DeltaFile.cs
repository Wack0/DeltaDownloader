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
        public readonly DateTime FileTime;
        public readonly ulong Version;
        public readonly FileTypeCode Code;
        public readonly DeltaFlags Flags;
        public readonly ulong TargetSize;
        public readonly CryptAlg HashAlgorithm;
        public readonly byte[] Hash;

        // Don't really care about this? For PA31 v1 it's 0x88.
        public readonly ulong HeaderInfoSize = 0x50; // sizeof(DELTA_HEADER_INFO)
        // Must be 1 for PA31, if 0 then UpdateCompression!DeltaPatch::CreatePatch creates a PA30.
        public readonly ulong IsPa31 = 0;
        // Observed to be 0 for PA31. Checked (on read and write) to make sure it's <= 1.
        public readonly ulong DeltaClientMinVersion = 0;
        // Not sure from just static analysis. Hash of source file maybe? The same hash algorithm as the other hash is used.
        public readonly byte[] AdditionalHash = new byte[0];

        public readonly PreProcess FileTypeHeader;

        private enum DeltaFileFormat
        {
            PA30,
            PA31
        }

        private static bool HasPA30SignatureAtOffset(byte[] bytes, int offset)
        {
            return bytes[offset + 0] == (byte)'P' && bytes[offset + 1] == (byte)'A' && bytes[offset + 2] == (byte)'3' && bytes[offset + 3] == (byte)'0';
        }

        private static bool HasPA31SignatureAtOffset(byte[] bytes, int offset)
        {
            return bytes[offset + 0] == (byte)'P' && bytes[offset + 1] == (byte)'A' && bytes[offset + 2] == (byte)'3' && bytes[offset + 3] == (byte)'1';
        }

        private static int FindOffsetOfDelta(byte[] bytes, int startOffset = 0)
        {
            while (!HasPA30SignatureAtOffset(bytes, startOffset) && !HasPA31SignatureAtOffset(bytes, startOffset))
            {
                startOffset += 4;
                if (startOffset >= bytes.Length) throw new InvalidDataException();
            }
            return startOffset;
        }

        private static void ParseHeaderPA30(BitReader reader,
            out ulong version,
            out FileTypeCode code,
            out DeltaFlags flags,
            out ulong targetSize,
            out CryptAlg hashAlgorithm,
            out byte[] hash)
        {
            version = reader.ReadInt();
            code = (FileTypeCode)reader.ReadInt();
            flags = (DeltaFlags)reader.ReadInt();
            targetSize = reader.ReadInt();
            hashAlgorithm = (CryptAlg)reader.ReadInt();
            hash = reader.ReadBuffer();
        }

        private static void ParseHeaderPA31(BitReader reader,
            out ulong version,
            out FileTypeCode code,
            out DeltaFlags flags,
            out ulong targetSize,
            out CryptAlg hashAlgorithm,
            out byte[] hash,
            out ulong headerInfoSize,
            out ulong isPa31,
            out ulong deltaClientMinVersion,
            out byte[] additionalHash)
        {
            ParseHeaderPA30(reader,
                out version,
                out code,
                out flags,
                out targetSize,
                out hashAlgorithm,
                out hash);

            headerInfoSize = reader.ReadInt();
            isPa31 = reader.ReadInt();
            deltaClientMinVersion = reader.ReadInt();
            additionalHash = reader.ReadBuffer();
        }

        public DeltaFile(byte[] bytes) : this(bytes, FindOffsetOfDelta(bytes))
        {

        }

        public DeltaFile(byte[] bytes, int startOffset)
        {
            DeltaFileFormat fileFormat;
            if (HasPA30SignatureAtOffset(bytes, startOffset))
            {
                fileFormat = DeltaFileFormat.PA30;
            }
            else if (HasPA31SignatureAtOffset(bytes, startOffset))
            {
                fileFormat = DeltaFileFormat.PA31;
            }
            else
            {
                throw new InvalidDataException();
            }

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
            switch (fileFormat)
            {
                case DeltaFileFormat.PA30:
                    ParseHeaderPA30(reader,
                        out Version,
                        out Code,
                        out Flags,
                        out TargetSize,
                        out HashAlgorithm,
                        out Hash);
                    break;

                case DeltaFileFormat.PA31:
                    var headerBytes = reader.ReadBuffer();
                    var headerReader = new BitReader(headerBytes);
                    ParseHeaderPA31(headerReader,
                        out Version,
                        out Code,
                        out Flags,
                        out TargetSize,
                        out HashAlgorithm,
                        out Hash,
                        out HeaderInfoSize,
                        out IsPa31,
                        out DeltaClientMinVersion,
                        out AdditionalHash);
                    Debug.Assert(headerReader.AtEnd);
                    break;
            }

            // buffers
            var preProcessBuffer = reader.ReadBuffer();
            var patchBuffer = reader.ReadBuffer();
            Debug.Assert(reader.AtEnd);

            FileTypeHeader = new PreProcess(Code, preProcessBuffer);
        }
    }
}
