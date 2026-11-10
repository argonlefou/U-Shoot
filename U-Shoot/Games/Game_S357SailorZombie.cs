using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UsCore.Config;
using UsCore.Launcher;
using UsCore.Logging;
using UsCore.MameOutput;
using UsCore.VirtualControllers;

namespace U_Shoot.Games
{
    public class Game_S357SailorZombie : BaseGame64
    {
        protected override bool PATCH_PEB_DEBUG_FLAG { get { return false; } }

        #region LOADER MEMORY ADDRESSES                
        private UInt64 _RomLoaded_Check_Address = 0x300D92328;
        #endregion

        #region INPUTS MEMORY ADDRESSES    
        private UInt64 _P1_Axis_Address = 0x336CA6E00;
        private UInt64 _P2_Axis_Address = 0x336CA6E28;
        private UInt64 _Buttons_Address = 0x300D91B1D;
        private UInt64 _Freeplay_Address = 0x336CA0EB3;
        #endregion

        #region OUTPUTS MEMORY ADDRESSES
        private UInt64 _Outputs_Address = 0x300D92248;
        private UInt64 _Credits_Address = 0x300D91AEB;

        //Custom Data
        private int _P1_LastRumble = 0;
        private int _P2_LastRumble = 0;
        #endregion

        private string _PPU_CachePath = string.Empty;
        private string _CPU_Type = string.Empty;

        public Game_S357SailorZombie(GameConfig Config) : base(Config)
        {
            _KnownMd5Prints.Add("RPCS3 v0.0.26 fork for System 357", "5c2c7918262a8e32a6a82659ab0fb365");
            _KnownMd5Prints.Add("RPCS3 v0.0.27 fork for System 357, GUN version", "3321f7771ae74e8027f0d4e18167d635");

            if (!_GameConfig.NoInputs)
            {
                _PPU_CachePath = Path.Combine(Path.GetDirectoryName(_GameConfig.TargetPath), "cache", "SCEEXE000", "ppu-se1PtZVS5iF9A6M5Y1vu0wNqiASu-EBOOT.BIN");
                if (!Directory.Exists(_PPU_CachePath))
                {
                    Logger.WriteLog("PPU cache not created, please run the emulator once to create PPU cache before using U-Shoot");
                    Environment.Exit(0);
                }

                Read_CPU_Type();
                if (string.IsNullOrEmpty(_CPU_Type))
                {
                    Logger.WriteLog("Impossible to detect CPU family name from existing PPU cache !");
                }
                Logger.WriteLog("LLVM PPU processor is " + _CPU_Type);

                Check_PPU_Cache();
            }

            _GameLauncher.Add_CmdLine_Arg(Path.Combine(Path.GetDirectoryName(_GameConfig.TargetPath), "dev_hdd0", "game", "SCEEXE000", "USRDIR", "EBOOT.BIN"), true);
            _GameLauncher.Add_CmdLine_Arg("--no-gui");
        }

        #region PPU cache check

        //PPU cache is named after the CPU familly. Getting that will help to make sure the PPU is correctly patched
        private void Read_CPU_Type()
        {
            foreach (string File in Directory.GetFiles(_PPU_CachePath))
            {
                string sFile = Path.GetFileName(File);

                if (sFile.StartsWith("v5-kusa-aQwdyxfu7HwmKZwJpsrgjQ-00001G"))
                {
                    try
                    {
                        _CPU_Type = sFile.Split(new string[] { ".obj.gz" }, StringSplitOptions.None)[0].Split(new string[] { "0001G-" }, StringSplitOptions.None)[1];
                    }
                    catch
                    {
                        Logger.WriteLog("Error : Impossible to detect LLVM CPU type");
                    }
                    if (_CPU_Type != string.Empty)
                        break;
                }
            }
        }

