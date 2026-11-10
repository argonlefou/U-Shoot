using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UsCore.Config;
using UsCore.Launcher;
using UsCore.Logging;
using UsCore.MameOutput;
using UsCore.Memory;
using UsCore.VirtualControllers;

namespace U_Shoot.Games
{
    public class Game_WndFriction : BaseGame32
    {
        protected override string GAMEDATA_FOLDER { get { return @"Windows\friction"; } }
        protected override bool PATCH_PEB_DEBUG_FLAG { get { return true; } }

        private const string KERNEL32_DLL = "kernel32.dll";
        private const string CREATETHREAD_API = "CreateThread";
        private const string VSIOBOARDDLL_NAME = "vsioboard.dll";
        private UInt32 _VsIoBoardDll_BaseAddress;

        //Default game resolution is 640x480
        private int _ResolutionWidth = 640;
        private int _ResolutionHeight = 480;

        #region LOADER MEMORY ADDRESSES                
        private UInt32 _DisableReboot_Patch_Offset = 0x0004C000;
        private UInt32 _StartSleep_Patch_Offset = 0x00001014;
        private UInt32 _DInputDisableKeyboard_Patch_Offset = 0x00097958;
        private UInt32 _DInputDisableMouse1_Patch_Offset = 0x00097ED0;
        private UInt32 _DInputDisableMouse2_Patch_Offset = 0x00097D05;
        private UInt32 _DInputDisableMouse3_Patch_Offset = 0x0004CF9B;
        private UInt32 _DInputDisableMouse4_Patch_Offset = 0x0009793D;
        private UInt32 _ResolutionWindowedPatch_Offset = 0x00041361;
        private UInt32 _ResolutionWidthPatch_Offset = 0x0004140E;
        private UInt32 _ResolutionHeightPatch_Offset = 0x00041407;
        private UInt32 _WindowedFocusPatch1_Offset = 0x000732E8;
        private UInt32 _WindowedFocusPatch2_Offset = 0x000732FC;
        private InjectionStruct32 _NoCursor_InjectionStruct = new InjectionStruct32(0x00073422,5);
        #endregion

        #region INPUTS MEMORY ADDRESSES    
        private UInt32 _LoadLibraryA_FunctionPtr_Offset = 0x00112074;
        private UInt32 _GetProcAddress_FunctionPtr_Offset = 0x00112070;
        private UInt32 _Sleep_FunctionPtr_Offset = 0x001120C0;

        private UInt32 _VsIoBoard_Calibrate_Offset = 0x000036FE;
        private UInt32 _VsIoBoard_Close_Offset = 0x0000384E;        
        private UInt32 _VsIoBoard_DisableGunFlashes_Offset = 0x00003137;
        private UInt32 _VsIoBoard_GetDeviceState_Offset = 0x000030E9;
        private UInt32 _VsIoBoard_GetGunParams_Offset = 0x0000379E;
        private UInt32 _VsIoBoard_GetNumCoins_Offset = 0x0000311C;
        private UInt32 _VsIoBoard_GetNumDefaultDevice_Offsets = 0x000030E5;
        private UInt32 _VsIoBoard_SetFlashDelay_Offset = 0x0000347F;
        private UInt32 _VsIoBoard_SetGunAccuracy_Offset = 0x000034F6;
        private UInt32 _VsIoBoard_GetHSyncPolarity_Offset = 0x00003313;
        private UInt32 _VsIoBoard_GetVSyncPolarity_Offset = 0x0000330D;
        private UInt32 _VsIoBoard_GetVideoMode_Offset = 0x00003307;
        private UInt32 _VsIoBoard_SetCalibration_Offset = 0x0000356F;
        private InjectionStruct32 _VsIoBoard_AddListener_InjectionStruct = new InjectionStruct32(0x0000413E, 8);
        private InjectionStruct32 _VsIoBoard_Init_InjectionStruct = new InjectionStruct32(0x00003E6D, 7);        
        private InjectionStruct32 _VsIoBoard_RemoveListener_InjectionStruct = new InjectionStruct32(0x00004168, 8);        
        private InjectionStruct32 _VsIoBoard_SetUpGun_InjectionStruct = new InjectionStruct32(0x00003319, 6);
        private InjectionStruct32 _LevelSelectCorrection_InjectionStruct = new InjectionStruct32(0x00023191, 16);
        private BreakpointStruct32 _LoadIOLibrary_BreakpointStruct = new BreakpointStruct32(0x0004C2D1, 5);

        //Custom Input Address
        private enum PlayerStructOffsets : int
        {
            AutoMode = 0,
            SkipFrames = 4,
            LastTriggerState = 8,
            SkippedFrames = 9,
            CurrentTriggerState = 10,
            ReloadState = 11,
            Data = 12
        }
        private enum PlayerDataOffsets : short
        {
            Value = 0,
            Id = 4,
            Type = 8,
            X = 10,
            Y = 12,
            Buttons = 14
        }
        private UInt32 _P1_Struct_Address;
        private UInt32 _P2_Struct_Address;
        private UInt32 _Service_Address;
        private UInt32 _Test_Address;
        private UInt32 _P1_Start_Address;
        private UInt32 _P2_Start_Address;
        private UInt32 _Coin1_Address;
        private UInt32 _Coin2_Address;
        private UInt32 _Kernel32Text_Address;
        private UInt32 _CreateThreadText_Address;
        private UInt32 _ListenersBankAddress;
        private UInt32 _CallListeners_CustomFunction_Address;
        private UInt32 _InputsCheck_CustomFunction_Address;
        #endregion

        #region OUTPUTS MEMORY ADDRESSES
        private UInt32 _OutputDataPtr_Offset = 0x00145088;
        private UInt32 _CreditsPtr_Offset = 0x001450C4;
        #endregion

        private Dictionary<string, string> _KnownMd5_UsbPluginsDll;

        public Game_WndFriction(GameConfig Config) : base(Config)
        {
            _KnownMd5Prints.Add("Friction original dump", "931146ddcac8429634401d08158b8bec");
            _KnownMd5_UsbPluginsDll = new Dictionary<string, string>
            {
                { "Friction original vsIOBoard.dll", "25a0495caecaf9bee66b09f13da5e8fa" },
                { "Friction hacked VSIOBOARD.dll - v1.0", "9bbe8c9bf916826d6ab22703f6d83f7b" },
                { "Friction hacked VSIOBOARD.dll - v2.0", "a86819412ce393fdb17e6ae6dbd5bd27" },
                { "Friction hacked VSIOBOARD.dll - v3.0", "03c872a3d141ee186412dfc0636510bc" }
            };

            if (_GameConfig.ResolutionWidth > 0)
                _ResolutionWidth = _GameConfig.ResolutionWidth;
            if (_GameConfig.ResolutionHeight > 0)
                _ResolutionHeight = _GameConfig.ResolutionHeight;
        }

        //That game will need to patch DLL a load time before main exe starts
        protected override void AttachDebuggerEvents()
        {
            if (!_GameConfig.NoInputs)
                _GameLauncher.OnLoadedDll += LoadDllEvent;
        }

        #region Debugger Events

        #region EXCEPTION_DEBUG_EVENT

