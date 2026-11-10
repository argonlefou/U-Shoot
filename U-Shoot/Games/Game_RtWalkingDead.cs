using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UsCore.Config;
using UsCore.Launcher;
using UsCore.Logging;
using UsCore.MameOutput;
using UsCore.Memory;
using UsCore.VirtualControllers;

namespace U_Shoot.Games
{
    public class Game_RtWalkingDead : BaseGame32
    {
        protected override bool PATCH_PEB_DEBUG_FLAG { get { return false; } }

        private const string ELFPATH_OPTION = "Game ELF";
        private const string LOADEROPTIONS_OPTION = "LinuxLoader options";

        #region LOADER MEMORY ADDRESSES                
        //Check instruction for game loaded
        private UInt32 _GameLoaded_Check_Address = 0x0819DE0B;
        private InjectionStruct32 _NoCrosshair_InjectionStruct = new InjectionStruct32(0x0806DF47, 8);
        #endregion

        #region INPUTS MEMORY ADDRESSES    
        private UInt32 _Calibration_Patch1_Address = 0x081C52BE;
        private UInt32 _Calibration_Patch2_Address = 0x081C52E2;
        private InjectionStruct32 _RawAxis_InjectionStruct = new InjectionStruct32(0x081CB412, 10);
        private InjectionStruct32 _CalibratedAxis_InjectionStruct = new InjectionStruct32(0x081C52B4, 5);
        private InjectionStruct32 _GunButtons_InjectionStruct = new InjectionStruct32(0x081CB475, 7);

        //Custom Values
        private UInt32 _RawAxes_CaveAddress = 0;
        private UInt32 _CalibratedAxes_CaveAddress = 0;
        private UInt32 _GunButtons_CaveAddress = 0;
        #endregion

        #region OUTPUTS MEMORY ADDRESSES
        private UInt32 _P1_Struct_Address = 0x090100A0;
        //private UInt32 _P2_Struct_Address = 0x09010274;
        private UInt32 _Lamp_Address = 0x08AC2188;
        private InjectionStruct32 _Recoil_InjectionStruct = new InjectionStruct32(0x0808B1D9, 7);
        private InjectionStruct32 _Damage_InjectionStruct = new InjectionStruct32(0x080762D6, 8);

        //Custom Values
        private UInt32 _RecoilStatus_CaveAddress = 0;
        private UInt32 _DamageStatus_CaveAddress = 0;
        #endregion

        public Game_RtWalkingDead(GameConfig Config) : base(Config)
        {
            string ElfPath = _GameConfig.GetStringOption(ELFPATH_OPTION + " [game]");

            _GameLauncher.Set_CmdLine_Args("\"" + ElfPath + "\" " + _GameConfig.GetStringOption(LOADEROPTIONS_OPTION));            

            _KnownMd5Prints.Add("Walking Dead - 01.05 - Teknoparrot Patched", "5158b185b977b38749845be958caddb6");
            _KnownMd5Prints.Add("Walking Dead - 01.05 - Original dump", "6951619cd907173862c5eb337e263af5");

            if (_GameConfig.HideConsole)
            {
                _GameLauncher.RedirectOutputsToNull = true;
                _GameLauncher.CreateNoWindow = true;
            }
        }

        protected override void ProcessTimerTick_Unhooked_CheckProc()
        {
            try
            {
                //To make sure LinuxLoader has loaded the rom entirely, we're looking for some random instruction to be present in memory before starting
                byte[] buffer = ReadBytes(_GameLoaded_Check_Address, 3);
                if (buffer.SequenceEqual(new byte[] { 0x8B, 0x40, 0x48 }))
                {
                    Logger.WriteLog("Walking Dead 1.05 binary detected");
                }
                else
                {
                    Logger.WriteLog("Game not Loaded, waiting...");
                    return;
                }

                ReadGameDataFromMd5Hash(GAMEDATA_FOLDER);
                Apply_MemoryHacks();
            }
            catch (Exception Ex)
            {
                Logger.WriteLog("Error waiting for the game to be loaded : " + Ex.Message);
            }
        }

        #region Debugger Events

        #region EXCEPTION_DEBUG_EVENT

