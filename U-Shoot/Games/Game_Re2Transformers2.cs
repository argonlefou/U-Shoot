using System;
using System.Collections.Generic;
using System.Reflection;
using UsCore.Config;
using UsCore.Launcher;
using UsCore.Logging;
using UsCore.MameOutput;
using UsCore.Memory;
using UsCore.VirtualControllers;

namespace U_Shoot.Games
{
    public class Game_Re2Transformers2  :BaseGame32
    {
        protected override string GAMEDATA_FOLDER { get { return @"XXXXX\YYYYY"; } }
        protected override bool PATCH_PEB_DEBUG_FLAG { get { return true; } }

        #region LOADER MEMORY ADDRESSES                
        private UInt32 _DisableCShell_Offset = 0x001E70E4;
        private UInt32 _RestoreHiddenConfig_Offset = 0x00240C80;
        private UInt32 _SetWindowedMode_Offset = 0x002409F5;
        private UInt32 _RemoveOriginalWindowedMode_Offset = 0x00240A1C;
        private UInt32 _ForceResolutionIndex_Offset = 0x00260AD5;
        private UInt32 _ResolutionTableHd_Offset = 0x008A5008;
        private UInt32 _DisableLEDBoardCreation_Offset = 0x002EB93C;
        private UInt32 _DisableSAEBoardCreation_Offset = 0x0017A4A2;
        private UInt32 _CGunMgrForceInputMouse_Offset1 = 0x000F54F9;
        private UInt32 _CGunMgrForceInputMouse_Offset2 = 0x000F551C;
        private UInt32 _CGunMgrForceInputMouse_Offset3 = 0x000F5534;
        private UInt32 _RelativePathFix_Ocean_Offset = 0x0010B018;
        private UInt32 _RelativePathFix_InfiniteOcean_Offset = 0x0010C0B2;
        private UInt32 _RelativePathFix_SmokeRenderer_Offset = 0x001EDC88;
        private UInt32 _RelativePathFix_TrackRenderer_Offset = 0x002174E2;
        private UInt32 _RelativePathFix_WaterDistort_Offset = 0x00111D01;
        //No Crosshair
        private UInt32 _PlMgr_IsHoldMode_FunctionOffset = 0x0024DDE0;
        private UInt32 _LaserSight_Patch_Offset = 0x002E558A;
        private UInt32 _GmMgr_GetGameMode_Function_Offset = 0x000E43C0;
        private UInt32 _GetResolution_Function_Offset = 0x002608B0;
        private InjectionStruct32 _NoCrosshair_InjectionStruct = new InjectionStruct32(0x000F6DB0, 5);
        #endregion

        #region INPUTS MEMORY ADDRESSES    
        private UInt32 _PlayerInputMode_Patch_Offset = 0x000F751A;
        private UInt32 _CreditMgr_IncCoin_Function_Offset = 0x00066DE0;
        private UInt32 _FrameRateMgr_ExecServer_Function_Offset = 0x00D70D0;
        private InjectionStruct32 _Axis_InjectionStruct = new InjectionStruct32(0x000F75CD, 6);
        private InjectionStruct32 _Buttons_InjectionStruct = new InjectionStruct32(0x000F787F, 5);
        private InjectionStruct32 _Credits_InjectionStruct = new InjectionStruct32(0x00240BCB, 5);
        private InjectionStruct32 _FixCrosshair_InjectionStruct = new InjectionStruct32(0x000F6DB5, 7);
        private InjectionStruct32 _FixEnnemyTarget_InjectionStruct = new InjectionStruct32(0x004EEF9D, 5);

        //Custom Input Address
        private UInt32 _P1_StartOn_Address;
        private UInt32 _P1_StartPress_Address;
        private UInt32 _P2_StartOn_Address;
        private UInt32 _P2_StartPress_Address;
        private UInt32 _P1_TriggerOn_Address;
        private UInt32 _P1_TriggerPress_Address;
        private UInt32 _P2_TriggerOn_Address;
        private UInt32 _P2_TriggerPress_Address;
        private UInt32 _LeverFrontOn_Address;
        private UInt32 _LeverFrontPress_Address;
        private UInt32 _LeverBackOn_Address;
        private UInt32 _LeverBackPress_Address;
        private UInt32 _P1_X_Address;
        private UInt32 _P1_Y_Address;
        private UInt32 _P2_X_Address;
        private UInt32 _P2_Y_Address;
        private UInt32 _AddCredit_Address;
        #endregion