        protected override void ExceptionEvent(object sender, x86_x64_Debugger.ExceptionDebugEventArgs e)
        {
            x86_x64_Debugger Dbg = (x86_x64_Debugger)sender;
            uint dwThreadId = e.dwThreadId;

            //When started by X64 process, X86 target process run in Wow64 emulator for X86 and Exceptions events are not the same :
            //STATUS_BREAKPOINT is now replaced by STATUS_WX86_BREAKPOINT
            if (e.ExceptionInfo.ExceptionRecord.ExceptionCode == Win32.STATUS_WX86_BREAKPOINT)
            {
                Logger.WriteLog(" > ExceptionCode: STATUS_WX86_BREAKPOINT");
                Logger.WriteLog("ExceptionAddress = 0x" + e.ExceptionInfo.ExceptionRecord.ExceptionAddress.ToString(Dbg.HexStringFormat));

                if (e.ExceptionInfo.ExceptionRecord.ExceptionAddress == _Process_EntryPointAddress && PATCH_PEB_DEBUG_FLAG)
                {
                    Logger.WriteLog(" > Initital Breakpoint");
                    Logger.WriteLog(" > Program entry point Breakpoint");
                    if (!RemoveBreakpoint(_EntryPoint_Breakpointstruct, e.dwThreadId))
                        return;

                    e.Handled = true;
                    Apply_MemoryHacks();
                }
                else
                {
                    //Using a breakpoint after call to LoadLibraryA() for "vsIOBoard.dll" because using the LOAD_DLL_EXCEPTION can be called too early to
                    //properly patch the DLL, it can still be overwritten by the OS before actualy be  part of the EXE
                    //The LOAD_DLL_EXCEPTION will still be used to get data on the dll handle, etc....
                    if ((UInt32)e.ExceptionInfo.ExceptionRecord.ExceptionAddress == GameProcess_ModuleBaseAddress + _LoadIOLibrary_BreakpointStruct.MemoryOffset)
                    {
                        Logger.WriteLog(" > LoadLibrary(vsIOBoard.dll) breakpoint reached");
                        if (!RemoveBreakpoint(_LoadIOLibrary_BreakpointStruct, e.dwThreadId))
                            return;

                        e.Handled = true;
                        SetHack_UsbPluginDll();
                        RaiseGameHookedEvent();
                    }
                }
            }
        }

        #endregion

        #region LOAD_DLL_DEBUG_EVENT

        protected override void LoadDllEvent(object sender, x86_x64_Debugger.LoadDllDebugEventArgs e)
        {
            x86_x64_Debugger Dbg = (x86_x64_Debugger)sender;
            IntPtr lpBaseOfDllLoad = e.LoadDllInfo.lpBaseOfDll;

            if (lpBaseOfDllLoad != IntPtr.Zero)
            {
                if (string.Equals(Path.GetFileName(e.DllPath), VSIOBOARDDLL_NAME, StringComparison.OrdinalIgnoreCase))
                {
                    Logger.WriteLog("==> Wanted DLL found !");
                    //HANDLE should always be in 32Bits range (https://learn.microsoft.com/fr-fr/windows/win32/winprog64/interprocess-communication?redirectedfrom=MSDN)
                    Logger.WriteLog("  + hModule: 0x" + e.LoadDllInfo.hFile.ToString("X8"));
                    Logger.WriteLog("  + fUnicode: " + e.LoadDllInfo.fUnicode.ToString());

                    _VsIoBoardDll_BaseAddress = (UInt32)e.LoadDllInfo.lpBaseOfDll;

                    string UsbPluginPath = _GameConfig.TargetPath.Replace(Path.GetFileName(_GameConfig.TargetFileName), VSIOBOARDDLL_NAME);
                    string Dll_Md5Hash = GetMd5HashAsString(UsbPluginPath);
                    CheckMd5(UsbPluginPath, _KnownMd5_UsbPluginsDll);
                    ReadGameDataFromMd5Hash(Dll_Md5Hash, GAMEDATA_FOLDER);
                }
            }
        }

        #endregion           

        #endregion

        #region Screen

