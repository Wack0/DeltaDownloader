using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wintellect.PowerCollections;

namespace MsDelta
{
    public class RiftTable
    {
        public bool Valid { get; }
        public OrderedMultiDictionary<ulong, ulong> Dictionary { get; } = new OrderedMultiDictionary<ulong, ulong>(true);

        public RiftTable(BitReader reader)
        {
            Valid = reader.ReadBool();
            if (!Valid) return;

            var leftFormat = new IntFormat(reader);
            var rightFormat = new IntFormat(reader);

            var count = reader.ReadInt();
            if (count > 0x0FFF_FFFF) throw new InvalidDataException();

            ulong key = 0;
            ulong value = 0;
            for (; count != 0; count--)
            {
                key += leftFormat.ReadNumber(reader);
                value += rightFormat.ReadNumber(reader);
                Dictionary.Add(key, key + value);
            }
        }
    }
}
