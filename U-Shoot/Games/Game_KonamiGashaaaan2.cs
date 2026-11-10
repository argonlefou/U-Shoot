using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using UsCore.Config;
using UsCore.Launcher;
using UsCore.Logging;
using UsCore.MameOutput;
using UsCore.Memory;
using UsCore.VirtualControllers;

namespace U_Shoot.Games
{
    public class Game_KonamiGashaaaan2 : BaseGame32
    {
        protected override string GAMEDATA_FOLDER { get { return @"MemoryData\Konami\gashaaaan2"; } }
        protected override bool PATCH_PEB_DEBUG_FLAG { get { return true; } }

        private const string SAVE_DRIVE_OPTION = "Save Drive letter";
        private const string DISPLAY_MODE_OPTION = "Display Mode";

        private const string DISPLAY_MODE_FULLSCREEN = "Fullscreen";
        private const string DISPLAY_MODE_FULLSCREEN_KEEP_RATIO = "Fullscreen + Keep Ratio";


        private const string IG2ACLIB_DLL_NAME = "ig2ACLib.dll";
        private const string LIBBMS_DLL_NAME = "libbms.dll";
        private UInt32 _ig2ACLib_BaseAddress = 0;       
        private UInt32 _libbms_BaseAddress = 0;

        #region LOADER MEMORY ADDRESSES
        private BreakpointStruct32 _LoadIg2AclibLibrary_BreakpointStruct = new BreakpointStruct32(0x00007CBA, 2);
        private UInt32 _Libbms_EAmusement_Patch1_Offset = 0x00001FC4;
        private UInt32 _Libbms_EAmusement_Patch2_Offset = 0x0000234B;
        private UInt32 _Libbms_CreditConfirmBlocking_Patch1_Offset = 0x00003121;
        private UInt32 _Libbms_CreditConfirmBlocking_Patch2_Offset = 0x000031D3;
        private UInt32 _Libbms_CreditConfirm_Patch_Offset = 0x00002ED0;
        private UInt32 _CoordinateCalculationCheck_Patch_Offset = 0x00004FAC;
        private UInt32 _ReadWindowedConfigOption_Patch_Offset = 0x001535BE;

        private UInt32 _ig2ACLibUpdate_CustomFunctionPtr = 0;
        private UInt32 _ig2ACLibBoot_CustomFunctionPtr = 0;
        private UInt32 _ig2ACLibBootWait_CustomFunctionPtr = 0;
        private UInt32 _ig2ACLibGetLastError_CustomFunctionPtr = 0;
        private UInt32 _ig2ACLibIOIsCalibrationEnd_CustomFunctionPtr = 0;
        private UInt32 _ig2ACLibIOSetCalibration_CustomFunctionPtr = 0;
        private UInt32 _ig2ACLibIOGetBallPosSensorPosNum_CustomFunctionPtr = 0;
        private UInt32 _ig2ACLibIOGetBallPosSensorPos_CustomFunctionPtr = 0;
        private UInt32 _ig2ACLibIOGetButton_CustomFunctionPtr = 0;
        private UInt32 _ig2ACLibIOSetLamp_CustomFunctionPtr = 0;
        private UInt32 _ig2ACLibIOSetBallSupply_CustomFunctionPtr = 0;
        private UInt32 _ig2ACLibIOSetBallSupplyStop_CustomFunctionPtr = 0;
        private UInt32 _ig2ACLibIOSetBallSupplyStopImmediate_CustomFunctionPtr = 0;
        private UInt32 _ig2ACLibIOGetBallSupplySensor_CustomFunctionPtr = 0;
        private UInt32 _ig2ACLibIOCoinBlockerOpen_CustomFunctionPtr = 0;
        private UInt32 _ig2ACLibIOCoinBlockerClose_CustomFunctionPtr = 0;
        private UInt32 _ig2ACLibIOCoinGetIncrease_CustomFunctionPtr = 0;
        private UInt32 _ig2ACLibIOSecurityRenew_CustomFunctionPtr = 0;
        private UInt32 _ig2ACLibIOSecurityRenewIsEnd_CustomFunctionPtr = 0;
        private UInt32 _ig2ACLibIOSecurityCheck_CustomFunctionPtr = 0;
        private UInt32 _ig2ACLibIOSecurityGetSystemID_CustomFunctionPtr = 0;
        private UInt32 _ig2ACLibIOSecurityGetHardwareID_CustomFunctionPtr = 0;
        private UInt32 _ig2ACLibIOSecurityGetSoftwareID_CustomFunctionPtr = 0;
        private UInt32 _ig2ACLibBookkeepingAddCoin_CustomFunctionPtr = 0;
        //private UInt32 _ig2ACLibBookkeepingSave_CustomFunctionPtr = 0;

        private UInt32 _ig2ACLibUpdate_PtrOffset = 0x0002B5E98;
        private UInt32 _ig2ACLibBoot_PtrOffset = 0x0002B5EA4;
        private UInt32 _ig2ACLibBootWait_PtrOffset = 0x0002B5EA8;
        private UInt32 _ig2ACLibGetLastError_PtrOffset = 0x0002B5EAC;
        private UInt32 _ig2ACLibIOIsCalibrationEnd_PtrOffset = 0x0002B5EC0;
        private UInt32 _ig2ACLibIOSetCalibration_PtrOffset = 0x0002B5EC4;
        private UInt32 _ig2ACLibIOGetBallPosSensorPosNum_PtrOffset = 0x0002B5ECC;
        private UInt32 _ig2ACLibIOGetBallPosSensorPos_PtrOffset = 0x0002B5ED0;
        private UInt32 _ig2ACLibIOGetButton_PtrOffset = 0x0002B5EE8;
        private UInt32 _ig2ACLibIOSetLamp_PtrOffset = 0x0002B5EEC;
        private UInt32 _ig2ACLibIOSetBallSupply_PtrOffset = 0x0002B5EF0;
        private UInt32 _ig2ACLibIOSetBallSupplyStop_PtrOffset = 0x0002B5EF4;
        private UInt32 _ig2ACLibIOSetBallSupplyStopImmediate_PtrOffset = 0x0002B5EF8;
        private UInt32 _ig2ACLibIOGetBallSupplySensor_PtrOffset = 0x0002B5EFC;
        private UInt32 _ig2ACLibIOCoinBlockerOpen_PtrOffset = 0x0002B5F00;
        private UInt32 _ig2ACLibIOCoinBlockerClose_PtrOffset = 0x0002B5F04;
        private UInt32 _ig2ACLibIOCoinGetIncrease_PtrOffset = 0x0002B5F08;
        private UInt32 _ig2ACLibIOSecurityRenew_PtrOffset = 0x0002B5F14;
        private UInt32 _ig2ACLibIOSecurityRenewIsEnd_PtrOffset = 0x0002B5F18;
        private UInt32 _ig2ACLibIOSecurityCheck_PtrOffset = 0x0002B5F1C;
        private UInt32 _ig2ACLibIOSecurityGetSystemID_PtrOffset = 0x0002B5F20;
        private UInt32 _ig2ACLibIOSecurityGetHardwareID_PtrOffset = 0x0002B5F24;
        private UInt32 _ig2ACLibIOSecurityGetSoftwareID_PtrOffset = 0x0002B5F28;
        private UInt32 _ig2ACLibBookkeepingAddCoin_PtrOffset = 0x002B5F54;
        //private UInt32 _ig2ACLibBookkeepingSave_PtrOffset = 0x002B5F5C;

        private InjectionStruct32 _ig2ACLib_LoadLibraryFunction_InjectionOffset = new InjectionStruct32(0x00007C2F, 5);
        private InjectionStruct32 _CreateWindowEx_Injectionstruct = new InjectionStruct32(0x00177102, 7);
        private InjectionStruct32 _D3dStretchRect_Injectionstruct = new InjectionStruct32(0x0013C9FD, 5);

        //Custom Addresses
        private UInt32 _ButtonsArray_CaveAddress = 0;
        private UInt32 _P1P2_Sensor1_CaveAddress = 0;
        private UInt32 _P3P4_Sensor1_CaveAddress = 0;
        private UInt32 _P1P2_Sensor2_CaveAddress = 0;
        private UInt32 _P3P4_Sensor2_CaveAddress = 0;
        private UInt32 _P1P2_Sensor3_CaveAddress = 0;
        private UInt32 _P3P4_Sensor3_CaveAddress = 0;
        private UInt32 _InsertedCoins_CaveAddress = 0;
        private UInt32 _P1_ShotDetection_CaveAddress = 0;
        private UInt32 _P2_ShotDetection_CaveAddress = 0;
        private UInt32 _P3_ShotDetection_CaveAddress = 0;
        private UInt32 _P4_ShotDetection_CaveAddress = 0;
        private UInt32 _DetectedShotCount_CaveAddress = 0;
        private UInt32 _BallXPositions_CaveAddress = 0;
        private UInt32 _BallYPositions_CaveAddress = 0;
        private UInt32 _SoftwareID_CaveAddress = 0;
        private UInt32 _SystemID_CaveAddress = 0;
        private UInt32 _HardwareID_CaveAddress = 0;
        private UInt32 _D3dStretchPdest_Address = 0;        
        #endregion

