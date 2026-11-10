using System;
using System.Runtime.InteropServices;
using System.Text;
using UsCore.RawInput.Win32;

namespace UsCore.RawInput
{
    public class RawInput_Keyboard : RawInputController
    {
        private const int MAX_PATH = 260;

        //Data used when processing RawInputMessage
        private RawInputDataKeyboard _RawInputDataKeyboard;

        public RawInput_Keyboard(IntPtr DeviceHandle) : base(DeviceHandle, RawInputDeviceType.RIM_TYPEKEYBOARD)
        {
            _Default_ProductName = "Standard Keyboard";
        }

        public override int NumberOfButtons
        {
            get { return _DeviceInfo.keyboard.dwNumberOfKeysTotal; }
        }

        /// <summary>
        /// Retrieving the raw input data struct when a WM_INPUT message has arrived
        /// For Mouse and keyboard we can directly fill the RawInput struct
        /// For HID we need to get RawHeader and RawHid separatly because RawHid is not constant size
        /// </summary>
        /// <param name="hRawInput">LParam parameter from WindowLoop during WM_INPUT message</param>
        /// <returns></returns>
        protected override bool GetRawInputData(IntPtr hRawInput)
        {
            uint HeaderSize = (uint)Marshal.SizeOf(typeof(RawInputHeader));
            uint dwSize = 0;
            uint size = (uint)_RawInputHeader.dwSize;
            Win32Api.GetRawInputData(hRawInput, RawInputUiCommand.RID_INPUT, IntPtr.Zero, ref dwSize, (uint)Marshal.SizeOf(typeof(RawInputHeader)));
            uint result;

            result = Win32Api.GetRawInputData(hRawInput, RawInputUiCommand.RID_INPUT, out _RawInputDataKeyboard, ref size, (uint)Marshal.SizeOf(typeof(RawInputHeader)));
            if (result != 0xFFFF && result == size)
            {
                //PARSE DATA
                SetButtonsState();
                return true;
            }
            
            return false;
        }

        /// <summary>
        /// Detect if a button/key was pressed in the last WM_INPUT message received
        /// </summary>
        protected override void SetButtonsState()
        {
            // Ignore key overrun state and keys not mapped to any virtual key code
            if (_RawInputDataKeyboard.data.MakeCode == Win32Define.KEYBOARD_OVERRUN_MAKE_CODE || _RawInputDataKeyboard.data.VKey >= byte.MaxValue)
                return;

            int FullScancode = GetFullScancodeFromLastKey();
            RawInputButton RiButton = GetButtonFromScancode(FullScancode);

            if (_RawInputDataKeyboard.data.Message == Win32Define.WM_KEYDOWN)
            {
                AnyKeyPressedEventArgs e = new AnyKeyPressedEventArgs(FullScancode, GetKeyName((ushort)FullScancode));
                RaiseAnyKeyPressedEvent(e);
                if (RiButton != null)
                    RiButton.SetState(true);
            }
            else if (_RawInputDataKeyboard.data.Message == Win32Define.WM_KEYUP)
            {
                if (RiButton != null)
                    RiButton.SetState(false);
            }
        }

        /// <summary>
        /// Return the Full scancode of the key, with extended bytes for LEFT/RIGHT side of the kayboard
        /// </summary>
        public ushort GetFullScancodeFromLastKey()
        {
            ushort ScanCode = 0;
            if (_RawInputDataKeyboard.data.MakeCode != 0)
            {
                // Compose the full scan code value with its extended byte
                byte lowByte = (byte)(_RawInputDataKeyboard.data.MakeCode & 0x7f);
                byte highByte = (_RawInputDataKeyboard.data.Flags & RawKeyboardFlags.RI_KEY_E0) != 0 ? (byte)0xe0 :
                                (_RawInputDataKeyboard.data.Flags & RawKeyboardFlags.RI_KEY_E1) != 0 ? (byte)0xe1 : (byte)0x00;
                ScanCode = (ushort)(lowByte | (highByte << 8));
            }
            else
            {
                // Scan code value may be empty for some buttons (e.g., multimedia buttons)
                // Try to get the scan code from the virtual key code
                ScanCode = (ushort)(Win32Api.MapVirtualKey(_RawInputDataKeyboard.data.VKey, Win32Define.MAPVK_VK_TO_VSC_EX) & 0xFFFF);
            }
            return ScanCode;
        }

        /// <summary>
        /// Return human readable name of a keyboard key, based on the ScanCode
        /// </summary>
        /// <returns></returns>
        public string GetKeyName(ushort ScanCode)
        {
            // Get the key name for debug output
            StringBuilder keyNameBuffer = new StringBuilder(MAX_PATH);   //MAX_PATH=260
            int lParam = MakeLParam(0, ((ScanCode >> 8) != 0 ? Win32Define.KF_EXTENDED : 0x00) | (ScanCode & 0xFF));
            Win32Api.GetKeyNameText(lParam, keyNameBuffer, MAX_PATH);

            return keyNameBuffer.ToString();
        }
        // Helper method to create LPARAM
        private int MakeLParam(int low, int high)
        {
            return (int)((ushort)low | ((ushort)high << 16));
        }

        /// <summary>
        /// Search for a specific button in the device registered buttons
        /// </summary>
        /// <param name="Scancode">Full Scancode of the Button, as specified in the config file</param>
        /// <returns></returns>
        private RawInputButton GetButtonFromScancode(int Scancode)
        {
            try
            {
                return _Buttons[Scancode];
            }
            catch { return null; }
        }

        /// <summary>
        /// Retrieve a specific button based on it's ID
        /// </summary>
        /// <param name="ButtonId">Button ID, as specified in the config file</param>
        /// <returns></returns>
        public override RawInputButton GetButton(int ButtonId)
        {
            return GetButtonFromScancode(ButtonId);
        }
    }
}
