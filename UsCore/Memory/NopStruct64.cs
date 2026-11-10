using System;

namespace UsCore.Memory
{
    public class NopStruct64 : NopStruct
    {
        public UInt64 MemoryOffset
        { get { return (UInt64)_MemoryOffset; } }

        public NopStruct64(IntPtr Offset, int NopLength) : base(Offset, NopLength)
        { }

        public NopStruct64(UInt64 Offset, int NopLength) : base(new IntPtr((long)Offset), NopLength)
        { }

        public NopStruct64(string OffsetAndNumber) : base(OffsetAndNumber, typeof(long))
        { }
    }
}