        protected override void ExceptionEvent(object sender, x86_x64_Debugger.ExceptionDebugEventArgs e)
        {
            //No need to to anything here, U-Shoot will have to wait for the ROM to be loaded in memory to apply Memory Hacks
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
                    PlayerController.Computed_Y = Convert.ToInt32(dRangeY - Math.Round(dRangeY * PlayerController.Computed_Y / TotalResY));
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


            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        #endregion        

        #region Inputs Hack

        protected override void Apply_InputsMemoryHack()
        {
            Create_InputsDataBank();
            _RawAxes_CaveAddress = InputsDatabank_Address;               //8 x 4 Bytes
            _CalibratedAxes_CaveAddress = InputsDatabank_Address + 32;   //8 x 4 Bytes
            _GunButtons_CaveAddress = InputsDatabank_Address + 64;       //8 x 1 Byte

            //Jumping over "invert" axis option for calibration
            WriteByte(_Calibration_Patch1_Address, 0xEB);

            //Jumping over last optional calibration calculation, after our own value is inserted
            WriteByte(_Calibration_Patch2_Address, 0xEB);

            SetHack_RawAxis();
            SetHack_CalibratedAxes();
            SetHack_GunButtons();

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        /// <summary>
        /// Change the float value used to be sent to the calibration procedure by our own
        /// 8 slots are used (0-based):
        /// - Slot 2 = P1 Reload
        /// - Slot 3 = P2 Reload
        /// - Slots 4,5,6,7 = P1x, P1y, P2x, P2y
        /// </summary>
        private void SetHack_RawAxis()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //mov eax,[eax*4+_Axes_CaveAddress]
            CaveMemory.Write_StrBytes("8B 04 85");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_RawAxes_CaveAddress));

            //Inject it
            CaveMemory.InjectToAddress(_RawAxis_InjectionStruct, "Raw Axes");
        }

