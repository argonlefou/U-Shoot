using System;

namespace UsCore.MameOutput
{
    /// <summary>
    /// Structure containing information about a specific client registered to get Mame output
    /// </summary>
    internal struct Wm_OutputClient
    {
        /// <summary>
        /// Client ID
        /// </summary>
        public UInt32 Id;
        /// <summary>
        /// Client hWnd
        /// </summary>
        public IntPtr hWnd;
    };
}
