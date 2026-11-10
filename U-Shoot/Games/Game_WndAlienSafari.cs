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
    public class Game_WndAlienSafari : BaseGame32
    {
        protected override string GAMEDATA_FOLDER { get { return @"windows\ads"; } }
        protected override bool PATCH_PEB_DEBUG_FLAG { get { return true; } }

        #region LOADER MEMORY ADDRESSES                
        private InjectionStruct32 _NoCrosshair_InjectionStruct = new InjectionStruct32(0x00015F71, 6);
        #endregion

        #region INPUTS MEMORY ADDRESSES    
        private InjectionStruct32 _Axis_InjectionStruct = new InjectionStruct32(0x0004C0BB, 6); //also 44C0FD ?
        private InjectionStruct32 _Buttons_InjectionStruct = new InjectionStruct32(0x00017AE0A, 8);
        
        //Custom Data
        private UInt32 _P1_X_CaveAddress = 0;
        private UInt32 _P1_Y_CaveAddress = 0;
        private UInt32 _P1_Trigger_CaveAddress = 0;
        private UInt32 _P1_TriggerEventCountdown_CaveAddress = 0;
        private UInt32 _P1_Reload_CaveAddress = 0;
        private UInt32 _P1_ReloadEventCountdown_CaveAddress = 0;
        private UInt32 _FloatZero_CaveAddress = 0;
        private UInt32 _FloatOne_CaveAddress = 0;
        #endregion

        #region OUTPUTS MEMORY ADDRESSES
        private InjectionStruct32 _IsPlaying_InjectionStruct = new InjectionStruct32(0x000502BE, 6);
        private InjectionStruct32 _StartLevel_InjectionStruct = new InjectionStruct32(0x00188E93, 5);
        private InjectionStruct32 _EndLevel_InjectionStruct = new InjectionStruct32(0x00188EC8, 7);
        private InjectionStruct32 _Recoil_InjectionStruct = new InjectionStruct32(0x000502FD, 6);

        //Custom Data
        private UInt32 _PlayerIsPlaying_CaveAddress = 0;
        private UInt32 _WeaponMaxAmmo_CaveAddress = 0;
        private UInt32 _WeaponCurrentAmmo_CaveAddress = 0;
        private UInt32 _WeaponCurrentAmmo2_CaveAddress = 0;
        #endregion

        private int _P1_WeaponMaxAmmo_Last = 0;  //Use to check if weapon has changed, to prevent triggering recoil when switching for a lower-munition weapon

        public Game_WndAlienSafari(GameConfig Config) : base(Config)
        {
            _KnownMd5Prints.Add("Aliens Disco Safari 1.0.0.1 - ToEng", "8b9db55dd8bf8af653f30fc0301cad6c");     //Decrypted exe
            _KnownMd5Prints.Add("Aliens Disco Safari 1.0.0.1 - Original", "2618455f69182c9c47e2d8a959cd00e9");  //Encrypted exe, unpacking itself at start
            _KnownMd5Prints.Add("Alien Attack On Earth 1.0.0.1", "e5ee89752da815205f007a3692343476");  //Not the same game, unsupported
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
                    double dMaxX = 800.0;
                    double dMaxY = 600.0;
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


            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }
        #endregion        

        #region Inputs Hack

        protected override void Apply_InputsMemoryHack()
        {
            Create_InputsDataBank();
            _P1_X_CaveAddress = InputsDatabank_Address;
            _P1_Y_CaveAddress = InputsDatabank_Address + 0x04;
            _P1_Trigger_CaveAddress = InputsDatabank_Address + 0x08;
            _P1_TriggerEventCountdown_CaveAddress = InputsDatabank_Address + 0x09;
            _P1_Reload_CaveAddress = InputsDatabank_Address + 0x0C;
            _P1_ReloadEventCountdown_CaveAddress = InputsDatabank_Address + 0x0D;
            _FloatZero_CaveAddress = InputsDatabank_Address + 0x20;
            WriteBytes(_FloatZero_CaveAddress, BitConverter.GetBytes(0.0f));
            _FloatOne_CaveAddress = InputsDatabank_Address + 0x24;
            WriteBytes(_FloatOne_CaveAddress, BitConverter.GetBytes(1.0f));

            SetHack_Axis();
            SetHack_Buttons();

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        private void SetHack_Axis()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //fstp dword ptr [eax+000000C0]
            CaveMemory.Write_StrBytes("D9 98 C0 00 00 00");
            //push ebx
            CaveMemory.Write_StrBytes("53");
            //mov ebx, [_P1_X_CaveAddress]
            CaveMemory.Write_StrBytes("8B 1D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_X_CaveAddress));
            //mov [eax+BC], ebx
            CaveMemory.Write_StrBytes("89 98 BC 00 00 00");
            //mov ebx, [_P1_Y_CaveAddress]
            CaveMemory.Write_StrBytes("8B 1D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_Y_CaveAddress));
            //mov [eax+C0], ebx
            CaveMemory.Write_StrBytes("89 98 C0 00 00 00");
            //pop ebx
            CaveMemory.Write_StrBytes("5B");

            //Inject it
            CaveMemory.InjectToOffset(_Axis_InjectionStruct, "AxisY");
        }

        /// <summary>
        /// The game is using 1 procedure to call for button state, based on a parameter index
        /// Result is a float value, thresold is 0.5f
        /// So it's safe to reply 0 for not presses and 0x3F80 (1.0f) for pressed
        /// 15 = Axis X (relative movement)     
        /// 16 = Axis Y (relative movement)     Both called by the previously done hack IN-GAME (move a bit in menus ?)
        /// 
        /// 5 = Trigger (EventDown)
        /// 8 = Trigger (Down)
        /// 7 = Reload (Eventdown)
        /// 
        /// 6,9 = ? called for trigger check also. Maybe a second button ?
        /// </summary>
        private void SetHack_Buttons()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //mov ecx,[esp+04]
            CaveMemory.Write_StrBytes("8B 4C 24 04");

            //P1 Trigger Down event
            //cmp ecx, 05
            CaveMemory.Write_StrBytes("83 F9 05");
            //jne P1_TriggerDown
            CaveMemory.Write_StrBytes("75 22");
            //cmp byte ptr [_P1_Trigger_CaveAddress],00
            CaveMemory.Write_StrBytes("80 3D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_Trigger_CaveAddress));
            CaveMemory.Write_StrBytes("00");
            //je Zero
            CaveMemory.Write_StrBytes("74 56");
            //cmp byte ptr [_P1_TriggerEventCountdown_CaveAddress],00
            CaveMemory.Write_StrBytes("80 3D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_TriggerEventCountdown_CaveAddress));
            CaveMemory.Write_StrBytes("00");
            //jg 00DE001D
            CaveMemory.Write_StrBytes("7F 02");
            //jmp Zero
            CaveMemory.Write_StrBytes("EB 4B");
            //fld dword ptr [_FloatOne_CaveAddress]
            CaveMemory.Write_StrBytes("D9 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_FloatOne_CaveAddress));
            //shr [_P1_TriggerEventCountdown_CaveAddress],1
            CaveMemory.Write_StrBytes("D1 2D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_TriggerEventCountdown_CaveAddress));
            //jmp Exit
            CaveMemory.Write_StrBytes("EB 43");

            //P1_TriggerDown:
            //cmp ecx,08
            CaveMemory.Write_StrBytes("83 F9 08");
            //jne Reload
            CaveMemory.Write_StrBytes("75 11");
            //cmp byte ptr [_P1_Trigger_CaveAddress],00
            CaveMemory.Write_StrBytes("80 3D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_Trigger_CaveAddress));
            CaveMemory.Write_StrBytes("00");
            //je Zero
            CaveMemory.Write_StrBytes("74 2F");
            //fld dword ptr [_P1_Trigger_CaveAddress]
            CaveMemory.Write_StrBytes("D9 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_FloatOne_CaveAddress));
            //jmp Exit
            CaveMemory.Write_StrBytes("EB 2D");

            //P1 Reload Down event
            //cmp ecx, 07
            CaveMemory.Write_StrBytes("83 F9 07");
            //jne Next
            CaveMemory.Write_StrBytes("75 22");
            //cmp byte ptr [_P1_Reload_CaveAddress],00
            CaveMemory.Write_StrBytes("80 3D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_Reload_CaveAddress));
            CaveMemory.Write_StrBytes("00");
            //je Zero
            CaveMemory.Write_StrBytes("74 19");
            //cmp byte ptr [_P1_ReloadEventCountdown_CaveAddress],00
            CaveMemory.Write_StrBytes("80 3D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_ReloadEventCountdown_CaveAddress));
            CaveMemory.Write_StrBytes("00");
            //jg 00DE001D
            CaveMemory.Write_StrBytes("7F 02");
            //jmp Zero
            CaveMemory.Write_StrBytes("EB 0E");
            //fld dword ptr [_FloatOne_CaveAddress]
            CaveMemory.Write_StrBytes("D9 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_FloatOne_CaveAddress));
            //shr [_P1_ReloadEventCountdown_CaveAddress],1
            CaveMemory.Write_StrBytes("D1 2D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_ReloadEventCountdown_CaveAddress));
            //jmp Exit
            CaveMemory.Write_StrBytes("EB 06");

            //Next:
            //fld dword ptr [_FloatZero_CaveAddress]
            CaveMemory.Write_StrBytes("D9 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_FloatZero_CaveAddress));

            //Inject it
            CaveMemory.InjectToOffset(_Buttons_InjectionStruct, "Buttons");
        }
        #endregion

        #region Outputs Hack

        protected override void Apply_OutputsMemoryHack()
        {
            Create_OutputsDataBank();
            _PlayerIsPlaying_CaveAddress = OutputsDatabank_Address;
            _WeaponCurrentAmmo_CaveAddress = OutputsDatabank_Address + 0x04;
            _WeaponCurrentAmmo2_CaveAddress = OutputsDatabank_Address + 0x08;
            _WeaponMaxAmmo_CaveAddress = OutputsDatabank_Address + 0x0C;

            SetHack_StartLevel();
            SetHack_UpdateOutputs();
            SetHack_EndLevel();

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        /// <summary>
        /// Intercept Start of level event to start generating recoil/ammo outputs
        /// </summary>
        private void SetHack_StartLevel()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //push eax
            CaveMemory.Write_StrBytes("50");
            //lea edx,[esp+04]
            CaveMemory.Write_StrBytes("8D 54 24 04");
            //mov [_PlayerIsPlaying_CaveAddress],1
            CaveMemory.Write_StrBytes("C6 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_PlayerIsPlaying_CaveAddress));
            CaveMemory.Write_StrBytes("01");

            //Inject it
            CaveMemory.InjectToOffset(_StartLevel_InjectionStruct, "EndLevel");
        }


        /// <summary>
        /// This procedure is looped when a level is running to check for inputs.
        /// </summary>
        private void SetHack_UpdateOutputs()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //push [ebp-000000A4]
            CaveMemory.Write_StrBytes("FF B5 5C FF FF FF");
            //push ebx
            CaveMemory.Write_StrBytes("53");
            //mov ebx,[eax+34]
            CaveMemory.Write_StrBytes("8B 58 34");
            //mov [_WeaponCurrentAmmo_CaveAddress],ebx
            CaveMemory.Write_StrBytes("89 1D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_WeaponCurrentAmmo_CaveAddress));
            //mov ebx,[eax+38]
            CaveMemory.Write_StrBytes("8B 58 38");
            //mov [_WeaponMaxAmmo_CaveAddress],ebx
            CaveMemory.Write_StrBytes("89 1D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_WeaponMaxAmmo_CaveAddress));
            //mov ebx,[eax+70]
            CaveMemory.Write_StrBytes("8B 58 70");
            //mov [_WeaponCurrentAmmo_CaveAddress],ebx
            CaveMemory.Write_StrBytes("89 1D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_WeaponCurrentAmmo2_CaveAddress));
            //pop ebx
            CaveMemory.Write_StrBytes("5B");

            //Inject it
            CaveMemory.InjectToOffset(_IsPlaying_InjectionStruct, "IsPlaying");
        }

        /// <summary>
        /// Intercept End of level event to stop generating recoil/ammo outputs
        /// </summary>
        private void SetHack_EndLevel()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //mov esi,[esp+6C]
            CaveMemory.Write_StrBytes("8B 74 24 6C");
            //mov mov eax,[esi+28]
            CaveMemory.Write_StrBytes("8B 46 28");
            //mov [_PlayerIsPlaying_CaveAddress],0
            CaveMemory.Write_StrBytes("C6 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_PlayerIsPlaying_CaveAddress));
            CaveMemory.Write_StrBytes("00");

            //Inject it
            CaveMemory.InjectToOffset(_EndLevel_InjectionStruct, "EndLevel");
        }

        #endregion

        #region No Crosshair Hack

        /// <summary>
        /// Force the game to believe cursor is disabled, to not render the drawing
        /// </summary>
        protected override void Apply_NoCrosshairMemoryHack()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //mov eax,[ebx+000000D4]
            CaveMemory.Write_StrBytes("8B 83 D4 00 00 00");
            //cmp dword ptr [_PlayerIsPlaying_CaveAddress],00
            CaveMemory.Write_StrBytes("83 3D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_PlayerIsPlaying_CaveAddress));
            CaveMemory.Write_StrBytes("00");
            //je OriginalCode
            CaveMemory.Write_StrBytes("74 02");
            //xor eax, eax
            CaveMemory.Write_StrBytes("31 C0");

            //Inject it
            CaveMemory.InjectToOffset(_NoCrosshair_InjectionStruct, "NoCrosshair");
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
        }

        protected override void GameController_OnButtonUp(object sender, VirtualGameControler.ButtonDownEventArgs e)
        { }

        protected override void UpdateAxis(PlayerID Id, int X, int Y)
        {
            if (Id == PlayerID.Player1)
            {
                byte[] bufferX = BitConverter.GetBytes((float)X);
                byte[] bufferY = BitConverter.GetBytes((float)Y);

                WriteBytes(_P1_X_CaveAddress, bufferX);
                WriteBytes(_P1_Y_CaveAddress, bufferY);
            }
        }

        protected override void PlayerController_OnButtonDown(object sender, VirtualGameControler.ButtonDownEventArgs e)
        {
            if (e.Id == PlayerID.Player1)
            {
                if (e.ButtonName.Equals("TRIGGER"))
                {
                    WriteByte(_P1_Trigger_CaveAddress, 0x01);
                    WriteByte(_P1_TriggerEventCountdown_CaveAddress, 0x02);
                }
                else if (e.ButtonName.Equals("RELOAD"))
                {
                    WriteByte(_P1_Reload_CaveAddress, 0x01);
                    WriteByte(_P1_ReloadEventCountdown_CaveAddress, 0x02);
                }
            }
        }
        protected override void PlayerController_OnButtonUp(object sender, VirtualGameControler.ButtonDownEventArgs e)
        {
            if (e.Id == PlayerID.Player1)
            {
                if (e.ButtonName.Equals("TRIGGER"))
                {
                    WriteByte(_P1_Trigger_CaveAddress, 0x00);
                    WriteByte(_P1_TriggerEventCountdown_CaveAddress, 0x00);
                }
                else if (e.ButtonName.Equals("RELOAD"))
                {
                    WriteByte(_P1_Reload_CaveAddress, 0x00);
                    WriteByte(_P1_ReloadEventCountdown_CaveAddress, 0x00);
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
                new GameOutput(OutputId.P1_Ammo),
                new AsyncGameOutput(OutputId.P1_CtmRecoil, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_ON_DELAY), ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_OFF_DELAY), 0),
                new GameOutput(OutputId.P1_GunMotor)
            };
        }

        /// <summary>
        /// Update all Outputs values before sending them to MameHooker
        /// </summary>
        public override void UpdateOutputValues()
        {
            int MotorState = 0;
            int P1_WeaponMaxAmmo = 0;
            _Ammo[0] = 0;

            int PlayerIsPlaying = ReadByte((_PlayerIsPlaying_CaveAddress));

            if (PlayerIsPlaying == 1)
            {
                _Ammo[0] = ReadInt32(_WeaponCurrentAmmo_CaveAddress);
                if (_Ammo[0] < 0)
                    _Ammo[0] = 0;

                //Max Ammo is 0 ==> Laser guns, activate rumble when trigger is pressed
                P1_WeaponMaxAmmo = ReadInt32(_WeaponMaxAmmo_CaveAddress);
                if (P1_WeaponMaxAmmo == 0)
                {
                    MotorState = ReadByte(_P1_Trigger_CaveAddress);
                    //Those weapons have different type of ammo counter
                    float fAmmo = ReadFloat(_WeaponCurrentAmmo2_CaveAddress);
                    _Ammo[0] = (int)fAmmo;
                }
                else
                {
                    //Bullets-firing gun : activate recoil when a munition is fired
                    if (P1_WeaponMaxAmmo == _P1_WeaponMaxAmmo_Last && _Ammo[0] < _LastAmmo[0])
                    {
                        SetOutputValue(OutputId.P1_CtmRecoil, 1);
                    }
                }
            }

            SetOutputValue(OutputId.P1_GunMotor, MotorState);
            SetOutputValue(OutputId.P1_Ammo, _Ammo[0]);

            _P1_WeaponMaxAmmo_Last = P1_WeaponMaxAmmo;
            _LastAmmo[0] = _Ammo[0];
        }

        #endregion
    }
}
