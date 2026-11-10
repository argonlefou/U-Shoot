using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using UsCore.Config;
using UsCore.Launcher;
using UsCore.Logging;
using UsCore.MameOutput;
using UsCore.Memory;
using UsCore.RawInput;
using UsCore.VirtualControllers;

namespace U_Shoot.Games
{
    public class BaseGame
    {
        protected virtual string GAMEDATA_FOLDER { get { return string.Empty; } }
        protected virtual bool PATCH_PEB_DEBUG_FLAG { get { return true; } }

        #region WIN32 Interrop

        internal class Win32
        {
            #region API

            [DllImport("kernel32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool CloseHandle(IntPtr hObject);

            [DllImport("user32.dll", SetLastError = true)]
            public static extern IntPtr CreateCursor(IntPtr hInst, int xHotSpot, int yHotSpot, int nWidth, int nHeight, byte[] pvANDPlane, byte[] pvXORPlane);

            [DllImport("user32.dll", SetLastError = true)]
            public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
            public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern bool FlushInstructionCache(IntPtr hProcess, IntPtr lpBaseAddress, UIntPtr dwSize);

            [DllImport("user32.dll", SetLastError = true)]
            public static extern bool GetClientRect(IntPtr hWnd, ref Rect rectangle);

            [DllImport("user32.dll", SetLastError = true)]
            public static extern IntPtr GetDC(IntPtr hWnd);

            [DllImport("user32.dll", SetLastError = true)]
            public static extern IntPtr GetDesktopWindow();

            [DllImport("user32.dll", SetLastError = true)]
            public static extern bool GetCursorPos(out POINT point);

            [DllImport("gdi32.dll", SetLastError = true)]
            public static extern int GetDeviceCaps(IntPtr hdc, int nIndex);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern void GetSystemInfo(ref SYSTEM_INFO Info);

            [DllImport("user32.dll")]
            public static extern int GetSystemMetrics(SystemMetricsIndex smIndex);

            [DllImport("user32.dll", SetLastError = true)]
            public static extern bool GetWindowRect(IntPtr hWnd, ref Rect rectangle);

            [DllImport("user32.dll", SetLastError = true)]
            public static extern int GetWindowText(IntPtr Hwnd, StringBuilder lpString, int nMaxCount);

            [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
            public static extern int GetWindowTextLength(IntPtr hWnd);

            [DllImport("user32.dll", SetLastError = true)]
            public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

            [DllImport("user32.dll", SetLastError = true)]
            public static extern bool IsWindowVisible(IntPtr hWnd);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

            [DllImport("kernel32.dll", SetLastError = true)]
            static public extern IntPtr OpenThread(ThreadAccess dwDesiredAccess, bool bInheritHandle, uint dwThreadId);

            [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
            public static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

            [DllImport("kernel32.dll", SetLastError = true, EntryPoint = "ReadProcessMemory")]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool ReadProcessMemory([In] IntPtr hProcess, [In] IntPtr lpBaseAddress, [Out] Byte[] lpBuffer, [In] UIntPtr nSize, [Out] out UIntPtr lpNumberOfBytesRead);

            [DllImport("user32.dll", SetLastError = true)]
            public static extern bool ScreenToClient(IntPtr hWnd, ref POINT lpPoint);

            [DllImport("shell32.dll", SetLastError = true)]
            public static extern int SHQueryUserNotificationState(out QUERY_USER_NOTIFICATION_STATE pquns);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern int VirtualQueryEx(IntPtr hProcess, IntPtr lpAddress, out MEMORY_BASIC_INFORMATION lpBuffer, uint dwLength);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern bool VirtualProtectEx(IntPtr hProcess, IntPtr lpAddress, UIntPtr dwSize, uint flNewProtect, out uint lpflOldProtect);            

            [DllImport("kernel32.dll", SetLastError = true, EntryPoint = "WriteProcessMemory")]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool WriteProcessMemory([In] IntPtr hProcess, [In] IntPtr lpBaseAddress, [Out] Byte[] lpBuffer, [In] UIntPtr nSize, [Out] out UIntPtr lpNumberOfBytesWritten);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern bool QueryPerformanceFrequency(out long lpFrequency);

            [DllImport("gdi32.dll", SetLastError = false)]
            public static extern IntPtr GetStockObject(int fnObject);

            [DllImport("user32.dll")]
            public static extern uint MapVirtualKeyEx(uint uCode, uint uMapType, IntPtr dwhkl);

            [DllImport("ntdll.dll", SetLastError = true)]
            public static extern int NtQueryInformationProcess(IntPtr processHandle, int processInformationClass, IntPtr processInformation, uint processInformationLength, IntPtr returnLength);

            #endregion

            #region Types

            /// <summary>
            /// The RECT structure defines a rectangle by the coordinates of its upper-left and lower-right corners.
            /// </summary>
            internal struct Rect
            {
                /// <summary>
                /// Specifies the x-coordinate of the upper-left corner of the rectangle.
                /// </summary>
                public int Left { get; set; }
                /// <summary>
                /// Specifies the y-coordinate of the upper-left corner of the rectangle.
                /// </summary>
                public int Top { get; set; }
                /// <summary>
                /// Specifies the x-coordinate of the lower-right corner of the rectangle.
                /// </summary>
                public int Right { get; set; }
                /// <summary>
                /// Specifies the y-coordinate of the lower-right corner of the rectangle.
                /// </summary>
                public int Bottom { get; set; }
            }

            /// <summary>
            /// The POINT structure defines the x- and y-coordinates of a point.
            /// https://docs.microsoft.com/en-us/windows/win32/api/windef/ns-windef-point
            /// </summary>
            [StructLayout(LayoutKind.Sequential)]
            internal struct POINT
            {
                public int X;
                public int Y;

                public POINT(int x, int y)
                {
                    this.X = x;
                    this.Y = y;
                }
            }

            internal enum QUERY_USER_NOTIFICATION_STATE
            {
                QUNS_NOT_PRESENT = 1,
                QUNS_BUSY = 2,
                QUNS_RUNNING_D3D_FULL_SCREEN = 3,
                QUNS_PRESENTATION_MODE = 4,
                QUNS_ACCEPTS_NOTIFICATIONS = 5,
                QUNS_QUIET_TIME = 6,
                QUNS_APP = 7
            };

            /// <summary>
            /// The system metric or configuration setting to be retrieved by GetsystemMetrics().
            /// https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getsystemmetrics
            /// </summary>
            internal enum SystemMetricsIndex : int
            {
                SM_ARRANGE = 56,
                SM_CLEANBOOT = 67,
                SM_CMONITORS = 80,
                SM_CMOUSEBUTTONS = 43,
                SM_CONVERTIBLESLATEMODE = 0x2003,
                SM_CXBORDER = 5,
                SM_CXCURSOR = 13,
                SM_CXDLGFRAME = 7,
                SM_CXDOUBLECLK = 36,
                SM_CXDRAG = 68,
                SM_CXEDGE = 45,
                SM_CXFIXEDFRAME = 7,
                SM_CXFOCUSBORDER = 83,
                SM_CXFRAME = 32,
                SM_CXFULLSCREEN = 16,
                SM_CXHSCROLL = 21,
                SM_CXHTHUMB = 10,
                SM_CXICON = 11,
                SM_CXICONSPACING = 38,
                SM_CXMAXIMIZED = 61,
                SM_CXMAXTRACK = 59,
                SM_CXMENUCHECK = 71,
                SM_CXMENUSIZE = 54,
                SM_CXMIN = 28,
                SM_CXMINIMIZED = 57,
                SM_CXMINSPACING = 47,
                SM_CXMINTRACK = 34,
                SM_CXPADDEDBORDER = 92,
                SM_CXSCREEN = 0,
                SM_CXSIZE = 30,
                SM_CXSIZEFRAME = 32,
                SM_CXSMICON = 49,
                SM_CXSMSIZE = 52,
                SM_CXVIRTUALSCREEN = 78,
                SM_CXVSCROLL = 2,
                SM_CYBORDER = 6,
                SM_CYCAPTION = 4,
                SM_CYCURSOR = 14,
                SM_CYDLGFRAME = 8,
                SM_CYDOUBLECLK = 37,
                SM_CYDRAG = 69,
                SM_CYEDGE = 46,
                SM_CYFIXEDFRAME = 8,
                SM_CYFOCUSBORDER = 84,
                SM_CYFRAME = 33,
                SM_CYFULLSCREEN = 17,
                SM_CYHSCROLL = 3,
                SM_CYICON = 12,
                SM_CYICONSPACING = 39,
                SM_CYKANJIWINDOW = 18,
                SM_CYMAXIMIZED = 62,
                SM_CYMAXTRACK = 60,
                SM_CYMENU = 15,
                SM_CYMENUCHECK = 72,
                SM_CYMENUSIZE = 55,
                SM_CYMIN = 29,
                SM_CYMINIMIZED = 58,
                SM_CYMINSPACING = 48,
                SM_CYMINTRACK = 35,
                SM_CYSCREEN = 1,
                SM_CYSIZE = 31,
                SM_CYSIZEFRAME = 33,
                SM_CYSMCAPTION = 51,
                SM_CYSMICON = 50,
                SM_CYSMSIZE = 53,
                SM_CYVIRTUALSCREEN = 79,
                SM_CYVSCROLL = 20,
                SM_CYVTHUMB = 9,
                SM_DBCSENABLED = 42,
                SM_DEBUG = 22,
                SM_DIGITIZER = 94,
                SM_IMMENABLED = 82,
                SM_MAXIMUMTOUCHES = 95,
                SM_MEDIACENTER = 87,
                SM_MENUDROPALIGNMENT = 40,
                SM_MIDEASTENABLED = 74,
                SM_MOUSEPRESENT = 19,
                SM_MOUSEHORIZONTALWHEELPRESENT = 91,
                SM_MOUSEWHEELPRESENT = 75,
                SM_NETWORK = 63,
                SM_PENWINDOWS = 41,
                SM_REMOTECONTROL = 0x2001,
                SM_REMOTESESSION = 0x1000,
                SM_SAMEDISPLAYFORMAT = 81,
                SM_SECURE = 44,
                SM_SERVERR2 = 89,
                SM_SHOWSOUNDS = 70,
                SM_SHUTTINGDOWN = 0x2000,
                SM_SLOWMACHINE = 73,
                SM_STARTER = 88,
                SM_SWAPBUTTON = 23,
                SM_SYSTEMDOCKED = 0x2004,
                SM_TABLETPC = 86,
                SM_XVIRTUALSCREEN = 76,
                SM_YVIRTUALSCREEN = 77
            }

            [StructLayout(LayoutKind.Sequential)]
            internal struct SYSTEM_INFO
            {
                internal ushort wProcessorArchitecture;
                internal ushort wReserved;
                internal uint dwPageSize;
                internal IntPtr lpMinimumApplicationAddress;
                internal IntPtr lpMaximumApplicationAddress;
                internal IntPtr dwActiveProcessorMask;
                internal uint dwNumberOfProcessors;
                internal uint dwProcessorType;
                internal uint dwAllocationGranularity;
                internal ushort wProcessorLevel;
                internal ushort wProcessorRevision;
            }

            internal struct MEMORY_BASIC_INFORMATION
            {
                public IntPtr BaseAddress;
                public IntPtr AllocationBase;
                public AllocationProtectEnum AllocationProtect;
                public IntPtr RegionSize;
                public StateEnum State;
                public AllocationProtectEnum Protect;
                public TypeEnum Type;
            }

            internal enum AllocationProtectEnum : uint
            {
                PAGE_EXECUTE = 0x00000010,
                PAGE_EXECUTE_READ = 0x00000020,
                PAGE_EXECUTE_READWRITE = 0x00000040,
                PAGE_EXECUTE_WRITECOPY = 0x00000080,
                PAGE_NOACCESS = 0x00000001,
                PAGE_READONLY = 0x00000002,
                PAGE_READWRITE = 0x00000004,
                PAGE_WRITECOPY = 0x00000008,
                PAGE_GUARD = 0x00000100,
                PAGE_NOCACHE = 0x00000200,
                PAGE_WRITECOMBINE = 0x00000400
            }

            internal enum StateEnum : uint
            {
                MEM_COMMIT = 0x1000,
                MEM_FREE = 0x10000,
                MEM_RESERVE = 0x2000
            }

            internal enum TypeEnum : uint
            {
                MEM_IMAGE = 0x1000000,
                MEM_MAPPED = 0x40000,
                MEM_PRIVATE = 0x20000
            }

            public enum CONTEXT_FLAGS : uint
            {
                CONTEXT_i386 = 0x10000,
                CONTEXT_i486 = 0x10000,   //  same as i386
                CONTEXT_CONTROL = CONTEXT_i386 | 0x01, // SS:SP, CS:IP, FLAGS, BP
                CONTEXT_INTEGER = CONTEXT_i386 | 0x02, // AX, BX, CX, DX, SI, DI
                CONTEXT_SEGMENTS = CONTEXT_i386 | 0x04, // DS, ES, FS, GS
                CONTEXT_FLOATING_POINT = CONTEXT_i386 | 0x08, // 387 state
                CONTEXT_DEBUG_REGISTERS = CONTEXT_i386 | 0x10, // DB 0-3,6,7
                CONTEXT_EXTENDED_REGISTERS = CONTEXT_i386 | 0x20, // cpu specific extensions
                CONTEXT_FULL = CONTEXT_CONTROL | CONTEXT_INTEGER | CONTEXT_SEGMENTS,
                CONTEXT_ALL = CONTEXT_CONTROL | CONTEXT_INTEGER | CONTEXT_SEGMENTS | CONTEXT_FLOATING_POINT | CONTEXT_DEBUG_REGISTERS | CONTEXT_EXTENDED_REGISTERS
            }

            [Flags]
            internal enum ThreadAccess : int
            {
                TERMINATE = (0x0001),
                SUSPEND_RESUME = (0x0002),
                GET_CONTEXT = (0x0008),
                SET_CONTEXT = (0x0010),
                SET_INFORMATION = (0x0020),
                QUERY_INFORMATION = (0x0040),
                SET_THREAD_TOKEN = (0x0080),
                IMPERSONATE = (0x0100),
                DIRECT_IMPERSONATION = (0x0200)
            }

            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            public struct PROCESS_BASIC_INFORMATION
            {
                public IntPtr ExitStatus;
                public IntPtr PebBaseAddress;
                public IntPtr AffinityMask;
                public IntPtr BasePriority;
                public UIntPtr UniqueProcessId;
                public IntPtr InheritedFromUniqueProcessId;

                public int Size
                {
                    get { return (int)Marshal.SizeOf(typeof(PROCESS_BASIC_INFORMATION)); }
                }
            }

            #endregion

            #region Constants

            public const uint EXCEPTION_DEBUG_EVENT = 1;
            public const UInt32 EXCEPTION_ACCESS_VIOLATION = 0xC0000005;
            public const UInt32 EXCEPTION_BREAKPOINT = 0x80000003;
            public const UInt32 EXCEPTION_DATATYPE_MISALIGNMENT = 0x80000002;
            public const UInt32 EXCEPTION_SINGLE_STEP = 0x80000004;
            public const UInt32 EXCEPTION_ARRAY_BOUNDS_EXCEEDED = 0xC000008C;
            public const UInt32 EXCEPTION_INT_DIVIDE_BY_ZERO = 0xC0000094;
            public const UInt32 STATUS_WX86_BREAKPOINT = 0x4000001F;
            public const UInt32 DBG_CONTROL_C = 0x40010006;

            public const uint PROCESS_VM_WRITE = 0x0020;
            public const uint PROCESS_VM_OPERATION = 0x0008;
            public const uint PROCESS_VM_READ = 0x0010;
            public const uint PROCESS_QUERY_INFORMATION = 0x0400;

            public const uint STATUS_BREAKPOINT = 0x80000003;

            public const int WHITE_BRUSH = 0;
            public const int LTGRAY_BRUSH = 1;
            public const int GRAY_BRUSH = 2;
            public const int DKGRAY_BRUSH = 3;
            public const int BLACK_BRUSH = 4;
            public const int NULL_BRUSH = 5;
            public const int HOLLOW_BRUSH = NULL_BRUSH;

            public const uint MAPVK_VK_TO_VSC = 0x00;
            public const uint MAPVK_VSC_TO_VK = 0x01;
            public const uint MAPVK_VK_TO_CHAR = 0x02;
            public const uint MAPVK_VSC_TO_VK_EX = 0x03;
            public const uint MAPVK_VK_TO_VSC_EX = 0x04;

            public const uint WM_APP = 0x8000;
            public const uint WM_QUIT = 0x12;

            #endregion
        }

        #endregion

        public delegate void GameHookedHandler(object sender, EventArgs e);
        public event GameHookedHandler OnGameHooked;

        public delegate void GameClosedHandler(object sender, EventArgs e);
        public event GameClosedHandler OnGameClosed;

        protected System.Timers.Timer _tProcess;
        protected int NoProcessTickCount = 0;
        protected bool _IsRunningTimerTickCheck = false;

        //Transparent window cursor created if needed
        protected IntPtr _CustomTransparentCursorHandle = IntPtr.Zero;

        //Flag to check if the Breakpoint Exception to come in the debugger is the initial one
        //In case PEB is not patched, we will use that breakpoint to apply hacks
        protected bool _IsInitialBreakpoint = true;

        //If PEB is not patched, debugger will receive Exception Breakpoints (including initial ExceptionBReakpoint) and 
        //detecting the first one will be needed to set ou Hack
        protected bool _IsFirstException = true;

        #region Game Process variables

        protected Process _Process;
        protected IntPtr _Process_Handle = IntPtr.Zero;
        protected IntPtr _Process_MemoryBaseAddress = IntPtr.Zero;
        protected IntPtr _Process_EntryPointAddress = IntPtr.Zero;
        protected IntPtr _Window_Handle = IntPtr.Zero;
        protected x86_x64_Debugger _GameLauncher;
        //For certain game, allowng this to get Process.MainWindowHandle instead of looking with window title if not possible
        protected bool _IsMainWindowHandlePossible = false;
        //For certain game, allowng this to get any process Window instead of looking for Title (like, foreign characters not beeing read...)
        protected bool _IsAnyWindowTitlePossible = false;
        //Depending on the different windows created (cmdline, emulator), it may be possible to change the Window Title filter type applied.
        //Default one is "contains" but some need different things
        public enum WindowTitleFilter
        {
            StartWith = 0,
            Contains,
            EndWith,
            Equals
        }
        protected WindowTitleFilter _WindowTitleFilterMode = WindowTitleFilter.Contains;

        //MD5 check of target binaries, may help to know if it's the wrong version or not compatible
        protected Dictionary<string, string> _KnownMd5Prints;
        protected string _TargetProcess_Md5Hash = string.Empty;

        //Output values handling
        protected List<GameOutput> _Outputs;
        public List<GameOutput> Outputs
        {
            get { return _Outputs; }
        }

        protected bool _ProcessHooked;
        public bool ProcessHooked
        {
            get { return _ProcessHooked; }
        }

        protected GameConfig _GameConfig;

        protected double _AxisX_ScaleFactor = 1.0;

        #endregion

        #region Custom Databanks

        protected IntPtr _LoaderDatabank_Address = IntPtr.Zero;
        protected IntPtr _InputsDatabank_Address = IntPtr.Zero;
        protected IntPtr _OutputsDatabank_Address = IntPtr.Zero;

        #endregion

        #region  Custom Outputs

        protected int[] _Ammo;
        protected int[] _LastAmmo;
        protected int[] _Life;
        protected int[] _LastLife;

        #endregion

        #region Debugger Events

        #region CREATE_PROCESS_DEBUG_EVENT

        protected virtual void CreateProcessEvent(object sender, x86_x64_Debugger.CreateProcessDebugEventArgs e)
        {
            x86_x64_Debugger Dbg = (x86_x64_Debugger)sender;

            Logger.WriteLog("[CREATE_PROCESS_DEBUG_EVENT]");

            _Process = Process.GetProcessById((int)(e.dwProcessId));
            _Process_Handle = Dbg.CreatedProcessHandle;
            _Process_MemoryBaseAddress = e.CreateProcessInfo.lpBaseOfImage;
            _Process_EntryPointAddress = e.CreateProcessInfo.lpStartAddress;

            Logger.WriteLog(" > Process ID: " + _Process.Id);
            Logger.WriteLog(" > Process Memory Base Address: 0x" + _Process_MemoryBaseAddress.ToString(Dbg.HexStringFormat));
            Logger.WriteLog(" > Process Entry Point Address: 0x" + _Process_EntryPointAddress.ToString(Dbg.HexStringFormat));
            //HANDLE should always be in 32Bits range (https://learn.microsoft.com/fr-fr/windows/win32/winprog64/interprocess-communication?redirectedfrom=MSDN)
            Logger.WriteLog(" > Process Handle: 0x" + _Process_Handle.ToString("X8"));
            Logger.WriteLog(" > Process hThread: 0x" + e.CreateProcessInfo.hThread.ToString("X8"));

            CheckExeMd5();
            ReadGameDataFromMd5Hash(GAMEDATA_FOLDER);

            //Removing the BeingDebugged flag in PED will stop the debigger from receiving initial EXCEPTION BREAKPOINT event
            //To replace this, we will manually place a INT3 breakpoint at program entry address
            if (PATCH_PEB_DEBUG_FLAG)
            {                
                SetEntryPointBreakpoint();
            }

            //Start the Timer that will either hook an emulator when ROM is loaded, or close the program if the target binary is not running
            _tProcess.Start();
        }

        #endregion

        #region CREATE_THREAD_DEBUG_EVENT

        protected virtual void CreateThreadEvent(object sender, x86_x64_Debugger.CreateThreadDebugEventArgs e)
        {
            x86_x64_Debugger Dbg = (x86_x64_Debugger)sender;

            Logger.WriteLog("[CREATE_THREAD_DEBUG_EVENT]");
            Logger.WriteLog(" > Thread ID: " + e.dwThreadId.ToString());
            //HANDLE should always be in 32Bits range (https://learn.microsoft.com/fr-fr/windows/win32/winprog64/interprocess-communication?redirectedfrom=MSDN)
            Logger.WriteLog(" > Thread Handle: 0x" + e.CreateThreadInfo.hThread.ToString("X8"));
            Logger.WriteLog(" > Thread Start address: 0x" + e.CreateThreadInfo.lpStartAddress.ToString(Dbg.HexStringFormat));
        }

        #endregion

        #region LOAD_DLL_DEBUG_EVENT

        protected virtual void LoadDllEvent(object sender, x86_x64_Debugger.LoadDllDebugEventArgs e)
        {            
            //Load DLL event will only be used when a game needs DLL patching at start
        }

        #endregion

        #region UNLOAD_DLL_DEBUG_EVENT

        protected virtual void UnloadDllEvent(object sender, x86_x64_Debugger.UnloadDllDebugEventArgs e)
        {
            x86_x64_Debugger Dbg = (x86_x64_Debugger)sender;

            string s = "[UNLOAD_DLL_DEBUG_EVENT] ";
            IntPtr lpBaseOfDllLoad = e.UnloadDllInfo.lpBaseOfDll;
            s += "lpBaseOfDllLoad: 0x" + lpBaseOfDllLoad.ToString(Dbg.HexStringFormat);
            Logger.WriteLog(s);
        }

        #endregion

        #region OUTPUTDEBUGSTRING_DEBUG_EVENT

        protected virtual void OutputDebugStringEvent(object sender, x86_x64_Debugger.OutputDebugStringDebugEventArgs e)
        {
            x86_x64_Debugger Dbg = (x86_x64_Debugger)sender;

            string s = "[OUTPUTDEBUGSTRING_DEBUG_EVENT] ";
            string Message = string.Empty;
            byte[] Buffer = ReadBytes(e.OutputDebugStringInfo.lpDebugStringData, e.OutputDebugStringInfo.nDebugStringLength);

            if (e.OutputDebugStringInfo.fUnicode == 0)
                Message = System.Text.Encoding.Default.GetString(Buffer);
            else
                Message = System.Text.Encoding.Unicode.GetString(Buffer);

            s += Message;
            Logger.WriteLog(s);
        }

        #endregion

        #region EXCEPTION_DEBUG_EVENT

        protected virtual void ExceptionEvent(object sender, x86_x64_Debugger.ExceptionDebugEventArgs e)
        {
            //Exception will be handled with different ways based on the desired target process architecture (32/64 Bits)
        }

        #endregion

        #region EXIT_THREAD_DEBUG_EVENT

        protected virtual void ExitThreadEvent(object sender, x86_x64_Debugger.ExitThreadDebugEventArgs e)
        {
            Logger.WriteLog("[EXIT_THREAD_DEBUG_EVENT] dwExitCode: " + e.ExitThreadInfo.dwExitCode.ToString());
        }

        #endregion

        #region EXIT_PROCESS_DEBUG_EVENT

        protected virtual void ExitProcessEvent(object sender, x86_x64_Debugger.ExitProcessDebugEventArgs e)
        {
            Logger.WriteLog("[EXIT_PROCESS_DEBUG_EVENT] dwExitCode = " + e.ExitProcessInfo.dwExitCode.ToString());
            if (_Process_Handle != IntPtr.Zero)
                Win32.CloseHandle(_Process_Handle);

            RaiseGameClosedEvent();
        }

        #endregion

        #endregion

        protected VirtualGameControler _GameController;

        /// <summary>
        /// Common constructor for every single games
        /// </summary>
        public BaseGame(GameConfig Config)
        {
            _GameConfig = Config;

            //Merging  Program config and Game config files
            //Program Config Keys will be used if a Key entry has not been set in Game specific configuration
            for (int i = 0; i < _GameConfig.System_Inputs.Count; i++)
            {
                if (_GameConfig.System_Inputs[i].GetNumberOfEntries() == 0)
                {
                    foreach (BaseConfig_Input ProgramItem in ProgramConfig.GetInstance().System_Inputs)
                    {
                        if (ProgramItem.Name.Equals(_GameConfig.System_Inputs[i].Name))
                        {
                            _GameConfig.System_Inputs[i] = ProgramItem;
                            break;
                        }
                    }
                }
            }
            for (int PlayerIndex = 0; PlayerIndex < _GameConfig.Players_Inputs.Count; PlayerIndex++)
            {
                for (int i = 0; i < _GameConfig.Players_Inputs[PlayerIndex].Inputs.Count; i++)
                {
                    if (_GameConfig.Players_Inputs[PlayerIndex].Inputs[i].GetNumberOfEntries() == 0)
                    {
                        foreach (BaseConfig_Player ProgramPlayer in ProgramConfig.GetInstance().Players_Inputs)
                        {
                            if (ProgramPlayer.Id == _GameConfig.Players_Inputs[PlayerIndex].Id)
                            {
                                foreach (BaseConfig_Input ProgramPlayerItem in ProgramPlayer.Inputs)
                                {
                                    if (ProgramPlayerItem.Name.Equals(_GameConfig.Players_Inputs[PlayerIndex].Inputs[i].Name))
                                    {
                                        _GameConfig.Players_Inputs[PlayerIndex].Inputs[i] = ProgramPlayerItem;
                                        break;
                                    }
                                }
                                break;
                            }
                        }
                    }
                }
            }

            _KnownMd5Prints = new Dictionary<String, String>();
            GetScreenResolution();
            Logger.WriteLog("Windows screen scaling : " + GetScreenScaling());

            _WindowRect = new Win32.Rect();
            _ClientWindowLocation = new Win32.POINT(0, 0);

            _ProcessHooked = false;
            Logger.WriteLog("Target path : " + Config.TargetPath);

            string strScaleFactor = _GameConfig.X_Scaling;
            if (strScaleFactor.Contains("/") && strScaleFactor.Split('/').Length > 1)
            {
                double d1 = Double.Parse(strScaleFactor.Split('/')[0]);
                double d2 = Double.Parse(strScaleFactor.Split('/')[1]);
                _AxisX_ScaleFactor = d1 / d2;
            }
            else
            {
                if (double.TryParse(strScaleFactor.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out double dTmpValue))
                {
                    _AxisX_ScaleFactor = dTmpValue;
                }
            }          

            CreateOutputList();
            _Ammo = new int[_GameConfig.PlayerCount];
            _LastAmmo = new int[_GameConfig.PlayerCount];
            _Life = new int[_GameConfig.PlayerCount];
            _LastLife = new int[_GameConfig.PlayerCount];

            _GameController = new VirtualGameControler(Config);
            if (!_GameConfig.NoInputs)
            {
                _GameController.OnButtonDown += GameController_OnButtonDown;
                _GameController.OnButtonUp += GameController_OnButtonUp;
                foreach (VirtualPlayerController c in _GameController.PlayerControllers)
                {
                    c.OnButtonDown += PlayerController_OnButtonDown;
                    c.OnButtonUp += PlayerController_OnButtonUp;
                    c.OnAxisChanged += PlayerController_OnAxisChanged;
                }
            }

            // Creating the Debugger and associated Events callbacks
            _GameLauncher = new x86_x64_Debugger(Config.TargetPath);

            //Launcher event, as sooon as the process is created and suspended
            _GameLauncher.OnProcessCreated += ProcessCreated;

            //Debug loop events
            _GameLauncher.OnCreateProcess += CreateProcessEvent;
            _GameLauncher.OnException += ExceptionEvent;
            _GameLauncher.OnExitProcess += ExitProcessEvent;

            //Creating a Timer to autoclose U-Shoot if the hook process is not present anymore
            //Launcher should get notified if debugger still attached, but it can be detached
            _tProcess = new System.Timers.Timer();
            _tProcess.Interval = 1000;
            _tProcess.Elapsed += new System.Timers.ElapsedEventHandler(TimerProcess_Elapsed);

            AttachDebuggerEvents();
        }

        /// <summary>
        /// The process will be started SUSPENDED, so that we can patch the PEB flags and avoid debugger detection by the game 
        /// </summary>
        protected virtual void ProcessCreated(object sender, EventArgs e)
        {
            if (!_GameLauncher.DisableDebugger)
            {
                if (PATCH_PEB_DEBUG_FLAG)
                    _GameLauncher.PatchPEB();
            }
        }

        /// <summary>
        /// Start the game by running the Debugger
        /// </summary>
        public void Start()
        {
            _GameLauncher.Start();
        }

        /// <summary>
        /// close the game ruuning
        /// </summary>
        public void Close(bool ForceKill = true)
        {
            if (_Process != null)
            {
                try
                {
                    _Process.CloseMainWindow();
                }
                catch { }

                if (ForceKill)
                {
                    System.Threading.Thread.Sleep(500);

                    if (!_Process.HasExited)
                        _Process.Kill();
                }
            }
            RaiseGameClosedEvent();
        }

        /// <summary>
        /// Attach more Debugger events if needed, according to the game
        /// Some will need DLL_LOADED, others CreateThread, etc..
        /// </summary>
        protected virtual void AttachDebuggerEvents()
        {
            //x86_x64_Debugger.OnCreateThread += CreateThreadEvent;
            //x86_x64_Debugger.OnLoadedDll += LoadDllEvent;
            //x86_x64_Debugger.OnUnloadedDll += UnloadDllEvent;

            //QuickDebugger.OnOutputDebugString += OutputDebugStringEvent;
            //x86_x64_Debugger.OnExitThread += ExitThreadEvent;
            //QuickDebugger.OnRip += RipEvent; 
        }

        /// <summary>
        /// Raise custom event to main window (to change TrayIcon status)
        /// </summary>
        protected void RaiseGameHookedEvent()
        {
            _ProcessHooked = true;

            // Make sure someone is listening to event
            if (OnGameHooked == null) return;
                OnGameHooked(this, new EventArgs());
        }

        protected void RaiseGameClosedEvent()
        {
            //_ProcessHooked = false;

            if (OnGameClosed != null)
                OnGameClosed(this, EventArgs.Empty);
        }

        /// <summary>
        /// When detaching the debugger, old process handle becomes invalid and memory operations can't be done
        /// That's why reopening the process with needed rights is mandatory
        /// </summary>
        public void DetachDebuggerAndReopenProcess()
        {
            _GameLauncher.DetachDebugger();
            _Process_Handle = Win32.OpenProcess(Win32.PROCESS_VM_WRITE | Win32.PROCESS_VM_OPERATION | Win32.PROCESS_VM_READ | Win32.PROCESS_QUERY_INFORMATION, false, (uint)_Process.Id);

            if (_Process_Handle == IntPtr.Zero)
            {
                Logger.WriteLog("[" + this.GetType().Name + "] Error trying to reopen the Process");
            }
        }

        private void TimerProcess_Elapsed(Object sender, EventArgs e)
        {
            if (!_IsRunningTimerTickCheck)
            {
                _IsRunningTimerTickCheck = true;

                if (!_ProcessHooked)
                {
                    ProcessTimerTick_Unhooked_CheckProc();
                }
                else
                {
                    ProcessTimerTick_Hooked_CheckProc();

                    if (_Process.HasExited)
                    {
                        NoProcessTickCount++;
                        if (NoProcessTickCount > 1)
                        {
                            RaiseGameClosedEvent();
                            _tProcess.Stop();
                        }
                    }
                }

                _IsRunningTimerTickCheck = false;
            }

        }

        protected virtual void ProcessTimerTick_Unhooked_CheckProc()
        { }

        protected virtual void ProcessTimerTick_Hooked_CheckProc()
        { }

        #region MD5 Verification

        /// <summary>
        /// Compute the MD5 hash of the target executable and compare it to the known list of MD5 Hashes
        /// This can be usefull if people are using some unknown dump with different memory, 
        /// or a wrong version of emulator
        /// This is absolutely not blocking, just for debuging with output log
        /// </summary>
        protected void CheckExeMd5()
        {
            _TargetProcess_Md5Hash = GetMd5HashAsString(_GameConfig.TargetPath);
            CheckMd5(_GameConfig.TargetPath, _KnownMd5Prints);
        }
        protected void CheckMd5(string FilePath, Dictionary<string, string> KnownPrints)
        {
            string sFileCheck = " Check Target: " + FilePath;            
            string StrHash = GetMd5HashAsString(FilePath);
            string sFileHash = " MD5 Hash: " + StrHash;
            string sCheckResult = string.Empty;

            if (string.IsNullOrEmpty(StrHash))
            {
                sCheckResult = " Error calculating MD5 HASH of the following file : " + FilePath;
            }
            else
            {
                string FoundMd5 = string.Empty;
                foreach (KeyValuePair<string, string> pair in KnownPrints)
                {
                    if (pair.Value.ToLower() == StrHash)
                    {
                        FoundMd5 = pair.Key;
                        break;
                    }
                }

                if (string.IsNullOrEmpty(FoundMd5))
                    sCheckResult = @"  /!\ MD5 Hash unknown, U-SHOOT may not work correctly with this target /!\";
                else
                    sCheckResult = " MD5 Hash is corresponding to a known target = " + FoundMd5;
            }

            string sBorder = ">";
            int iLength = Math.Max(sFileCheck.Length, sFileHash.Length);
            iLength = Math.Max(iLength, sCheckResult.Length);
            for (int i = 0; i < iLength; i++)
            {
                sBorder += "-";
            }
            sBorder += "<";

            Logger.WriteLog(sBorder);
            Logger.WriteLog(sFileCheck);
            Logger.WriteLog(sFileHash);
            Logger.WriteLog(sCheckResult);
            Logger.WriteLog(sBorder);
        }

        /// <summary>
        /// Compute the MD5 hash from the target file.
        /// </summary>
        /// <param name="FileName">Full  filepath of the targeted executable.</param>
        protected string GetMd5HashAsString(string FileName)
        {
            if (File.Exists(FileName))
            {
                using (var md5 = MD5.Create())
                {
                    using (var stream = File.OpenRead(FileName))
                    {
                        var hash = md5.ComputeHash(stream);
                        string StrHash = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                        return StrHash;
                    }
                }
            }
            return string.Empty;
        }

        #endregion

        #region Screen

        protected int _DesktopScreenWidth;
        protected int _DesktopScreenHeight;
        
        protected int _ScreenCursorPosX;
        protected int _ScreenCursorPosY;
        
        internal  Win32.Rect _WindowRect;
        internal Win32.POINT _ClientWindowLocation;
        internal Win32.Rect _ClientRect;
        
        protected bool _IsFullscreen;
        
        protected virtual IntPtr GetGameWindowHandle()
        { 
            if (_Window_Handle != IntPtr.Zero) 
                return _Window_Handle;

            if (_Process != null && ! _Process.HasExited)
            {
                Win32.EnumWindows((hWnd, lParam) =>
                {
                    Win32.GetWindowThreadProcessId(hWnd, out uint windowPid);

                    if (windowPid == _Process.Id && Win32.IsWindowVisible(hWnd))
                    {
                        int length = Win32.GetWindowTextLength(hWnd);
                        if (length >= 0)
                        {
                            StringBuilder builder = new StringBuilder(length);
                            Win32.GetWindowText(hWnd, builder, length + 1);
                            string WindowTitle = builder.ToString();
                            Logger.WriteLog("Found a Window: Handle = 0x" + hWnd.ToString("X8") + ", Title = " + WindowTitle);
                            if (_GameConfig.WindowTitle.Length > 0)
                            {
                                bool bIsTitleMatch = false;
                                switch (_WindowTitleFilterMode)
                                {
                                    case WindowTitleFilter.StartWith : bIsTitleMatch = WindowTitle.ToLower().StartsWith(_GameConfig.WindowTitle.ToLower()); break;
                                    case WindowTitleFilter.Contains : bIsTitleMatch = WindowTitle.ToLower().Contains(_GameConfig.WindowTitle.ToLower()); break;
                                    case WindowTitleFilter.EndWith: bIsTitleMatch = WindowTitle.ToLower().EndsWith(_GameConfig.WindowTitle.ToLower()); break;
                                    case WindowTitleFilter.Equals: bIsTitleMatch = string.Equals(WindowTitle, _GameConfig.WindowTitle, StringComparison.OrdinalIgnoreCase); break;
                                    default: break;
                                }
                                //if (Regex.Match(WindowTitle.ToLower(), _GameConfig.WindowTitle.ToLower()).Success)
                                if (bIsTitleMatch)
                                {
                                    _Window_Handle = hWnd;
                                    Logger.WriteLog("=> Selecting 0x" + hWnd.ToString("X8") + " as game Window Handle");
                                    return false; // Stop enumeration
                                }
                            }
                            else if (_IsAnyWindowTitlePossible)
                            {
                                //Sometimes it is impossible to specify a Window Title, either because the text is in chinese/japanese or foreign character
                                //And it can't be written in our XML files
                                //So just returning the first one found in that case
                                Logger.WriteLog("=> No WindowTitle specified");
                                _Window_Handle = hWnd;
                                Logger.WriteLog("=> Selecting 0x" + hWnd.ToString("X8") + " as game Window Handle");
                                return false; // Stop enumeration
                            }
                        }
                    }
                    return true; // Continue
                }, IntPtr.Zero);

                if (_IsMainWindowHandlePossible == true && _Process.MainWindowHandle != IntPtr.Zero)
                {
                    Logger.WriteLog("Using Process.MainWindowHandle=0x" + _Process.MainWindowHandle.ToString("X8") + " as Game Window Handle");
                    _Window_Handle = _Process.MainWindowHandle;
                }
            }

            return IntPtr.Zero; 
        }

        /// <summary>
        /// HKEY_CURRENT_USER\Control Panel\Desktop\
        ///Key: LogPixels
        ///Values:
        ///96   –  Smaller 100%
        ///120  –  Medium 125%
        ///144  -  Larger 150%
        ///192  –  Extra Large 200%
        ///240  –  Custom 250%
        ///288  –  Custom 300%
        ///384  –  Custom 400%
        ///480  –  Custom 500%*/
        /// </summary>
        private string GetScreenScaling()
        {
            IntPtr desktopDc = Win32.GetDC(IntPtr.Zero);
            // Get native resolution
            //#DEFINE LOGPIXELSX       88
            //#DEFINE LOGPIXELSY       90
            int horizontalDPI = Win32.GetDeviceCaps(desktopDc, 88);
            int verticalDPI = Win32.GetDeviceCaps(desktopDc, 90);

            return (horizontalDPI * 100 / 96).ToString() + "% (HorizontalDPI=" + horizontalDPI + ", VerticalDPI=" + verticalDPI + ")";

            /*switch (horizontalDPI.ToString())
            {
                case "96": return "100%";
                case "120": return "125%";
                case "144": return "150%";
                case "192": return "200%";
                case "240": return "250%";
                case "288": return "300%";
                case "384": return "400%";
                case "480": return "500%";
                default: return @"Unknown value : " + horizontalDPI.ToString();

            }*/
        }

        public virtual bool GetFullscreenStatus()
        {
            Win32.QUERY_USER_NOTIFICATION_STATE state;
            int r = Win32.SHQueryUserNotificationState(out state);
            if (r == 0)
            {
                Logger.WriteLog(" > NotificationState: " + state.ToString());
                switch (state)
                {
                    // A screen saver is displayed, the machine is locked, or a nonactive Fast User Switching session is in progress.
                    case Win32.QUERY_USER_NOTIFICATION_STATE.QUNS_NOT_PRESENT: return false;

                    // A full-screen application is running or Presentation Settings are applied. Presentation Settings allow a user to put their machine into a state fit for an uninterrupted presentation, such as a set of PowerPoint slides, with a single click.
                    case Win32.QUERY_USER_NOTIFICATION_STATE.QUNS_BUSY: return true;

                    // A full-screen (exclusive mode) Direct3D application is running.
                    case Win32.QUERY_USER_NOTIFICATION_STATE.QUNS_RUNNING_D3D_FULL_SCREEN: return true;

                    // The user has activated Windows presentation settings to block notifications and pop-up messages.
                    case Win32.QUERY_USER_NOTIFICATION_STATE.QUNS_PRESENTATION_MODE: return false;

                    // None of the other states are found, notifications can be freely sent.
                    case Win32.QUERY_USER_NOTIFICATION_STATE.QUNS_ACCEPTS_NOTIFICATIONS: return false;

                    // We are in OOBE quiet period
                    case Win32.QUERY_USER_NOTIFICATION_STATE.QUNS_QUIET_TIME: return false;

                    // A Windows Store app is running.
                    case Win32.QUERY_USER_NOTIFICATION_STATE.QUNS_APP: return false;

                    default: return false;
                }
            }
            else
            {
                Logger.WriteLog("Error running SHQueryUserNotificationState: " + r.ToString());
                return false;
            }
        }

        public virtual void GetScreenResolution()
        {
            _DesktopScreenWidth = Win32.GetSystemMetrics(Win32.SystemMetricsIndex.SM_CXSCREEN);
            _DesktopScreenHeight = Win32.GetSystemMetrics(Win32.SystemMetricsIndex.SM_CYSCREEN);
            /*_screenWidth = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width;
            _screenHeight = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height;*/
        }

        public void GetScreenresolution2()
        {
            IntPtr hDesktop = Win32.GetDesktopWindow();
            Win32.Rect DesktopRect = new Win32.Rect();
            Win32.GetWindowRect(hDesktop, ref DesktopRect);
            _DesktopScreenWidth = DesktopRect.Right;
            _DesktopScreenHeight = DesktopRect.Bottom;
        }

        /// <summary>
        /// Contains value inside min-max range
        /// </summary>
        protected int Clamp(int val, int minVal, int maxVal)
        {
            if (val > maxVal) return maxVal;
            else if (val < minVal) return minVal;
            else return val;
        }

        /// <summary>
        /// Transforming 0x0000-0xFFFF absolute rawdata to absolute x,y position on Desktop resolution
        /// </summary>
        public int ScreenScale(int val, int fromMinVal, int fromMaxVal, int toMinVal, int toMaxVal)
        {
            return ScreenScale(val, fromMinVal, fromMinVal, fromMaxVal, toMinVal, toMinVal, toMaxVal);
        }
        protected int ScreenScale(int val, int fromMinVal, int fromOffVal, int fromMaxVal, int toMinVal, int toOffVal, int toMaxVal)
        {
            double fromRange;
            double frac;
            if (fromMaxVal > fromMinVal)
            {
                val = Clamp(val, fromMinVal, fromMaxVal);
                if (val > fromOffVal)
                {
                    fromRange = (double)(fromMaxVal - fromOffVal);
                    frac = (double)(val - fromOffVal) / fromRange;
                }
                else if (val < fromOffVal)
                {
                    fromRange = (double)(fromOffVal - fromMinVal);
                    frac = (double)(val - fromOffVal) / fromRange;
                }
                else
                    return toOffVal;
            }
            else if (fromMinVal > fromMaxVal)
            {
                val = Clamp(val, fromMaxVal, fromMinVal);
                if (val > fromOffVal)
                {
                    fromRange = (double)(fromMinVal - fromOffVal);
                    frac = (double)(fromOffVal - val) / fromRange;
                }
                else if (val < fromOffVal)
                {
                    fromRange = (double)(fromOffVal - fromMaxVal);
                    frac = (double)(fromOffVal - val) / fromRange;
                }
                else
                    return toOffVal;
            }
            else
                return toOffVal;
            double toRange;
            if (toMaxVal > toMinVal)
            {
                if (frac >= 0)
                    toRange = (double)(toMaxVal - toOffVal);
                else
                    toRange = (double)(toOffVal - toMinVal);
                return toOffVal + (int)(toRange * frac);
            }
            else
            {
                if (frac >= 0)
                    toRange = (double)(toOffVal - toMaxVal);
                else
                    toRange = (double)(toMinVal - toOffVal);
                return toOffVal - (int)(toRange * frac);
            }
        }

        /// <summary>
        /// Convert screen location of pointer to Client area location
        /// </summary>
        public virtual bool ClientScale(VirtualPlayerController PlayerController)
        {
            //Retrieve info for debug/replay purpose
            GetClientwindowInfo();

            //Convert Screen location to Client location
            if (_Process != null)
            {
                Win32.POINT p = new Win32.POINT(PlayerController.Computed_X, PlayerController.Computed_Y);
                if (Win32.ScreenToClient(GetGameWindowHandle(), ref p))
                {
                    PlayerController.Computed_X = p.X;
                    PlayerController.Computed_Y = p.Y;
                    return true;
                }
                else
                    return false;
            }
            else
                return false;
        }

        public void GetClientwindowInfo()
        {
            if (_Process != null)
            {
                if (Win32.GetWindowRect(GetGameWindowHandle(), ref _WindowRect))
                {
                    _ClientWindowLocation.X = _WindowRect.Left;
                    _ClientWindowLocation.Y = _WindowRect.Top;
                }
                else
                {
                    _ClientWindowLocation.X = 0;
                    _ClientWindowLocation.Y = 0;
                }
            }
        }

        /// <summary>
        /// Get the Client rect to translate axis coordinate to game coordinates
        /// Only used in windowed Mode
        /// </summary>
        public virtual bool GetClientRect()
        {

            //Window size
            _ClientRect = new Win32.Rect();
            if (Win32.GetClientRect(GetGameWindowHandle(), ref _ClientRect))
                return true;
            else
                return false;
        }

        /// <summary>
        /// Convert client area pointer location to Game speciffic data for memory injection
        /// </summary>
        public virtual bool GameScale(VirtualPlayerController PlayerController)
        {
            if (_Process_Handle != IntPtr.Zero)
            {
                try
                {
                    double TotalResX = _ClientRect.Right - _ClientRect.Left;
                    double TotalResY = _ClientRect.Bottom - _ClientRect.Top;
                    Logger.WriteLog("Game Window Rect (Px) = [ " + TotalResX + "x" + TotalResY + " ]");

                    if (PlayerController.Computed_X < 0)
                        PlayerController.Computed_X = 0;
                    if (PlayerController.Computed_Y < 0)
                        PlayerController.Computed_Y = 0;
                    if (PlayerController.Computed_X > (int)TotalResX)
                        PlayerController.Computed_X = (int)TotalResX;
                    if (PlayerController.Computed_Y > (int)TotalResY)
                        PlayerController.Computed_Y = (int)TotalResY;
                    return true;
                }
                catch (Exception ex)
                {
                    Logger.WriteLog("Error scaling mouse coordonates to GameFormat : " + ex.Message.ToString());
                }
            }
            return false;
        }

        #endregion

        #region Game Memory Configuration

        /// <summary>
        /// Read memory values in .cfg file, whose name depends on the targeted system
        /// Used for DEMUL targets
        /// </summary>
        protected virtual void ReadGameData()
        { }
        
        /// <summary>
        /// Read memory values in .cfg file, whose name depends on the MD5 hash of the targeted exe or Dll.
        /// Mostly used for PC games
        /// </summary>
        /// <param name="GameData_Folder"></param>
        protected virtual void ReadGameDataFromMd5Hash(string Md5Hash, string GameData_Folder)
        {
            string ConfigFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Properties.Resources.DATA_MEMORY_CONFIG_FOLDER, GameData_Folder, Md5Hash + ".cfg");
            if (File.Exists(ConfigFile))
            {
                Logger.WriteLog("Reading game memory setting from " + ConfigFile);
                using (StreamReader sr = new StreamReader(ConfigFile))
                {
                    string line;
                    string FieldName = string.Empty;
                    line = sr.ReadLine();
                    while (line != null)
                    {
                        if (!line.StartsWith(";"))
                        {
                            string[] buffer = line.Split('=');
                            if (buffer.Length > 1)
                            {
                                try
                                {
                                    FieldName = "_" + buffer[0].Trim();
                                    if (buffer[0].Contains("Nop"))
                                    {
                                        SetNopStructFromString(buffer[1].Trim(), FieldName);
                                    }
                                    else if (buffer[0].Contains("InjectionStruct"))
                                    {
                                        SetInjectionStructFromString(buffer[1].Trim(), FieldName);
                                    }
                                    else if (buffer[0].Contains("BreakpointStruct"))
                                    {
                                        SetBreakpointStructFromString(buffer[1].Trim(), FieldName);
                                    }
                                    else
                                    {
                                        SetAddressValueFromString(buffer[1].Substring(3).Trim(), FieldName);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Logger.WriteLog("Error reading game data for " + FieldName + " : " + ex.Message.ToString());
                                }
                            }
                        }
                        line = sr.ReadLine();
                    }
                    sr.Close();
                }
            }
            else
            {
                Logger.WriteLog("File not found : " + ConfigFile);
            }
        }

        /// <summary>
        /// Shorter version to automatically use EXE md5 hash, this is the most common case
        /// </summary>
        /// <param name="GameData_Folder"></param>
        protected virtual void ReadGameDataFromMd5Hash(string GameData_Folder)
        {
            ReadGameDataFromMd5Hash(_TargetProcess_Md5Hash, GameData_Folder);
        }
        

        #endregion

        #region Memory Hack 

        protected virtual void Apply_MemoryHacks()
        {
            Apply_LoaderMemoryHack();

            if (!_GameConfig.NoInputs)
                Apply_InputsMemoryHack();
            else
                Logger.WriteLog("Input Hack disabled");

            Apply_OutputsMemoryHack();

            if (_GameConfig.NoCrosshair)
            {
                Logger.WriteLog("Applying No-Crosshair hack...");
                Apply_NoCrosshairMemoryHack();
            }
        }

        protected virtual void Apply_LoaderMemoryHack()
        { }

        protected virtual void Create_LoaderDataBank()
        { }

        protected virtual void Apply_InputsMemoryHack()
        { }

        protected virtual void Create_InputsDataBank()
        { }

        protected virtual void Apply_OutputsMemoryHack()
        { }

        protected virtual void Create_OutputsDataBank()
        { }

        protected virtual void Apply_NoCrosshairMemoryHack()
        { }

        protected bool CreateTransparentCursor()
        {
            byte[] andMask = new byte[32 * 4]; // 32x32 cursor
            byte[] xorMask = new byte[32 * 4];

            // AND mask = all 1s → transparent
            for (int i = 0; i < andMask.Length; i++)
                andMask[i] = 0xFF;

            _CustomTransparentCursorHandle = Win32.CreateCursor(
                IntPtr.Zero,
                0, 0,
                32, 32,
                andMask,
                xorMask
            );

            if (_CustomTransparentCursorHandle != IntPtr.Zero)
            {
                return true;
            }
            {
                Logger.WriteLog("Error creating custom transparent cursor");
                return false;
            }
        }

        protected virtual void SetNopStructFromString(string sData, string FieldName)
        {
            //Function will be different between 32/64 bits target : Address will be parsed either as int/long to be changed into to IntPtr
        }

        protected virtual void SetInjectionStructFromString(string sData, string FieldName)
        {
            //Function will be different between 32/64 bits target : Address will be parsed either as int/long to be changed into to IntPtr
        }

        protected virtual void SetBreakpointStructFromString(string sData, string FieldName)
        {
            //Function will be different between 32/64 bits target : Address will be parsed either as int/long to be changed into to IntPtr
        }

        protected virtual void SetAddressValueFromString(string sData, string FieldName)
        {
            //Function will be different between 32/64 bits target : Address will be parsed either as int/long to be changed into to IntPtr
            //IntPtr.Parse() not available in .NET 4.8
        }

        protected Byte ReadByte(IntPtr Address)
        {
            byte[] Buffer = { 0 };
            UIntPtr bytesRead = UIntPtr.Zero;
            if (!Win32.ReadProcessMemory(_Process_Handle, Address, Buffer, (UIntPtr)1, out bytesRead))
            {
                Logger.WriteLog("Cannot read memory at address 0x" + Address.ToString("X16"));
            }
            return Buffer[0];
        }

        protected Byte[] ReadBytes(IntPtr Address, int BytesCount)
        {
            byte[] Buffer = new byte[BytesCount];
            UIntPtr bytesRead = UIntPtr.Zero;
            if (!Win32.ReadProcessMemory(_Process_Handle, Address, Buffer, (UIntPtr)Buffer.Length, out bytesRead))
            {
                Logger.WriteLog("Cannot read memory at address 0x" + Address.ToString("X16"));
            }
            return Buffer;
        }

        protected virtual IntPtr ReadPtr(IntPtr PtrAddress)
        {
            //Return value will be different between 32/64 bits target : ReadBytes will be parsed as either int or long
            return IntPtr.Zero;
        }

        protected virtual IntPtr ReadPtrChain(IntPtr BaseAddress, IntPtr[] Offsets)
        {
            //Return value will be different between 32/64 bits target : ReadBytes will be parsed as either int or long
            return IntPtr.Zero;
        }

        protected virtual UInt32 ReadUInt32(IntPtr Address)
        {
            //Return value will be different between 32/64 bits target : ReadBytes will be parsed as either int or long
            return 0;
        }

        protected virtual Int32 ReadInt32(IntPtr Address)
        {
            //Return value will be different between 32/64 bits target : ReadBytes will be parsed as either int or long
            return 0;
        }

        protected virtual UInt64 ReadUInt64(IntPtr Address)
        {
            //Return value will be different between 32/64 bits target : ReadBytes will be parsed as either int or long
            return 0;
        }

        protected virtual Int64 ReadInt64(IntPtr Address)
        {
            //Return value will be different between 32/64 bits target : ReadBytes will be parsed as either int or long
            return 0;
        }

        protected virtual bool WriteByte(IntPtr Address, byte bData)
        {
            UIntPtr bytesWritten = UIntPtr.Zero;
            return WriteBytes(Address, new byte[] { bData});
        }

        protected virtual bool WriteBytes(IntPtr Address, byte[] Buffer)
        {
            UIntPtr bytesWritten = UIntPtr.Zero;
            if (Win32.WriteProcessMemory(_Process_Handle, Address, Buffer, (UIntPtr)Buffer.Length, out bytesWritten))
            {
                if ((int)bytesWritten == Buffer.Length)
                    return true;
                else
                    return false;
            }
            else
                return false;
        }

        protected virtual void SetNops(IntPtr BaseAddress, NopStruct32 Nop)
        {
            //NopStruct will be different between 32/64 bits target : Address will be parsed either as int/long to be added to IntPtr
        }

        protected virtual void Apply_OR_ByteMask(IntPtr MemoryAddress, byte Mask)
        {
            byte b = ReadByte(MemoryAddress);
            b |= Mask;
            WriteByte(MemoryAddress, b);
        }

        protected virtual void Apply_AND_ByteMask(IntPtr MemoryAddress, byte Mask)
        {
            byte b = ReadByte(MemoryAddress);
            b &= Mask;
            WriteByte(MemoryAddress, b);
        }

        protected virtual void SetEntryPointBreakpoint()
        {
            //SetEntryPointBreakpoint will be different between 32 / 64 bits target : used structures will be either BreakPointStruct32 or BreakPointStruct64
        }

        #endregion

        #region Inputs

        protected virtual void PlayerController_OnAxisChanged(object sender, VirtualGameControler.AxisChangedEventArgs e)
        {
            // Handle here screen to client, to game, etc......
            if (_ProcessHooked)
            {
                VirtualPlayerController PlayerController = _GameController.PlayerControllers[(int)e.Id];
                InputAxesEntry AxesConfig = _GameConfig.GetPlayerAxesEntryFromDevicePath((int)e.Id, ((VirtualPlayerController)sender).AxisController.DevicePath);

                Logger.WriteLog("RawData event for " + e.Id.ToString() + ":");
                Logger.WriteLog(" > Device rawinput data (Hex) = [ " + PlayerController.RawX.ToString("X8") + ", " + PlayerController.RawY.ToString("X8") + " ]");

                GetScreenResolution();
                Logger.WriteLog(" > PrimaryScreen Size (Px) = [ " + _DesktopScreenWidth + "x" + _DesktopScreenHeight + " ]");

                //Overrriding RAWINPUT data (relative movement) for single mouse by a call to GetCursorPos WIN32 API
                //That way we can get mouse position as if it's Absolute position                
                if (PlayerController.AxisController.DeviceType == RawInputDeviceType.RIM_TYPEMOUSE)
                {
                    if (!PlayerController.AxisController.IsAbsoluteAxis)
                    {
                        Logger.WriteLog(" > Relative positionning detected, switching to cursor position :");
                        Win32.POINT p = new Win32.POINT();
                        if (Win32.GetCursorPos(out p))
                        {
                            Logger.WriteLog(" > WM_MOUSEMOVE Cursor position = [ " + p.X + "," + p.Y + " ]");
                            PlayerController.Computed_X = p.X;
                            PlayerController.Computed_Y = p.Y;
                            Logger.WriteLog(" > OnScreen Cursor Position (Px) = [ " + p.X + ", " + p.X + " ]");
                        }
                        else
                        {
                            Logger.WriteLog(" > WM_MOUSEMOVE : GetCursorPos() returned error");
                            return;
                        }
                    }
                    else
                    {
                        PlayerController.Computed_X = ScreenScale(PlayerController.RawX, (int)AxesConfig.X.MinValue, (int)AxesConfig.X.MaxValue, 0, _DesktopScreenWidth);
                        PlayerController.Computed_Y = ScreenScale(PlayerController.RawY, (int)AxesConfig.Y.MinValue, (int)AxesConfig.Y.MaxValue, 0, _DesktopScreenHeight);
                    }
                }

                //Overriding Analog guns calibration and invert axis if necessary
                if (AxesConfig != null && PlayerController.AxisController.DeviceType == RawInputDeviceType.RIM_TYPEHID)
                {
                    if (AxesConfig.OverrideCalibration)
                    {
                        Logger.WriteLog(" > Overriding player axis range values : X => [ " + AxesConfig.X.MinValue.ToString() + ", " + AxesConfig.X.MaxValue.ToString() + " ], Y => [ " + AxesConfig.Y.MinValue.ToString() + ", " + AxesConfig.Y.MaxValue.ToString() + " ]");
                        PlayerController.Computed_X = ScreenScale(PlayerController.RawX, (int)AxesConfig.X.MinValue, (int)AxesConfig.X.MaxValue, 0, _DesktopScreenWidth);
                        PlayerController.Computed_Y = ScreenScale(PlayerController.RawY, (int)AxesConfig.Y.MinValue, (int)AxesConfig.Y.MaxValue, 0, _DesktopScreenHeight);
                    }
                    else
                    {
                        PlayerController.Computed_X = ScreenScale(PlayerController.RawX, (int)PlayerController.AxisController.Axes[AxesConfig.X.ID].MinValue, (int)PlayerController.AxisController.Axes[AxesConfig.X.ID].MaxValue, 0, _DesktopScreenWidth);
                        PlayerController.Computed_Y = ScreenScale(PlayerController.RawY, (int)PlayerController.AxisController.Axes[AxesConfig.Y.ID].MinValue, (int)PlayerController.AxisController.Axes[AxesConfig.Y.ID].MaxValue, 0, _DesktopScreenHeight);
                    }

                    //Optionnal invert axis
                    if (AxesConfig.X.IsInverted)
                        PlayerController.Computed_X = _DesktopScreenWidth - PlayerController.Computed_X;
                    if (AxesConfig.Y.IsInverted)
                        PlayerController.Computed_Y = _DesktopScreenWidth - PlayerController.Computed_Y;
                }

                //Change X asxis scaling based on user requirements
                if (_AxisX_ScaleFactor != 1.0)
                {
                    Logger.WriteLog(" > Forcing X Scaling Factor = " + _AxisX_ScaleFactor.ToString());
                    double HalfScreenSize = (double)_DesktopScreenWidth / 2.0;
                    double NewX = (((double)PlayerController.Computed_X - HalfScreenSize) * _AxisX_ScaleFactor) + HalfScreenSize;
                    PlayerController.Computed_X = Convert.ToInt32(NewX);
                    Logger.WriteLog(" > Forced scaled OnScreen Cursor Position (Px) = [ " + PlayerController.Computed_X + ", " + PlayerController.Computed_Y + " ]");
                }

                Logger.WriteLog(" > OnScreen Cursor Position (Px) = [ " + PlayerController.Computed_X + ", " + PlayerController.Computed_Y + " ]");

                _IsFullscreen = GetFullscreenStatus();
                //if (!_IsFullscreen)
                //    Logger.WriteLog(" > ClientWindow Style = Windowed");
                //else
                //    Logger.WriteLog(" > ClientWindow Style = FullScreen");
                //Logger.WriteLog(" > ClientWindow Style = Windowed");

                ////Retrieve info for debug/replay purpose
                //GetClientwindowInfo();

                //if (!ClientScale(PlayerController))
                //{
                //    Logger.WriteLog(" > Error converting screen location to client location");
                //    return;
                //}
                //Logger.WriteLog(" > ClientWindow Location (px) = [ " + _ClientWindowLocation.X.ToString() + ", " + _ClientWindowLocation.Y.ToString() + " ]");
                //Logger.WriteLog(" > ClientWindow Size (px) = [ " + (_WindowRect.Right - _WindowRect.Left).ToString() + "x" + (_WindowRect.Bottom - _WindowRect.Top).ToString() + " ]");
                //Logger.WriteLog(" > OnClient Cursor Position (Px) = [ " + PlayerController.Computed_X.ToString() + ", " + PlayerController.Computed_Y.ToString() + " ]");

                //if (!GetClientRect())
                //{
                //    Logger.WriteLog(" > Error getting client Rect");
                //    return;
                //}

                if (!_IsFullscreen)
                {
                    Logger.WriteLog(" > ClientWindow Style = Windowed");

                    //Retrieve info for debug/replay purpose
                    GetClientwindowInfo();

                    if (!ClientScale(PlayerController))
                    {
                        Logger.WriteLog(" > Error converting screen location to client location");
                        return;
                    }
                    Logger.WriteLog(" > ClientWindow Location (px) = [ " + _ClientWindowLocation.X.ToString() + ", " + _ClientWindowLocation.Y.ToString() + " ]");
                    Logger.WriteLog(" > ClientWindow Size (px) = [ " + (_WindowRect.Right - _WindowRect.Left).ToString() + "x" + (_WindowRect.Bottom - _WindowRect.Top).ToString() + " ]");
                    Logger.WriteLog(" > OnClient Cursor Position (Px) = [ " + PlayerController.Computed_X.ToString() + ", " + PlayerController.Computed_Y.ToString() + " ]");

                    if (!GetClientRect())
                    {
                        Logger.WriteLog(" > Error getting client Rect");
                        return;
                    }
                }
                else
                {
                    Logger.WriteLog(" > ClientWindow Style = FullScreen");

                    //No need to translate coordinates from screen -> client and risk error.
                    //As fullscreen, we will consider window size = screen size

                    Win32.Rect r = new Win32.Rect
                    {
                        Top = 0,
                        Left = 0,
                        Bottom = _DesktopScreenHeight,
                        Right = _DesktopScreenWidth
                    };
                    _ClientRect = r;
                }

                if (!GameScale(PlayerController))
                {
                    Logger.WriteLog(" > Error converting client location to game location");
                    return;
                }

                Logger.WriteLog(" > Game Position (Hex) = [ " + PlayerController.Computed_X.ToString("X4") + ", " + PlayerController.Computed_Y.ToString("X4") + " ]");
                Logger.WriteLog(" > Game Position (Dec) = [ " + PlayerController.Computed_X.ToString() + ", " + PlayerController.Computed_Y.ToString() + " ]");
               
                Logger.WriteLog("-");
                
                UpdateAxis(e.Id, PlayerController.Computed_X, PlayerController.Computed_Y);
            }
        }

        protected virtual void UpdateAxis(PlayerID Id, int X, int Y)
        { }

        protected virtual void GameController_OnButtonDown(object sender, VirtualGameControler.ButtonDownEventArgs e)
        { }

        protected virtual void GameController_OnButtonUp(object sender, VirtualGameControler.ButtonDownEventArgs e)
        { }      

        protected virtual void PlayerController_OnButtonDown(object sender, VirtualGameControler.ButtonDownEventArgs e)
        { }

        protected virtual void PlayerController_OnButtonUp(object sender, VirtualGameControler.ButtonDownEventArgs e)
        { }

        #endregion

        #region MAME-Like Outputs

        protected virtual void CreateOutputList()
        {
            _Outputs = new List<GameOutput>();
        }

        public virtual void UpdateOutputValues()
        { }

        public void SetMamePauseState(bool PauseState)
        {
            if (PauseState)
                SetOutputValue(OutputId.MamePause, 1);
            else
                SetOutputValue(OutputId.MamePause, 0);
        }

        /// <summary>
        /// Return a GameOutput object corresponding to a desired GameOutputId
        /// </summary>
        /// <param name="Id">Desired GameOutputId</param>
        /// <returns>Desired GameOutput object</returns>
        protected GameOutput GetOutputById(OutputId Id)
        {
            foreach (GameOutput CurrentOutput in _Outputs)
            {
                if (CurrentOutput.Id == (uint)Id)
                    return CurrentOutput;
            }
            return null;
        }

        /// <summary>
        /// Update a value for the desired GameOutput
        /// </summary>
        /// <param name="Id">GameOutput Id to update</param>
        /// <param name="Value">Value to update the GameOutput object</param>
        protected void SetOutputValue(OutputId Id, int Value)
        {
            foreach (GameOutput CurrentOutput in _Outputs)
            {
                if (CurrentOutput.Id == (uint)Id)
                {
                    CurrentOutput.OutputValue = Value;
                    break;
                }
            }
        }

        /// <summary>
        /// Return the Text description for a desired Id
        /// </summary>
        /// <param name="Id">Desired GameOutput Id</param>
        /// <returns>GameOutput string description</returns>
        public string GetOutputDescriptionFromId(uint Id)
        {
            foreach (GameOutput o in _Outputs)
            {
                if (o.Id == Id)
                    return o.Name;
            }
            return String.Empty;
        }

        #endregion
    }
}
