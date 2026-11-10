using System;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using UsCore.Config;
using UsCore.Launcher;
using UsCore.Logging;
using UsCore.Memory;

namespace U_Shoot.Games
{
    public class BaseGame32 : BaseGame
    {
        protected override void ProcessCreated(object sender, EventArgs e)
        {
            base.ProcessCreated(sender, e);

            if (_GameLauncher.DisableDebugger)
            {
                x86_x64_Debugger d = (x86_x64_Debugger)sender;
                _Process = Process.GetProcessById((int)d.CreatedProcessId);
                _Process_Handle = d.CreatedProcessHandle;

                //Allocate memory for a new PROCESS_BASIC_INFORMATION structure
                IntPtr pbi = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(Win32.PROCESS_BASIC_INFORMATION)));
                //Allocate memory for a long
                IntPtr outLong = Marshal.AllocHGlobal(sizeof(long));
                IntPtr outPtr = IntPtr.Zero;

                int queryStatus = 0;
                //Store API call success in a boolean
                queryStatus = Win32.NtQueryInformationProcess(_Process_Handle, 0, pbi, (uint)Marshal.SizeOf(typeof(Win32.PROCESS_BASIC_INFORMATION)), outLong);

                ////STATUS_SUCCESS = 0, so if API call was successful querySuccess should contain 0 ergo we reverse the check.
                if (queryStatus == 0)
                {
                    Win32.PROCESS_BASIC_INFORMATION o = Marshal.PtrToStructure<Win32.PROCESS_BASIC_INFORMATION>(pbi);
                    _Process_MemoryBaseAddress = new IntPtr(ReadInt32((UInt32)(o.PebBaseAddress + 0x10)));

                    //Free allocated space
                    Marshal.FreeHGlobal(pbi);
                }

                Logger.WriteLog("Process successfully created :");
                Logger.WriteLog(" > Process ID: " + _Process.Id);
                Logger.WriteLog(" > Process Memory Base Address: 0x" + _Process_MemoryBaseAddress.ToString("X16"));
                //HANDLE should always be in 32Bits range (https://learn.microsoft.com/fr-fr/windows/win32/winprog64/interprocess-communication?redirectedfrom=MSDN)
                Logger.WriteLog(" > Process Handle: 0x" + _Process_Handle.ToString("X8"));

                CheckExeMd5();
                ReadGameDataFromMd5Hash(GAMEDATA_FOLDER);

