using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UsCore.Config;
using UsCore.Launcher;
using UsCore.Logging;
using UsCore.MameOutput;
using UsCore.Memory;
using UsCore.VirtualControllers;

namespace U_Shoot.Games
{
    public class Game_ArcadepcHsfr : BaseGame32
    {
        protected override bool PATCH_PEB_DEBUG_FLAG { get { return false; } }

        private const string GAMEASSEMBLY_DLL_NAME = "GameAssembly.dll";
        private IntPtr _GameAssemblyDll_BaseAddress = IntPtr.Zero;

        private const string LANGUAGE_OPTION = "Language";
        private const string PLAYER_NUM_OPTION = "Players";

        #region LOADER MEMORY ADDRESSES                
        private UInt32 _SuperDog_Offset = 0x008C1095;
        private UInt32 _DogCheck_Offset = 0x00276879;
        private UInt32 _ForceKeyboard_Offset = 0x00439604;
        private UInt32 _NumberFormatInfo_Func_Offset = 0x004C3B90;
        private UInt32 _PatchTicketsOut_Offset = 0x00428038;
        private UInt32 _ShowCursor_Patch_Offset = 0x0043E32E;
        private InjectionStruct32 _UafGameStarter_Awake_InjectionStruct = new InjectionStruct32(0x0043E74B, 5);
        private InjectionStruct32 _UafGameManager_Start_InjectionStruct = new InjectionStruct32(0x004393F3, 6);
        private InjectionStruct32 _UafControl_Init_InjectionStruct = new InjectionStruct32(0x004234AE, 6);
        private InjectionStruct32 _UafControlManager_Start_InjectionStruct = new InjectionStruct32(0x004236C2, 6);
        private InjectionStruct32 _DsLevelSelect_Start_DecimalPatch1_InjectionStruct = new InjectionStruct32(0x001D7B63, 5);
        private InjectionStruct32 _DsLevelSelect_Start_DecimalPatch2_InjectionStruct = new InjectionStruct32(0x001D7A77, 5);
        private InjectionStruct32 _DsLevelSelect_Update_DecimalPatch_InjectionStruct = new InjectionStruct32(0x001D9286, 6);
        private InjectionStruct32 _FwMissionUI_UpdateTimer_DecimalPatch_InjectionStruct = new InjectionStruct32(0x002F52F6, 5);
        private InjectionStruct32 _FwMissionTriggerUI_UpdateTimer_DecimalPatch_InjectionStruct = new InjectionStruct32(0x002F4956, 5);
        #endregion

        #region INPUTS MEMORY ADDRESSES    
        private UInt32 _Input_GetKeyDownInt_Ptr_Offset = 0x00B5A4E8;
        private InjectionStruct32 _UAfControlManager_MoveGunController_InjectionStruct = new InjectionStruct32(0x0042E9DB, 5);

        //Custom Data
        private UInt32 _AxisX_Array_CaveAddress;
        private UInt32 _AxisY_Array_CaveAddress;
        private UInt32 _Buttons_CaveAddress;
        #endregion

        #region OUTPUTS MEMORY ADDRESSES
        private InjectionStruct32 _UafGameManager_Update_InjectionStruct = new InjectionStruct32(0x0043A14A, 6);
        private InjectionStruct32 _UafControlManager_OpenLight_InjectionStruct = new InjectionStruct32(0x00428844, 6);
        private InjectionStruct32 _UafControlManager_CloseLight_InjectionStruct = new InjectionStruct32(0x00428A34, 6);
        private InjectionStruct32 _UafControlManager_StartSeatShake_InjectionStruct = new InjectionStruct32(0x00430CB8, 6);
        private InjectionStruct32 _UafControlManager_CloseSeatShake_InjectionStruct = new InjectionStruct32(0x00430E48, 6);
        private InjectionStruct32 _UafControlManager_StartShakeApparatusOut_InjectionStruct = new InjectionStruct32(0x00430A48, 6);
        private InjectionStruct32 _UafControlManager_CloseShake_InjectionStruct = new InjectionStruct32(0x00430978, 6);
        private InjectionStruct32 _UafControlManager_WaterMistControl_InjectionStruct = new InjectionStruct32(0x0042FA03, 6);
        private InjectionStruct32 _UafControlManager_Update_InjectionStruct = new InjectionStruct32(0x00428DB2, 5);
        private InjectionStruct32 _SgPlayer_PlayEffectHitted_InjectionStruct = new InjectionStruct32(0x003BD333, 6);

        //Custom Data
        private UInt32 _Light_Array_CaveAddress;
        private UInt32 _Motor_Array_CaveAddress;
        private UInt32 _DamageArray_CaveAddress;
        private UInt32 _UafGameManager_Instance_CaveAddress;
        private UInt32 _UafControlManager_Instance_CaveAddress;
        private UInt32 _WaterMistState_CaveAddress;
        #endregion

        #region Settings 

        //Temporary : Game settings ?
        public enum VERSION_TYPE
        {
            DEVELOP,
            BOSS_TEST,
            VIDEO_RECORD,
            AUTO_TEST,
            TEST,
            APPROVAL,
            RELEASE
        }
        public enum SPECIAL_VERSION
        {
            None,
            UDC,
            HG,
            US
        }
        public enum Language
        {
            Chinese,
            English
        }
        private VERSION_TYPE _GameVersionType = VERSION_TYPE.RELEASE;
        private SPECIAL_VERSION _GameSpecialVersionType = SPECIAL_VERSION.None;

        #endregion

        public Game_ArcadepcHsfr(GameConfig Config) : base(Config)
        {
            _KnownMd5Prints.Add("Hot Shots Fire Rescue - v3.3.20221123r.60.1PUMP", "eec51a30066a2a19fa7d3ad494518eab");
            _KnownMd5Prints.Add("Hot Shots Fire Rescue - v3.3.20221123r.60.1PUMP unpacked dll from SpiderZSoft", "bda23dea9d914d2992520748ca6ed1db");
        }

