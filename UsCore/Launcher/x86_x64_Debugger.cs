using System;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using UsCore.Logging;

namespace UsCore.Launcher
{
    public class x86_x64_Debugger
    {
        private enum IMAGE_FILE_MACHINE
        {
            UNKNOWN = 0x0000,
            TARGET_HOST = 0x0001,
            I386 = 0x014C,
            R3000 = 0x0162, //MIPS little-endian, 0x160 big-endian
            R4000 = 0x0166, //MIPS little-endian
            R10000 = 0x0168, //MIPS little-endian
            WCEMIPSV2 = 0x0169, //MIPS little-endian WCE v2
            ALPHA = 0x0184, //Alpha_AXP
            SH3 = 0x01a2, //SH3 little-endian
            SH3DSP = 0x01a3, //SH3DSP
            SH3E = 0x01a4, //SH3E little-endian
            SH4 = 0x01a6, //SH4 little-endian
            SH5 = 0x01a8, //SH5
            ARM = 0x01c0, //Little-Endian ARM
            THUMB = 0x01c2, //Arm Thumb/Thumb-2 Little-Endian
            ARMNT = 0x01c4, //Little-Endian ARM Thumb-2, (Cette constante est disponible à partir de Windows 7 et Windows Server 2008 R2.)
            AM33 = 0x01d3, //TAM33BD
            POWERPC = 0x01F0, //IBM PowerPC Little-Endian
            POWERPCFP = 0x01f1, //POWERPCFP
            IA64 = 0x0200, //Intel 64
            MIPS16 = 0x0266, //MIPS
            ALPHA64 = 0x0284, //ALPHA64
            MIPSFPU = 0x0366, //MIPS
            MIPSFPU16 = 0x0466, //MIPS
            AXP64 = 0x0284, //AXP64
            TRICORE = 0x0520, //Infineon
            CEF = 0x0CEF, //CEF
            EBC = 0x0EBC, //Code d’octet EFI
            AMD64 = 0x8664, //AMD64 (K8)
            M32R = 0x9041, //M32R little-endian
            ARM64 = 0xAA64, //Little-Endian ARM64, (Cette constante est disponible à partir de Windows 8.1 et Windows Server 2012 R2.)
            CEE = 0xC0EE
        }

        public UInt32 CreatedProcessId { get; private set; }
        public IntPtr CreatedProcessHandle { get; private set; }

        private Thread _DebuggerThread;

        private string _TargetPath;
        private string _CmdlineArguments = string.Empty;

        //Handle to a NULL file to redirect outputs if needed
        private IntPtr _hNul = IntPtr.Zero;

        //Default flags
        private bool _bInheritHandle = false;
        private UInt32 _dwCreationFlags = Win32.DEBUG_ONLY_THIS_PROCESS | Win32.CREATE_SUSPENDED | Win32.CREATE_UNICODE_ENVIRONMENT;
        private IMAGE_FILE_MACHINE _ProcessMachine = IMAGE_FILE_MACHINE.UNKNOWN;
        private IMAGE_FILE_MACHINE _NativeMachine = IMAGE_FILE_MACHINE.UNKNOWN;
        private volatile bool _ContinueDebugging = true;
        //Options
        private bool _RedirectOutputsToNull = false;
        private bool _CreateNoWindow = false;
        private bool _DisableDebugger = false;
        private bool _AddEnvironmentVariableBlock = true;
        //Accessors
        public bool RedirectOutputsToNull
        {
            get { return _RedirectOutputsToNull; }
            set { _RedirectOutputsToNull = value; }
        }
        public bool CreateNoWindow
        {
            get { return _CreateNoWindow; }
            set { _CreateNoWindow = value; }
        }
        public bool DisableDebugger
        {
            get { return _DisableDebugger; }
            set { _DisableDebugger = value; }
        }
        public bool AddEnvironmentVariableBlock
        {
            get { return _AddEnvironmentVariableBlock; }
            set { _AddEnvironmentVariableBlock = value; }
        }

        private bool _NativeInitialBreakpointSeen = false;
        private bool _Wx86InitialBreakpointSeen = false;

        /// <summary>
        /// Changing the HEX display formating based on the targetted process architecture:
        /// </summary>
        public string HexStringFormat
        {
            get
            {
                switch (_ProcessMachine)
                {
                    case IMAGE_FILE_MACHINE.I386: return "X8";
                    default: return "X16";
                }
            }
        }

