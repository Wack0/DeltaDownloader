using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MsDelta
{
    public struct BitScanner
    {
        private static int[] _table = {
            0,  1, 16,  2, 29, 17,  3, 22,
            30, 20, 18, 11, 13,  4,  7, 23,
            31, 15, 28, 21, 19, 10, 12,  6,
            14, 27,  9,  5, 26,  8, 25, 24,
        };

        private static int[] _tableReverse =
        {
             0,  1, 28,  2, 29, 14, 24,  3,
            30, 22, 20, 15, 25, 17,  4,  8,
            31, 27, 13, 23, 21, 19, 16,  7,
            26, 12, 18,  6, 11,  5, 10,  9
        };

        private const int DeBruijnSequence = 0x6EB14F9;
        private const int DeBruijnSequenceReverse = 0x77CB531;

        private static int IsolateLsb(uint x) => (int)(x & -x);
        private static int IsolateMsb(uint x)
        {
            x |= (x >> 1);
            x |= (x >> 2);
            x |= (x >> 4);
            x |= (x >> 8);
            x |= (x >> 16);
            x = (x >> 1) + 1;
            return (int)x;
        }
        public static int BitScanForward(int x)
          => _table[(uint)(IsolateLsb((uint)x) * DeBruijnSequence) >> 27];

        public static int BitScanReverse(int x)
            => _tableReverse[(uint)(IsolateMsb((uint)x) * DeBruijnSequenceReverse) >> 27];
}
}