        //IL2CPP code is within GameAssembly.dll that gets loaded by the Unity player in the main exe
        protected override void AttachDebuggerEvents()
        {
            _GameLauncher.OnLoadedDll += LoadDllEvent;
        }

        #region Debugger Events

        #region EXCEPTION_DEBUG_EVENT

        protected override void ExceptionEvent(object sender, x86_x64_Debugger.ExceptionDebugEventArgs e)
        {
            //No PEB to patch and no patch to apply when EXe is loaded.
            //Everything must be done after GameAssembly.dll has loaded


            //Using Generic Exception event Handling to set Input/Outputs Hacks
            //base.ExceptionEvent(sender, e);
        }

        #endregion

        #region LOAD_DLL_DEBUG_EVENT

        protected override void LoadDllEvent(object sender, x86_x64_Debugger.LoadDllDebugEventArgs e)
        {
            x86_x64_Debugger Dbg = (x86_x64_Debugger)sender;
            IntPtr lpBaseOfDllLoad = e.LoadDllInfo.lpBaseOfDll;

            if (lpBaseOfDllLoad != IntPtr.Zero)
            {
                if (string.Equals(Path.GetFileName(e.DllPath), GAMEASSEMBLY_DLL_NAME, StringComparison.OrdinalIgnoreCase))
                {
                    Logger.WriteLog("==> Wanted DLL found !");
                    //HANDLE should always be in 32Bits range (https://learn.microsoft.com/fr-fr/windows/win32/winprog64/interprocess-communication?redirectedfrom=MSDN)
                    Logger.WriteLog("  + hModule: 0x" + e.LoadDllInfo.hFile.ToString("X8"));
                    Logger.WriteLog("  + fUnicode: " + e.LoadDllInfo.fUnicode.ToString());

                    _GameAssemblyDll_BaseAddress = e.LoadDllInfo.lpBaseOfDll;

                    CheckMd5(Path.Combine(Path.GetDirectoryName(_GameConfig.TargetPath), Path.GetFileName(e.DllPath)), _KnownMd5Prints);
                    Apply_MemoryHacks();
                    RaiseGameHookedEvent();
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

                    double dMinX = -960.0;
                    double dMinY = -540.0;
                    double dMaxX = 959.0;
                    double dMaxY = 539.0;
                    double dRangeX = dMaxX - dMinX + 1;
                    double dRangeY = dMaxY - dMinY + 1;

                    PlayerController.Computed_X = Convert.ToInt32(Math.Round(dRangeX * PlayerController.Computed_X / TotalResX) - dRangeX / 2);
                    PlayerController.Computed_Y = Convert.ToInt32((Math.Round(dRangeY * PlayerController.Computed_Y / TotalResY) - dRangeY / 2) * -1);
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
            //GameHooked event will be raised in DLL_LOADED event, once the usb DLL has been patched
            base.Apply_MemoryHacks();
        }

        #region Loader Hack

        protected override void Apply_LoaderMemoryHack()
        {
            if (_GameConfig.NoCursor)
            {
                //Forcing 0 in EAX before calling Win32 API
                WriteBytes((UInt32)_GameAssemblyDll_BaseAddress + _ShowCursor_Patch_Offset, new byte[] { 0x31, 0xC0, 0x90, 0x90 });
            }

            // 'xor eax, eax' to replace call for dog and force result (eax) to 0 (== DOG_OK)
            WriteBytes((UInt32)_GameAssemblyDll_BaseAddress + _SuperDog_Offset, new byte[] { 0x31, 0xC0 });

            // CheckDog.CheckDogFeature(): call to Get_Dog_File() (return a byte in a loop of RequestedbyteNumber) is creating error. 
            /// Replacing the call function by 'mov eax, 00' to force get result of "00" (== Perpetual bit flag) without calling
            WriteBytes((UInt32)_GameAssemblyDll_BaseAddress + _DogCheck_Offset, new byte[] { 0xB8, 0x00, 0x00, 0x00, 0x00 });

            SetHack_Resolution();
            SetHack_EditGameManagerData();
            SetHack_PatchDecimalSeparator();

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        /// <summary>
        /// In UAF_GameStarter.Awake() function, changing parameters of 
        /// Screen.SetScreenResolution() call
        /// </summary>
        private void SetHack_Resolution()
        {
            int Width = _GameConfig.ResolutionWidth != 0 ? _GameConfig.ResolutionWidth : 1920;
            int Height = _GameConfig.ResolutionHeight != 0 ? _GameConfig.ResolutionHeight : 1080;
            int Fullscreen = _GameConfig.Fullscreen  ? 1 : 0;

            Codecave32 CaveMemory = new Codecave32(_Process, (UInt32)_GameAssemblyDll_BaseAddress);

            //push Width
            CaveMemory.Write_StrBytes("68");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(Width));
            //mov [esp+04],Height
            CaveMemory.Write_StrBytes("C7 44 24 04");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(Height));
            //mov [esp+08],Fullscreen
            CaveMemory.Write_StrBytes("C7 44 24 08");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(Fullscreen));

            //Inject it
            CaveMemory.InjectToOffset(_UafGameStarter_Awake_InjectionStruct, "Res");
        }