                //Start the Timer that will either hook an emulator when ROM is loaded, or close the program if the target binary is not running
                _tProcess.Start();
            }
        }

        #region Debugger Event

        #region EXCEPTION_DEBUG_EVENT

        protected override void ExceptionEvent(object sender, x86_x64_Debugger.ExceptionDebugEventArgs e)
        {
            x86_x64_Debugger Dbg = (x86_x64_Debugger)sender;
            uint dwThreadId = e.dwThreadId;
            
            //When started by X64 process, X86 target process run in Wow64 emulator for X86 and Exceptions events are not the same :
            //STATUS_BREAKPOINT is now replaced by STATUS_WX86_BREAKPOINT
            if (e.ExceptionInfo.ExceptionRecord.ExceptionCode == Win32.STATUS_WX86_BREAKPOINT)
            {
                //If PEB is patched, InitialException will not be received so the only one we are waiting for here is the entry point INT3 Breakpint created earlier
                if (PATCH_PEB_DEBUG_FLAG)
                {
                    if (e.ExceptionInfo.ExceptionRecord.ExceptionAddress == _Process_EntryPointAddress)
                    {
                        Logger.WriteLog(" > Program entry point Breakpoint");
                        if (!RemoveBreakpoint(_EntryPoint_Breakpointstruct, e.dwThreadId))
                            return;

                        e.Handled = true;
                        Apply_MemoryHacks();
                    }
                }
                else
                {
                    if (_IsFirstException)
                    {
                        Logger.WriteLog(" > Initital Breakpoint");
                        Apply_MemoryHacks();
                        _IsFirstException = false;
                    }
                }
            }
        }

        #endregion

        #endregion

        public BaseGame32(GameConfig Config) : base(Config)
        { }

        protected UInt32 GameProcess_ModuleBaseAddress
        { get { return (UInt32)_Process_MemoryBaseAddress; }}

        protected UInt32 LoaderDatabank_Address
        { get { return (UInt32)_LoaderDatabank_Address; } }

        protected UInt32 InputsDatabank_Address
        { get { return (UInt32)_InputsDatabank_Address; } }

        protected UInt32 OutputsDatabank_Address
        { get { return (UInt32)_OutputsDatabank_Address; } }

        protected BreakpointStruct32 _EntryPoint_Breakpointstruct = null;

        #region Memory Hack 32 Bits

        /// <summary>
        /// Adding some 32Bits-oriented definitions to help keep the code cleaner without too much cast
        /// </summary>
        protected bool WriteByte(UInt32 dwAddress, byte bData)
        {
            return WriteByte(new IntPtr(dwAddress), bData);
        }
        protected bool WriteBytes(UInt32 dwAddress, byte[] Buffer)
        {
            return WriteBytes(new IntPtr(dwAddress), Buffer);
        }
        protected void Apply_OR_ByteMask(UInt32 MemoryAddress, byte Mask)
        {
            Apply_OR_ByteMask(new IntPtr(MemoryAddress), Mask);
        }
        protected void Apply_AND_ByteMask(UInt32 MemoryAddress, byte Mask)
        {
            Apply_AND_ByteMask(new IntPtr(MemoryAddress), Mask);
        }
        protected byte ReadByte(UInt32 dwAddress)
        {
            return ReadByte(new IntPtr(dwAddress));
        }
        protected byte[] ReadBytes(UInt32 dwAddress, int BytesCount)
        {
            return ReadBytes(new IntPtr(dwAddress), BytesCount);
        }
        protected UInt32 ReadUInt32(UInt32 dwAddress)
        {
            return BitConverter.ToUInt32(ReadBytes(dwAddress, 4), 0);
        }
        protected Int32 ReadInt32(UInt32 dwAddress)
        {
            return BitConverter.ToInt32(ReadBytes(dwAddress, 4), 0);
        }
        protected UInt64 ReadUInt64(UInt32 dwAddress)
        {
            return BitConverter.ToUInt64(ReadBytes(dwAddress, 8), 0);
        }
        protected Int64 ReadInt64(UInt32 dwAddress)
        {
            return BitConverter.ToInt64(ReadBytes(dwAddress, 8), 0);
        }
        protected float ReadFloat(UInt32 dwAddress)
        {
            return BitConverter.ToSingle(ReadBytes(dwAddress, 4), 0);
        }
        protected UInt32 ReadPtr(UInt32 dwAddress)
        {
            UInt32 iValue = 0;
            if (dwAddress != 0)
            {
                byte[] Buffer = ReadBytes(dwAddress, 4);
                iValue = BitConverter.ToUInt32(Buffer, 0);
            }
            return iValue;
        }
        protected UInt32 ReadPtrChain(UInt32 dwBaseAddress, UInt32[] Offsets)
        {
            UInt32 dwResult = 0;
            if (dwBaseAddress != 0)
            {
                byte[] Buffer = ReadBytes(dwBaseAddress, 4);
                dwResult = BitConverter.ToUInt32(Buffer, 0);
                
                if (dwResult == 0)
                {
                    return dwResult;
                }
                else
                {
                    for (int i = 0; i < Offsets.Length; i++)
                    {
                        Buffer = ReadBytes(dwResult + Offsets[i], 4);
                        dwResult = BitConverter.ToUInt32(Buffer, 0);

                        if (dwResult == 0)
                            return dwResult;
                    }
                }
            }
            return dwResult;
        }
        protected void SetNops(UInt32 BaseAddress, NopStruct32 Nop)
        {
            UInt32 dwNopMemoryOffset = Nop.MemoryOffset;
            for (uint i = 0; i < Nop.Length; i++)
            {    
                if (!WriteByte(BaseAddress + dwNopMemoryOffset + i, 0x90))                
                {
                    Logger.WriteLog("Impossible to NOP address 0x" + BaseAddress.ToString("X8"));
                    break;
                }
            }
        }
        /// <summary>
        /// Adding a software breakpoint (0xCC - INT3) to a specific memory address
        /// </summary>
        protected void SetBreakpoint(BreakpointStruct32 Bp, UInt32 BaseAddress)
        {
            UInt32 BreakpointAddress = BaseAddress + Bp.MemoryOffset;
            Bp.InitialAssemblyCode = ReadByte(BreakpointAddress);
            WriteByte(BreakpointAddress, 0xCC);
            Win32.FlushInstructionCache(_Process_Handle, new IntPtr(BreakpointAddress), new UIntPtr(1));
        }
        protected void SetBreakpoint(BreakpointStruct32 Bp)
        {
            SetBreakpoint(Bp, GameProcess_ModuleBaseAddress);
        }
        protected override void SetEntryPointBreakpoint()
        {
            //BreakPointStruct needs a Memory Offset, but _Process_EntryPointAddress is an absolute Address
            _EntryPoint_Breakpointstruct = new BreakpointStruct32((UInt32)_Process_EntryPointAddress - GameProcess_ModuleBaseAddress, 1);
            SetBreakpoint(_EntryPoint_Breakpointstruct);
        }

        /// <summary>
        /// Once Breakpoint operation is done, restoring the initial byte value before the 0xCC and decrease RIP so that execution can continue normally
        /// </summary>
        /// protected bool RemoveBreakpoint(BreakpointStruct32 Bp, uint dwThreadId, UInt32 BaseAddress)
        protected bool RemoveBreakpoint(BreakpointStruct32 Bp, uint dwThreadId)
        {
            return RemoveBreakpoint(Bp, dwThreadId, GameProcess_ModuleBaseAddress);
        }
        protected bool RemoveBreakpoint(BreakpointStruct32 Bp, uint dwThreadId, UInt32 BaseAddress)
        {
            ThreadContextWow64 Context32 = (ThreadContextWow64)_GameLauncher.GetThreadContext(dwThreadId);
            if (Context32 == null)
            {
                Logger.WriteLog(" > GetThreadContext() error !");
                return false;
            }

            Context32.Context.Eip--;
            if (!_GameLauncher.SetThreadContext(dwThreadId, Context32))
            {
                Logger.WriteLog(" > SetThreadContext() error !");
                return false;
            }

            return RestoreBreakpointBytecode(Bp, BaseAddress);
        }
        private bool RestoreBreakpointBytecode(BreakpointStruct32 Bp, UInt32 BaseAddress)
        {
            UInt32 BreakpointAddress = BaseAddress + Bp.MemoryOffset;
            bool bResult = WriteByte(BreakpointAddress, Bp.InitialAssemblyCode);
            Win32.FlushInstructionCache(_Process_Handle, new IntPtr(BreakpointAddress), new UIntPtr(1));
            return bResult;
        }
               

        protected void ReplaceReadOnlyString(UInt32[] OffsetArray, byte[] NewStringBytes)
        {
            ReplaceReadOnlyString(GameProcess_ModuleBaseAddress, OffsetArray, NewStringBytes);
        }
        protected void ReplaceReadOnlyString(UInt32 ModuleBaseAddress, UInt32[] OffsetArray, byte[] NewStringBytes)
        {
            try
            {
                Win32.SYSTEM_INFO si = new Win32.SYSTEM_INFO();
                Win32.GetSystemInfo(ref si);
                Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() -> GetSystemInfo.dwPageSize=0x" + si.dwPageSize.ToString("X8"));

                //Storing original Memory protection flag                
                Win32.AllocationProtectEnum[] OriginalMemoryProtectionList = new Win32.AllocationProtectEnum[OffsetArray.Length];
                for (int i = 0; i < OffsetArray.Length; i++)
                {
                    IntPtr pAddress = new IntPtr(ModuleBaseAddress + OffsetArray[i]);
                    Win32.MEMORY_BASIC_INFORMATION memoryInformation = GetMemoryInformationFromAddress(_Process_Handle, pAddress);
                    Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() -> VirtualQueryEx(0x" + pAddress.ToString("X8") + ") returned : Protect=" + memoryInformation.Protect.ToString() + ", Regionsize=0x" + memoryInformation.RegionSize.ToString("X8") + ", BaseAddress=0x" + memoryInformation.BaseAddress.ToString("X8"));
                    OriginalMemoryProtectionList[i] = memoryInformation.Protect;
                }

                //Checking permission and changing memory/string
                for (int i = 0; i < OffsetArray.Length; i++)
                {
                    IntPtr pAddress = new IntPtr(ModuleBaseAddress + OffsetArray[i]);

                    /*byte[] b = ReadBytes((UInt32)_Process_MemoryBaseAddress + _SportsOptionFolder_String_Offsets_1[i], 7);
                    string s = string.Empty;
                    for (int j = 0; j < b.Length; j++)
                    {
                        s += "0x" + b[j].ToString("X2") + " ";
                    }
                    WriteLog(MethodBase.GetCurrentMethod().Name + "() -> before 0x" + _SportsOptionFolder_String_Offsets_1[i].ToString("X8") + " =  " + s);*/

                    if (OriginalMemoryProtectionList[i] != Win32.AllocationProtectEnum.PAGE_READWRITE)
                    {
                        uint oldProtect = 0;
                        if (!Win32.VirtualProtectEx(_Process_Handle, pAddress, (UIntPtr)(si.dwPageSize), (uint)Win32.AllocationProtectEnum.PAGE_READWRITE, out oldProtect))
                            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() -> VirtualProtectEx() Error");
                        else
                            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() -> Successfully change 0x" + pAddress.ToString("X8") + " memory protection to " + Win32.AllocationProtectEnum.PAGE_READWRITE.ToString() + ", oldProtect=" + ((Win32.AllocationProtectEnum)oldProtect).ToString());
                    }

                    WriteBytes(pAddress, NewStringBytes);
                }

                //Restoring Original state
                for (int i = 0; i < OffsetArray.Length; i++)
                {
                    IntPtr pAddress = new IntPtr(ModuleBaseAddress + OffsetArray[i]);
                    uint oldProtect = 0;
                    if (!Win32.VirtualProtectEx(_Process_Handle, pAddress, (UIntPtr)(si.dwPageSize), (uint)OriginalMemoryProtectionList[i], out oldProtect))
                        Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() -> VirtualProtectEx() Error");
                    else
                        Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() -> Successfully restored memory protection to " + OriginalMemoryProtectionList[i].ToString() + ", oldProtect=" + ((Win32.AllocationProtectEnum)oldProtect).ToString());
                }
            }
            catch (Exception Ex)
            {
                Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() Exception : " + Ex.Message.ToString());
            }
        }

        private Win32.MEMORY_BASIC_INFORMATION GetMemoryInformationFromAddress(IntPtr ProcessHandle, IntPtr Address)
        {
            Win32.MEMORY_BASIC_INFORMATION memoryInformation = new Win32.MEMORY_BASIC_INFORMATION();

            try
            {
                if (Win32.VirtualQueryEx(ProcessHandle, Address, out memoryInformation, (uint)Marshal.SizeOf(typeof(Win32.MEMORY_BASIC_INFORMATION))) == 0)
                    Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() -> VirtualQueryEx() Error");

            }
            catch (Exception Ex)
            {
                Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() -> Exception : " + Ex.Message.ToString());
            }

            return memoryInformation;
        }

        #region Overrides for 32/64 Bits Functions 
        protected override void Create_LoaderDataBank()
        {
            try
            {
                Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);
                _LoaderDatabank_Address = new IntPtr(CaveMemory.CaveAddress);
                Logger.WriteLog("Loader data will be stored at : 0x" + CaveMemory.CaveAddress.ToString("X8"));
            }
            catch (Exception Ex)
            {
                Logger.WriteLog("Impossible to create Loader Databank : " + Ex.Message);
            }
        }

        protected override void Create_InputsDataBank()
        {
            try
            {
                Codecave32 CaveMemoryInputs = new Codecave32(_Process, GameProcess_ModuleBaseAddress);
                _InputsDatabank_Address = new IntPtr(CaveMemoryInputs.CaveAddress);
                Logger.WriteLog("Custom input data will be stored at : 0x" + CaveMemoryInputs.CaveAddress.ToString("X8"));
            }
            catch (Exception Ex)
            {
                Logger.WriteLog("Impossible to create Inputs Databank : " + Ex.Message);
            }
        }

        protected override void Create_OutputsDataBank()
        {
            try
            {
                Codecave32 CaveMemoryOutputs = new Codecave32(_Process, GameProcess_ModuleBaseAddress);
                _OutputsDatabank_Address = new IntPtr(CaveMemoryOutputs.CaveAddress);
                Logger.WriteLog("Custom output data will be stored at : 0x" + CaveMemoryOutputs.CaveAddress.ToString("X8"));
            }
            catch (Exception Ex)
            {
                Logger.WriteLog("Impossible to create Outputs Databank : " + Ex.Message);
            }
        }

        protected override void SetNopStructFromString(string sData, string FieldName)
        {
            NopStruct32 n = new NopStruct32(sData);
            this.GetType().GetField(FieldName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase).SetValue(this, n);
            Logger.WriteLog(FieldName + " successfully set to following value : 0x" + n.MemoryOffset.ToString("X8") + "|" + n.Length.ToString());
        }

        protected override void SetInjectionStructFromString(string sData, string FieldName)
        {
            InjectionStruct32 n = new InjectionStruct32(sData);
            this.GetType().GetField(FieldName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase).SetValue(this, n);
            Logger.WriteLog(FieldName + " successfully set to following value : 0x" + n.InjectionOffset.ToString("X8") + "|" + n.Length.ToString());
        }

        protected override void SetBreakpointStructFromString(string sData, string FieldName)
        {
            BreakpointStruct32 n = new BreakpointStruct32(sData);
            this.GetType().GetField(FieldName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase).SetValue(this, n);
            Logger.WriteLog(FieldName + " successfully set to following value : 0x" + n.MemoryOffset.ToString("X8") + "|" + n.Length.ToString());
        }

        protected override void SetAddressValueFromString(string sData, string FieldName)
        {
            UInt32 v = UInt32.Parse(sData, NumberStyles.HexNumber);
            this.GetType().GetField(FieldName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase).SetValue(this, v);
            Logger.WriteLog(FieldName + " successfully set to following value : 0x" + v.ToString("X8"));
        }
        #endregion


        #endregion
    }
}
