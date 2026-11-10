using System;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using UsCore.Config;
using UsCore.Launcher;
using UsCore.Logging;
using UsCore.Memory;

namespace U_Shoot.Games
{
    public class BaseGame64 : BaseGame
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
                    _Process_MemoryBaseAddress = new IntPtr(ReadInt64((UInt64)(o.PebBaseAddress + 0x10)));
                    
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
            
            if (e.ExceptionInfo.ExceptionRecord.ExceptionCode == Win32.EXCEPTION_BREAKPOINT)
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

        public BaseGame64(GameConfig Config) : base(Config)
        { }

        protected UInt64 GameProcess_ModuleBaseAddress
        { get { return (UInt64)_Process_MemoryBaseAddress; } }

        protected UInt64 LoaderDatabank_Address
        { get { return (UInt64)_LoaderDatabank_Address; } }

        protected UInt64 InputsDatabank_Address
        { get { return (UInt64)_InputsDatabank_Address; } }

        protected UInt64 OutputsDatabank_Address
        { get { return (UInt64)_OutputsDatabank_Address; } }

        protected BreakpointStruct64 _EntryPoint_Breakpointstruct = null;

        #region Memory Hack 64 Bits

        /// <summary>
        /// Adding some 64Bits-oriented definitions to help keep the code cleaner without too much cast
        /// </summary>
        protected bool WriteByte(UInt64 dwAddress, byte bData)
        {
            return WriteByte(new IntPtr((long)dwAddress), bData);
        }
        protected bool WriteBytes(UInt64 dwAddress, byte[] Buffer)
        {
            return WriteBytes(new IntPtr((long)dwAddress), Buffer);
        }
        protected void Apply_OR_ByteMask(UInt64 MemoryAddress, byte Mask)
        {
            Apply_OR_ByteMask(new IntPtr((long)MemoryAddress), Mask);
        }
        protected void Apply_AND_ByteMask(UInt64 MemoryAddress, byte Mask)
        {
            Apply_AND_ByteMask(new IntPtr((long)MemoryAddress), Mask);
        }
        protected byte ReadByte(UInt64 MemoryAddress)
        {
            return ReadByte(new IntPtr((long)MemoryAddress));
        }
        protected byte[] ReadBytes(UInt64 MemoryAddress, int BytesCount)
        {
            return ReadBytes(new IntPtr((long)MemoryAddress), BytesCount);
        }
        protected UInt32 ReadUInt32(UInt64 MemoryAddress)
        {
            return BitConverter.ToUInt32(ReadBytes(MemoryAddress, 4), 0);
        }
        protected Int32 ReadInt32(UInt64 MemoryAddress)
        {
            return BitConverter.ToInt32(ReadBytes(MemoryAddress, 4), 0);
        }
        protected UInt64 ReadUInt64(UInt64 MemoryAddress)
        {
            return BitConverter.ToUInt64(ReadBytes(MemoryAddress, 8), 0);
        }
        protected Int64 ReadInt64(UInt64 MemoryAddress)
        {
            return BitConverter.ToInt64(ReadBytes(MemoryAddress, 8), 0);
        }
        protected float ReadFloat(UInt64 MemoryAddress)
        {
            return BitConverter.ToSingle(ReadBytes(MemoryAddress, 4), 0);
        }
        protected UInt64 ReadPtr(UInt64 qwAddress)
        {
            Int64 lValue = 0;
            if (qwAddress != 0)
            {
                byte[] Buffer = ReadBytes(qwAddress, 8);
                lValue = BitConverter.ToInt64(Buffer, 0);
            }
            return (UInt64)lValue;
        }
        protected UInt64 ReadPtrChain(UInt64 BaseAddress, UInt64[] Offsets)
        {
            Int64 qwResult = 0;
            if (BaseAddress != 0)
            {
                byte[] Buffer = ReadBytes(BaseAddress, 8);
                qwResult = BitConverter.ToInt64(Buffer, 0);

                if (qwResult == 0)
                {
                    return (UInt64)qwResult;
                }
                else
                {
                    for (int i = 0; i < Offsets.Length; i++)
                    {
                        Buffer = ReadBytes((UInt64)(qwResult + (long)(Offsets[i])), 8);
                        qwResult = BitConverter.ToInt64(Buffer, 0);

                        if (qwResult == 0)
                            return (UInt64)qwResult;
                    }
                }

            }
            return (UInt64)qwResult;
        }
        protected void SetNops(UInt64 BaseAddress, NopStruct64 Nop)
        {
            long lBaseAddress = (Int64)BaseAddress + (long)Nop.MemoryOffset;
            IntPtr pBaseAddress = new IntPtr(lBaseAddress);
            for (int i = 0; i < Nop.Length; i++)
            {
                IntPtr Address = IntPtr.Add(pBaseAddress, i);
                if (!WriteByte(Address, 0x90))
                {
                    Logger.WriteLog("Impossible to NOP address 0x" + Address.ToString("X16"));
                    break;
                }
            }
        }

        /// <summary>
        /// Adding a software breakpoint (0xCC - INT3) to a specific memory address
        /// </summary>
        protected void SetBreakpoint(BreakpointStruct64 Bp, UInt64 BaseAddress)
        {
            UInt64 BreakpointAddress = BaseAddress + Bp.MemoryOffset;
            Bp.InitialAssemblyCode = ReadByte(BreakpointAddress);
            WriteByte(BreakpointAddress, 0xCC);
            Win32.FlushInstructionCache(_Process_Handle, new IntPtr((long)BreakpointAddress), new UIntPtr(1));
        }
        protected void SetBreakpoint(BreakpointStruct64 Bp)
        {
            SetBreakpoint(Bp, GameProcess_ModuleBaseAddress);
        }
        protected override void SetEntryPointBreakpoint()
        {
            //BreakPointStruct needs a Memory Offset, but _Process_EntryPointAddress is an absolute Address
            _EntryPoint_Breakpointstruct = new BreakpointStruct64((UInt64)_Process_EntryPointAddress - GameProcess_ModuleBaseAddress, 1);
            SetBreakpoint(_EntryPoint_Breakpointstruct);
        }

