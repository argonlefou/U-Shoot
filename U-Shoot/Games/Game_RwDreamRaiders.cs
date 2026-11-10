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
    public class Game_RwDreamRaiders : BaseGame32
    {
        protected override bool PATCH_PEB_DEBUG_FLAG { get { return false; } }

        private const string RING_DATA_FOLDER = "sv";
        private const string RINGIODLL_NAME = "Ring_IO.dll";
        private UInt32 _RingIoDll_BaseAddress;

        private const string FREEPLAY_OPTION = "Freeplay";
        private const string CREDITS_START_OPTION = "Credits to Start";
        private const string CREDITS_CONTINUE_OPTION = "Credits to Continue";

        #region LOADER MEMORY ADDRESSES  
        //private UInt32 _LevelSelect_NoCrosshair_Patch_Offset = 0x974C2;
        private UInt32 _Ingame_NoCrosshair_Patch_Offset = 0x8486D;
        private UInt32 _Freeplay_Offset = 0x607FE2;
        private UInt32 _CreditsToStart_Offset = 0x607FE7;
        private UInt32 _CreditsToContinue_Offset = 0x607FE8;
        private InjectionStruct32 _NoCursor_InjectionStruct = new InjectionStruct32(0x000E2404, 6);
        private InjectionStruct32 _Credits_InjectionStruct = new InjectionStruct32(0x000301A6F, 6);

        //Custom Data
        private UInt32 _Float00_CaveAddress;
        #endregion

        #region INPUTS MEMORY ADDRESSES
        private UInt32 _ScreenWidth_Offset = 0x007D827C;
        private UInt32 _ScreenHeight_Offset = 0x007D8280;
        private UInt32 _InitialCall_Offset = 0x003229E9;
        private UInt32 _LoadLibraryA_Ptr_Offset = 0x00335124;
        private UInt32 _RingIo_DataConfigBuffer_Offset = 0x00054F10;
        private UInt32 _JvsEnable_Patch_Offset = 0x000D9885;
        private UInt32 _GunEnable_Patch_Offset = 0x000D98A4;
        private UInt32 _SeatEnable_Patch_Offset = 0x000D98E9;
        private NopStruct32 _Nop_TestBtnCrash = new NopStruct32(0x0000181E, 6);
        private NopStruct32 _Nop_Keyboard = new NopStruct32(0x000F0F99, 7);
        private InjectionStruct32 _BootLoader_InjectionStruct = new InjectionStruct32(0x00314524, 5);
        private InjectionStruct32 _RingConfig_InjectionStruct = new InjectionStruct32(0x000014D2, 5);
        private InjectionStruct32 _JvsInputs_InjectionStruct = new InjectionStruct32(0x000DB97B, 5);
        private InjectionStruct32 _Calibration_InjectionStruct = new InjectionStruct32(0x000D94E2, 7);
        private BreakpointStruct32 _RingIoUnpacked_Breakpointstruct = new BreakpointStruct32(0x634DD, 5);

        //Custom Data
        private UInt32 _Axes_CaveAddress;
        private UInt32 _Buttons_CaveAddress;
        private UInt32 _RingIoDll_Path_CaveAddress;
        private UInt32 _RingConfigData_CaveAddress;
        #endregion

        #region OUTPUTS MEMORY ADDRESSES
        private UInt32 _Outputs_Offset = 0x0308A0A4;
        private UInt32 _P1_Status_Offset = 0x0118FF60;
        private UInt32 _P1_Life_Offset = 0x0118FECC;
        private UInt32 _P1_FiredBullets_Offset = 0x118FFBC;
        private UInt32 _Credits_Offset = 0x00607FF0;
        #endregion

        public Game_RwDreamRaiders(GameConfig Config) : base(Config)
        {
            _KnownMd5Prints.Add("Sega Dream Raider - Unpatched Header", "abedceb2135ab50c1182dda31153667687f53469");
            _KnownMd5Prints.Add("Sega Dream Raider - Patched Header", "ee96b126534355d96c12449291bdc44bb2d13e29");
            _KnownMd5Prints.Add("Sega Dream Raider - Original Dump", "4264540b2a24f3359a3deb5f1e95e392");
            _KnownMd5Prints.Add("Sega Dream Raider - For JConfig", "da993b12f7572f828c578c9eb73b3111");

            //RConfig needs "sv\E2PROM.bin" and "sv\SRAM.bin" or the game won't start.
            //Those file will not be created by the game if not present, thus they need to be copied if missing
            string E2promPath = _GameConfig.TargetPath.Replace(Path.GetFileName(_GameConfig.TargetFileName), RING_DATA_FOLDER + "\\E2PROM.bin");
            string SramPath = _GameConfig.TargetPath.Replace(Path.GetFileName(_GameConfig.TargetFileName), RING_DATA_FOLDER + "\\SRAM.bin");

            if (!File.Exists(E2promPath) || !File.Exists(SramPath))
            {
                try
                {
                    Logger.WriteLog("RConfig E2PROM or SRAM not found, copying default ones....");
                    string ZipPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Properties.Resources.DATA_PAYLOADS_FOLDER, "RConfig", "sdr.zip");
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

                    //Using the u sual 0x00-0xFF JVS data range for base axis data
                    //Using calculation later based on screen rersolution to force perfect calibration in-game
                    double dMinX = 0;
                    double dMinY = 0;
                    double dMaxX = 255.0;
                    double dMaxY = 255.0;
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
            Create_LoaderDataBank();
            _Float00_CaveAddress = LoaderDatabank_Address;            

            if (_GameConfig.NoCursor)
            {
                if (CreateTransparentCursor())
                    SetHack_NoCursor();
            }

            SetHack_Credits();

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

        private void SetHack_Credits()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            byte bFreeplay = _GameConfig.GetBoolOption(FREEPLAY_OPTION, false) ? (byte)0x01 : (byte)0x00;
            byte bStart = 1;
            byte bContinue = 1;
            byte.TryParse(_GameConfig.GetStringOption(CREDITS_START_OPTION, "1"), out bStart);
            byte.TryParse(_GameConfig.GetStringOption(CREDITS_CONTINUE_OPTION, "1"), out bContinue);

            //mov byte ptr[game.exe+_Freeplay_Offset], bFreeplay
            CaveMemory.Write_StrBytes("C6 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _Freeplay_Offset));
            CaveMemory.Write_Byte(bFreeplay);

            //mov byte ptr[game.exe+_CreditsToStart_Offset], bFreeplay
            CaveMemory.Write_StrBytes("C6 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _CreditsToStart_Offset));
            CaveMemory.Write_Byte(bStart);

            //mov byte ptr[game.exe+_CreditsToContinue_Offset], bFreeplay
            CaveMemory.Write_StrBytes("C6 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _CreditsToContinue_Offset));
            CaveMemory.Write_Byte(bContinue);

            //mov esi,[esp+1C]
            CaveMemory.Write_StrBytes("8B 74 24 1C");
            //test esi,esi
            CaveMemory.Write_StrBytes("85 F6");

            //Inject it
            CaveMemory.InjectToOffset(_Credits_InjectionStruct, "Credits");
        }

        #endregion

        #region Inputs Hack

        protected override void Apply_InputsMemoryHack()
        {
            Create_InputsDataBank();
            _Axes_CaveAddress = InputsDatabank_Address;    //2*4 bytes for JVS RAW + 2*4 bytes for calibrated
            _Buttons_CaveAddress = InputsDatabank_Address + 0x10;
            _RingIoDll_Path_CaveAddress = InputsDatabank_Address + 0x20;   //char[255]
            _RingConfigData_CaveAddress = InputsDatabank_Address + 0x120; //244 Bytes

            //Writing the DLL path to load it with LoadLibraryA();
            string JconfigDllPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Properties.Resources.DATA_PAYLOADS_FOLDER, @"RConfig\Ring_IO.dll");
            WriteBytes(_RingIoDll_Path_CaveAddress, Encoding.ASCII.GetBytes(JconfigDllPath));

            //Fedding the content of ring_config.bin directly to the DLL, removing the need of the file
            //This is almost "no options activated at all" config for RConfig.
            WriteBytes(_RingConfigData_CaveAddress, new byte[]{
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFA, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x3C, 0x00, 0x00, 0x00, 0x28, 0x00, 0x00, 0x00, 0x10, 0x00, 0x00, 0x00, 0x00, 0x05, 0x00, 0x00, 0xD0, 0x02, 0x00, 0x00, 0x00, 0x05, 0x00, 0x00, 0xD0, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x06, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x1B, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
            });

            //forcing JVSENABLE/GUNENABLE/SEATENABLE to use RConfig
            //JVS and GUN are default to "1", just skipping vale read from INI
            //Seat too, but we don't want that so forcing it to 0 before
            WriteByte(GameProcess_ModuleBaseAddress + _JvsEnable_Patch_Offset, 0xEB);
            WriteByte(GameProcess_ModuleBaseAddress + _GunEnable_Patch_Offset, 0xEB);
            WriteByte(GameProcess_ModuleBaseAddress + _SeatEnable_Patch_Offset, 0x00);
            WriteByte(GameProcess_ModuleBaseAddress + _SeatEnable_Patch_Offset + 0x04, 0xEB);

            SetHack_Bootloader();
            SetHack_Inputs();
            SetHack_Calibration();

            //Pressing the TEST button (or Keyboard equivalent) sets a byte to quit the game
            SetNops(GameProcess_ModuleBaseAddress, _Nop_TestBtnCrash);

            //NOPing call to GetKeyboardstate so that keyboard is not used (it is normally, even with JVS enabled)
            SetNops(GameProcess_ModuleBaseAddress, _Nop_Keyboard);

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        /// <summary>
        /// At .exe entry, adding a small codecave to call LoadLibraryA() with RConfig dll
        /// </summary>
        private void SetHack_Bootloader()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //push _JconfigDll_Path_CaveAddress
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
        /// Editing Ring_IO.dll so that the data normaally read in the local ring_config.bin is relaced by some data directly by U-Shoot
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
            //mov edi,LGI.exe+_RingIo_DataConfigBuffer_Offset
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
        /// Replacing the Inputs state after JVS sets all states
        /// </summary>
        private void SetHack_Inputs()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //mov ebx,[_Axes_CaveAddress]
            CaveMemory.Write_StrBytes("8B 1D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Axes_CaveAddress));
            //mov [esi+000000A8],ebx
            CaveMemory.Write_StrBytes("89 9E A8 00 00 00");

            //mov ebx,[_Axes_CaveAddress + 4]
            CaveMemory.Write_StrBytes("8B 1D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Axes_CaveAddress + 4));
            //mov [esi+000000AC],ebx
            CaveMemory.Write_StrBytes("89 9E AC 00 00 00");

            //mov bl,[_Buttons_CaveAddress]
            CaveMemory.Write_StrBytes("8A 1D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Buttons_CaveAddress));
            //mov [esi+000000C8],bl
            CaveMemory.Write_StrBytes("88 9E C8 00 00 00");

            //mov ebx,[_Buttons_CaveAddress + 4]
            CaveMemory.Write_StrBytes("8B 1D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Buttons_CaveAddress + 4));
            //mov [esi+000000CA],ebx
            CaveMemory.Write_StrBytes("89 9E CA 00 00 00");

            //mov ebx,[_Buttons_CaveAddress + 8]
            CaveMemory.Write_StrBytes("8B 1D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Buttons_CaveAddress + 8));
            //mov [esi+000000A4],ebx
            CaveMemory.Write_StrBytes("89 9E A4 00 00 00");

            //add eax,
            CaveMemory.Write_StrBytes("83 C0 F8");
            //pop ebx
            CaveMemory.Write_StrBytes("5B");
            //pop ecx
            CaveMemory.Write_StrBytes("59");


            //Inject it
            CaveMemory.InjectToOffset(_JvsInputs_InjectionStruct, "Buttons");
        }

        /// <summary>
        /// Overriding the game calibration by setting custom calibrated values once the game had done its own procedure
        /// </summary>
        private void SetHack_Calibration()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //mov ebp,[esp+04]
            CaveMemory.Write_StrBytes("8B 6C 24 04");
            //shl ebp,02
            CaveMemory.Write_StrBytes("C1 E5 02");
            //add ebp,_Axes_CaveAddress
            CaveMemory.Write_StrBytes("81 C5");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Axes_CaveAddress + 0x08));
            //mov ebp,[ebp+00]
            CaveMemory.Write_StrBytes("8B 6D 00");
            //and ebp,0000FFFF
            CaveMemory.Write_StrBytes("81 E5 FF FF 00 00");
            //mov [esi],ebp
            CaveMemory.Write_StrBytes("89 2E");

            //mov ebp,[esp+04]
            CaveMemory.Write_StrBytes("8B 6C 24 04");
            //shl ebp,02
            CaveMemory.Write_StrBytes("C1 E5 02");
            //add ebp,_Axes_CaveAddress
            CaveMemory.Write_StrBytes("81 C5");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Axes_CaveAddress + 0x0A));
            //mov ebp,[ebp+00]
            CaveMemory.Write_StrBytes("8B 6D 00");
            //and ebp,0000FFFF
            CaveMemory.Write_StrBytes("81 E5 FF FF 00 00");
            //mov [edi],ebp
            CaveMemory.Write_StrBytes("89 2F");

            //cmp dword ptr [game.exe+308A09C],00
            CaveMemory.Write_StrBytes("83 3D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + 0x308A09C));
            CaveMemory.Write_StrBytes("00");

            //Inject it
            CaveMemory.InjectToOffset(_Calibration_InjectionStruct, "Calibration");
        }

        #endregion

        #region Outputs Hack

        protected override void Apply_OutputsMemoryHack()
        {
            Create_OutputsDataBank();

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        #endregion

        #region No Crosshair Hack

        protected override void Apply_NoCrosshairMemoryHack()
        {
            //When drawing crosshair sprites, changing hardcoded ALPHA float value to custom 0.0f
            //Keeping it on menu select is better
            //WriteBytes(_LevelSelect_NoCrosshair_Patch_Offset, BitConverter.GetBytes(_Float00_CaveAddress));
            WriteBytes(GameProcess_ModuleBaseAddress + _Ingame_NoCrosshair_Patch_Offset, BitConverter.GetBytes(_Float00_CaveAddress));

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
                ushort CoinValue = BitConverter.ToUInt16(ReadBytes(_Buttons_CaveAddress + 0x08, 2), 0);
                WriteBytes(_Buttons_CaveAddress + 0x08 , BitConverter.GetBytes(CoinValue + 1));
            }
            else if (e.ButtonName.Equals("COIN MECH. 2"))
            {
                ushort CoinValue = BitConverter.ToUInt16(ReadBytes(_Buttons_CaveAddress + 0x0A, 2), 0);
                WriteBytes(_Buttons_CaveAddress + 0x0A, BitConverter.GetBytes(CoinValue + 1));
            }
            else if (e.ButtonName.Equals("SERVICE"))
            {
                Apply_OR_ByteMask(_Buttons_CaveAddress + 0x04, 0x40);
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
                Apply_AND_ByteMask(_Buttons_CaveAddress + 0x04, 0xBF);
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
                byte[] bufferX = BitConverter.GetBytes((UInt16)X);
                byte[] bufferY = BitConverter.GetBytes((UInt16)Y);

                WriteByte(_Axes_CaveAddress + 0x03 + ((uint)Id * 0x04), (byte)X);
                WriteByte(_Axes_CaveAddress + 0x01 + ((uint)Id * 0x04), (byte)Y);

                double CalibratedX = (double)X / 255.0 * (double)ReadUInt32(GameProcess_ModuleBaseAddress + _ScreenWidth_Offset);
                double CalibratedY = (double)Y / 255.0 * (double)ReadUInt32(GameProcess_ModuleBaseAddress + _ScreenHeight_Offset);
                WriteBytes(_Axes_CaveAddress + 8 + ((uint)Id * 0x04), BitConverter.GetBytes((UInt16)CalibratedX));
                WriteBytes(_Axes_CaveAddress + 10 + ((uint)Id * 0x04), BitConverter.GetBytes((UInt16)CalibratedY));
            }
        }

        protected override void PlayerController_OnButtonDown(object sender, VirtualGameControler.ButtonDownEventArgs e)
        {
            if (e.Id != PlayerID.None)
            {
                UInt32 PlayerIndex = (UInt32)e.Id;

                if (e.ButtonName.Equals("TRIGGER"))
                {
                    Apply_OR_ByteMask(_Buttons_CaveAddress + 0x04 + (PlayerIndex * 0x02), 0x02);
                }
                else if (e.ButtonName.Equals("START"))
                {
                    Apply_OR_ByteMask(_Buttons_CaveAddress + 0x04 + (PlayerIndex * 0x02), 0x80);
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
                    Apply_AND_ByteMask(_Buttons_CaveAddress + 0x04 + (PlayerIndex * 0x02), 0xFD);
                }
                else if (e.ButtonName.Equals("START"))
                {
                    Apply_AND_ByteMask(_Buttons_CaveAddress + 0x04 + (PlayerIndex * 0x02), 0x7E);
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
                new GameOutput(OutputId.P1_Lmp_R),
                new GameOutput(OutputId.P1_Lmp_G),
                new GameOutput(OutputId.P1_Lmp_B),
                new GameOutput(OutputId.P2_Lmp_R),
                new GameOutput(OutputId.P2_Lmp_G),
                new GameOutput(OutputId.P2_Lmp_B),
                new GameOutput(OutputId.LmpRear_R),
                new GameOutput(OutputId.LmpRear_G),
                new GameOutput(OutputId.LmpRear_B),
                new GameOutput(OutputId.Blower_Level),
                new GameOutput(OutputId.P1_GunMotor),
                new GameOutput(OutputId.P2_GunMotor),
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
            byte OutputData1 = ReadByte(GameProcess_ModuleBaseAddress + _Outputs_Offset);
            byte OutputData2 = ReadByte(GameProcess_ModuleBaseAddress + _Outputs_Offset + 1);
            byte OutputData3 = ReadByte(GameProcess_ModuleBaseAddress + _Outputs_Offset + 2);
            SetOutputValue(OutputId.P1_LmpStart, OutputData1 >> 7 & 0x01);
            SetOutputValue(OutputId.P2_LmpStart, OutputData1 >> 4 & 0x01);
            SetOutputValue(OutputId.P1_Lmp_R, OutputData2 >> 7 & 0x01);
            SetOutputValue(OutputId.P1_Lmp_G, OutputData2 >> 6 & 0x01);
            SetOutputValue(OutputId.P1_Lmp_B, OutputData2 >> 5 & 0x01);
            SetOutputValue(OutputId.P2_Lmp_R, OutputData2 >> 4 & 0x01);
            SetOutputValue(OutputId.P2_Lmp_G, OutputData2 >> 3 & 0x01);
            SetOutputValue(OutputId.P2_Lmp_B, OutputData2 >> 2 & 0x01);
            SetOutputValue(OutputId.LmpRear_R, OutputData2 >> 1 & 0x01);
            SetOutputValue(OutputId.LmpRear_G, OutputData2 & 0x01);
            SetOutputValue(OutputId.LmpRear_B, OutputData3 >> 7 & 0x01);
            SetOutputValue(OutputId.Blower_Level, (OutputData1 & 0x03) + (OutputData3 >> 4 & 0x04));
            SetOutputValue(OutputId.P1_GunMotor, OutputData1 >> 6 & 0x01);
            SetOutputValue(OutputId.P2_GunMotor, OutputData1 >> 3 & 0x01);

            for (uint i = 0; i < _GameConfig.PlayerCount; i++)
            {
                _Life[i] = 0;
                int Player_Status = ReadByte(GameProcess_ModuleBaseAddress + _P1_Status_Offset + (i * 0x9C));
                if (Player_Status == 1)
                {
                    _Life[i] = ReadByte(GameProcess_ModuleBaseAddress + _P1_Life_Offset + (i * 0x14));

                    //[Damaged] custom Output                
                    if (_Life[i] < _LastLife[i])
                        SetOutputValue(OutputId.P1_Damaged +i, 1);

                    _Ammo[i] = ReadInt32(GameProcess_ModuleBaseAddress + _P1_FiredBullets_Offset + (i * 0x9C));
                    if (_Ammo[i] > _LastAmmo[i])
                        SetOutputValue(OutputId.P1_CtmRecoil + i, 1);
                }

                _LastLife[i] = _Life[i];
                _LastAmmo[i] = _Ammo[i];

                SetOutputValue(OutputId.P1_Life + i, _Life[i]);
            }

            SetOutputValue(OutputId.Credits, ReadByte(GameProcess_ModuleBaseAddress + _Credits_Offset));
        }

        #endregion
    }
}
