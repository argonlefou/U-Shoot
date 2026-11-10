using System;

namespace UsCore.Memory
{
    public class BreakpointStruct64 : BreakpointStruct
    {
        public UInt64 MemoryOffset
        { get { return (UInt64)_MemoryOffset; } }

        public UInt64 NextInstructionAddress
        { get { return (MemoryOffset + (UInt64)Length); } }

        public BreakpointStruct64(IntPtr MemoryOffset, int InstructionLength) : base(MemoryOffset, InstructionLength)
        { }

        public BreakpointStruct64(UInt64 MemoryOffset, int InstructionLength) : base(new IntPtr((long)MemoryOffset), InstructionLength)
        { }

        public BreakpointStruct64(string OffsetAndLength) : base(OffsetAndLength, typeof(int))
        { }

        public override string ToString()
        {
            return "Address = 0x" + _MemoryOffset.ToString("X16") + ", Hex Code = 0x" + InitialAssemblyCode.ToString("X2") + ", Next Instruction Address = 0x" + NextInstructionAddress.ToString("X16");
        }
    }
}