        //Check if pre-patched PPU cache files are present in the cache folder
        //If not, the game won't be able to be control by DemulShooter
        private void Check_PPU_Cache()
        {
            foreach (FileInfo file in new DirectoryInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Properties.Resources.DATA_PAYLOADS_FOLDER, "RPCS3", "SailorZombie")).GetFiles())
            {
                string AwaitedMd5 = GetMd5HashAsString(file.FullName);
                string TargetCacheFile = Path.Combine(_PPU_CachePath, file.Name + _CPU_Type + ".obj.gz");

                Logger.WriteLog("Checking PPU cache file : " + TargetCacheFile);
                if (File.Exists(TargetCacheFile))
                {
                    string TargetMd5 = GetMd5HashAsString(TargetCacheFile);
                    if (AwaitedMd5.Equals(TargetMd5))
                    {
                        Logger.WriteLog(" -> MD5 Check OK");
                    }
                    else
                    {
                        Logger.WriteLog(" -> MD5 Check FAIL, trying to replace file...");
                        try
                        {
                            file.CopyTo(TargetCacheFile, true);
                        }
                        catch (Exception Ex)
                        {
                            Logger.WriteLog("Error trying to copy : " + Ex.Message.ToString());
                        }
                    }
                }
            }
        }

        #endregion

        protected override void ProcessTimerTick_Unhooked_CheckProc()
        {
            try
            {
                //To make sure RPCS3 has loaded the rom entirely, we're looking for some random instruction to be present in memory before starting
                byte[] buffer = ReadBytes(_RomLoaded_Check_Address, 10);
                if (Encoding.ASCII.GetString(buffer).Equals("NBGI.;USIO"))
                {
                    Logger.WriteLog("Game loaded");
                    CheckExeMd5();
                    ReadGameDataFromMd5Hash(GAMEDATA_FOLDER);
                    RaiseGameHookedEvent();
                }
                else
                {
                    Logger.WriteLog("Game not Loaded, waiting...");
                    return;
                }
            }
            catch (Exception Ex)
            {
                Logger.WriteLog("Error waiting for the game to be loaded : " + Ex.Message);
            }
        }

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
                    double dMaxX = 1919.0;
                    double dMaxY = 1079.0;
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

        #region Inputs

        protected override void GameController_OnButtonDown(object sender, VirtualGameControler.ButtonDownEventArgs e)
        {
            if (e.ButtonName.Equals("EXIT"))
            {
                Close();
            }
            else if (e.ButtonName.Equals("COIN"))
            {
                if (ReadByte(_Freeplay_Address) == 0)
                {
                byte bCredits = ReadByte(_Credits_Address);
                if (bCredits < 98)
                    WriteByte(_Credits_Address, ++bCredits);
                }
            }
            else if (e.ButtonName.Equals("TEST"))
            {
                if (ReadByte(_Buttons_Address + 7) == 0)
                    WriteByte(_Buttons_Address + 7, 0x80);
                else
                    WriteByte(_Buttons_Address + 7, 0x00);
            }
            else if (e.ButtonName.Equals("SERVICE"))
            {
                Apply_OR_ByteMask(_Buttons_Address, 0x40);
            }
            else if (e.ButtonName.Equals("UP"))
            {
                Apply_OR_ByteMask(_Buttons_Address, 0x20);
            }
            else if (e.ButtonName.Equals("DOWN"))
            {
                Apply_OR_ByteMask(_Buttons_Address, 0x10);
            }
            else if (e.ButtonName.Equals("ENTER"))
            {
                Apply_OR_ByteMask(_Buttons_Address, 0x02);
            }
        }

        protected override void GameController_OnButtonUp(object sender, VirtualGameControler.ButtonDownEventArgs e)
        {
            if (e.ButtonName.Equals("SERVICE"))
            {
                Apply_AND_ByteMask(_Buttons_Address, 0xBF);
            }
            else if (e.ButtonName.Equals("UP"))
            {
                Apply_AND_ByteMask(_Buttons_Address, 0xDF);
            }
            else if (e.ButtonName.Equals("DOWN"))
            {
                Apply_AND_ByteMask(_Buttons_Address, 0xEF);
            }
            else if (e.ButtonName.Equals("ENTER"))
            {
                Apply_AND_ByteMask(_Buttons_Address, 0xFD);
            }
        }

        protected override void UpdateAxis(PlayerID Id, int X, int Y)
        {
            if (Id != PlayerID.None)
            {
                byte[] bufferX = BitConverter.GetBytes((UInt16)X);
                byte[] bufferY = BitConverter.GetBytes((UInt16)Y);

                Array.Reverse(bufferX);
                Array.Reverse(bufferY);

                if (Id == PlayerID.Player1)
                {
                    WriteBytes(_P1_Axis_Address, bufferX);
                    WriteBytes(_P1_Axis_Address + 2, bufferY);
                }
                else if (Id == PlayerID.Player2)
                {
                    WriteBytes(_P2_Axis_Address, bufferX);
                    WriteBytes(_P2_Axis_Address + 2, bufferY);
                }
            }
        }

        protected override void PlayerController_OnButtonDown(object sender, VirtualGameControler.ButtonDownEventArgs e)
        {
            if (e.Id != PlayerID.None)
            {
                UInt32 PlayerIndex = (UInt32)e.Id;

                if (e.ButtonName.Equals("TRIGGER LEFT"))
                {
                    int iMask = 0x80 >> ((int)PlayerIndex * 3);
                    Apply_OR_ByteMask(_Buttons_Address + 1, (byte)iMask);
                }
                else if (e.ButtonName.Equals("TRIGGER RIGHT"))
                {
                    int iMask = 0x40 >> ((int)PlayerIndex * 3);
                    Apply_OR_ByteMask(_Buttons_Address + 1, (byte)iMask);
                }
                else if (e.ButtonName.Equals("START"))
                {
                    int iMask = 0x20 >> ((int)PlayerIndex * 3);
                    Apply_OR_ByteMask(_Buttons_Address + 1, (byte)iMask);
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
                    int iMask = 0x80 >> ((int)PlayerIndex * 3);
                    Apply_AND_ByteMask(_Buttons_Address + 1, (byte)~iMask);
                }
                else if (e.ButtonName.Equals("TRIGGER RIGHT"))
                {
                    int iMask = 0x40 >> ((int)PlayerIndex * 3);
                    Apply_AND_ByteMask(_Buttons_Address + 1, (byte)~iMask);
                }
                else if (e.ButtonName.Equals("START"))
                {
                    int iMask = 0x20 >> ((int)PlayerIndex * 3);
                    Apply_AND_ByteMask(_Buttons_Address + 1, (byte)~iMask);
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
                new GameOutput(OutputId.P1_GunMotor),
                new GameOutput(OutputId.P2_GunMotor),
                new GameOutput(OutputId.P1_LmpStart),
                new GameOutput(OutputId.P2_LmpStart),
                new GameOutput(OutputId.LmpRoom),
                //
                new GameOutput(OutputId.P1_AirFront),
                new GameOutput(OutputId.P2_AirFront),
                new GameOutput(OutputId.VibrationSeat),
                //Custom Outputs
                new AsyncGameOutput(OutputId.P1_CtmRecoil, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_ON_DELAY), ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_OFF_DELAY), 0),
                new AsyncGameOutput(OutputId.P2_CtmRecoil, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_ON_DELAY), ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_OFF_DELAY), 0),
                /*_Outputs.Add(new GameOutput(OutputId.P1_Life));
                _Outputs.Add(new GameOutput(OutputId.P2_Life));
                _Outputs.Add(new AsyncGameOutput(OutputId.P1_Damaged, MameOutputHelper.CustomDamageDelay, 100, 0));
                _Outputs.Add(new AsyncGameOutput(OutputId.P2_Damaged, MameOutputHelper.CustomDamageDelay, 100, 0));*/
                new GameOutput(OutputId.Credits)
            };
        }

        /// <summary>
        /// Update all Outputs values before sending them to MameHooker
        /// </summary>
        public override void UpdateOutputValues()
        {
            //LEDs
            byte bLEDs = ReadByte(_Outputs_Address + 1);
            SetOutputValue(OutputId.P1_LmpStart, bLEDs >> 7 & 0x01);
            SetOutputValue(OutputId.P2_LmpStart, bLEDs >> 6 & 0x01);
            SetOutputValue(OutputId.LmpRoom, ReadByte(_Outputs_Address + 18) & 0x0F);  //Analog ? Goes from 0xF0 to 0xFF and back to F0
            //Other
            byte bMech = ReadByte((IntPtr)_Outputs_Address);
            SetOutputValue(OutputId.P1_AirFront, bMech >> 3 & 0x01);
            SetOutputValue(OutputId.P2_AirFront, bMech >> 2 & 0x01);

            //Seat vibration is coded on 2 Bits, and can have 4 Values (OFF/LOW/MEDIUM/HIGH)
            int SeatVbr = bLEDs >> 1 & 0x03;
            if (SeatVbr == 0)
                SetOutputValue(OutputId.VibrationSeat, 0);
            else if (SeatVbr == 1)
                SetOutputValue(OutputId.VibrationSeat, 2);
            else if (SeatVbr == 2)
                SetOutputValue(OutputId.VibrationSeat, 1);
            else if (SeatVbr == 3)
                SetOutputValue(OutputId.VibrationSeat, 3);

            //Rumble is using bit-changing values on a bi-stable state (not monostable) on the higher 4bits of the byte 
            // [Bit3] [Bit2] [Bit1] [Bit0] 
            //P1 rumble activation swap 1 from Bit3/Bit2 (i.e : 8 -> 7-> 8-> 7) 
            //P2 rumble activation swap 1 from Bit1/Bit0 (i.e : 1 -> 0-> 1-> 0) 
            int P1_Rumble = bMech & 0xC0;
            int P2_Rumble = bMech & 0x30;

            if (_P1_LastRumble != 0 && P1_Rumble != _P1_LastRumble && P1_Rumble != 0)
            {
                SetOutputValue(OutputId.P1_GunMotor, 1);
                SetOutputValue(OutputId.P1_CtmRecoil, 1);
            }
            else
                SetOutputValue(OutputId.P1_GunMotor, 0);

            if (_P2_LastRumble != 0 && P2_Rumble != _P2_LastRumble && P2_Rumble != 0)
            {
                SetOutputValue(OutputId.P2_GunMotor, 1);
                SetOutputValue(OutputId.P2_CtmRecoil, 1);
            }
            else
                SetOutputValue(OutputId.P2_GunMotor, 0);

            _P1_LastRumble = P1_Rumble;
            _P2_LastRumble = P2_Rumble;

            SetOutputValue(OutputId.Credits, ReadByte(_Credits_Address));
        }

        #endregion
    }
}
