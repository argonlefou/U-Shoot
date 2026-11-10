using System;
using System.Collections.Generic;
using System.IO;
using System.Media;
using System.Reflection;
using UsCore.Config;
using UsCore.Launcher;
using UsCore.Logging;
using UsCore.MameOutput;
using UsCore.Memory;
using UsCore.VirtualControllers;

namespace U_Shoot.Games
{
    public class Game_WndHod2pc : BaseGame32
    {
        protected override string GAMEDATA_FOLDER { get { return @"Windows\hod2pc"; } }
        protected override bool PATCH_PEB_DEBUG_FLAG { get { return true; } }

        #region LOADER MEMORY ADDRESSES   
        private UInt32 _SetResolutionMode_Patch_Offset = 0x0009E462;
        private UInt32 _BloodColorTextureName_Offset = 0x00177B98;
        private UInt32 _Autoreload_Patch_Offset = 0x0009E6D2;
        private UInt32 _NoCrosshair_Patch_Offset = 0x00016ABE;
        private UInt32 _Language_Patch_Offset = 0x0;
        private NopStruct32 _Nop_DiskCheck = new NopStruct32(0x000A6841, 2);
        private NopStruct32 _Nop_ForceFullscreenHiddenSetting = new NopStruct32(0x000A4E11, 2);
        #endregion

        #region INPUTS MEMORY ADDRESSES    
        private UInt32 _Buttons_Offset = 0x005C9034;
        private UInt32 _P1_X_Offset = 0x005C8FC4;
        private UInt32 _P1_Y_Offset = 0x005C8FC6;
        private UInt32 _Credits_Offset = 0x005C8E60;
        private UInt32 _ControlsIniFunction_Offset = 0x0001E370;
        private NopStruct32 _Nop_X = new NopStruct32(0x0000D1AD, 4);
        private NopStruct32 _Nop_Y = new NopStruct32(0x0000D1B7, 4);
        private InjectionStruct32 _Buttons_Injectionstruct = new InjectionStruct32(0x0000D778, 6);

        //Custom Data
        private UInt32 _Buttons_CaveAddress;
        #endregion

        #region OUTPUTS MEMORY ADDRESSES
        private UInt32 _P1_Status_Offset = 0x005A5C62;
        private UInt32 _P1_Life_Offset = 0x005A5C66;
        private UInt32 _P1_Ammo_Offset = 0x005A5C7C;
        private UInt32 _P2_Status_Offset = 0x005A5D92;
        private UInt32 _P2_Life_Offset = 0x005A5D96;
        private UInt32 _P2_Ammo_Offset = 0x005A5DAC;
        #endregion

        //Play the "Coins" sound when adding coin
        SoundPlayer _SndPlayer;

