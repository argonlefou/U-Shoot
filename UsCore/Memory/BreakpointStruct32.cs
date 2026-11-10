using System;

namespace UsCore.Memory
{
    public class BreakpointStruct32 : BreakpointStruct
    {
        public UInt32 MemoryOffset
        { get { return (UInt32)_MemoryOffset; } }

        public UInt32 NextInstructionAddress
        { get { return (MemoryOffset + (UInt32)Length); } }

        public BreakpointStruct32(IntPtr MemoryOffset, int InstructionLength) : base(MemoryOffset, InstructionLength)
        { }

        public BreakpointStruct32(UInt32 MemoryOffset, int InstructionLength) : base (new IntPtr(MemoryOffset), InstructionLength)
        { }

        public BreakpointStruct32(string OffsetAndLength) : base(OffsetAndLength, typeof(int))
        { }

        public override string ToString()
        {
            return "Address = 0x" + _MemoryOffset.ToString("X8") + ", Hex Code = 0x" + InitialAssemblyCode.ToString("X2") + ", Next Instruction Address = 0x" + NextInstructionAddress.ToString("X8");
        }
    }
}
