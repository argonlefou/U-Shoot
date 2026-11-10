using System;
using System.Globalization;
using UsCore.Logging;

namespace UsCore.Memory
{
    public class NopStruct
    {
        protected IntPtr _MemoryOffset;
        protected int _Length;

        public int Length
        { get { return _Length; } }

        public NopStruct(IntPtr Offset, int NopLength)
        {
            _MemoryOffset = Offset;
            _Length = NopLength;
        }

        public NopStruct(string OffsetAndNumber, Type OffsetParsingType)
        {
            _MemoryOffset = IntPtr.Zero;
            _Length = 0;
            if (OffsetAndNumber != null)
            {
                try
                {
                    string[] strParts = OffsetAndNumber.Split('|');
                    _Length = int.Parse(strParts[1]);

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
                    Logger.WriteLog("Impossible to load " + this.GetType().Name + " from following String : " + OffsetAndNumber);
                }
            }
        }
    }
}
