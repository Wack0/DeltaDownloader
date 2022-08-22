using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MsDelta
{
    internal enum FileTypeCodeBit
    {
        Raw,
        I386,
        IA64,
        AMD64,
        CLI4_I386,
        CLI4_AMD64,
        CLI4_ARM,
        CLI4_ARM64
    }
    [Flags]
    public enum FileTypeCode : ulong
    {
        Raw = (1 << FileTypeCodeBit.Raw),
        I386 = (1 << FileTypeCodeBit.I386),
        IA64 = (1 << FileTypeCodeBit.IA64),
        AMD64 = (1 << FileTypeCodeBit.AMD64),
        CLI4_I386 = (1 << FileTypeCodeBit.CLI4_I386),
        CLI4_AMD64 = (1 << FileTypeCodeBit.CLI4_AMD64),
        CLI4_ARM = (1 << FileTypeCodeBit.CLI4_ARM),
        CLI4_ARM64 = (1 << FileTypeCodeBit.CLI4_ARM64)
    }
}
