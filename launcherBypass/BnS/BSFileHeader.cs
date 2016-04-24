using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace launcherBypass.BnS
{
    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    internal struct BSFileHeader
    {
        [MarshalAs(UnmanagedType.I1)]
        [FieldOffset(0)]
        internal byte somenum;

        [MarshalAs(UnmanagedType.I1)]
        [FieldOffset(1)]
        internal bool somebool;

        [MarshalAs(UnmanagedType.I1)]
        [FieldOffset(2)]
        internal bool somebool2;

        [MarshalAs(UnmanagedType.I1)]
        [FieldOffset(3)]
        internal bool somebool3;

        [MarshalAs(UnmanagedType.I4)]
        [FieldOffset(4)]
        internal int uncompressed;

        [MarshalAs(UnmanagedType.I4)]
        [FieldOffset(8)]
        internal int compressedNoPadding;

        [MarshalAs(UnmanagedType.I4)]
        [FieldOffset(12)]
        internal int compressed;

        [MarshalAs(UnmanagedType.I4)]
        [FieldOffset(16)]
        internal int offset;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 60)]
        [FieldOffset(20)]
        internal byte[] zero;
    }
}
