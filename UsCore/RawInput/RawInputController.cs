using System;
using System.Collections.Generic;
using UsCore.Logging;
using static UsCore.RawInput.RawInputAxis;

namespace UsCore.RawInput
{

    public class RawInputController
    {
        #region Events

        public delegate void AnyKeyPressedEventHandler(object sender, AnyKeyPressedEventArgs e);
        public event AnyKeyPressedEventHandler OnAnyKeyPressed;

        //Create a different event for HID controller to be used in GUI control detections 
        //With fill be triggered with a deadzone in the center to not auto-trigger OnAxisChanged with small changes
        //Keeping the original event for In-Game where we want full precision
        public delegate void AnyAxisChangedEventHandler(object sender, AnyAxisChangedEventArgs e);
        public event AnyAxisChangedEventHandler OnAnyAxisChanged_WithDeadzone;

        public class AnyKeyPressedEventArgs : EventArgs
        {
            public int KeyId { get; private set; }
            public string KeyName { get; private set; }

            public AnyKeyPressedEventArgs(int KeyId, string KeyName)
            {
                this.KeyId = KeyId;
                this.KeyName = KeyName;
            }
        }
        public class AnyAxisChangedEventArgs : EventArgs
        {
            public int Value { get; private set; }
            public int AxisId { get; private set; }

            public AnyAxisChangedEventArgs(int AxisId, int Value)
            {
                this.AxisId = AxisId;
                this.Value = Value;
            }
        }


        protected void RaiseAnyKeyPressedEvent(AnyKeyPressedEventArgs e)
        {
            if (OnAnyKeyPressed != null)
            {
                OnAnyKeyPressed(this, e);
            }
        }

        protected void RaiseAnyAxisChangedEvent(AnyAxisChangedEventArgs e)
        {
            if (OnAnyAxisChanged_WithDeadzone != null)
            {
                OnAnyAxisChanged_WithDeadzone(this, e);
            }
        }

        #endregion

        protected const string _Default_ManufacturerName = "[Unknown Manufacturer]";
        protected string _Default_ProductName = "[Unknown Product]";

        protected IntPtr _hDevice;
        protected RawInputDeviceType _dwType;
        protected string _DevicePath;
        internal RawInputDeviceInfo _DeviceInfo;
        protected string _ManufacturerName;
        protected string _ProductName;
        #region Accessors

        public IntPtr DeviceHandle
        {
            get { return _hDevice; }
        }

        public RawInputDeviceType DeviceType
        {
            get { return _dwType; }
        }

        public string DevicePath
        {
            get { return _DevicePath; }
        }

        public String PID
        {
            get
            {
                if (_dwType == RawInputDeviceType.RIM_TYPEHID)
                {
                    return "0x" + _DeviceInfo.hid.dwProductId.ToString("X4");
                }
                else if (_DevicePath.Contains("PID_"))
                {
                    return "0x" + _DevicePath.Substring(_DevicePath.IndexOf("PID_") + 4, 4);
                }
                else return "";
            }
        }

        public String VID
        {
            get
            {
                if (_dwType == RawInputDeviceType.RIM_TYPEHID)
                {
                    return "0x" + _DeviceInfo.hid.dwVendorId.ToString("X4");
                }
                else if (_DevicePath.Contains("VID_"))
                {
                    return "0x" + _DevicePath.Substring(_DevicePath.IndexOf("VID_") + 4, 4);
                }
                else return "";
            }
        }

        public String ManufacturerName
        {
            get { return _ManufacturerName; }
        }

        public String ProductName
        {
            get { return _ProductName; }
        }

        public string ComputedDeviceName
        {
            get
            {
                if (_ProductName.Equals(string.Empty))
                    return _ManufacturerName;
                return _ProductName;
            }
        }

        #endregion

        //Common struct used for any type of devices
        internal RawInputHeader _RawInputHeader;
        protected Dictionary<int, RawInputButton> _Buttons;
        protected Dictionary<int, RawInputAxis> _Axes;
        #region Accessors

        public Dictionary<int, RawInputButton> Buttons
        { get { return _Buttons; } }

        public Dictionary<int, RawInputAxis> Axes
        { get { return _Axes; } }

        #endregion


        //Adding some field to know if the device is "relative" (i.e normal mouse) or "Absolute" (i.e lightgun)
        public virtual bool IsAbsoluteAxis
        {
            get { return false; }
        }

        //Adding some field to know the total amount of button in the device
        public virtual int NumberOfButtons
        {
            get { return 0; }
        }

        public Dictionary<int, string> SupportedRawInputAxis
        { get; protected set; }
        