        public Game_WndHod2pc(GameConfig Config) : base(Config)
        {
            _KnownMd5Prints.Add("The House of The dead II - PC-ISOZONE - ver.2001 / 05 / 09", "97c9e516a287aab33a455a396dadaa45");
            _KnownMd5Prints.Add("The House of The dead II - PC-ISOZONE (cracked) - ver.2001 / 05 / 09", "eb51d3856997581ed3aa8ecb7d6d8d07");
            _KnownMd5Prints.Add("The House of The dead II - PC - ver.0.995 (2000-12-13)", "a54ec23a78d07a78653bbd31919db0b5");
            _KnownMd5Prints.Add("The House of The dead II - PC - ver.2001 / 10 / 04", "258f350774317f785b430be32d2abe9a");
            _KnownMd5Prints.Add("The House of The Dead II - PC - ver.0.995 Japan", "3f316779a56e223454ca4b9239d82f90");
            _KnownMd5Prints.Add("The House of The dead II - PC - Japan ver.1.001 (2000-12-14)", "81050e6fd8c058b4453336ae4ba1aebe");
            _KnownMd5Prints.Add("The House of The dead II - PC - Unknown Release #1", "fd53bc12b72958c819cf6931787df3cb");

            //Try to load the "coin" sound
            try
            {
                String strCoinSndPath = _GameConfig.TargetPath.Substring(0, _GameConfig.TargetPath.Length - 8);
                strCoinSndPath += @"sound\SE\START_COIN\coin1_16.wav";
                if (File.Exists(strCoinSndPath))
                {
                    //Some dumps have mp3 sound files instead of wav, but SoundPlayer can't read them
                    _SndPlayer = new SoundPlayer(strCoinSndPath);
                }                
            }
            catch
            {
                Logger.WriteLog("Unable to find/open the coin1_16.wav file for Hotd2");
            }
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

                    double dMinX = -320.0;
                    double dMinY = -240.0;
                    double dMaxX = 320.0;
                    double dMaxY = 240.0;
                    double dRangeX = dMaxX - dMinX + 1;
                    double dRangeY = dMaxY - dMinY + 1;

                    PlayerController.Computed_X = Convert.ToInt16(Math.Round(dRangeX * PlayerController.Computed_X / TotalResX) + dMinX);
                    PlayerController.Computed_Y = Convert.ToInt16((Math.Round(dRangeY * PlayerController.Computed_Y / TotalResY) + dMinY) * -1);
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
            if (_GameConfig.Windowed)
            {
                //Game is using a resolution table, hardcoded to use 640x480 16Bits Fullscreen.
                //We can change the index to load a Windowed mode
                WriteByte(GameProcess_ModuleBaseAddress + _SetResolutionMode_Patch_Offset, 3); //Or 7 to use 32Bits color ?

                //And game is also reading hidden ini file (hglCore.ini) with a fullscreen option
                //Noping test to choose fullscreen mode if setting is set to 1
                SetNops(GameProcess_ModuleBaseAddress, _Nop_ForceFullscreenHiddenSetting);
            }

            //No CD patch, jmp over check
            SetNops(GameProcess_ModuleBaseAddress, _Nop_DiskCheck);

            //Changing the texture name used by the game from scr_blood_green.bin to scr_blood_red.bin
            if (_GameConfig.GetBoolOption("RedBlood"))
                ReplaceReadOnlyString(new UInt32[] { _BloodColorTextureName_Offset + 0x0A }, new byte[] { 0x72, 0x65, 0x64, 0x2E, 0x62, 0x69, 0x6E, 0x00, 0x00 });

            //Autoreload setting is read in ini file, disabling it            
            if (_GameConfig.GetBoolOption("NoAutoReload"))
            {
                //Only used in 1 release ??
                if (_TargetProcess_Md5Hash.Equals("97c9e516a287aab33a455a396dadaa45") || _TargetProcess_Md5Hash.Equals("eb51d3856997581ed3aa8ecb7d6d8d07")) 
                    WriteBytes(GameProcess_ModuleBaseAddress + _Autoreload_Patch_Offset, new byte[] { 0x31, 0xC0, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90 });
            }

            //For Japanese versions that yield error message box on european Windows
            //Other dumps do not check that
            if (_TargetProcess_Md5Hash.Equals("3f316779a56e223454ca4b9239d82f90") || _TargetProcess_Md5Hash.Equals("81050e6fd8c058b4453336ae4ba1aebe"))
                WriteByte(GameProcess_ModuleBaseAddress + _Language_Patch_Offset, 0xEB);

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }
        #endregion        

        #region Inputs Hack