        public x86_x64_Debugger(string Target_Path, string Arguments = null)
        {
            _TargetPath = Target_Path;
            _DebuggerThread = new Thread(new ThreadStart(DebuggerThread));
            
        }
        public void Set_CmdLine_Args(string StrArgs)
        {
            _CmdlineArguments = StrArgs;
        }
        public void Add_CmdLine_Arg(string strArg, bool SurroundQuotes = false)
        {
            _CmdlineArguments += SurroundQuotes ? ("\"" + strArg + "\"") : strArg;
            _CmdlineArguments += " ";
        }
       
        public void Start()
        {
            _DebuggerThread.Start();
        }

        public void DetachDebugger()
        {
            _ContinueDebugging = false;            
        }

        private void DebuggerThread()
        {          
            Win32.PROCESS_INFORMATION pInfo = new Win32.PROCESS_INFORMATION();
            Win32.STARTUPINFO sInfo = new Win32.STARTUPINFO();
            sInfo.cb = Marshal.SizeOf(typeof(Win32.STARTUPINFO));

            // Specific for Aliens Extermination 
            //glutInitWithExit is GLUT's Win32 initialization routine, and there's a well - known quirk in classic GLUT/ freeglut Win32 ports: during init,
            //it calls GetStartupInfo() to read the STARTUPINFO your caller supplied to CreateProcess,
            //specifically checking dwFlags & STARTF_USESHOWWINDOW and wShowWindow,
            //to decide the initial window state(normal / iconic / hidden).
            //
            //ShellExecuteEx internally builds its own STARTUPINFO when it calls CreateProcess under the hood,
            //and it does set STARTF_USESHOWWINDOW with wShowWindow = nShow.
            //Raw CreateProcess call never sets that flag at all — so GLUT's startup-info-reading code takes a different,
            //apparently buggy, branch when the flag is absent versus present.
            const int STARTF_USESHOWWINDOW = 0x00000001;
            const short SW_SHOWNORMAL = 1;
            sInfo.dwFlags = STARTF_USESHOWWINDOW;   // <-- new
            sInfo.wShowWindow = SW_SHOWNORMAL;      // <-- new
            // <---------- KEEPING IT AS DEFAULT ???? -------->


            /* If Security Attributes are needed instead of IntPtr.Zero */
            //Win32.SECURITY_ATTRIBUTES pSec = new Win32.SECURITY_ATTRIBUTES();
            //Win32.SECURITY_ATTRIBUTES tSec = new Win32.SECURITY_ATTRIBUTES();
            //pSec.nLength = Marshal.SizeOf(pSec);
            //tSec.nLength = Marshal.SizeOf(tSec);

            string TargetFolder = Path.GetDirectoryName(_TargetPath);
            Logger.WriteLog("[DebuggerThread] Trying to start: " + _TargetPath);
            Logger.WriteLog("[DebuggerThread] Command line args: " + _CmdlineArguments);
            Logger.WriteLog("[DebuggerThread] Target directory : " + TargetFolder);

            if (!File.Exists(_TargetPath))
            {
                Logger.WriteLog("[DebuggerThread] Error ! Program file not found: " + _TargetPath);
                Environment.Exit(0);
            }

            //Some games will write a lot of things to the default console (mainly Linux emulators)
            //Redirecting outputs to null will speed up the booting process (A LOT !)
            if (_RedirectOutputsToNull)
            {
                Win32.SECURITY_ATTRIBUTES sa = new Win32.SECURITY_ATTRIBUTES
                {
                    nLength = Marshal.SizeOf(typeof(Win32.SECURITY_ATTRIBUTES)),
                    lpSecurityDescriptor = IntPtr.Zero,
                    bInheritHandle = true          // handle must be inheritable
                };

                _hNul = Win32.CreateFile("NUL", Win32.GENERIC_WRITE, Win32.FILE_SHARE_WRITE, ref sa, Win32.OPEN_EXISTING, 0, IntPtr.Zero);

                if (_hNul == new IntPtr(-1))
                {
                    Logger.WriteLog("[DebuggerThread] Error ! Can't get handle to NUL file for redirecting stderr and stdout");
                    Environment.Exit(0);
                }

                sInfo.dwFlags |= (int)Win32.STARTF_USESTDHANDLES;
                sInfo.hStdOutput = _hNul;
                sInfo.hStdError = _hNul;

                _bInheritHandle = true;
            }

            //Some process will be started as a cmdline window and then run the graphics, using that option can hide the command window
            if (_CreateNoWindow)
                _dwCreationFlags |= Win32.CREATE_NO_WINDOW;

            //Some process will not need the debugger function so let thze process run normally
            if (_DisableDebugger)
                _dwCreationFlags &= ~Win32.DEBUG_ONLY_THIS_PROCESS;

            //Process ned to be created with SUSPENDED state to change PDB data before it can cheeck for a debugger attached
            //lpApplicationName is Null so lpCommandLine is full command line : exe path + arguments AND exe path must be quoted if it has  !
            StringBuilder cmdLine = new StringBuilder("\"" + _TargetPath + "\" " + _CmdlineArguments);
            IntPtr pEnv = _AddEnvironmentVariableBlock ? BuildEnvironmentBlock() : IntPtr.Zero;
            bool bRetCode = false;
            try
            {
                bRetCode = Win32.CreateProcess(null, cmdLine, IntPtr.Zero, IntPtr.Zero, _bInheritHandle, _dwCreationFlags, pEnv, TargetFolder, ref sInfo, out pInfo);
            }
            catch (Exception ex)
            {
                Logger.WriteLog("[DebuggerThread] Error ! CreateProcess() failed");
                Logger.WriteLog(ex.Message.ToString());
            }
            finally
            {
                if (_hNul != IntPtr.Zero)
                    Win32.CloseHandle(_hNul);

                if (pEnv != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(pEnv);
                    pEnv = IntPtr.Zero;
                }
            }

            if (!bRetCode)
            {
                Logger.WriteLog(String.Format("Cannot start {0} file for debugging.", _TargetPath));
                //Logger.WriteLog(String.Format("Cannot start {0} file for debugging. Error: {1}", _TargetPath, (int)Win32API.GetLastError()));
                return;
            }
            else
            {
                Logger.WriteLog("[DebuggerThread] Process succesfully created: pid=" + pInfo.dwProcessId + ", hProcess=" + pInfo.hProcess.ToString());
                CreatedProcessId = (UInt32)pInfo.dwProcessId;
                CreatedProcessHandle = pInfo.hProcess;

                ushort pMachine, nMachine;
                Win32.IsWow64Process2(pInfo.hProcess, out pMachine, out nMachine);
                _ProcessMachine = (IMAGE_FILE_MACHINE)pMachine;
                _NativeMachine = (IMAGE_FILE_MACHINE)nMachine;
                Logger.WriteLog("IsWow64Process2() returned: ProcessMachine=" + _ProcessMachine.ToString() + ", NativeMachine=" + _NativeMachine.ToString());

                if (OnProcessCreated != null)
                    OnProcessCreated(this, EventArgs.Empty);

                //Resume process and start debugging
                Win32.ResumeThread(pInfo.hThread);
                Win32.CloseHandle(pInfo.hThread);

                //No need to loop for events if not debugging
                if (_DisableDebugger)
                    return;
            }

            DEBUG_EVENT _DebugEvent;
            int NativeDebugEventSize = IntPtr.Size == 8 ? 0xB0 : 0x60;
            IntPtr debugEventBuffer = Marshal.AllocHGlobal(NativeDebugEventSize);
            try
            {
                while (_ContinueDebugging)
                {
                    bool bb = Win32.WaitForDebugEvent(debugEventBuffer, 100);
                    //bool bb = Win32.WaitForDebugEvent(out _DebugEvent, Win32.INFINITE);
                    if (!bb)
                    {
                        int err = Marshal.GetLastWin32Error();
                        if (err == Win32.ERROR_SEM_TIMEOUT)
                            continue;                       

                        Logger.WriteLog("[DebuggerThread] WaitForDebugEvent failed, error " + err);
                        break;
                    }
                    else
                    {
                        _DebugEvent = Marshal.PtrToStructure<DEBUG_EVENT>(debugEventBuffer);
                        //Logger.WriteLog(" -> " + _DebugEvent.dwDebugEventCode.ToString());
                        Win32.ContinueStatus Status = Win32.ContinueStatus.DBG_CONTINUE;
                        switch (_DebugEvent.dwDebugEventCode)
                        {
                            case DebugEventType.CREATE_PROCESS_DEBUG_EVENT:
                                {
                                    CreatedProcessId = _DebugEvent.dwProcessId;
                                    IntPtr hProcessFile = _DebugEvent.u.CreateProcessInfo.hFile;
                                    try
                                    {
                                        if (OnCreateProcess != null)
                                            OnCreateProcess(this, new CreateProcessDebugEventArgs(_DebugEvent.dwProcessId, _DebugEvent.u.CreateProcessInfo));
                                    }
                                    finally
                                    {
                                        if (hProcessFile != IntPtr.Zero && hProcessFile != new IntPtr(-1))
                                            Win32.CloseHandle(hProcessFile);
                                    }
                                }
                                break;
                            case DebugEventType.CREATE_THREAD_DEBUG_EVENT:
                                {
                                    if (OnCreateThread != null)
                                        OnCreateThread(this, new CreateThreadDebugEventArgs(_DebugEvent.dwThreadId, _DebugEvent.u.CreateThread));
                                }
                                break;
                            case DebugEventType.EXCEPTION_DEBUG_EVENT:
                                {
                                    // Thread naming exception, must handle this on priority
                                    if (_DebugEvent.u.Exception.ExceptionRecord.ExceptionCode == Win32.MS_VC_EXCEPTION)
                                    {
                                        Status = Win32.ContinueStatus.DBG_EXCEPTION_NOT_HANDLED;
                                        break; // skip ALL processing
                                    }

                                    var ex = _DebugEvent.u.Exception.ExceptionRecord;
                                    uint dwFirstChance = _DebugEvent.u.Exception.dwFirstChance;

                                    Logger.WriteLog("[EXCEPTION_DEBUG_EVENT]");
                                    Logger.WriteLog(" > Exception Code: 0x" + ex.ExceptionCode.ToString("X8"));
                                    Logger.WriteLog(" > Exception Address: 0x" + ex.ExceptionAddress.ToString(HexStringFormat));
                                    Logger.WriteLog(" > dwFirstChance: " + dwFirstChance);
                                    Logger.WriteLog(" > ThreadID: " + _DebugEvent.dwThreadId.ToString());

                                    for (int i = 0; i < Math.Min(ex.NumberParameters, 15u); i++)
                                        Logger.WriteLog($"   Param[{i}] = 0x{ex.GetParameter(i):X}");

                                    var exArgs = new ExceptionDebugEventArgs(_DebugEvent.dwThreadId, _DebugEvent.u.Exception);
                                    if (OnException != null)
                                        OnException(this, exArgs);

                                    switch (ex.ExceptionCode)
                                    {
                                        case Win32.EXCEPTION_BREAKPOINT:
                                            {
                                                if (!_NativeInitialBreakpointSeen)
                                                {
                                                    _NativeInitialBreakpointSeen = true;
                                                    Status = Win32.ContinueStatus.DBG_CONTINUE;
                                                }
                                                else
                                                    Status = exArgs.Handled ? Win32.ContinueStatus.DBG_CONTINUE : Win32.ContinueStatus.DBG_EXCEPTION_NOT_HANDLED;
                                            }break;

                                        case Win32.STATUS_WX86_BREAKPOINT:
                                            {
                                                if (!_Wx86InitialBreakpointSeen)
                                                {
                                                    _Wx86InitialBreakpointSeen = true;
                                                    Status = Win32.ContinueStatus.DBG_CONTINUE;
                                                }
                                                else
                                                    Status = exArgs.Handled ? Win32.ContinueStatus.DBG_CONTINUE : Win32.ContinueStatus.DBG_EXCEPTION_NOT_HANDLED;
                                            }break;

                                        case Win32.EXCEPTION_SINGLE_STEP:
                                            {
                                                Status = Win32.ContinueStatus.DBG_CONTINUE;
                                            }break;

                                        default:
                                            {
                                                Status = exArgs.Handled ? Win32.ContinueStatus.DBG_CONTINUE : Win32.ContinueStatus.DBG_EXCEPTION_NOT_HANDLED;
                                            }break;
                                    }
                                }
                                break;
                            case DebugEventType.EXIT_PROCESS_DEBUG_EVENT:
                                {
                                    Win32.ContinueDebugEvent(_DebugEvent.dwProcessId, _DebugEvent.dwThreadId, Win32.ContinueStatus.DBG_CONTINUE);
                                    _ContinueDebugging = false;

                                    if (OnExitProcess != null)
                                        OnExitProcess(this, new ExitProcessDebugEventArgs(_DebugEvent.u.ExitProcess));
                                }
                                return;
                            case DebugEventType.EXIT_THREAD_DEBUG_EVENT:
                                {
                                    if (OnExitThread != null)
                                        OnExitThread(this, new ExitThreadDebugEventArgs(_DebugEvent.u.ExitThread));
                                }
                                break;
                            case DebugEventType.LOAD_DLL_DEBUG_EVENT:
                                {
                                    IntPtr hDllFile = _DebugEvent.u.LoadDll.hFile;
                                    IntPtr lpBaseOfDllLoad = _DebugEvent.u.LoadDll.lpBaseOfDll;
                                    x86_x64_Debugger.DllNameSource source;
                                    string DllPath = GetDllPath(_DebugEvent.u.LoadDll, out source);

                                    string s = "[LOAD_DLL_DEBUG_EVENT] hFile: 0x" + hDllFile.ToString("X") + ", lpBaseOfDllLoad: 0x" + lpBaseOfDllLoad.ToString(HexStringFormat);
                                    if (DllPath != string.Empty)
                                        s += ", (" + source + ") Module name: " + DllPath;
                                    else
                                        s += ", Module name: <unknown>";

                                    try
                                    {
                                        if (OnLoadedDll != null)
                                        {
                                            Logger.WriteLog(s);     
                                            OnLoadedDll(this, new LoadDllDebugEventArgs(_DebugEvent.u.LoadDll, DllPath));
                                        }
                                    }
                                    finally
                                    {
                                        if (hDllFile != IntPtr.Zero && hDllFile != new IntPtr(-1))
                                            Win32.CloseHandle(hDllFile);
                                    }
                                }
                                break;
                            case DebugEventType.OUTPUT_DEBUG_STRING_EVENT:
                                {
                                    if (OnOutputDebugString != null)
                                        OnOutputDebugString(this, new OutputDebugStringDebugEventArgs(_DebugEvent.u.DebugString));
                                }
                                break;
                            case DebugEventType.RIP_EVENT:
                                {
                                    if (OnRip != null)
                                        OnRip(this, new RipDebugEventArgs(_DebugEvent.u.RipInfo));
                                }
                                break;
                            case DebugEventType.UNLOAD_DLL_DEBUG_EVENT:
                                {
                                    if (OnUnloadedDll != null)
                                        OnUnloadedDll(this, new UnloadDllDebugEventArgs(_DebugEvent.u.UnloadDll));
                                }
                                break;
                            default: break;
                        }
                        Win32.ContinueDebugEvent(_DebugEvent.dwProcessId, _DebugEvent.dwThreadId, Status);
                    }
                }

                Win32.DebugActiveProcessStop((uint)CreatedProcessId);
                Logger.WriteLog("[DebuggerThread] Debugger has been detached from the running process");
            }
            catch (Exception Ex)
            {
                Logger.WriteLog("[DebuggerThread] Debug Messages loop error: " + Ex.Message.ToString());
            }
            finally
            {
                Marshal.FreeHGlobal(debugEventBuffer);
                if (CreatedProcessHandle != IntPtr.Zero)
                {
                    Win32.CloseHandle(CreatedProcessHandle);
                    CreatedProcessHandle = IntPtr.Zero;
                }
            }
        }

