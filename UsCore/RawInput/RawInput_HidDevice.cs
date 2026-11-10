using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UsCore.Logging;
using UsCore.RawInput.Win32;

namespace UsCore.RawInput
{
    public class RawInput_HidDevice : RawInputController
    {        
        //If the controller is HID device, we will use the following struct to parse WM_INPUT:
        private RawHid _RawHidData;
        private int _Hid_NumberofButtons;
        private int _Hid_NumberofAxis;
        private List<ushort> _Hid_Axis_List;
        #region Accessors

        public override int NumberOfButtons
        {
            get { return _Hid_NumberofButtons; }
        }
        
        public int NumberOfAxis
        {
            get { return _Hid_NumberofAxis; }
        }

        public List<ushort> AxisList
        {
            get { return _Hid_Axis_List; }
        }

        #endregion

        //Adding some field to know if the device is "relative" (i.e normal mouse) or "Absolute" (i.e lightgun)
        public override bool IsAbsoluteAxis
        {
            get
            {
                return true;
            }
        }

        //HidP variables
        private IntPtr _pPreparsedData;
        private HidPCaps _Caps;
        private HidPButtonCaps[] _pButtonCaps;
        private HidPValueCaps[] _pValueCaps;
        //#region Accessors

        //public HidPCaps HID_Capabilities
        //{
        //    get { return _Caps; }
        //}

        //public HidPButtonCaps[] HID_ButtonCapabilitiesArray
        //{
        //    get { return _pButtonCaps; }
        //}

        //public HidPValueCaps[] HID_ValueCapabilitiesArray
        //{
        //    get { return _pValueCaps; }
        //}

        //public HidPValueCaps[] HID_OutputValueCapabilitiesArray
        //{
        //    get { return _pOutputValueCaps; }
        //}

        //#endregion

        public RawInput_HidDevice(IntPtr DeviceHandle) : base(DeviceHandle, RawInputDeviceType.RIM_TYPEHID)
        {
            _Default_ProductName = "Unknown HID Device";

            SupportedRawInputAxis = new Dictionary<int, string>()
            {
                { 0x30, "X" },
                { 0x31, "Y" },
                { 0x32, "Z" },
                { 0x33, "Rx" },
                { 0x34, "Ry" },
                { 0x35, "Rz" }
            };

            _pPreparsedData = GetPreparsedData();
            if (_pPreparsedData != IntPtr.Zero)
            {
                if (!GetCapabilities(_pPreparsedData, out _Caps))
                {
                    Logger.WriteLog("[" + this.GetType().Name + "] CCtor() Error: Impossible to get Capabilities for device " + _DevicePath);
                    return;
                }

                if (!GetButtonCapabilities(_pPreparsedData, _Caps, out _pButtonCaps))
                {
                    Logger.WriteLog("[" + this.GetType().Name + "] CCtor() Error: Error: Impossible to get Button Capabilities for device " + _DevicePath);
                    return;
                }
                _Hid_NumberofButtons = GetNumberOfHidDeviceButtons(_pPreparsedData, _pButtonCaps);
                for (int i = _pButtonCaps[0].Range.UsageMin; i <= _pButtonCaps[0].Range.UsageMax; i++)
                {
                    RegisterButton(i);  //Button1 .... Button[n]
                }

                if (!GetValueCapabilities(_pPreparsedData, _Caps, out _pValueCaps))
                {
                    Logger.WriteLog("[" + this.GetType().Name + "] CCtor() Error: Error: Impossible to get Value Capabilities for device " + _DevicePath);
                    return;
                }

                //Number of absolute axis
                _Hid_Axis_List = new List<ushort>();
                for (int i = 0; i < _pValueCaps.Length; i++)
                {
                    if (_pValueCaps[i].IsAbsolute)
                    {
                        _Hid_NumberofAxis++;
                        _Hid_Axis_List.Add(_pValueCaps[i].Range.UsageMin);

                        RegisterAxis(_pValueCaps[i].Range.UsageMin, _pValueCaps[i].LogicalMin, Correct_Axis_Max(_pValueCaps[i].LogicalMax));
                    }
                }
            }
        }