        #region OUTPUTS MEMORY ADDRESSES
        private UInt32 _pPlayerManager_Address = 0x200F7CD8;
        private UInt32 _pCreditsManager_Address = 0x200E9750;
        private UInt32 _CPlayer1_Offset = 0x34;
        private UInt32 _CPlayer2_Offset = 0x38;
        private UInt32 _Cplayer_ID_Offset = 0x60;
        private UInt32 _Cplayer_Mode_Offset = 0x64;
        private UInt32 _Cplayer_Life_Offset = 0x68;
        private InjectionStruct32 _StartLamps_InjectionStruct = new InjectionStruct32(0x00067159, 5);
        private InjectionStruct32 _GunLamps_InjectionStruct = new InjectionStruct32(0x000FC124, 5);
        private InjectionStruct32 _Recoil_InjectionStruct = new InjectionStruct32(0x0024AAFF, 6);
        private InjectionStruct32 _Damage_InjectionStruct = new InjectionStruct32(0x00246C69, 6);

        //Custom Outputs Address
        private UInt32 _P1_LmpStart_Address;
        private UInt32 _P2_LmpStart_Address;
        private UInt32 _P1_LmpGun_Address;
        private UInt32 _P2_LmpGun_Address;
        private UInt32 _P1_Recoil_Address;
        private UInt32 _P2_Recoil_Address;
        private UInt32 _P1_Damage_Address;
        private UInt32 _P2_Damage_Address;
        #endregion

        public Game_Re2Transformers2(GameConfig Config) : base(Config)
        {
            _KnownMd5Prints.Add("Transformers Shadow Rising v180605 - Original Dump", "b3b1f4ad6408d6ee946761a00f761455");
        }

        #region Debugger Events

        #region EXCEPTION_DEBUG_EVENT

        protected override void ExceptionEvent(object sender, x86_x64_Debugger.ExceptionDebugEventArgs e)
        {
            //Using Generic Exception event Handling to set Input/Outputs Hacks
            base.ExceptionEvent(sender, e);
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
            RaiseGameHookedEvent();
        }

