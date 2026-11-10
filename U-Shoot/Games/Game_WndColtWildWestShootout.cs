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
    public class Game_WndColtWildWestShootout : BaseGame32
    {
        protected override bool PATCH_PEB_DEBUG_FLAG { get { return true; } }

        #region LOADER MEMORY ADDRESSES                
        private UInt32 _DiscCheck_Patch_Offset = 0x0000115B;
        InjectionStruct32 _NoCrosshair_InjectionStruct = new InjectionStruct32(0x000071D9, 5);
        private UInt32 _NoGun_InstructionOffset = 0x000079F0;
        private UInt32 _P1_CrosshairId_Offset = 0x0006AF88;

        #endregion

        #region INPUTS MEMORY ADDRESSES    
        private UInt32 _P1_X_Offset = 0x00022030;
        private UInt32 _P1_Y_Offset = 0x00022034;
        private UInt32 _P1_Buttons_Offset = 0x00093B68;
        private NopStruct32 _Nop_X = new NopStruct32(0x00009AB4, 6);
        private NopStruct32 _Nop_Y = new NopStruct32(0x00009ABA, 6);
        private UInt32 _MouseControlMessage_Patch_Offset = 0x0000C480;
        #endregion

        #region OUTPUTS MEMORY ADDRESSES
        private UInt32 _P1_IsDead_Offset = 0x000924BC;
        private UInt32 _P1_Life_Offset = 0x000924AC;
        private UInt32 _P1_Weapon_Offset = 0x000924D8;
        private UInt32 _P1_Ammo_Offset = 0x000924E0;
        #endregion

        private int _P1_LastWeapon = 0;

        public Game_WndColtWildWestShootout(GameConfig Config) : base(Config)
        {
            _KnownMd5Prints.Add("Colt's Wild West Shootout", "97c9e516a287aab33a455a396dadaa45");
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
                    double dMaxX = 320.0;
                    double dMaxY = 240.0;
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
            //Game is looking for a disc drive with the CD name, returning the drive numver or (-1) if not found. Just forcing it to something
            //The game normaly uses that value to load files from the disc when they are not in the install folder, copying all files in the install folder will fix that
            WriteBytes(GameProcess_ModuleBaseAddress + _DiscCheck_Patch_Offset, new byte[] { 0xB8, 0x03, 0x00, 0x000 }); //mov eax,00000003

            if (_GameConfig.NoGun)
                Apply_GunMod();

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        private void Apply_GunMod()
        {
            WriteBytes(GameProcess_ModuleBaseAddress + _NoGun_InstructionOffset, new byte[] { 0xE9, 0x8E, 0x00, 0x00, 0x00 });
        }
        #endregion        

        #region Inputs Hack

        protected override void Apply_InputsMemoryHack()
        {
            SetNops(GameProcess_ModuleBaseAddress, _Nop_X);
            SetNops(GameProcess_ModuleBaseAddress, _Nop_Y);

            //Jump over the switch case handling MOUSE BUTTONS event
            WriteBytes(GameProcess_ModuleBaseAddress + _MouseControlMessage_Patch_Offset, new byte[] { 0x31, 0xC0, 0x90, 0x90, 0x90, 0x90 });

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        #endregion

        #region NoCrosshair Hack

        protected override void Apply_NoCrosshairMemoryHack()
        {
            Apply_CrosshairMod();
            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        /// <summary>
        /// Crosshair ID is used to change sprite:
        /// 1 = Star
        /// 2 = Exit
        /// 3 = Reticle
        /// Changing it to 0 displays nothing, instead of the "3"
        /// </summary>
        private void Apply_CrosshairMod()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //mov eax,["Wild West Shootout.exe" + _P1_CrosshairId_Offset]
            CaveMemory.Write_StrBytes("8B 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _P1_CrosshairId_Offset));
            //cmp eax,03
            CaveMemory.Write_StrBytes("83 F8 03");
            //jne exit
            CaveMemory.Write_StrBytes("75 02");
            //xor eax,eax
            CaveMemory.Write_StrBytes("31 C0");

            //Inject it
            CaveMemory.InjectToOffset(_NoCrosshair_InjectionStruct, "No Crosshair");
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
        {
        }

        protected override void UpdateAxis(PlayerID Id, int X, int Y)
        {
            if (Id == PlayerID.Player1)
            {
                byte[] bufferX = BitConverter.GetBytes(X);
                byte[] bufferY = BitConverter.GetBytes(Y);

                WriteBytes(GameProcess_ModuleBaseAddress + _P1_X_Offset, bufferX);
                WriteBytes(GameProcess_ModuleBaseAddress + _P1_Y_Offset, bufferY);
            }
        }

        protected override void PlayerController_OnButtonDown(object sender, VirtualGameControler.ButtonDownEventArgs e)
        {
            if (e.Id == PlayerID.Player1)
            {
                if (e.ButtonName.Equals("TRIGGER"))
                {
                    Apply_OR_ByteMask(GameProcess_ModuleBaseAddress + _P1_Buttons_Offset, 0x01);                        
                }
                else if (e.ButtonName.Equals("RELOAD"))
                {
                    Apply_OR_ByteMask(GameProcess_ModuleBaseAddress + _P1_Buttons_Offset, 0x02);
                }
            }
        }
        protected override void PlayerController_OnButtonUp(object sender, VirtualGameControler.ButtonDownEventArgs e)
        {
            if (e.Id == PlayerID.Player1)
            {
                if (e.ButtonName.Equals("TRIGGER"))
                {
                    Apply_AND_ByteMask(GameProcess_ModuleBaseAddress + _P1_Buttons_Offset, 0xFE);
                }
                else if (e.ButtonName.Equals("RELOAD"))
                {
                    Apply_AND_ByteMask(GameProcess_ModuleBaseAddress + _P1_Buttons_Offset, 0xFD);
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
            _Outputs = new List<GameOutput>();
            _Outputs = new List<GameOutput>
            {
                new SyncBlinkingGameOutput(OutputId.P1_CtmLmpStart, 500),
                new GameOutput(OutputId.P1_Ammo),
                new AsyncGameOutput(OutputId.P1_CtmRecoil, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_ON_DELAY), ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_OFF_DELAY), 0),
                new GameOutput(OutputId.P1_Life),
                new AsyncGameOutput(OutputId.P1_Damaged, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_DAMAGED_DELAY), 100, 0),
            };
        }

        /// <summary>
        /// Update all Outputs values before sending them to MameHooker
        /// </summary>
        public override void UpdateOutputValues()
        {
            _Life[0] = 0;
            _Ammo[0] = 0;
            int P1_CurrentWeapon = 0;

            if (ReadByte(GameProcess_ModuleBaseAddress + _P1_IsDead_Offset) == 0)
            {
                _Life[0] = ReadByte(GameProcess_ModuleBaseAddress + _P1_Life_Offset);

                //Force Start Lamp to Off
                SetOutputValue(OutputId.P1_CtmLmpStart, 0);

                _Ammo[0] = ReadByte(GameProcess_ModuleBaseAddress + _P1_Ammo_Offset);

                //Different weapons can change max ammo to have a lesser ammo count and cause recoil
                P1_CurrentWeapon = ReadByte(GameProcess_ModuleBaseAddress + _P1_Weapon_Offset);
                if (P1_CurrentWeapon == _P1_LastWeapon)
                {
                    //Custom Recoil
                    if (_Ammo[0] < _LastAmmo[0])
                        SetOutputValue(OutputId.P1_CtmRecoil, 1);
                }

                //[Damaged] custom Output                
                if (_Life[0] < _LastLife[0])
                    SetOutputValue(OutputId.P1_Damaged, 1);
            }
            else
            {
                //Enable Start Lamp Blinking
                SetOutputValue(OutputId.P1_CtmLmpStart, -1);
            }

            _LastAmmo[0] = _Ammo[0];
            _LastLife[0] = _Life[0];
            _P1_LastWeapon = P1_CurrentWeapon;

            SetOutputValue(OutputId.P1_Ammo, _Ammo[0]);
            SetOutputValue(OutputId.P1_Life, _Life[0]);
        }

        #endregion
    }
}