        /// <summary>
        /// Once Breakpoint operation is done, restoring the initial byte value before the 0xCC and decrease RIP so that execution can continue normally
        /// </summary>
        protected bool RemoveBreakpoint(BreakpointStruct64 Bp, uint dwThreadId)
        {
            return RemoveBreakpoint(Bp, dwThreadId, GameProcess_ModuleBaseAddress);
        }
        protected bool RemoveBreakpoint(BreakpointStruct64 Bp, uint dwThreadId, UInt64 BaseAddress)
        {
            ThreadContext64 Context64 = (ThreadContext64)_GameLauncher.GetThreadContext(dwThreadId);
            if (Context64 == null)
            {
                Logger.WriteLog(" > GetThreadContext() error !");
                return false;
            }

            Context64.Context.Rip--;
            if (!_GameLauncher.SetThreadContext(dwThreadId, Context64))
            {
                Logger.WriteLog(" > SetThreadContext() error !");
                return false;
            }

            return RestoreBreakpointBytecode(Bp, BaseAddress);
        }
        private bool RestoreBreakpointBytecode(BreakpointStruct64 Bp, UInt64 BaseAddress)
        {
            UInt64 BreakpointAddress = BaseAddress + Bp.MemoryOffset;
            bool bResult = WriteByte(BreakpointAddress, Bp.InitialAssemblyCode);
            Win32.FlushInstructionCache(_Process_Handle, new IntPtr((long)BreakpointAddress), new UIntPtr(1));
            return bResult;
        }

        protected void ReplaceReadOnlyString(UInt64[] OffsetArray, byte[] NewStringBytes)
        {
            ReplaceReadOnlyString(GameProcess_ModuleBaseAddress, OffsetArray, NewStringBytes);
        }
        protected void ReplaceReadOnlyString(UInt64 ModuleBaseAddress, UInt64[] OffsetArray, byte[] NewStringBytes)
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
                    IntPtr pAddress = (IntPtr)(ModuleBaseAddress + OffsetArray[i]);
                    Win32.MEMORY_BASIC_INFORMATION memoryInformation = GetMemoryInformationFromAddress(_Process_Handle, pAddress);
                    Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() -> VirtualQueryEx(0x" + pAddress.ToString("X8") + ") returned : Protect=" + memoryInformation.Protect.ToString() + ", Regionsize=0x" + memoryInformation.RegionSize.ToString("X8") + ", BaseAddress=0x" + memoryInformation.BaseAddress.ToString("X8"));
                    OriginalMemoryProtectionList[i] = memoryInformation.Protect;
                }

                //Checking permission and changing memory/string
                for (int i = 0; i < OffsetArray.Length; i++)
                {
                    IntPtr pAddress = (IntPtr)(ModuleBaseAddress + OffsetArray[i]);

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
                    IntPtr pAddress = (IntPtr)(ModuleBaseAddress + OffsetArray[i]);
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

        #endregion

        #region Overrides for 32/64 Bits Functions 
        protected override void Create_LoaderDataBank()
        {
            try
            {
                Codecave64 CaveMemory = new Codecave64(_Process,  GameProcess_ModuleBaseAddress);
                _LoaderDatabank_Address = new IntPtr((long)CaveMemory.CaveAddress);
                Logger.WriteLog("Loader data will be stored at : 0x" + CaveMemory.CaveAddress.ToString("X16"));
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
                Codecave64 CaveMemoryInputs = new Codecave64(_Process, GameProcess_ModuleBaseAddress);
                _InputsDatabank_Address = new IntPtr((long)CaveMemoryInputs.CaveAddress);
                Logger.WriteLog("Custom input data will be stored at : 0x" + CaveMemoryInputs.CaveAddress.ToString("X16"));
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
                Codecave64 CaveMemoryOutputs = new Codecave64(_Process, GameProcess_ModuleBaseAddress);
                _OutputsDatabank_Address = new IntPtr((long)CaveMemoryOutputs.CaveAddress);
                Logger.WriteLog("Custom output data will be stored at : 0x" + CaveMemoryOutputs.CaveAddress.ToString("X16"));
            }
            catch (Exception Ex)
            {
                Logger.WriteLog("Impossible to create Outputs Databank : " + Ex.Message);
            }
        }

        protected override void SetNopStructFromString(string sData, string FieldName)
        {
            NopStruct64 n = new NopStruct64(sData);
            this.GetType().GetField(FieldName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase).SetValue(this, n);
            Logger.WriteLog(FieldName + " successfully set to following value : 0x" + n.MemoryOffset.ToString("X16") + "|" + n.Length.ToString());
        }

        protected override void SetInjectionStructFromString(string sData, string FieldName)
        {
            InjectionStruct64 n = new InjectionStruct64(sData);
            this.GetType().GetField(FieldName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase).SetValue(this, n);
            Logger.WriteLog(FieldName + " successfully set to following value : 0x" + n.InjectionOffset.ToString("X16") + "|" + n.Length.ToString());
        }

        protected override void SetAddressValueFromString(string sData, string FieldName)
        {
            UInt64 v = UInt64.Parse(sData, NumberStyles.HexNumber);
            this.GetType().GetField(FieldName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase).SetValue(this, v);
            Logger.WriteLog(FieldName + " successfully set to following value : 0x" + v.ToString("X16"));
        }

        #endregion
                
    }
}
