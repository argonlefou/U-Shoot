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
    public class Game_LindberghHotdEx : BaseGame32
    {
        protected override string GAMEDATA_FOLDER { get { return @"Lindbergh\hotdex"; } }
        protected override bool PATCH_PEB_DEBUG_FLAG { get { return false; } }

        private const string ELFPATH_OPTION = "Game ELF";
        private const string LOADEROPTIONS_OPTION = "LinuxLoader options";
        private const string TESTMENU_OPTION = "TEST Menu";

        #region LOADER MEMORY ADDRESSES                
        //Check instruction for game loaded
        private UInt32 _GameLoaded_Check_Address = 0x0815C65A;
        private UInt32 _TestMenuLoaded_Check_Address = 0x0805DAD2;
        #endregion

        #region INPUTS MEMORY ADDRESSES    
        private InjectionStruct32 _JvsRawAxes_InjectionStruct = new InjectionStruct32(0x084BD186, 7);
        private InjectionStruct32 _AdjustedAxes_InjectionStruct = new InjectionStruct32(0x81039C1, 7);  //Not Called !
        private InjectionStruct32 _Buttons_InjectionStruct = new InjectionStruct32(0x084BCF45, 7);
        private InjectionStruct32 _Coins_InjectionStruct = new InjectionStruct32(0x084BD06F, 7);

        //Custom Data
        private UInt32 _JvsRawAxes_CaveAddress;
        private UInt32 _AdjustedAxes_CaveAddress;
        private UInt32 _Buttons_CaveAddress;
        private UInt32 _Coins_CaveAddress;
        #endregion

        #region OUTPUTS MEMORY ADDRESSES
        private UInt32 _JvsMgrPtr_Address = 0x0A9DFEBC;
        private UInt32 _GameMgrPtr_Address = 0xA9DFE98;
        private UInt32 _Credits_Address = 0xAA3DD80;
        private UInt32 _Freeplay_Address = 0xAA3DD67;
        #endregion

        public Game_LindberghHotdEx(GameConfig Config) : base(Config)
        {
            string ElfPath = _GameConfig.GetStringOption(ELFPATH_OPTION + " [hodexRI.elf]");

            if (_GameConfig.GetBoolOption(TESTMENU_OPTION))
                ElfPath = ElfPath.Replace("hodexRI.elf", "hodextestR.elf");

            _GameLauncher.Set_CmdLine_Args("\"" + ElfPath + "\" " + _GameConfig.GetStringOption(LOADEROPTIONS_OPTION));

            _KnownMd5Prints.Add("House Of The Dead EX(SBRC) - TP Dump", "69a85f6f2ba82e014cf7e3f65f7b3d2a");
            _KnownMd5Prints.Add("House Of The Dead EX(SBRC) TEST Menu - TP Dump", "834e46dcf31a7db500f8892101f598ff");

            _KnownMd5Prints.Add("House Of The Dead EX(SBRC) - Clean Dump", "cf33998ef942adfc8f0f0c3a53cfb6ad");
            _KnownMd5Prints.Add("House Of The Dead EX(SBRC) TEST Menu - Clean Dump", "e330d11450bc25d38b4faa02d550038f");

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
                    if (buffer.SequenceEqual(new byte[] { 0x8B, 0x15, 0x90 }))
                    {
                        Logger.WriteLog("House Of The Dead EX (SBRC) - TEST Menu binary detected");
                        _TargetProcess_Md5Hash = _KnownMd5Prints["House Of The Dead EX(SBRC) TEST Menu - Clean Dump"];
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
                    if (buffer.SequenceEqual(new byte[] { 0x8B, 0x15, 0xBC }))
                    {
                        Logger.WriteLog("House Of The Dead EX (SBRC) Game binary detected");
                        //So far, all binaries are sharing the same code so no Memory Files are needed
                        //_TargetProcess_Md5Hash = _KnownMd5Prints[HOTDEX_DESC];
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

            SetHack_Axes();
            //SetHack_AdjustedAxes();
            SetHack_Buttons();
            SetHack_Coins();

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        /// <summary>
        /// At the end of amJvspAckAnalogInput(), replacing Axis value before memory copy
        /// </summary>
        private void SetHack_Axes()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //lea ecx,[ebx-B]
            CaveMemory.Write_StrBytes("8D 4B F5");
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
        /// At the end of amJvspAckSwInput(), removing the wanted buttons bit states from the source memory (Trigger, Reload, and Grenade)
        /// and changing the bits with custom values before memorycopy
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

        #region Outputs Hack

        protected override void Apply_OutputsMemoryHack()
        {
            Create_OutputsDataBank();

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
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
                else if (e.ButtonName.Equals("PEDAL"))
                {
                    int iMask = 0x08 >> (int)PlayerIndex;
                    Apply_OR_ByteMask(_Buttons_CaveAddress + 6, (byte)iMask);
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
                else if (e.ButtonName.Equals("PEDAL"))
                {
                    int iMask = 0x08 >> (int)PlayerIndex;
                    Apply_AND_ByteMask(_Buttons_CaveAddress + 6, (byte)~iMask);
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
            _Outputs = new List<GameOutput>
            {
                new GameOutput(OutputId.P1_LmpStart),
                new GameOutput(OutputId.P2_LmpStart),
                new GameOutput(OutputId.LmpPanel),
                new GameOutput(OutputId.Lmp1),
                new GameOutput(OutputId.Lmp2),
                new GameOutput(OutputId.Lmp3),
                new GameOutput(OutputId.Lmp4),
                new GameOutput(OutputId.Lmp5),
                new GameOutput(OutputId.Lmp6),
                new GameOutput(OutputId.P1_LmpFoot),
                new GameOutput(OutputId.P2_LmpFoot),
                new GameOutput(OutputId.P1_Life),
                new GameOutput(OutputId.P2_Life),
                new AsyncGameOutput(OutputId.P1_Damaged, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_DAMAGED_DELAY), 100, 0),
                new AsyncGameOutput(OutputId.P2_Damaged, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_DAMAGED_DELAY), 100, 0),                
                /*
                _Outputs.Add(new AsyncGameOutput(OutputId.P1_CtmRecoil, MameOutputHelper.CustomRecoilOnDelay, MameOutputHelper.CustomRecoilOffDelay, 0));
                _Outputs.Add(new AsyncGameOutput(OutputId.P2_CtmRecoil, MameOutputHelper.CustomRecoilOnDelay, MameOutputHelper.CustomRecoilOffDelay, 0));            
                 */
                new GameOutput(OutputId.Credits)
            };
        }

        /// <summary>
        /// Update all Outputs values before sending them to MameHooker
        /// </summary>
        public override void UpdateOutputValues()
        {
            //Original Outputs
            UInt32 Outputs_Address = ReadUInt32(_JvsMgrPtr_Address);
            SetOutputValue(OutputId.P1_LmpStart, ReadByte(Outputs_Address) >> 7 & 0x01);
            SetOutputValue(OutputId.P2_LmpStart, ReadByte(Outputs_Address) >> 4 & 0x01);
            SetOutputValue(OutputId.LmpPanel, ReadByte(Outputs_Address + 1) >> 7 & 0x01);
            SetOutputValue(OutputId.Lmp1, ReadByte(Outputs_Address + 1) >> 6 & 0x01);
            SetOutputValue(OutputId.Lmp2, ReadByte(Outputs_Address + 1) >> 5 & 0x01);
            SetOutputValue(OutputId.Lmp3, ReadByte(Outputs_Address + 1) >> 4 & 0x01);
            SetOutputValue(OutputId.Lmp4, ReadByte(Outputs_Address + 1) >> 3 & 0x01);
            SetOutputValue(OutputId.Lmp5, ReadByte(Outputs_Address + 1) >> 2 & 0x01);
            SetOutputValue(OutputId.Lmp6, ReadByte(Outputs_Address + 1) >> 1 & 0x01);
            SetOutputValue(OutputId.P1_LmpFoot, ReadByte(Outputs_Address) >> 5 & 0x01);
            SetOutputValue(OutputId.P2_LmpFoot, ReadByte(Outputs_Address) >> 2 & 0x01);

            //Custom Outputs
            UInt32 GameManager_Address = ReadPtr(_GameMgrPtr_Address);
            for (uint i = 0; i < _GameConfig.PlayerCount; i++)
            {
                _Life[i] = 0;

                if (GameManager_Address != 0)
                {
                    _Life[i] = ReadByte(GameManager_Address + 0x48 + (4 * i));
                    if (_Life[i] < _LastLife[i])
                        SetOutputValue(OutputId.P1_Damaged + i, 1);
                }

                _LastLife[i] = _Life[i];
                SetOutputValue(OutputId.P1_Life + i, _Life[i]);
            }

            //Displaying Credits if no FREEPLAY enabled
            if (ReadByte(_Freeplay_Address) == 0)
                SetOutputValue(OutputId.Credits, ReadByte(_Credits_Address));
            else
                SetOutputValue(OutputId.Credits, 0);
        }

        #endregion
    }
}