        #region Loader Hack
        protected override void Apply_LoaderMemoryHack()
        {
            //Disabling the CShell necessity, if not the game is shutting down if no correct Keep-alive reply from the Shell.exe program
            WriteByte(GameProcess_ModuleBaseAddress + _DisableCShell_Offset, 0x00);

            //GetConfigIniDir() function is stripped. Putting back a fixed name (./App.ini) to restore the possibility to change some settings
            WriteBytes(GameProcess_ModuleBaseAddress + _RestoreHiddenConfig_Offset, new byte[] { 0xB8, 0x86, 0x0C, 0x64, 0x00, 0xC3, 0x2E, 0x2F, 0x41, 0x70, 0x70, 0x2E, 0x69, 0x6E, 0x69, 0x00 });

            //LED Board is connected through COM port
            //Setting COM access result to (-1) instead of trying to open COM port for real
            WriteBytes(GameProcess_ModuleBaseAddress + _DisableLEDBoardCreation_Offset, new byte[] { 0xB8, 0xFF, 0xFF, 0xFF, 0xFF, 0xEB, 0x1D, 0x90, 0x90, 0x90, 0x90 });

            //Force not to create SAEBoard
            WriteByte(GameProcess_ModuleBaseAddress + _DisableSAEBoardCreation_Offset, 0xEB);

            //CGunMgr() init input mode by checking first if JVS is enabled, forcing it to false
            WriteByte(GameProcess_ModuleBaseAddress + _CGunMgrForceInputMouse_Offset1, 0xEB);
            //Then checking if SAEBoard is active, forcing it to false also
            WriteByte(GameProcess_ModuleBaseAddress + _CGunMgrForceInputMouse_Offset2, 0xEB);
            //Finally forcing input mode to '1' (mouse) for P2 (already 1 for P1 normally)
            WriteByte(GameProcess_ModuleBaseAddress + _CGunMgrForceInputMouse_Offset3, 0x1);

            //Replacing the original default value for Windowed mode by our own 
            WriteByte(GameProcess_ModuleBaseAddress + _SetWindowedMode_Offset, _GameConfig.Fullscreen ? (byte)0x00 : (byte)0x01);
            //Disabling the default Windowed mode set after reading the "hidden" config file
            WriteBytes(GameProcess_ModuleBaseAddress + _RemoveOriginalWindowedMode_Offset, new byte[] { 0x90, 0x90, 0x90, 0x90, 0x90, 0x90 });

            //Screen Size Hack :
            //First step is to force the game to choose index 0xF in the resolution table, whatever SWITCH or option is used
            WriteBytes(GameProcess_ModuleBaseAddress + _ForceResolutionIndex_Offset, new byte[] { 0x90, 0x90, 0xB8, 0x0F });
            //Second step is to replace Width and Height values for the desires Resolution by our own
            int Width = _GameConfig.ResolutionWidth != 0 ? _GameConfig.ResolutionWidth : 1920;
            int Height = _GameConfig.ResolutionHeight != 0 ? _GameConfig.ResolutionHeight : 1080;
            for (uint i = 0; i < 3; i++)
            {
                WriteBytes(GameProcess_ModuleBaseAddress + _ResolutionTableHd_Offset + (i * 8), BitConverter.GetBytes(Width));
                WriteBytes(GameProcess_ModuleBaseAddress + _ResolutionTableHd_Offset + (i * 8) + 4, BitConverter.GetBytes(Height));
            }

            // Not needed
            //Credits force value to 0 instead of -1 (-1 won't update value)    ?????? to confirm when set_credit hack is done
            //WriteByte(GameProcess_ModuleBaseAddress + 0x66953, 0x00);

            //When trying to load shadders from disks, game sometimes have issues with Absolute path name (based on the current dir) with special characters or length(?)
            //Forcing it to load relative path name may fix the issue
            WriteBytes(GameProcess_ModuleBaseAddress + _RelativePathFix_Ocean_Offset, new byte[] { 0x90, 0x90 });
            WriteBytes(GameProcess_ModuleBaseAddress + _RelativePathFix_InfiniteOcean_Offset, new byte[] { 0x90, 0x90 });
            WriteBytes(GameProcess_ModuleBaseAddress + _RelativePathFix_SmokeRenderer_Offset, new byte[] { 0x90, 0x90 });
            WriteBytes(GameProcess_ModuleBaseAddress + _RelativePathFix_TrackRenderer_Offset, new byte[] { 0x90, 0x90 });
            WriteBytes(GameProcess_ModuleBaseAddress + _RelativePathFix_WaterDistort_Offset, new byte[] { 0x90, 0x90 });

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }
        #endregion        

        #region Inputs Hack

