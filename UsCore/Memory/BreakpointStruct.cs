using System;
using System.Globalization;
using UsCore.Logging;

namespace UsCore.Memory
{
    public class BreakpointStruct
    {
        protected IntPtr _MemoryOffset;

        public int Length
        { get; protected set; }

        public byte InitialAssemblyCode
        { get; set; }        

        public BreakpointStruct(IntPtr MemoryOffset, int InstructionLength)
        {
            _MemoryOffset = MemoryOffset;
            Length = InstructionLength;
        }

        public BreakpointStruct(string OffsetAndLength, Type OffsetParsingType)
        {
            _MemoryOffset = IntPtr.Zero;
            Length = 0;
            if (OffsetAndLength != null)
            {
                try
                {
                    string[] strParts = OffsetAndLength.Split('|');
                    Length = int.Parse(strParts[1]);

                    if (OffsetParsingType == typeof(int))
                    {
                        _MemoryOffset = new IntPtr(int.Parse(strParts[0].Substring(2).Trim(), NumberStyles.HexNumber));
                    }
                    else if (OffsetParsingType == typeof(long))
                    {
                        _MemoryOffset = new IntPtr(long.Parse(strParts[0].Substring(2).Trim(), NumberStyles.HexNumber));
                    }
                }
                catch
                {
                    Logger.WriteLog("Impossible to load " + this.GetType().Name + " from following String : " + OffsetAndLength);
                }
            }
        }
    }
}