        /// <summary>
        /// At the end of that procedure, the game has moved the raw axis value to [-1.0, 1.0] axis value
        /// Overriding the result to align the crosshair with the gun sight
        /// 8 slots are used (0-based):
        /// - Slot 2 = P1 Reload
        /// - Slot 3 = P2 Reload
        /// - Slots 4,5,6,7 = P1x, P1y, P2x, P2y
        /// </summary>
        private void SetHack_CalibratedAxes()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //push ebx
            CaveMemory.Write_StrBytes("53");
            //mov ebx,[ebp+30]
            CaveMemory.Write_StrBytes("8B 5D 30");
            //shl ebx,02
            CaveMemory.Write_StrBytes("C1 E3 02");
            //add ebx,_CalibratedAxes_CaveAddress
            CaveMemory.Write_StrBytes("81 C3");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_CalibratedAxes_CaveAddress));
            //mov ebx,[ebx]
            CaveMemory.Write_StrBytes("8B 1B");
            //mov eax,[ebp+08]
            CaveMemory.Write_StrBytes("8B 45 08");
            //mov [eax+04],ebx
            CaveMemory.Write_StrBytes("89 58 04");
            //pop ebx
            CaveMemory.Write_StrBytes("5B");
            //mov eax,[eax]
            CaveMemory.Write_StrBytes("8B 00");

            //Inject it
            CaveMemory.InjectToAddress(_CalibratedAxis_InjectionStruct, "Calibrated Axes");
        }

        /// <summary>
        /// Gun buttons (= trigger only ?) is just a counter, incremented with each press/release
        /// There again, 8 slots are available (0-based) :
        /// - Slot 4 : P1 Trigger 
        /// - Slot 6 : P2 Trigger
        /// </summary>
        private void SetHack_GunButtons()
        {            
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //movzx eax,byte ptr [eax+_GunButtons_CaveAddress]
            CaveMemory.Write_StrBytes("0F B6 80");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_GunButtons_CaveAddress));

            //Inject it
            CaveMemory.InjectToAddress(_GunButtons_InjectionStruct, "Gun Buttons");
        }

        #endregion

        #region Outputs Hack

        protected override void Apply_OutputsMemoryHack()
        {
            Create_OutputsDataBank();
            _RecoilStatus_CaveAddress = OutputsDatabank_Address;
            _DamageStatus_CaveAddress = OutputsDatabank_Address + 0x04;

            SetHack_Damage();
            SetHack_Recoil();

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        /// <summary>
        /// Intercep call to log damage taken by a player to set a custom flag to "1"
        /// </summary>
        private void SetHack_Damage()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //push edi
            CaveMemory.Write_StrBytes("57");
            //sub edi,01
            CaveMemory.Write_StrBytes("83 EF 01");
            //add edi,_DamageStatus_CaveAddress
            CaveMemory.Write_StrBytes("81 C7");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_DamageStatus_CaveAddress));
            //mov byte ptr[edi],01
            CaveMemory.Write_StrBytes("C6 07 01");
            //pop edi
            CaveMemory.Write_StrBytes("5F");
            //mov [esp+04],00000040
            CaveMemory.Write_StrBytes("C7 44 24 04 40 00 00 00");

            //Inject it
            CaveMemory.InjectToAddress(_Damage_InjectionStruct, "Damage");
        }

        /// <summary>
        /// Intercept call for checking ForceFeedback enabled setting on shoot
        /// </summary>
        private void SetHack_Recoil()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //push ebx
            CaveMemory.Write_StrBytes("53");
            //add ebx, _RecoilStatus_CaveAddress
            CaveMemory.Write_StrBytes("81 C3");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_RecoilStatus_CaveAddress));
            //mov byte ptr[ebx],01
            CaveMemory.Write_StrBytes("C6 03 01");
            //pop ebx
            CaveMemory.Write_StrBytes("5B");

            //mov [esp],088C735C
            CaveMemory.Write_StrBytes("C7 04 24 5C 73 8C 08");

            //Inject it
            CaveMemory.InjectToAddress(_Recoil_InjectionStruct, "Recoil");
        }

        #endregion

        #region No Crosshair Hack

        /// <summary>
        /// Forcing one axis value to -1.0f to draw crosshairs
        /// </summary>
        protected override void Apply_NoCrosshairMemoryHack()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //lea edx,[esp+18]
            CaveMemory.Write_StrBytes("8D 54 24 18");
            //mov [edx],BF800000
            CaveMemory.Write_StrBytes("C7 02 00 00 80 BF");
            //mov [esp+04],edx
            CaveMemory.Write_StrBytes("89 54 24 04");

            //InjectIt
            CaveMemory.InjectToAddress(_NoCrosshair_InjectionStruct, "NoCrosshair");

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

            }
            else if (e.ButtonName.Equals("SERVICE"))
            {

            }
            else if (e.ButtonName.Equals("TEST"))
            {

            }
            else if (e.ButtonName.Equals("VOLUME UP"))
            {

            }
            else if (e.ButtonName.Equals("VOLUME DOWN"))
            {

            }
        }

        protected override void GameController_OnButtonUp(object sender, VirtualGameControler.ButtonDownEventArgs e)
        {
            if (e.ButtonName.Equals("SERVICE"))
            {

            }
            else if (e.ButtonName.Equals("TEST"))
            {

            }
            else if (e.ButtonName.Equals("VOLUME UP"))
            {

            }
            else if (e.ButtonName.Equals("VOLUME DOWN"))
            {

            }
        }

        protected override void UpdateAxis(PlayerID Id, int X, int Y)
        {
            if (Id != PlayerID.None)
            {
                //Raw axes value
                byte[] bufferX = BitConverter.GetBytes(X);
                byte[] bufferY = BitConverter.GetBytes(Y);
                WriteBytes(_RawAxes_CaveAddress + 16 + (8 * (uint)Id), bufferX);
                WriteBytes(_RawAxes_CaveAddress + 20 + (8 * (uint)Id), bufferY);

                //Float 'calibrated' axes values
                float cX = 1.0f * (float)X / 1024.0f;
                float cY = 1.0f * (float)Y / 1024.0f;
                WriteBytes(_CalibratedAxes_CaveAddress + 16 + (8 * (uint)Id), BitConverter.GetBytes(cX));
                WriteBytes(_CalibratedAxes_CaveAddress + 20 + (8 * (uint)Id), BitConverter.GetBytes(cY));
            }
        }

        protected override void PlayerController_OnButtonDown(object sender, VirtualGameControler.ButtonDownEventArgs e)
        {
            if (e.Id != PlayerID.None)
            {
                UInt32 PlayerIndex = (UInt32)e.Id;

                if (e.ButtonName.Equals("TRIGGER"))
                {
                    UInt32 ButtonAddress = _GunButtons_CaveAddress + 4 + (2 * PlayerIndex);
                    byte b = ReadByte(ButtonAddress);
                    WriteByte(ButtonAddress, ++b);
                }
                else if (e.ButtonName.Equals("RELOAD"))
                {
                    //Reload is an analog value, when player is pulling the lever. Max raw power is 0x3FF and calibrated power 1023.0f
                    WriteBytes(_RawAxes_CaveAddress + 8 + (4 * PlayerIndex), BitConverter.GetBytes(0x000003FF));
                    WriteBytes(_CalibratedAxes_CaveAddress + 8 + (4 * PlayerIndex), BitConverter.GetBytes((float)1023));
                }
                else if (e.ButtonName.Equals("START"))
                {

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
                    UInt32 ButtonAddress = _GunButtons_CaveAddress + 4 + (2 * PlayerIndex);
                    byte b = ReadByte(ButtonAddress);
                    WriteByte(ButtonAddress, ++b);
                }
                else if (e.ButtonName.Equals("RELOAD"))
                {
                    //Reload is an analog value, when player is pulling the lever. Min power is 0
                    WriteBytes(_RawAxes_CaveAddress + 8 + (4 * PlayerIndex), BitConverter.GetBytes(0));
                    WriteBytes(_CalibratedAxes_CaveAddress + 8 + (4 * PlayerIndex), BitConverter.GetBytes(0));
                }
                else if (e.ButtonName.Equals("START"))
                {

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
                new GameOutput(OutputId.P1_LmpHolder),
                new GameOutput(OutputId.P2_LmpHolder),
                new GameOutput(OutputId.P1_LmpPanel),
                new GameOutput(OutputId.P2_LmpPanel),
                new GameOutput(OutputId.LmpRoof_R),
                new GameOutput(OutputId.LmpRoof_G),
                new GameOutput(OutputId.LmpRoof_B),
                new GameOutput(OutputId.LmpLgMarquee),
                new GameOutput(OutputId.LmpSqMarquee),
                new GameOutput(OutputId.LmpWalker_R),
                new GameOutput(OutputId.LmpWalker_G),
                new GameOutput(OutputId.LmpWalker_B),
                new GameOutput(OutputId.LmpWalkerEyes),
                new GameOutput(OutputId.LmpWalkerCeiling),
                new GameOutput(OutputId.LmpPosts),
                new GameOutput(OutputId.LmpRear_R),

                //In Teknoparrot, Guns hardware is not emulated, so the game is not running original gun recoil procedures 
                /*_Outputs.Add(new GameOutput(OutputId.P1_GunMotor));
                _Outputs.Add(new GameOutput(OutputId.P2_GunMotor));*/
                new GameOutput(OutputId.P1_Ammo),
                new GameOutput(OutputId.P2_Ammo),
                new AsyncGameOutput(OutputId.P1_CtmRecoil, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_ON_DELAY), ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_OFF_DELAY), 0),
                new AsyncGameOutput(OutputId.P2_CtmRecoil, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_ON_DELAY), ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_OFF_DELAY), 0),
                new GameOutput(OutputId.P1_Life),
                new GameOutput(OutputId.P2_Life),
                new AsyncGameOutput(OutputId.P1_Damaged, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_DAMAGED_DELAY), 100, 0),
                new AsyncGameOutput(OutputId.P2_Damaged, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_DAMAGED_DELAY), 100, 0)
            };
            //_Outputs.Add(new GameOutput(OutputId.Credits));
        }

        /// <summary>
        /// Update all Outputs values before sending them to MameHooker
        /// </summary>
        public override void UpdateOutputValues()
        {
            //Original Outputs
            SetOutputValue(OutputId.P1_LmpStart, ReadByte(_Lamp_Address + 0x18));
            SetOutputValue(OutputId.P2_LmpStart, ReadByte(_Lamp_Address + 0x1C));
            SetOutputValue(OutputId.P1_LmpGun, (int)(ReadFloat(_Lamp_Address) * 20));
            SetOutputValue(OutputId.P2_LmpGun, (int)(ReadFloat(_Lamp_Address + 0x04) * 200));
            SetOutputValue(OutputId.P1_LmpHolder, (int)(ReadFloat(_Lamp_Address + 0x84) * 200));
            SetOutputValue(OutputId.P2_LmpHolder, (int)(ReadFloat(_Lamp_Address + 0xB4) * 200));
            SetOutputValue(OutputId.P1_LmpPanel, (int)(ReadFloat(_Lamp_Address + 0x7C) * 200));
            SetOutputValue(OutputId.P2_LmpPanel, (int)(ReadFloat(_Lamp_Address + 0x80) * 200));
            SetOutputValue(OutputId.LmpRoof_R, (int)(ReadFloat(_Lamp_Address + 0x88) * 200));
            SetOutputValue(OutputId.LmpRoof_G, (int)(ReadFloat(_Lamp_Address + 0x8C) * 200));
            SetOutputValue(OutputId.LmpRoof_B, (int)(ReadFloat(_Lamp_Address + 0x90) * 200));

            //Long Marquee uses 4 outputs, so we will add them to be sure it won't be missed if the game only triggers a part of them
            byte LongMarqueeStatus = 0;
            for (uint i = 0; i < 3; i++)
            {
                if (ReadByte(_Lamp_Address + 0x94 + 4 * i + 3) != 0x00)
                    LongMarqueeStatus |= 1;
            }
            SetOutputValue(OutputId.LmpLgMarquee, LongMarqueeStatus);
            SetOutputValue(OutputId.LmpSqMarquee, (int)(ReadFloat(_Lamp_Address + 0xA4) * 200));
            SetOutputValue(OutputId.LmpWalker_R, (int)(ReadFloat(_Lamp_Address + 0xA8) * 200));
            SetOutputValue(OutputId.LmpWalker_G, (int)(ReadFloat(_Lamp_Address + 0xAC) * 200));
            SetOutputValue(OutputId.LmpWalker_B, (int)(ReadFloat(_Lamp_Address + 0xB0) * 200));
            SetOutputValue(OutputId.LmpWalkerEyes, ReadByte(_Lamp_Address + 0x2C));
            SetOutputValue(OutputId.LmpWalkerCeiling, ReadByte(_Lamp_Address + 0x28));
            SetOutputValue(OutputId.LmpPosts, ReadByte(_Lamp_Address + 0x20));
            SetOutputValue(OutputId.LmpRear_R, ReadByte(_Lamp_Address + 0x24));

            //Custom Outputs
            for (uint i = 0; i < 2; i++)
            {
                _Life[i] = 0;
                _Ammo[i] = 0;

                //Check if the Player is currently playing
                UInt32 _PlayerStruct_Address = _P1_Struct_Address + (i * 0x1D4);
                if (ReadByte(_PlayerStruct_Address + 0x01) == 1)
                {
                    _Ammo[i] = ReadInt32(_PlayerStruct_Address + 0x6C);
                    if (_Ammo[i] < 0)
                        _Ammo[i] = 0;

                    _Life[i] = ReadInt32(_PlayerStruct_Address + 0x10);
                    if (_Life [i] < 0)
                        _Life[i] = 0;
                }

                //Reading our own flags for damage and recoil and resetting them
                if (ReadByte(_RecoilStatus_CaveAddress + i) == 1)
                {
                    SetOutputValue(OutputId.P1_CtmRecoil + i, 1);
                    WriteByte(_RecoilStatus_CaveAddress + i, 0);
                }
                if (ReadByte(_DamageStatus_CaveAddress + i) == 1)
                {
                    SetOutputValue(OutputId.P2_CtmRecoil + i, 1);
                    WriteByte(_DamageStatus_CaveAddress + i, 0);
                }

                SetOutputValue(OutputId.P1_Ammo + i, _Ammo[i]);
                SetOutputValue(OutputId.P1_Life + i, _Life[i]);
            }

            //SetOutputValue(OutputId.Credits, ReadByte(_Credits_Address));
        }

        #endregion
    }
}
