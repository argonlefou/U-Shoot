using System;
using System.Globalization;
using UsCore.Logging;

namespace UsCore.Memory
{
    public class InjectionStruct
    {
        protected IntPtr _InjectionOffset;
        protected int _Length;

        public int Length
        { get { return _Length; } }

        public InjectionStruct(IntPtr Offset, int length)
        {
            _InjectionOffset = Offset;
            _Length = length;
        }

        public InjectionStruct(string OffsetAndLength, Type OffsetParsingType)
        {
            _InjectionOffset = IntPtr.Zero;
            _Length = 0;
            if (OffsetAndLength != null)
            {
                try
                {
                    string[] strParts = OffsetAndLength.Split('|');
                    _Length = int.Parse(strParts[1]);

                    if (OffsetParsingType == typeof(int))
                    {
                        _InjectionOffset = new IntPtr(int.Parse(strParts[0].Substring(2).Trim(), NumberStyles.HexNumber));
                    }
                    else if (OffsetParsingType == typeof(long))
                    {
                        _InjectionOffset = new IntPtr(long.Parse(strParts[0].Substring(2).Trim(), NumberStyles.HexNumber));
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