        protected override void Apply_InputsMemoryHack()
        {
            // This will be used to store custom input values
            // Games has 2 distinct requets for button : "press" and "on"
            Create_InputsDataBank();
            _P1_StartOn_Address = InputsDatabank_Address;
            _P2_StartOn_Address = InputsDatabank_Address + 0x04;
            _P1_StartPress_Address = InputsDatabank_Address + 0x08;
            _P2_StartPress_Address = InputsDatabank_Address + 0x0C;
            _P1_TriggerOn_Address = InputsDatabank_Address + 0x10;
            _P2_TriggerOn_Address = InputsDatabank_Address + 0x14;
            _P1_TriggerPress_Address = InputsDatabank_Address + 0x18;
            _P2_TriggerPress_Address = InputsDatabank_Address + 0x1C;
            _LeverFrontOn_Address = InputsDatabank_Address + 0x20;
            _LeverFrontPress_Address = InputsDatabank_Address + 0x24;
            _LeverBackOn_Address = InputsDatabank_Address + 0x28;
            _LeverBackPress_Address = InputsDatabank_Address + 0x2C;
            _P1_X_Address = InputsDatabank_Address + 0x30;
            _P1_Y_Address = InputsDatabank_Address + 0x34;
            _P2_X_Address = InputsDatabank_Address + 0x38;
            _P2_Y_Address = InputsDatabank_Address + 0x3C;
            _AddCredit_Address = InputsDatabank_Address + 0x40;

            SetHack_Axis();
            SetHack_Buttons();
            SetHack_Credits();
            SetHack_CorrectReticlePosition();
            SetHack_CorrectEnnemyTarget();

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        /// <summary>
        ///Hacking Axis proc : 
        ///In CGunMgr::MainProc()
        ///Input type beeing force to "Mouse", the hack will separate P1/P2 proc (EBX value) and put values in ShotX and ShotX members of the INPUT struct
        ///At the end : ESI+4 receives X, and ESI+5 for Y (one byte each)
        /// </summary>>
        private void SetHack_Axis()
        {
            //Forcing The InputMode to "1" (mouse) in the switch check in CGunMgr::MainProc loop
            WriteBytes(GameProcess_ModuleBaseAddress + _PlayerInputMode_Patch_Offset, new byte[] { 0xB8, 0x01, 0x00, 0x00, 0x00, 0x90, 0x90, 0x90, 0x90 });

            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //sar eax, 8
            CaveMemory.Write_StrBytes("C1 F8 08");
            //test ebx,ebx
            CaveMemory.Write_StrBytes("85 DB");
            //jne Player2
            CaveMemory.Write_StrBytes("75 14");
            //mov eax,[_P1_X_Address]
            CaveMemory.Write_StrBytes("8B 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_X_Address));
            //mov [esi+4], al
            CaveMemory.Write_StrBytes("88 46 04");
            //mov eax,[_P1_Y_Address]
            CaveMemory.Write_StrBytes("8B 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_Y_Address));
            //mov [esi+5], al
            CaveMemory.Write_StrBytes("88 46 05");
            //jmp exit
            CaveMemory.Write_StrBytes("EB 12");
            //mov eax,[_P2_X_Address]
            CaveMemory.Write_StrBytes("8B 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P2_X_Address));
            //mov [esi+4], al
            CaveMemory.Write_StrBytes("88 46 04");
            //mov eax,[_P2_Y_Address]
            CaveMemory.Write_StrBytes("8B 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P2_Y_Address));
            //mov [esi+5], al
            CaveMemory.Write_StrBytes("88 46 05");

            //Inject it
            CaveMemory.InjectToOffset(_Axis_InjectionStruct, "Axis");
        }

        /// <summary>
        /// Ath the end of the CGun::MainProc we are overwritting buttons values with data read on our custom Databank
        /// Looks like putting 1 in the Button_On byte is not necesary for gameplay (autofire still on) and causes repeated inputs on Name entry screen
        /// </summary>
        private void SetHack_Buttons()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //push edi
            CaveMemory.Write_StrBytes("57");
            //shl ebx, 2
            CaveMemory.Write_StrBytes("C1 E3 02");

            //mov eax, _P1_TriggerPress_Address
            CaveMemory.Write_StrBytes("B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_TriggerPress_Address));
            //add eax, ebx
            CaveMemory.Write_StrBytes("01 D8");
            //mov edi, [eax]
            CaveMemory.Write_StrBytes("8B 38");
            //mov [esi+08],edi
            CaveMemory.Write_StrBytes("89 7E 08");

            ////mov eax, _P1_TriggerOn_Address
            //CaveMemory.Write_StrBytes("B8");
            //CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_TriggerOn_Address));
            ////add eax, ebx
            //CaveMemory.Write_StrBytes("01 D8");
            ////mov edi, [eax]
            //CaveMemory.Write_StrBytes("8B 38");
            ////mov [esi+0C],edi
            //CaveMemory.Write_StrBytes("89 7E 0C");

            //mov eax, _P1_StartPress_Address
            CaveMemory.Write_StrBytes("B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_StartPress_Address));
            //add eax, ebx
            CaveMemory.Write_StrBytes("01 D8");
            //mov edi, [eax]
            CaveMemory.Write_StrBytes("8B 38");
            //mov [esi+28],edi
            CaveMemory.Write_StrBytes("89 7E 28");

            ////mov eax, _P1_StartOn_Address
            //CaveMemory.Write_StrBytes("B8");
            //CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_StartOn_Address));
            ////add eax, ebx
            //CaveMemory.Write_StrBytes("01 D8");
            ////mov edi, [eax]
            //CaveMemory.Write_StrBytes("8B 38");
            ////mov [esi+2C],edi
            //CaveMemory.Write_StrBytes("89 7E 2C");

            //mov eax, _LeverFrontPress_Address
            CaveMemory.Write_StrBytes("B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_LeverFrontPress_Address));
            //mov edi, [eax]
            CaveMemory.Write_StrBytes("8B 38");
            //mov [esi+30],edi
            CaveMemory.Write_StrBytes("89 7E 30");

            //mov eax, _LeverFrontOn_Address
            CaveMemory.Write_StrBytes("B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_LeverFrontOn_Address));
            //mov edi, [eax]
            CaveMemory.Write_StrBytes("8B 38");
            //mov [esi+34],edi
            CaveMemory.Write_StrBytes("89 7E 34");

            //mov eax, _LeverBackPress_Address
            CaveMemory.Write_StrBytes("B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_LeverBackPress_Address));
            //mov edi, [eax]
            CaveMemory.Write_StrBytes("8B 38");
            //mov [esi+38],edi
            CaveMemory.Write_StrBytes("89 7E 38");

            //mov eax, _LeverBackOn_Address
            CaveMemory.Write_StrBytes("B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_LeverBackOn_Address));
            //mov edi, [eax]
            CaveMemory.Write_StrBytes("8B 38");
            //mov [esi+3C],edi
            CaveMemory.Write_StrBytes("89 7E 3C");

            //shr ebx, 2
            CaveMemory.Write_StrBytes("C1 EB 02");
            //pop edi
            CaveMemory.Write_StrBytes("5F");
            //xor eax, eax
            CaveMemory.Write_StrBytes("31 C0");

            //Inject it
            CaveMemory.InjectToOffset(_Buttons_InjectionStruct, "Buttons");
        }

        /// <summary>
        /// Adding a byte check on a custom "Set Credits" flag in the App::Main loop
        /// That way we can call an existing function to add credits and play sound
        /// Changing the credits value is quicker but sound will not be played....
        /// </summary>
        private void SetHack_Credits()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //cmp dword ptr [_AddCredit_Address],00
            CaveMemory.Write_StrBytes("83 3D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_AddCredit_Address));
            CaveMemory.Write_StrBytes("00");
            //je originalcode
            CaveMemory.Write_StrBytes("0F 84 14 00 00 00");
            //mov [_AddCredit_Address],00
            CaveMemory.Write_StrBytes("C7 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_AddCredit_Address));
            CaveMemory.Write_StrBytes("00 00 00 00");
            //push 00
            CaveMemory.Write_StrBytes("6A 00");
            //call CreditMgr_IncCoin()
            CaveMemory.Write_call(GameProcess_ModuleBaseAddress + _CreditMgr_IncCoin_Function_Offset);
            //add esp, 4
            CaveMemory.Write_StrBytes("83 C4 04");
            //Originalcode
            //call _FrameRateMgr_ExecServer_Function_Offset()
            CaveMemory.Write_call(GameProcess_ModuleBaseAddress + _FrameRateMgr_ExecServer_Function_Offset);

            //Inject it
            CaveMemory.InjectToOffset(_Credits_InjectionStruct, "Credits");
        }

        /// <summary>
        /// Reticle seems to be misaligned if resolution is not 1920x1080
        /// </summary>
        private void SetHack_CorrectReticlePosition()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //mov [esp+14],00000780
            CaveMemory.Write_StrBytes("C7 44 24 14 80 07 00 00");
            //mov [esp+10],00000438
            CaveMemory.Write_StrBytes("C7 44 24 10 38 04 00 00");
            //mov eax,[ebp-04]
            CaveMemory.Write_StrBytes("8B 45 FC");
            //movd xmm0,eax
            CaveMemory.Write_StrBytes("66 0F 6E C0");

            //Inject it
            CaveMemory.InjectToOffset(_FixCrosshair_InjectionStruct, "FixCrosshair");
        }

        /// <summary>
        /// On a different resolution than 1920x1080, ennemy targets on screen are mislocated
        /// The game is measuring window width and window height but even with this, it's buggy
        /// Forcing window dimension to 1920p fix the issue (strangely ?)
        /// For that, the game calls to get center values
        /// Then the game calls for WindowSize
        /// = Forcing Center and max values
        /// </summary>
        private void SetHack_CorrectEnnemyTarget()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //mov [ebp-4], 44700000
            CaveMemory.Write_StrBytes("C7 45 FC 00 00 70 44");

            //mov [ebp-8], 44070000
            CaveMemory.Write_StrBytes("C7 45 F8 00 00 07 44");

            //mov [ebp-18], 44870000
            CaveMemory.Write_StrBytes("C7 45 E8 00 00 87 44");

            //mov [ebp-10], 44f00000
            CaveMemory.Write_StrBytes("C7 45 F0 00 00 F0 44");

            //movss xmm0,[ebp-18]
            CaveMemory.Write_StrBytes("F3 0F 10 45 E8");

            //Inject it
            CaveMemory.InjectToOffset(_FixEnnemyTarget_InjectionStruct, "FixEnnemyTarget");
        }

        #endregion

        #region Outputs Hack

        protected override void Apply_OutputsMemoryHack()
        {
            Create_OutputsDataBank();
            _P1_LmpStart_Address = OutputsDatabank_Address;
            _P2_LmpStart_Address = OutputsDatabank_Address + 0x04;
            _P1_LmpGun_Address = OutputsDatabank_Address + 0x08;
            _P2_LmpGun_Address = OutputsDatabank_Address + 0x0C;
            _P1_Recoil_Address = OutputsDatabank_Address + 0x10;
            _P2_Recoil_Address = OutputsDatabank_Address + 0x14;
            _P1_Damage_Address = OutputsDatabank_Address + 0x18;
            _P2_Damage_Address = OutputsDatabank_Address + 0x1C;

            SetHack_Output_StartLamp();
            SetHack_Output_GunLamp();
            SetHack_Recoil();
            SetHack_Damage();

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        /// <summary>
        /// CCreditMgr::ExecServer() ends it's loop by calling a blanked function to send Lamp Data
        /// Intercepting the call will allow to read the values
        /// </summary>
        private void SetHack_Output_StartLamp()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //mov [_P1_LmpStart_Address], edi
            CaveMemory.Write_StrBytes("89 3D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_LmpStart_Address));
            //mov [_P2_LmpStart_Address], ebx
            CaveMemory.Write_StrBytes("89 1D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P2_LmpStart_Address));

            //Inject it
            CaveMemory.InjectToOffset(_StartLamps_InjectionStruct, "StartLamps");
        }

        /// <summary>
        /// CCreditMgr::ExecServer() ends it's loop by calling a blanked function to send Lamp Data
        /// Intercepting the call will allow to read the values
        /// </summary>
        private void SetHack_Output_GunLamp()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //push eax
            CaveMemory.Write_StrBytes("50");
            //mov eax, [esp+4]
            CaveMemory.Write_StrBytes("8B 44 24 04");
            //mov [_P1_LmpGun_Address], eax
            CaveMemory.Write_StrBytes("A3");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_LmpGun_Address));
            //mov eax, [esp+8]
            CaveMemory.Write_StrBytes("8B 44 24 08");
            //mov [_P2_LmpGun_Address], eax
            CaveMemory.Write_StrBytes("A3");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P2_LmpGun_Address));

            //Inject it
            CaveMemory.InjectToOffset(_GunLamps_InjectionStruct, "GunLamps");
        }

        /// <summary>
        /// When a bullet is fired, the game sends a message to the  Shell executable and increment a bullet counter
        /// Intercepting the call allow us to put our own flag
        /// </summary>
        private void SetHack_Recoil()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //movsx eax,byte ptr [esi+60]
            CaveMemory.Write_StrBytes("0F BE 46 60");
            //shl eax,02
            CaveMemory.Write_StrBytes("C1 E0 02");
            //add eax, _P1_Recoil_Address
            CaveMemory.Write_StrBytes("05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_Recoil_Address));
            //mov [eax], 1
            CaveMemory.Write_StrBytes("C7 00 01 00 00 00");
            //movsx eax,byte ptr [esi+60]
            CaveMemory.Write_StrBytes("0F BE 46 60");
            //push 00
            CaveMemory.Write_StrBytes("6A 00");

            //Inject it
            CaveMemory.InjectToOffset(_Recoil_InjectionStruct, "Recoil");
        }

        /// <summary>
        /// When a player takes dammage, the game sends a command to the Shell executable.
        /// Intercepting the call allow us to put our own flag
        /// </summary>
        private void SetHack_Damage()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //mov [ebp+15],al
            CaveMemory.Write_StrBytes("88 45 15");
            //xor eax,eax
            CaveMemory.Write_StrBytes("31 C0");
            //mov al,[edi+60]
            CaveMemory.Write_StrBytes("8A 47 60");
            //shl eax,02
            CaveMemory.Write_StrBytes("C1 E0 02");
            //add eax, _P1_Damage_Address
            CaveMemory.Write_StrBytes("05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_Damage_Address));
            //mov [eax], 1
            CaveMemory.Write_StrBytes("C7 00 01 00 00 00");
            //lea eax,[ebp+14]
            CaveMemory.Write_StrBytes("8D 45 14");

            //Inject it
            CaveMemory.InjectToOffset(_Damage_InjectionStruct, "Dammage");
        }

        /// <summary>
        /// The game is checking if Recoil is enabled for each bullet fired.
        /// Using this request call, we can generate the start of our own CustomRecoil output event
        /// </summary>
        private void SetHack_RecoilP1()
        {

        }

        private void SetHack_RecoilP2()
        {

        }

        #endregion

        #region No Crosshair Hack

        /// <summary>
        /// Removing all UI targetting display :
        /// - Reticle (?)
        /// - Laser
        /// - Gun Model (deactivated when KID MODE is on)
        /// </summary>
        protected override void Apply_NoCrosshairMemoryHack()
        {
            // PlayerGunObj::UpdateLaserSight(PlayerGunObj *this)
            // Check if laser is on -> force JmMP to remove sight
            WriteByte(GameProcess_ModuleBaseAddress + _LaserSight_Patch_Offset, 0xEB);

            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //push eax
            CaveMemory.Write_StrBytes("50");
            //call GmMgr_GetGameMode()
            CaveMemory.Write_call(GameProcess_ModuleBaseAddress + _GmMgr_GetGameMode_Function_Offset);
            //cmp eax,06
            CaveMemory.Write_StrBytes("83 F8 06");
            //jb original code
            CaveMemory.Write_StrBytes("72 1A");
            //cmp eax,07
            CaveMemory.Write_StrBytes("83 F8 07");
            //ja original code
            CaveMemory.Write_StrBytes("77 15");
            //call _PlMgr_IsHoldMode
            CaveMemory.Write_call(GameProcess_ModuleBaseAddress + _PlMgr_IsHoldMode_FunctionOffset);
            //test eax, eax
            CaveMemory.Write_StrBytes("85 C0");
            //jne originalcode
            CaveMemory.Write_StrBytes("75 0C");
            //mov [esi],C1F00000
            CaveMemory.Write_StrBytes("C7 06 00 00 F0 C1");
            //mov [edi],C1F00000
            CaveMemory.Write_StrBytes("C7 07 00 00 F0 C1");
            //Originalcode:
            //pop eax
            CaveMemory.Write_StrBytes("58");
            //call GetResolution()
            CaveMemory.Write_call(GameProcess_ModuleBaseAddress + _GetResolution_Function_Offset);

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
            else if (e.ButtonName.Equals("COIN"))
            {
                WriteByte(_AddCredit_Address, 0x01);
            }
            else if (e.ButtonName.Equals("LEVER FRONT"))
            {
                WriteByte(_LeverFrontOn_Address, 0x01);
                WriteByte(_LeverFrontPress_Address, 0x01);
            }
            else if (e.ButtonName.Equals("LEVER BACK"))
            {
                WriteByte(_LeverBackOn_Address, 0x01);
                WriteByte(_LeverBackPress_Address, 0x01);
            }
        }

        protected override void GameController_OnButtonUp(object sender, VirtualGameControler.ButtonDownEventArgs e)
        { }

        protected override void UpdateAxis(PlayerID Id, int X, int Y)
        {
            if (Id != PlayerID.None)
            {
                byte[] bufferX = BitConverter.GetBytes(X);
                byte[] bufferY = BitConverter.GetBytes(Y);

                WriteBytes(_P1_X_Address + ((uint)Id * 0x08), bufferX);
                WriteBytes(_P1_Y_Address + ((uint)Id * 0x08), bufferY);
            }
        }

        protected override void PlayerController_OnButtonDown(object sender, VirtualGameControler.ButtonDownEventArgs e)
        {
            if (e.Id != PlayerID.None)
            {
                UInt32 PlayerIndex = (UInt32)e.Id;

                if (e.ButtonName.Equals("TRIGGER"))
                {
                    WriteByte(_P1_TriggerOn_Address + (PlayerIndex * 0x04), 0x01);
                    WriteByte(_P1_TriggerPress_Address + (PlayerIndex * 0x04), 0x01);
                }
                else if (e.ButtonName.Equals("START"))
                {
                    WriteByte(_P1_StartOn_Address + (PlayerIndex * 0x04), 0x01);
                    WriteByte(_P1_StartPress_Address + (PlayerIndex * 0x04), 0x01);
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
                    WriteByte(_P1_TriggerOn_Address + (PlayerIndex * 0x04), 0x00);
                    WriteByte(_P1_TriggerPress_Address + (PlayerIndex * 0x04), 0x00);
                }
                else if (e.ButtonName.Equals("START"))
                {
                    WriteByte(_P1_StartOn_Address + (PlayerIndex * 0x04), 0x00);
                    WriteByte(_P1_StartPress_Address + (PlayerIndex * 0x04), 0x00);
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
                new GameOutput(OutputId.P1_LmpGun),
                new GameOutput(OutputId.P2_LmpGun),
                new GameOutput(OutputId.LmpRoom),
                new GameOutput(OutputId.LmpLever),
                new GameOutput(OutputId.Lmp_Downlight),
                new GameOutput(OutputId.P1_GunRecoil),
                new GameOutput(OutputId.P2_GunRecoil),
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
            //Genuine Outputs
            SetOutputValue(OutputId.P1_LmpStart, ReadByte(_P1_LmpStart_Address));
            SetOutputValue(OutputId.P2_LmpStart, ReadByte(_P2_LmpStart_Address));
            SetOutputValue(OutputId.P1_LmpGun, ReadByte(_P1_LmpGun_Address));
            SetOutputValue(OutputId.P2_LmpGun, ReadByte(_P2_LmpGun_Address));

            //To-do : 
            //Lamp side, Lever and recoil ??

            //Custom Outputs
            _Life[0] = 0;
            _Life[1] = 0;
            int Credits = 0;

            //Life is read in a struct that is only created when a game has started
            UInt32 PlayerManager = ReadPtr(_pPlayerManager_Address);
            if (PlayerManager != 0)
            {
                UInt32 CPlayer1 = ReadPtr(PlayerManager + _CPlayer1_Offset);
                if (CPlayer1 != 0)
                {
                    if (ReadByte(CPlayer1 + _Cplayer_Mode_Offset) == 3 && ReadByte(CPlayer1 + _Cplayer_ID_Offset) == 0)
                    {
                        _Life[0] = ReadInt32(CPlayer1 + _Cplayer_Life_Offset);
                    }
                }

                UInt32 CPlayer2 = ReadPtr(PlayerManager + _CPlayer2_Offset);
                if (CPlayer2 != 0)
                {
                    if (ReadByte(CPlayer2 + _Cplayer_Mode_Offset) == 3 && ReadByte(CPlayer2 + _Cplayer_ID_Offset) == 1)
                    {
                        _Life[1] = ReadInt32(CPlayer2 + _Cplayer_Life_Offset);
                    }
                }
            }

            //Reading our own flags and resetting them
            if (ReadByte(_P1_Recoil_Address) == 1)
            {
                SetOutputValue(OutputId.P1_CtmRecoil, 1);
                WriteByte(_P1_Recoil_Address, 0);
            }
            if (ReadByte(_P2_Recoil_Address) == 1)
            {
                SetOutputValue(OutputId.P2_CtmRecoil, 1);
                WriteByte(_P2_Recoil_Address, 0);
            }
            if (ReadByte(_P1_Damage_Address) == 1)
            {
                SetOutputValue(OutputId.P1_Damaged, 1);
                WriteByte(_P1_Damage_Address, 0);
            }
            if (ReadByte(_P2_Damage_Address) == 1)
            {
                SetOutputValue(OutputId.P2_Damaged, 1);
                WriteByte(_P2_Damage_Address, 0);
            }

            SetOutputValue(OutputId.P1_Life, _Life[0]);
            SetOutputValue(OutputId.P2_Life, _Life[1]);

            UInt32 CCreditsManager = ReadPtr(_pCreditsManager_Address);
            if (CCreditsManager != 0)
            {
                Credits = ReadByte(CCreditsManager + 0x38);
            }
            SetOutputValue(OutputId.Credits, Credits);
        }

        #endregion
    }
}