        private bool ReadBytes(IntPtr Address, byte[] Buffer)
        {
            UIntPtr Read = UIntPtr.Zero;
            return Win32.ReadProcessMemory(CreatedProcessHandle, Address, Buffer, (UIntPtr)Buffer.Length, ref Read) && Read.ToUInt64() == (ulong)Buffer.Length;
        }

        private bool WriteBytes(IntPtr Address, byte[] Buffer)
        {
            UIntPtr Written = UIntPtr.Zero;
            return Win32.WriteProcessMemory(CreatedProcessHandle, Address, Buffer,(UIntPtr)Buffer.Length, ref Written) && Written.ToUInt64() == (ulong)Buffer.Length;
        }

        private void PatchPebAt(string Label, IntPtr Peb, int ntGlobalFlagOffset, string HexFormat)
        {
            Logger.WriteLog("[DebuggerThread.PatchPEB] Found " + Label + " at address 0x" + Peb.ToString(HexFormat));

            var b = new byte[1];
            var f = new byte[4];

            // BeingDebugged (PEB offset 0x2)
            if (!ReadBytes(Peb + 0x2, b)) 
            {
                Logger.WriteLog("[DebuggerThread.PatchPEB] " + Label + ": cannot read BeingDebugged"); 
                return; 
            }
            Logger.WriteLog("[DebuggerThread.PatchPEB] " + Label + " BeingDebugged = 0x" + b[0].ToString("X2"));
            WriteBytes(Peb + 0x2, new byte[] { 0x00 });
            ReadBytes(Peb + 0x2, b);
            Logger.WriteLog("[DebuggerThread.PatchPEB] " + Label + " BeingDebugged changed to : 0x" + b[0].ToString("X2"));

            // NtGlobalFlag
            if (!ReadBytes(Peb + ntGlobalFlagOffset, f)) 
            { 
                Logger.WriteLog("[DebuggerThread.PatchPEB] " + Label + ": cannot read NtGlobalFlag"); 
                return; 
            }
            Logger.WriteLog("[DebuggerThread.PatchPEB] " + Label + " NtGlobalFlag = 0x" + BitConverter.ToInt32(f, 0).ToString("X4"));
            WriteBytes(Peb + ntGlobalFlagOffset, new byte[4]);
            ReadBytes(Peb + ntGlobalFlagOffset, f);
            Logger.WriteLog("[DebuggerThread.PatchPEB] " + Label + " NtGlobalFlag changed to : 0x" + BitConverter.ToInt32(f, 0).ToString("X4"));
        }
        public void PatchPEB()
        {
            var Pbi = new Win32.PROCESS_BASIC_INFORMATION();
            int ReturnLength;
            int Status = Win32.NtQueryInformationProcess(CreatedProcessHandle, 0, ref Pbi, Marshal.SizeOf<Win32.PROCESS_BASIC_INFORMATION>(), out ReturnLength);

            if (Status != 0)
            {
                Logger.WriteLog("[DebuggerThread.PatchPEB] NtQueryInformationProcess failed: 0x" + Status.ToString("X8"));
                return;
            }

            // Native (64-bit) PEB: for a WOW64 target this is still the 64-bit PEB
            if (Pbi.PebBaseAddress != IntPtr.Zero)
                PatchPebAt("PEB64", Pbi.PebBaseAddress, 0xBC, "X16");

            // 32-bit PEB, only for WOW64 targets (ProcessWow64Information = 26)
            if (_ProcessMachine == IMAGE_FILE_MACHINE.I386)
            {
                IntPtr Peb32Ptr = IntPtr.Zero;
                Win32.NtQueryInformationProcess(CreatedProcessHandle, 26, ref Peb32Ptr, IntPtr.Size, out ReturnLength);

                if (Peb32Ptr != IntPtr.Zero)
                    PatchPebAt("PEB32", Peb32Ptr, 0x68, "X8");
            }
        }

