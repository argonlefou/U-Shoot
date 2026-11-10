using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using UsCore.Logging;
using UsCore.RawInput.Win32;
using static UsCore.RawInput.Win32Api;

namespace UsCore.RawInput
{
    public class RawInputManager
    {
        private static RawInputManager _Instance;

        //A Message-Loop only window will be created to be able to receive WM_INPUT messages
        private IntPtr _RawMessageWnd_hWnd = IntPtr.Zero;
        private WndProc delegWndProc;
        private delegate IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        //Event handler for reception of a new WM_INPUT message
        public delegate void WmInputEventHandler(object sender, WmInputEventArgs e);
        public static event WmInputEventHandler OnRawInputMessage;
        public class WmInputEventArgs : EventArgs
        {
            public IntPtr hRawInput { get; private set; }
            public WmInputEventArgs(IntPtr LParam)
            {
                hRawInput = LParam;
            }
        }

        public List<RawInputController> RawInputControllers
        { get; private set; }

        public static RawInputManager GetInstance()
        {
            if (_Instance == null)
                _Instance = new RawInputManager();

            return _Instance;
        }

        public RawInputManager()
        {
            RawInputControllers = new List<RawInputController>();
        }

        public bool InitManager()
        {
            if (RawInputControllers.Count == 0)
            {
                try
                {
                    RawInputControllers = GetRawInputDevices(new RawInputDeviceType[] { RawInputDeviceType.RIM_TYPEHID, RawInputDeviceType.RIM_TYPEKEYBOARD, RawInputDeviceType.RIM_TYPEMOUSE });
                    Logger.WriteLog("[" + this.GetType().Name + "] Found " + RawInputControllers.Count + " RawInput Controllers");
                }
                catch (Exception ex)
                {
                    Logger.WriteLog("[" + this.GetType().Name + "] Error getting all RawInputDevices from the system.");
                    Logger.WriteLog(ex.Message.ToString());
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Enumerates available RawInput Devices and returned selected Types as an Array
        /// </summary>
        /// <param name="AccecptedDeviceType">Filter to select devices to keep based on their Type. Others won't be returned in the Array</param>
        /// <returns>Array of RawInputController from desired Type(s)</returns>
        private List<RawInputController> GetRawInputDevices(RawInputDeviceType[] AccecptedDeviceTypes)
        {
            uint deviceCount = 0;
            var dwSize = (Marshal.SizeOf(typeof(RawInputDeviceList)));
            List<RawInputController> Result = new List<RawInputController>();

            if (Win32Api.GetRawInputDeviceList(IntPtr.Zero, ref deviceCount, (uint)dwSize) == 0)
            {
                IntPtr pRawInputDeviceList = Marshal.AllocHGlobal((int)(dwSize * deviceCount));
                Win32Api.GetRawInputDeviceList(pRawInputDeviceList, ref deviceCount, (uint)dwSize);

                for (int i = 0; i < deviceCount; i++)
                {
                    // On Window 8 64bit when compiling against .Net > 3.5 using .ToInt32 you will generate an arithmetic overflow. Leave as it is for 32bit/64bit applications
                    RawInputDeviceList rid = (RawInputDeviceList)Marshal.PtrToStructure(new IntPtr((pRawInputDeviceList.ToInt64() + (dwSize * i))), typeof(RawInputDeviceList));

                    foreach (RawInputDeviceType Type in AccecptedDeviceTypes)
                    {
                        if (rid.dwType == Type)
                        {
                            switch (rid.dwType)
                            {
                                case RawInputDeviceType.RIM_TYPEMOUSE:
                                    {
                                        RawInput_Mouse Mouse = new RawInput_Mouse(rid.hDevice);
                                        Result.Add(Mouse);
                                    }
                                    break;
                                case RawInputDeviceType.RIM_TYPEKEYBOARD:
                                    {
                                        RawInput_Keyboard Kbd = new RawInput_Keyboard(rid.hDevice);
                                        Result.Add(Kbd);
                                    }
                                    break;
                                case RawInputDeviceType.RIM_TYPEHID:
                                    {
                                        RawInput_HidDevice Hid = new RawInput_HidDevice(rid.hDevice);
                                        Result.Add(Hid);
                                    }
                                    break;
                                default:
                                    {
                                        RawInputController controller = new RawInputController(rid.hDevice, rid.dwType);
                                        Result.Add(controller);
                                    }
                                    break;
                            }
                        }
                    }
                }
                Marshal.FreeHGlobal(pRawInputDeviceList);
            }
            return Result;
        }

        /// <summary>
        /// Register to RawInput thanks to the previously created window Handle
        /// </summary>
        public bool RegisterDevices()
        {
            if (_RawMessageWnd_hWnd == IntPtr.Zero)
            {
                if (!CreateRawMessageWindow())
                    return false;

                RawInputDevice[] rid = new RawInputDevice[4];
                rid[0].UsagePage = HidUsagePage.GENERIC;
                rid[0].Usage = HidUsage.Joystick;
                rid[0].dwFlags = RawInputDeviceFlags.RIDEV_INPUTSINK;
                rid[0].hwndTarget = _RawMessageWnd_hWnd;

                rid[1].UsagePage = HidUsagePage.GENERIC;
                rid[1].Usage = HidUsage.Mouse;
                rid[1].dwFlags = RawInputDeviceFlags.RIDEV_INPUTSINK;
                rid[1].hwndTarget = _RawMessageWnd_hWnd;

                rid[2].UsagePage = HidUsagePage.GENERIC;
                rid[2].Usage = HidUsage.Gamepad;
                rid[2].dwFlags = RawInputDeviceFlags.RIDEV_INPUTSINK;
                rid[2].hwndTarget = _RawMessageWnd_hWnd;

                rid[3].UsagePage = HidUsagePage.GENERIC;
                rid[3].Usage = HidUsage.Keyboard;
                rid[3].dwFlags = RawInputDeviceFlags.RIDEV_INPUTSINK;
                rid[3].hwndTarget = _RawMessageWnd_hWnd;

                if (!Win32Api.RegisterRawInputDevices(rid, (uint)rid.Length, (uint)Marshal.SizeOf(rid[0])))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Create a messageLoop-only Window (invsible) to treat WM_* messages
        /// </summary>
        /// <returns></returns>
        private bool CreateRawMessageWindow()
        {
            delegWndProc = myWndProc;

            WNDCLASSEX wind_class = new WNDCLASSEX
            {
                cbSize = Marshal.SizeOf(typeof(WNDCLASSEX)),
                style = 0,
                hbrBackground = IntPtr.Zero,
                cbClsExtra = 0,
                cbWndExtra = 0,
                hInstance = Marshal.GetHINSTANCE(this.GetType().Module)
            };
            ;// alternative: Process.GetCurrentProcess().Handle;
            wind_class.hIcon = IntPtr.Zero;
            wind_class.hCursor = IntPtr.Zero;
            wind_class.lpszMenuName = null;
            wind_class.lpszClassName = "RawInput_MsgLoop";
            wind_class.lpfnWndProc = Marshal.GetFunctionPointerForDelegate(delegWndProc);
            wind_class.hIconSm = IntPtr.Zero;
            ushort regResult = Win32Api.RegisterClassEx(ref wind_class);

            if (regResult == 0)
            {
                int error = Marshal.GetLastWin32Error();
                return false;
            }
            string wndClass = wind_class.lpszClassName;

            //This version worked and resulted in a non-zero hWnd
            //_hWnd = CreateWindowEx(0, regResult, "Hello Win32", WS_OVERLAPPEDWINDOW | WS_VISIBLE, 0, 0, 300, 400, IntPtr.Zero, IntPtr.Zero, wind_class.hInstance, IntPtr.Zero);

            IntPtr HWND_MESSAGE = new IntPtr(-3);
            _RawMessageWnd_hWnd = Win32Api.CreateWindowEx(0, regResult, "U-Shoot_RawInputWnd", 0, 0, 0, 0, 0, HWND_MESSAGE, IntPtr.Zero, wind_class.hInstance, IntPtr.Zero);


            if (_RawMessageWnd_hWnd == IntPtr.Zero)
            {
                int error = Marshal.GetLastWin32Error();
                return false;
            }
            return true;

            //The explicit message pump is not necessary, messages are obviously dispatched by the framework.
            //However, if the while loop is implemented, the functions are called... Windows mysteries...
            //MSG msg;
            //while (GetMessage(out msg, IntPtr.Zero, 0, 0) != 0)
            //{
            //    TranslateMessage(ref msg);
            //    DispatchMessage(ref msg);
            //}
        }

        /// <summary>
        /// Window Message-Loop
        /// </summary>
        private IntPtr myWndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            switch (msg)
            {
                case Win32Define.WM_INPUT:
                    {                        
                        if (OnRawInputMessage != null)
                            OnRawInputMessage(this, new WmInputEventArgs(lParam));

                        RawInputController c = GetControllerFromWmInputHandle(lParam);
                        if (c != null)
                        {
                            c.ProcessRawInputData(lParam);
                        }
                    }
                    break;
                case Win32Define.WM_QUIT:
                    {
                        Logger.WriteLog("[" + this.GetType().Name + "] WM_QUIT message received !");
                    }
                    break;
                default:
                    break;
            }
            return Win32Api.DefWindowProc(hWnd, msg, wParam, lParam);
        }

        /// <summary>
        /// Chech if a given DevicePath has the corresponding controller plugged in the sysyem
        /// </summary>
        /// <param name="Path">The text Device Path of the RawInputController to be found</param>
        /// <returns>TRUE if the corresponding RawInputController if found, otherwise, FALSE</returns>
        public bool IsControllerPlugged(string Path)
        {
            foreach (RawInputController c in RawInputControllers)
            {
                if (c.DevicePath == Path)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Get a RawInputController from the available list based on the hDevice we get in a RawInputHeader struct
        /// </summary>
        /// <param name="hRawInput">Handle to the RawInputDevice, usually in a RawInputHeader struct</param>
        /// <returns>The corresponding RawInputController if found, otherwise, NULL</returns>
        public RawInputController GetControllerFromDeviceHandle(IntPtr hDevice)
        {
            foreach (RawInputController c in RawInputControllers)
            {
                if (c.DeviceHandle == hDevice)
                    return c;
            }
            return null;
        }

        /// <summary>
        /// Get a RawInputController from the available list based on the hRawInput
        /// </summary>
        /// <param name="hRawInput">Handle to the RawInput struct, usually LParam parameter from WindowLoop during WM_INPUT message</param>
        /// <returns>The corresponding RawInputController if found, otherwise, NULL</returns>
        public RawInputController GetControllerFromWmInputHandle(IntPtr hRawInput)
        {
            RawInputHeader Header;
            if (GetRawInputHeader(hRawInput, out Header))
            {
                return GetControllerFromDeviceHandle(Header.hDevice);
            }
            return null;
        }

        /// <summary>
        /// Get a RawInputController from the available list based on the hRawInput
        /// </summary>
        /// <param name="Path">The text Device Path of the RawInputController to be found</param>
        /// <returns>The corresponding RawInputController if found, otherwise, NULL</returns>
        public RawInputController GetControllerFromDevicePath(string Path)
        {
            foreach (RawInputController c in RawInputControllers)
            {
                if (c.DevicePath == Path)
                    return c;
            }
            return null;
        }

        /// <summary>
        /// Retrieve the RawInputHeader struct from a WM_INPUT LParam
        /// </summary>
        /// <param name="hRawInput">Handle to the RawInput struct, usually LParam parameter from WindowLoop during WM_INPUT message</param>
        /// <returns>TRUE if success, otherwise, FALSE</returns>
        internal bool GetRawInputHeader(IntPtr hRawInput, out RawInputHeader riHeader)
        {
            uint HeaderSize = (uint)Marshal.SizeOf(typeof(RawInputHeader));
            if (Win32Api.GetRawInputData(hRawInput, RawInputUiCommand.RID_HEADER, out riHeader, ref HeaderSize, HeaderSize) != HeaderSize)
            {
                //Error
                return false;
            }
            return true;
        }

        /// <summary>
        /// Retrieve the DeviceName from a raw input device
        /// This name is also the filename used to open a File Handle with this device
        /// </summary>
        /// <param name="hDevice">A handle to the raw input device. This comes from the hDevice member of RAWINPUTHEADER or from GetRawInputDeviceList.</param>
        /// <returns>Device name as string</returns>
        internal string GetDevicePath(IntPtr hDevice)
        {
            uint pcbSize = 0;
            string DevicePath = string.Empty;

            Win32Api.GetRawInputDeviceInfo(hDevice, RawInputUiCommand.RIDI_DEVICENAME, IntPtr.Zero, ref pcbSize);
            if (pcbSize > 0)
            {
                IntPtr pData = Marshal.AllocHGlobal((int)pcbSize);
                Win32Api.GetRawInputDeviceInfo(hDevice, RawInputUiCommand.RIDI_DEVICENAME, pData, ref pcbSize);

                DevicePath = Marshal.PtrToStringAnsi(pData);
                Marshal.FreeHGlobal(pData);
            }
            return DevicePath;
        }

        /// <summary>
        /// Retrive RawInputDeviceInformation structure for the device
        /// </summary>
        /// <param name="hDevice">A handle to the raw input device. This comes from the hDevice member of RAWINPUTHEADER or from GetRawInputDeviceList.</param>
        /// <returns>RawInputDeviceInfo struct</returns>
        internal RawInputDeviceInfo GetDeviceInfo(IntPtr hDevice)
        {
            uint pcbSize = 0;
            RawInputDeviceInfo DeviceInfo = new RawInputDeviceInfo();
            Win32Api.GetRawInputDeviceInfo(hDevice, RawInputUiCommand.RIDI_DEVICEINFO, IntPtr.Zero, ref pcbSize);
            if (pcbSize > 0)
            {
                IntPtr pData = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(RawInputDeviceInfo)));
                Win32Api.GetRawInputDeviceInfo(hDevice, RawInputUiCommand.RIDI_DEVICEINFO, pData, ref pcbSize);
                DeviceInfo = (RawInputDeviceInfo)Marshal.PtrToStructure(pData, typeof(RawInputDeviceInfo));
                Marshal.FreeHGlobal(pData);
            }
            return DeviceInfo;
        }

        /// <summary>
        /// Read and store Manufacturer string and Product string from the device
        /// These values are read by opening a File Handle with the device
        /// </summary>
        internal string GetManufacturerString(string DevicePath)
        {
            string ManufacturerName = string.Empty;

            IntPtr DeviceFileHandle = Win32Api.CreateFile(DevicePath,
                                                        Win32Api.DesiredAccess.None,
                                                        Win32Api.ShareMode.Read | Win32Api.ShareMode.Write,
                                                        IntPtr.Zero,
                                                        Win32Api.CreateDisposition.OpenExisting,
                                                        0,
                                                        IntPtr.Zero);
            if (DeviceFileHandle != new IntPtr(-1))
            {
                byte[] buf = new byte[256];
                if (Win32Api.HidD_GetManufacturerString(DeviceFileHandle, buf, (uint)buf.Length))
                {
                    ManufacturerName = Encoding.Unicode.GetString(buf, 0, buf.Length);
                    if (ManufacturerName.Contains("\0"))
                        ManufacturerName = ManufacturerName.Substring(0, ManufacturerName.IndexOf("\0"));
                }
                Win32Api.CloseHandle(DeviceFileHandle);
            }

            return ManufacturerName;
        }

        /// <summary>
        /// Read and store Manufacturer string and Product string from the device
        /// These values are read by opening a File Handle with the device
        /// </summary>
        internal string GetProductString(string DevicePath)
        {
            string ProductName = string.Empty;

            IntPtr DeviceFileHandle = Win32Api.CreateFile(DevicePath,
                                                        Win32Api.DesiredAccess.None,
                                                        Win32Api.ShareMode.Read | Win32Api.ShareMode.Write,
                                                        IntPtr.Zero,
                                                        Win32Api.CreateDisposition.OpenExisting,
                                                        0,
                                                        IntPtr.Zero);
            if (DeviceFileHandle != new IntPtr(-1))
            {
                byte[] buf = new byte[256];
                if (Win32Api.HidD_GetProductString(DeviceFileHandle, buf, (uint)buf.Length))
                {
                    ProductName = Encoding.Unicode.GetString(buf, 0, buf.Length);
                    if (ProductName.Contains("\0"))
                    {
                        ProductName = ProductName.Substring(0, ProductName.IndexOf("\0"));
                    }
                }

                Win32Api.CloseHandle(DeviceFileHandle);
            }

            return ProductName;
        }

        /// <summary>
        /// Alternative way to get Device strings, based on the driver
        /// 
        /// </summary>
        public bool FillDeviceInfo(string DevicePath, out string DeviceId, out string FriendlyName, out string Manufacturer)
        {
            DeviceId = null;
            FriendlyName = null;
            Manufacturer = null;

            DEVPROPKEY DEVPKEY_Device_InstanceId = new DEVPROPKEY
            {
                fmtid = new Guid(0x78c34fc8, 0x104a, 0x4aca, 0x9e, 0xa4, 0x52, 0x4d, 0x52, 0x99, 0x6e, 0x57),
                pid = 256
            };

            DEVPROPKEY DEVPKEY_NAME = new DEVPROPKEY
            {
                fmtid = new Guid(0xb725f130, 0x47ef, 0x101a, 0xa5, 0xf1, 0x02, 0x60, 0x8c, 0x9e, 0xeb, 0xac),
                pid = 10
            };

            DEVPROPKEY DEVPKEY_Device_Manufacturer = new DEVPROPKEY
            {
                fmtid = new Guid(0xa45c254e, 0xdf1c, 0x4efd, 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0),
                pid = 13
            };

            uint propertyType;
            uint propertySize = 0;
            uint cr = Win32Api.CM_Get_Device_Interface_PropertyW(
                DevicePath,
                ref DEVPKEY_Device_InstanceId,
                out propertyType,
                IntPtr.Zero,
                ref propertySize,
                0);

            if (cr != Win32Define.CR_BUFFER_SMALL)
                return false;

            byte[] deviceIdBuffer = new byte[propertySize];
            cr = Win32Api.CM_Get_Device_Interface_PropertyW(
                DevicePath,
                ref DEVPKEY_Device_InstanceId,
                out propertyType,
                deviceIdBuffer,
                ref propertySize,
                0);

            if (cr != Win32Define.CR_SUCCESS)
                return false;

            DeviceId = Encoding.Unicode.GetString(deviceIdBuffer).TrimEnd('\0');

            // Exemple: "HID\VID_203A&PID_FFFC&MI_01\7&2de99099&0&0000"

            uint devInst;
            cr = Win32Api.CM_Locate_DevNodeW(out devInst, DeviceId, Win32Define.CM_LOCATE_DEVNODE_NORMAL);

            if (cr != Win32Define.CR_SUCCESS)
                return false;

            //Friendly Name
            propertySize = 0;
            cr = Win32Api.CM_Get_DevNode_PropertyW(
                devInst,
                ref DEVPKEY_NAME,
                out propertyType,
                IntPtr.Zero,
                ref propertySize,
                0);

            if (cr != Win32Define.CR_BUFFER_SMALL)
                return false;

            byte[] friendlyStringBuffer = new byte[propertySize];
            cr = Win32Api.CM_Get_DevNode_PropertyW(
                devInst,
                ref DEVPKEY_NAME,
                out propertyType,
                friendlyStringBuffer,
                ref propertySize,
                0);

            if (cr == Win32Define.CR_SUCCESS)
                FriendlyName = Encoding.Unicode.GetString(friendlyStringBuffer).TrimEnd('\0');

            //Manufacturer Name
            propertySize = 0;
            cr = Win32Api.CM_Get_DevNode_PropertyW(
                devInst,
                ref DEVPKEY_Device_Manufacturer,
                out propertyType,
                IntPtr.Zero,
                ref propertySize,
                0);

            if (cr != Win32Define.CR_BUFFER_SMALL)
                return false;

            byte[] manufacturerBuffer = new byte[propertySize];
            cr = Win32Api.CM_Get_DevNode_PropertyW(
                devInst,
                ref DEVPKEY_Device_Manufacturer,
                out propertyType,
                manufacturerBuffer,
                ref propertySize,
                0);

            if (cr == Win32Define.CR_SUCCESS)
                Manufacturer = Encoding.Unicode.GetString(manufacturerBuffer).TrimEnd('\0');

            return true;
        }
    }
}