        protected override void Apply_InputsMemoryHack()
        {
            Create_InputsDataBank();
            _Buttons_CaveAddress = InputsDatabank_Address;

            SetNops(GameProcess_ModuleBaseAddress, _Nop_X);
            SetNops(GameProcess_ModuleBaseAddress, _Nop_Y);

            SetHack_ControlsMode();
            SetHack_Buttons();

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        /// <summary>
        /// Because of different dumps handling differently how the player chooses controls (Keyboard / None / Mouse + Keyboard) in Config.exe tool
        /// Forcing the game to use KEYBOARD for both players
        /// Some dumps are working with all choices, others aren't
        /// </summary>
        private void SetHack_ControlsMode()
        {
            WriteBytes(GameProcess_ModuleBaseAddress + _ControlsIniFunction_Offset + 0x06, new byte[] { 0xB8, 0x01, 0x00, 0x00, 0x00, 0xEB, 0x3D, 0x90 });
            WriteBytes(GameProcess_ModuleBaseAddress + _ControlsIniFunction_Offset + 0x4F, new byte[] { 0xEB, 0x15, 0x90, 0x90 });
            WriteBytes(GameProcess_ModuleBaseAddress + _ControlsIniFunction_Offset + 0x6B, new byte[] { 0xEB, 0x11, 0x90 });
            WriteBytes(GameProcess_ModuleBaseAddress + _ControlsIniFunction_Offset + 0x83, new byte[] { 0xEB, 0x13, 0x90 });
            WriteBytes(GameProcess_ModuleBaseAddress + _ControlsIniFunction_Offset + 0x9D, new byte[] { 0xEB, 0x15, 0x90, 0x90, 0x90 });
        }


        /// <summary>
        /// Applying a bitmask to the current buttons state so that we can override needed ones while keeping others untouched (Up, Down, etc..)
        /// </summary>
        private void SetHack_Buttons()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);
            //and ecx,FFF1FFF1
            CaveMemory.Write_StrBytes("81 E1 F1 FF F1 FF");
            //or ecx,[_StartButtons_CaveAddress]
            CaveMemory.Write_StrBytes("0B 0D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Buttons_CaveAddress));
            //mov[Hod2.exe + _Buttons_Offset],ecx
            CaveMemory.Write_StrBytes("89 0D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _Buttons_Offset));

            //Inject
            CaveMemory.InjectToOffset(_Buttons_Injectionstruct, "Buttons");
        }

        #endregion

        #region Outputs Hack

        protected override void Apply_OutputsMemoryHack()
        {
            Create_OutputsDataBank();

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        #endregion

        #region NoCrosshair Hack
        protected override void Apply_NoCrosshairMemoryHack()
        {
            //Setting float Alpha to 0.0 instead of 1.0
            WriteBytes(GameProcess_ModuleBaseAddress + _NoCrosshair_Patch_Offset, new byte[] { 0x00, 0x00, 0x00, 0x00 });

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
                byte Credits = ReadByte(GameProcess_ModuleBaseAddress + _Credits_Offset);
                Credits++;
                WriteByte(GameProcess_ModuleBaseAddress + _Credits_Offset, Credits);
                if (_SndPlayer != null)
                    _SndPlayer.Play();
            }
        }

        protected override void GameController_OnButtonUp(object sender, VirtualGameControler.ButtonDownEventArgs e)
        {
        }

        protected override void UpdateAxis(PlayerID Id, int X, int Y)
        {
            if (Id != PlayerID.None)
            {
                //Axis are encoded into 2-Bytes in the Player Data struct, need to cast that as Int16
                byte[] bufferX = BitConverter.GetBytes((Int16)X);
                byte[] bufferY = BitConverter.GetBytes((Int16)Y);

                WriteBytes(GameProcess_ModuleBaseAddress + _P1_X_Offset + 0x28 * (uint)Id, bufferX);
                WriteBytes(GameProcess_ModuleBaseAddress + _P1_Y_Offset + 0x28 * (uint)Id, bufferY);
            }
        }

        protected override void PlayerController_OnButtonDown(object sender, VirtualGameControler.ButtonDownEventArgs e)
        {
            if (e.Id != PlayerID.None)
            {
                UInt32 PlayerIndex = (UInt32)e.Id;

                if (e.ButtonName.Equals("TRIGGER"))
                {
                    Apply_OR_ByteMask(_Buttons_CaveAddress + 2 * PlayerIndex, 0x04);
                }
                else if (e.ButtonName.Equals("RELOAD"))
                {
                    Apply_OR_ByteMask(_Buttons_CaveAddress + 2 * PlayerIndex, 0x02);
                }
                else if (e.ButtonName.Equals("START"))
                { 
                    Apply_OR_ByteMask(_Buttons_CaveAddress + 2* PlayerIndex, 0x08);
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
                    Apply_AND_ByteMask(_Buttons_CaveAddress + 2 * PlayerIndex, 0xFB);
                }
                else if (e.ButtonName.Equals("RELOAD"))
                {
                    Apply_AND_ByteMask(_Buttons_CaveAddress + 2 * PlayerIndex, 0xFD);
                }
                else if (e.ButtonName.Equals("START"))
                {
                    Apply_AND_ByteMask(_Buttons_CaveAddress + 2 * PlayerIndex, 0xF7);
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
                new SyncBlinkingGameOutput(OutputId.P1_CtmLmpStart, 500),
                new SyncBlinkingGameOutput(OutputId.P2_CtmLmpStart, 500),
                new GameOutput(OutputId.P1_Ammo),
                new GameOutput(OutputId.P2_Ammo),
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
            //Player status :
            //[4] = Continue Screen
            //[5] = InGame
            //[6] = Game Over
            //[7] = Time attack Scoreboard
            //[9] = Menu or Attract Mode
            // We will use these values to compute ourselve Recoil and P1/P2 Start Button Lights
            UInt32 P1_Status = ReadByte(GameProcess_ModuleBaseAddress + _P1_Status_Offset);
            UInt32 P2_Status = ReadByte(GameProcess_ModuleBaseAddress + _P2_Status_Offset);
            for (int i = 0; i < _GameConfig.PlayerCount; i++)
            {
                _Life[i] = 0;
                _Ammo[i] = 0;
            }

            if (P1_Status == 5)
            {
                //Force Start Lamp to Off
                SetOutputValue(OutputId.P1_CtmLmpStart, 0);

                _Life[0] = ReadByte(GameProcess_ModuleBaseAddress + _P1_Life_Offset);
                _Ammo[0] = ReadByte(GameProcess_ModuleBaseAddress + _P1_Ammo_Offset);

                //Custom Recoil
                if (_Ammo[0] < _LastAmmo[0])
                    SetOutputValue(OutputId.P1_CtmRecoil, 1);                

                //[Damaged] custom Output                
                if (_Life[0] < _LastLife[0])
                    SetOutputValue(OutputId.P1_Damaged, 1);
            }
            else
            {
                //Enable Start Lamp Blinking
                SetOutputValue(OutputId.P1_CtmLmpStart, -1);
            }

            if (P2_Status == 5)
            {
                //Force Start Lamp to Off
                SetOutputValue(OutputId.P2_CtmLmpStart, 0);

                _Life[1] = ReadByte(GameProcess_ModuleBaseAddress + _P2_Life_Offset);
                _Ammo[1] = ReadByte(GameProcess_ModuleBaseAddress + _P2_Ammo_Offset);

                //Custom Recoil
                if (_Ammo[1] < _LastAmmo[1])
                    SetOutputValue(OutputId.P2_CtmRecoil, 1);

                //[Damaged] custom Output                
                if (_Life[1] < _LastLife[1])
                    SetOutputValue(OutputId.P2_Damaged, 1);
            }
            else
            {
                //Enable Start Lamp Blinking
                SetOutputValue(OutputId.P2_CtmLmpStart, -1);
            }

            for (uint i = 0; i < _GameConfig.PlayerCount; i++)
            {
                _LastLife[i] = _Life[i];
                _LastAmmo[i] = _Ammo[i];

                SetOutputValue(OutputId.P1_Ammo + i, _Ammo[i]);
                SetOutputValue(OutputId.P1_Life + i, _Life[i]);                
            }
            SetOutputValue(OutputId.Credits, ReadByte(GameProcess_ModuleBaseAddress + _Credits_Offset));
        }

        #endregion
    }
}
