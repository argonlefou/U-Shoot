using System;

namespace UsCore.Memory
{
    public class InjectionStruct64 : InjectionStruct
    {
        public UInt64 InjectionOffset
        { get { return (UInt64)_InjectionOffset; } }

        public UInt64 InjectionReturnOffset
        { get { return ((InjectionOffset + (ulong)_Length)); } }

        public int NeededNops
        { get { return (_Length - 14); } }

        public InjectionStruct64(IntPtr Offset, int Length) : base(Offset, Length)
        { }

        public InjectionStruct64(UInt64 Offset, int Length) : base(new IntPtr((long)Offset), Length)
        { }

        public InjectionStruct64(string OffsetAndLength) : base(OffsetAndLength, typeof(long))
        { }
    }
}
