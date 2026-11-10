using System;

namespace UsCore.Memory
{
    // <summary>
    /// Defines how many NOP to write at a given Memory offset
    /// </summary>
    public class NopStruct32 : NopStruct
    {
        public UInt32 MemoryOffset
        { get { return (UInt32)_MemoryOffset; } }

        public NopStruct32(IntPtr Offset, int NopLength) : base(Offset, NopLength)
        { }

        public NopStruct32(UInt32 Offset, int NopLength) : base(new IntPtr(Offset), NopLength)
        { }

        public NopStruct32(string OffsetAndNumber) : base(OffsetAndNumber, typeof(int))
        { }
    }
}
