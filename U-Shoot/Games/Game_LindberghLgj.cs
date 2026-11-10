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
    public class Game_LindberghLgj : BaseGame32
    {
        protected override string GAMEDATA_FOLDER { get { return @"Lindbergh\lgj"; } }
        protected override bool PATCH_PEB_DEBUG_FLAG { get { return false; } }

        private const string LGJ_DESC = "Let's Go Jungle (SBLC)";
        private const string LGJ_REVA_DESC = "Let's Go Jungle (SBLC) (Rev.A)";

        private const string ELFPATH_OPTION = "Game ELF";
        private const string LOADEROPTIONS_OPTION = "LinuxLoader options";
        private const string TESTMENU_OPTION = "TEST Menu";

        #region LOADER MEMORY ADDRESSES                
        //Check instruction for game loaded
        private UInt32 _RomLoaded_Check_Address = 0x0807925B;
        private UInt32 _NoCrosshair_Patch_Address = 0x080B0D06;
        #endregion

        #region INPUTS MEMORY ADDRESSES    
        private InjectionStruct32 _JvsRawAxes_InjectionStruct = new InjectionStruct32(0x084E8CDC, 7);
        private InjectionStruct32 _Buttons_InjectionStruct = new InjectionStruct32(0x084E8AF5, 7);
        private InjectionStruct32 _Coins_InjectionStruct = new InjectionStruct32(0x084EDCFC, 7);

        //Game is using complex calibration procedure, overriding float values after this :
        private NopStruct32 _Nop_AdjustedAxis_X = new NopStruct32(0x080A9AD2, 8);
        private NopStruct32 _Nop_AdjustedAxis_Y = new NopStruct32(0x080A97FE, 8);
        //INPUT_STRUCT offset in game
        private UInt32 _Player1_InputPtr_Address = 0x087D3BB0;
        private const UInt32 INPUT_X_OFFSET = 0x134;
        private const UInt32 INPUT_Y_OFFSET = 0x138;

        //Custom Data
        private UInt32 _JvsRawAxes_CaveAddress;
        private UInt32 _Buttons_CaveAddress;
        private UInt32 _Coins_CaveAddress;
        #endregion

        #region OUTPUTS MEMORY ADDRESSES
        private UInt32 _JvsOutput_Address = 0x087D186D;
        private UInt32 _Credits_Address = 0x08C08420;
        private UInt32 _PlayersStatus_Address = 0x87D3DC0;
        private InjectionStruct32 _Recoil_InjectionStruct = new InjectionStruct32(0x080AFA1C, 6);

        //Custom Data
        private UInt32 _CustomRecoil_CaveAddress;
        #endregion

        public Game_LindberghLgj(GameConfig Config) : base(Config)
        {
            string sLoaderOptions = _GameConfig.GetStringOption(LOADEROPTIONS_OPTION);
            if (_GameConfig.GetBoolOption(TESTMENU_OPTION))
                sLoaderOptions = "-t " + sLoaderOptions;

            _GameLauncher.Set_CmdLine_Args("\"" + _GameConfig.GetStringOption(ELFPATH_OPTION + " [lgj_final]") + "\" " + sLoaderOptions);

            _KnownMd5Prints.Add(LGJ_DESC, "37b0239d4fbc6256c65dd70beb087a34");
            _KnownMd5Prints.Add(LGJ_REVA_DESC, "875486650a16abce5b1901fd319001c0");

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
                //And this instruction is also helping us detecting whether the game file is Rev.A or Rev.B or Rev.C binary, to call the corresponding hack
                byte[] buffer = ReadBytes(_RomLoaded_Check_Address, 3);
                if (buffer.SequenceEqual(new byte[] { 0x8B, 0x1D, 0xB0 }))
                {
                    Logger.WriteLog(LGJ_DESC + " binary detected");
                    _TargetProcess_Md5Hash = _KnownMd5Prints[LGJ_DESC];
                }
                else if (buffer.SequenceEqual(new byte[] { 0x00, 0x00, 0x8B }))
                {
                    Logger.WriteLog(LGJ_REVA_DESC + " binary detected");
                    _TargetProcess_Md5Hash = _KnownMd5Prints[LGJ_REVA_DESC];
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
                    double dMaxX = 255.0;
                    double dMaxY = 255.0;
                    double dRangeX = dMaxX - dMinX + 1;
                    double dRangeY = dMaxY - dMinY + 1;

                    PlayerController.Computed_X = Convert.ToInt32(dMaxX - Math.Round(dRangeX * PlayerController.Computed_X / TotalResX));
                    PlayerController.Computed_Y = Convert.ToInt32(dMaxY - Math.Round(dRangeY * PlayerController.Computed_Y / TotalResY));
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
            _Buttons_CaveAddress = InputsDatabank_Address + 0x20;
            _Coins_CaveAddress = InputsDatabank_Address + 0x28;

            SetHack_Axes();
            SetHack_Buttons();
            SetHack_Coins();

            // Noping X,Y axis values update in the following procedure :
            // acpPlayer::input()
            SetNops(0, _Nop_AdjustedAxis_X);
            SetNops(0, _Nop_AdjustedAxis_Y);

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

        #region Outputs Hack

        protected override void Apply_OutputsMemoryHack()
        {
            Create_OutputsDataBank();
            _CustomRecoil_CaveAddress = OutputsDatabank_Address;

            SetHack_Recoil();
            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        /// <summary>
        /// Intercepting the bullet count increase call to create our own recoil
        /// </summary>
        private void SetHack_Recoil()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //lea edx,[edi+01]
            CaveMemory.Write_StrBytes("8D 57 01");
            //mov [esi+14],edi
            CaveMemory.Write_StrBytes("89 7E 14");
            //push eax
            CaveMemory.Write_StrBytes("50");
            //mov    eax,DWORD PTR [ebx+0x118] 
            CaveMemory.Write_StrBytes("8B 83 18 01 00 00");
            //mov byte ptr [eax+_CustomRecoil_CaveAddress],01
            CaveMemory.Write_StrBytes("C6 80");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_CustomRecoil_CaveAddress));
            CaveMemory.Write_StrBytes("01");
            //pop eax
            CaveMemory.Write_StrBytes("58");

            //Inject it
            CaveMemory.InjectToAddress(_Recoil_InjectionStruct, "Recoil");
        }

        #endregion

        #region No Crosshair Hack

        /// <summary>
        /// acpPlayer::calc() calls AbkTrunk::StartBranch() a 2 different places, depending on the crosshair displayed (no shoot / shoot)
        /// Changing the last parameter from 1 to 0 when the needed cursor is "shoot" will mask it
        /// </summary>
        protected override void Apply_NoCrosshairMemoryHack()
        {
            WriteByte(_NoCrosshair_Patch_Address, 0x00);

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
                float X_Value = 1.0f - (2.0f * (float)X / 255.0f);
                float Y_Value = (2.0f * (float)Y / 255.0f) - 1.0f;

                //First channels of analog inputs in JVS are RawPosition (0x00-0xFF) for each players (Bytes 0-7)
                WriteByte(_JvsRawAxes_CaveAddress + ((uint)Id * 4), (byte)X);
                WriteByte(_JvsRawAxes_CaveAddress + 2 + ((uint)Id * 4), (byte)Y);

                //Copy calibrated float value
                WriteBytes(ReadPtr(_Player1_InputPtr_Address - ((uint)Id * 4)) + INPUT_X_OFFSET, BitConverter.GetBytes(X_Value));
                WriteBytes(ReadPtr(_Player1_InputPtr_Address - ((uint)Id * 4)) + INPUT_Y_OFFSET, BitConverter.GetBytes(Y_Value));
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
                new GameOutput(OutputId.LmpRoom),
                new GameOutput(OutputId.LmpCoin),
                new GameOutput(OutputId.P1_GunMotor),
                new GameOutput(OutputId.P2_GunMotor),
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
            //Original Outputs
            byte[] MotorStatus = new byte[_GameConfig.PlayerCount];
            MotorStatus[0] = (byte)(ReadByte(_JvsOutput_Address) >> 3 & 0x01);
            MotorStatus[1] = (byte)(ReadByte(_JvsOutput_Address) >> 6 & 0x01);
            SetOutputValue(OutputId.P1_LmpStart, ReadByte(_JvsOutput_Address) >> 7 & 0x01);
            SetOutputValue(OutputId.P2_LmpStart, ReadByte(_JvsOutput_Address) >> 4 & 0x01);
            SetOutputValue(OutputId.LmpRoom, ReadByte(_JvsOutput_Address) >> 5 & 0x01);
            SetOutputValue(OutputId.LmpCoin, ReadByte(_JvsOutput_Address) >> 2 & 0x01);
            SetOutputValue(OutputId.P1_GunMotor, MotorStatus[0]);
            SetOutputValue(OutputId.P2_GunMotor, MotorStatus[1]);

            //Custom Outputs
            for (uint i = 0; i < _GameConfig.PlayerCount; i++)
            {
                _Life[i] = 0;
                UInt32 Player_StructAddress = ReadPtr(_Player1_InputPtr_Address - (i * 4));

                //Checking this byte (player playing ?) otherwise the game is still enabing rumble during Attract Mode
                if (ReadByte(_PlayersStatus_Address + (i * 4)) == 1)
                {
                    if (Player_StructAddress != 0)
                    {
                        _Life[i] = (int)ReadFloat(Player_StructAddress + 0x4BC);
                        //[Damaged] custom Output                
                        if (_Life[i] < _LastLife[i])
                            SetOutputValue(OutputId.P1_Damaged + i, 1);

                        //2nd filter : enabling recoil only if genuine gun is rumbling
                        if (ReadByte(_CustomRecoil_CaveAddress + i) == 1 && MotorStatus[i] == 1)
                        {
                            SetOutputValue(OutputId.P1_CtmRecoil + i, 1);
                            WriteByte(_CustomRecoil_CaveAddress + i, 0);
                        }
                    }
                }

                _LastLife[i] = _Life[i];

                SetOutputValue(OutputId.P1_Life + i, _Life[i]);
            }
           
            SetOutputValue(OutputId.Credits, ReadByte(_Credits_Address));
        }

        #endregion
    }
}
