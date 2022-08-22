using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MsDelta
{
    public class PreProcess
    {
        public ulong ImageBase { get; }
        public uint GlobalPointer { get; }
        public uint TimeStamp { get; }
        public Dictionary<ulong, ulong> RiftTable { get; }
        public CliMetadata CliMetadata { get; }
        public PreProcess(FileTypeCode code, byte[] buffer) : this(code, new BitReader(buffer)) { }

        public PreProcess(FileTypeCode code, BitReader buffer)
        {
            //bool IsNet40 = false;
            switch (code)
            {
                case FileTypeCode.Raw:
                    // nothing
                    return;
                case FileTypeCode.I386:
                case FileTypeCode.IA64:
                case FileTypeCode.AMD64:
                    break;
                default:
                    //IsNet40 = true;
                    break;
            }

            ImageBase = buffer.Read64(64);
            GlobalPointer = buffer.ReadU32();
            TimeStamp = buffer.ReadU32();
            var riftTable = new RiftTable(buffer);
            if (riftTable.Valid)
            {
                RiftTable = new Dictionary<ulong, ulong>();
                foreach (var pair in riftTable.Dictionary.KeyValuePairs)
                {
                    RiftTable.Add(pair.Key, pair.Value);
                }
            }
            CliMetadata = new CliMetadata(buffer);
            if (!CliMetadata.m_Valid) CliMetadata = null;
            //Debug.Assert(buffer.AtEnd);
        }
    }
}
