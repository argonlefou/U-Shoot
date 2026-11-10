using System;

namespace UsCore.Memory
{
    public class InjectionStruct32 : InjectionStruct
    {
        public UInt32 InjectionOffset
        { get { return (UInt32)_InjectionOffset; } }

        public UInt32 InjectionReturnOffset
        { get { return (InjectionOffset + (uint)_Length); } }

        public int NeededNops
        { get { return (_Length - 5); } }

        public InjectionStruct32(IntPtr Offset, int Length) : base(Offset, Length)
        { }

        public InjectionStruct32(UInt32 Offset, int Length) : base (new IntPtr(Offset), Length)
        { }

        public InjectionStruct32(string OffsetAndLength) : base (OffsetAndLength, typeof(int))
        { }
    }
}