        /// <summary>
        /// Retrieve the pointer to the previously parsed data for a HID device
        /// </summary>
        /// <returns>A pointer to the pPreparsedData struct is succes, otherwise IntPtr.Zero</returns>
        private IntPtr GetPreparsedData()
        {
            uint result = 0;
            uint bufferSize = 0;
            result = Win32Api.GetRawInputDeviceInfo(_hDevice, RawInputUiCommand.RID_PREPARSEDDATA, IntPtr.Zero, ref bufferSize);
            if (result != 0)
            {
                return IntPtr.Zero;
            }

            IntPtr pPreparsedData = Marshal.AllocHGlobal((int)bufferSize);
            result = Win32Api.GetRawInputDeviceInfo(_hDevice, RawInputUiCommand.RID_PREPARSEDDATA, pPreparsedData, ref bufferSize);
            if (result == 0)
            {
                Marshal.FreeHGlobal(pPreparsedData);
                return IntPtr.Zero;
            }

            return pPreparsedData;
        }

        /// <summary>
        /// Retrieve a top-level collection's HIDP_CAPS structure for this device.
        /// </summary>
        /// <param name="pPreparsedData">Pointer to a top-level collection's preparsed data.</param>
        /// <param name="Caps">Pointer to a caller-allocated buffer that the routine uses to return a collection's HIDP_CAPS structure</param>
        /// <returns>TRUE is success, otherwise FALSE</returns>
        private bool GetCapabilities(IntPtr pPreparsedData, out HidPCaps Caps)
        {
            if (Win32Api.HidP_GetCaps(pPreparsedData, out Caps) != NtStatus.Success)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Retrieve a button capability array that describes all the HID control buttons in a top-level collection for a specified type of HID report.
        /// </summary>
        /// <param name="pPreparsedData">Pointer to a top-level collection's preparsed data.</param>
        /// <param name="Caps">Pointer to a caller-allocated buffer that the routine uses to return a collection's HIDP_CAPS structure</param>
        /// <param name="pButtonCaps">Pointer to a caller-allocated buffer that the routine uses to return a button capability array for the specified report type.</param>
        /// <returns>TRUE is success, otherwise FALSE</returns>
        private bool GetButtonCapabilities(IntPtr pPreparsedData, HidPCaps Caps, out HidPButtonCaps[] pButtonCaps)
        {
            ushort capsLength = Caps.NumberInputButtonCaps;
            pButtonCaps = new HidPButtonCaps[capsLength];
            if (Win32Api.HidP_GetButtonCaps(HidPReportType.Input, pButtonCaps, ref capsLength, pPreparsedData) != NtStatus.Success)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Retrieve a value capability array that describes all the HID control buttons in a top-level collection for a specified type of HID report.
        /// </summary>
        /// <param name="pPreparsedData">Pointer to a top-level collection's preparsed data.</param>
        /// <param name="Caps">Pointer to a caller-allocated buffer that the routine uses to return a collection's HIDP_CAPS structure</param>
        /// <param name="pButtonCaps">Pointer to a caller-allocated buffer that the routine uses to return a value capability array for the specified report type.</param>
        /// <returns>TRUE is success, otherwise FALSE</returns>
        private bool GetValueCapabilities(IntPtr pPreparsedData, HidPCaps Caps, out HidPValueCaps[] pValueCaps)
        {
            ushort capsLength = Caps.NumberInputValueCaps;
            pValueCaps = new HidPValueCaps[capsLength];
            if (Win32Api.HidP_GetValueCaps(HidPReportType.Input, pValueCaps, ref capsLength, pPreparsedData) != NtStatus.Success)
            {
                return false;
            }
            return true;
        }
        private bool GetOutputValueCapabilities(IntPtr pPreparsedData, HidPCaps Caps, out HidPValueCaps[] pValueCaps)
        {
            ushort capsLength = Caps.NumberOutputValueCaps;
            pValueCaps = new HidPValueCaps[capsLength];
            if (Win32Api.HidP_GetValueCaps(HidPReportType.Output, pValueCaps, ref capsLength, pPreparsedData) != NtStatus.Success)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Count the number of available buttons for this specific HID device
        /// </summary>
        /// <param name="pPreparsedData">Pointer to a previously parsed data structure</param>        
        /// <returns>Number of available input buttons for this HID device</returns>
        private int GetNumberOfHidDeviceButtons(IntPtr pPreparsedData, HidPButtonCaps[] pButtonCaps)
        {
            if (_pPreparsedData != IntPtr.Zero)
            {
                int nButtons = pButtonCaps[0].Range.UsageMax - pButtonCaps[0].Range.UsageMin + 1;
                return nButtons;
            }
            return 0;
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

            IntPtr p = Marshal.AllocHGlobal((int)size);
            result = Win32Api.GetRawInputData(hRawInput, RawInputUiCommand.RID_INPUT, p, ref size, HeaderSize);
            if (result != 0xFFFF && result == size)
            {
                IntPtr RawHidDataPtr = IntPtr.Add(p, (int)HeaderSize);
                _RawHidData = RawHid.FromIntPtr(RawHidDataPtr);
                Marshal.FreeHGlobal(p);

                //PARSE DATA
                SetButtonsState();
                SetAxisValue();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Set button state based on the RawInputFlag received
        /// If any button pressed, trigger the event
        /// </summary>
        protected override void SetButtonsState()
        {
            try
            {
                //HidP_GetUsages() will fill :
                // - usage[] with the list of ButtonNumber pressed (zero filled for other buttons)
                // - usageLength with the number of pressed buttons at the time of the repport
                // Usage[] array size will be Number of buttons, to be able to host all Buttons Number if they are all pressed at the same time..
                ushort[] Usage = new ushort[_Hid_NumberofButtons];
                uint UsageLength = (uint)_Hid_NumberofButtons;

                if (Win32Api.HidP_GetUsages(HidPReportType.Input, _pButtonCaps[0].UsagePage, 0, Usage, ref UsageLength, _pPreparsedData, _RawHidData.bRawData, (uint)_RawHidData.dwSizeHid) != NtStatus.Success)
                {
                    Logger.WriteLog("[" + this.GetType().Name + "] SetButtonsState() error: Impossible to get Usages for device " + _ManufacturerName + " " + _ProductName);
                    return;
                }

                //Raising AnyButton pressed for GUI 
                if (UsageLength > 0)
                {
                    AnyKeyPressedEventArgs e = new AnyKeyPressedEventArgs(Usage[0], "Button " + Usage[0].ToString());
                    RaiseAnyKeyPressedEvent(e);
                }

                //Filling buttons state for gameplay
                //Couple of ideas to speed process :
                // - Knowing the number of pressed buttons (UsageLength), we can stop searching for "Pressed" values in the Usage[] result once we reach the number of updated buttons
                // - If the Usage[] value is not at least UsageMin, chances are high that the value is "0" which means "not pressed"
                int PressedButtonsUpdatedCount = 0;
                foreach (KeyValuePair<int, RawInputButton> b in _Buttons)
                {
                    if (PressedButtonsUpdatedCount < UsageLength)
                    {
                        foreach (ushort PressedButtonId in Usage)
                        {
                            if (PressedButtonId >= _pButtonCaps[0].Range.UsageMin && PressedButtonId == b.Key)
                            {
                                b.Value.SetState(true);
                                PressedButtonsUpdatedCount++;
                                break;
                            }
                            else
                            {
                                b.Value.SetState(false);
                            }
                        }
                    }
                    else
                    {
                        b.Value.SetState(false);
                    }
                }
            }
            catch (Exception Ex)
            {
                Logger.WriteLog("[" + this.GetType().Name + "] SetButtonsState() Error : " + Ex.Message.ToString());
            }
        }

        /// <summary>
        /// Set Axis value
        /// For HID device, we need to look into available axes to select the ones we need and extract the data from it
        /// </summary>
        protected override void SetAxisValue()
        {
            try
            {
                RawInputAxis RiAxis = null;
                
                //Updating Values
                for (int i = 0; i < _pValueCaps.Length; i++)
                {
                    Int32 value;

                    if (!_pValueCaps[i].IsRange)
                    {
                        RiAxis = _Axes[_pValueCaps[i].NotRange.Usage];

                        if (Win32Api.HidP_GetUsageValue(HidPReportType.Input, _pValueCaps[i].UsagePage, 0, _pValueCaps[i].NotRange.Usage, out value, _pPreparsedData, _RawHidData.bRawData, (uint)_RawHidData.dwSizeHid) != NtStatus.Success)
                        {
                            Logger.WriteLog("[" + this.GetType().Name + "] SetAxisValue() error: Impossible to get UsageValue for device " + _ManufacturerName + " " + _ProductName);
                            return;
                        }
                    }
                    else
                    {
                        RiAxis = _Axes[_pValueCaps[i].Range.UsageMin];

                        if (Win32Api.HidP_GetUsageValue(HidPReportType.Input, _pValueCaps[i].UsagePage, 0, _pValueCaps[i].Range.UsageMin, out value, _pPreparsedData, _RawHidData.bRawData, (uint)_RawHidData.dwSizeHid) != NtStatus.Success)
                        {
                            Logger.WriteLog("[" + this.GetType().Name + "] SetAxisValue() error: Impossible to get UsageValue for device " + _ManufacturerName + " " + _ProductName);
                            return;
                        }
                    }
                    RiAxis.Value = CorrectNegative_Value(value, (int)RiAxis.MinValue);

                    if (SupportedRawInputAxis.ContainsKey(RiAxis.AxisId))
                    {
                        AnyAxisChangedEventArgs e = new AnyAxisChangedEventArgs(RiAxis.AxisId, RiAxis.Value);
                        RaiseAnyAxisChangedEvent(e);
                    }
                }
            }
            catch (Exception Ex)
            {
                Logger.WriteLog("[" + this.GetType().Name + "]  SetAxisValue() error: " + Ex.Message.ToString());
            }
        }

        /// <summary>
        /// Special case for Sony Dual SHock 3 or WiiMotes with XInput driver :
        /// The driver is returning 0xFFFFFFFF as LogicalMax (which is -1 for a signed long)
        /// whereas real data are between 0x00000000 and 0x0000FFFF            
        /// </summary>
        /// <param name="Max_Value"></param>
        private int Correct_Axis_Max(int Max_Value)
        {
            if (Max_Value == -1) //-1 = 0xFFFFFFFF
                return 0x0000FFFF;
            else
                return Max_Value;
        }

        /// <summary>
        /// Trying to resolve an issue for APAC or device with negative Xmin [-2048;2048]
        /// GetUsageValue here is returning a 16bits signed values (i.e 0x0000F800 for -2048) instead of a full 32bits (i.e 0xFFFFF800)
        /// As a consequence, the Xmin being 0xFFFFF800, the value is not good for later calculation.
        /// Not sure if it's my code which is faulty and I do not have a device like this to test myself.
        /// The following workaround will simply add missing 0xFFFF0000 bytes when it's needed            
        /// </summary>
        /// <param name="Value"></param>
        private int CorrectNegative_Value(int Value, int MinValue)
        {
            if (MinValue < 0)
            {
                if ((Int16)Value < 0)
                    return (int)((uint)Value | 0xFFFF0000);
            }

            return Value;
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