        /// <summary>
        /// At the start of the UAF_GameManager.Start() function, we can change values previously set in Ctor()
        /// Like MAX_PLAYER, game version (release, debug, test)
        /// </summary>
        private void SetHack_EditGameManagerData()
        {
            string strPlayers = _GameConfig.GetStringOption(PLAYER_NUM_OPTION, "2");
            string strLanguage = _GameConfig.GetStringOption(LANGUAGE_OPTION, "EN");
            int iPlayers = 2;
            int iLanguage = 1;

            int.TryParse(strPlayers, out iPlayers);

            if (strLanguage.Equals("EN"))
                iLanguage = 1;
            else if (strLanguage.Equals("CH"))
                iLanguage = 0;

            Codecave32 CaveMemory = new Codecave32(_Process, (UInt32)_GameAssemblyDll_BaseAddress);

            //mov byte ptr [eax],01
            CaveMemory.Write_StrBytes("C6 00 01");
            //mov edi,[ebp+08]
            CaveMemory.Write_StrBytes("8B 7D 08");

            //Now EDI has the UAF_GameManager Instance address
            //mov [edi+0C], _PlayerNumber
            CaveMemory.Write_StrBytes("C7 47 0C");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(iLanguage));
            //mov [edi+14], _GameVersionType
            CaveMemory.Write_StrBytes("C7 47 14");
            CaveMemory.Write_Bytes(BitConverter.GetBytes((uint)_GameVersionType));
            //mov [edi+18], _GameSpecialVersionType
            CaveMemory.Write_StrBytes("C7 47 18");
            CaveMemory.Write_Bytes(BitConverter.GetBytes((uint)_GameSpecialVersionType));
            //mov [edi+1C], _PlayerNumber
            CaveMemory.Write_StrBytes("C7 47 1C");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(iPlayers));

