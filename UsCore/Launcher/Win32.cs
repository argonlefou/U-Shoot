using System;
using System.Runtime.InteropServices;
using System.Text;

namespace UsCore.Launcher
{
    public enum DebugEventType : uint
    {
        RIP_EVENT = 9,
        OUTPUT_DEBUG_STRING_EVENT = 8,
        UNLOAD_DLL_DEBUG_EVENT = 7,
        LOAD_DLL_DEBUG_EVENT = 6,
        EXIT_PROCESS_DEBUG_EVENT = 5,
        EXIT_THREAD_DEBUG_EVENT = 4,
        CREATE_PROCESS_DEBUG_EVENT = 3,
        CREATE_THREAD_DEBUG_EVENT = 2,
        EXCEPTION_DEBUG_EVENT = 1,
    }

    #region DEBUG_EVENT Pinvoke

    [StructLayout(LayoutKind.Sequential)]
    public struct DEBUG_EVENT
    {
        public DebugEventType dwDebugEventCode;
        public uint dwProcessId;
        public uint dwThreadId;
        public DEBUG_EVENT_UNION u;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct DEBUG_EVENT_UNION
    {
        [FieldOffset(0)] public EXCEPTION_DEBUG_INFO Exception;
        [FieldOffset(0)] public CREATE_THREAD_DEBUG_INFO CreateThread;
        [FieldOffset(0)] public CREATE_PROCESS_DEBUG_INFO CreateProcessInfo;
        [FieldOffset(0)] public EXIT_THREAD_DEBUG_INFO ExitThread;
        [FieldOffset(0)] public EXIT_PROCESS_DEBUG_INFO ExitProcess;
        [FieldOffset(0)] public LOAD_DLL_DEBUG_INFO LoadDll;
        [FieldOffset(0)] public UNLOAD_DLL_DEBUG_INFO UnloadDll;
        [FieldOffset(0)] public OUTPUT_DEBUG_STRING_INFO DebugString;
        [FieldOffset(0)] public RIP_INFO RipInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct EXCEPTION_DEBUG_INFO
    {
        public EXCEPTION_RECORD ExceptionRecord;
        public uint dwFirstChance;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct EXCEPTION_RECORD
    {
        public uint ExceptionCode;
        public uint ExceptionFlags;
        public IntPtr ExceptionRecordPtr;
        public IntPtr ExceptionAddress;
        public uint NumberParameters;

        public UIntPtr Info0, Info1, Info2, Info3, Info4,
                       Info5, Info6, Info7, Info8, Info9,
                       Info10, Info11, Info12, Info13, Info14;

        public ulong GetParameter(int index)
        {
            if (index < 0 || index >= 15 || (uint)index >= NumberParameters)
                throw new ArgumentOutOfRangeException("index");

            switch (index)
            {
                case 0: return Info0.ToUInt64();
                case 1: return Info1.ToUInt64();
                case 2: return Info2.ToUInt64();
                case 3: return Info3.ToUInt64();
                case 4: return Info4.ToUInt64();
                case 5: return Info5.ToUInt64();
                case 6: return Info6.ToUInt64();
                case 7: return Info7.ToUInt64();
                case 8: return Info8.ToUInt64();
                case 9: return Info9.ToUInt64();
                case 10: return Info10.ToUInt64();
                case 11: return Info11.ToUInt64();
                case 12: return Info12.ToUInt64();
                case 13: return Info13.ToUInt64();
                default: return Info14.ToUInt64();
            }
        }
    }


    [StructLayout(LayoutKind.Sequential)]
    public struct CREATE_THREAD_DEBUG_INFO
    {
        public IntPtr hThread;
        public IntPtr lpThreadLocalBase;
        public IntPtr lpStartAddress;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct CREATE_PROCESS_DEBUG_INFO
    {
        public IntPtr hFile;
        public IntPtr hProcess;
        public IntPtr hThread;
        public IntPtr lpBaseOfImage;
        public uint dwDebugInfoFileOffset;
        public uint nDebugInfoSize;
        public IntPtr lpThreadLocalBase;
        public IntPtr lpStartAddress;
        public IntPtr lpImageName;
        public ushort fUnicode;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct EXIT_THREAD_DEBUG_INFO
    {
        public uint dwExitCode;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct EXIT_PROCESS_DEBUG_INFO
    {
        public uint dwExitCode;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct LOAD_DLL_DEBUG_INFO
    {
        public IntPtr hFile;
        public IntPtr lpBaseOfDll;
        public uint dwDebugInfoFileOffset;
        public uint nDebugInfoSize;
        public IntPtr lpImageName;
        public ushort fUnicode;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct UNLOAD_DLL_DEBUG_INFO
    {
        public IntPtr lpBaseOfDll;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct OUTPUT_DEBUG_STRING_INFO
    {
        public IntPtr lpDebugStringData;
        public ushort fUnicode;
        public ushort nDebugStringLength;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RIP_INFO
    {
        public uint dwError;
        public uint dwType;
    }
    
    #endregion

    internal class Win32
    {
        #region Debugger WIN32 functions

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool CreateProcess(
            string lpApplicationName,
            StringBuilder lpCommandLine,   // must be writable, NOT a plain string
            IntPtr lpProcessAttributes,
            IntPtr lpThreadAttributes,
            bool bInheritHandles,
            uint dwCreationFlags,
            IntPtr lpEnvironment,
            string lpCurrentDirectory,
            [In] ref STARTUPINFO lpStartupInfo,
            out PROCESS_INFORMATION lpProcessInformation);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool IsWow64Process2(IntPtr process, out ushort processMachine, out ushort nativeMachine);

        [DllImport("kernel32.dll", EntryPoint = "WaitForDebugEvent")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WaitForDebugEvent(out DEBUG_EVENT lpDebugEvent, uint dwMilliseconds);

        [DllImport("kernel32.dll", EntryPoint = "WaitForDebugEvent", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WaitForDebugEvent(IntPtr lpDebugEvent, uint dwMilliseconds);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool ContinueDebugEvent(uint dwProcessId, uint dwThreadId, ContinueStatus dwContinueStatus);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool DebugActiveProcessStop(uint dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, UIntPtr dwSize, ref UIntPtr lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, UIntPtr dwSize, ref UIntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll")]
        public static extern long GetFileSizeEx(IntPtr hFile, out long lpFileSize);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr CreateFileMapping(IntPtr hFile, IntPtr lpFileMappingAttributes, PageProtection flProtect, uint dwMaximumSizeHigh, uint dwMaximumSizeLow, string lpName);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr MapViewOfFile(IntPtr hFileMappingObject, FileMapAccess dwDesiredAccess, UInt32 dwFileOffsetHigh, UInt32 dwFileOffsetLow, UIntPtr dwNumberOfBytesToMap);

        [DllImport("Psapi.dll", EntryPoint = "GetMappedFileNameW", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        public static extern int GetMappedFileName(IntPtr hProcess, IntPtr lpv, [Out] StringBuilder lpFilename, short nSize);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr GetCurrentProcess();

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

        [DllImport("kernel32.dll", EntryPoint = "GetThreadContext", SetLastError = true)]
        public static extern bool GetThreadContext(IntPtr hThread, IntPtr lpContext);

        [DllImport("kernel32.dll", EntryPoint = "SetThreadContext", SetLastError = true)]
        public static extern bool SetThreadContext(IntPtr hThread, IntPtr lpContext);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool Wow64GetThreadContext(IntPtr hThread, IntPtr lpContext);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool Wow64SetThreadContext(IntPtr hThread, IntPtr lpContext);

        //
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenThread(ThreadAccess dwDesiredAccess, bool bInheritHandle, uint dwThreadId);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern uint SuspendThread(IntPtr hThread);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern int ResumeThread(IntPtr hThread);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool FlushInstructionCache(IntPtr hProcess, IntPtr lpBaseAddress, UIntPtr dwSize);

        [DllImport("ntdll.dll", SetLastError = true)]
        public static extern int NtQueryInformationProcess(IntPtr processHandle, int processInformationClass, ref PROCESS_BASIC_INFORMATION processInformation, int processInformationLength, out int returnLength);

        [DllImport("ntdll.dll", SetLastError = true)]
        public static extern int NtQueryInformationProcess(IntPtr processHandle, int processInformationClass, ref IntPtr processInformation, int processInformationLength, out int returnLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool VirtualProtectEx(IntPtr hProcess, IntPtr lpAddress, UIntPtr dwSize, uint flNewProtect, out uint lpflOldProtect);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr CreateFile(string lpFileName, uint dwDesiredAccess, uint dwShareMode, ref SECURITY_ATTRIBUTES lpSecurityAttributes, uint dwCreationDisposition, uint dwFlagsAndAttributes, IntPtr hTemplateFile);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern uint GetFinalPathNameByHandleW(IntPtr hFile, StringBuilder path, uint len, uint flags);

        [DllImport("psapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern uint GetMappedFileNameW(IntPtr hProcess, IntPtr lpv, StringBuilder name, uint size);



        #endregion

        #region Win32 Struct

        [StructLayout(LayoutKind.Sequential)]
        public struct SECURITY_ATTRIBUTES
        {
            public int nLength;
            public IntPtr lpSecurityDescriptor;
            public bool bInheritHandle;
        }

        // This also works with CharSet.Ansi as long as the calling function uses the same character set.
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct STARTUPINFOEX
        {
            public STARTUPINFO StartupInfo;
            public IntPtr lpAttributeList;
        }

        // This also works with CharSet.Ansi as long as the calling function uses the same character set.
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct STARTUPINFO
        {
            public Int32 cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
            public Int32 dwX;
            public Int32 dwY;
            public Int32 dwXSize;
            public Int32 dwYSize;
            public Int32 dwXCountChars;
            public Int32 dwYCountChars;
            public Int32 dwFillAttribute;
            public Int32 dwFlags;
            public Int16 wShowWindow;
            public Int16 cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public int dwProcessId;
            public int dwThreadId;
        }

        // Add these to your Win32 class
        [StructLayout(LayoutKind.Sequential)]
        public struct PROCESS_BASIC_INFORMATION
        {
            public IntPtr Reserved1;
            public IntPtr PebBaseAddress;
            public IntPtr Reserved2_0;
            public IntPtr Reserved2_1;
            public IntPtr UniqueProcessId;
            public IntPtr Reserved3;
        }

        #endregion

        #region Win32 Constants

        public enum ContinueStatus : uint
        {
            DBG_CONTINUE = 0x00010002,
            DBG_EXCEPTION_NOT_HANDLED = 0x80010001,
            DBG_REPLY_LATER = 0x40010001
        }

        [Flags]
        public enum PageProtection : uint
        {
            NoAccess = 0x01,
            Readonly = 0x02,
            ReadWrite = 0x04,
            WriteCopy = 0x08,
            Execute = 0x10,
            ExecuteRead = 0x20,
            ExecuteReadWrite = 0x40,
            ExecuteWriteCopy = 0x80,
            Guard = 0x100,
            NoCache = 0x200,
            WriteCombine = 0x400,
        }

        [Flags]
        public enum FileMapAccess : uint
        {
            FileMapCopy = 0x0001,
            FileMapWrite = 0x0002,
            FileMapRead = 0x0004,
            FileMapAllAccess = 0x001f,
            FileMapExecute = 0x0020,
        }

        [Flags]
        public enum ThreadAccess : int
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

        public const uint EXCEPTION_DEBUG_EVENT = 1;
        public const UInt32 EXCEPTION_ACCESS_VIOLATION = 0xC0000005;
        public const UInt32 EXCEPTION_BREAKPOINT = 0x80000003;
        public const UInt32 EXCEPTION_DATATYPE_MISALIGNMENT = 0x80000002;
        public const UInt32 EXCEPTION_SINGLE_STEP = 0x80000004;
        public const UInt32 EXCEPTION_ARRAY_BOUNDS_EXCEEDED = 0xC000008C;
        public const UInt32 EXCEPTION_INT_DIVIDE_BY_ZERO = 0xC0000094;
        public const UInt32 STATUS_WX86_BREAKPOINT = 0x4000001F;
        public const UInt32 DBG_CONTROL_C = 0x40010006;
        public const UInt32 MS_VC_EXCEPTION = 0x406D1388;


        public const uint STATUS_BREAKPOINT = 0x80000003;

        public const uint DEBUG_PROCESS = 0x00000001;
        public const uint DEBUG_ONLY_THIS_PROCESS = 0x00000002;
        public const uint CREATE_SUSPENDED = 0x00000004;
        public const uint CREATE_UNICODE_ENVIRONMENT = 0x00000400;
        public const UInt32 INFINITE = 0xffffffff;
        public const int ERROR_SEM_TIMEOUT = 121;

        public const uint CONTEXT_CONTROL = 0x00100001;
        public const uint CONTEXT_INTEGER = 0x00100002;
        public const uint DBG_CONTINUE = 0x00010002;

        public const uint PAGE_EXECUTE_READWRITE = 0x40;

        //dwFlag
        public const uint STARTF_USESTDHANDLES = 0x00000100;       
        public const uint CREATE_NO_WINDOW = 0x08000000;     //Can be used to hide console window at start

        public const uint GENERIC_WRITE = 0x40000000;
        public const uint FILE_SHARE_WRITE = 0x00000002;
        public const uint OPEN_EXISTING = 3;

        #endregion
    }
}
