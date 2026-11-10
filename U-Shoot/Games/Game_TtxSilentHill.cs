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
    public class Game_TtxSilentHill : BaseGame32
    {
        protected override string GAMEDATA_FOLDER { get { return @"Ttx\sha"; } }
        protected override bool PATCH_PEB_DEBUG_FLAG { get { return true; } }

        private const string KACBOOKKEEPINGDLL_NAME = "kacbookkeeping.dll";
        private UInt32 _KacbookkeepingDll_BaseAddress;
        private const string SHAIOLIBDLL_NAME = "shaiolib.dll";
        private UInt32 _ShaiolibDll_BaseAddress;

        private const string SYSTEM_ID = "U-SHOOT\0";
        private const string HARDWARE_ID = "ARGON-LEFOU\0";
        private const string SOFTWARE_ID = "1234\0";

        private const string LEVEL_SELECT_OPTION = "Level Select";
        private const string GREEN_BANNER_OPTION = "Remove Green Banner";
        private const string AMD_FIX_OPTION = "AMD Fix";
        private const string LANGUAGE_OPTION = "Language";
        private const string LOCAL_SAVE_OPTION = "Local Save";

        #region LOADER MEMORY ADDRESSES
        private UInt32 _GreenBanner_Patch_Offset = 0x0028BA72;
        private UInt32 _NoCursor_Patch_Offset = 0x000062DA;
        private UInt32 _AmdFix_Patch_Offset = 0x00141FE9;
        private UInt32 _LocalSave_Patch1_Offset = 0x00009188;
        private UInt32 _LocalSave_Patch2_Offset = 0x000091A0;
        private UInt32 _LocalSave_Patch3_Offset = 0x000017C8;
        private UInt32 _LevelSelect_Patch_Offset = 0x0028ABA1;
        private NopStruct32 _Nop_JapLanguage1 = new NopStruct32(0x000D189F, 2);
        private NopStruct32 _Nop_JapLanguage2 = new NopStruct32(0x000D18D7, 2);
        private NopStruct32 _Nop_CrcCheck = new NopStruct32(0x000D7073, 2);
        private InjectionStruct32 _ScreenMode_InjectionStruct = new InjectionStruct32(0x0000760C, 5);
        #endregion

        #region INPUTS MEMORY ADDRESSES    
        //private UInt32 _ScreenWidth_Offset = 0x000083D4;
        //private UInt32 _ScreenHeight_Offset = 0x000083CF;
        private NopStruct32 _Nop_CalibrationCalc = new NopStruct32(0x000D1303, 2);
        private InjectionStruct32 _ShootResolution_InjectionStruct = new InjectionStruct32(0x00008D31, 6);
        //shaiolib function spointers offset:
        private UInt32 _Shaiolib_10Key_disable_Ptr_Offset = 0x00280500;
        private UInt32 _Shaiolib_10Key_enable_Ptr_Offset = 0x00280504;
        private UInt32 _Shaiolib_10Key_status_Ptr_Offset = 0x002804D4;
        private UInt32 _Shaiolib_BUTTON_status_Ptr_Offset = 0x00280524;
        private UInt32 _Shaiolib_CARD_start_Ptr_Offset = 0x002804B8;
        private UInt32 _Shaiolib_CARD_status_Ptr_Offset = 0x002804B0;
        private UInt32 _Shaiolib_CARD_stop_Ptr_Offset = 0x002804B4;
        private UInt32 _Shaiolib_COIN_blocker_close_Ptr_Offset = 0x002804FC;
        private UInt32 _Shaiolib_COIN_blocker_open_Ptr_Offset = 0x002804F8;
        private UInt32 _Shaiolib_COIN_consume_Ptr_Offset = 0x002804E0;
        private UInt32 _Shaiolib_COIN_count_Ptr_Offset = 0x002804DC;
        private UInt32 _Shaiolib_COIN_countup_Ptr_Offset = 0x00280530;
        private UInt32 _Shaiolib_GUN_disable_Ptr_Offset = 0x00280518;
        private UInt32 _Shaiolib_GUN_enable_Ptr_Offset = 0x0028051C;
        private UInt32 _Shaiolib_GUN_flash_off_Ptr_Offset = 0x00280514;
        private UInt32 _Shaiolib_GUN_flash_on_Ptr_Offset = 0x00280510;
        private UInt32 _Shaiolib_GUN_reaction_Ptr_Offset = 0x00280508;
        private UInt32 _Shaiolib_GUN_set_reaction_pattern_Ptr_Offset = 0x0028050C;
        private UInt32 _Shaiolib_GUN_status_Ptr_Offset = 0x002804D8;
        private UInt32 _Shaiolib_LED_change_Ptr_Offset = 0x00280538;
        private UInt32 _Shaiolib_SECURE_check_Ptr_Offset = 0x002804CC;
        private UInt32 _Shaiolib_SECURE_check_no_errror_Ptr_Offset = 0x00280520;
        private UInt32 _Shaiolib_SECURE_isrenewal_Ptr_Offset = 0x002804C8;
        private UInt32 _Shaiolib_SECURE_renew_Ptr_Offset = 0x002804D0;
        private UInt32 _Shaiolib_SYSINFO_get_hardware_id_Ptr_Offset = 0x002804C0;
        private UInt32 _Shaiolib_SYSINFO_get_mother_board_Ptr_Offset = 0x0028052C;
        private UInt32 _Shaiolib_SYSINFO_get_software_id_Ptr_Offset = 0x002804BC;
        private UInt32 _Shaiolib_SYSINFO_get_system_id_Ptr_Offset = 0x002804C4;
        private UInt32 _Shaiolib_get_lasterror_Ptr_Offset = 0x00280528;
        private UInt32 _Shaiolib_initialize_Ptr_Offset = 0x002804A8;
        private UInt32 _Shaiolib_is_boot_all_Ptr_Offset = 0x002804F4; 
        private UInt32 _Shaiolib_is_boot_board_button_Ptr_Offset = 0x00280534;
        private UInt32 _Shaiolib_is_boot_board_gun_Ptr_Offset = 0x002804F0;
        private UInt32 _Shaiolib_is_error_Ptr_Offset = 0x002804A4;
        private UInt32 _Shaiolib_reset_error_Ptr_Offset = 0x002804EC;
        private UInt32 _Shaiolib_start_boot_Ptr_Offset = 0x002804AC;
        private UInt32 _Shaiolib_terminate_Ptr_Offset = 0x002804E8;
        private UInt32 _Shaiolib_update_Ptr_Offset = 0x002804E4;

        //Custom Data
        private UInt32 _Buttons_CaveAddress;
        private UInt32 _GunData_CaveAddress;
        private UInt32 _CoinCount_CaveAddress;
        private UInt32 _MotherboardId_String_CaveAddress;
        private UInt32 _SystemId_String_CaveAddress;
        private UInt32 _HardwareId_String_CaveAddress;
        private UInt32 _SoftwareId_String_CaveAddress;
        private UInt32 _TrueStub_FuntcionAddress;
        private UInt32 _FalseStub_FunctionAddress;
        #endregion

        #region OUTPUTS MEMORY ADDRESSES
        private UInt32 _DataPointer_Offset = 0x002DE4C8;
        private UInt32 _Credits_Offset = 0x002F9008;
        private InjectionStruct32 _Recoil_Injection_Offset = new InjectionStruct32(0x000D0CD0, 6);
        private InjectionStruct32 _Damaged_InjectionStruct = new InjectionStruct32(0x000105911, 5);

        //Custom Data
        private UInt32 _Recoil_CaveAddress;
        private UInt32 _Damaged_CaveAddress;
        #endregion

        private Dictionary<string, string> _KnownMd5_ShaiolibDll;

        public Game_TtxSilentHill(GameConfig Config) : base(Config)
        {
            _KnownMd5Prints.Add("Silent Hill Arcade - Original KSHG_no_cursor.exe", "0e58fd1c7bcb5e0cace0e4ee548ecd0c");
            _KnownMd5Prints.Add("Silent Hill Arcade - Original KSHG.exe", "2778219dcaf3d8e09fb197417b17dc1b");

            _KnownMd5_ShaiolibDll = new Dictionary<string, string>
            {
                { "Original shaiolib.dll", "265feedebc6bdfacaa332b92c1c456cf" },
                { "Win10 patched shaiolib.dll", "4326c9e36cd2f02c2d370074d1b7e364" },
                { "Win7/XP patched shaiolib.dll", "02044ee92baa8f4ac90dbd1818666a91" },
            };
        }

        protected override void AttachDebuggerEvents()
        {
            _GameLauncher.OnLoadedDll += LoadDllEvent;
        }

        #region Debugger Events

        #region EXCEPTION_DEBUG_EVENT

        protected override void ExceptionEvent(object sender, x86_x64_Debugger.ExceptionDebugEventArgs e)
        {
            //Using Generic Exception event Handling to set Input/Outputs Hacks
            base.ExceptionEvent(sender, e);
        }

        #endregion

        #region LOAD_DLL_DEBUG_EVENT

        protected override void LoadDllEvent(object sender, x86_x64_Debugger.LoadDllDebugEventArgs e)
        {
            x86_x64_Debugger Dbg = (x86_x64_Debugger)sender;
            IntPtr lpBaseOfDllLoad = e.LoadDllInfo.lpBaseOfDll;

            if (lpBaseOfDllLoad != IntPtr.Zero)
            {
                if (string.Equals(Path.GetFileName(e.DllPath), KACBOOKKEEPINGDLL_NAME, StringComparison.OrdinalIgnoreCase))
                {
                    Logger.WriteLog("==> Wanted DLL found !");
                    //HANDLE should always be in 32Bits range (https://learn.microsoft.com/fr-fr/windows/win32/winprog64/interprocess-communication?redirectedfrom=MSDN)
                    Logger.WriteLog("  + hModule: 0x" + e.LoadDllInfo.hFile.ToString("X8"));
                    Logger.WriteLog("  + fUnicode: " + e.LoadDllInfo.fUnicode.ToString());

                    _KacbookkeepingDll_BaseAddress = (UInt32)e.LoadDllInfo.lpBaseOfDll;

                    if (_GameConfig.GetBoolOption(LOCAL_SAVE_OPTION))
                        SetHack_LocalSave();
                }
                else if (string.Equals(Path.GetFileName(e.DllPath), SHAIOLIBDLL_NAME, StringComparison.OrdinalIgnoreCase))
                {
                    Logger.WriteLog("==> Wanted DLL found !");
                    //HANDLE should always be in 32Bits range (https://learn.microsoft.com/fr-fr/windows/win32/winprog64/interprocess-communication?redirectedfrom=MSDN)
                    Logger.WriteLog("  + hModule: 0x" + e.LoadDllInfo.hFile.ToString("X8"));
                    Logger.WriteLog("  + fUnicode: " + e.LoadDllInfo.fUnicode.ToString());

                    _ShaiolibDll_BaseAddress = (UInt32)e.LoadDllInfo.lpBaseOfDll;

                    string ShaiolibDll_Path = _GameConfig.TargetPath.Replace(Path.GetFileName(_GameConfig.TargetFileName), SHAIOLIBDLL_NAME);
                    string Dll_Md5Hash = GetMd5HashAsString(ShaiolibDll_Path);
                    CheckMd5(ShaiolibDll_Path, _KnownMd5_ShaiolibDll);
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
                    double dMaxX = 640.0;
                    double dMaxY = 480.0;
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
            RaiseGameHookedEvent();
        }

        #region Loader Hack

        protected override void Apply_LoaderMemoryHack()
        {
            //Remove the exe CRC check
            SetNops(GameProcess_ModuleBaseAddress, _Nop_CrcCheck);

            if (_GameConfig.Windowed)
            {
                SetHack_WindowedMode();
            }

            //English Language is hardcoded into the game initialisation process
            if (_GameConfig.GetStringOption(LANGUAGE_OPTION) != "English")
            {
                SetHack_Language();
            }

            //Replace './message.tbl' string by 'bpl/bar.dds' to remove the scrolling green banner on main screen
            if (_GameConfig.GetBoolOption(GREEN_BANNER_OPTION))
            {
                ReplaceReadOnlyString(new uint[] { _GreenBanner_Patch_Offset }, new byte[] { 0x62, 0x70, 0x6C, 0x2F, 0x62, 0x61, 0x72, 0x2E, 0x64, 0x64, 0x73 });
            }

            //Pushing original value parameter for SetCursor call is removing mouse cursor on my computer, 
            //whereas using "KSHG_noCursor.exe" - which has NOPed instruction instead - still shows the  "wait" spinning cursor. 
            //No guarantees on this patch....test to try. 
            if (_GameConfig.NoCursor)
            {
                WriteBytes(GameProcess_ModuleBaseAddress + _NoCursor_Patch_Offset, new byte[] { 0x6A, 0x00 });
            }

            //Fix transparent ennemies with AMD GPU
            if (_GameConfig.GetBoolOption(AMD_FIX_OPTION))
            {
                //pop ecx
                //pop ebx
                //mov eax,[KSHG.exe + 2D19C4]
                //pop edi
                //pop esi
                //mov esp, ebp
                //pop ebp
                //ret 0004
                WriteBytes(GameProcess_ModuleBaseAddress + _AmdFix_Patch_Offset, new byte[] { 0x59, 0x5B, 0xA1, 0xC4, 0x19, 0x6D, 0x00, 0x5F, 0x5E, 0x8B, 0xE5, 0x5D, 0xC2, 0x04, 0x00 });
            }

            //Set the starting level
            string Level = _GameConfig.GetStringOption(LEVEL_SELECT_OPTION, "1-1");
            ReplaceReadOnlyString(new UInt32[] { _LevelSelect_Patch_Offset }, Encoding.ASCII.GetBytes(Level.Replace('-', '_'))); 

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        /// <summary>
        /// Changing D3DPRESENT_PARAMETERS just before call to d3dCreate9()
        /// BackBufferFormat = D3DFMT_UNKNOWN   
        /// WINDOWED = TRUE
        /// FullScreen_RefreshRateInHz = 0
        /// </summary>
        private void SetHack_WindowedMode()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //push ebp
            CaveMemory.Write_StrBytes("55");
            //lea ebp,[esi+38]
            CaveMemory.Write_StrBytes("8D 6E 38");
            //mov byte ptr [ebp+08],00
            CaveMemory.Write_StrBytes("C6 45 08 00");   //D3DFMT_UNKNOWN
            //mov byte ptr [ebp+20],01
            CaveMemory.Write_StrBytes("C6 45 20 01");   //Windowed
            //mov byte ptr [ebp+30],00
            CaveMemory.Write_StrBytes("C6 45 30 00");   //0 Hz
            //push ebp
            CaveMemory.Write_StrBytes("55");

            //Inject it
            CaveMemory.InjectToOffset(_ScreenMode_InjectionStruct, "Screen Mode");
        }

        /// <summary>
        /// Default Language is English and is hardcoded to be so
        /// Game starts with ENG language and check security dongle in a loop for following languages
        /// JAP is 0, but only checked after all other failed.
        /// To force JAP, it's possible to make a custom shaiolib_SECURE_check_no_error() only setting TRUE for desired ID
        /// But it's simpler to force the loop to fail and make the game test JAP in the end
        /// </summary>
        private void SetHack_Language()
        {
            SetNops(GameProcess_ModuleBaseAddress, _Nop_JapLanguage1);
            SetNops(GameProcess_ModuleBaseAddress, _Nop_JapLanguage2);
        }

        /// <summary>
        /// kacbookkeeping.dll is saving/reading Bookkeeping in E:/KSHG/KACBK and F:/KSHG/KACBK
        /// Current dir at the time it's called is : /data/eng
        /// So we can't use simple './' replacement as it's not in the root folder, change it to ../../KACBK
        /// So we need to jump over the CreateDirectory() call and change the name for the CreateFile() call
        /// </summary>
        private void SetHack_LocalSave()
        {
            ReplaceReadOnlyString(_KacbookkeepingDll_BaseAddress, new UInt32[] { _LocalSave_Patch1_Offset, _LocalSave_Patch2_Offset }, new byte[] { 0x2E, 0x2E, 0x2F, 0x2E, 0x2E, 0x2F, 0x4B, 0x41, 0x43, 0x42, 0x4B, 0x00, 0x00 });

            //CreateDir : just jump over
            WriteBytes(_KacbookkeepingDll_BaseAddress + _LocalSave_Patch3_Offset, new byte[] { 0xEB, 0x06 });
        }

        #endregion        

        #region Inputs Hack

        protected override void Apply_InputsMemoryHack()
        {
            Create_InputsDataBank();
            _Buttons_CaveAddress = InputsDatabank_Address;                      //12 Bytes
            _GunData_CaveAddress = InputsDatabank_Address + 0x10;               //32 Bytes
            _CoinCount_CaveAddress = InputsDatabank_Address + 0x30;
            _MotherboardId_String_CaveAddress = InputsDatabank_Address + 0x38;  //1  Byte
            _SystemId_String_CaveAddress = InputsDatabank_Address + 0x40;       //24 Bytes
            _HardwareId_String_CaveAddress = InputsDatabank_Address + 0x60;     //19 Bytes
            _SoftwareId_String_CaveAddress = InputsDatabank_Address + 0x20;

            //Fixing 3D world shoot calculation
            SetHack_GameResolutionShoot();

            //Create our stub functions
            Create_FalseStub();
            Create_TrueStub();

            //Replace pointers to shaiolib.dll to pointers to our stubs
            ReplaceReadOnlyString(new uint[]
                {
                    _Shaiolib_10Key_status_Ptr_Offset,
                    _Shaiolib_COIN_countup_Ptr_Offset,
                    _Shaiolib_get_lasterror_Ptr_Offset,
                    _Shaiolib_is_error_Ptr_Offset,
                    _Shaiolib_reset_error_Ptr_Offset,
                    _Shaiolib_start_boot_Ptr_Offset
                }, BitConverter.GetBytes(_FalseStub_FunctionAddress));

            ReplaceReadOnlyString(new uint[]
                {
                    _Shaiolib_10Key_disable_Ptr_Offset,
                    _Shaiolib_10Key_enable_Ptr_Offset,
                    _Shaiolib_CARD_start_Ptr_Offset,
                    _Shaiolib_CARD_status_Ptr_Offset,
                    _Shaiolib_CARD_stop_Ptr_Offset,
                    _Shaiolib_COIN_blocker_close_Ptr_Offset,
                    _Shaiolib_COIN_blocker_open_Ptr_Offset,                    
                    _Shaiolib_GUN_disable_Ptr_Offset,
                    _Shaiolib_GUN_enable_Ptr_Offset,
                    _Shaiolib_GUN_flash_off_Ptr_Offset,
                    _Shaiolib_GUN_flash_on_Ptr_Offset,
                    _Shaiolib_GUN_reaction_Ptr_Offset,
                    _Shaiolib_GUN_set_reaction_pattern_Ptr_Offset,
                    _Shaiolib_LED_change_Ptr_Offset,
                    _Shaiolib_SECURE_check_Ptr_Offset,
                    _Shaiolib_SECURE_check_no_errror_Ptr_Offset,
                    _Shaiolib_SECURE_isrenewal_Ptr_Offset,
                    _Shaiolib_SECURE_renew_Ptr_Offset,                 
                    _Shaiolib_initialize_Ptr_Offset,
                    _Shaiolib_is_boot_all_Ptr_Offset,
                    _Shaiolib_is_boot_board_button_Ptr_Offset,
                    _Shaiolib_is_boot_board_gun_Ptr_Offset,
                    _Shaiolib_terminate_Ptr_Offset,
                    _Shaiolib_update_Ptr_Offset
                }, BitConverter.GetBytes(_TrueStub_FuntcionAddress));


            WriteBytes(_MotherboardId_String_CaveAddress, Encoding.ASCII.GetBytes("A\0"));
            SetHack_MotherBoardId();

            WriteBytes(_SystemId_String_CaveAddress, Encoding.ASCII.GetBytes(SYSTEM_ID));
            SetHack_SystemId();

            WriteBytes(_HardwareId_String_CaveAddress, Encoding.ASCII.GetBytes(HARDWARE_ID));
            SetHack_HardwareId();

            WriteBytes(_SoftwareId_String_CaveAddress, Encoding.ASCII.GetBytes(SOFTWARE_ID));
            SetHack_SoftwareId();

            //Handling COINS
            SetHack_CoinCount();
            SetHack_CoinConsume();

            //Replacing inputs
            SetHack_Buttons();
            SetHack_Guns();

            //Forcing the game to not apply calibration calculation
            //In the function called whenever a trigger press is detected
            SetNops(GameProcess_ModuleBaseAddress, _Nop_CalibrationCalc);
        }

        /// <summary>
        /// 3D, 2D, sprites, TEST menu shoot locatio for SHOTS are all handled differently from the inital axis values (640x480), but almost all of them are based on hardcoded 640x480 resolution
        /// 3D world location is computed based on boudaries obtained with GetScreenRect(), as a result this will mess aim if DgVoodoo is used with upsalling or resolution change.
        /// By forcing the result to 640x480, and GameScaling inputs to [640,480] with U-Shoot, all inputs should scale correctly in any mode
        /// 
        /// Note that GetScreenRect() is also called in ther part of the code, but for now, it stays unchanged
        /// </summary>
        private void SetHack_GameResolutionShoot()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);
            //mov [esi+2C],00000280
            CaveMemory.Write_StrBytes("C7 46 2C 80 02 00 00");
            //mov [esi+30],000001E0
            CaveMemory.Write_StrBytes("C7 46 30 E0 01 00 00");
            //Inject it
            CaveMemory.InjectToOffset(_ShootResolution_InjectionStruct, "GameResolutionShoot");

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        /// <summary>
        /// Create a Stub doing nothing else than returning EAX = 1
        /// </summary>
        private void Create_TrueStub()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //mov eax, 1
            CaveMemory.Write_StrBytes("B8 01 00 00 00");
            //ret
            CaveMemory.Write_StrBytes("C3");

            _TrueStub_FuntcionAddress = CaveMemory.CaveAddress;
        }

        /// <summary>
        /// Create a Stub doing nothing else than returning EAX = 0
        /// </summary>
        private void Create_FalseStub()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //xor eax, eax
            CaveMemory.Write_StrBytes("33 C0");
            //ret
            CaveMemory.Write_StrBytes("C3");

            _FalseStub_FunctionAddress = CaveMemory.CaveAddress;
        }

        /// <summary>
        /// Set EAX to a memory pointer to MotherBoard String ID
        /// </summary>
        private void SetHack_MotherBoardId()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //mov eax, _MotherboardID_String_CaveAddress
            CaveMemory.Write_StrBytes("B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_MotherboardId_String_CaveAddress));
            //ret
            CaveMemory.Write_StrBytes("C3");

            ReplaceReadOnlyString(new UInt32[] { _Shaiolib_SYSINFO_get_mother_board_Ptr_Offset }, BitConverter.GetBytes(CaveMemory.CaveAddress));
        }

        /// <summary>
        /// Set EAX to a memory pointer to Software String ID
        /// </summary>
        private void SetHack_SystemId()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //mov eax, _SystemId_String_CaveAddress
            CaveMemory.Write_StrBytes("B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_SystemId_String_CaveAddress));
            //ret
            CaveMemory.Write_StrBytes("C3");

            ReplaceReadOnlyString(new UInt32[] { _Shaiolib_SYSINFO_get_system_id_Ptr_Offset }, BitConverter.GetBytes(CaveMemory.CaveAddress));
        }

        /// <summary>
        /// Set EAX to a memory pointer to Hardware String ID
        /// </summary>
        private void SetHack_HardwareId()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //mov eax, _SystemId_String_CaveAddress
            CaveMemory.Write_StrBytes("B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_HardwareId_String_CaveAddress));
            //ret
            CaveMemory.Write_StrBytes("C3");

            ReplaceReadOnlyString(new UInt32[] { _Shaiolib_SYSINFO_get_hardware_id_Ptr_Offset }, BitConverter.GetBytes(CaveMemory.CaveAddress));
        }

        /// <summary>
        /// Set EAX to a memory pointer to Software String ID
        /// </summary>
        private void SetHack_SoftwareId()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //mov eax, _SoftwareId_String_CaveAddress
            CaveMemory.Write_StrBytes("B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_SoftwareId_String_CaveAddress));
            //ret
            CaveMemory.Write_StrBytes("C3");

            ReplaceReadOnlyString(new UInt32[] { _Shaiolib_SYSINFO_get_software_id_Ptr_Offset }, BitConverter.GetBytes(CaveMemory.CaveAddress));
        }

        /// <summary>
        /// Game keeps reading the number of new COINS
        /// Filling the &Param with the value
        /// </summary>
        private void SetHack_CoinCount()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //push esi
            CaveMemory.Write_StrBytes("56");
            // mov esi, DWORD PTR[esp + 0x8]
            CaveMemory.Write_StrBytes("8B 74 24 08");
            //mov eax, [_CoinCount_CaveAddress]
            CaveMemory.Write_StrBytes("A1");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_CoinCount_CaveAddress));
            //mov DWORD PTR[esi],eax
            CaveMemory.Write_StrBytes("89 06");
            //pop esi
            CaveMemory.Write_StrBytes("5E");
            //xor eax, eax
            //push esi
            CaveMemory.Write_StrBytes("31 C0");
            //ret
            CaveMemory.Write_StrBytes("C3");

            ReplaceReadOnlyString(new UInt32[] { _Shaiolib_COIN_count_Ptr_Offset }, BitConverter.GetBytes(CaveMemory.CaveAddress));
        }

        /// <summary>
        /// And then, game send IO board how much of the new credits it has validated
        /// Removing them from the IO counter
        /// </summary>
        private void SetHack_CoinConsume()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //push esi
            CaveMemory.Write_StrBytes("56");
            // mov esi, DWORD PTR[esp + 0x8]
            CaveMemory.Write_StrBytes("8B 74 24 08");
            //mov eax, [_CoinCount_CaveAddress]
            CaveMemory.Write_StrBytes("A1");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_CoinCount_CaveAddress));
            //sub eax, esi
            CaveMemory.Write_StrBytes("29 F0");
            //mov [_CoinCount_CaveAddress], eax
            CaveMemory.Write_StrBytes("A3");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_CoinCount_CaveAddress));
            //pop esi
            CaveMemory.Write_StrBytes("5E");
            //xor eax, eax
            //push esi
            CaveMemory.Write_StrBytes("31 C0");
            //ret
            CaveMemory.Write_StrBytes("C3");

            ReplaceReadOnlyString(new UInt32[] { _Shaiolib_COIN_consume_Ptr_Offset }, BitConverter.GetBytes(CaveMemory.CaveAddress));
        }

        /// <summary>
        /// Replace the original function pointer to a custom codecave, copying our data to feed the game
        /// </summary>
        private void SetHack_Buttons()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //push ecx
            CaveMemory.Write_StrBytes("51");
            //mov ecx,[esp+08]
            CaveMemory.Write_StrBytes("8B 4C 24 08");
            //mov eax,[_Buttons_CaveAddress]
            CaveMemory.Write_StrBytes("8B 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Buttons_CaveAddress));
            //mov [ecx],eax
            CaveMemory.Write_StrBytes("89 01");
            //and [_Buttons_CaveAddress],FEFEFEFE
            CaveMemory.Write_StrBytes("81 25");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Buttons_CaveAddress));
            CaveMemory.Write_StrBytes("FE FE FE FE");

            //mov eax,[_Buttons_CaveAddress + 4]
            CaveMemory.Write_StrBytes("8B 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Buttons_CaveAddress + 4));
            //mov [ecx+4],eax
            CaveMemory.Write_StrBytes("89 41 04");
            //and [_Buttons_CaveAddress + 4],FEFEFEFE
            CaveMemory.Write_StrBytes("81 25");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Buttons_CaveAddress + 4));
            CaveMemory.Write_StrBytes("FE FE FE FE");

            //mov eax,[_Buttons_CaveAddress + 8]
            CaveMemory.Write_StrBytes("8B 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Buttons_CaveAddress + 8));
            //mov [ecx+8],eax
            CaveMemory.Write_StrBytes("89 41 08");
            //and [_Buttons_CaveAddress + 8],FEFEFEFE
            CaveMemory.Write_StrBytes("81 25");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Buttons_CaveAddress + 8));
            CaveMemory.Write_StrBytes("FE FE FE FE");

            //pop ecx
            CaveMemory.Write_StrBytes("59");
            //mov eax, 1
            CaveMemory.Write_StrBytes("B8 01 00 00 00");
            //ret
            CaveMemory.Write_StrBytes("C3");

            ReplaceReadOnlyString(new UInt32[] { _Shaiolib_BUTTON_status_Ptr_Offset }, BitConverter.GetBytes(CaveMemory.CaveAddress));
        }

        /// <summary>
        /// Replace the original function pointer to a custom codecave, copying our data to feed the game
        /// </summary>
        private void SetHack_Guns()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //push esi
            CaveMemory.Write_StrBytes("56");
            //push edi
            CaveMemory.Write_StrBytes("57");
            //push ecx
            CaveMemory.Write_StrBytes("51");
            //mov edi,esp
            CaveMemory.Write_StrBytes("8B FC");
            //mov edi, [edi+10]
            CaveMemory.Write_StrBytes("8B 7F 10");
            //mov esi,_GunData_CaveAddress
            CaveMemory.Write_StrBytes("BE");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_GunData_CaveAddress));
            //mov ecx,00000008
            CaveMemory.Write_StrBytes("B9 08 00 00 00");
            //repe movsd
            CaveMemory.Write_StrBytes("F3 A5");

            //Resetting pressed button flag ?
            //mov esi,_GunData_CaveAddress
            CaveMemory.Write_StrBytes("BE");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_GunData_CaveAddress));
            //mov [esi+08],00000000
            CaveMemory.Write_StrBytes("C7 46 08 00 00 00 00");
            //mov [esi+0C],00000000
            CaveMemory.Write_StrBytes("C7 46 0C 00 00 00 00");
            //mov [esi+18],00000000
            CaveMemory.Write_StrBytes("C7 46 18 00 00 00 00");
            //mov [esi+1C],00000000
            CaveMemory.Write_StrBytes("C7 46 1C 00 00 00 00");


            //pop ecx
            CaveMemory.Write_StrBytes("59");
            //pop edi
            CaveMemory.Write_StrBytes("5F");
            //pop esi
            CaveMemory.Write_StrBytes("5E");

            //mov eax, 1
            CaveMemory.Write_StrBytes("B8 01 00 00 00");
            //ret
            CaveMemory.Write_StrBytes("C3");

            ReplaceReadOnlyString(new UInt32[] { _Shaiolib_GUN_status_Ptr_Offset }, BitConverter.GetBytes(CaveMemory.CaveAddress));
        }

        #endregion

        #region Outputs Hack

        protected override void Apply_OutputsMemoryHack()
        {
            Create_OutputsDataBank();
            _Recoil_CaveAddress = OutputsDatabank_Address;
            _Damaged_CaveAddress = OutputsDatabank_Address + 0x10;

            SetHack_Recoil();
            SetHack_Damaged();

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        /// <summary>
        /// To activate gun recoil, the game is calling the GUN_Reaction() function of shaiolib.dll.
        /// This function has been ripped from the cracked Dll, so we are injecting a piece of code
        /// to intercept the calls and handle the Recoil ourselves when the game wants to
        /// </summary>
        private void SetHack_Recoil()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //push eax
            CaveMemory.Write_StrBytes("50");
            //mov eax,[esp+08]
            CaveMemory.Write_StrBytes("8B 44 24 08");            
            //add eax, _Recoil_CaveAddress
            CaveMemory.Write_StrBytes("05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Recoil_CaveAddress));
            //mov byte ptr [eax],01
            CaveMemory.Write_StrBytes("C6 00 01");
            //pop eax
            CaveMemory.Write_StrBytes("58");
            //ret
            CaveMemory.Write_StrBytes("C3");

            //inject it
            CaveMemory.InjectToOffset(_Recoil_Injection_Offset, "Recoil");
        }

        /// <summary>
        /// Intercepting the Life subsctract operation when player is damaged
        /// EBX has player index
        /// </summary>
        private void SetHack_Damaged()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //mov byte ptr[ebx+_Damaged_CaveAddress],01
            CaveMemory.Write_StrBytes("C6 83");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Damaged_CaveAddress));
            CaveMemory.Write_StrBytes("01");
            //movss [ecx+08],xmm0
            CaveMemory.Write_StrBytes("F3 0F 11 41 08");

            //Inject it
            CaveMemory.InjectToOffset(_Damaged_InjectionStruct, "Damaged");
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
                byte bCoin = ReadByte(_CoinCount_CaveAddress);
                bCoin++;
                WriteByte(_CoinCount_CaveAddress, bCoin);

                //COIN MECH in TEST menu
                WriteByte(_Buttons_CaveAddress + 0x02, 0x81);
            }
            else if (e.ButtonName.Equals("SERVICE"))
            {
                WriteByte(_Buttons_CaveAddress + 0x01, 0x81);
            }
            else if (e.ButtonName.Equals("TEST"))
            {
                WriteByte(_Buttons_CaveAddress, 0x81);
            }
        }

        protected override void GameController_OnButtonUp(object sender, VirtualGameControler.ButtonDownEventArgs e)
        {
            if (e.ButtonName.Equals("COIN"))
            {
                //COIN MECH in TEST menu
                WriteByte(_Buttons_CaveAddress + 0x02, 0x01);
            }
            if (e.ButtonName.Equals("SERVICE"))
            {
                WriteByte(_Buttons_CaveAddress + 0x01, 0x01);
            }
            else if (e.ButtonName.Equals("TEST"))
            {
                WriteByte(_Buttons_CaveAddress, 0x01);
            }
        }

        protected override void UpdateAxis(PlayerID Id, int X, int Y)
        {
            if (Id != PlayerID.None)
            {
                byte[] bufferX = BitConverter.GetBytes(X);
                byte[] bufferY = BitConverter.GetBytes(Y);

                WriteBytes(_GunData_CaveAddress + ((uint)Id * 0x10), bufferX);
                WriteBytes(_GunData_CaveAddress + 0x04 + ((uint)Id * 0x10), bufferY);
            }
        }

        protected override void PlayerController_OnButtonDown(object sender, VirtualGameControler.ButtonDownEventArgs e)
        {
            if (e.Id != PlayerID.None)
            {
                UInt32 PlayerIndex = (UInt32)e.Id;

                if (e.ButtonName.Equals("TRIGGER"))
                {
                    WriteBytes(_GunData_CaveAddress + 0x08 + (PlayerIndex * 0x10), new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00 });
                }
                else if (e.ButtonName.Equals("RELOAD"))
                {
                    WriteBytes(_GunData_CaveAddress + 0x08 + (PlayerIndex * 0x10), new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF });
                }
                else if (e.ButtonName.Equals("START"))
                {
                    WriteByte(_Buttons_CaveAddress +  0x03 + PlayerIndex, 0x81);
                }
                else if (e.ButtonName.Equals("OPTION L"))
                {
                    WriteByte(_Buttons_CaveAddress + 0x05 + (PlayerIndex * 0x02), 0x81);
                }
                else if (e.ButtonName.Equals("OPTION R"))
                {
                    WriteByte(_Buttons_CaveAddress + 0x06 + (PlayerIndex * 0x02), 0x81);
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
                    WriteBytes(_GunData_CaveAddress + 0x08 + (PlayerIndex * 0x10), new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });
                }
                else if (e.ButtonName.Equals("RELOAD"))
                {
                    WriteBytes(_GunData_CaveAddress + 0x08 + (PlayerIndex * 0x10), new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });
                }
                else if (e.ButtonName.Equals("START"))
                {
                    WriteByte(_Buttons_CaveAddress + 0x03 + PlayerIndex, 0x01);
                }
                else if (e.ButtonName.Equals("OPTION L"))
                {
                    WriteByte(_Buttons_CaveAddress + 0x05 + (PlayerIndex * 0x02), 0x01);
                }
                else if (e.ButtonName.Equals("OPTION R"))
                {
                    WriteByte(_Buttons_CaveAddress + 0x06 + (PlayerIndex * 0x02), 0x01);
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
                new GameOutput(OutputId.LmpLeft),
                new GameOutput(OutputId.LmpRight),
                new GameOutput(OutputId.P1_LmpCard_R),
                new GameOutput(OutputId.P1_LmpCard_G),
                new GameOutput(OutputId.P2_LmpCard_R),
                new GameOutput(OutputId.P2_LmpCard_G),
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
            //Original Outputs
            byte bOutput = ReadByte(GameProcess_ModuleBaseAddress + 0x002E38C0);
            SetOutputValue(OutputId.P1_LmpStart, ReadByte(GameProcess_ModuleBaseAddress + 0x002E38C4));
            SetOutputValue(OutputId.P2_LmpStart, ReadByte(GameProcess_ModuleBaseAddress + 0x002E38CC));
            SetOutputValue(OutputId.P1_LmpCard_R, ReadByte(GameProcess_ModuleBaseAddress + 0x002E38D4));
            SetOutputValue(OutputId.P1_LmpCard_G, ReadByte(GameProcess_ModuleBaseAddress + 0x002E38DC));
            SetOutputValue(OutputId.P2_LmpCard_R, ReadByte(GameProcess_ModuleBaseAddress + 0x002E38E4));
            SetOutputValue(OutputId.P2_LmpCard_G, ReadByte(GameProcess_ModuleBaseAddress + 0x002E38EC));
            SetOutputValue(OutputId.LmpLeft, ReadByte(GameProcess_ModuleBaseAddress + 0x002E38F4));
            SetOutputValue(OutputId.LmpRight, ReadByte(GameProcess_ModuleBaseAddress + 0x002E38FC));


            //Custom Outputs
            UInt32 PlayerDataPtr = ReadPtr(_DataPointer_Offset);
            for (uint i = 0; i < _GameConfig.PlayerCount; i++)
            {
                if (ReadByte(_Recoil_CaveAddress + i) == 1)
                {
                    SetOutputValue(OutputId.P1_CtmRecoil + i, 1);
                    WriteByte(_Recoil_CaveAddress + i, 0);
                }

                if (ReadByte(_Damaged_CaveAddress + i) == 1)
                {
                    SetOutputValue(OutputId.P1_Damaged + i, 1);
                    WriteByte(_Damaged_CaveAddress + i, 0);
                }

                UInt32 pLife = ReadPtr(PlayerDataPtr + 0x1C + (4 * i));
                float Life = ReadFloat(pLife);
                SetOutputValue(OutputId.P1_Life + i, (int)Life);
            }

            SetOutputValue(OutputId.Credits, ReadByte(GameProcess_ModuleBaseAddress + _Credits_Offset));
        }

        #endregion
    }
}