            //Inject it
            CaveMemory.InjectToOffset(_UafGameManager_Start_InjectionStruct, "UAF_GameManager.Start()");
        }

        /// <summary>
        /// Timer string format is supposed to be 00.00 and the char "." is used to split
        /// On different language computer, format end up beeing 00,00 and the split fails and causes errors
        /// Changing the split Character from "," to "." to solve that
        /// </summary>
        private void SetHack_PatchDecimalSeparator()
        {
            SetHack_PatchDecimalSeparator_DsLevelSelect_Start(_DsLevelSelect_Start_DecimalPatch1_InjectionStruct);
            SetHack_PatchDecimalSeparator_DsLevelSelect_Start(_DsLevelSelect_Start_DecimalPatch2_InjectionStruct);
            SetHack_PatchDecimalSeparator_DsLevelSelect_Update();
            SetHack_PatchDecimalSeparator_FwMissionXXXUI_UpdateTimer(_FwMissionUI_UpdateTimer_DecimalPatch_InjectionStruct);
            SetHack_PatchDecimalSeparator_FwMissionXXXUI_UpdateTimer(_FwMissionTriggerUI_UpdateTimer_DecimalPatch_InjectionStruct);
        }
        //DS_LevelSelect.Start() : We can get the numberDecimalSeparator string from the previous received NumberFormatInfo result
        //And replace the char used for Split() with this one
        private void SetHack_PatchDecimalSeparator_DsLevelSelect_Start(InjectionStruct32 TargetInjection)
        {
            Codecave32 CaveMemory = new Codecave32(_Process, (UInt32)_GameAssemblyDll_BaseAddress);

            //mov ecx,[esp+10]
            CaveMemory.Write_StrBytes("8B 4C 24 10");
            //mov ecx,[ecx+1C]
            CaveMemory.Write_StrBytes("8B 49 1C");
            //mov ecx,[ecx+0C]
            CaveMemory.Write_StrBytes("8B 49 0C");

            //Inject it
            CaveMemory.InjectToOffset(TargetInjection, "DS_LevelSelect.Start()");
        }
        //DS_LevelSelect.Update() : We need to call System::Globalization::NumberFormatInfo::get_CurrentInfo() to retrieve the current separator
        //And replace the char used for Split() with this one
        private void SetHack_PatchDecimalSeparator_DsLevelSelect_Update()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, (UInt32)_GameAssemblyDll_BaseAddress);

            //push eax
            CaveMemory.Write_StrBytes("50");
            //push 00
            CaveMemory.Write_StrBytes("6A 00");
            //call GameAssembly.dll+4C3B90  [System::Globalization::NumberFormatInfo::get_CurrentInfo()]
            CaveMemory.Write_call((UInt32)_GameAssemblyDll_BaseAddress + _NumberFormatInfo_Func_Offset);
            //add esp,04
            CaveMemory.Write_StrBytes("83 C4 04");
            //mov ecx,[eax+1C]
            CaveMemory.Write_StrBytes("8B 48 1C");
            //pop eax
            CaveMemory.Write_StrBytes("58");
            //push [ecx+0C]
            CaveMemory.Write_StrBytes("FF 71 0C");
            //push 00
            CaveMemory.Write_StrBytes("6A 00");
            //mov ecx,eax
            CaveMemory.Write_StrBytes("8B C8");

            //Inject it
            CaveMemory.InjectToOffset(_DsLevelSelect_Update_DecimalPatch_InjectionStruct, "DS_LevelSelect.Update()");
        }
        //FW_MissionUI_UpdateTimer() and FW_MissionTriggerUI_UpdateTimer(): We can get the numberDecimalSeparator string from the previous received NumberFormatInfo result
        //And replace the char used for Split() with this one
        private void SetHack_PatchDecimalSeparator_FwMissionXXXUI_UpdateTimer(InjectionStruct32 TargetInjection)
        {
            Codecave32 CaveMemory = new Codecave32(_Process, (UInt32)_GameAssemblyDll_BaseAddress);

            //mov ecx,[edi+1C]
            CaveMemory.Write_StrBytes("8B 4F 1C");
            //mov ecx,[ecx+0C]
            CaveMemory.Write_StrBytes("8B 49 0C");

            //Inject it
            CaveMemory.InjectToOffset(TargetInjection, "FW_MissionUI_UpdateTimer() / FW_MissionTriggerUI_UpdateTimer()");
        }
        #endregion        

        #region Inputs Hack

        protected override void Apply_InputsMemoryHack()
        {
            Create_InputsDataBank();
            _AxisX_Array_CaveAddress = InputsDatabank_Address;
            _AxisY_Array_CaveAddress = InputsDatabank_Address + 0x10;
            _Buttons_CaveAddress = InputsDatabank_Address + 0x20;   //Replacing Input.GetKeyDown

            /// UAF_GameManager.start(): 'mov [eax+0x0C], 01' to force UAF_ControlManager.ControlType to Keyboard (or 0 for USB).
            WriteBytes((UInt32)_GameAssemblyDll_BaseAddress + _ForceKeyboard_Offset, new byte[] { 0xC7, 0x40, 0x0C, 0x01, 0x00, 0x00, 0x00 });

            /// <summary>
            /// Without the IO board, the game can try to give Tickets but has no return on that.
            /// So it will display an error message box with un-given tickets left.
            /// To patch that, blocking the Tickets counter to be given to 0
            /// </summary>
            WriteBytes((UInt32)_GameAssemblyDll_BaseAddress + _PatchTicketsOut_Offset, new byte[] { 0x31, 0xC9, 0x90 });    //xor ecx, ecx

            SetHack_EditUserControlKeys();
            SetHack_EditSystemControlKeys();
            SetHack_Axis();
            SetHack_CreateCustomKeyDown();           

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        /// <summary>
        /// UAF_Control.Init() will allow us to change Keys set in constructor
        /// Each player has it's own UAF_Control instance
        /// </summary>
        private void SetHack_EditUserControlKeys()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, (UInt32)_GameAssemblyDll_BaseAddress);

            //mov esi,[ebp+08]
            CaveMemory.Write_StrBytes("8B 75 08");
            //Now ESI has the UAF_Control Instance address
            //mov edx,[esi+08]
            CaveMemory.Write_StrBytes("8B 56 08");  //Getting player index
            //mov [esi+10],edx
            CaveMemory.Write_StrBytes("89 56 10");
            //add dword ptr [esi+10],35
            CaveMemory.Write_StrBytes("83 46 10 35");   //Each player COIN key will be 0x35(Alpha5) + PlayerIndex (Alpha6, etc...)

            //push ebx
            CaveMemory.Write_StrBytes("53");
            //push eax
            CaveMemory.Write_StrBytes("50");

            //Start buttons is in an Array, checking how many buttons are set and changing them all to 0x31(Alpha1) + PlayerIndex
            //mov ebx,[esi+18]
            CaveMemory.Write_StrBytes("8B 5E 18");
            //mov eax,[ebx+0C]
            CaveMemory.Write_StrBytes("8B 43 0C");
            //test eax,eax
            CaveMemory.Write_StrBytes("85 C0");
            //je skip
            CaveMemory.Write_StrBytes("74 0B");
            //add ebx,10
            CaveMemory.Write_StrBytes("83 C3 10");
            //mov [ebx],edx
            CaveMemory.Write_StrBytes("89 13");
            //add dword ptr [ebx],31
            CaveMemory.Write_StrBytes("83 03 31");
            //add ebx,04
            CaveMemory.Write_StrBytes("83 C3 04");
            //dec eax
            CaveMemory.Write_StrBytes("48");
            //jne again
            CaveMemory.Write_StrBytes("75 F5");

            //pop eax
            CaveMemory.Write_StrBytes("58");
            //pop ebx
            CaveMemory.Write_StrBytes("5B");

            //mov edx,[esi+14]
            CaveMemory.Write_StrBytes("8B 56 14");

            //Inject it
            CaveMemory.InjectToOffset(_UafControl_Init_InjectionStruct, "UAF_Control.Init()");
        }

        /// <summary>
        /// UAF_ControlManager.Start() will allow us to change Keys set in constructor
        /// BossButtonKey[] Array has FIX, SERVE and TEST buttons
        /// </summary>
        private void SetHack_EditSystemControlKeys()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, (UInt32)_GameAssemblyDll_BaseAddress);

            //mov eax,[edi+1C]
            CaveMemory.Write_StrBytes("8B 47 1C");
            //cmp dword ptr [eax+0C],03
            CaveMemory.Write_StrBytes("83 78 0C 03");
            //jne Next
            CaveMemory.Write_StrBytes("75 15");
            //mov [eax+10],00000112
            CaveMemory.Write_StrBytes("C7 40 10 12 01 00 00");
            //mov [eax+14],00000113
            CaveMemory.Write_StrBytes("C7 40 14 13 01 00 00");
            //mov [eax+18],00000114
            CaveMemory.Write_StrBytes("C7 40 18 14 01 00 00");
            //mov eax,[edi+10]
            CaveMemory.Write_StrBytes("8B 47 10");
            //add esp,08
            CaveMemory.Write_StrBytes("83 C4 08");

            //Inject it
            CaveMemory.InjectToOffset(_UafControlManager_Start_InjectionStruct, "UAF_ControlManager.Start()");
        }

        /// <summary>
        /// Inside UAF_ControllerManager.MoveGunController(), if CONTROL_TYPE is keyboard, the game waits for a KEY to be pressed to update Values
        /// Removing that part and inserting our own values, forcing a TRUE return (UAF_ControllerManager.flag = 1)
        /// </summary>
        private void SetHack_Axis()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, (UInt32)_GameAssemblyDll_BaseAddress);

            //mov byte ptr [esi+000001D4],01
            CaveMemory.Write_StrBytes("C6 86 D4 01 00 00 01");
            //mov eax,[ebp+1C]
            CaveMemory.Write_StrBytes("8B 45 1C");

            //mov esi,_AxisX_Array_CaveAddress
            CaveMemory.Write_StrBytes("BE");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_AxisX_Array_CaveAddress));
            //mov esi,[esi+ebx*4]
            CaveMemory.Write_StrBytes("8B 34 9E");
            //mov [eax],esi
            CaveMemory.Write_StrBytes("89 30");
            //mov esi,_AxisY_Array_CaveAddress
            CaveMemory.Write_StrBytes("BE");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_AxisY_Array_CaveAddress));
            //mov esi,[esi+ebx*4]
            CaveMemory.Write_StrBytes("8B 34 9E");
            //mov [eax+04],esi
            CaveMemory.Write_StrBytes("89 70 04");
            //mov eax,00000001
            CaveMemory.Write_StrBytes("B8 01 00 00 00");
            //pop edi
            CaveMemory.Write_StrBytes("5F");
            //pop esi
            CaveMemory.Write_StrBytes("5E");
            //pop ebx
            CaveMemory.Write_StrBytes("5B");
            //mov esp,ebp
            CaveMemory.Write_StrBytes("8B E5");
            //pop ebp
            CaveMemory.Write_StrBytes("5D");
            //ret
            CaveMemory.Write_StrBytes("C3");

            //Inject it
            CaveMemory.InjectToOffset(_UAfControlManager_MoveGunController_InjectionStruct, "UAF_ControlManager.MovegunController()");
        }

        /// <summary>
        /// Game is calling Input.GetKeyStateInt() to get button pulses
        /// Function Ptr address is replaced by this custom function, returning result based on a custom input table
        /// </summary>
        private void SetHack_CreateCustomKeyDown()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, (UInt32)_GameAssemblyDll_BaseAddress);

            //mov eax,_Buttons_CaveAddress
            CaveMemory.Write_StrBytes("B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Buttons_CaveAddress));
            //add eax,[esp+04]
            CaveMemory.Write_StrBytes("03 44 24 04");
            //cmp byte ptr [eax],01
            CaveMemory.Write_StrBytes("80 38 01");
            //jne Down
            CaveMemory.Write_StrBytes("75 0A");
            //mov byte ptr [eax],00
            CaveMemory.Write_StrBytes("C6 00 00");
            //mov eax,00000001
            CaveMemory.Write_StrBytes("B8 01 00 00 00");
            //jmp Exit
            CaveMemory.Write_StrBytes("EB 02");

            //Down:
            //xor eax,eax
            CaveMemory.Write_StrBytes("31 C0");

            //Exit:
            //ret
            CaveMemory.Write_StrBytes("C3");

            WriteBytes((UInt32)_GameAssemblyDll_BaseAddress + _Input_GetKeyDownInt_Ptr_Offset, BitConverter.GetBytes(CaveMemory.CaveAddress));
        }

        #endregion

        #region Outputs Hack

        protected override void Apply_OutputsMemoryHack()
        {
            Create_OutputsDataBank();
            _Light_Array_CaveAddress = OutputsDatabank_Address;
            _Motor_Array_CaveAddress = OutputsDatabank_Address + 0x10;
            _DamageArray_CaveAddress = OutputsDatabank_Address + 0x20;
            _UafGameManager_Instance_CaveAddress = OutputsDatabank_Address + 0x30;
            _WaterMistState_CaveAddress = OutputsDatabank_Address + 0x40;
            _UafControlManager_Instance_CaveAddress = OutputsDatabank_Address + 0x44;

            SetHack_GetGameManagerInstance();
            SetHack_ControlManagerInstance();
            SetHack_LightsOn();
            SetHack_LightsOff();
            SetHack_SeatOn();
            SetHack_SeatOff();
            SetHack_GunShakeOn();
            SetHack_GunShakeOff();
            SetHack_Damage();
            SetHack_MistState();

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        /// <summary>
        /// Hooking to UAF_GameManager.Update() will allow us to get the Instance pointer of the Singleton
        /// Used later to get coins, player status, etc...
        /// </summary>
        private void SetHack_GetGameManagerInstance()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, (UInt32)_GameAssemblyDll_BaseAddress);

            //mov edi,[ebp+08]
            CaveMemory.Write_StrBytes("8B 7D 08");
            //add esp,04
            CaveMemory.Write_StrBytes("83 C4 04");
            //mov [_UafGameManager_Instance_CaveAddress],edi
            CaveMemory.Write_StrBytes("89 3D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_UafGameManager_Instance_CaveAddress));

            //Inject it
            CaveMemory.InjectToOffset(_UafGameManager_Update_InjectionStruct, "UAF_GameManager.Update()");
        }

        /// <summary>
        /// Intercepting calls to function, where we can get Player ID, Lamp Id and Lamp State (1 = ON, 2 = Blink)
        /// </summary>
        private void SetHack_LightsOn()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, (UInt32)_GameAssemblyDll_BaseAddress);

            //mov eax,[eax+08]
            CaveMemory.Write_StrBytes("8B 40 08");
            //push eax
            CaveMemory.Write_StrBytes("50");
            //mov esi,[ebp+0C]
            CaveMemory.Write_StrBytes("8B 75 0C");
            //shl esi,02
            CaveMemory.Write_StrBytes("C1 E6 02");
            //add esi,[ebp+10]
            CaveMemory.Write_StrBytes("03 75 10");
            //add esi,_Light_Array_CaveAddress
            CaveMemory.Write_StrBytes("81 C6");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Light_Array_CaveAddress));
            //mov eax,[ebp+14]
            CaveMemory.Write_StrBytes("8B 45 14");
            //mov [esi],al
            CaveMemory.Write_StrBytes("88 06");
            //pop eax
            CaveMemory.Write_StrBytes("58");
            //mov esi,[ebp+10]
            CaveMemory.Write_StrBytes("8B 75 10");

            //Inject it
            CaveMemory.InjectToOffset(_UafControlManager_OpenLight_InjectionStruct, "UAF_ControlManager.OpenLight()");
        }
        private void SetHack_LightsOff()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, (UInt32)_GameAssemblyDll_BaseAddress);

            //mov eax,[eax+08]
            CaveMemory.Write_StrBytes("8B 40 08");
            //push eax
            CaveMemory.Write_StrBytes("50");
            //mov edi,[ebp+0C]
            CaveMemory.Write_StrBytes("8B 7D 0C");
            //shl edi,02
            CaveMemory.Write_StrBytes("C1 E7 02");
            //add edi,[ebp+10]
            CaveMemory.Write_StrBytes("03 7D 10");
            //add edi,_Light_Array_CaveAddress
            CaveMemory.Write_StrBytes("81 C7");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Light_Array_CaveAddress));
            //xor eax, eax
            CaveMemory.Write_StrBytes("31 C0");
            //mov [edi],al
            CaveMemory.Write_StrBytes("88 07");
            //pop eax
            CaveMemory.Write_StrBytes("58");
            //mov edi,[ebp+10]
            CaveMemory.Write_StrBytes("8B 7D 10");

            //Inject it
            CaveMemory.InjectToOffset(_UafControlManager_CloseLight_InjectionStruct, "UAF_ControlManager.CloseLight()");
        }

        /// <summary>
        /// Intercepting calls to function, where we can get Player ID
        /// </summary>
        private void SetHack_SeatOn()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, (UInt32)_GameAssemblyDll_BaseAddress);

            //mov eax,[ebp+0C]
            CaveMemory.Write_StrBytes("8B 45 0C");
            //add eax,_Motor_Array_CaveAddress
            CaveMemory.Write_StrBytes("05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Motor_Array_CaveAddress));
            //mov byte ptr[eax],1
            CaveMemory.Write_StrBytes("C6 00 01");
            //mov eax,[edi+0000020C]
            CaveMemory.Write_StrBytes("8B 87 0C 02 00 00");

            //Inject it
            CaveMemory.InjectToOffset(_UafControlManager_StartSeatShake_InjectionStruct, "UAF_ControlManager.StartSeatShake()");
        }
        private void SetHack_SeatOff()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, (UInt32)_GameAssemblyDll_BaseAddress);

            //mov eax,[ebp+0C]
            CaveMemory.Write_StrBytes("8B 45 0C");
            //add eax,_Motor_Array_CaveAddress
            CaveMemory.Write_StrBytes("05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Motor_Array_CaveAddress));
            //mov byte ptr[eax],0
            CaveMemory.Write_StrBytes("C6 00 00");
            //mov eax,[edi+0000020C]
            CaveMemory.Write_StrBytes("8B 87 0C 02 00 00");

            //Inject it
            CaveMemory.InjectToOffset(_UafControlManager_CloseSeatShake_InjectionStruct, "UAF_ControlManager.CloseSeatShake()");
        }

        /// <summary>
        /// Intercepting calls to function, where we can get Player ID
        /// </summary>
        private void SetHack_GunShakeOn()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, (UInt32)_GameAssemblyDll_BaseAddress);

            //mov eax,[ebp+0C]
            CaveMemory.Write_StrBytes("8B 45 0C");
            //add eax,_Motor_Array_CaveAddress
            CaveMemory.Write_StrBytes("05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Motor_Array_CaveAddress + 4));
            //mov byte ptr[eax],1
            CaveMemory.Write_StrBytes("C6 00 01");
            //mov eax,[edi+00000208]
            CaveMemory.Write_StrBytes("8B 87 08 02 00 00");

            //Inject it
            CaveMemory.InjectToOffset(_UafControlManager_StartShakeApparatusOut_InjectionStruct, "UAF_ControlManager.StartShakeApparatusOut()");
        }
        private void SetHack_GunShakeOff()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, (UInt32)_GameAssemblyDll_BaseAddress);

            //mov eax,[ebp+0C]
            CaveMemory.Write_StrBytes("8B 45 0C");
            //add eax,_Motor_Array_CaveAddress
            CaveMemory.Write_StrBytes("05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Motor_Array_CaveAddress + 4));
            //mov byte ptr[eax],0
            CaveMemory.Write_StrBytes("C6 00 00");
            //mov eax,[edi+00000208]
            CaveMemory.Write_StrBytes("8B 87 08 02 00 00");

            //Inject it
            CaveMemory.InjectToOffset(_UafControlManager_CloseShake_InjectionStruct, "UAF_ControlManager.CloseShake()");
        }

        /// <summary>
        /// Intercepting calls to function, where we can get Player ID
        /// </summary>
        private void SetHack_Damage()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, (UInt32)_GameAssemblyDll_BaseAddress);

            //and esp, -10
            CaveMemory.Write_StrBytes("83 E4 F0");
            //sub esp,28
            CaveMemory.Write_StrBytes("83 EC 28");
            //mov ecx,[ebp+10]
            CaveMemory.Write_StrBytes("8B 4D 10");
            //cmp ecx, -1
            CaveMemory.Write_StrBytes("83 F9 FF");
            //jne Next
            CaveMemory.Write_StrBytes("75 0D");
            //mov ecx,_DamageArray_CaveAddress
            CaveMemory.Write_StrBytes("B9");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_DamageArray_CaveAddress));
            //mov [ecx],01010101
            CaveMemory.Write_StrBytes("C7 01 01 01 01 01");
            //jmp Exit
            CaveMemory.Write_StrBytes("EB 09");
            //add ecx,_DamageArray_CaveAddress
            CaveMemory.Write_StrBytes("81 C1");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_DamageArray_CaveAddress));
            //mov byte ptr[ecx],1
            CaveMemory.Write_StrBytes("C6 01 01");

            //Inject it
            CaveMemory.InjectToOffset(_SgPlayer_PlayEffectHitted_InjectionStruct, "SG_Player.PlayEffectHitted()");
        }

        /// <summary>
        /// Intercepting call to change the state
        /// </summary>
        private void SetHack_MistState()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, (UInt32)_GameAssemblyDll_BaseAddress);

            //and esp, -10
            CaveMemory.Write_StrBytes("83 E4 F0");
            //sub esp,38
            CaveMemory.Write_StrBytes("83 EC 38");
            //mov ecx,[ebp+0C]
            CaveMemory.Write_StrBytes("8B 4D 0C");
            //mov [_WaterMistState_CaveAddress], ecx
            CaveMemory.Write_StrBytes("89 0D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_WaterMistState_CaveAddress));

            //Inject it
            CaveMemory.InjectToOffset(_UafControlManager_WaterMistControl_InjectionStruct, "UAF_ControlManager.WaterMistState()");
        }

        /// <summary>
        /// Hooking to UAF_ControlManager.Update() will allow us to get the Instance pointer of the Singleton
        /// Used later to get water status for guns
        private void SetHack_ControlManagerInstance()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, (UInt32)_GameAssemblyDll_BaseAddress);

            //mov esi,[ebp+08]
            CaveMemory.Write_StrBytes("8B 75 08");
            //xor edx,edx
            //mov [_UafControlManager_Instance_CaveAddress],esi
            CaveMemory.Write_StrBytes("89 35");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_UafControlManager_Instance_CaveAddress));

            //Inject it
            CaveMemory.InjectToOffset(_UafControlManager_Update_InjectionStruct, "UAF_ControlManager.Update()");
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
            else if (e.ButtonName.Equals("TEST"))
            {
                WriteByte(_Buttons_CaveAddress + 0x112, 0x01);
            }
            else if (e.ButtonName.Equals("SERVE"))
            {
                WriteByte(_Buttons_CaveAddress + 0x113, 0x01);
            }
            else if (e.ButtonName.Equals("FIX"))
            {
                WriteByte(_Buttons_CaveAddress + 0x114, 0x01);
            }
        }

        protected override void GameController_OnButtonUp(object sender, VirtualGameControler.ButtonDownEventArgs e)
        {
            if (e.ButtonName.Equals("TEST"))
            {
                if (e.ButtonName.Equals("TEST"))
                {
                    WriteByte(_Buttons_CaveAddress + 0x112, 0x00);
                }
                else if (e.ButtonName.Equals("SERVE"))
                {
                    WriteByte(_Buttons_CaveAddress + 0x113, 0x00);
                }
                else if (e.ButtonName.Equals("FIX"))
                {
                    WriteByte(_Buttons_CaveAddress + 0x114, 0x00);
                }
            }
        }

        protected override void UpdateAxis(PlayerID Id, int X, int Y)
        {
            if (Id != PlayerID.None)
            {
                byte[] bufferX = BitConverter.GetBytes((float)X);
                byte[] bufferY = BitConverter.GetBytes((float)Y);

                WriteBytes(_AxisX_Array_CaveAddress + ((UInt32)Id * 4), bufferX);
                WriteBytes(_AxisY_Array_CaveAddress + ((UInt32)Id * 4), bufferY);
            }
        }

        protected override void PlayerController_OnButtonDown(object sender, VirtualGameControler.ButtonDownEventArgs e)
        {
            if (e.Id != PlayerID.None)
            {
                UInt32 PlayerIndex = (UInt32)e.Id;

                if (e.ButtonName.Equals("COIN"))
                {
                    WriteByte(_Buttons_CaveAddress + 0x35 + PlayerIndex, 0x01);
                }
                else if (e.ButtonName.Equals("START"))
                {
                    WriteByte(_Buttons_CaveAddress + 0x31 + PlayerIndex, 0x01);
                }
            }
        }
        protected override void PlayerController_OnButtonUp(object sender, VirtualGameControler.ButtonDownEventArgs e)
        {
            if (e.Id != PlayerID.None)
            {
                UInt32 PlayerIndex = (UInt32)e.Id;

                if (e.ButtonName.Equals("COIN"))
                {
                    WriteByte(_Buttons_CaveAddress + 0x35 + PlayerIndex, 0x00);
                }
                else if (e.ButtonName.Equals("START"))
                {
                    WriteByte(_Buttons_CaveAddress + 0x31 + PlayerIndex, 0x00);
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
                new GameOutput(OutputId.P1_WaterFire),
                new GameOutput(OutputId.P2_WaterFire),
                new GameOutput(OutputId.P3_WaterFire),
                new GameOutput(OutputId.P4_WaterFire),
                new GameOutput(OutputId.P1_BigGun),
                new GameOutput(OutputId.P2_BigGun),
                new GameOutput(OutputId.P3_BigGun),
                new GameOutput(OutputId.P4_BigGun),
                new GameOutput(OutputId.SmokeSwitch),
                new SyncBlinkingGameOutput(OutputId.P1_LmpStart, 500),
                new SyncBlinkingGameOutput(OutputId.P2_LmpStart, 500),
                new SyncBlinkingGameOutput(OutputId.P3_LmpStart, 500),
                new SyncBlinkingGameOutput(OutputId.P4_LmpStart, 500),
                new BlinkGameOutput(OutputId.P1_LmpGun, 500),
                new BlinkGameOutput(OutputId.P2_LmpGun, 500),
                new BlinkGameOutput(OutputId.P3_LmpGun, 500),
                new BlinkGameOutput(OutputId.P4_LmpGun, 500),
                new GameOutput(OutputId.P1_ChairShake),
                new GameOutput(OutputId.P2_ChairShake),
                new GameOutput(OutputId.P3_ChairShake),
                new GameOutput(OutputId.P4_ChairShake),
                new GameOutput(OutputId.P1_GunMotor),
                new GameOutput(OutputId.P2_GunMotor),
                new GameOutput(OutputId.P3_GunMotor),
                new GameOutput(OutputId.P4_GunMotor),
                new AsyncGameOutput(OutputId.P1_Damaged, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_DAMAGED_DELAY), 100, 0),
                new AsyncGameOutput(OutputId.P2_Damaged, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_DAMAGED_DELAY), 100, 0),
                new AsyncGameOutput(OutputId.P3_Damaged, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_DAMAGED_DELAY), 100, 0),
                new AsyncGameOutput(OutputId.P4_Damaged, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_DAMAGED_DELAY), 100, 0),
                new GameOutput(OutputId.P1_Credits),
                new GameOutput(OutputId.P2_Credits),
                new GameOutput(OutputId.P3_Credits),
                new GameOutput(OutputId.P4_Credits)
            };
        }

        /// <summary>
        /// Update all Outputs values before sending them to MameHooker
        /// </summary>
        public override void UpdateOutputValues()
        {
            int[] Coins = new int[4];
            byte[] IsGaming = new byte[4];
            UInt32 pGameManager = ReadPtr(_UafGameManager_Instance_CaveAddress);
            if (pGameManager != 0)
            {
                UInt32 pCoins = ReadPtr(pGameManager + 0x40);
                if (pCoins != 0)
                {
                    int ArrayLength = (int)ReadPtr(pCoins + 0x0C);
                    for (int i = 0; i < ArrayLength; i++)
                    {
                        Coins[i] = (int)ReadPtr((UInt32)(pCoins + 0x10 + 4 * i));
                    }
                }

                UInt32 pIsGaming = ReadPtr(pGameManager + 0x5C);
                if (pIsGaming != 0)
                {
                    int ArrayLength = (int)ReadPtr(pIsGaming + 0x0C);
                    for (int i = 0; i < ArrayLength; i++)
                    {
                        IsGaming[i] = ReadByte((UInt32)(pIsGaming + 0x10 + i));
                    }
                }
            }

            int[] SmallWater = new int[4];
            int[] BigWater = new int[4];
            UInt32 pControlManager = ReadPtr(_UafControlManager_Instance_CaveAddress);
            if (pControlManager != 0)
            {
                UInt32 pWaterPump = ReadPtr(pControlManager + 0x1E8);
                if (pWaterPump != 0)
                {
                    int ArrayLength = (int)ReadPtrChain(pWaterPump + 0x08, new UInt32[] { 0x00 });
                    int ItemLength = (int)ReadPtrChain(pWaterPump + 0x08, new UInt32[] { 0x08 });
                    for (int i = 0; i < ArrayLength; i++)
                    {
                        BigWater[i] = (int)ReadByte((UInt32)(pWaterPump + 0x10 + ItemLength * i));
                        SmallWater[i] = (int)ReadByte((UInt32)(pWaterPump + 0x10 + ItemLength * i + 1));
                    }
                }
            }

            SetOutputValue(OutputId.P1_WaterFire, SmallWater[0]);
            SetOutputValue(OutputId.P2_WaterFire, SmallWater[1]);
            SetOutputValue(OutputId.P3_WaterFire, SmallWater[2]);
            SetOutputValue(OutputId.P4_WaterFire, SmallWater[3]);

            SetOutputValue(OutputId.P1_BigGun, BigWater[0]);
            SetOutputValue(OutputId.P2_BigGun, BigWater[1]);
            SetOutputValue(OutputId.P3_BigGun, BigWater[2]);
            SetOutputValue(OutputId.P4_BigGun, BigWater[3]);

            SetOutputValue(OutputId.SmokeSwitch, ReadByte(_WaterMistState_CaveAddress));

            SetOutputValue(OutputId.P1_LmpStart, GetBlinkingLightState(ReadByte(_Light_Array_CaveAddress + 1)));
            SetOutputValue(OutputId.P1_LmpGun, ReadByte(_Light_Array_CaveAddress + 2));
            SetOutputValue(OutputId.P2_LmpStart, GetBlinkingLightState(ReadByte(_Light_Array_CaveAddress + 5)));
            SetOutputValue(OutputId.P2_LmpGun, ReadByte(_Light_Array_CaveAddress + 6));
            SetOutputValue(OutputId.P3_LmpStart, GetBlinkingLightState(ReadByte(_Light_Array_CaveAddress + 9)));
            SetOutputValue(OutputId.P3_LmpGun, ReadByte(_Light_Array_CaveAddress + 10));
            SetOutputValue(OutputId.P4_LmpStart, GetBlinkingLightState(ReadByte(_Light_Array_CaveAddress + 13)));
            SetOutputValue(OutputId.P4_LmpGun, ReadByte(_Light_Array_CaveAddress + 14));

            SetOutputValue(OutputId.P1_ChairShake, ReadByte(_Motor_Array_CaveAddress));
            SetOutputValue(OutputId.P2_ChairShake, ReadByte(_Motor_Array_CaveAddress + 1));
            SetOutputValue(OutputId.P3_ChairShake, ReadByte(_Motor_Array_CaveAddress + 2));
            SetOutputValue(OutputId.P4_ChairShake, ReadByte(_Motor_Array_CaveAddress + 3));
            SetOutputValue(OutputId.P1_GunMotor, ReadByte(_Motor_Array_CaveAddress + 4));
            SetOutputValue(OutputId.P2_GunMotor, ReadByte(_Motor_Array_CaveAddress + 5));
            SetOutputValue(OutputId.P3_GunMotor, ReadByte(_Motor_Array_CaveAddress + 6));
            SetOutputValue(OutputId.P4_GunMotor, ReadByte(_Motor_Array_CaveAddress + 7));

            //When the game "Hits" some player, ID can be -1 (= All Players ?)
            //In that case the codeCave will set the Damage flags to all players
            //And will will filter here with the player Status to only enable the Damage output for playing players
            if (ReadByte(_DamageArray_CaveAddress) == 1 && IsGaming[0] == 1)
            {
                SetOutputValue(OutputId.P1_Damaged, 1);
                WriteByte(_DamageArray_CaveAddress, 0x00);
            }
            if (ReadByte(_DamageArray_CaveAddress + 1) == 1 && IsGaming[1] == 1)
            {
                SetOutputValue(OutputId.P2_Damaged, 1);
                WriteByte(_DamageArray_CaveAddress + 1, 0x00);
            }
            if (ReadByte(_DamageArray_CaveAddress + 2) == 1 && IsGaming[2] == 1)
            {
                SetOutputValue(OutputId.P3_Damaged, 1);
                WriteByte(_DamageArray_CaveAddress + 2, 0x00);
            }
            if (ReadByte(_DamageArray_CaveAddress + 3) == 1 && IsGaming[3] == 1)
            {
                SetOutputValue(OutputId.P4_Damaged, 1);
                WriteByte(_DamageArray_CaveAddress + 3, 0x00);
            }

            SetOutputValue(OutputId.P1_Credits, Coins[0]);
            SetOutputValue(OutputId.P2_Credits, Coins[1]);
            SetOutputValue(OutputId.P3_Credits, Coins[2]);
            SetOutputValue(OutputId.P4_Credits, Coins[3]);

        }
        //Game sets values as this :
        //0 = OFF
        //1 = CONSTANT ON
        //2 = CONSTANT OFF
        //We need -1 instead of 2 for custom blinking output state
        private int GetBlinkingLightState(byte ReadValue)
        {
            if (ReadValue == 2)
                return -1;
            else
                return ReadValue;
        }


        #endregion
    }
}

