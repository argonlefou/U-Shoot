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
    public class Game_RwLgi3d : BaseGame32
    {
        protected override bool PATCH_PEB_DEBUG_FLAG { get { return false; } }

        private const string RING_DATA_FOLDER = "sv";
        private const string RINGIODLL_NAME = "Ring_IO.dll";
        private UInt32 _RingIoDll_BaseAddress;

        private const string REGION_OPTION = "Region";
        private const string TESTMENU_OPTION = "TEST menu";
        private const string FREEPLAY_OPTION = "Freeplay";
        private const string CREDITS_START_OPTION = "Credits to Start";
        private const string CREDITS_CONTINUE_OPTION = "Credits to Continue";


        #region LOADER MEMORY ADDRESSES    
        private UInt32 _OutputCOM_Patch_Offset = 0x46B0BF;
        private UInt32 _GetRegion_Function_Offset = 0x2FD130;
        private UInt32 _AmFreeplay_Offset = 0x0056E1FA;
        private UInt32 _AmCreditsToStart_Offset = 0x0056E1FF;
        private UInt32 _AmCreditsTocontinue_Offset = 0x0056E201;
        private UInt32 _AmCreditsInit_Patch_Offset = 0x0008A60E;
        private UInt32 _3dOffLogo_Patch_Offset = 0x00595284;
        private InjectionStruct32 _NoCursor_InjectionStruct = new InjectionStruct32(0x00305A2E, 6);
        private InjectionStruct32 _ResolutionChange_InjectionStruct = new InjectionStruct32(0x004A421E, 7);
        private InjectionStruct32 _NoCrosshair_InjectionStruct = new InjectionStruct32(0x00497237, 5);

        //Custom Data
        private UInt32 _CrosshairCallerOffset = 0x0049773A;        
        #endregion

        #region INPUTS MEMORY ADDRESSES    
        private UInt32 _InitialCall_Offset = 0x002FF888;
        private UInt32 _LoadLibraryA_Ptr_Offset = 0x004FA0AC;
        private UInt32 _RingIo_DataConfigBuffer_Offset = 0x00054F10;
        private UInt32 _Axis_Patch_Offset = 0x002FE603;
        private InjectionStruct32 _BootLoader_InjectionStruct = new InjectionStruct32(0x002FF048, 5);
        private InjectionStruct32 _RingConfig_InjectionStruct = new InjectionStruct32(0x000014D2, 5);
        private InjectionStruct32 _Buttons_InjectionStruct = new InjectionStruct32(0x002FE592, 8);
        private InjectionStruct32 _TestBtn_InjectionStruct = new InjectionStruct32(0x002FE5CF, 7);
        private InjectionStruct32 _Coin_InjectionStruct = new InjectionStruct32(0x002FE666, 5);
        private InjectionStruct32 _CalibrationValues_InjectionStruct = new InjectionStruct32(0x0049DC27, 6);
        private BreakpointStruct32 _RingIoUnpacked_Breakpointstruct = new BreakpointStruct32(0x634DD, 5);

        //Custom Data
        private UInt32 _Axes_CaveAddress;
        private UInt32 _Buttons_CaveAddress;
        private UInt32 _Buttons2_CaveAddress;
        private UInt32 _Coins_CaveAddress;
        private UInt32 _RingIoDll_Path_CaveAddress;
        private UInt32 _RingConfigData_CaveAddress;
        #endregion

        #region OUTPUTS MEMORY ADDRESSES
        private UInt32 _OutputsPtr_Offset = 0x0065DA20;
        private UInt32 _PlayersStructPtr_Offset = 0x008429F0;
        private UInt32 _Credits_Offset = 0x842A88;
        private UInt32 _StrlenFunction_Offset = 0x002FF3EC;
        private InjectionStruct32 _Recoil_InjectionStruct = new InjectionStruct32(0x003518F1, 5);
        private InjectionStruct32 _CurrentSequence_InjectionStruct = new InjectionStruct32(0x0002615BF, 6);

        //Custom Data
        private UInt32 _P1_CustomRecoil_CaveAddress;
        private UInt32 _P2_CustomRecoil_CaveAddress;
        private UInt32 _CurrentSequenceStringLength;
        private UInt32 _CurrentSequenceString;
        #endregion

        private byte _GameRegion = 0x01; //JPN
        private bool _IsAttractMode = true;

        public Game_RwLgi3d(GameConfig Config) : base(Config)
        {
            _KnownMd5Prints.Add("Let's Go Island 3D - Original Dump", "ef9e3625684e0d52eab5bc1f0c68c7c3");
            _KnownMd5Prints.Add("Let's Go Island 3D - For JConfig", "49c5de5df60f475a965b2d894b3477c6");

            if (_GameConfig.GetBoolOption(TESTMENU_OPTION))
                _GameLauncher.Set_CmdLine_Args("-TestMode");

            switch (_GameConfig.GetStringOption(REGION_OPTION))
            {
                case "JPN": _GameRegion = 0x01; break;
                case "USA": _GameRegion = 0x02; break;
                case "EXP": _GameRegion = 0x04; break;
                case "CHN": _GameRegion = 0x08; break;
                default: break;
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
                    string ZipPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Properties.Resources.DATA_PAYLOADS_FOLDER, "RConfig", "lgi3d.zip");
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

                    //TEST menu can't be accessed to do calibration
                    //Choosen solution is to force Calibration Values for Min-Max axis to [0x00-0xFF] when we write axis values in memory
                    //So we can safely use full range of values now :   
                    double dMinX = 0;
                    double dMinY = 0;
                    double dMaxX = 255.0;
                    double dMaxY = 255.0;
                    double dRangeX = dMaxX - dMinX + 1;
                    double dRangeY = dMaxY - dMinY + 1;

                    PlayerController.Computed_X = Convert.ToInt32(dMaxX - Math.Round(dRangeX * PlayerController.Computed_X / TotalResX));
                    PlayerController.Computed_Y = Convert.ToInt32(dMaxY - Math.Round(dRangeY * PlayerController.Computed_Y / TotalResY));
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

            //Setting game resolution and window style
            SetHack_Resolution();

            //The game is opening a COM port to get-put data for chair motors/outputs, and keep it open for read/write forever...causing issue if a lightgun same PORT to send recoil
            WriteBytes(GameProcess_ModuleBaseAddress + _OutputCOM_Patch_Offset, new byte[] { 0xEB, 0x2B, 0x90, 0x90, 0x90, 0x90 });

            //Force returning the wanted REGION id when game asks for it
            //mov al, RegionID
            //ret
            WriteBytes(GameProcess_ModuleBaseAddress + _GetRegion_Function_Offset, new byte[] { 0xB0, _GameRegion, 0xC3, 0x90, 0x90 });

            //Game is loading hardcoded default values for credits settings, whatever JConfig is saving in EEPROM
            //Using that to insert custom values
            byte bStart = 1;
            byte bContinue = 1;
            byte.TryParse(_GameConfig.GetStringOption(CREDITS_START_OPTION, "1"), out bStart);
            byte.TryParse(_GameConfig.GetStringOption(CREDITS_CONTINUE_OPTION, "1"), out bContinue);
            ReplaceReadOnlyString(new uint[] { _AmFreeplay_Offset }, new byte[] { _GameConfig.GetBoolOption(FREEPLAY_OPTION) ? (byte)1 : (byte)0 } );
            ReplaceReadOnlyString(new uint[] { _AmCreditsToStart_Offset }, new byte[] { bStart });
            ReplaceReadOnlyString(new uint[] { _AmCreditsTocontinue_Offset }, new byte[] { bContinue });
            //forcing the game to use default values instead of real ones. Always the case with JConfig anwyway ??
            WriteBytes(GameProcess_ModuleBaseAddress + _AmCreditsInit_Patch_Offset, new byte[] { 0x90, 0x90 });

            //If 3D mode is disabled (ysually the case for home computers), the game displays an ugly "3D OFF" sprite
            //Replacing the sprite file (I_CMN_3D_Off.afb) by a transparent one (fade.afb) will do the trick to remove it
            ReplaceReadOnlyString(new UInt32[] {_3dOffLogo_Patch_Offset }, new byte[] { 0x66, 0x61, 0x64, 0x65, 0x2E, 0x61, 0x66, 0x62, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });

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

        /// <summary>
        /// After the game has finished setting all resolutions settings from fRawDb,
        /// Overriding WindowSize and FrontEnd (2P graphics ?) size, as well as fullscreen state
        /// Default hardcoded values are 1920x1080, Fullscreen
        /// </summary>
        private void SetHack_Resolution()
        {
            int Width = _GameConfig.ResolutionWidth != 0 ? _GameConfig.ResolutionWidth : 1920;
            int Height = _GameConfig.ResolutionHeight != 0 ? _GameConfig.ResolutionHeight : 1080;

            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //mov [esi],00000780
            CaveMemory.Write_StrBytes("C7 06");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(Width));   //WindowWidth
            //mov [esi+04],00000438
            CaveMemory.Write_StrBytes("C7 46 04");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(Height));  //WindowHeight

            //mov [esi+10],00000780
            CaveMemory.Write_StrBytes("C7 46 10");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(Width));   //FrontendWidth
            //mov [esi+14],00000438
            CaveMemory.Write_StrBytes("C7 46 14");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(Height));  //FrontendHeight
            //mov [esi+28],eax
            CaveMemory.Write_StrBytes("89 46 28");

            if (_GameConfig.Windowed)                               //Fullscreen
                //mov byte ptr [esi+40],00
                CaveMemory.Write_StrBytes("C6 46 40 00");
            else
                //mov byte ptr [esi+40],01
                CaveMemory.Write_StrBytes("C6 46 40 01");

            CaveMemory.InjectToOffset(_ResolutionChange_InjectionStruct, "Resolution");
        }

        #endregion        

        #region Inputs Hack

        protected override void Apply_InputsMemoryHack()
        {
            Create_InputsDataBank();
            _Axes_CaveAddress = InputsDatabank_Address;
            _Buttons_CaveAddress = InputsDatabank_Address + 0x10;
            _Buttons2_CaveAddress = InputsDatabank_Address + 0x12;
            _Coins_CaveAddress = InputsDatabank_Address + 0x14;
            _RingIoDll_Path_CaveAddress = InputsDatabank_Address + 0x20;   //char[255]
            _RingConfigData_CaveAddress = InputsDatabank_Address + 0x120; //244 Bytes

            //Writing the DLL path to load it with LoadLibraryA();
            //string JconfigDllPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Properties.Resources.LAUNCHER_TOOLS_FOLDER, @"lgi3d\JVSEmuLGI3D.dll");
            string JconfigDllPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Properties.Resources.DATA_PAYLOADS_FOLDER, @"RConfig\Ring_IO.dll");
            WriteBytes(_RingIoDll_Path_CaveAddress, Encoding.ASCII.GetBytes(JconfigDllPath));
            
            //Fedding the content of ring_config.bin directly to the DLL, removing the need of the file
            //This is almost "no options activated at all" config for RConfig.
            WriteBytes(_RingConfigData_CaveAddress, new byte[]{
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFA, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x3C, 0x00, 0x00, 0x00, 0x28, 0x00, 0x00, 0x00, 0x10, 0x00, 0x00, 0x00, 0x00, 0x05, 0x00, 0x00, 0xD0, 0x02, 0x00, 0x00, 0x00, 0x05, 0x00, 0x00, 0xD0, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x06, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x1B, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
            });

            SetHack_Bootloader();

            //Replacing Axis data returned from JVS data
            //mov cx, word ptr[_Axes_CaveAddress + edi *2]
            List<byte> bPatch = new List<byte>() 
            { 
                0x90, 
                0x66, 0x8B, 0x0C, 0x7D 
            };
            bPatch.AddRange(BitConverter.GetBytes(_Axes_CaveAddress));
            WriteBytes(GameProcess_ModuleBaseAddress + _Axis_Patch_Offset, bPatch.ToArray());

            SetHack_Buttons();
            SetHack_TestBtn();
            SetHack_Coins();
            SetHack_Calibration();

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
        /// Replacing the Buttons state byte value read by JVS thread when games asks for it
        /// [ebx-1] has player buttons (in cl)
        /// [ebx] for P1 has 2D/3D button (in dl)
        /// </summary>
        private void SetHack_Buttons()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //mov cl,[edi+_Buttons_CaveAddress]
            CaveMemory.Write_StrBytes("8A 8F");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Buttons_CaveAddress));
            //mov [ebx-01], cl
            CaveMemory.Write_StrBytes("88 4B FF");
            //mov dl, [edi+_Buttons2_CaveAddress]
            CaveMemory.Write_StrBytes("8A 97");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Buttons2_CaveAddress));
            //movzx ecx,byte ptr [esp+13]
            CaveMemory.Write_StrBytes("0F B6 4C 24 13");

            //Inject it
            CaveMemory.InjectToOffset(_Buttons_InjectionStruct, "Buttons");
        }

        /// <summary>
        /// TEST Button is read a bit further than the other ones, and is not stored in the same byte
        /// </summary>
        private void SetHack_TestBtn()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //and al,[esp+10]
            CaveMemory.Write_StrBytes("22 44 24 10");
            //mov al,[_Buttons_CaveAddress + 2]
            CaveMemory.Write_StrBytes("A0");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Buttons_CaveAddress + 2));
            //mov [esi+04],al
            CaveMemory.Write_StrBytes("88 46 04");

            //Inject it
            CaveMemory.InjectToOffset(_TestBtn_InjectionStruct, "TEST Button");
        }

        /// <summary>
        /// Editing coin in value to replace original emulated input
        /// </summary>
        private void SetHack_Coins()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //mov ax,bx
            CaveMemory.Write_StrBytes("66 89 D8");
            //shl eax,1
            CaveMemory.Write_StrBytes("D1 E0");
            //add eax,_Coins_CaveAddress
            CaveMemory.Write_StrBytes("05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Coins_CaveAddress));
            //mov eax,[eax]
            CaveMemory.Write_StrBytes("66 8B 00");

            //Inject it
            CaveMemory.InjectToOffset(_Coin_InjectionStruct, "Coins");
        }

        /// <summary>
        ///To bypass the need of Calibration (calculated at each level entry), we can force the game to use max range (0x00-0xFF) for JVS data
        ///The game uses these values as Min values (we can force them to 0):
        ///00C17D29 (p1 min x)
        ///00C17D2D (p1 min y)
        ///00C17D31 (p2 min x)
        ///00C17D35 (p2 min y)
        ///For max value, the game is using complicated FloatingPoint/Double operations with a kind of ratio values.
        ///Forcing the following values enable the use of 0x00-0xFF full range values to match with (0.0 - 1024.0) calculated values.                                         
        /// </summary>
        private void SetHack_Calibration()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //xor edi, edi
            CaveMemory.Write_StrBytes("31 FF");
            //xor ebx,ebx
            CaveMemory.Write_StrBytes("31 DB");
            //xor ebp, ebp
            CaveMemory.Write_StrBytes("31 ED");
            //xor ecx, ecx
            CaveMemory.Write_StrBytes("31 C9");
            //mov [esp+10], 0x00000000
            CaveMemory.Write_StrBytes("C7 44 24 10 00 00 00 00");
            //mov [esp+14], 0x0000FF00
            CaveMemory.Write_StrBytes("C7 44 24 14 00 FF 00 00");
            //mov [esp+18], 0x0000FF00
            CaveMemory.Write_StrBytes("C7 44 24 18 00 FF 00 00");
            //mov [esp+1C], 0x0000FF00
            CaveMemory.Write_StrBytes("C7 44 24 1C 00 FF 00 00");
            //mov esi, 0x0000FF00
            CaveMemory.Write_StrBytes("BE 00 FF 00 00");
            //mov     dword_C17D28, edi
            CaveMemory.Write_StrBytes("89 3D 28 7D C1 00");
            
            //Inject it
            CaveMemory.InjectToOffset(_CalibrationValues_InjectionStruct, "Calibration Values");
        }

        #endregion

        #region Outputs Hack

        protected override void Apply_OutputsMemoryHack()
        {
            Create_OutputsDataBank();
            _P1_CustomRecoil_CaveAddress = OutputsDatabank_Address;
            _P2_CustomRecoil_CaveAddress = OutputsDatabank_Address + 0x01;
            _CurrentSequenceStringLength = OutputsDatabank_Address + 0x04;
            _CurrentSequenceString = OutputsDatabank_Address + 0x10;

            SetHack_Recoil();
            SetHack_GetCurrentSequence();

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        //Original game is simply setting a Motor to vibrate, so simply using this data to create or pulsed custom recoil will not be synchronized with bullets shot
        //as the pulses lenght and spaceing will depend on DemulShooter output pulse config data.
        //To synch recoil pulse with projectiles, this hack allows to intercept the code shooting the actual projectile to generate the pulse
        private void SetHack_Recoil()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //mov ecx,[edi+04]
            CaveMemory.Write_StrBytes("8B 4F 04");
            //mov byte ptr [ecx+_P1_CustomRecoil_CaveAddress], 1
            CaveMemory.Write_StrBytes("C6 81");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_CustomRecoil_CaveAddress));
            CaveMemory.Write_StrBytes("01");
            //xor al,al
            CaveMemory.Write_StrBytes("30 C0");

            //Inject it
            CaveMemory.InjectToOffset(_Recoil_InjectionStruct, "Recoil");
        }

        /// <summary>
        /// Get the current sequence string, this will be used later to filter when the game is in "Advertise" mode
        /// </summary>
        private void SetHack_GetCurrentSequence()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //mov mov eax,[esp+54]
            CaveMemory.Write_StrBytes("8B 44 24 54");
            //test eax, eax
            CaveMemory.Write_StrBytes("85 C0");
            //je Exit
            CaveMemory.Write_StrBytes("74 2A");
            //push eax
            CaveMemory.Write_StrBytes("50");
            //call strlen()
            CaveMemory.Write_call(GameProcess_ModuleBaseAddress + _StrlenFunction_Offset);
            //add esp, 4
            CaveMemory.Write_StrBytes("83 C4 04");
            //push ecx
            CaveMemory.Write_StrBytes("51");
            //push esi
            CaveMemory.Write_StrBytes("56");
            //push edi
            CaveMemory.Write_StrBytes("57");
            //mov ecx, _CurrentSequenceStringLength
            CaveMemory.Write_StrBytes("B9");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_CurrentSequenceStringLength));
            //mov [ecx], eax
            CaveMemory.Write_StrBytes("89 01");
            //mov ecx,eax
            CaveMemory.Write_StrBytes("8B C8");
            //inc ecx
            CaveMemory.Write_StrBytes("41");
            //mov mov esi,[esp+60]
            CaveMemory.Write_StrBytes("8B 74 24 60");
            //mov edi, _CurrentSequenceString
            CaveMemory.Write_StrBytes("BF");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_CurrentSequenceString));
            //repe movsb
            CaveMemory.Write_StrBytes("F3 A4");
            //pop edi
            CaveMemory.Write_StrBytes("5F");
            //pop esi
            CaveMemory.Write_StrBytes("5E");
            //pop ecx
            CaveMemory.Write_StrBytes("59");
            //mov mov eax,[esp+54]
            CaveMemory.Write_StrBytes("8B 44 24 54");
            //test eax,eax
            CaveMemory.Write_StrBytes("85 C0");

            //Inject it
            CaveMemory.InjectToOffset(_CurrentSequence_InjectionStruct, "Current Sequence");
        }

        #endregion

        #region No Crosshair Hack

        /// <summary>
        /// The game is reading Axis data to handle gun firing and crosshair display
        /// That funtion is called by different caller, caller address return is in @ESP+14
        /// After loading Y float value in [edi+2C], replacing it with -1.0f to remove crosshair from scree IF caller is the one used during gameplay
        /// That way, cinematic and (maybe ?) name entry (if there are some) will keep crosshairs
        /// In-Game caller  = LGI.EXE + 0x897735
        /// Cinematic caller= LGI.EXE + 0x898567
        /// Others          = ?? (to analyse)
        /// </summary>
        protected override void Apply_NoCrosshairMemoryHack()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //movss [edi+2C],xmm0
            CaveMemory.Write_StrBytes("F3 0F 11 47 2C");
            //cmp [esp+34],LGI.exe+_CrosshairCallerFunctionOffset
            CaveMemory.Write_StrBytes("81 7C 24 34");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _CrosshairCallerOffset));
            //jne Exit
            CaveMemory.Write_StrBytes("75 07");
            //mov [edi+2C],C2C80000
            CaveMemory.Write_StrBytes("C7 47 2C 00 00 C8 C2");  //0xC2C80000 = -1.0f

            //Inject it
            CaveMemory.InjectToOffset(_NoCrosshair_InjectionStruct, "No Crosshair");

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
                //Max value is based on game REGION (see code @6FB7F8)
                ushort CoinValue = BitConverter.ToUInt16(ReadBytes(_Coins_CaveAddress, 2), 0);
                WriteBytes(_Coins_CaveAddress, BitConverter.GetBytes(CoinValue + 1));
            }
            else if (e.ButtonName.Equals("COIN MECH. 2"))
            {
                //Max value is based on game REGION (see code @6FB7F8)
                ushort CoinValue = BitConverter.ToUInt16(ReadBytes(_Coins_CaveAddress + 2, 2), 0);
                WriteBytes(_Coins_CaveAddress + 2, BitConverter.GetBytes(CoinValue + 1));
            }
            else if (e.ButtonName.Equals("SERVICE"))
            {
                Apply_OR_ByteMask(_Buttons_CaveAddress, 0x40);
            }
            else if (e.ButtonName.Equals("TEST"))
            {
                Apply_OR_ByteMask(_Buttons_CaveAddress + 2, 0x80);
            }
            else if (e.ButtonName.Equals("3D/2D SWITCH"))
            {
                Apply_OR_ByteMask(_Buttons2_CaveAddress, 0x04);
            }
        }

        protected override void GameController_OnButtonUp(object sender, VirtualGameControler.ButtonDownEventArgs e)
        {
            if (e.ButtonName.Equals("SERVICE"))
            {
                Apply_AND_ByteMask(_Buttons_CaveAddress, 0xBF);
            }
            else if (e.ButtonName.Equals("TEST"))
            {
                Apply_AND_ByteMask(_Buttons_CaveAddress + 2, 0x7F);
            }
            else if (e.ButtonName.Equals("3D/2D SWITCH"))
            {
                Apply_AND_ByteMask(_Buttons2_CaveAddress, 0xFB);
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
            }
        }

        protected override void PlayerController_OnButtonDown(object sender, VirtualGameControler.ButtonDownEventArgs e)
        {
            if (e.Id != PlayerID.None)
            {
                UInt32 PlayerIndex = (UInt32)e.Id;

                if (e.ButtonName.Equals("TRIGGER LEFT"))
                {
                    Apply_OR_ByteMask(_Buttons_CaveAddress + PlayerIndex, 0x02);
                }
                if (e.ButtonName.Equals("TRIGGER RIGHT"))
                {
                    Apply_OR_ByteMask(_Buttons_CaveAddress + PlayerIndex, 0x01);
                }
                else if (e.ButtonName.Equals("START"))
                {
                    Apply_OR_ByteMask(_Buttons_CaveAddress + PlayerIndex, 0x80);
                }
            }
        }
        protected override void PlayerController_OnButtonUp(object sender, VirtualGameControler.ButtonDownEventArgs e)
        {
            if (e.Id != PlayerID.None)
            {
                UInt32 PlayerIndex = (UInt32)e.Id;

                if (e.ButtonName.Equals("TRIGGER LEFT"))
                {
                    Apply_AND_ByteMask(_Buttons_CaveAddress + PlayerIndex, 0xFD);
                }
                if (e.ButtonName.Equals("TRIGGER RIGHT"))
                {
                    Apply_AND_ByteMask(_Buttons_CaveAddress + PlayerIndex, 0xFE);
                }
                else if (e.ButtonName.Equals("START"))
                {
                    Apply_AND_ByteMask(_Buttons_CaveAddress + PlayerIndex, 0x7E);
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
            //Gun motor : stays activated when trigger is pulled
            //Gun recoil : not used ??
            _Outputs = new List<GameOutput>
            {
                new GameOutput(OutputId.P1_LmpStart),
                new GameOutput(OutputId.P2_LmpStart),
                new GameOutput(OutputId.Lmp2D3D),
                new GameOutput(OutputId.P1_AirFront),
                new GameOutput(OutputId.P2_AirFront),
                new GameOutput(OutputId.P1_AirRear),
                new GameOutput(OutputId.P2_AirRear),
                new GameOutput(OutputId.P1_GunRecoil),
                new GameOutput(OutputId.P1_GunMotor),
                new GameOutput(OutputId.P2_GunRecoil),
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
            byte[] buffer = ReadBytes(GameProcess_ModuleBaseAddress + _OutputsPtr_Offset, 4);
            byte OutputData1 = ReadByte(BitConverter.ToUInt32(buffer, 0) + 0x44);
            byte OutputData2 = ReadByte(BitConverter.ToUInt32(buffer, 0) + 0x45);
            SetOutputValue(OutputId.P1_LmpStart, OutputData1 >> 7 & 0x01);
            SetOutputValue(OutputId.P2_LmpStart, OutputData1 >> 4 & 0x01);
            SetOutputValue(OutputId.Lmp2D3D, OutputData1 >> 1 & 0x01);
            SetOutputValue(OutputId.P1_AirFront, OutputData2 >> 7 & 0x01);
            SetOutputValue(OutputId.P2_AirFront, OutputData2 >> 6 & 0x01);
            SetOutputValue(OutputId.P1_AirRear, OutputData2 >> 5 & 0x01);
            SetOutputValue(OutputId.P2_AirRear, OutputData2 >> 4 & 0x01);
            SetOutputValue(OutputId.P1_GunRecoil, OutputData1 >> 5 & 0x01);
            SetOutputValue(OutputId.P1_GunMotor, OutputData1 >> 6 & 0x01);
            SetOutputValue(OutputId.P2_GunRecoil, OutputData1 >> 2 & 0x01);
            SetOutputValue(OutputId.P2_GunMotor, OutputData1 >> 3 & 0x01);

            //Custom Outputs
            UInt32 StrLength = ReadUInt32(_CurrentSequenceStringLength);
            byte[] bCurrentSeq = ReadBytes(_CurrentSequenceString, (int)StrLength);
            string sCurrentSeq = System.Text.ASCIIEncoding.ASCII.GetString(bCurrentSeq);
            if (sCurrentSeq.StartsWith("Adv"))
                _IsAttractMode = true;
            if (sCurrentSeq.StartsWith("GameSeq"))
                _IsAttractMode = false;

            for (uint i = 0; i < _GameConfig.PlayerCount; i++)
            {
                _Life[i] = 0;

                if (!_IsAttractMode)
                {
                    _Life[i] = (int)ReadFloat(ReadPtrChain(GameProcess_ModuleBaseAddress + _PlayersStructPtr_Offset, new uint[] { 0x8 + (4 * i), 0x14 }) + 0x20);
                    if (_Life[i] < 0)
                        _Life[i] = 0;

                    if (_Life[i] < _LastLife[i])
                        SetOutputValue(OutputId.P1_Damaged + i, 1);
                }

                _LastLife[i] = _Life[i];

                SetOutputValue(OutputId.P1_Life + i, _Life[i]);
            }

            //Using constant "ON" value from motor to create asynch outputs for recoil
            //Pulses are not sync with bullets, but with DemulShooter output on/off timings only
            /*SetOutputValue(OutputId.P1_CtmRecoil, OutputData1 >> 6 & 0x01);
            SetOutputValue(OutputId.P2_CtmRecoil, OutputData1 >> 3 & 0x01);*/

            //New method : reading intercepted value for bullet fired, so that recoil is sync with bullets
            //Need to filter this with MOTOR_ON, if not : recoil will be activated when bullets are fired during attract
            if (ReadByte(_P1_CustomRecoil_CaveAddress) == 1 && (OutputData1 >> 6 & 0x01) == 1)
            {
                SetOutputValue(OutputId.P1_CtmRecoil, 1);
                WriteByte(_P1_CustomRecoil_CaveAddress, 0);
            }
            if (ReadByte(_P2_CustomRecoil_CaveAddress) == 1 && (OutputData1 >> 3 & 0x01) == 1)
            {
                SetOutputValue(OutputId.P2_CtmRecoil, 1);
                WriteByte(_P2_CustomRecoil_CaveAddress, 0);
            }
            SetOutputValue(OutputId.Credits, ReadByte(GameProcess_ModuleBaseAddress + _Credits_Offset));
        }

        #endregion
    }
}