        /// <summary>
        /// Convert client area pointer location to Game speciffic data for memory injection
        /// </summary>
        public override bool GameScale(VirtualPlayerController PlayerController)
        {
            if (_Process_Handle != IntPtr.Zero)
            {
                try
                {
                    double TotalResX = _ClientRect.Right - _ClientRect.Left;
                    double TotalResY = _ClientRect.Bottom - _ClientRect.Top;
                    Logger.WriteLog(" > Game Window Rect (Px) = [ " + TotalResX + "x" + TotalResY + " ]");

                    double dMinX = 0;
                    double dMinY = 0;
                    double dMaxX = (double)(_ResolutionWidth - 1);
                    double dMaxY = (double)(_ResolutionHeight - 1);
                    double dRangeX = dMaxX - dMinX + 1;
                    double dRangeY = dMaxY - dMinY + 1;

                    PlayerController.Computed_X = Convert.ToInt32(Math.Round(dRangeX * PlayerController.Computed_X / TotalResX));
                    PlayerController.Computed_Y = Convert.ToInt32(Math.Round(dRangeY * PlayerController.Computed_Y / TotalResY));
                    if (PlayerController.Computed_X < (int)dMinX)
                        PlayerController.Computed_X = (int)dMinX;
                    if (PlayerController.Computed_Y < (int)dMinY)
                        PlayerController.Computed_Y = (int)dMinY;
                    if (PlayerController.Computed_X > (int)dMaxX)
                        PlayerController.Computed_X = (int)dMaxX;
                    if (PlayerController.Computed_Y > (int)dMaxY)
                        PlayerController.Computed_Y = (int)dMaxY;

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

        #region MemoryHack

        protected override void Apply_MemoryHacks()
        {
            //GameHooked event will be raised after a Breakpoint, once the DLL is fully loaded
            base.Apply_MemoryHacks();
        }

        #region Loader Hack
        protected override void Apply_LoaderMemoryHack()
        {
            //Jmp over the Reboot message send
            WriteByte(GameProcess_ModuleBaseAddress + _DisableReboot_Patch_Offset, 0xEB);

            //Replace the long sleep value by 0 in push
            WriteBytes(GameProcess_ModuleBaseAddress + _StartSleep_Patch_Offset, new byte[] { 0x00, 0x00, 0x00, 0x00 });

            //Focusing windowed mode crash on click, removing it for Windowed mode
            WriteBytes(GameProcess_ModuleBaseAddress + _WindowedFocusPatch1_Offset, new byte[] { 0x90, 0x90, 0x90, 0x90, 0x90 });
            WriteBytes(GameProcess_ModuleBaseAddress + _WindowedFocusPatch2_Offset, new byte[] { 0x90, 0x90, 0x90, 0x90, 0x90 });

            //Setting Resolution
            WriteBytes(GameProcess_ModuleBaseAddress + _ResolutionWidthPatch_Offset, BitConverter.GetBytes(_ResolutionWidth));
            WriteBytes(GameProcess_ModuleBaseAddress + _ResolutionHeightPatch_Offset, BitConverter.GetBytes(_ResolutionHeight));

            //Removing the cursor by setting the Handle to our transparent one when calling RegisterClass() 
            if (_GameConfig.NoCursor)
            {
                if (CreateTransparentCursor())
                    SetHack_NoCursor();
            }

            if (_GameConfig.Windowed)
                WriteBytes(GameProcess_ModuleBaseAddress + _ResolutionWindowedPatch_Offset, new byte[] { 0x90, 0x90, 0xC6, 0x46, 0x2D, 0x00 });
            else if (_GameConfig.Fullscreen)
                WriteBytes(GameProcess_ModuleBaseAddress + _ResolutionWindowedPatch_Offset, new byte[] { 0x90, 0x90, 0xC6, 0x46, 0x2D, 0x01});

            //if -NoInputSwitch is enabled, the loader will not put the breakpoint to patch loaded vsIOBoard.dll
            //In that case, the hooked event must be set from here
            if (_GameConfig.NoInputs)
            {
                RaiseGameHookedEvent();
            }
            else
            {
                //Set BReakpoint to intercept LoadLibraryA() call for vsIOBoard.dll and apply Inputs patch
                SetBreakpoint(_LoadIOLibrary_BreakpointStruct);
            }

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        private void SetHack_NoCursor()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //pop esi
            CaveMemory.Write_StrBytes("5E");
            //push esi
            CaveMemory.Write_StrBytes("56");
            //mov eax, _CustomTransparentCursorHandle
            CaveMemory.Write_StrBytes("B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes((UInt32)_CustomTransparentCursorHandle));
            //mov [ebp-14], eax
            CaveMemory.Write_StrBytes("89 45 EC");

            //Inject it
            CaveMemory.InjectToOffset(_NoCursor_InjectionStruct, "No Cursor");
        }
        #endregion        

        #region Inputs Hack

        protected override void Apply_InputsMemoryHack()
        {
            Create_InputsDataBank();

            //Create players struct to pass to the game Listeners functions
            _P1_Struct_Address = InputsDatabank_Address;
            WriteByte(_P1_Struct_Address + (int)PlayerStructOffsets.Data + (int)PlayerDataOffsets.Value, 2);
            WriteByte(_P1_Struct_Address + (int)PlayerStructOffsets.Data + (int)PlayerDataOffsets.Id, 0);
            WriteByte(_P1_Struct_Address + (int)PlayerStructOffsets.Data + (int)PlayerDataOffsets.Type, 1);
            _P2_Struct_Address = InputsDatabank_Address + 0x20;
            WriteByte(_P2_Struct_Address + (int)PlayerStructOffsets.Data + (int)PlayerDataOffsets.Value, 2);
            WriteByte(_P2_Struct_Address + (int)PlayerStructOffsets.Data + (int)PlayerDataOffsets.Id, 1);
            WriteByte(_P2_Struct_Address + (int)PlayerStructOffsets.Data + (int)PlayerDataOffsets.Type, 1);
            
            //Custom flag to call the listeners
            _Test_Address = InputsDatabank_Address + 0x40;
            _Service_Address = InputsDatabank_Address + 0x41;
            _P1_Start_Address = InputsDatabank_Address + 0x42;
            _P2_Start_Address = InputsDatabank_Address + 0x43;
            _Coin1_Address = InputsDatabank_Address + 0x44;
            _Coin2_Address = InputsDatabank_Address + 0x45;

            //Create TEXT data to be loaded by WIN32 API
            _Kernel32Text_Address = InputsDatabank_Address + 0x60;
            WriteBytes(_Kernel32Text_Address, ASCIIEncoding.ASCII.GetBytes(KERNEL32_DLL));
            _CreateThreadText_Address = InputsDatabank_Address + 0x70;
            WriteBytes(_CreateThreadText_Address, ASCIIEncoding.ASCII.GetBytes(CREATETHREAD_API));
           
            //Creating a bank to store game controls listeners functions
            Codecave32 ListenerBank = new Codecave32(_Process, GameProcess_ModuleBaseAddress);
            _ListenersBankAddress = (UInt32)ListenerBank.CaveAddress;
            
            //Disable DInput 
            WriteByte(GameProcess_ModuleBaseAddress + _DInputDisableKeyboard_Patch_Offset, 0x00);
            WriteBytes(GameProcess_ModuleBaseAddress + _DInputDisableMouse1_Patch_Offset, new byte[] { 0xEB, 0x58 });
            WriteBytes(GameProcess_ModuleBaseAddress + _DInputDisableMouse2_Patch_Offset, new byte[] { 0xE9, 0xAA, 0x01, 0x00, 0x00 });
            WriteByte(GameProcess_ModuleBaseAddress + _DInputDisableMouse3_Patch_Offset, 0xC3);
            WriteBytes(GameProcess_ModuleBaseAddress + _DInputDisableMouse4_Patch_Offset, new byte[] { 0xE9, 0x9D, 0x00, 0x00, 0x00 });

            //Create custom functions, but they will only be called if input hacks is enabled
            SetHack_CreateCallListeners_Procedure();
            SetHack_CreateInputsCheckProcedure();

            SetHack_CorrectLevelSelectAim();

            //Setting initial player gun positions
            WriteBytes(_P1_Struct_Address + (UInt32)PlayerStructOffsets.Data + (UInt32)PlayerDataOffsets.X, BitConverter.GetBytes((UInt16)(_ResolutionWidth / 2)));
            WriteBytes(_P1_Struct_Address + (UInt32)PlayerStructOffsets.Data + (UInt32)PlayerDataOffsets.Y, BitConverter.GetBytes((UInt16)(_ResolutionHeight / 2)));

            WriteBytes(_P2_Struct_Address + (UInt32)PlayerStructOffsets.Data + (UInt32)PlayerDataOffsets.X, BitConverter.GetBytes((UInt16)(_ResolutionWidth / 2)));
            WriteBytes(_P2_Struct_Address + (UInt32)PlayerStructOffsets.Data + (UInt32)PlayerDataOffsets.Y, BitConverter.GetBytes((UInt16)(_ResolutionHeight / 2)));

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        /// <summary>
        /// Patch the Usb IO Dll when it's loaded by the game
        /// </summary>
        private void SetHack_UsbPluginDll()
        {
            //Force return 1 for VsIoBoard_Calibrate()
            WriteBytes((UInt32)_VsIoBoardDll_BaseAddress + _VsIoBoard_Calibrate_Offset, new byte[] { 0xB8, 0x20, 0x01, 0x00, 0x00, 0xC3 });
            //Force return 0 for VsIoBoard_Close()
            WriteBytes((UInt32)_VsIoBoardDll_BaseAddress + _VsIoBoard_Close_Offset, new byte[] { 0x31, 0xC0, 0xC3 });
            //Force return 1 for VsIoBoard_DisableGunFlashes()
            WriteBytes((UInt32)_VsIoBoardDll_BaseAddress + _VsIoBoard_DisableGunFlashes_Offset, new byte[] { 0xB8, 0x01, 0x00, 0x00, 0x00, 0xC3 });
            //Force return 0 for VsIoBoard_GetDeviceState()
            WriteBytes((UInt32)_VsIoBoardDll_BaseAddress + _VsIoBoard_GetDeviceState_Offset, new byte[] { 0x31, 0xC0, 0xC3 });
            //Force return 0 for VsIoBoard_GetGunParams()
            WriteBytes((UInt32)_VsIoBoardDll_BaseAddress + _VsIoBoard_GetGunParams_Offset, new byte[] { 0x31, 0xC0, 0xC3 });
            //Force return 0 for VsIoBoard_GetNumCoins()
            WriteBytes((UInt32)_VsIoBoardDll_BaseAddress + _VsIoBoard_GetNumCoins_Offset, new byte[] { 0x31, 0xC0, 0xC3 });
            //Force return 2 for VsIoBoard_GetNumDefaultDevice()
            WriteBytes((UInt32)_VsIoBoardDll_BaseAddress + _VsIoBoard_GetNumDefaultDevice_Offsets, new byte[] { 0xB8, 0x02, 0x00, 0x00, 0xC3 });
            //Force return 0 for VsIoBoard_SetCalibration()
            WriteBytes((UInt32)_VsIoBoardDll_BaseAddress + _VsIoBoard_SetCalibration_Offset, new byte[] { 0x31, 0xC0, 0xC3 });
            //Force return 1 for VsIoBoard_SetFlashDelay()
            WriteBytes((UInt32)_VsIoBoardDll_BaseAddress + _VsIoBoard_SetFlashDelay_Offset, new byte[] { 0xB8, 0x01, 0x00, 0x00, 0x00, 0xC3 });
            //Force return 1 for VsIoBoard_SetGunAccuraccy()
            WriteBytes((UInt32)_VsIoBoardDll_BaseAddress + _VsIoBoard_SetGunAccuracy_Offset, new byte[] { 0xB8, 0x01, 0x00, 0x00, 0x00, 0xC3 });
            //Force return 0 for VsIoBoard_getxSyncPolarity()
            WriteBytes((UInt32)_VsIoBoardDll_BaseAddress + _VsIoBoard_GetHSyncPolarity_Offset, new byte[] { 0x31, 0xC0, 0xC3 });
            WriteBytes((UInt32)_VsIoBoardDll_BaseAddress + _VsIoBoard_GetVSyncPolarity_Offset, new byte[] { 0x31, 0xC0, 0xC3 });
            //Force return 1 for VsIoBoard_getVideoMode()
            WriteBytes((UInt32)_VsIoBoardDll_BaseAddress + _VsIoBoard_GetVideoMode_Offset, new byte[] { 0xB8, 0x01, 0x00, 0x00, 0x00, 0xC3 });

            SetHack_VsIoBoard_Init();
            SetHack_SetUpGun();
            SetHack_AddListener();
            SetHack_RemoveListener();

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        /// <summary>
        /// Create out own thread with custom procedure to handle events
        /// </summary>
        private void SetHack_VsIoBoard_Init()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, _VsIoBoardDll_BaseAddress);
            //push ebx
            CaveMemory.Write_StrBytes("53");
            //push ecx
            CaveMemory.Write_StrBytes("51");
            //push edx
            CaveMemory.Write_StrBytes("52");
            //push "Kernel32"
            CaveMemory.Write_StrBytes("68");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Kernel32Text_Address));
            //call dword ptr [LoadLibraryA_Ptr]
            CaveMemory.Write_StrBytes("FF 15");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _LoadLibraryA_FunctionPtr_Offset));
            //test eax,eax
            CaveMemory.Write_StrBytes("85 C0");
            //je Exit
            CaveMemory.Write_StrBytes("74 21");
            //push "GetProcAddress"
            CaveMemory.Write_StrBytes("68");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_CreateThreadText_Address));
            //push eax
            CaveMemory.Write_StrBytes("50");
            //call dword ptr [GetProcAddress_Ptr]
            CaveMemory.Write_StrBytes("FF 15");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _GetProcAddress_FunctionPtr_Offset));
            //test eax,eax
            CaveMemory.Write_StrBytes("85 C0");
            //je Exit
            CaveMemory.Write_StrBytes("74 11");
            //push 00
            CaveMemory.Write_StrBytes("6A 00");
            //push 00
            CaveMemory.Write_StrBytes("6A 00");
            //push 00
            CaveMemory.Write_StrBytes("6A 00");
            //push _InputsCheck_CustomFunction_Address  
            CaveMemory.Write_StrBytes("68");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_InputsCheck_CustomFunction_Address));
            //push 00
            CaveMemory.Write_StrBytes("6A 00");
            //push 00
            CaveMemory.Write_StrBytes("6A 00");
            //call eax  (-> CreateThread())
            CaveMemory.Write_StrBytes("FF D0");
            
            //Exit:
            //pop edx
            CaveMemory.Write_StrBytes("5A");
            //pop ecx
            CaveMemory.Write_StrBytes("59");
            //pop ebx
            CaveMemory.Write_StrBytes("5B");
            //xor eax, eax
            CaveMemory.Write_StrBytes("31 C0");
            //ret
            CaveMemory.Write_StrBytes("C3");

            //Inject
            CaveMemory.InjectToOffset(_VsIoBoard_Init_InjectionStruct, "vsIOBoard_Init");
        }

        /// <summary>
        /// Intercept calls to SetUpGun() procedure to extract Automode and skipedFrames parameter for each player
        /// AutoMode = 0 or 2 : not automatic
        /// AutoMode = 1 : Automatic
        /// </summary>
        private void SetHack_SetUpGun()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, _VsIoBoardDll_BaseAddress);
            //push ebx
            CaveMemory.Write_StrBytes("53");
            //mov eax,[esp+08]
            CaveMemory.Write_StrBytes("8B 44 24 08");
            //shl eax,05
            CaveMemory.Write_StrBytes("C1 E0 05");
            //mov ebx,_P1_Struct_Address
            CaveMemory.Write_StrBytes("BB");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_Struct_Address));
            //add ebx,eax
            CaveMemory.Write_StrBytes("01 C3");
            //mov eax,[esp+0C]
            CaveMemory.Write_StrBytes("8B 44 24 0C");
            //mov [ebx],eax
            CaveMemory.Write_StrBytes("89 03");
            //mov eax,[esp+14]
            CaveMemory.Write_StrBytes("8B 44 24 14");
            //mov [ebx+04],eax
            CaveMemory.Write_StrBytes("89 43 04");
            //pop ebx
            CaveMemory.Write_StrBytes("5B");
            //ret
            CaveMemory.Write_StrBytes("C3");

            //Inject it
            CaveMemory.InjectToOffset(_VsIoBoardDll_BaseAddress, _VsIoBoard_SetUpGun_InjectionStruct, "vsIOBoard_SetUpGun");
        }

        /// <summary>
        /// Intercept the different procedures the game is giving to the vsIOBoard.dll via the AddListener() function
        /// When a button/axis event occurs, the dll calls all registered listeners for ecah event
        /// </summary>
        private void SetHack_AddListener()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, (UInt32)_VsIoBoardDll_BaseAddress);
            //push ebp
            CaveMemory.Write_StrBytes("55");
            //mov ebp, esp
            CaveMemory.Write_StrBytes("8B EC");
            //push ebx
            CaveMemory.Write_StrBytes("53");
            //push ecx
            CaveMemory.Write_StrBytes("51");            
            //mov eax,_ListenersBankAddress + 4
            CaveMemory.Write_StrBytes("B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_ListenersBankAddress + 0x04));
            //mov ecx,[_ListenersBankAddress]
            CaveMemory.Write_StrBytes("8B 0D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_ListenersBankAddress));
            //shl ecx,02
            CaveMemory.Write_StrBytes("C1 E1 02");
            //add eax,ecx
            CaveMemory.Write_StrBytes("01 C8");
            //mov ebx,[ebp+08]
            CaveMemory.Write_StrBytes("8B 5D 08");            
            //mov [eax],ebx
            CaveMemory.Write_StrBytes("89 18");            
            //shr ecx, 2
            CaveMemory.Write_StrBytes("C1 E9 02");
            //inc ecx
            CaveMemory.Write_StrBytes("41");
            //mov [_ListenersBankAddress], ecx 
            CaveMemory.Write_StrBytes("89 0D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_ListenersBankAddress));
            //Exit:
            //pop ecx
            CaveMemory.Write_StrBytes("59");
            //pop ebx
            CaveMemory.Write_StrBytes("5B");
            //mov esp, ebp
            CaveMemory.Write_StrBytes("8B E5");
            //pop ebp
            CaveMemory.Write_StrBytes("5D");
            //mov eax,00000001
            CaveMemory.Write_StrBytes("B8 01 00 00 00");
            //ret
            CaveMemory.Write_StrBytes("C3");

            //Inject it
            CaveMemory.InjectToOffset(_VsIoBoardDll_BaseAddress, _VsIoBoard_AddListener_InjectionStruct, "vsIOBoard_AddListener");
        }

        /// <summary>
        /// Intercept the different procedures the game is asking the vsIOBoard.dll to delete via RemoveListener() function
        /// Remove all occurences of the same Listener from the list
        /// </summary>
        private void SetHack_RemoveListener()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, (UInt32)_VsIoBoardDll_BaseAddress);
            //push ebp
            CaveMemory.Write_StrBytes("55");
            //mov ebp,esp
            CaveMemory.Write_StrBytes("8B EC");
            //push ebx
            CaveMemory.Write_StrBytes("53");
            //push ecx
            CaveMemory.Write_StrBytes("51");
            //push edx
            CaveMemory.Write_StrBytes("52");
            //push edi
            CaveMemory.Write_StrBytes("57");

            //Start:
            //xor ebx,ebx
            CaveMemory.Write_StrBytes("31 DB");
            //mov edx,_ListenersBankAddress + 4
            CaveMemory.Write_StrBytes("BA");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_ListenersBankAddress + 0x04));
            //mov edi,[ebp+08]
            CaveMemory.Write_StrBytes("8B 7D 08");

            //CheckNextItem:
            //cmp [edx],edi
            CaveMemory.Write_StrBytes("39 3A");
            //je RemoveAndShift
            CaveMemory.Write_StrBytes("74 0E");
            //inc ebx
            CaveMemory.Write_StrBytes("43");
            //add edx,04
            CaveMemory.Write_StrBytes("83 C2 04");
            //cmp ebx,[_ListenersBankAddress]
            CaveMemory.Write_StrBytes("3B 1D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_ListenersBankAddress));
            //jl CheckNextItem
            CaveMemory.Write_StrBytes("7C F0");
            //jmp Exit
            CaveMemory.Write_StrBytes("EB 24");

            //RemoveAndShift
            //mov ecx,[_ListenersBankAddress]
            CaveMemory.Write_StrBytes("8B 0D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_ListenersBankAddress));
            //sub ecx,ebx
            CaveMemory.Write_StrBytes("29 D9");
            //ShiftNext:
            //mov eax,[edx+04]
            CaveMemory.Write_StrBytes("8B 42 04");
            //mov [edx],eax
            CaveMemory.Write_StrBytes("89 02");
            //dec ecx
            CaveMemory.Write_StrBytes("49");
            //add edx,04
            CaveMemory.Write_StrBytes("83 C2 04");
            //test ecx,ecx
            CaveMemory.Write_StrBytes("85 C9");
            //jne ShiftNext
            CaveMemory.Write_StrBytes("75 F3");
            //mov eax,[_ListenersBankAddress]
            CaveMemory.Write_StrBytes("8B 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_ListenersBankAddress));
            //dec eax
            CaveMemory.Write_StrBytes("48");
            //mov [_ListenersBankAddress],eax
            CaveMemory.Write_StrBytes("89 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_ListenersBankAddress));
            //jmp Start
            CaveMemory.Write_StrBytes("EB C0");

            //Exit:
            //pop edi
            CaveMemory.Write_StrBytes("5F");
            //pop edx
            CaveMemory.Write_StrBytes("5A");
            //pop ecx
            CaveMemory.Write_StrBytes("59");
            //pop ebx
            CaveMemory.Write_StrBytes("5B");
            //mov esp,ebp
            CaveMemory.Write_StrBytes("8B E5");
            //pop ebp
            CaveMemory.Write_StrBytes("5D");
            //mov eax,00000001
            CaveMemory.Write_StrBytes("B8 01 00 00 00");
            //ret
            CaveMemory.Write_StrBytes("C3");

            //Inject it
            CaveMemory.InjectToOffset(_VsIoBoardDll_BaseAddress, _VsIoBoard_RemoveListener_InjectionStruct, "vsIOBoard_RemoveListener");
        }

        /// <summary>
        /// This creates a function that will call all registered listener with 2 parameter :
        /// First is Listener index (0 is GUN, 4 is COIN, 8 is TEST, 12 is START)
        /// Second is the listener parameter (player Id, Gun data pointer, etc...)
        /// </summary>
        private void SetHack_CreateCallListeners_Procedure()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);
            //push ebp
            CaveMemory.Write_StrBytes("55");
            //mov ebp,esp
            CaveMemory.Write_StrBytes("8B EC");
            //push esi
            CaveMemory.Write_StrBytes("56");
            //push edi
            CaveMemory.Write_StrBytes("57");
            //push eax
            CaveMemory.Write_StrBytes("50");
            //push ebx
            CaveMemory.Write_StrBytes("53");
            //cmp dword ptr [_ListenersBankAddress],00
            CaveMemory.Write_StrBytes("83 3D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_ListenersBankAddress));   //First DWORD is number of regstered Listeners
            CaveMemory.Write_StrBytes("00");
            //je Exit
            CaveMemory.Write_StrBytes("74 22");
            //xor ebx,ebx
            CaveMemory.Write_StrBytes("31 DB");
            //mov esi,_ListenersBankAddress+4
            CaveMemory.Write_StrBytes("BE");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_ListenersBankAddress + 4));   //Second DWORD is start of Listeners PTR list
            //Loop:
            //mov ecx,[esi]
            CaveMemory.Write_StrBytes("8B 0E");
            //mov eax,[ecx]
            CaveMemory.Write_StrBytes("8B 01");
            //mov edi,[ebp+08]
            CaveMemory.Write_StrBytes("8B 7D 08");
            //add eax,edi
            CaveMemory.Write_StrBytes("01 F8");
            //mov edi,[ebp+0C]
            CaveMemory.Write_StrBytes("8B 7D 0C");
            //push edi
            CaveMemory.Write_StrBytes("57");
            //call dword ptr [eax]
            CaveMemory.Write_StrBytes("FF 10");
            //inc ebx
            CaveMemory.Write_StrBytes("43");
            //add esi,04
            CaveMemory.Write_StrBytes("83 C6 04");
            //cmp ebx,[_ListenersBankAddress]
            CaveMemory.Write_StrBytes("3B 1D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_ListenersBankAddress));   //Comparing to the Listeners num to loop
            //jl Loop
            CaveMemory.Write_StrBytes("7C E5");
            //Exit:
            //pop ebx
            CaveMemory.Write_StrBytes("5B");
            //pop eax
            CaveMemory.Write_StrBytes("58");
            //pop edi
            CaveMemory.Write_StrBytes("5F");
            //pop esi
            CaveMemory.Write_StrBytes("5E");
            //mov esp,ebp
            CaveMemory.Write_StrBytes("8B E5");
            //pop ebp
            CaveMemory.Write_StrBytes("5D");
            //ret
            CaveMemory.Write_StrBytes("C3");

            _CallListeners_CustomFunction_Address = (UInt32)CaveMemory.CaveAddress;
            Logger.WriteLog("Adding custom function CallListeners(int Index, int Param) at 0x" + _CallListeners_CustomFunction_Address.ToString("X8"));
        }

        /// <summary>
        /// Injectiong some custom procedure to check for our own events and new value, to trigger the registered listeners
        /// The place it is injected look like it's running once each frame
        /// </summary>
        private void SetHack_CreateInputsCheckProcedure()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);
            //LoopStart:            
            //Coin1:            
            //cmp byte ptr [_Coin1_Address],01
            CaveMemory.Write_StrBytes("80 3D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Coin1_Address));
            CaveMemory.Write_StrBytes("01");
            //jne Coin2
            CaveMemory.Write_StrBytes("75 13");
            //mov byte ptr [_Coin1_Address],00
            CaveMemory.Write_StrBytes("C6 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Coin1_Address));
            CaveMemory.Write_StrBytes("00");
            //push 01
            CaveMemory.Write_StrBytes("6A 01");
            //push 04
            CaveMemory.Write_StrBytes("6A 04");
            //call _CallListeners_CustomFunction_Address
            CaveMemory.Write_call(_CallListeners_CustomFunction_Address);
            //add esp,08
            CaveMemory.Write_StrBytes("83 C4 08");

            //Coin2:
            //cmp byte ptr [_Coin2_Address],01
            CaveMemory.Write_StrBytes("80 3D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Coin2_Address));
            CaveMemory.Write_StrBytes("01");
            //jne Service
            CaveMemory.Write_StrBytes("75 13");
            //mov byte ptr [_Coin2_Address],00
            CaveMemory.Write_StrBytes("C6 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Coin2_Address));
            CaveMemory.Write_StrBytes("00");
            //push 02
            CaveMemory.Write_StrBytes("6A 02");
            //push 04
            CaveMemory.Write_StrBytes("6A 04");
            //call _CallListeners_CustomFunction_Address
            CaveMemory.Write_call(_CallListeners_CustomFunction_Address);
            //add esp,08
            CaveMemory.Write_StrBytes("83 C4 08");

            //Service:
            //cmp byte ptr [_Service_Address],01
            CaveMemory.Write_StrBytes("80 3D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Service_Address));
            CaveMemory.Write_StrBytes("01");
            //jne Test
            CaveMemory.Write_StrBytes("75 13");
            //mov byte ptr [_Service_Address],00
            CaveMemory.Write_StrBytes("C6 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Service_Address));
            CaveMemory.Write_StrBytes("00");
            //push 00
            CaveMemory.Write_StrBytes("6A 00");
            //push 04
            CaveMemory.Write_StrBytes("6A 04");
            //call _CallListeners_CustomFunction_Address
            CaveMemory.Write_call(_CallListeners_CustomFunction_Address);
            //add esp,08
            CaveMemory.Write_StrBytes("83 C4 08");

            //Test:
            //cmp byte ptr [_Test_Address],01
            CaveMemory.Write_StrBytes("80 3D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Test_Address));
            CaveMemory.Write_StrBytes("01");
            //jne P1Start
            CaveMemory.Write_StrBytes("75 13");
            //mov byte ptr [_Test_Address],00
            CaveMemory.Write_StrBytes("C6 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Test_Address));
            CaveMemory.Write_StrBytes("00");
            //push 01
            CaveMemory.Write_StrBytes("6A 01");
            //push 08
            CaveMemory.Write_StrBytes("6A 08");
            //call _CallListeners_CustomFunction_Address
            CaveMemory.Write_call(_CallListeners_CustomFunction_Address);
            //add esp,08
            CaveMemory.Write_StrBytes("83 C4 08");

            //P1 Start:
            //cmp byte ptr [_P1_Start_Address],01
            CaveMemory.Write_StrBytes("80 3D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_Start_Address));
            CaveMemory.Write_StrBytes("01");
            //jne P2Start
            CaveMemory.Write_StrBytes("75 13");
            //mov byte ptr [_P1_Start_Address],00
            CaveMemory.Write_StrBytes("C6 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_Start_Address));
            CaveMemory.Write_StrBytes("00");
            //push 01
            CaveMemory.Write_StrBytes("6A 01");
            //push 0C
            CaveMemory.Write_StrBytes("6A 0C");
            //call _CallListeners_CustomFunction_Address
            CaveMemory.Write_call(_CallListeners_CustomFunction_Address);
            //add esp,08
            CaveMemory.Write_StrBytes("83 C4 08");

            //P2 Start:
            //cmp byte ptr [_P2_Start_Address],01
            CaveMemory.Write_StrBytes("80 3D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P2_Start_Address));
            CaveMemory.Write_StrBytes("01");
            //jne P1 Axis
            CaveMemory.Write_StrBytes("75 13");
            //mov byte ptr [_P2_Start_Address],00
            CaveMemory.Write_StrBytes("C6 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P2_Start_Address));
            CaveMemory.Write_StrBytes("00");
            //push 02
            CaveMemory.Write_StrBytes("6A 02");
            //push 0C
            CaveMemory.Write_StrBytes("6A 0C");
            //call _CallListeners_CustomFunction_Address
            CaveMemory.Write_call(_CallListeners_CustomFunction_Address);
            //add esp,08
            CaveMemory.Write_StrBytes("83 C4 08");

            //GunButtons:
            //xor edi, edi
            CaveMemory.Write_StrBytes("31 FF");
            //mov eax,_P1_Struct_Address
            CaveMemory.Write_StrBytes("B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_Struct_Address));
            //mov ebx,edi
            CaveMemory.Write_StrBytes("8B DF");
            //shl ebx,05
            CaveMemory.Write_StrBytes("C1 E3 05");
            //add eax, ebx
            CaveMemory.Write_StrBytes("01 D8");
            //movzx ebx,byte ptr [eax+0B]
            CaveMemory.Write_StrBytes("0F B6 58 0B");
            //shl ebx,1
            CaveMemory.Write_StrBytes("D1 E3");
            //mov [eax+1A],bl
            CaveMemory.Write_StrBytes("88 58 1A");
            //mov cl,[eax+0A]
            CaveMemory.Write_StrBytes("8A 48 0A");
            //or [eax+1A], cl
            CaveMemory.Write_StrBytes("08 48 1A");
            //test cl,cl
            CaveMemory.Write_StrBytes("84 C9");
            //jne TriggerPressed
            CaveMemory.Write_StrBytes("75 09");
            //mov byte ptr [eax+09],00
            CaveMemory.Write_StrBytes("C6 40 09 00");
            //mov [eax+08],cl
            CaveMemory.Write_StrBytes("88 48 08");
            //jmp SendGunData
            CaveMemory.Write_StrBytes("EB 31");
            //TriggerPressed:
            //mov ebx,[eax+08]
            CaveMemory.Write_StrBytes("8B 58 08");
            //and ebx,01
            CaveMemory.Write_StrBytes("83 E3 01");
            //test ebx,ebx
            CaveMemory.Write_StrBytes("85 DB");
            //je CheckFramesCount
            CaveMemory.Write_StrBytes("74 16");
            //mov bl,[eax+09]
            CaveMemory.Write_StrBytes("8A 58 09");
            //inc bl
            CaveMemory.Write_StrBytes("FE C3");
            //mov [eax+09],bl
            CaveMemory.Write_StrBytes("88 58 09");
            //cmp bl,[eax+04]
            CaveMemory.Write_StrBytes("3A 58 04");
            //jle CheckFramesCount
            CaveMemory.Write_StrBytes("7E 09");
            //cmp dword ptr [eax],01
            CaveMemory.Write_StrBytes("83 38 01");
            //jne CheckFramesCount
            CaveMemory.Write_StrBytes("75 04");
            //mov byte ptr [eax+09],00
            CaveMemory.Write_StrBytes("C6 40 09 00");
            //CheckFramesCount:
            //mov [eax+08],cl
            CaveMemory.Write_StrBytes("88 48 08");
            //cmp byte ptr [eax+09],00
            CaveMemory.Write_StrBytes("80 78 09 00");
            //je SendGunData
            CaveMemory.Write_StrBytes("74 08");
            //movzx ebx,cl
            CaveMemory.Write_StrBytes("0F B6 D9");
            //not bl
            CaveMemory.Write_StrBytes("F6 D3");
            //and byte ptr[eax+1A],bl
            CaveMemory.Write_StrBytes("20 58 1A");
            //SendGunData:
            //add eax, 12
            CaveMemory.Write_StrBytes("83 C0 0C");
            //push GunData
            CaveMemory.Write_StrBytes("50");
            //push 00
            CaveMemory.Write_StrBytes("6A 00");
            //call _CallListeners_CustomFunction_Address
            CaveMemory.Write_call(_CallListeners_CustomFunction_Address);
            //add esp,08
            CaveMemory.Write_StrBytes("83 C4 08");
            //Break:
            //inc edi
            CaveMemory.Write_StrBytes("47");
            //cmp edi, 2
            CaveMemory.Write_StrBytes("83 FF 02");
            //jl PlayerData
            CaveMemory.Write_StrBytes("7C 93"); 

            //Sleep(16ms) ~= 1 frame duration in 60fps (16,7ms)
            //push 16
            CaveMemory.Write_StrBytes("6A 10");
            //call dword ptr [Sleep_Ptr]
            CaveMemory.Write_StrBytes("FF 15");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _Sleep_FunctionPtr_Offset));
            //jmp StartLoop
            CaveMemory.Write_StrBytes("E9 DC FE FF FF");

            _InputsCheck_CustomFunction_Address = (UInt32)CaveMemory.CaveAddress;
            Logger.WriteLog("Adding custom function CallListeners(int Index, int Param) at 0x" + _InputsCheck_CustomFunction_Address.ToString("X8"));
        }

        private void SetHack_CorrectLevelSelectAim()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //Correct X axis :
            //movzx ecx,word ptr [edi+0A]
            CaveMemory.Write_StrBytes("0F B7 4F 0A");
            //cvtsi2ss xmm0,ecx
            CaveMemory.Write_StrBytes("F3 0F 2A C1");
            //mov ecx,00000280
            CaveMemory.Write_StrBytes("B9 80 02 00 00");
            //cvtsi2ss xmm1,ecx
            CaveMemory.Write_StrBytes("F3 0F 2A C9");
            //mulss xmm0,xmm1
            CaveMemory.Write_StrBytes("F3 0F 59 C1");
            //mov ecx,_ResolutionWidth
            CaveMemory.Write_StrBytes("B9");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_ResolutionWidth));
            //cvtsi2ss xmm1,ecx
            CaveMemory.Write_StrBytes("F3 0F 2A C9");
            //divss xmm0,xmm1
            CaveMemory.Write_StrBytes("F3 0F 5E C1");
            //cvtss2si ecx,xmm0
            CaveMemory.Write_StrBytes("F3 0F 2D C8");
            //lea eax,[esi+1C]
            CaveMemory.Write_StrBytes("8D 46 1C");

            //mov [eax],ecx
            CaveMemory.Write_StrBytes("89 08");

            //Correct Y axis:
            //movzx edx,word ptr [edi+0C]
            CaveMemory.Write_StrBytes("0F B7 57 0C");
            //cvtsi2ss xmm0,edx
            CaveMemory.Write_StrBytes("F3 0F 2A C2");
            //mov edx,000001E0
            CaveMemory.Write_StrBytes("BA E0 01 00 00");
            //cvtsi2ss xmm1,edx
            CaveMemory.Write_StrBytes("F3 0F 2A CA");
            //mulss xmm0,xmm1
            CaveMemory.Write_StrBytes("F3 0F 59 C1");
            //mov edx,_ResolutionHeight
            CaveMemory.Write_StrBytes("BA");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_ResolutionHeight));
            //cvtsi2ss xmm1,edx
            CaveMemory.Write_StrBytes("F3 0F 2A CA");
            //divss xmm0,xmm1
            CaveMemory.Write_StrBytes("F3 0F 5E C1");
            //cvtss2si edx,xmm0
            CaveMemory.Write_StrBytes("F3 0F 2D D0");
            //lea ecx,[esi+20]
            CaveMemory.Write_StrBytes("8D 4E 20");

            //Inject it
            CaveMemory.InjectToOffset(_LevelSelectCorrection_InjectionStruct, "Level Select Aim Correction");
        }
        #endregion

        #endregion

        #region Inputs

        protected override void GameController_OnButtonDown(object sender, VirtualGameControler.ButtonDownEventArgs e)
        {
            if (e.ButtonName.Equals("EXIT"))
            {
                Close();
            }
            else if (e.ButtonName.Equals("COIN MECH. 1"))
            {
                WriteByte(_Coin1_Address, 0x01);
            }
            else if (e.ButtonName.Equals("COIN MECH. 2"))
            {
                WriteByte(_Coin2_Address, 0x01);
            }
            else if (e.ButtonName.Equals("SERVICE"))
            {
                WriteByte(_Service_Address, 0x01);
            }
            else if (e.ButtonName.Equals("TEST"))
            {
                WriteByte(_Test_Address, 0x01);
            }
        }

        protected override void GameController_OnButtonUp(object sender, VirtualGameControler.ButtonDownEventArgs e)
        {
            //Nothing to be done
        }

        //Gun data listener will be called once each frame
        //Axis are simple values that will be written directly in the gun data struct
        protected override void UpdateAxis(PlayerID Id, int X, int Y)
        {
            if (Id != PlayerID.None)
            {
                //Axis are encoded into 2-Bytes in the Player Data struct, need to cas that as Int16 otherwise it will override Buttons bytes right after in the struct
                byte[] bufferX = BitConverter.GetBytes((UInt16)X);
                byte[] bufferY = BitConverter.GetBytes((UInt16)Y);

                WriteBytes(_P1_Struct_Address + 32 * (UInt32)Id + (UInt32)PlayerStructOffsets.Data + (UInt32)PlayerDataOffsets.X, bufferX);
                WriteBytes(_P1_Struct_Address + 32 * (UInt32)Id + (UInt32)PlayerStructOffsets.Data + (UInt32)PlayerDataOffsets.Y, bufferY);
            }
        }

        //Trigger button, on the other hand, will need some tests to handle auto-fire when Trigger stays pressed
        //This is why a separate byte will receive the trigger state, and this will be computed to create the data copied in the gun data struct
        protected override void PlayerController_OnButtonDown(object sender, VirtualGameControler.ButtonDownEventArgs e)
        {
            if (e.Id != PlayerID.None)
            {
                UInt32 PlayerIndex = (UInt32)e.Id;

                if (e.ButtonName.Equals("TRIGGER"))
                {
                    WriteByte(_P1_Struct_Address + 32 * PlayerIndex + (UInt32)PlayerStructOffsets.CurrentTriggerState, 0x05);
                }
                else if (e.ButtonName.Equals("RELOAD"))
                {
                    WriteByte(_P1_Struct_Address + 32 * PlayerIndex + (UInt32)PlayerStructOffsets.ReloadState, 0x05);
                }
                else if (e.ButtonName.Equals("START"))
                {
                    WriteByte(_P1_Start_Address + PlayerIndex, 0x01);
                }
            }
        }
        protected override void PlayerController_OnButtonUp(object sender, VirtualGameControler.ButtonDownEventArgs e)
        {
            if (e.Id != PlayerID.None)
            {
                UInt32 PlayerIndex = (UInt32)e.Id;

                if (e.ButtonName.Equals("TRIGGER"))
                {
                    WriteByte(_P1_Struct_Address + 32 * PlayerIndex + (UInt32)PlayerStructOffsets.CurrentTriggerState, 0x00);
                }
                else if (e.ButtonName.Equals("RELOAD"))
                {
                    WriteByte(_P1_Struct_Address + 32 * PlayerIndex + (UInt32)PlayerStructOffsets.ReloadState, 0x00);
                }
            }
        }

        #endregion

        #region Outputs

        /// <summary>
        /// Create the Output list that we will be looking for and forward to MameHooker
        /// </summary>
        protected override void CreateOutputList()
        {
            _Outputs = new List<GameOutput>
            {
                new SyncBlinkingGameOutput(OutputId.P1_CtmLmpStart, 500),
                new SyncBlinkingGameOutput(OutputId.P2_CtmLmpStart, 500),
                new GameOutput(OutputId.P1_Ammo),
                new GameOutput(OutputId.P2_Ammo),
                new AsyncGameOutput(OutputId.P1_CtmRecoil, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_ON_DELAY), ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_OFF_DELAY), 0),
                new AsyncGameOutput(OutputId.P2_CtmRecoil, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_ON_DELAY), ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_OFF_DELAY), 0),
                new GameOutput(OutputId.P1_Life),
                new GameOutput(OutputId.P2_Life),
                new AsyncGameOutput(OutputId.P1_Damaged, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_DAMAGED_DELAY), 100, 0),
                new AsyncGameOutput(OutputId.P2_Damaged, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_DAMAGED_DELAY), 100, 0),
                new GameOutput(OutputId.Credits)
            };
        }

        /// <summary>
        /// Update all Outputs values before sending them to MameHooker
        /// </summary>
        public override void UpdateOutputValues()
        {            
            UInt32 _GameDataPtr = ReadPtr(GameProcess_ModuleBaseAddress + _OutputDataPtr_Offset);
            //In this game, the engine is swapping P1/P2 data according to the 1st Player to be started
            //To know which data is corresponding to which player, we need to process a little bit...
            //When players are uninitialized, both values are set to [-1]
            //When a player starts a game, PlayerA get a value of [0] if it's Player1, or [1] if it's player 2. This is used later by the game
            //to add an offset to determine which player is playing for accessing data.
            //When a second player is entering the game, PlayerB gets [0] if it's player1, [1] if it's player2
            byte[] Players_Offset = new byte[] { 0x00, 0x00 };
            Players_Offset[0] = ReadByte(_GameDataPtr + 0x28 + 0x45C);
            Players_Offset[1] = ReadByte(_GameDataPtr + 0x28 + 0x460);

            Array.Clear(_Life, 0, _Life.Length);
            Array.Clear(_Ammo, 0, _Ammo.Length);
            int[] StartLmp = new int[] { -1, -1 };

            for (uint i = 0; i < Players_Offset.Length; i++)
            {
                if (Players_Offset[i] != 0xFF)
                {
                    //We can now look at the "InGame" flag for the player
                    //1 = OnGame, 0 = Not Playing (GameOver or Continue)
                    if (ReadByte(_GameDataPtr + 0x28 + 0x439 + i) == 1)
                    {
                        //Force Start Lamp to Off
                        StartLmp[Players_Offset[i]] = 0;

                        _Life[Players_Offset[i]] = ReadByte(_GameDataPtr + 0x28 + 0x44C + (i * 4));
                        _Ammo[Players_Offset[i]] = ReadByte(ReadPtr(_GameDataPtr + 0x28 + 0x560 + (i * 4)) + 8);

                        //Custom Recoil
                        if (_Ammo[Players_Offset[i]] < _LastAmmo[Players_Offset[i]])
                        {
                            if (Players_Offset[i] == 0)
                                SetOutputValue(OutputId.P1_CtmRecoil, 1);
                            else if (Players_Offset[i] == 1)
                                SetOutputValue(OutputId.P2_CtmRecoil, 1);
                        }

                        //[Damaged] custom Output                
                        if (_Life[Players_Offset[i]] < _LastLife[Players_Offset[i]])
                        {
                            if (Players_Offset[i] == 0)
                                SetOutputValue(OutputId.P1_Damaged, 1);
                            else if (Players_Offset[i] == 1)
                                SetOutputValue(OutputId.P2_Damaged, 1);
                        }
                    }
                }
            }


            for (int i = 0; i < _LastAmmo.Length; i++)
            {
                _LastAmmo[i] = _Ammo[i];
                _LastLife[i] = _Life[i];
            }

            SetOutputValue(OutputId.P1_CtmLmpStart, StartLmp[0]);
            SetOutputValue(OutputId.P2_CtmLmpStart, StartLmp[1]);
            SetOutputValue(OutputId.P1_Ammo, _Ammo[0]);
            SetOutputValue(OutputId.P2_Ammo, _Ammo[1]);
            SetOutputValue(OutputId.P1_Life, _Life[0]);
            SetOutputValue(OutputId.P2_Life, _Life[1]);
            SetOutputValue(OutputId.Credits, ReadByte(ReadPtr(GameProcess_ModuleBaseAddress + _CreditsPtr_Offset)));
        }

        #endregion
    }
}
