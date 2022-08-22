using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MsDelta
{
    [Flags]
    public enum DeltaFlags : ulong
    {
        /** No flags. */
        DELTA_FLAG_NONE = (0x00000000),
        /** Allow application of legacy PA19 deltas by mspatcha.dll. */
        DELTA_APPLY_FLAG_ALLOW_PA19 = (0x00000001),
        /** Transform E8 pieces (relative calls in x86), of target file . */
        DELTA_FLAG_E8 = (0x00000001), /* flags[ 0 ] */
        /** Mark non-executable parts of source PE. */
        DELTA_FLAG_MARK = (0x00000002), /* flags[ 1 ] */
        /** Transform imports of source PE. */
        DELTA_FLAG_IMPORTS = (0x00000004), /* flags[ 2 ] */
        /** Transform exports of source PE. */
        DELTA_FLAG_EXPORTS = (0x00000008), /* flags[ 3 ] */
        /** Transform resources of source PE. */
        DELTA_FLAG_RESOURCES = (0x00000010), /* flags[ 4 ] */
        /** Transform relocations of source PE. */
        DELTA_FLAG_RELOCS = (0x00000020), /* flags[ 5 ] */
        /** Smash lock prefixes of source PE. */
        DELTA_FLAG_I386_SMASHLOCK = (0x00000040), /* flags[ 6 ] */
        /** Transform relative jumps of source I386 (x86), PE. */
        DELTA_FLAG_I386_JMPS = (0x00000080), /* flags[ 7 ] */
        /** Transform relative calls of source I386 (x86), PE. */
        DELTA_FLAG_I386_CALLS = (0x00000100), /* flags[ 8 ] */
        /** Transform instructions of source AMD64 (x86-64), PE. */
        DELTA_FLAG_AMD64_DISASM = (0x00000200), /* flags[ 9 ] */
        /** Transform pdata of source AMD64 (x86-64), PE. */
        DELTA_FLAG_AMD64_PDATA = (0x00000400), /* flags[ 10 ] */
        /** Transform intstructions of source IA64 (Itanium), PE. */
        DELTA_FLAG_IA64_DISASM = (0x00000800), /* flags[ 11 ] */
        /** Transform pdata of source IA64 (Itanium), PE. */
        DELTA_FLAG_IA64_PDATA = (0x00001000), /* flags[ 12 ] */
        /** Unbind source PE. */
        DELTA_FLAG_UNBIND = (0x00002000), /* flags[ 13 ] */
        /** Transform CLI instructions of source PE. */
        DELTA_FLAG_CLI_DISASM = (0x00004000), /* flags[ 14 ] */
        /** Transform CLI Metadata of source PE. */
        DELTA_FLAG_CLI_METADATA = (0x00008000), /* flags[ 15 ] */
        /** Transform headers of source PE. */
        DELTA_FLAG_HEADERS = (0x00010000), /* flags[ 16 ] */
        /** Allow source or target file or buffer to exceed its default size limit. */
        DELTA_FLAG_IGNORE_FILE_SIZE_LIMIT = (0x00020000), /* flags[ 17 ] */
        /** Allow options buffer or file to exceeed its default size limit. */
        DELTA_FLAG_IGNORE_OPTIONS_SIZE_LIMIT = (0x00040000), /* flags[ 18 ] */
        /** Transform instructions of source ARM PE. */
        DELTA_FLAG_ARM_DISASM = (0x00080000), /* flags[ 19 ] */
        /** Transform pdata of source ARM PE. */
        DELTA_FLAG_ARM_PDATA = (0x00100000), /* flags[ 20 ] */
        /** Transform CLI4 Metadata of source PE. */
        DELTA_FLAG_CLI4_METADATA = (0x00200000), /* flags[ 21 ] */
        /** Transform CLI4 instructions of source PE. */
        DELTA_FLAG_CLI4_DISASM = (0x00400000), /* flags[ 22 ] */
        /** Transform instructions of source ARM PE. */
        DELTA_FLAG_ARM64_DISASM = (0x00800000), /* flags[ 23 ] */
        /** Transform pdata of source ARM PE. */
        DELTA_FLAG_ARM64_PDATA = (0x01000000), /* flags[ 24 ] */
    }
}