        /// <summary>
        /// Create some Environment Variable Block for the process.
        /// The block is sorted case-insensitively and double-null terminated, as Windows expects.
        /// CreateProcess P/Invoke must use CharSet.Unicode
        /// </summary>
        /// <returns></returns>
        private IntPtr BuildEnvironmentBlock()
        {
            var vars = new System.Collections.Generic.SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (DictionaryEntry KvPair in Environment.GetEnvironmentVariables())
                vars[(string)KvPair.Key] = (string)KvPair.Value;

            if (!vars.ContainsKey("_NO_DEBUG_HEAP"))
                vars["_NO_DEBUG_HEAP"] = "1";

            StringBuilder sb = new StringBuilder();
            foreach (var kv in vars)
                sb.Append(kv.Key).Append('=').Append(kv.Value).Append('\0');
            sb.Append('\0');

            return Marshal.StringToHGlobalUni(sb.ToString());
        }

        public void StartProcess()
        {

        }

        #region DLL-Name tools

        public enum DllNameSource
        {
            None,
            FileHandle,     // GetFinalPathNameByHandle: full path
            MappedFile,     // GetMappedFileName: full path
            ImageName,      // lpImageName read from the target: usually full path
            ExportName      // PE export directory: internal name only, NOT a path
        }

        /// <summary>
        /// Resolves the name of a DLL from a LOAD_DLL_DEBUG_EVENT.
        /// </summary>
        public string GetDllPath(LOAD_DLL_DEBUG_INFO info, out DllNameSource source)
        {
            return GetDllPath(CreatedProcessHandle, info, _ProcessMachine == IMAGE_FILE_MACHINE.I386, out source);
        }
        public static string GetDllPath(IntPtr hProcess, LOAD_DLL_DEBUG_INFO info, bool targetIs32Bit, out DllNameSource source)
        {
            string Path = string.Empty;
            StringBuilder sb = new StringBuilder(1024);
            uint n = 0;

            // 1) From the file handle (can be NULL)
            if (info.hFile != IntPtr.Zero && info.hFile != new IntPtr(-1))
            {                
                n = Win32.GetFinalPathNameByHandleW(info.hFile, sb, (uint)sb.Capacity, 0); // VOLUME_NAME_DOS
                if (n > 0 && n < sb.Capacity)
                {
                    Path = sb.ToString();
                    if (Path.StartsWith(@"\\?\UNC\", StringComparison.OrdinalIgnoreCase))
                        Path = @"\\" + Path.Substring(8);
                    if (Path.StartsWith(@"\\?\"))
                        Path = Path.Substring(4);

                    if (!string.IsNullOrEmpty(Path))
                    {
                        source = DllNameSource.FileHandle;
                        return Path;
                    }
                }
            }

            // 2) Ask the OS which file is mapped at the base address in the target
            sb = new StringBuilder(1024);
            n = Win32.GetMappedFileNameW(hProcess, info.lpBaseOfDll, sb, (uint)sb.Capacity);
            if (n > 0)
            {
                if (!string.IsNullOrEmpty(sb.ToString()))
                {
                    source = DllNameSource.FileHandle;
                    return sb.ToString();
                }
            }

 /*           // 3) lpImageName: a pointer, in the target, to a pointer to the name (often NULL)
            Path = ReadImageName(hProcess, info, targetIs32Bit);
            if (!string.IsNullOrEmpty(Path))
            {
                source = DllNameSource.ImageName;
                return Path;
            }

            // 4) Last resort: the DLL's internal name from its PE export directory
            Path = ReadExportName(hProcess, info.lpBaseOfDll);
            if (!string.IsNullOrEmpty(Path))
            {
                source = DllNameSource.ExportName;
                return Path;
            }
 */
            //Finally
            source = DllNameSource.None;
            return null;
        }

        #endregion

        public IThreadContext GetThreadContext(UInt32 ThreadId)
        {          
            IThreadContext Context = GetCorrectContextStruct();
            IntPtr hThread = Win32.OpenThread(Win32.ThreadAccess.GET_CONTEXT | Win32.ThreadAccess.SUSPEND_RESUME, false, ThreadId);
            if (hThread == IntPtr.Zero)
            {
                Logger.WriteLog("GetThreadContextFromProcess(): Error getting Thread Handle for Thread ID=" + ThreadId);
                return null;
            }
            bool b = Context.Load(hThread);
            Win32.CloseHandle(hThread);
            return b ? Context : null;
        }

        public bool SetThreadContext(UInt32 ThreadId, IThreadContext Context)
        {
            IntPtr hThread = Win32.OpenThread(Win32.ThreadAccess.SET_CONTEXT | Win32.ThreadAccess.SUSPEND_RESUME, false, ThreadId);
            if (hThread == IntPtr.Zero)
            {
                Logger.WriteLog("SetThreadContextFromProcess(): Error getting Thread Handle for Thread ID=" + ThreadId);
                return false;
            }
            bool b = Context.Store(hThread);
            Win32.CloseHandle(hThread);
            return b;
        }
        
        private IThreadContext GetCorrectContextStruct()
        {
            if (_NativeMachine == IMAGE_FILE_MACHINE.AMD64)
            {
                if (_ProcessMachine == IMAGE_FILE_MACHINE.I386)
                    return new ThreadContextWow64();

                return new ThreadContext64();
            }

            //Discarded
            return null;
        }

        #region Delegates and QuickDebugger class events

        public delegate void ProcessCreatedEventHandler(object sender, EventArgs e);
        public event ProcessCreatedEventHandler OnProcessCreated;

        public delegate void CreateProcessDebugEventHandler(object sender, CreateProcessDebugEventArgs e);
        public event CreateProcessDebugEventHandler OnCreateProcess;

        public delegate void CreateThreadDebugEventHandler(object sender, CreateThreadDebugEventArgs e);
        public event CreateThreadDebugEventHandler OnCreateThread;

        public delegate void ExitThreadDebugEventHandler(object sender, ExitThreadDebugEventArgs e);
        public event ExitThreadDebugEventHandler OnExitThread;

        public delegate void LoadDllDebugEventHandler(object sender, LoadDllDebugEventArgs e);
        public event LoadDllDebugEventHandler OnLoadedDll;

        public delegate void UnloadDllDebugEventHandler(object sender, UnloadDllDebugEventArgs e);
        public event UnloadDllDebugEventHandler OnUnloadedDll;

        public delegate void ExceptionDebugEventHandler(object sender, ExceptionDebugEventArgs e);
        public event ExceptionDebugEventHandler OnException;

        public delegate void OutputDebugStringDebugEventHandler(object sender, OutputDebugStringDebugEventArgs e);
        public event OutputDebugStringDebugEventHandler OnOutputDebugString;

        public delegate void RipDebugEventHandler(object sender, RipDebugEventArgs e);
        public event RipDebugEventHandler OnRip;

        public delegate void ExitProcessDebugEventHandler(object sender, ExitProcessDebugEventArgs e);
        public event ExitProcessDebugEventHandler OnExitProcess;

        public class CreateProcessDebugEventArgs : EventArgs
        {
            public CREATE_PROCESS_DEBUG_INFO CreateProcessInfo { get; private set; }
            public UInt32 dwProcessId;
            public CreateProcessDebugEventArgs(UInt32 ProcessId, CREATE_PROCESS_DEBUG_INFO DebugEvent)
            {
                dwProcessId = ProcessId;
                CreateProcessInfo = DebugEvent;
            }
        }

        public class CreateThreadDebugEventArgs : EventArgs
        {
            public CREATE_THREAD_DEBUG_INFO CreateThreadInfo { get; private set; }
            public UInt32 dwThreadId;

            public CreateThreadDebugEventArgs(UInt32 ThreadId, CREATE_THREAD_DEBUG_INFO DebugEvent)
            {
                dwThreadId = ThreadId;
                CreateThreadInfo = DebugEvent;
            }
        }

        public class ExitThreadDebugEventArgs : EventArgs
        {
            public EXIT_THREAD_DEBUG_INFO ExitThreadInfo { get; private set; }

            public ExitThreadDebugEventArgs(EXIT_THREAD_DEBUG_INFO DebugEvent)
            {
                ExitThreadInfo = DebugEvent;
            }
        }

        public class LoadDllDebugEventArgs : EventArgs
        {
            public LOAD_DLL_DEBUG_INFO LoadDllInfo { get; private set; }
            public string DllPath {get; private set; }

            public LoadDllDebugEventArgs(LOAD_DLL_DEBUG_INFO DebugEvent, string Dll_Path)
            {
                LoadDllInfo = DebugEvent;
                DllPath = Dll_Path;
            }
        }

        public class UnloadDllDebugEventArgs : EventArgs
        {
            public UNLOAD_DLL_DEBUG_INFO UnloadDllInfo { get; private set; }

            public UnloadDllDebugEventArgs(UNLOAD_DLL_DEBUG_INFO DebugEvent)
            {
                UnloadDllInfo = DebugEvent;
            }
        }

        public class ExceptionDebugEventArgs : EventArgs
        {
            public EXCEPTION_DEBUG_INFO ExceptionInfo { get; private set; }
            public UInt32 dwThreadId;
            public bool Handled { get; set; }

            public ExceptionDebugEventArgs(UInt32 ThreadId, EXCEPTION_DEBUG_INFO DebugEvent)
            {
                dwThreadId = ThreadId;
                ExceptionInfo = DebugEvent;
            }
        }

        public class OutputDebugStringDebugEventArgs : EventArgs
        {
            public OUTPUT_DEBUG_STRING_INFO OutputDebugStringInfo { get; private set; }

            public OutputDebugStringDebugEventArgs(OUTPUT_DEBUG_STRING_INFO DebugEvent)
            {
                OutputDebugStringInfo = DebugEvent;
            }
        }

        public class RipDebugEventArgs : EventArgs
        {
            public RIP_INFO RipInfo { get; private set; }

            public RipDebugEventArgs(RIP_INFO DebugEvent)
            {
                RipInfo = DebugEvent;
            }
        }

        public class ExitProcessDebugEventArgs : EventArgs
        {
            public EXIT_PROCESS_DEBUG_INFO ExitProcessInfo { get; private set; }

            public ExitProcessDebugEventArgs(EXIT_PROCESS_DEBUG_INFO DebugEvent)
            {
                ExitProcessInfo = DebugEvent;
            }
        }

        #endregion
    }
}
