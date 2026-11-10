using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
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
    public class Game_RwOpGhostJvs : BaseGame32
    {
        protected override bool PATCH_PEB_DEBUG_FLAG { get { return false; } }

        private const string RING_DATA_FOLDER = "sv";
        private const string RINGIODLL_NAME = "Ring_IO.dll";
        private UInt32 _RingIoDll_BaseAddress;

        private const string TESTMENU_OPTION = "TEST Menu";
        private const string REGION_OPTION = "Region";
        private const string FREEPLAY_OPTION = "Freeplay";
        private const string CREDITS_START_OPTION = "Credits to Start";
        private const string CREDITS_CONTINUE_OPTION = "Credits to Continue";
        private const string CREDITS_PERCOIN_OPTION = "Credits per coin";

        #region LOADER MEMORY ADDRESSES                
        private InjectionStruct32 _NoCursor_InjectionStruct = new InjectionStruct32(0x00112500, 6);
        #endregion

        #region INPUTS MEMORY ADDRESSES    
        private UInt32 _InitialCall_Offset = 0x001BB687;
        private UInt32 _LoadLibraryA_Ptr_Offset = 0x001DF030;
        private UInt32 _RingIo_DataConfigBuffer_Offset = 0x00054F10;
        private UInt32 _RomType_Patch_Offset = 0x00013446;
        private UInt32 _DipSwLang_Patch_Offset = 0x0018B62A;
        private UInt32 _EnableJvs_Patch_Offset = 0x0001314B;
        private UInt32 _EnableAmLib_Patch_Offset = 0x000131B4;
        private UInt32 _EnableLedBoard_Patch_Offset = 0x00013180;
        private UInt32 _EnableInputDevices_Patch_Offset = 0x0001B73A;
        private UInt32 _Calibration_Patch_Offset = 0x0009DF35;
        private NopStruct32 _Nop_RestoreAppSettings = new NopStruct32(0x00001B8D1, 2);
        private InjectionStruct32 _BootLoader_InjectionStruct = new InjectionStruct32(0x001A96E5, 5);
        private InjectionStruct32 _RingConfig_InjectionStruct = new InjectionStruct32(0x000014D2, 5);
        private InjectionStruct32 _AmLibSettings_InjectionStruct = new InjectionStruct32(0x000AC558, 6);
        private InjectionStruct32 _JvsButtons_InjectionStruct = new InjectionStruct32(0x00198BC1, 7);
        private InjectionStruct32 _JvsAxes_InjectionStruct = new InjectionStruct32(0x00198DC8, 5);
        private InjectionStruct32 _Credits_InjectionStruct = new InjectionStruct32(0x00198CDD, 5);
        private BreakpointStruct32 _RingIoUnpacked_Breakpointstruct = new BreakpointStruct32(0x634DD, 5);
        
        //Custom Data
        private UInt32 _Axes_CaveAddress;
        private UInt32 _Buttons_CaveAddress;
        private UInt32 _Credits_CaveAddress;
        private UInt32 _RingIoDll_Path_CaveAddress;
        private UInt32 _RingConfigData_CaveAddress;
        #endregion

        #region OUTPUTS MEMORY ADDRESSES
        private UInt32 _InternalLedOutputs_Ptr_Offset = 0x0065C1F4;
        private UInt32 _Credits_Offset = 0x002416C0;
        private InjectionStruct32 _InternalRecoil_InjectionStruct = new InjectionStruct32(0x00030A10, 5);

        //Custom Data
        private UInt32 _RecoilStatus_CaveAddress;
        #endregion

        //Region/Language selector --> see code sub_413520()

        //ROM TYPE:
        //0,3 : Final Jp/En
        //1   : Final Jp/En/Ru
        //2   : Final Ko/Zh   <-- crash
        private int _RomType = 1;

        //For RomType 1:
        //1 = Japanese
        //2 = English
        //4 = Russian (with DipSwitch set ON / 0x41)
        //4 = English (with DipSwitch set OFF / 0x40)
        //
        //For RomType 2:
        //4 = Korean (Game ERROR)
        //8 = Chineese (Game ERROR)
        private int _Region = 4;

        //DipSwitch : 0x40 or 0x41
        private byte _DipSwitch_Lang = 0x40;

        public Game_RwOpGhostJvs(GameConfig Config) : base(Config)
        {
            //Windows name will be LUA file loaded with '/' in path name
            _IsAnyWindowTitlePossible = true;

            _KnownMd5Prints.Add("Operation Ghost - Original Dump", "40f795933abc4f441c98acc778610aa2");
            _KnownMd5Prints.Add("Operation Ghost - For JConfig", "19a949581145ed8478637d286a4b85a0");

            if (_GameConfig.GetBoolOption(TESTMENU_OPTION))
                _GameLauncher.Set_CmdLine_Args("-testmode");

            string strRegion = _GameConfig.GetStringOption(REGION_OPTION, "EN");
            switch (strRegion.ToUpper())
            {
                case "JP": _Region = 1; _DipSwitch_Lang = 0x40; break;
                case "US": _Region = 2; _DipSwitch_Lang = 0x40; break;
                case "RU": _Region = 4; _DipSwitch_Lang = 0x41; break;
                default: _Region = 2; _DipSwitch_Lang = 0x40; break;
            }

            //RConfig needs "sv\E2PROM.bin" and "sv\SRAM.bin" or the game won't start.
            //Those file will not be created by the game if not present, thus they need to be copied if missing
            string E2promPath = _GameConfig.TargetPath.Replace(Path.GetFileName(_GameConfig.TargetFileName), RING_DATA_FOLDER + "\\E2PROM.bin");
            string SramPath = _GameConfig.TargetPath.Replace(Path.GetFileName(_GameConfig.TargetFileName), RING_DATA_FOLDER + "\\SRAM.bin");

            if (!File.Exists(E2promPath) || !File.Exists(SramPath))
            {
                try
                {
                    Logger.WriteLog("RConfig E2PROM or SRAM not found, copying default ones....");
                    string ZipPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Properties.Resources.DATA_PAYLOADS_FOLDER, "RConfig", "og.zip");
                    using (ZipArchive source = ZipFile.Open(ZipPath, ZipArchiveMode.Read, null))
                    {
                        foreach (ZipArchiveEntry entry in source.Entries)
                        {
                            string fullPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(_GameConfig.TargetPath), entry.FullName));

                            //Creating "sv" folder if missing
                            if (string.IsNullOrEmpty(entry.Name))
                            {
                                if (!Directory.Exists(fullPath))
                                    Directory.CreateDirectory(fullPath);

                                continue;
                            }

                            //Only extracting missing files to keep older ones
                            if (!File.Exists(fullPath))
                                entry.ExtractToFile(fullPath, true);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.WriteLog("Extraction error : " + ex.Message.ToString());
                }
                Logger.WriteLog("Extraction complete !");
            }
        }

        protected override void AttachDebuggerEvents()
        {
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

                if (_IsFirstException)
                {
                    Logger.WriteLog(" > Initital Breakpoint");
                    Apply_MemoryHacks();
                    _IsFirstException = false;
                }
                else
                {
                    if ((UInt32)e.ExceptionInfo.ExceptionRecord.ExceptionAddress == _RingIoDll_BaseAddress + _RingIoUnpacked_Breakpointstruct.MemoryOffset)
                    {
                        Logger.WriteLog(" > RingIO.dll unpacked breakpoint reached");
                        if (!RemoveBreakpoint(_RingIoUnpacked_Breakpointstruct, e.dwThreadId, _RingIoDll_BaseAddress))
                            return;

                        e.Handled = true;
                        SetHack_RingIOConfig();
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
                if (string.Equals(Path.GetFileName(e.DllPath), RINGIODLL_NAME, StringComparison.OrdinalIgnoreCase))
                {
                    Logger.WriteLog("==> Wanted DLL found !");
                    //HANDLE should always be in 32Bits range (https://learn.microsoft.com/fr-fr/windows/win32/winprog64/interprocess-communication?redirectedfrom=MSDN)
                    Logger.WriteLog("  + hModule: 0x" + e.LoadDllInfo.hFile.ToString("X8"));
                    Logger.WriteLog("  + fUnicode: " + e.LoadDllInfo.fUnicode.ToString());

                    //At that point, the loaded dll has only the UPX stub that will unpack the original code
                    //Setting a breakpoint at the end of the unpacking process will allow to access original code bytes and mod it
                    _RingIoDll_BaseAddress = (UInt32)e.LoadDllInfo.lpBaseOfDll;
                    SetBreakpoint(_RingIoUnpacked_Breakpointstruct, _RingIoDll_BaseAddress);
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
                    double dMaxX = 1023.0;
                    double dMaxY = 1023.0;
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
            base.Apply_MemoryHacks();

            //GameHooked event will be triggered once the RConfig DLL has been loaded if Input support is checked
            //Overwise, it needs to be done now
            if (_GameConfig.NoInputs)
                RaiseGameHookedEvent();
        }

        #region Loader Hack

        protected override void Apply_LoaderMemoryHack()
        {
            if (_GameConfig.NoCursor)
            {
                if (CreateTransparentCursor())
                    SetHack_NoCursor();
            }

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        /// <summary>
        /// Replacing call to LoadCursor() by  setting hCursor value in EAX
        /// </summary>
        private void SetHack_NoCursor()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //mov eax, _CustomTransparentCursorHandle
            CaveMemory.Write_StrBytes("B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes((UInt32)_CustomTransparentCursorHandle));
            //add esp, 8
            CaveMemory.Write_StrBytes("83 C4 08");

            //Inject it
            CaveMemory.InjectToOffset(_NoCursor_InjectionStruct, "No Cursor");
        }
        
        #endregion        

        #region Inputs Hack

        protected override void Apply_InputsMemoryHack()
        {
            Create_InputsDataBank();
            _Axes_CaveAddress = InputsDatabank_Address;     //Game is reading for each player, 2x2bytes for each (= 16 in total)
            _Buttons_CaveAddress = InputsDatabank_Address + 0x10;
            _Credits_CaveAddress = InputsDatabank_Address + 0x18;
            _RingIoDll_Path_CaveAddress = InputsDatabank_Address + 0x20;   //char[255]
            _RingConfigData_CaveAddress = InputsDatabank_Address + 0x120; //244 Bytes

            //Writing the DLL path to load it with LoadLibraryA();
            string RingIoDllPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Properties.Resources.DATA_PAYLOADS_FOLDER, @"RConfig\Ring_IO.dll");
            WriteBytes(_RingIoDll_Path_CaveAddress, Encoding.ASCII.GetBytes(RingIoDllPath));

            //Fedding the content of ring_config.bin directly to the DLL, removing the need of the file
            //This is almost "no options activated at all" config for RConfig.
            WriteBytes(_RingConfigData_CaveAddress, new byte[]{
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFA, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x3C, 0x00, 0x00, 0x00, 0x28, 0x00, 0x00, 0x00, 0x10, 0x00, 0x00, 0x00, 0x00, 0x05, 0x00, 0x00, 0xD0, 0x02, 0x00, 0x00, 0x00, 0x05, 0x00, 0x00, 0xD0, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x06, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x1B, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
            });

            SetHack_Bootloader();

            SetHack_AmLibSettings();

            //Force RomType param, only read in the ini file
            List<byte> bCode = new List<byte> { 0xB8 };
            bCode.AddRange(BitConverter.GetBytes(_RomType));
            for (int i = 0; i < 9; i++)
                bCode.Add(0x90);
            WriteBytes(GameProcess_ModuleBaseAddress + _RomType_Patch_Offset, bCode.ToArray());

            //Force DipSwitch read for Languauge option
            WriteBytes(GameProcess_ModuleBaseAddress + _DipSwLang_Patch_Offset, new byte[] { 0xB1, (byte)~_DipSwitch_Lang, 0x90, 0x90 });

            //Force EnableJvs = TRUE after reading ini file
            //mov eax, 1
            WriteBytes(GameProcess_ModuleBaseAddress + _EnableJvs_Patch_Offset, new byte[] { 0xB8, 0x01, 0x00, 0x00, 0x00 });

            //Force EnableAmLib = TRUE after reading ini file
            //mov edx, 1
            WriteBytes(GameProcess_ModuleBaseAddress + _EnableAmLib_Patch_Offset, new byte[] { 0xBA, 0x01, 0x00, 0x00, 0x00 });

            //Force EnableLedBoard = FALSE after reading ini file
            //xor eax, eax
            WriteBytes(GameProcess_ModuleBaseAddress + _EnableLedBoard_Patch_Offset, new byte[] { 0x31, 0xC0, 0xEB, 0x0A, 0x90, 0x90, 0x90 });

            //Jumping over if EnableInputDevices = FALSE
            WriteBytes(GameProcess_ModuleBaseAddress + _EnableInputDevices_Patch_Offset, new byte[] { 0xE9, 0x9F, 0x00, 0x00, 0x00, 0x90 });

            SetHack_Buttons();
            SetHack_Credits();
            SetHack_Axis();

            //Force the game to skip calibration procedure so that injected axes value remains perfect
            if (!_GameConfig.GetBoolOption(TESTMENU_OPTION))
                WriteByte(GameProcess_ModuleBaseAddress + _Calibration_Patch_Offset, 0xEB);

            //The game checks some OS detail with GetVersionEx() at start, and it seems it's checking OSVERSIONINFOEX::wSuiteMask byte for VER_SUITE_TERMINAL flag
            //to set a Flag enabling EEPROM data loading (AppSettingByRingWide / AppSettingByJVS)
            //To restore that : 
            //- Either force GetVersionEx() to success as wanted        ( => Tests for unwanted behavior ?)
            //- Or just NOP the check for 'AppSettingByRingWide' flag   ( => Less complete solution but no erratic behavior outside of this check)
            SetNops(GameProcess_ModuleBaseAddress, _Nop_RestoreAppSettings);
            //WriteBytes(GameProcess_ModuleBaseAddress + 0x13052, new byte[] { 0x90, 0x90 }); <-- GetVersionEx() patch                     

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        /// <summary>
        /// At .exe entry, adding a small codecave to call LoadLibraryA() with RConfig dll
        /// </summary>
        private void SetHack_Bootloader()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //push _RingIoDll_Path_CaveAddress
            CaveMemory.Write_StrBytes("68");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_RingIoDll_Path_CaveAddress));
            //call _LoadLibraryA();
            CaveMemory.Write_StrBytes("FF 15");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _LoadLibraryA_Ptr_Offset));
            //call InitialCallAddress
            CaveMemory.Write_call(GameProcess_ModuleBaseAddress + _InitialCall_Offset);

            //Inject it
            CaveMemory.InjectToOffset(_BootLoader_InjectionStruct, "Bootloader");
        }

        /// <summary>
        /// Editing Ring_IO.dll so that the data normally read in the local ring_config.bin is relaced by some data directly by U-Shoot
        /// ring_config has all kind of options for the emulator layer and data is mandatory
        /// </summary>
        private void SetHack_RingIOConfig()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, _RingIoDll_BaseAddress);

            //push edi
            CaveMemory.Write_StrBytes("57");
            //push ecx
            CaveMemory.Write_StrBytes("51");
            //mov esi,_RingConfigData_CaveAddress
            CaveMemory.Write_StrBytes("BE");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_RingConfigData_CaveAddress));
            //mov edi,RingIo.Dll+_RingIo_DataConfigBuffer_Offset
            CaveMemory.Write_StrBytes("BF");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_RingIoDll_BaseAddress + _RingIo_DataConfigBuffer_Offset));
            //mov ecx,0000003D
            CaveMemory.Write_StrBytes("B9 3D 00 00 00");
            //repe movsd
            CaveMemory.Write_StrBytes("F3 A5");
            //mov esi,00000000
            CaveMemory.Write_StrBytes("BE 00 00 00 00");
            //pop ecx
            CaveMemory.Write_StrBytes("59");
            //pop edi
            CaveMemory.Write_StrBytes("5F");

            //Inject it
            CaveMemory.InjectToOffset(_RingConfig_InjectionStruct, "RingConfig data");
        }

        /// <summary>
        /// Just after the game has loaded the EEPROM backup data, overriding needed values
        /// Mostly Credits data
        /// And Region
        /// </summary>
        private void SetHack_AmLibSettings()
        {
            byte bFreeplay = _GameConfig.GetBoolOption(FREEPLAY_OPTION, false) ? (byte)0x01 : (byte)0x00;
            byte bStart = 1;
            byte bContinue = 1;
            byte bCoinsCredits = 1;
            byte.TryParse(_GameConfig.GetStringOption(CREDITS_START_OPTION, "1"), out bStart);
            byte.TryParse(_GameConfig.GetStringOption(CREDITS_CONTINUE_OPTION, "1"), out bContinue);
            byte.TryParse(_GameConfig.GetStringOption(CREDITS_PERCOIN_OPTION, "1"), out bCoinsCredits);

            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //lea edi,[ebp+00000928]
            CaveMemory.Write_StrBytes("8D BD 28 09 00 00");
            //mov byte ptr [ebp+4C], Region
            CaveMemory.Write_StrBytes("C6 45 4C");
            CaveMemory.Write_Byte((byte)_Region);
            //mov byte ptr [ebp+6A], FREEPLAY
            CaveMemory.Write_StrBytes("C6 45 6A");
            CaveMemory.Write_Byte(bFreeplay);
            //mov byte ptr [ebp+6B], CREDITS PER COIN
            CaveMemory.Write_StrBytes("C6 45 6B");
            CaveMemory.Write_Byte(bCoinsCredits);
            //mov byte ptr [ebp+6F], CREDITS TO START
            CaveMemory.Write_StrBytes("C6 45 6F");
            CaveMemory.Write_Byte(bStart);
            //mov byte ptr [ebp+70], CREDITS TO CONTINUE
            CaveMemory.Write_StrBytes("C6 45 70");
            CaveMemory.Write_Byte(bContinue);

            //Inject it
            CaveMemory.InjectToOffset(_AmLibSettings_InjectionStruct, "Credits Param");
        }

        /// <summary>
        /// Instead of reading the JVS buffer input, reading custom buffer
        /// </summary>
        private void SetHack_Axis()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //mov ecx,[esp]
            CaveMemory.Write_StrBytes("8B 0C 24");
            //shl ecx,1
            CaveMemory.Write_StrBytes("D1 E1");
            //add ecx,_Axes_CaveAddress
            CaveMemory.Write_StrBytes("81 C1");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Axes_CaveAddress));
            //mov ecx,[ecx]
            CaveMemory.Write_StrBytes("8B 09");
            //and ecx,0000FFFF
            CaveMemory.Write_StrBytes("81 E1 FF FF 00 00");
            //mov [ebx],cx
            CaveMemory.Write_StrBytes("66 89 0B");
            //xor eax,eax
            CaveMemory.Write_StrBytes("31 C0");

            //Inject it
            CaveMemory.InjectToOffset(_JvsAxes_InjectionStruct, "Axes");
        }

        /// <summary>
        /// Instead of reading the JVS buffer input, reading custom buffer
        /// </summary>
        private void SetHack_Buttons()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //lea ecx,[esi-A]
            CaveMemory.Write_StrBytes("8D 4E F6");
            //add ecx,_Buttons_CaveAddress
            CaveMemory.Write_StrBytes("81 C1");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Buttons_CaveAddress));

            //Inject it
            CaveMemory.InjectToOffset(_JvsButtons_InjectionStruct, "Buttons");
        }

        /// <summary>
        /// Instead of reading the JVS buffer input, reading custom buffer
        /// </summary>
        private void SetHack_Credits()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //mov eax,[esp+10]
            CaveMemory.Write_StrBytes("8B 44 24 10");
            //sub eax,01
            CaveMemory.Write_StrBytes("83 E8 01");
            //shl eax,02
            CaveMemory.Write_StrBytes("C1 E0 02");
            //add eax,_Credits_CaveAddress
            CaveMemory.Write_StrBytes("05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Credits_CaveAddress));
            //mov eax,[eax]
            CaveMemory.Write_StrBytes("8B 00");
            //mov [ebx],ax
            CaveMemory.Write_StrBytes("66 89 03");
            //xor eax,eax
            CaveMemory.Write_StrBytes("31 C0");

            //Inject it
            CaveMemory.InjectToOffset(_Credits_InjectionStruct, "Credits");
        }

        #endregion

        #region Outputs Hack

        protected override void Apply_OutputsMemoryHack()
        {
            Create_OutputsDataBank();
            _RecoilStatus_CaveAddress = OutputsDatabank_Address;

            SetHack_Recoil();

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        /// <summary>
        /// In that function 'RequestRecoil()' the game calls a procedure to get the WeaponManager corresponding to the event
        /// When JVS emulation is enabled, it returns 3(P1) or 4(P2)
        /// When JVS emulation is disabled, it returns 1(P1) or 2(P2)
        /// When JVS emulation is enabled, output can be obtained by the JVS report bytes, so this injection will only be needed for non-JVS
        /// </summary>
        private void SetHack_Recoil()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //mov eax,[eax]
            CaveMemory.Write_StrBytes("8B 00");
            //sub eax,03
            CaveMemory.Write_StrBytes("83 E8 03");
            //mov byte ptr[eax+_Recoil_CaveAddress], 1
            CaveMemory.Write_StrBytes("C6 80");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_RecoilStatus_CaveAddress));
            CaveMemory.Write_StrBytes("01");

            //Inject it
            CaveMemory.InjectToOffset(_InternalRecoil_InjectionStruct, "Recoil");
        }

        #endregion

        #region No Crosshair Hack

        protected override void Apply_NoCrosshairMemoryHack()
        {
            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
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
                byte b = ReadByte(_Credits_CaveAddress);
                WriteByte(_Credits_CaveAddress, (byte)(b + 1));
            }
            else if (e.ButtonName.Equals("COIN MECH. 2"))
            {
                byte b = ReadByte(_Credits_CaveAddress + 0x04);
                WriteByte(_Credits_CaveAddress + 4, (byte)(b + 1));
            }
            else if (e.ButtonName.Equals("SERVICE"))
            {
                Apply_OR_ByteMask(_Buttons_CaveAddress + 0x01, 0x40);
            }
            else if (e.ButtonName.Equals("TEST"))
            {
                Apply_OR_ByteMask(_Buttons_CaveAddress, 0x80);
            }
        }

        protected override void GameController_OnButtonUp(object sender, VirtualGameControler.ButtonDownEventArgs e)
        {
            if (e.ButtonName.Equals("SERVICE"))
            {
                Apply_AND_ByteMask(_Buttons_CaveAddress + 0x01, 0xBF);
            }
            else if (e.ButtonName.Equals("TEST"))
            {
                Apply_AND_ByteMask(_Buttons_CaveAddress, 0x7F);
            }
        }

        protected override void UpdateAxis(PlayerID Id, int X, int Y)
        {
            if (Id != PlayerID.None)
            {
                byte[] bufferX = BitConverter.GetBytes(X << 6);
                byte[] bufferY = BitConverter.GetBytes(Y << 6);

                WriteByte(_Axes_CaveAddress + ((uint)Id * 0x04), bufferX[0]);
                WriteByte(_Axes_CaveAddress + 1 + ((uint)Id * 0x04), bufferX[1]);
                WriteByte(_Axes_CaveAddress + 2 + ((uint)Id * 0x04), bufferY[0]);
                WriteByte(_Axes_CaveAddress + 3 + ((uint)Id * 0x04), bufferY[1]);

                if (X < 5 || X > 1019 || Y < 5 || Y > 1019)
                    Apply_OR_ByteMask(_Buttons_CaveAddress + 1 + ((uint)Id * 2), 0x01);
                else
                    Apply_AND_ByteMask(_Buttons_CaveAddress + 1 + ((uint)Id * 2), 0xFE);
            }
        }

        protected override void PlayerController_OnButtonDown(object sender, VirtualGameControler.ButtonDownEventArgs e)
        {
            if (e.Id != PlayerID.None)
            {
                UInt32 PlayerIndex = (UInt32)e.Id;

                if (e.ButtonName.Equals("TRIGGER"))
                {
                    Apply_OR_ByteMask(_Buttons_CaveAddress + 1 + (PlayerIndex * 0x02), 0x02);
                }
                else if (e.ButtonName.Equals("RELOAD"))
                {
                    Apply_OR_ByteMask(_Buttons_CaveAddress + 1 + (PlayerIndex * 0x02), 0x01);
                }
                else if (e.ButtonName.Equals("CHANGE"))
                {
                    Apply_OR_ByteMask(_Buttons_CaveAddress + 2 + (PlayerIndex * 0x02), 0x40);
                }
                else if (e.ButtonName.Equals("ACTION"))
                {
                    Apply_OR_ByteMask(_Buttons_CaveAddress + 2 + (PlayerIndex * 0x02), 0x80);
                }
                else if (e.ButtonName.Equals("START"))
                {
                    Apply_OR_ByteMask(_Buttons_CaveAddress + 1 + (PlayerIndex * 0x02), 0x80);
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
                    Apply_AND_ByteMask(_Buttons_CaveAddress + 1 + (PlayerIndex * 0x02), 0xFD);
                }
                else if (e.ButtonName.Equals("RELOAD"))
                {
                    Apply_AND_ByteMask(_Buttons_CaveAddress + 1 + (PlayerIndex * 0x02), 0xFE);
                }
                else if (e.ButtonName.Equals("CHANGE"))
                {
                    Apply_AND_ByteMask(_Buttons_CaveAddress + 2 + (PlayerIndex * 0x02), 0xBF);
                }
                else if (e.ButtonName.Equals("ACTION"))
                {
                    Apply_AND_ByteMask(_Buttons_CaveAddress + 2 + (PlayerIndex * 0x02), 0x7F);
                }
                else if (e.ButtonName.Equals("START"))
                {
                    Apply_AND_ByteMask(_Buttons_CaveAddress + 1 + (PlayerIndex * 0x02), 0x7F);
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
                new GameOutput(OutputId.P1_LmpStart),
                new GameOutput(OutputId.P2_LmpStart),
                new GameOutput(OutputId.LmpBillboard),
                new GameOutput(OutputId.P1_LmpHolder),
                new GameOutput(OutputId.P2_LmpHolder),
                new AsyncGameOutput(OutputId.P1_CtmRecoil, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_ON_DELAY), ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_OFF_DELAY), 0),
                new AsyncGameOutput(OutputId.P2_CtmRecoil, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_ON_DELAY), ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_OFF_DELAY), 0),
                new GameOutput(OutputId.Credits)
            };
        }

        /// <summary>
        /// Update all Outputs values before sending them to MameHooker
        /// </summary>
        public override void UpdateOutputValues()
        {
            //ReadPtr() + 15E0 = Outputs
            //1 Byte per output (0 / 1)
            // +0 = Billboard
            // +1 = P1 Holder
            // +2 = P2 Holder
            // +3 = Unused --
            // +4 = P1 START
            // +5 = P2 START
            UInt32 LedStatusAddress = ReadPtr(GameProcess_ModuleBaseAddress + _InternalLedOutputs_Ptr_Offset) + 0x15E0;
            SetOutputValue(OutputId.P1_LmpStart, ReadByte(LedStatusAddress + 4));
            SetOutputValue(OutputId.P2_LmpStart, ReadByte(LedStatusAddress + 5));
            SetOutputValue(OutputId.LmpBillboard, ReadByte(LedStatusAddress));
            SetOutputValue(OutputId.P1_LmpHolder, ReadByte(LedStatusAddress + 1));
            SetOutputValue(OutputId.P2_LmpHolder, ReadByte(LedStatusAddress + 2));

            for (uint i = 0; i < _GameConfig.PlayerCount; i++)
            {
                if (ReadByte(_RecoilStatus_CaveAddress + i) == 1)
                {
                    SetOutputValue(OutputId.P1_CtmRecoil + i, 1);
                    WriteByte(_RecoilStatus_CaveAddress + i, 0x00);
                }
            }

            SetOutputValue(OutputId.Credits, ReadByte(GameProcess_ModuleBaseAddress + _Credits_Offset));

            //Credits will be calculated by using the formula  : Credits = Coins / CoinsByCredits
            //Warning : Need to handle "Divide by 0" error if game is closed brutally ! 
            //int Credits = 0;
            //UInt32 AmLibData_Address = ReadPtr(GameProcess_ModuleBaseAddress + _AmLibData_Ptr_Offset);
            //try
            //{
            //    if (AmLibData_Address != 0)
            //        Credits = ReadByte(AmLibData_Address + 0x1E0) / ReadByte(AmLibData_Address + 0x1D4);
            //}
            //catch { }
            //SetOutputValue(OutputId.Credits, Credits);
        }

        #endregion
    }
}