        #region INPUT MEMORY ADDRESSES

        #endregion

        #region OUTPUTS MEMORY ADDRESSES
        private UInt32 _CreditsPtr_Offset = 0x002BB0D8;

        //custom Values
        private UInt32 _Lamps_CaveAddress = 0;
        private UInt32 _CoinBlocker_CaveAddress = 0;
        private UInt32 _MotorUnits_CaveAddress = 0;
        #endregion

        //Original ratio is 8/3 (dual 4/3 screens side by side)
        private double _OriginalRatio = 3.0 / 8.0;

        private string _DisplayMode = "Default";
        private string _SaveDriveLetter = "C";
        private bool _DisableBookkeepingAddCoin = false;

        private Dictionary<string, string> _KnownMd5_Ig2aclibDll;
        private Dictionary<string, string> _KnownMd5_LibbmsDll;

        public Game_KonamiGashaaaan2(GameConfig Config) : base(Config)
        {
            _KnownMd5Prints.Add("Gashaaaan Refills v. JA-A01:2009-01-13", "db158b447371831317229ecc79920ee8");
            _KnownMd5_LibbmsDll = new Dictionary<string, string>
            {
                { "Libbms.dll - v. JA-A01:2009-01-13", "5cc6c75426ad4e0fa517e250bc52bcc1" }
            };
            _KnownMd5_Ig2aclibDll = new Dictionary<string, string>
            {
                { "ig2ACLib.dll - v. JA-A01:2009-01-13", "fa544eb0aaa006621c74f53c0cac1aa8" }
            };

            _DisplayMode = _GameConfig.GetStringOption(DISPLAY_MODE_OPTION, "Default");
            _SaveDriveLetter = _GameConfig.GetStringOption(SAVE_DRIVE_OPTION, "C");
            _DisableBookkeepingAddCoin = true;
        }

        protected override void AttachDebuggerEvents()
        {
            if (!_GameConfig.NoInputs)
                _GameLauncher.OnLoadedDll += LoadDllEvent;
        }

        #region Debugger Events

        protected override void CreateProcessEvent(object sender, x86_x64_Debugger.CreateProcessDebugEventArgs e)
        {
            base.CreateProcessEvent(sender, e);
            SetBreakpoint(_LoadIg2AclibLibrary_BreakpointStruct);
        }

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
                    //Using a breakpoint after call to LoadLibraryA() for "ig2AcLib.dll", and after the Inputs codecave is changing the function pointers
                    //Output hack is also altering those pointers, and can be overwritten by Input codecave if done at program entry (Input codecave is only applied later when the DLL loads)
                    //So, at that point, the only solution is to run Outputs hack a second time
                    if ((UInt32)e.ExceptionInfo.ExceptionRecord.ExceptionAddress == GameProcess_ModuleBaseAddress + _LoadIg2AclibLibrary_BreakpointStruct.MemoryOffset)
                    {
                        Logger.WriteLog(" > LoadLibrary(vsIOBoard.dll) breakpoint reached");
                        if (!RemoveBreakpoint(_LoadIg2AclibLibrary_BreakpointStruct, e.dwThreadId))
                            return;

                        e.Handled = true;
                        Apply_OutputsMemoryHack();
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
                if (string.Equals(Path.GetFileName(e.DllPath), IG2ACLIB_DLL_NAME, StringComparison.OrdinalIgnoreCase))
                {
                    Logger.WriteLog("==> Wanted DLL found !");
                    //HANDLE should always be in 32Bits range (https://learn.microsoft.com/fr-fr/windows/win32/winprog64/interprocess-communication?redirectedfrom=MSDN)
                    Logger.WriteLog("  + hModule: 0x" + e.LoadDllInfo.hFile.ToString("X8"));
                    Logger.WriteLog("  + fUnicode: " + e.LoadDllInfo.fUnicode.ToString());

                    _ig2ACLib_BaseAddress = (UInt32)e.LoadDllInfo.lpBaseOfDll;

                    string UsbPluginPath = _GameConfig.TargetPath.Replace(Path.GetFileName(_GameConfig.TargetFileName), IG2ACLIB_DLL_NAME);
                    CheckMd5(UsbPluginPath, _KnownMd5_Ig2aclibDll);

                    //Change hardcoded path d:/BOOKKEEPINGS, e:/BOOKKEEPINGS, f:/BOOKKEEPINGS, etc....
                    ReplaceReadOnlyString(_ig2ACLib_BaseAddress, new UInt32[] { 0x368DC, 0x3690C, 0x3693C }, Encoding.ASCII.GetBytes("./BOOKKEEPINGS\0"));
                    ReplaceReadOnlyString(_ig2ACLib_BaseAddress, new UInt32[] { 0x368EC, 0x3691C, 0x3694C }, Encoding.ASCII.GetBytes("./BOOKKEEPINGS/BOOKKEEPINGS\0"));
                }
                else if (string.Equals(Path.GetFileName(e.DllPath), LIBBMS_DLL_NAME, StringComparison.OrdinalIgnoreCase))
                {
                    Logger.WriteLog("==> Wanted DLL found !");
                    //HANDLE should always be in 32Bits range (https://learn.microsoft.com/fr-fr/windows/win32/winprog64/interprocess-communication?redirectedfrom=MSDN)
                    Logger.WriteLog("  + hModule: 0x" + e.LoadDllInfo.hFile.ToString("X8"));
                    Logger.WriteLog("  + fUnicode: " + e.LoadDllInfo.fUnicode.ToString());

                    _libbms_BaseAddress = (UInt32)e.LoadDllInfo.lpBaseOfDll;

                    string UsbPluginPath = _GameConfig.TargetPath.Replace(Path.GetFileName(_GameConfig.TargetFileName), LIBBMS_DLL_NAME);
                    CheckMd5(UsbPluginPath, _KnownMd5_LibbmsDll);

                    Apply_Libbms_Hacks();
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
                    double dMaxX = 1280.0;
                    double dMaxY = 480.0;
                    double dRangeX = dMaxX - dMinX + 1;
                    double dRangeY = dMaxY - dMinY + 1;

                    PlayerController.Computed_X = Convert.ToInt32(Math.Round(dRangeX * PlayerController.Computed_X / TotalResX));


                    if (_DisplayMode == "Fullscreen + KeepRatio")
                    {
                        Logger.WriteLog("Modified display ratio, adjusting aim...");

                        double dViewportHeight = _OriginalRatio * TotalResY;
                        double SideBarsHeight = (TotalResX - dViewportHeight) / 2.0;
                        PlayerController.Computed_Y -= (int)SideBarsHeight;
                        PlayerController.Computed_Y = Convert.ToInt32(Math.Round((double)PlayerController.Computed_Y) * dMaxY / dViewportHeight);
                    }
                    else
                    {
                        Logger.WriteLog("MouseHookCallback() => Detected original windowed ratio OR fullscreen image, continuing directly...");
                        PlayerController.Computed_Y = Convert.ToInt32(Math.Round(dRangeY * PlayerController.Computed_Y / TotalResY));
                    }
                    
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
        }

        #region Loader Hack

        protected override void Apply_LoaderMemoryHack()
        {
            Create_LoaderDataBank();

            _SystemID_CaveAddress = LoaderDatabank_Address;
            WriteBytes(_SystemID_CaveAddress, Encoding.ASCII.GetBytes("SY_USHOOT"));
            _HardwareID_CaveAddress = LoaderDatabank_Address + 0x10;
            WriteBytes(_HardwareID_CaveAddress, Encoding.ASCII.GetBytes("HW_ARGONLEFOU"));
            _SoftwareID_CaveAddress = LoaderDatabank_Address + 0x20;
            WriteBytes(_SoftwareID_CaveAddress, Encoding.ASCII.GetBytes("SW_U-SHOOT"));

            ReplaceReadOnlyString(new UInt32[] { 0x25DD6C }, Encoding.ASCII.GetBytes(_SaveDriveLetter));
            ReplaceReadOnlyString(new UInt32[] { 0x25DD70 }, Encoding.ASCII.GetBytes(_SaveDriveLetter));

            //Creating custom RECT to rectify display ratio :
            _D3dStretchPdest_Address = LoaderDatabank_Address + 0x40;
            //LEFT: always 0
            WriteBytes(_D3dStretchPdest_Address, BitConverter.GetBytes(0));
            //TOP: calculatting LetterBox size
            double dViewportHeight = _OriginalRatio * Screen.PrimaryScreen.Bounds.Width;
            double LetterboxBarsHeight = (Screen.PrimaryScreen.Bounds.Height - dViewportHeight) / 2.0;
            WriteBytes(_D3dStretchPdest_Address + 4, BitConverter.GetBytes((int)LetterboxBarsHeight));
            //RIGHT: screen width 
            WriteBytes(_D3dStretchPdest_Address + 8, BitConverter.GetBytes(Screen.PrimaryScreen.Bounds.Width));
            //BOTTOM : Top letterbox black + viewport size
            WriteBytes(_D3dStretchPdest_Address + 0xC, BitConverter.GetBytes((int)(dViewportHeight + LetterboxBarsHeight)));

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() => Databank Address = 0x" + LoaderDatabank_Address.ToString("X8"));
            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() => D3dStretchRect pDest = { 0, " + (int)LetterboxBarsHeight + ", " + Screen.PrimaryScreen.Bounds.Width + ", " + (int)(dViewportHeight + LetterboxBarsHeight) + " }");

            Mod_CreateWindow();

            if (_DisplayMode.ToLower() == DISPLAY_MODE_FULLSCREEN_KEEP_RATIO.ToLower())
                Mod_D3dStrechRect();

            /*_SaveDataPath_CaveAddress = (UInt32)_LoaderDatabank_Address + 0x30;
            WriteBytes(_SaveDataPath_CaveAddress, Encoding.UTF8.GetBytes(SAVEDATA_PATH));
            _CustomBookkeeping_Path1 = (UInt32)_LoaderDatabank_Address + 0x40;
            WriteBytes(_CustomBookkeeping_Path1, Encoding.UTF8.GetBytes(BOOKKEEPING_PATH_1));
            _CustomBookkeeping_Path2 = (UInt32)_LoaderDatabank_Address + 0x60;
            WriteBytes(_CustomBookkeeping_Path2, Encoding.UTF8.GetBytes(BOOKKEEPING_PATH_2));*/

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        /// <summary>
        /// Replace width/Height of CreateWindowEx to match fullscreen
        /// Also replace dwStyle flags to fake fullscren
        /// Also force "windowed" setting form "settings.tbl" to be read as "1" 
        /// 
        /// Also replace cursor by trabsparent one if needed
        /// </summary>
        private void Mod_CreateWindow()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);
            //mov esi,[esp+0C]
            CaveMemory.Write_StrBytes("8B 74 24 0C");
            //lea eax,[esi+04]
            CaveMemory.Write_StrBytes("8D 46 04");

            if (_GameConfig.NoCursor && CreateTransparentCursor())
            {
                //mov [eax+18], _CustomTransparentCursorHandle
                CaveMemory.Write_StrBytes("C7 40 18");
                CaveMemory.Write_Bytes(BitConverter.GetBytes((UInt32)_CustomTransparentCursorHandle));
            }

            if (_DisplayMode.ToLower() == DISPLAY_MODE_FULLSCREEN.ToLower() || _DisplayMode.ToLower() == DISPLAY_MODE_FULLSCREEN_KEEP_RATIO.ToLower())
            {
                //Force "windowed" settings to be "1"
                WriteBytes(GameProcess_ModuleBaseAddress + _ReadWindowedConfigOption_Patch_Offset, new byte[] { 0xB0, 0x01, 0x90, 0x90, 0x90 });

                //mov [esi+3C], Height
                CaveMemory.Write_StrBytes("C7 46 3C");
                CaveMemory.Write_Bytes(BitConverter.GetBytes(Screen.PrimaryScreen.Bounds.Height));
                //mov [esi+38], Width
                CaveMemory.Write_StrBytes("C7 46 38");
                CaveMemory.Write_Bytes(BitConverter.GetBytes(Screen.PrimaryScreen.Bounds.Width));
                //mov [esi+2C],94000000
                CaveMemory.Write_StrBytes("C7 46 2C 00 00 00 94");
            }

            //Inject it    
            CaveMemory.InjectToOffset(_CreateWindowEx_Injectionstruct, "Createwindow");
        }