        /// <summary>
        /// Constructor, fill as much Hidp_Struct as we can at the creation
        /// </summary>
        /// <param name="DeviceHandle">A handle to the raw input device.</param>
        /// <param name="DeviceType">The type of the raw input device</param>
        /// <param name="Caller">Caller class so that we can send data back for WriteLogDebug</param>
        public RawInputController(IntPtr DeviceHandle, RawInputDeviceType DeviceType)
        {
            _hDevice = DeviceHandle;
            _dwType = DeviceType;
            _DevicePath = String.Empty;
            _ManufacturerName = String.Empty;
            _ProductName = String.Empty;
            _Buttons = new Dictionary<int, RawInputButton>();
            _Axes = new Dictionary<int, RawInputAxis>();
            

            GetDevicePath();
            GetDeviceInfo();

            //DeviceName is also the path to acces the device with OpenFile
            FindManufacturerAndProductString();
        }

        /// <summary>
        /// Parse data from the WM_INPUT message to extact axis and button values
        /// </summary>
        /// <param name="hRawInput">LParam parameter from WindowLoop during WM_INPUT message</param>
        public void ProcessRawInputData(IntPtr hRawInput)
        {
            if (RawInputManager.GetInstance().GetRawInputHeader(hRawInput, out _RawInputHeader))
            {
                if (!GetRawInputData(hRawInput))
                    Logger.WriteLog("[" + this.GetType().Name + "] ProcessRawInputData() Error : Impossible to get RawInputData");
            }
            else
            {
                Logger.WriteLog("[" + this.GetType().Name + "] ProcessRawInputData() Error : Impossible to get RawInputHeader");
            }
        }       

        /// <summary>
        /// Retrieve the DeviceName from a raw input device
        /// This name is also the filename used to open a File Handle with this device
        /// </summary>
        private void GetDevicePath()
        {
            _DevicePath = RawInputManager.GetInstance().GetDevicePath(_hDevice);
        }

        /// <summary>
        /// Retrive RawInputDeviceInformation structure for the device
        /// </summary>
        private void GetDeviceInfo()
        {
            _DeviceInfo = RawInputManager.GetInstance().GetDeviceInfo(_hDevice);
        }

        /// <summary>
        /// Read and store Manufacturer string and Product string from the device
        /// These values are read by opening a File Handle with the device
        /// </summary>
        private void FindManufacturerAndProductString()
        {
            string DeviceId, Manufacturer2, Product2;
            RawInputManager.GetInstance().FillDeviceInfo(DevicePath, out DeviceId, out Manufacturer2, out Product2);

            _ManufacturerName = RawInputManager.GetInstance().GetManufacturerString(_DevicePath);
            if (_ManufacturerName.Equals(string.Empty))
            {
                if (Manufacturer2.Equals(string.Empty))
                    _ManufacturerName = _Default_ManufacturerName;
                else
                    _ManufacturerName = Manufacturer2;
            }

            _ProductName = RawInputManager.GetInstance().GetProductString(_DevicePath);
            //if (_ProductName.Equals(string.Empty))
            //{
            //    if (Product2.Equals(string.Empty))
            //        _ProductName = _Default_ProductName;
            //    else
            //        _ProductName = Product2;
            //}
        }

        /// <summary>
        /// Retrieving the raw input data struct when a WM_INPUT message has arrived
        /// For Mouse and keyboard we can directly fill the RawInput struct
        /// For HID we need to get RawHeader and RawHid separatly because RawHid is not constant size
        /// </summary>
        /// <param name="hRawInput">LParam parameter from WindowLoop during WM_INPUT message</param>
        /// <returns></returns>
        protected virtual bool GetRawInputData(IntPtr hRawInput)
        {
            return false;
        }

        /// <summary>
        /// Add RawInputButton to listen to, and be able to attach listener to it
        /// </summary>
        /// <param name="KeyCode"></param>
        public void RegisterButton(int KeyCode)
        {
            if (!_Buttons.ContainsKey(KeyCode))
            {
                RawInputButton b = new RawInputButton();
                _Buttons.Add(KeyCode, b);
            }
        }

        /// <summary>
        /// Add RawAxis to listen to, and be able to attach listener to it
        /// </summary>
        public void RegisterAxis(int AxisId, long MinValue, long MaxValue)
        {
            if (!_Axes.ContainsKey(AxisId))
            {
                RawInputAxis a = new RawInputAxis(AxisId, MinValue, MaxValue);
                _Axes.Add(AxisId, a);
            }
        }

        /// <summary>
        /// When parsing new RawInputData, that procedure will trigger AnyButtonChanged events if needed
        /// </summary>
        protected virtual void SetButtonsState()
        { }

        /// <summary>
        /// When parsing new RawInputData, that procedure will trigger OnAxisChanged events if needed
        /// </summary>
        protected virtual void SetAxisValue()
        { }

        public virtual RawInputButton GetButton(int ButtonId)
        { return null; }

        public virtual RawInputAxis GetAxis(int AxisId)
        { 
            return _Axes[AxisId]; 
        }

    }

}
