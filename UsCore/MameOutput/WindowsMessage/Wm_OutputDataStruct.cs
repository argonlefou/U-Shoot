using System;
using System.Runtime.InteropServices;

namespace UsCore.MameOutput
{    
    /// <summary>
    /// Structure used to transfer String ID data to MameHooker
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    internal struct Wm_OutputDataStruct
    {
        /// <summary>
        /// Requested ID
        /// </summary>
        public UInt32 Id;
        // The string is included inline in the data.
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 255)]
        public string lpStr;
    }
}
