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
    public class Game_Lindbergh2Spicy : BaseGame32
    {
        protected override string GAMEDATA_FOLDER { get { return @"Lindbergh\2spicy"; } }
        protected override bool PATCH_PEB_DEBUG_FLAG { get { return false; } }

        private const string TOOSPICY_GAME_DESC = "2 Spicy (SBMV)";
        private const string TOOSPICY_TEST_DESC = "2 Spicy (SBMV) TEST Menu";

        private const string ELFPATH_OPTION = "Game ELF";
        private const string LOADEROPTIONS_OPTION = "LinuxLoader options";
        private const string TESTMENU_OPTION = "TEST Menu";

        #region LOADER MEMORY ADDRESSES                
        //Check instruction for game loaded
        private UInt32 _GameLoaded_Check_Address = 0x082EFC63;
        private UInt32 _TestMenuLoaded_Check_Address = 0x80564A8;
        #endregion

        #region INPUTS MEMORY ADDRESSES    
        private InjectionStruct32 _JvsRawAxes_InjectionStruct = new InjectionStruct32(0x0831F80D, 7);
        //private InjectionStruct _AdjustedAxes_InjectionStruct = new InjectionStruct(????, 6);
        private InjectionStruct32 _Buttons_InjectionStruct = new InjectionStruct32(0x0831F5CC, 7);
        private InjectionStruct32 _Coins_InjectionStruct = new InjectionStruct32(0x0831F6F6, 7);

        //Custom Data
        private UInt32 _JvsRawAxes_CaveAddress;
        private UInt32 _AdjustedAxes_CaveAddress;
        private UInt32 _Buttons_CaveAddress;
        private UInt32 _Coins_CaveAddress;
        #endregion

        #region OUTPUTS MEMORY ADDRESSES
        private UInt32 _OutputsPtr_Address = 0x0A89F944;
        private UInt32 _Credits_Address = 0x0C8C0240;
        private UInt32 _PlayerStructPtr_Address = 0x0867B10C;
        private UInt32 _AmmoPtr_Address = 0x0888F8F8;
        #endregion

        public Game_Lindbergh2Spicy(GameConfig Config) : base(Config)
        {
            string ElfPath = _GameConfig.GetStringOption(ELFPATH_OPTION + " [apacheM.elf]");

            if (_GameConfig.GetBoolOption(TESTMENU_OPTION))
                ElfPath = ElfPath.Replace("apacheM.elf", "apachetestM.elf");

            _GameLauncher.Set_CmdLine_Args("\"" + ElfPath + "\" " + _GameConfig.GetStringOption(LOADEROPTIONS_OPTION));

            _KnownMd5Prints.Add(TOOSPICY_GAME_DESC, "b2183415493f9901dd45197364d51ebb");
            _KnownMd5Prints.Add(TOOSPICY_TEST_DESC, "d4151db64a6945f8d4fdf1ba5022e9fb");

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
                //Protocol will be different depending on the Elf loaded (Game or TestMenu)
                if (_GameConfig.GetBoolOption(TESTMENU_OPTION))
                {
                    byte[] buffer = ReadBytes(_TestMenuLoaded_Check_Address, 3);
                    if (buffer.SequenceEqual(new byte[] { 0x55, 0x8B, 0xEC }))
                    {
                        Logger.WriteLog(TOOSPICY_TEST_DESC + " binary detected");
                        _TargetProcess_Md5Hash = _KnownMd5Prints[TOOSPICY_TEST_DESC];
                    }
                    else
                    {
                        Logger.WriteLog("Game not Loaded, waiting...");
                        return;
                    }
                }
                else
                {
                    byte[] buffer = ReadBytes(_GameLoaded_Check_Address, 3);
                    if (buffer.SequenceEqual(new byte[] { 0xB8, 0x30, 0x24 }))
                    {
                        Logger.WriteLog(TOOSPICY_GAME_DESC + " binary detected");
                        //So far, all binaries are sharing the same code so no Memory Files are needed
                        //_TargetProcess_Md5Hash = _KnownMd5Prints[TOOSPICY_GAME_DESC];
                    }
                    else
                    {
                        Logger.WriteLog("Game not Loaded, waiting...");
                        return;
                    }
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

        #region Inputs Hack

        protected override void Apply_InputsMemoryHack()
        {
            Create_InputsDataBank();
            _JvsRawAxes_CaveAddress = InputsDatabank_Address;
            _AdjustedAxes_CaveAddress = InputsDatabank_Address + 0x10;
            _Buttons_CaveAddress = InputsDatabank_Address + 0x20;
            _Coins_CaveAddress = InputsDatabank_Address + 0x28;

            SetHack_JvsRawAxes();

            //This one is different and does not have the usual lindbergh shooter function 
            //SetHack_AdjustedAxes();

            SetHack_Buttons();
            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        /// <summary>
        /// At the end of amJvspAckAnalogInput(), replacing Axis value before memory copy
        /// </summary>
        private void SetHack_JvsRawAxes()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //lea ecx,[ebx-9]
            CaveMemory.Write_StrBytes("8D 4B F7");
            //movzx ecx,word ptr [ecx+_Axes_CaveAddress]
            CaveMemory.Write_StrBytes("0F B6 89");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_JvsRawAxes_CaveAddress));
            //lea ebx,[ebx+esi+00000102]
            CaveMemory.Write_StrBytes("8D 9C 33 02 01 00 00");
            //mov [ebx],cx
            CaveMemory.Write_StrBytes("66 89 0B");

            //Inject it
            CaveMemory.InjectToAddress(_JvsRawAxes_InjectionStruct, "Axes");
        }

        /// <summary>
        /// At the end of amJvspAckSwInput(), overriding buttons bytes before memory copy
        /// TEST byte (EBX = 5) is one-byte copy
        /// Other data is 2-bytes copy
        /// </summary>
        private void SetHack_Buttons()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //lea eax,[edx+esi+00000102]
            CaveMemory.Write_StrBytes("8D 84 32 02 01 00 00");

            //cmp esi,05
            CaveMemory.Write_StrBytes("83 FE 05");
            //je TestByte
            CaveMemory.Write_StrBytes("74 0A");
            //movzx ecx,word ptr [esi+_Buttons_CaveAddress]
            CaveMemory.Write_StrBytes("0F B7 8E");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Buttons_CaveAddress));
            //mov [eax],cx
            CaveMemory.Write_StrBytes("66 89 08");
            //TestByte:
            //movzx ecx,byte ptr [esi+_Buttons_CaveAddress]
            CaveMemory.Write_StrBytes("0F B6 8E");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Buttons_CaveAddress));
            //mov [eax],cl
            CaveMemory.Write_StrBytes("88 08");

            //Inject it
            CaveMemory.InjectToAddress(_Buttons_InjectionStruct, "Buttons");
        }

        /// <summary>
        /// At the end of amJvspAckCoinInput(), overriding COIN value bytes before memory copy
        /// </summary>
        private void SetHack_Coins()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //lea ecx,[eax-1C]
            CaveMemory.Write_StrBytes("8D 48 E4");
            //add ecx,_Coins_CaveAddress
            CaveMemory.Write_StrBytes("81 C1");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Coins_CaveAddress));
            //movzx ecx,word ptr [ecx]
            CaveMemory.Write_StrBytes("0F B7 09");
            //lea eax,[eax+edi+00000102]
            CaveMemory.Write_StrBytes("8D 84 38 02 01 00 00");
            //mov [eax],cx
            CaveMemory.Write_StrBytes("66 89 08");

            //Inject it
            CaveMemory.InjectToAddress(_Coins_InjectionStruct, "Coins");
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
            else if (e.ButtonName.Equals("COIN MECH. 1"))
            {
                byte bCoinValue = ReadByte(_Coins_CaveAddress + 1);
                WriteByte(_Coins_CaveAddress + 1, (byte)(bCoinValue + 1));
            }
            else if (e.ButtonName.Equals("COIN MECH. 2"))
            {
                byte bCoinValue = ReadByte(_Coins_CaveAddress + 3);
                WriteByte(_Coins_CaveAddress + 3, (byte)(bCoinValue + 1));
            }
            else if (e.ButtonName.Equals("SERVICE"))
            {
                Apply_OR_ByteMask(_Buttons_CaveAddress + 6, 0x40);
            }
            else if (e.ButtonName.Equals("TEST"))
            {
                Apply_OR_ByteMask(_Buttons_CaveAddress + 5, 0x80);
            }
        }

        protected override void GameController_OnButtonUp(object sender, VirtualGameControler.ButtonDownEventArgs e)
        {
            if (e.ButtonName.Equals("SERVICE"))
            {
                Apply_AND_ByteMask(_Buttons_CaveAddress + 6, 0xBF);
            }
            else if (e.ButtonName.Equals("TEST"))
            {
                Apply_AND_ByteMask(_Buttons_CaveAddress + 5, 0x7F);
            }
        }

        protected override void UpdateAxis(PlayerID Id, int X, int Y)
        {
            if (Id != PlayerID.None)
            {
                //First channels of analog inputs in JVS are RawPosition (0x00-0xFF) for each players (Bytes 0-7)
                WriteByte(_JvsRawAxes_CaveAddress + ((uint)Id * 4), (byte)X);
                WriteByte(_JvsRawAxes_CaveAddress + 2 + ((uint)Id * 4), (byte)Y);
            }
        }

        protected override void PlayerController_OnButtonDown(object sender, VirtualGameControler.ButtonDownEventArgs e)
        {
            if (e.Id != PlayerID.None)
            {
                UInt32 PlayerIndex = (UInt32)e.Id;

                if (e.ButtonName.Equals("TRIGGER"))
                {
                    Apply_OR_ByteMask(_Buttons_CaveAddress + 6 + (PlayerIndex * 2), 0x02);
                }
                else if (e.ButtonName.Equals("RELOAD"))
                {
                    Apply_OR_ByteMask(_Buttons_CaveAddress + 6 + (PlayerIndex * 2), 0x01);
                }
                else if (e.ButtonName.Equals("PEDAL LEFT"))
                {
                    Apply_OR_ByteMask(_Buttons_CaveAddress + 6 + (PlayerIndex * 2), 0x08);
                }
                else if (e.ButtonName.Equals("PEDAL RIGHT"))
                {
                    Apply_OR_ByteMask(_Buttons_CaveAddress + 6 + (PlayerIndex * 2), 0x04);
                }
                else if (e.ButtonName.Equals("START"))
                {
                    Apply_OR_ByteMask(_Buttons_CaveAddress + 6 + (PlayerIndex * 2), 0x80);
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
                    Apply_AND_ByteMask(_Buttons_CaveAddress + 6 + (PlayerIndex * 2), 0xFD);
                }
                else if (e.ButtonName.Equals("RELOAD"))
                {
                    Apply_AND_ByteMask(_Buttons_CaveAddress + 6 + (PlayerIndex * 2), 0xFE);
                }
                else if (e.ButtonName.Equals("PEDAL LEFT"))
                {
                    Apply_AND_ByteMask(_Buttons_CaveAddress + 6 + (PlayerIndex * 2), 0xF7);
                }
                else if (e.ButtonName.Equals("PEDAL RIGHT"))
                {
                    Apply_AND_ByteMask(_Buttons_CaveAddress + 6 + (PlayerIndex * 2), 0xFB);
                }
                else if (e.ButtonName.Equals("START"))
                {
                    Apply_AND_ByteMask(_Buttons_CaveAddress + 6 + (PlayerIndex * 2), 0x7F);
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
            //Gun recoil : Is activated for every bullet shot
            _Outputs = new List<GameOutput>
            {
                new GameOutput(OutputId.P1_LmpStart),
                new GameOutput(OutputId.LmpPanel),
                new GameOutput(OutputId.Lmp1),
                new GameOutput(OutputId.Lmp2),
                new GameOutput(OutputId.Lmp3),
                new GameOutput(OutputId.Lmp4),
                new GameOutput(OutputId.Lmp5),
                new GameOutput(OutputId.Lmp6),
                new GameOutput(OutputId.P1_GunRecoil),
                new AsyncGameOutput(OutputId.P1_CtmRecoil, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_ON_DELAY), ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_OFF_DELAY), 0),
                new GameOutput(OutputId.P1_Life),
                new AsyncGameOutput(OutputId.P1_Damaged, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_DAMAGED_DELAY), 100, 0),
                new GameOutput(OutputId.Credits)
            };
        }

        /// <summary>
        /// Update all Outputs values before sending them to MameHooker
        /// </summary>
        public override void UpdateOutputValues()
        {
            //Original Outputs
            UInt32 Outputs_Address = ReadUInt32(_OutputsPtr_Address);
            int RecoilStatus = ReadByte(Outputs_Address) >> 6 & 0x01;
            SetOutputValue(OutputId.P1_LmpStart, ReadByte(Outputs_Address) >> 7 & 0x01);
            SetOutputValue(OutputId.LmpPanel, ReadByte(Outputs_Address + 1) >> 7 & 0x01);
            SetOutputValue(OutputId.Lmp1, ReadByte(Outputs_Address + 1) >> 1 & 0x01);
            SetOutputValue(OutputId.Lmp2, ReadByte(Outputs_Address + 1) >> 2 & 0x01);
            SetOutputValue(OutputId.Lmp3, ReadByte(Outputs_Address + 1) >> 3 & 0x01);
            SetOutputValue(OutputId.Lmp4, ReadByte(Outputs_Address + 1) >> 4 & 0x01);
            SetOutputValue(OutputId.Lmp5, ReadByte(Outputs_Address + 1) >> 5 & 0x01);
            SetOutputValue(OutputId.Lmp6, ReadByte(Outputs_Address + 1) >> 6 & 0x01);
            SetOutputValue(OutputId.P1_GunRecoil, RecoilStatus);

            //Custom Outputs
            _Life[0] = 0;
            _Ammo[0] = 0;

            //Filter InGame and not in attract Demo
            if (ReadByte(ReadPtr(_PlayerStructPtr_Address) + 0x27) != 0 && ReadByte(ReadPtr(_PlayerStructPtr_Address) + 0x2D) == 1)
            {
                _Life[0] = ReadByte(ReadPtr(_PlayerStructPtr_Address) + 0x78);
                _Ammo[0] = ReadByte(ReadPtr(_AmmoPtr_Address) + 0x04);

                //[Damaged] custom Output  
                if (_Life[0] < _LastLife[0])
                    SetOutputValue(OutputId.P1_Damaged, 1);
            }

            _LastLife[0] = _Life[0];
            _LastAmmo[0] = _Ammo[0];

            SetOutputValue(OutputId.P1_Ammo, _Ammo[0]);
            //Custom Recoil will be ctivated just ike the original one
            SetOutputValue(OutputId.P1_CtmRecoil, RecoilStatus);
            SetOutputValue(OutputId.P1_Life, _Life[0]);
            SetOutputValue(OutputId.Credits, ReadByte(_Credits_Address));
        }

        #endregion
    }
}
