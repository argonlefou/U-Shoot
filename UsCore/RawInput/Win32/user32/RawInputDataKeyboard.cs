using System;
using System.Runtime.InteropServices;

namespace UsCore.RawInput.Win32
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct RawInputDataKeyboard
    {
        public RawInputHeader header;           // 64 bit header size is 24  32 bit the header size is 16
        public RawKeyboard data;                // Creating the rest in a struct allows the header size to align correctly for 32 or 64 bit
    }

    /// <summary>
    /// Contains information about the state of the Keyboard
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    internal struct RawKeyboard
    {
        /// <summary>
        /// Specifies the scan code associated with a key press
        /// </summary>
        [FieldOffset(0)]
        public ushort MakeCode;

        /// <summary>
        /// Flags for scan code information
        /// </summary>
        [FieldOffset(2)]
        public RawKeyboardFlags Flags;

        /// <summary>
        /// Reserved
        /// </summary>
        [FieldOffset(4)]
        public ushort Reserved;

        /// <summary>
        /// The corresponding legacy virtual-key code.
        /// </summary>
        [FieldOffset(6)]
        public ushort VKey;

        /// <summary>
        /// The corresponding legacy keyboard window message, for example WM_KEYDOWN, WM_SYSKEYDOWN, and so forth.
        /// </summary>
        [FieldOffset(8)]
        public uint Message;

        /// <summary>
        /// The device-specific additional information for the event.
        /// </summary>
        [FieldOffset(12)]
        public ulong ExtraInformation;

        public override string ToString()
        {
            return string.Format("MakeCode: {0}, Flags: {1}, Vkey: {2}, Message: {3}, ExtraInformation: {4}", MakeCode, Flags, VKey, Message, ExtraInformation);
        }
    }

    [Flags]
    internal enum RawKeyboardFlags : ushort
    {
        /// <summary>
        /// The key is down.
        /// </summary>
        RI_KEY_MAKE = 0,

        /// <summary>
        /// The key is up.
        /// </summary>
        RI_KEY_BREAK =  1,

        /// <summary>
        /// The scan code has the E0 prefix.
        /// </summary>
        RI_KEY_E0 = 2,

        /// <summary>
        /// The scan code has the E1 prefix.
        /// </summary>
        RI_KEY_E1 = 4
    }
}