        /// <summary>
        /// When D3dStretchRect is call at that place, pDestRect is NULL so it is taking the whole surface
        /// Replacing the push NULL by pushing a custom RECT struct having good dimansion to change the display ratio as it should
        /// 
        /// D3dStretchRect() is also called elsewhere in the same procedure, with a calculated Rect
        /// Need to change it also ? But it does not look like it's going in that part of the procedure to call
        /// </summary>
        private void Mod_D3dStrechRect()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //push edi
            CaveMemory.Write_StrBytes("57");
            //mov [esp],_D3dStretchPdest_Address
            CaveMemory.Write_StrBytes("C7 04 24");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_D3dStretchPdest_Address));
            //push ecx
            CaveMemory.Write_StrBytes("51");
            //push edi
            CaveMemory.Write_StrBytes("57");
            //push edx
            CaveMemory.Write_StrBytes("52");
            //push esi
            CaveMemory.Write_StrBytes("56");

            //Inject it
            CaveMemory.InjectToOffset(_D3dStretchRect_Injectionstruct, "Fullscreen ratio");
        }
              
        #endregion

        #region Inputs Hack

        protected override void Apply_InputsMemoryHack()
        {
            Create_InputsDataBank();
            _ButtonsArray_CaveAddress = InputsDatabank_Address;             //24 bytes
            _P1P2_Sensor1_CaveAddress = InputsDatabank_Address + 0x20;
            _P3P4_Sensor1_CaveAddress = InputsDatabank_Address + 0x21;
            _P1P2_Sensor2_CaveAddress = InputsDatabank_Address + 0x22;
            _P3P4_Sensor2_CaveAddress = InputsDatabank_Address + 0x23;
            _P1P2_Sensor3_CaveAddress = InputsDatabank_Address + 0x24;
            _P3P4_Sensor3_CaveAddress = InputsDatabank_Address + 0x25;
            WriteBytes(_P1P2_Sensor1_CaveAddress, new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });   //Set all sensors to "OFF"
            _InsertedCoins_CaveAddress = InputsDatabank_Address + 0x28;     //2 Bytes : 1=Current State, 2=Previous State
            _P1_ShotDetection_CaveAddress = InputsDatabank_Address + 0x30;  //Same thing
            _P2_ShotDetection_CaveAddress = InputsDatabank_Address + 0x32;
            _P3_ShotDetection_CaveAddress = InputsDatabank_Address + 0x34;
            _P4_ShotDetection_CaveAddress = InputsDatabank_Address + 0x36;
            _DetectedShotCount_CaveAddress = InputsDatabank_Address + 0x38; //1 Byte
            _BallXPositions_CaveAddress = InputsDatabank_Address + 0x40;    //2 bytes * 4 players
            _BallYPositions_CaveAddress = InputsDatabank_Address + 0x48;    //2 bytes * 4 players

            Mod_CoordinateCalculation();

            Apply_Ig2Lib_Hacks();

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        /// <summary>
        /// Game is computing received coordinate as float and put them to square to compare...and it fails
        /// Waiting to see why...in the meantime, forcing it good
        /// </summary>
        private void Mod_CoordinateCalculation()
        {
            WriteBytes(GameProcess_ModuleBaseAddress + _CoordinateCalculationCheck_Patch_Offset, new byte[] { 0x90, 0x90 });

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        #region ig2ACLib.dll Hacks

        /// <summary>
        /// Creating custom functions that will replace some original ig2ACLIb calls
        /// Custom function pointer will be written instead of initial function pointer, after the game has loaded the DLL and GetProcAddress all the functions
        /// </summary>
        private void Apply_Ig2Lib_Hacks()
        {
            Create_ig2ACLibNullSub_CustomFunction();
            Create_ig2ACLibBootWait_CustomFunction();
            Create_ig2ACLibGetLastError_CustomFunction();
            Create_2ACLibIOIsCalibrationEnd_CustomFunction();
            Create_ig2ACLibIOGetBallPosSensorPosNum_CustomFunction();
            Create_ig2ACLibIOGetBallPosSensorPos();
            Create_ig2ACLibIOGetButton_CustomFunction();
            Create_ig2ACLibIOCoinBlockerOpen_CustomFunction();
            Create_ig2ACLibIOGetBallSupplySensor_CustomFunction();
            Create_ig2ACLibIOCoinBlockerClose_CustomFunction();
            Create_ig2ACLibIOCoinGetIncrease_CustomFunction();
            Create_ig2ACLibIOSecurityRenewIsEnd_CustomFunction();
            Create_ig2ACLibIOSecurityCheck_CustomFunction();
            Create_ig2ACLibIOSecurityGetSystemID_CustomFunction();
            Create_ig2ACLibIOSecurityGetHardwareID_CustomFunction();
            Create_ig2ACLibIOSecurityGetSoftwareID_CustomFunction();

            /*** 1) ig2ACLib functions pointer are stored in memory, we will replace the pointer content by the addresses of our custom functions ***/
            /*** 2) ig2ACLib beeing loaded (at least twice) later, pointer willl be replaced by original  functions at that moment (loop parsing) ***/
            /***    So at the end of the loading procedure, a Codecave will be needed to overwrite those pointers at that moment only.            ***/
            try
            {
                Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

                //mov eax, FunctionPointer_Address
                CaveMemory.Write_StrBytes("B8");
                CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _ig2ACLibUpdate_PtrOffset));
                //mov [eax], CusomPointerAddress
                CaveMemory.Write_StrBytes("C7 00");
                CaveMemory.Write_Bytes(BitConverter.GetBytes(_ig2ACLibUpdate_CustomFunctionPtr));

                //mov eax, FunctionPointer_Address
                CaveMemory.Write_StrBytes("B8");
                CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _ig2ACLibBoot_PtrOffset));
                //mov [eax], CusomPointerAddress
                CaveMemory.Write_StrBytes("C7 00");
                CaveMemory.Write_Bytes(BitConverter.GetBytes(_ig2ACLibBoot_CustomFunctionPtr));

                //mov eax, FunctionPointer_Address
                CaveMemory.Write_StrBytes("B8");
                CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _ig2ACLibBootWait_PtrOffset));
                //mov [eax], CusomPointerAddress
                CaveMemory.Write_StrBytes("C7 00");
                CaveMemory.Write_Bytes(BitConverter.GetBytes(_ig2ACLibBootWait_CustomFunctionPtr));

                //mov eax, FunctionPointer_Address
                CaveMemory.Write_StrBytes("B8");
                CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _ig2ACLibGetLastError_PtrOffset));
                //mov [eax], CusomPointerAddress
                CaveMemory.Write_StrBytes("C7 00");
                CaveMemory.Write_Bytes(BitConverter.GetBytes(_ig2ACLibGetLastError_CustomFunctionPtr));

                //mov eax, FunctionPointer_Address
                CaveMemory.Write_StrBytes("B8");
                CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _ig2ACLibIOIsCalibrationEnd_PtrOffset));
                //mov [eax], CusomPointerAddress
                CaveMemory.Write_StrBytes("C7 00");
                CaveMemory.Write_Bytes(BitConverter.GetBytes(_ig2ACLibIOIsCalibrationEnd_CustomFunctionPtr));

                //mov eax, FunctionPointer_Address
                CaveMemory.Write_StrBytes("B8");
                CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _ig2ACLibIOSetCalibration_PtrOffset));
                //mov [eax], CusomPointerAddress
                CaveMemory.Write_StrBytes("C7 00");
                CaveMemory.Write_Bytes(BitConverter.GetBytes(_ig2ACLibIOSetCalibration_CustomFunctionPtr));

                //mov eax, FunctionPointer_Address
                CaveMemory.Write_StrBytes("B8");
                CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _ig2ACLibIOGetBallPosSensorPosNum_PtrOffset));
                //mov [eax], CusomPointerAddress
                CaveMemory.Write_StrBytes("C7 00");
                CaveMemory.Write_Bytes(BitConverter.GetBytes(_ig2ACLibIOGetBallPosSensorPosNum_CustomFunctionPtr));

                //mov eax, FunctionPointer_Address
                CaveMemory.Write_StrBytes("B8");
                CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _ig2ACLibIOGetBallPosSensorPos_PtrOffset));
                //mov [eax], CusomPointerAddress
                CaveMemory.Write_StrBytes("C7 00");
                CaveMemory.Write_Bytes(BitConverter.GetBytes(_ig2ACLibIOGetBallPosSensorPos_CustomFunctionPtr));

                //mov eax, FunctionPointer_Address
                CaveMemory.Write_StrBytes("B8");
                CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _ig2ACLibIOGetButton_PtrOffset));
                //mov [eax], CusomPointerAddress
                CaveMemory.Write_StrBytes("C7 00");
                CaveMemory.Write_Bytes(BitConverter.GetBytes(_ig2ACLibIOGetButton_CustomFunctionPtr));

                //mov eax, FunctionPointer_Address
                CaveMemory.Write_StrBytes("B8");
                CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _ig2ACLibIOSetLamp_PtrOffset));
                //mov [eax], CusomPointerAddress
                CaveMemory.Write_StrBytes("C7 00");
                CaveMemory.Write_Bytes(BitConverter.GetBytes(_ig2ACLibIOSetLamp_CustomFunctionPtr));

                //mov eax, FunctionPointer_Address
                CaveMemory.Write_StrBytes("B8");
                CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _ig2ACLibIOSetBallSupply_PtrOffset));
                //mov [eax], CusomPointerAddress
                CaveMemory.Write_StrBytes("C7 00");
                CaveMemory.Write_Bytes(BitConverter.GetBytes(_ig2ACLibIOSetBallSupply_CustomFunctionPtr));

                //mov eax, FunctionPointer_Address
                CaveMemory.Write_StrBytes("B8");
                CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _ig2ACLibIOSetBallSupplyStop_PtrOffset));
                //mov [eax], CusomPointerAddress
                CaveMemory.Write_StrBytes("C7 00");
                CaveMemory.Write_Bytes(BitConverter.GetBytes(_ig2ACLibIOSetBallSupplyStop_CustomFunctionPtr));

                //mov eax, FunctionPointer_Address
                CaveMemory.Write_StrBytes("B8");
                CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _ig2ACLibIOSetBallSupplyStopImmediate_PtrOffset));
                //mov [eax], CusomPointerAddress
                CaveMemory.Write_StrBytes("C7 00");
                CaveMemory.Write_Bytes(BitConverter.GetBytes(_ig2ACLibIOSetBallSupplyStopImmediate_CustomFunctionPtr));

                //mov eax, FunctionPointer_Address
                CaveMemory.Write_StrBytes("B8");
                CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _ig2ACLibIOGetBallSupplySensor_PtrOffset));
                //mov [eax], CusomPointerAddress
                CaveMemory.Write_StrBytes("C7 00");
                CaveMemory.Write_Bytes(BitConverter.GetBytes(_ig2ACLibIOGetBallSupplySensor_CustomFunctionPtr));

                //mov eax, FunctionPointer_Address
                CaveMemory.Write_StrBytes("B8");
                CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _ig2ACLibIOCoinBlockerOpen_PtrOffset));
                //mov [eax], CusomPointerAddress
                CaveMemory.Write_StrBytes("C7 00");
                CaveMemory.Write_Bytes(BitConverter.GetBytes(_ig2ACLibIOCoinBlockerOpen_CustomFunctionPtr));

                //mov eax, FunctionPointer_Address
                CaveMemory.Write_StrBytes("B8");
                CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _ig2ACLibIOCoinBlockerClose_PtrOffset));
                //mov [eax], CusomPointerAddress
                CaveMemory.Write_StrBytes("C7 00");
                CaveMemory.Write_Bytes(BitConverter.GetBytes(_ig2ACLibIOCoinBlockerClose_CustomFunctionPtr));

                //mov eax, FunctionPointer_Address
                CaveMemory.Write_StrBytes("B8");
                CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _ig2ACLibIOCoinGetIncrease_PtrOffset));
                //mov [eax], CusomPointerAddress
                CaveMemory.Write_StrBytes("C7 00");
                CaveMemory.Write_Bytes(BitConverter.GetBytes(_ig2ACLibIOCoinGetIncrease_CustomFunctionPtr));

                //mov eax, FunctionPointer_Address
                CaveMemory.Write_StrBytes("B8");
                CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _ig2ACLibIOSecurityRenew_PtrOffset));
                //mov [eax], CusomPointerAddress
                CaveMemory.Write_StrBytes("C7 00");
                CaveMemory.Write_Bytes(BitConverter.GetBytes(_ig2ACLibIOSecurityRenew_CustomFunctionPtr));

                //mov eax, FunctionPointer_Address
                CaveMemory.Write_StrBytes("B8");
                CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _ig2ACLibIOSecurityRenewIsEnd_PtrOffset));
                //mov [eax], CusomPointerAddress
                CaveMemory.Write_StrBytes("C7 00");
                CaveMemory.Write_Bytes(BitConverter.GetBytes(_ig2ACLibIOSecurityRenewIsEnd_CustomFunctionPtr));

                //mov eax, FunctionPointer_Address
                CaveMemory.Write_StrBytes("B8");
                CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _ig2ACLibIOSecurityCheck_PtrOffset));
                //mov [eax], CusomPointerAddress
                CaveMemory.Write_StrBytes("C7 00");
                CaveMemory.Write_Bytes(BitConverter.GetBytes(_ig2ACLibIOSecurityCheck_CustomFunctionPtr));

                //mov eax, FunctionPointer_Address
                CaveMemory.Write_StrBytes("B8");
                CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _ig2ACLibIOSecurityGetSystemID_PtrOffset));
                //mov [eax], CusomPointerAddress
                CaveMemory.Write_StrBytes("C7 00");
                CaveMemory.Write_Bytes(BitConverter.GetBytes(_ig2ACLibIOSecurityGetSystemID_CustomFunctionPtr));

                //mov eax, FunctionPointer_Address
                CaveMemory.Write_StrBytes("B8");
                CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _ig2ACLibIOSecurityGetHardwareID_PtrOffset));
                //mov [eax], CusomPointerAddress
                CaveMemory.Write_StrBytes("C7 00");
                CaveMemory.Write_Bytes(BitConverter.GetBytes(_ig2ACLibIOSecurityGetHardwareID_CustomFunctionPtr));

                //mov eax, FunctionPointer_Address
                CaveMemory.Write_StrBytes("B8");
                CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _ig2ACLibIOSecurityGetSoftwareID_PtrOffset));
                //mov [eax], CusomPointerAddress
                CaveMemory.Write_StrBytes("C7 00");
                CaveMemory.Write_Bytes(BitConverter.GetBytes(_ig2ACLibIOSecurityGetSoftwareID_CustomFunctionPtr));

                //Some time, consulting Bookkeepings crash the game if it's not "blank"
                //Using this to test if disabling Coin number saving can make it work ??
                if (_DisableBookkeepingAddCoin)
                {
                    //mov eax, FunctionPointer_Address
                    CaveMemory.Write_StrBytes("B8");
                    CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _ig2ACLibBookkeepingAddCoin_PtrOffset));
                    //mov [eax], CusomPointerAddress
                    CaveMemory.Write_StrBytes("C7 00");
                    CaveMemory.Write_Bytes(BitConverter.GetBytes(_ig2ACLibBookkeepingAddCoin_CustomFunctionPtr));
                }

                //pop edi
                CaveMemory.Write_StrBytes("5F");
                //pop esi
                CaveMemory.Write_StrBytes("5E");
                //xor eax,eax
                CaveMemory.Write_StrBytes("33 C0");
                //pop ebx
                CaveMemory.Write_StrBytes("5B");

                //Inject it
                CaveMemory.InjectToOffset(_ig2ACLib_LoadLibraryFunction_InjectionOffset, "ig2ACLib LoadLibrary overwrite");

                Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
            }
            catch (Exception Ex)
            {
                Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() Error : " + Ex.Message.ToString());
            }
        }

        /// <summary>
        /// To replace call to unwanted functions that are no more needed without the real machine
        /// Just EAX=0 and RET
        /// </summary>
        private void Create_ig2ACLibNullSub_CustomFunction()
        {
            try
            {
                Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

                //xor eax, eax
                CaveMemory.Write_StrBytes("31 C0");
                //ret
                CaveMemory.Write_StrBytes("C3");

                //
                // Add target function here :
                //
                _ig2ACLibUpdate_CustomFunctionPtr = (UInt32)CaveMemory.CaveAddress;
                _ig2ACLibBoot_CustomFunctionPtr = (UInt32)CaveMemory.CaveAddress;
                _ig2ACLibIOSetCalibration_CustomFunctionPtr = (UInt32)CaveMemory.CaveAddress;
                _ig2ACLibIOSetLamp_CustomFunctionPtr = (UInt32)CaveMemory.CaveAddress;
                _ig2ACLibIOSetBallSupply_CustomFunctionPtr = (UInt32)CaveMemory.CaveAddress;
                _ig2ACLibIOSetBallSupplyStop_CustomFunctionPtr = (UInt32)CaveMemory.CaveAddress;
                _ig2ACLibIOSetBallSupplyStopImmediate_CustomFunctionPtr = (UInt32)CaveMemory.CaveAddress;
                _ig2ACLibIOSecurityRenew_CustomFunctionPtr = (UInt32)CaveMemory.CaveAddress;
                _ig2ACLibBookkeepingAddCoin_CustomFunctionPtr = (UInt32)CaveMemory.CaveAddress;

                Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() succesfully created at 0x" + CaveMemory.CaveAddress.ToString("X8"));
            }
            catch (Exception Ex)
            {
                Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() Error : " + Ex.Message.ToString());
            }
        }

        /// <summary>
        /// Replacing call to ig2ACLibBootWait() and setting good values ourselves
        /// </summary>
        private void Create_ig2ACLibBootWait_CustomFunction()
        {
            try
            {
                Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

                //mov [esp+28],00000000
                CaveMemory.Write_StrBytes("C7 44 24 28 00 00 00 00");
                //mov [esp+2C],00000000
                CaveMemory.Write_StrBytes("C7 44 24 2C 00 00 00 00");
                //mov [esp+30],00000000
                CaveMemory.Write_StrBytes("C7 44 24 30 00 00 00 00");
                //ret
                CaveMemory.Write_StrBytes("C3");

                _ig2ACLibBootWait_CustomFunctionPtr = (UInt32)CaveMemory.CaveAddress;
                Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() succesfully created at 0x" + CaveMemory.CaveAddress.ToString("X8"));
            }
            catch (Exception Ex)
            {
                Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() Error : " + Ex.Message.ToString());
            }
        }

        /// <summary>
        /// Replacing call to ig2ACLibGetLastError() and setting good values ourselves
        /// </summary>
        private void Create_ig2ACLibGetLastError_CustomFunction()
        {
            try
            {
                Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

                //mov byte ptr[esp+1B],00
                CaveMemory.Write_StrBytes("C6 44 24 1B 00");
                //ret
                CaveMemory.Write_StrBytes("C3");

                _ig2ACLibGetLastError_CustomFunctionPtr = (UInt32)CaveMemory.CaveAddress;
                Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() succesfully created at 0x" + CaveMemory.CaveAddress.ToString("X8"));
            }
            catch (Exception Ex)
            {
                Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() Error : " + Ex.Message.ToString());
            }
        }

        /// <summary>
        /// Replacing call to Create_ig2ACLibIsCalibrationEnd_CustomFunction() and setting good return values ourselves
        /// </summary>
        private void Create_2ACLibIOIsCalibrationEnd_CustomFunction()
        {
            try
            {
                Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

                //mov byte ptr[ecx],01
                CaveMemory.Write_StrBytes("C6 01 01");
                //mov byte ptr[ecx],01
                CaveMemory.Write_StrBytes("C6 02 01");
                //xor eax, eax
                CaveMemory.Write_StrBytes("31 C0");
                //ret
                CaveMemory.Write_StrBytes("C3");

                _ig2ACLibIOIsCalibrationEnd_CustomFunctionPtr = (UInt32)CaveMemory.CaveAddress;
                Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() succesfully created at 0x" + CaveMemory.CaveAddress.ToString("X8"));
            }
            catch (Exception Ex)
            {
                Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() Error : " + Ex.Message.ToString());
            }
        }

        /// <summary>
        /// Replacing call to ig2ACLibIOGetBallPosSensorPosNum() function by writing our own value
        /// This gets the number of simultaneous hits detected by the sensors (each one will then be checked for Axis after)
        /// 
        /// We could have forced it to "1". This would be good for mouse play, but using lightguns it would not allow simultaneous shots
        /// </summary>
        private void Create_ig2ACLibIOGetBallPosSensorPosNum_CustomFunction()
        {
            try
            {
                Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

                //movzx eax,byte ptr [_DetectedShotCount_CaveAddress]
                CaveMemory.Write_StrBytes("0F B6 05");
                CaveMemory.Write_Bytes(BitConverter.GetBytes(_DetectedShotCount_CaveAddress));
                //mov byte ptr [_DetectedShotCount_CaveAddress],00
                CaveMemory.Write_StrBytes("C6 05");
                CaveMemory.Write_Bytes(BitConverter.GetBytes(_DetectedShotCount_CaveAddress));
                CaveMemory.Write_StrBytes("00");
                //mov [esp+1C],eax
                CaveMemory.Write_StrBytes("89 44 24 1C");
                //ret
                CaveMemory.Write_StrBytes("C3");

                _ig2ACLibIOGetBallPosSensorPosNum_CustomFunctionPtr = (UInt32)CaveMemory.CaveAddress;
                Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() succesfully created at 0x" + CaveMemory.CaveAddress.ToString("X8"));
            }
            catch (Exception Ex)
            {
                Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() Error : " + Ex.Message.ToString());
            }
        }

        /// <summary>
        /// For each detected ball in the last function, ig2ACLibIOGetBallPosSensorPos will be called to get X/Y value of the ball
        /// 2-Bytes value, range [1280*640]
        /// </summary>
        private void Create_ig2ACLibIOGetBallPosSensorPos()
        {
            try
            {
                Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

                //mov ecx,ebx
                CaveMemory.Write_StrBytes("8B CB");
                //shl ecx,01
                CaveMemory.Write_StrBytes("D1 E1");
                //add ecx,_BallXPositions_CaveAddress
                CaveMemory.Write_StrBytes("81 C1");
                CaveMemory.Write_Bytes(BitConverter.GetBytes(_BallXPositions_CaveAddress));
                //movzx ecx,word ptr [ecx]
                CaveMemory.Write_StrBytes("0F B7 09");
                //mov [esp+28],cx
                CaveMemory.Write_StrBytes("66 89 4C 24 28");
                //mov ecx,ebx
                CaveMemory.Write_StrBytes("8B CB");
                //shl ecx,01
                CaveMemory.Write_StrBytes("D1 E1");
                //add ecx,_BallYPositions_CaveAddress
                CaveMemory.Write_StrBytes("81 C1");
                CaveMemory.Write_Bytes(BitConverter.GetBytes(_BallYPositions_CaveAddress));
                //movzx ecx,word ptr [ecx]
                CaveMemory.Write_StrBytes("0F B7 09");
                //mov [esp+2A],cx
                CaveMemory.Write_StrBytes("66 89 4C 24 2A");
                //ret
                CaveMemory.Write_StrBytes("C3");

                _ig2ACLibIOGetBallPosSensorPos_CustomFunctionPtr = (UInt32)CaveMemory.CaveAddress;
                Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() succesfully created at 0x" + CaveMemory.CaveAddress.ToString("X8"));
            }
            catch (Exception Ex)
            {
                Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() Error : " + Ex.Message.ToString());
            }
        }

        /// <summary>
        /// Replacing Button input data by our codecave values
        /// </summary>
        private void Create_ig2ACLibIOGetButton_CustomFunction()
        {
            try
            {
                Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

                //push ecx
                CaveMemory.Write_StrBytes("51");
                //push esi
                CaveMemory.Write_StrBytes("56");
                //push edi
                CaveMemory.Write_StrBytes("57");
                //mov esi, _ButtonsArray_CaveAddress
                CaveMemory.Write_StrBytes("BE");
                CaveMemory.Write_Bytes(BitConverter.GetBytes(_ButtonsArray_CaveAddress));
                //mov edi, ecx
                CaveMemory.Write_StrBytes("8B F9");
                //mov ecx,00000006
                CaveMemory.Write_StrBytes("B9 06 00 00 00");
                //repe movsd
                CaveMemory.Write_StrBytes("F3 A5");
                //pop edi
                CaveMemory.Write_StrBytes("5F");
                //pop esi
                CaveMemory.Write_StrBytes("5E");
                //pop ecx
                CaveMemory.Write_StrBytes("59");
                //ret
                CaveMemory.Write_StrBytes("C3");

                _ig2ACLibIOGetButton_CustomFunctionPtr = (UInt32)CaveMemory.CaveAddress;
                Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() succesfully created at 0x" + CaveMemory.CaveAddress.ToString("X8"));
            }
            catch (Exception Ex)
            {
                Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() Error : " + Ex.Message.ToString());
            }
        }

        /// <summary>
        /// Replacing Sebsors input data by our codecave values
        /// </summary>
        private void Create_ig2ACLibIOGetBallSupplySensor_CustomFunction()
        {
            try
            {
                Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

                //push ecx
                CaveMemory.Write_StrBytes("51");
                //push esi
                CaveMemory.Write_StrBytes("56");
                //push edi
                CaveMemory.Write_StrBytes("57");
                //mov esi, _P1P2_Sensor1_CaveAddress
                CaveMemory.Write_StrBytes("BE");
                CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1P2_Sensor1_CaveAddress));
                //mov edi,esp
                CaveMemory.Write_StrBytes("8B FC");
                //add edi,5C
                CaveMemory.Write_StrBytes("83 C7 5C");
                //mov ecx,00000006
                CaveMemory.Write_StrBytes("B9 06 00 00 00");
                //repe movsb
                CaveMemory.Write_StrBytes("F3 A4");
                //pop edi
                CaveMemory.Write_StrBytes("5F");
                //pop esi
                CaveMemory.Write_StrBytes("5E");
                //pop ecx
                CaveMemory.Write_StrBytes("59");
                //ret
                CaveMemory.Write_StrBytes("C3");

                _ig2ACLibIOGetBallSupplySensor_CustomFunctionPtr = (UInt32)CaveMemory.CaveAddress;
                Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() succesfully created at 0x" + CaveMemory.CaveAddress.ToString("X8"));
            }
            catch (Exception Ex)
            {
                Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() Error : " + Ex.Message.ToString());
            }
        }

        /// <summary>
        /// Just called without returning anything, but can be used to activate outputs with DemulShooter
        /// And anyway....freeing the genuine lib from useless work
        /// </summary>
        private void Create_ig2ACLibIOCoinBlockerOpen_CustomFunction()
        {
            try
            {
                Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

                //ret
                CaveMemory.Write_StrBytes("C3");

                _ig2ACLibIOCoinBlockerOpen_CustomFunctionPtr = (UInt32)CaveMemory.CaveAddress;
                Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() succesfully created at 0x" + CaveMemory.CaveAddress.ToString("X8"));
            }
            catch (Exception Ex)
            {
                Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() Error : " + Ex.Message.ToString());
            }
        }

        /// <summary>
        /// Just called without returning anything, but can be used to activate outputs with DemulShooter
        /// And anyway....freeing the genuine lib from useless work
        /// </summary>
        private void Create_ig2ACLibIOCoinBlockerClose_CustomFunction()
        {
            try
            {
                Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

                //ret
                CaveMemory.Write_StrBytes("C3");

                _ig2ACLibIOCoinBlockerClose_CustomFunctionPtr = (UInt32)CaveMemory.CaveAddress;
                Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() succesfully created at 0x" + CaveMemory.CaveAddress.ToString("X8"));
            }
            catch (Exception Ex)
            {
                Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() Error : " + Ex.Message.ToString());
            }
        }

        /// <summary>
        /// Replace call to ig2ACLibIOCoinGetIncrease() by our custom value of inserted coins to simulate coin-in
        /// </summary>
        private void Create_ig2ACLibIOCoinGetIncrease_CustomFunction()
        {
            try
            {
                Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

                //mov eax,byte ptr[_InsertedCredits_CaveAddress]
                CaveMemory.Write_StrBytes("0F B6 05");
                CaveMemory.Write_Bytes(BitConverter.GetBytes(_InsertedCoins_CaveAddress));
                //mov byte ptr[_InsertedCredits_CaveAddress], 0
                CaveMemory.Write_StrBytes("C6 05");
                CaveMemory.Write_Bytes(BitConverter.GetBytes(_InsertedCoins_CaveAddress));
                CaveMemory.Write_StrBytes("00");
                //mov[esp + 20],eax
                CaveMemory.Write_StrBytes("89 44 24 20");
                //ret
                CaveMemory.Write_StrBytes("C3");

                _ig2ACLibIOCoinGetIncrease_CustomFunctionPtr = (UInt32)CaveMemory.CaveAddress;
                Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() succesfully created at 0x" + CaveMemory.CaveAddress.ToString("X8"));
            }
            catch (Exception Ex)
            {
                Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() Error : " + Ex.Message.ToString());
            }
        }

        /// <summary>
        /// Faking Security check OK
        /// </summary>
        private void Create_ig2ACLibIOSecurityRenewIsEnd_CustomFunction()
        {
            try
            {
                Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

                //mov byte ptr[esp+17], 1
                CaveMemory.Write_StrBytes("C6 44 24 17 01");
                //xor eax, eax
                CaveMemory.Write_StrBytes("31 C0");
                //ret
                CaveMemory.Write_StrBytes("C3");

                _ig2ACLibIOSecurityRenewIsEnd_CustomFunctionPtr = (UInt32)CaveMemory.CaveAddress;
                Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() succesfully created at 0x" + CaveMemory.CaveAddress.ToString("X8"));
            }
            catch (Exception Ex)
            {
                Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() Error : " + Ex.Message.ToString());
            }
        }

        /// <summary>
        /// Faking Security check OK
        /// </summary>
        private void Create_ig2ACLibIOSecurityCheck_CustomFunction()
        {
            try
            {
                Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

                //mov byte ptr[esp+22], 1
                CaveMemory.Write_StrBytes("C6 44 24 22 01");
                //mov byte ptr[esp+23], 1
                CaveMemory.Write_StrBytes("C6 44 24 23 01");
                //xor eax, eax
                CaveMemory.Write_StrBytes("31 C0");
                //ret
                CaveMemory.Write_StrBytes("C3");

                _ig2ACLibIOSecurityCheck_CustomFunctionPtr = (UInt32)CaveMemory.CaveAddress;
                Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() succesfully created at 0x" + CaveMemory.CaveAddress.ToString("X8"));
            }
            catch (Exception Ex)
            {
                Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() Error : " + Ex.Message.ToString());
            }
        }

        /// <summary>
        /// Write custom string :-)
        /// </summary>
        private void Create_ig2ACLibIOSecurityGetSystemID_CustomFunction()
        {
            try
            {
                Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

                //push ecx
                CaveMemory.Write_StrBytes("51");
                //push esi
                CaveMemory.Write_StrBytes("56");
                //push edi
                CaveMemory.Write_StrBytes("57");
                //mov esi, _SystemID_CaveAddress
                CaveMemory.Write_StrBytes("BE");
                CaveMemory.Write_Bytes(BitConverter.GetBytes(_SystemID_CaveAddress));
                //mov edi,esp
                CaveMemory.Write_StrBytes("8B FC");
                //add edi,28
                CaveMemory.Write_StrBytes("83 C7 28");
                //mov ecx,00000010
                CaveMemory.Write_StrBytes("B9 10 00 00 00");
                //repe movsb
                CaveMemory.Write_StrBytes("F3 A4");
                //pop edi
                CaveMemory.Write_StrBytes("5F");
                //pop esi
                CaveMemory.Write_StrBytes("5E");
                //pop ecx
                CaveMemory.Write_StrBytes("59");
                //xor eax, eax
                CaveMemory.Write_StrBytes("31 C0");
                //ret
                CaveMemory.Write_StrBytes("C3");

                _ig2ACLibIOSecurityGetSystemID_CustomFunctionPtr = (UInt32)CaveMemory.CaveAddress;
                Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() succesfully created at 0x" + CaveMemory.CaveAddress.ToString("X8"));
            }
            catch (Exception Ex)
            {
                Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() Error : " + Ex.Message.ToString());
            }

        }

        /// <summary>
        /// Write custom string ;-)
        /// </summary>
        private void Create_ig2ACLibIOSecurityGetHardwareID_CustomFunction()
        {
            try
            {
                Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

                //push ecx
                CaveMemory.Write_StrBytes("51");
                //push esi
                CaveMemory.Write_StrBytes("56");
                //push edi
                CaveMemory.Write_StrBytes("57");
                //mov esi, _HardwareID_CaveAddress
                CaveMemory.Write_StrBytes("BE");
                CaveMemory.Write_Bytes(BitConverter.GetBytes(_HardwareID_CaveAddress));
                //mov edi,esp
                CaveMemory.Write_StrBytes("8B FC");
                //add edi,28
                CaveMemory.Write_StrBytes("83 C7 28");
                //mov ecx,00000010
                CaveMemory.Write_StrBytes("B9 10 00 00 00");
                //repe movsb
                CaveMemory.Write_StrBytes("F3 A4");
                //pop edi
                CaveMemory.Write_StrBytes("5F");
                //pop esi
                CaveMemory.Write_StrBytes("5E");
                //pop ecx
                CaveMemory.Write_StrBytes("59");
                //xor eax, eax
                CaveMemory.Write_StrBytes("31 C0");
                //ret
                CaveMemory.Write_StrBytes("C3");

                _ig2ACLibIOSecurityGetHardwareID_CustomFunctionPtr = (UInt32)CaveMemory.CaveAddress;
                Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() succesfully created at 0x" + CaveMemory.CaveAddress.ToString("X8"));
            }
            catch (Exception Ex)
            {
                Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() Error : " + Ex.Message.ToString());
            }
        }

        /// <summary>
        /// Write custom string ;-)
        /// </summary>
        private void Create_ig2ACLibIOSecurityGetSoftwareID_CustomFunction()
        {
            try
            {
                Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

                //push ecx
                CaveMemory.Write_StrBytes("51");
                //push esi
                CaveMemory.Write_StrBytes("56");
                //push edi
                CaveMemory.Write_StrBytes("57");
                //mov esi, _SoftwareID_CaveAddress
                CaveMemory.Write_StrBytes("BE");
                CaveMemory.Write_Bytes(BitConverter.GetBytes(_SoftwareID_CaveAddress));
                //mov edi,esp
                CaveMemory.Write_StrBytes("8B FC");
                //add edi,28
                CaveMemory.Write_StrBytes("83 C7 28");
                //mov ecx,00000010
                CaveMemory.Write_StrBytes("B9 10 00 00 00");
                //repe movsb
                CaveMemory.Write_StrBytes("F3 A4");
                //pop edi
                CaveMemory.Write_StrBytes("5F");
                //pop esi
                CaveMemory.Write_StrBytes("5E");
                //pop ecx
                CaveMemory.Write_StrBytes("59");
                //xor eax, eax
                CaveMemory.Write_StrBytes("31 C0");
                //ret
                CaveMemory.Write_StrBytes("C3");

                _ig2ACLibIOSecurityGetSoftwareID_CustomFunctionPtr = (UInt32)CaveMemory.CaveAddress;
                Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() succesfully created at 0x" + CaveMemory.CaveAddress.ToString("X8"));
            }
            catch (Exception Ex)
            {
                Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() Error : " + Ex.Message.ToString());
            }

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        #endregion

        #endregion

        #region Libbms.dll Hacks

        private void Apply_Libbms_Hacks()
        {
            try
            {
                /// Prevent the lib from opening the named pipe, and forcing good value return 
                WriteBytes(_libbms_BaseAddress + _Libbms_EAmusement_Patch1_Offset, new byte[] { 0xE9, 0x81, 0x03, 0x00, 0x00, 0x90 });
                WriteByte(_libbms_BaseAddress + _Libbms_EAmusement_Patch2_Offset, 0x00);

                // Replace content of bms_credit_confirm_blocking by return 0 ( = OK )
                WriteBytes(_libbms_BaseAddress + _Libbms_CreditConfirmBlocking_Patch1_Offset, new byte[] { 0xE9, 0x9D, 0x00, 0x00, 0x00, 0x90 });
                WriteBytes(_libbms_BaseAddress + _Libbms_CreditConfirmBlocking_Patch2_Offset, new byte[] { 0x31, 0xC0 });

                //Not seen that one call, so did it just in case but may need to check crash if it is called ???
                WriteBytes(_libbms_BaseAddress + _Libbms_CreditConfirm_Patch_Offset, new byte[] { 0x31, 0xC0, 0xC3, 0x90, 0x90, 0x90 });

                Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
            }
            catch (Exception Ex)
            {
                Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() Error : " + Ex.Message.ToString());
            }

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        #endregion

        #region Outputs Hack

        protected override void Apply_OutputsMemoryHack()
        {
             Create_OutputsDataBank();
            _Lamps_CaveAddress = OutputsDatabank_Address;
            _CoinBlocker_CaveAddress = OutputsDatabank_Address + 0x10;
            _MotorUnits_CaveAddress = OutputsDatabank_Address + 0x14;

            SetHack_ig2ACLibIOSetLamp();
            SetHack_ig2ACLibIOCoinBlockerOpen();
            SetHack_ig2ACLibIOCoinBlockerClose();

            SetHack_ig2ACLibIOSetBallSupply();
            SetHack_ig2ACLibIOSetBallSupplyStop();

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        /// <summary>
        /// Create a simple function that gets the Lamp Id (ESP+4) and Lamp value (ESP+8) before RET
        /// </summary>
        private void SetHack_ig2ACLibIOSetLamp()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //push ecx
            CaveMemory.Write_StrBytes("51");
            //mov eax,_Lamps_CaveAddress
            CaveMemory.Write_StrBytes("B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Lamps_CaveAddress));
            //add eax,[esp+08]
            CaveMemory.Write_StrBytes("03 44 24 08");
            //mov ecx,[esp+0C]
            CaveMemory.Write_StrBytes("8B 4C 24 0C");
            //mov [eax],cl
            CaveMemory.Write_StrBytes("88 08");
            //pop ecx
            CaveMemory.Write_StrBytes("59");
            //xor eax,eax
            CaveMemory.Write_StrBytes("31 C0");
            //ret
            CaveMemory.Write_StrBytes("C3");

            WriteBytes(GameProcess_ModuleBaseAddress + _ig2ACLibIOSetLamp_PtrOffset, BitConverter.GetBytes((UInt32)CaveMemory.CaveAddress));
        }

        /// <summary>
        /// Creating a simple function to set a flag
        /// </summary>
        private void SetHack_ig2ACLibIOCoinBlockerOpen()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //mov byte ptr[_CoinBlocker_CaveAddress], 1
            CaveMemory.Write_StrBytes("C6 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_CoinBlocker_CaveAddress));
            CaveMemory.Write_StrBytes("01");
            //ret
            CaveMemory.Write_StrBytes("C3");

            WriteBytes(GameProcess_ModuleBaseAddress + _ig2ACLibIOCoinBlockerOpen_PtrOffset, BitConverter.GetBytes((UInt32)CaveMemory.CaveAddress));
        }

        /// <summary>
        /// Creating a simple function to set a flag
        /// </summary>
        private void SetHack_ig2ACLibIOCoinBlockerClose()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //mov byte ptr[_CoinBlocker_CaveAddress], 1
            CaveMemory.Write_StrBytes("C6 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_CoinBlocker_CaveAddress));
            CaveMemory.Write_StrBytes("00");
            //ret
            CaveMemory.Write_StrBytes("C3");

            WriteBytes(GameProcess_ModuleBaseAddress + _ig2ACLibIOCoinBlockerClose_PtrOffset, BitConverter.GetBytes((UInt32)CaveMemory.CaveAddress));
        }

        /// <summary>
        /// Creating simple functions to set a flag
        /// Motor ID (0/1) is in ESP+4
        /// </summary>
        private void SetHack_ig2ACLibIOSetBallSupply()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //mov eax,_Lamps_CaveAddress
            CaveMemory.Write_StrBytes("B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_MotorUnits_CaveAddress));
            //add eax,[esp+04]
            CaveMemory.Write_StrBytes("03 44 24 04");
            //mov byte ptr[eax], 1
            CaveMemory.Write_StrBytes("C6 00 01");
            //xor eax,eax
            CaveMemory.Write_StrBytes("31 C0");
            //ret
            CaveMemory.Write_StrBytes("C3");

            WriteBytes(GameProcess_ModuleBaseAddress + _ig2ACLibIOSetBallSupply_PtrOffset, BitConverter.GetBytes((UInt32)CaveMemory.CaveAddress));
        }

        /// <summary>
        /// Creating simple functions to set a flag
        /// Motor ID (0/1) is in ESP+4
        /// Applying the same function for the 2 different procedure (Stop + StopImmediate)
        /// </summary>
        private void SetHack_ig2ACLibIOSetBallSupplyStop()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //mov eax,_MotorUnits_CaveAddress
            CaveMemory.Write_StrBytes("B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_MotorUnits_CaveAddress));
            //add eax,[esp+04
            CaveMemory.Write_StrBytes("03 44 24 04");
            //mov byte ptr[eax], 0
            CaveMemory.Write_StrBytes("C6 00 00");
            //xor eax,eax
            CaveMemory.Write_StrBytes("31 C0");
            //ret
            CaveMemory.Write_StrBytes("C3");

            WriteBytes(GameProcess_ModuleBaseAddress + _ig2ACLibIOSetBallSupplyStop_PtrOffset, BitConverter.GetBytes((UInt32)CaveMemory.CaveAddress));
            WriteBytes(GameProcess_ModuleBaseAddress + _ig2ACLibIOSetBallSupplyStopImmediate_PtrOffset, BitConverter.GetBytes((UInt32)CaveMemory.CaveAddress));
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
            else if (e.ButtonName.Equals("COIN"))
            {
                WriteByte(_ButtonsArray_CaveAddress + 0x0B, 0x01);

                //Low level Hook does not get "Key already pressed" flag on lParam, so we need to do it ourselves
                if (ReadByte(_InsertedCoins_CaveAddress + 0x01) == 0)
                {
                    WriteBytes(_InsertedCoins_CaveAddress, new byte[] { 0x01, 0x01 });
                }
            }
            else if (e.ButtonName.Equals("SERVICE"))
            {
                WriteByte(_ButtonsArray_CaveAddress + 0x0A, 0x01);
            }
            else if (e.ButtonName.Equals("TEST"))
            {
                WriteByte(_ButtonsArray_CaveAddress + 0x09, 0x01);
            }
        }

        protected override void GameController_OnButtonUp(object sender, VirtualGameControler.ButtonDownEventArgs e)
        {
            if (e.ButtonName.Equals("COIN"))
            {
                WriteByte(_ButtonsArray_CaveAddress + 0x0B, 0x00);
                WriteBytes(_InsertedCoins_CaveAddress, new byte[] { 0x00, 0x00 });
            }
            else if (e.ButtonName.Equals("SERVICE"))
            {
                WriteByte(_ButtonsArray_CaveAddress + 0x0A, 0x00);
            }
            else if (e.ButtonName.Equals("TEST"))
            {
                WriteByte(_ButtonsArray_CaveAddress + 0x09, 0x00);
            }
        }

        protected override void UpdateAxis(PlayerID Id, int X, int Y)
        {
            if (Id != PlayerID.None)
            {
                byte[] bufferX = BitConverter.GetBytes(X);
                byte[] bufferY = BitConverter.GetBytes(Y);
            }
        }

        protected override void PlayerController_OnButtonDown(object sender, VirtualGameControler.ButtonDownEventArgs e)
        {
            if (e.Id != PlayerID.None)
            {
                UInt32 PlayerIndex = (UInt32)e.Id;
                VirtualPlayerController Controller = (VirtualPlayerController)sender;

                if (e.ButtonName.Equals("TRIGGER"))
                {
                    uint ShotCount = ReadByte(_DetectedShotCount_CaveAddress);
                    //Low level Hook does not get "Key already pressed" flag on lParam, so we need to do it ourselves
                    if (ReadByte(_P1_ShotDetection_CaveAddress + (PlayerIndex * 2)) == 0)
                    {
                        WriteBytes(_P1_ShotDetection_CaveAddress + (PlayerIndex * 2), new byte[] { 0x01, 0x01 });
                        WriteBytes(_BallXPositions_CaveAddress + (ShotCount * 2), BitConverter.GetBytes((Int16)Controller.Computed_X));
                        WriteBytes(_BallYPositions_CaveAddress + (ShotCount * 2), BitConverter.GetBytes((Int16)Controller.Computed_Y));
                        ShotCount += 1;
                    }
                    WriteByte(_DetectedShotCount_CaveAddress, (byte)ShotCount);
                }
                else if (e.ButtonName.Equals("START"))
                {
                    WriteByte(_ButtonsArray_CaveAddress + 0x0C + PlayerIndex, 0x01);
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
                    WriteBytes(_P1_ShotDetection_CaveAddress + (PlayerIndex * 2), new byte[] { 0x00, 0x00 });
                }
                else if (e.ButtonName.Equals("START"))
                {
                    WriteByte(_ButtonsArray_CaveAddress + 0x0C + PlayerIndex, 0x00);
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
                new GameOutput(OutputId.P3_LmpStart),
                new GameOutput(OutputId.P4_LmpStart),
                new GameOutput(OutputId.P1_LmpFront),
                new GameOutput(OutputId.P2_LmpFront),
                new GameOutput(OutputId.P3_LmpFront),
                new GameOutput(OutputId.P4_LmpFront),
                new GameOutput(OutputId.MotorUnit_1),
                new GameOutput(OutputId.MotorUnit_2),
                new GameOutput(OutputId.CoinBlocker_1),
                new GameOutput(OutputId.Credits)
            };
        }

        /// <summary>
        /// Update all Outputs values before sending them to MameHooker
        /// </summary>
        public override void UpdateOutputValues()
        {
            //Genuine Outputs
            SetOutputValue(OutputId.P1_LmpStart, ReadByte(_Lamps_CaveAddress));
            SetOutputValue(OutputId.P2_LmpStart, ReadByte(_Lamps_CaveAddress + 1));
            SetOutputValue(OutputId.P3_LmpStart, ReadByte(_Lamps_CaveAddress + 2));
            SetOutputValue(OutputId.P4_LmpStart, ReadByte(_Lamps_CaveAddress + 3));
            SetOutputValue(OutputId.P1_LmpFront, ReadByte(_Lamps_CaveAddress + 4));
            SetOutputValue(OutputId.P2_LmpFront, ReadByte(_Lamps_CaveAddress + 5));
            SetOutputValue(OutputId.P3_LmpFront, ReadByte(_Lamps_CaveAddress + 6));
            SetOutputValue(OutputId.P4_LmpFront, ReadByte(_Lamps_CaveAddress + 7));
            SetOutputValue(OutputId.MotorUnit_1, ReadByte(_MotorUnits_CaveAddress));
            SetOutputValue(OutputId.MotorUnit_2, ReadByte(_MotorUnits_CaveAddress + 1));
            SetOutputValue(OutputId.CoinBlocker_1, ReadByte(_CoinBlocker_CaveAddress));

            int Credits = (int)ReadPtrChain(GameProcess_ModuleBaseAddress + _CreditsPtr_Offset, new UInt32[] { 0x20 });
            SetOutputValue(OutputId.Credits, Credits);
        }

        #endregion
    }
}
