using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UsCore.RawInput.Win32;

namespace UsCore.RawInput
{
    public class RawInput_Mouse : RawInputController
    {
        //If the controller is Mouse device, we will use the following struct 
        private RawInputDataMouse _RawInputDataMouse;

        private enum SupportedRawInputMouseButtons : int
        {
            Left = 0,
            Right,
            Middle,
            Xbutton1,
            Xbutton2
        }

        

        //Storing Human readable string for mouse buttons, will be used in GUI to set pressed keys
        private Dictionary<int, string> _MouseButtonNames;

        public RawInput_Mouse(IntPtr DeviceHandle) : base(DeviceHandle, RawInputDeviceType.RIM_TYPEMOUSE)
        {
            _Default_ProductName = "Mouse";

            SupportedRawInputAxis = new Dictionary<int, string>()
            {
                { 0x30, "X" },
                { 0x31, "Y" }
            };

            //RawInputMouse messages can only give data for 5 buttons
            //So, we can easily register all of them for any mouse device
            for (int i = 0; i < Enum.GetNames(typeof(SupportedRawInputMouseButtons)).Length; i++)
            {
                RegisterButton(i);
            }

            //RawInputMouse only have 2 axis : X and Y
            //If MOUSE_MOVE_ABSOLUTE value is specified, lLastX and lLastY contain normalized absolute coordinates between 0 and 65,535.
            //Coordinate(0, 0) maps onto the upper-left corner of the display surface;
            //Coordinate(65535, 65535) maps onto the lower-right corner.
            //In a multimonitor system, the coordinates map to the primary monitor.
            foreach (KeyValuePair<int, string> Axis in SupportedRawInputAxis) 
            {
                RegisterAxis(Axis.Key, 0, 0xFFFF);
            }

            _MouseButtonNames = new Dictionary<int, string>()
            {
                { 0x001, "Left Button" },
                { 0x004, "Right Button"},
                { 0x010, "Middle Button"},
                { 0x040, "XButton 1"},
                { 0x100, "XButton 2"},
            };
        }

        //Adding some field to know if the device is "relative" (i.e normal mouse) or "Absolute" (i.e lightgun)
        public override bool IsAbsoluteAxis
        {
            get
            {
                return _RawInputDataMouse.data.usFlags == 0 ? false : true;
            }
        }

        //Number of buttons from RawInputDeviceInfo
        //Should not be used that much...
        public override int NumberOfButtons
        {
            get { return _DeviceInfo.mouse.dwNumberOfButtons; }
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

            result = Win32Api.GetRawInputData(hRawInput, RawInputUiCommand.RID_INPUT, out _RawInputDataMouse, ref size, (uint)Marshal.SizeOf(typeof(RawInputHeader)));
            if (result != 0xFFFF && result == size)
            {
                //PARSE DATA
                SetButtonsState();
                SetAxisValue();
                return true;
            }
            
            return false;
        }

        /// <summary>
        /// Set button state based on the RawInputFlag received
        /// </summary>
        protected override void SetButtonsState()
        {
            switch (_RawInputDataMouse.data.usButtonFlags)
            {
                //Button down flags
                case RawMouseButtonFlags.RI_MOUSE_LEFT_BUTTON_DOWN:
                    {
                        _Buttons[(int)SupportedRawInputMouseButtons.Left].SetState(true);
                        AnyKeyPressedEventArgs e = new AnyKeyPressedEventArgs((int)SupportedRawInputMouseButtons.Left, _MouseButtonNames[(int)_RawInputDataMouse.data.usButtonFlags]);
                        RaiseAnyKeyPressedEvent(e);
                    } break;
                case RawMouseButtonFlags.RI_MOUSE_MIDDLE_BUTTON_DOWN:
                    {
                        _Buttons[(int)SupportedRawInputMouseButtons.Middle].SetState(true);
                        AnyKeyPressedEventArgs e = new AnyKeyPressedEventArgs((int)SupportedRawInputMouseButtons.Middle, _MouseButtonNames[(int)_RawInputDataMouse.data.usButtonFlags]);
                        RaiseAnyKeyPressedEvent(e);
                    }
                    break;
                case RawMouseButtonFlags.RI_MOUSE_RIGHT_BUTTON_DOWN:
                    {
                        _Buttons[(int)SupportedRawInputMouseButtons.Right].SetState(true);
                        AnyKeyPressedEventArgs e = new AnyKeyPressedEventArgs((int)SupportedRawInputMouseButtons.Right, _MouseButtonNames[(int)_RawInputDataMouse.data.usButtonFlags]);
                        RaiseAnyKeyPressedEvent(e);
                    }
                    break;
                case RawMouseButtonFlags.RI_MOUSE_BUTTON_4_DOWN:
                    {
                        _Buttons[(int)SupportedRawInputMouseButtons.Xbutton1].SetState(true);
                        AnyKeyPressedEventArgs e = new AnyKeyPressedEventArgs((int)SupportedRawInputMouseButtons.Xbutton1, _MouseButtonNames[(int)_RawInputDataMouse.data.usButtonFlags]);
                        RaiseAnyKeyPressedEvent(e);
                    }
                    break;
                case RawMouseButtonFlags.RI_MOUSE_BUTTON_5_DOWN:
                    {
                        _Buttons[(int)SupportedRawInputMouseButtons.Xbutton2].SetState(true);
                        AnyKeyPressedEventArgs e = new AnyKeyPressedEventArgs((int)SupportedRawInputMouseButtons.Xbutton2, _MouseButtonNames[(int)_RawInputDataMouse.data.usButtonFlags]);
                        RaiseAnyKeyPressedEvent(e);
                    }
                    break;


                //Button Up flags
                case RawMouseButtonFlags.RI_MOUSE_LEFT_BUTTON_UP:
                    {
                        _Buttons[(int)SupportedRawInputMouseButtons.Left].SetState(false);
                    }
                    break;
                case RawMouseButtonFlags.RI_MOUSE_MIDDLE_BUTTON_UP:
                    {
                        _Buttons[(int)SupportedRawInputMouseButtons.Middle].SetState(false);
                    }
                    break;
                case RawMouseButtonFlags.RI_MOUSE_RIGHT_BUTTON_UP:
                    {
                        _Buttons[(int)SupportedRawInputMouseButtons.Right].SetState(false);
                    }
                    break;
                case RawMouseButtonFlags.RI_MOUSE_BUTTON_4_UP:
                    {
                        _Buttons[(int)SupportedRawInputMouseButtons.Xbutton1].SetState(false);
                    }
                    break;
                case RawMouseButtonFlags.RI_MOUSE_BUTTON_5_UP:
                    {
                        _Buttons[(int)SupportedRawInputMouseButtons.Xbutton2].SetState(false);
                    }
                    break;
                default: return;
            }
        }

        /// <summary>
        /// Set Axis value
        /// Pretty easy there, as we only have 2 Axis (X and Y)
        /// </summary>
        protected override void SetAxisValue()
        {
            _Axes[0x30].Value = _RawInputDataMouse.data.lLastX;
            _Axes[0x31].Value = _RawInputDataMouse.data.lLastY;
            RaiseAnyAxisChangedEvent(new AnyAxisChangedEventArgs(0x30, _Axes[0x30].Value));
        }

        /// <summary>
        /// Retrieve a specific button based on it's ID
        /// </summary>
        /// <param name="ButtonId">Button ID, as specified in the config file</param>
        /// <returns></returns>
        public override RawInputButton GetButton(int ButtonId)
        {
            return _Buttons[ButtonId];
        }
    }
}
