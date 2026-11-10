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
    public class Game_LindberghHotd4sp : BaseGame32
    {
        protected override string GAMEDATA_FOLDER { get { return @"Lindbergh\hotd4sp"; } }
        protected override bool PATCH_PEB_DEBUG_FLAG { get { return false; } }

        private const string HOTD4SP_REVB_GAME_DESC = "House of The Dead 4 Secial (SBLS) (Rev.B)";
        private const string HOTD4SP_REVB_TEST_DESC = "House of The Dead 4 Secial (SBLS) (Rev.B) TEST Menu";

        private const string ELFPATH_OPTION = "Game ELF";
        private const string LOADEROPTIONS_OPTION = "LinuxLoader options";
        private const string TESTMENU_OPTION = "TEST Menu";

        #region LOADER MEMORY ADDRESSES                
        //Rom loaded + Rom version check
        private UInt32 _GameLoaded_Check_Address = 0x080505A2;
        private UInt32 _TestMenuLoaded_Check_Address = 0x0805483B;
        #endregion

        #region INPUTS MEMORY ADDRESSES    
        //Inputs       
        private InjectionStruct32 _JvsRawAxes_InjectionStruct = new InjectionStruct32(0x08366CDC, 7);
        private InjectionStruct32 _AdjustedAxes_InjectionStruct = new InjectionStruct32(0x08179BEA, 11);
        private InjectionStruct32 _Buttons_InjectionStruct = new InjectionStruct32(0x08366AF5, 7);
        private InjectionStruct32 _Coins_InjectionStruct = new InjectionStruct32(0x08366C10, 7);

        //Custom Data
        private UInt32 _JvsRawAxes_CaveAddress;
        private UInt32 _AdjustedAxes_CaveAddress;
        private UInt32 _Buttons_CaveAddress;
        private UInt32 _Coins_CaveAddress;
        #endregion

        #region OUTPUTS MEMORY ADDRESSES
        private UInt32 _Outputs_Address = 0x084C35CB;
        private UInt32 _Outputs2_Ptr_Address = 0x0A69F8BC;
        private UInt32 _GameInfos_Address = 0x0A69F92C;
        private UInt32 _Credits_Address = 0x0A6AEFA4;     //also 0x0A6C2E40 
        #endregion

        public Game_LindberghHotd4sp(GameConfig Config) : base(Config)
        {
            string ElfPath = _GameConfig.GetStringOption(ELFPATH_OPTION + " [hod4M.elf]");

            if (_GameConfig.GetBoolOption(TESTMENU_OPTION))
                ElfPath = ElfPath.Replace("hod4M.elf", "hod4testM.elf");

            _GameLauncher.Set_CmdLine_Args("\"" + ElfPath + "\" " + _GameConfig.GetStringOption(LOADEROPTIONS_OPTION));

            _KnownMd5Prints.Add(HOTD4SP_REVB_GAME_DESC, "74baffc7429973f3df8902a31c5164a0");
            _KnownMd5Prints.Add(HOTD4SP_REVB_TEST_DESC, "398b5f4f1d233a1d135168bc318e509e");

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
                    if (buffer.SequenceEqual(new byte[] { 0x68, 0x80, 0xE4 }))
                    {
                        Logger.WriteLog(HOTD4SP_REVB_TEST_DESC+ "  binary detected");
                        _TargetProcess_Md5Hash = _KnownMd5Prints[HOTD4SP_REVB_TEST_DESC];
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
                    if (buffer.SequenceEqual(new byte[] { 0x0D, 0x80, 0x00 }))
                    {
                        Logger.WriteLog(HOTD4SP_REVB_GAME_DESC + " binary detected");
                        //So far, all binaries are sharing the same code so no Memory Files are needed
                        //_TargetProcess_Md5Hash = _KnownMd5Prints[HOTD4SP_REVB_GAME_DESC];
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
            //For now the default calibration is good enough to not use this, and the game still has bugs....be back at it later
            //P1/P2 is not running in that loop (only P1)
            //Did not manage to get P2 playing ?
            if (!_GameConfig.GetBoolOption(TESTMENU_OPTION))
                SetHack_AdjustedAxes();
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

            //lea ecx,[eax-B]
            CaveMemory.Write_StrBytes("8D 48 F5");
            //movzx ecx,word ptr [ecx+_Axes_CaveAddress]
            CaveMemory.Write_StrBytes("0F B6 89");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_JvsRawAxes_CaveAddress));
            //lea eax,[eax+esi+00000102]
            CaveMemory.Write_StrBytes("8D 84 30 02 01 00 00");
            //mov [eax],cx
            CaveMemory.Write_StrBytes("66 89 08");

            //Inject it
            CaveMemory.InjectToAddress(_JvsRawAxes_InjectionStruct, "Axes");
        }

        /// <summary>
        /// Replacing GetAdjstGunPos() content
        /// </summary>
        private void SetHack_AdjustedAxes()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //mov al,[_AdjustedAxes_CaveAddress]
            CaveMemory.Write_StrBytes("A0");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_AdjustedAxes_CaveAddress));
            //mov [ebp+04],al
            CaveMemory.Write_StrBytes("88 45 04");
            //mov al,[_AdjustedAxes_CaveAddress + 1]
            CaveMemory.Write_StrBytes("A0");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_AdjustedAxes_CaveAddress + 1));
            //mov [ebp+05],al
            CaveMemory.Write_StrBytes("88 45 05");

            //Inject it
            CaveMemory.InjectToAddress(_AdjustedAxes_InjectionStruct, "Adjusted Axes");
        }

        /// <summary>
        /// At the end of amJvspAckSwInput(), overriding buttons bytes before memory copy
        /// TEST byte (EBX = 5) is one-byte copy
        /// Other data is 2-bytes copy
        /// </summary>
        private void SetHack_Buttons()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //lea eax,[edx+ebx+00000102]
            CaveMemory.Write_StrBytes("8D 84 1A 02 01 00 00");

            //cmp ebx,05
            CaveMemory.Write_StrBytes("83 FB 05");
            //je TestByte
            CaveMemory.Write_StrBytes("74 0A");
            //movzx ecx,word ptr [ebx+_Buttons_CaveAddress]
            CaveMemory.Write_StrBytes("0F B7 8B");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Buttons_CaveAddress));
            //mov [eax],cx
            CaveMemory.Write_StrBytes("66 89 08");
            //TestByte:
            //movzx ecx,byte ptr [ebx+_Buttons_CaveAddress]
            CaveMemory.Write_StrBytes("0F B6 8B");
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
            else if (e.ButtonName.Equals("START"))
            {
                Apply_OR_ByteMask(_Buttons_CaveAddress + 6, 0x80);
            }
            else if (e.ButtonName.Equals("ACTION"))
            {
                Apply_OR_ByteMask(_Buttons_CaveAddress + 7, 0x40);
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
            else if (e.ButtonName.Equals("START"))
            {
                Apply_AND_ByteMask(_Buttons_CaveAddress + 6, 0x7F);
            }
            else if (e.ButtonName.Equals("ACTION"))
            {
                Apply_AND_ByteMask(_Buttons_CaveAddress + 7, 0xBF);
            }
        }

        protected override void UpdateAxis(PlayerID Id, int X, int Y)
        {
            if (Id != PlayerID.None)
            {
                //First channels of analog inputs in JVS are RawPosition (0x00-0xFF) for each players (Bytes 0-7)
                WriteByte(_JvsRawAxes_CaveAddress + ((uint)Id * 4), (byte)X);
                WriteByte(_JvsRawAxes_CaveAddress + 2 + ((uint)Id * 4), (byte)Y);

                WriteByte(_AdjustedAxes_CaveAddress + ((uint)Id * 2), (byte)X);
                WriteByte(_AdjustedAxes_CaveAddress + 1 + ((uint)Id * 2), (byte)Y);
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
                else if (e.ButtonName.Equals("GRENADE"))
                {
                    Apply_OR_ByteMask(_Buttons_CaveAddress + 7 + (PlayerIndex * 2), 0x80);
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
                else if (e.ButtonName.Equals("GRENADE"))
                {
                    Apply_AND_ByteMask(_Buttons_CaveAddress + 7 + (PlayerIndex * 2), 0x7F);
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
                new GameOutput(OutputId.P1_GunMotor),
                new GameOutput(OutputId.P2_GunMotor),
                new GameOutput(OutputId.P1_AirFront),
                new GameOutput(OutputId.P1_AirRear),
                new GameOutput(OutputId.P2_AirFront),
                new GameOutput(OutputId.P2_AirRear),
                new GameOutput(OutputId.LmpStop),
                new GameOutput(OutputId.LmpAction),
                new GameOutput(OutputId.LmpReset),
                new GameOutput(OutputId.LmpError),
                new GameOutput(OutputId.LmpSafety),
                new GameOutput(OutputId.LmpFloor),
                new GameOutput(OutputId.LmpSpot),
                new GameOutput(OutputId.LmpBillboard_R),
                new GameOutput(OutputId.LmpBillboard_G),
                new GameOutput(OutputId.LmpBillboard_B),
                new GameOutput(OutputId.P1_Ammo),
                new GameOutput(OutputId.P2_Ammo),
                new AsyncGameOutput(OutputId.P1_CtmRecoil, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_ON_DELAY), ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_OFF_DELAY), 0),
                new AsyncGameOutput(OutputId.P2_CtmRecoil, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_ON_DELAY), ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_OFF_DELAY), 0),
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
            int P1_Motor_Status = ReadByte(_Outputs_Address) >> 4 & 0x01;
            int P2_Motor_Status = ReadByte(_Outputs_Address) >> 5 & 0x01;

            //SetOutputValue(OutputId.P1_LmpStart, ReadByte(_Outputs_Address) >> 7 & 0x01);
            SetOutputValue(OutputId.P1_GunMotor, P1_Motor_Status);
            SetOutputValue(OutputId.P2_GunMotor, P2_Motor_Status);
            SetOutputValue(OutputId.P1_AirFront, ReadByte(_Outputs_Address) & 0x01);
            SetOutputValue(OutputId.P2_AirFront, ReadByte(_Outputs_Address) >> 1 & 0x01);
            SetOutputValue(OutputId.P1_AirRear, ReadByte(_Outputs_Address) >> 2 & 0x01);
            SetOutputValue(OutputId.P2_AirRear, ReadByte(_Outputs_Address) >> 3 & 0x01);
            SetOutputValue(OutputId.LmpStop, ReadByte(_Outputs_Address + 2) >> 3 & 0x01);
            SetOutputValue(OutputId.LmpReset, ReadByte(_Outputs_Address + 2) & 0x01);
            SetOutputValue(OutputId.LmpError, ReadByte(_Outputs_Address + 2) >> 1 & 0x01);
            SetOutputValue(OutputId.LmpSafety, ReadByte(_Outputs_Address + 2) >> 2 & 0x01);
            SetOutputValue(OutputId.LmpFloor, ReadByte(_Outputs_Address + 2) >> 4 & 0x01);
            SetOutputValue(OutputId.LmpSpot, ReadByte(_Outputs_Address + 2) >> 5 & 0x01);
            //Billboard : 0x80=RED, 0x81=GREEN, 0x82=BLUE
            int BillboardState = ReadByte(_Outputs_Address + 1);
            if (BillboardState != 0)
            {
                switch (BillboardState)
                {
                    case 0x80:
                        {
                            SetOutputValue(OutputId.LmpBillboard_R, 1);
                            SetOutputValue(OutputId.LmpBillboard_G, 0);
                            SetOutputValue(OutputId.LmpBillboard_B, 0);
                        }break;
                    case 0x81:
                        {
                            SetOutputValue(OutputId.LmpBillboard_R, 0);
                            SetOutputValue(OutputId.LmpBillboard_G, 1);
                            SetOutputValue(OutputId.LmpBillboard_B, 0);
                        }break;
                    case 0x82:
                        {
                            SetOutputValue(OutputId.LmpBillboard_R, 0);
                            SetOutputValue(OutputId.LmpBillboard_G, 0);
                            SetOutputValue(OutputId.LmpBillboard_B, 1);
                        }break;
                    default:
                        {
                            SetOutputValue(OutputId.LmpBillboard_R, 0);
                            SetOutputValue(OutputId.LmpBillboard_G, 0);
                            SetOutputValue(OutputId.LmpBillboard_B, 0);
                        }break;
                }
            }
            else
            {
                SetOutputValue(OutputId.LmpBillboard_R, 0);
                SetOutputValue(OutputId.LmpBillboard_G, 0);
                SetOutputValue(OutputId.LmpBillboard_B, 0);
            }

            //Start and Action Buttons are on another memory location
            Byte bOutputs2 = ReadByte(ReadPtr(_Outputs2_Ptr_Address));
            SetOutputValue(OutputId.LmpAction, bOutputs2 >> 4 & 0x01);
            SetOutputValue(OutputId.P1_LmpStart, bOutputs2 >> 7 & 0x01);

            if (!_GameConfig.GetBoolOption(TESTMENU_OPTION))
            {

                //Custom Outputs
                _Life[0] = 0;
                _Ammo[0] = 0;
                _Ammo[1] = 0;

                if (ReadPtr(_GameInfos_Address) != 0)
                {
                    _Life[0] = ReadByte(ReadPtr(_GameInfos_Address) + 0x40);
                    //[Damaged] custom Output                
                    if (_Life[0] < _LastLife[0])
                        SetOutputValue(OutputId.P1_Damaged, 1);

                    _Ammo[0] = ReadByte(ReadPtrChain(_GameInfos_Address, new UInt32[] { 0x34 }) + 0x2D4);

                    //No attract mode so we can just reuse the original recoil flag
                    SetOutputValue(OutputId.P1_CtmRecoil, P1_Motor_Status);

                    _Ammo[1] = ReadByte(ReadPtrChain(_GameInfos_Address, new UInt32[] { 0x38 }) + 0x2D4);

                    //No attract mode so we can just reuse the original recoil flag
                    SetOutputValue(OutputId.P2_CtmRecoil, P2_Motor_Status);
                }

                _LastLife[0] = _Life[0];

                SetOutputValue(OutputId.P1_Ammo, _Ammo[0]);
                SetOutputValue(OutputId.P2_Ammo, _Ammo[1]);
                SetOutputValue(OutputId.P1_Life, _Life[0]);
            }

            SetOutputValue(OutputId.Credits, ReadByte(_Credits_Address));
        }

        #endregion
    }
}
