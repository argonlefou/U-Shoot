using System;
using System.Runtime.InteropServices;
using System.Text;
using UsCore.RawInput.Win32;

namespace UsCore.RawInput
{
    internal class Win32Api
    {
        #region cfgmgr32

        [DllImport("cfgmgr32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern uint CM_Get_Device_Interface_PropertyW(
            string pszDeviceInterface,
            ref DEVPROPKEY PropertyKey,
            out uint PropertyType,
            IntPtr PropertyBuffer,
            ref uint PropertyBufferSize,
            uint ulFlags);

        [DllImport("cfgmgr32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern uint CM_Get_Device_Interface_PropertyW(
            string pszDeviceInterface,
            ref DEVPROPKEY PropertyKey,
            out uint PropertyType,
            byte[] PropertyBuffer,
            ref uint PropertyBufferSize,
            uint ulFlags);

        [DllImport("cfgmgr32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern uint CM_Locate_DevNodeW(
            out uint pdnDevInst,
            string pDeviceID,
            uint ulFlags);

        [DllImport("cfgmgr32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern uint CM_Get_DevNode_PropertyW(
            uint dnDevInst,
            ref DEVPROPKEY PropertyKey,
            out uint PropertyType,
            IntPtr PropertyBuffer,
            ref uint PropertyBufferSize,
            uint ulFlags);

        [DllImport("cfgmgr32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern uint CM_Get_DevNode_PropertyW(
            uint dnDevInst,
            ref DEVPROPKEY PropertyKey,
            out uint PropertyType,
            byte[] PropertyBuffer,
            ref uint PropertyBufferSize,
            uint ulFlags);

        // Structures
        [StructLayout(LayoutKind.Sequential)]
        public struct DEVPROPKEY
        {
            public Guid fmtid;
            public uint pid;
        }

        #endregion

        #region hid.dll

        [DllImport("hid", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.U1)]
        public static extern bool HidD_GetManufacturerString(IntPtr HidDeviceObject, [Out] byte[] Buffer, uint BufferLength);

        [DllImport("hid", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.U1)]
        public static extern bool HidD_GetProductString(IntPtr HidDeviceObject, [Out] byte[] Buffer, uint BufferLength);

        [DllImport("hid.dll", SetLastError = true)]
        public static extern NtStatus HidP_GetCaps(IntPtr pPreparsedData, out HidPCaps Capabilities);

        [DllImport("hid.dll", SetLastError = true)]
        public static extern NtStatus HidP_GetButtonCaps(HidPReportType ReportType, [Out] HidPButtonCaps[] ButtonCaps, ref ushort ButtonCapsLength, IntPtr pPreparsedData);

        [DllImport("hid.dll", CharSet = CharSet.Auto)]
        public static extern NtStatus HidP_GetValueCaps(HidPReportType ReportType, [Out] HidPValueCaps[] Values, ref ushort ValueCapsLength, IntPtr pPreparsedData);

        [DllImport("hid")]
        public static extern NtStatus HidP_GetUsages(HidPReportType ReportType, ushort UsagePage, ushort LinkCollection, [Out] ushort[] UsageList, ref uint UsageLength, IntPtr pPreparsedData, byte[] Report, uint ReportLength);

        [DllImport("hid")]
        //public static extern NtStatus HidP_GetUsageValue(HidPReportType ReportType, ushort UsagePage, ushort LinkCollection, ushort Usage, out long UsageValue, IntPtr pPreparsedData, byte[] Report, uint ReportLength);
        public static extern NtStatus HidP_GetUsageValue(HidPReportType ReportType, ushort UsagePage, ushort LinkCollection, ushort Usage, out int UsageValue, IntPtr pPreparsedData, byte[] Report, uint ReportLength);

        [DllImport("hid")]
        public static extern NtStatus HidP_GetScaledUsageValue(HidPReportType ReportType, ushort UsagePage, ushort LinkCollection, ushort Usage, out int UsageValue, IntPtr pPreparsedData, byte[] Report, uint ReportLength);

        #endregion

        #region kernel32.dll

        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr CreateFile(string lpFileName, DesiredAccess dwDesiredAccess, ShareMode dwShareMode, IntPtr lpSecurityAttributes, CreateDisposition dwCreationDisposition, uint dwFlagsAndAttributes, IntPtr hTemplateFile);

        [DllImport("kernel32", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr hObject);

        [Flags]
        public enum DesiredAccess : uint
        {
            None,
            Write = 0x40000000,
            Read = 0x80000000
        }

        [Flags]
        public enum ShareMode : uint
        {
            None,
            Read = 0x00000001,
            Write = 0x00000002,
            Delete = 0x00000004
        }

        public enum CreateDisposition : uint
        {
            CreateNew = 1,
            CreateAlways,
            OpenExisting,
            OpenAlways,
            TruncateExisting
        }

        #endregion

        #region user32.dll

        [DllImport("user32.dll", SetLastError = true, EntryPoint = "CreateWindowEx")]
        public static extern IntPtr CreateWindowEx(
           int dwExStyle,
           UInt16 regResult,
           //string lpClassName,
           string lpWindowName,
           UInt32 dwStyle,
           int x,
           int y,
           int nWidth,
           int nHeight,
           IntPtr hWndParent,
           IntPtr hMenu,
           IntPtr hInstance,
           IntPtr lpParam);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr DefWindowProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int GetKeyNameText(int lParam, StringBuilder lpString, int nSize);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetRawInputData(IntPtr hRawInput, RawInputUiCommand uiCommand, IntPtr pData, ref uint pcbSize, uint cbSizeHeader);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetRawInputData(IntPtr hRawInput, RawInputUiCommand uiCommand, out RawInputHeader pData, ref uint pcbSize, uint cbSizeHeader);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetRawInputData(IntPtr hRawInput, RawInputUiCommand uiCommand, out RawInputDataMouse pData, ref uint pcbSize, uint cbSizeHeader);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetRawInputData(IntPtr hRawInput, RawInputUiCommand uiCommand, out RawInputDataKeyboard pData, ref uint pcbSize, uint cbSizeHeader);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetRawInputDeviceInfo(IntPtr hDevice, RawInputUiCommand uiCommand, IntPtr pData, ref uint dataSize);

        [DllImport("user32", SetLastError = true)]
        public static extern uint GetRawInputDeviceInfo(IntPtr hDevice, RawInputUiCommand uiCommand, out RawInputDeviceInfo pData, uint pcbSize);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetRawInputDeviceList(IntPtr pRawInputDeviceList, ref uint NumberDevices, uint Size);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint MapVirtualKey(uint uCode, uint uMapType);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool RegisterRawInputDevices(RawInputDevice[] pRawInputDevice, uint NumberDevices, uint Size);

        [DllImport("user32.dll", SetLastError = true, EntryPoint = "RegisterClassEx")]
        public static extern UInt16 RegisterClassEx([In] ref WNDCLASSEX lpWndClass);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        internal struct WNDCLASSEX
        {
            [MarshalAs(UnmanagedType.U4)]
            public int cbSize;
            [MarshalAs(UnmanagedType.U4)]
            public int style;
            public IntPtr lpfnWndProc;
            public int cbClsExtra;
            public int cbWndExtra;
            public IntPtr hInstance;
            public IntPtr hIcon;
            public IntPtr hCursor;
            public IntPtr hbrBackground;
            public string lpszMenuName;
            public string lpszClassName;
            public IntPtr hIconSm;
        }

        #endregion

    }

    internal class Win32Define
    {
        public const ushort KEYBOARD_OVERRUN_MAKE_CODE = 0xFF;
        public const int KF_EXTENDED = 0x0100;
        public const int MAPVK_VK_TO_VSC_EX = 4;

        //Mouse and Keyboard Hook
        public const int WH_MOUSE_LL = 14;
        public const int WH_KEYBOARD_LL = 13;

        //Windows messages
        public const UInt32 WM_QUIT = 0x0012;
        public const UInt32 WM_COPYDATA = 0x004A;
        public const UInt32 WM_INPUT = 0x00FF;
        public const UInt32 WM_KEYDOWN = 0x0100;
        public const UInt32 WM_KEYUP = 0x0101;
        public const UInt32 WM_SYSKEYDOWN = 0x0104;
        public const UInt32 WM_MOUSEMOVE = 0x0200;
        public const UInt32 WM_LBUTTONDOWN = 0x0201;
        public const UInt32 WM_LBUTTONUP = 0x0202;
        public const UInt32 WM_RBUTTONDOWN = 0x0204;
        public const UInt32 WM_RBUTTONUP = 0x0205;
        public const UInt32 WM_MBUTTONDOWN = 0x0207;
        public const UInt32 WM_MBUTTONUP = 0x0208;
        public const UInt32 WM_MOUSEWHEEL = 0x020A;
        public const UInt32 WM_USB_DEVICECHANGE = 0x0219;
        public const UInt32 WM_APP = 0x8000;

        //Mapped Memory File
        public const UInt32 ERROR_ALREADY_EXISTS = 183;
        public const Int32 INVALID_HANDLE_VALUE = -1;

        public const UInt32 STANDARD_RIGHTS_REQUIRED = 0x000F0000;
        public const UInt32 SECTION_QUERY = 0x0001;
        public const UInt32 SECTION_MAP_WRITE = 0x0002;
        public const UInt32 SECTION_MAP_READ = 0x0004;
        public const UInt32 SECTION_MAP_EXECUTE = 0x0008;
        public const UInt32 SECTION_EXTEND_SIZE = 0x0010;
        public const UInt32 SECTION_ALL_ACCESS = (STANDARD_RIGHTS_REQUIRED | SECTION_QUERY |
            SECTION_MAP_WRITE |
            SECTION_MAP_READ |
            SECTION_MAP_EXECUTE |
            SECTION_EXTEND_SIZE);
        public const UInt32 FILE_MAP_ALL_ACCESS = SECTION_ALL_ACCESS;

        //Cfgmgr32
        //Return values
        public const uint CR_SUCCESS = 0;
        public const uint CR_BUFFER_SMALL = 26;

        //CM_Locate_DevNode Flags
        public const uint CM_LOCATE_DEVNODE_NORMAL = 0;
    }

}
