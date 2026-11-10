using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UsCore.Config;
using UsCore.Launcher;
using UsCore.Logging;
using UsCore.MameOutput;
using UsCore.Memory;
using UsCore.VirtualControllers;

namespace U_Shoot.Games
{
    public class Game_LindberghGhostSquadEvo : BaseGame32
    {
        protected override string GAMEDATA_FOLDER { get { return @"Lindbergh\gsevo"; } }
        protected override bool PATCH_PEB_DEBUG_FLAG { get { return false; } }

        private const string GSEVO_DESC = "Ghost Squad Evolution (SBNJ) (Rev.A)";

        private const string ELFPATH_OPTION = "Game ELF";
        private const string LOADEROPTIONS_OPTION = "LinuxLoader options";
        private const string TESTMENU_OPTION = "TEST Menu";

        #region LOADER MEMORY ADDRESSES                
        //Rom loaded + Rom version check
        private UInt32 _RomLoaded_Check_Address = 0x0807C9A0;
        #endregion

        #region INPUT MEMORY ADDRESSES
        private InjectionStruct32 _JvsRawAxes_InjectionStruct = new InjectionStruct32(0x08185951, 7);
        private InjectionStruct32 _Buttons_InjectionStruct = new InjectionStruct32(0x08185710, 7);
        private InjectionStruct32 _Coins_InjectionStruct = new InjectionStruct32(0x0818583A, 7);
        private InjectionStruct32 _Calibration_InjectionStruct = new InjectionStruct32(0x8069BCE, 7);

        //Custom Data
        private UInt32 _JvsRawAxes_CaveAddress;
        private UInt32 _Buttons_CaveAddress;
        private UInt32 _Coins_CaveAddress;
        private UInt32 _CalibrationData_CaveAddress;
        #endregion

        #region OUTPUT MEMORY ADDRESSES
        private UInt32 _P1_GameState_Address = 0x086617E8;
        private UInt32 _P2_GameState_Address = 0x8661994;
        private UInt32 _P1_LifePtr_Address = 0x086618D4;
        private UInt32 _P2_LifePtr_Address = 0x08661A80;
        private UInt32 _Outputs_Address = 0x08656358;
        private UInt32 _Credits_Address = 0x00AE9FBA0;
        private InjectionStruct32 _PlayerDamage_InjectionStruct = new InjectionStruct32(0x0810C9D9, 6);
        private InjectionStruct32 _Recoil_InjectionStruct = new InjectionStruct32(0x080E52EC, 6);

        //Custom Data
        private UInt32 _Damage_CaveAddress;
        private UInt32 _Recoil_Caveaddress;
        #endregion

        public Game_LindberghGhostSquadEvo(GameConfig Config) : base(Config)
        {
            string sLoaderOptions = _GameConfig.GetStringOption(LOADEROPTIONS_OPTION);
            if (_GameConfig.GetBoolOption(TESTMENU_OPTION))
                sLoaderOptions = "-t " + sLoaderOptions; 

            _GameLauncher.Set_CmdLine_Args("\"" + _GameConfig.GetStringOption(ELFPATH_OPTION + " [vsg]") + "\" " + sLoaderOptions);

            _KnownMd5Prints.Add(GSEVO_DESC, "bf8fcf04d6d90f9f5dd33ca76f1b670a");

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
                if (buffer.SequenceEqual(new byte[] { 0xA1, 0x90, 0x05 }))
                {
                    Logger.WriteLog(GSEVO_DESC + " binary detected");
                    _TargetProcess_Md5Hash = _KnownMd5Prints[GSEVO_DESC];
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
            _Buttons_CaveAddress = InputsDatabank_Address + 0x10;
            _Coins_CaveAddress = InputsDatabank_Address + 0x18;
            _CalibrationData_CaveAddress = InputsDatabank_Address + 0x20;   //24Bytes for each player

            WriteBytes(_CalibrationData_CaveAddress, new byte[] { 0x00, 0x00, 0x10, 0x00, 0x00, 0x00, 0xE0, 0x00, 0xD1, 0xFE, 0x02, 0x00, 0xFD, 0xFF, 0xFD, 0xFF, 0x2E, 0x01, 0xFF, 0xFF, 0x00, 0x00, 0x21, 0xFF });
            WriteBytes(_CalibrationData_CaveAddress + 0x24, new byte[] { 0x00, 0x00, 0x10, 0x00, 0x00, 0x00, 0xE0, 0x00, 0xD1, 0xFE, 0x02, 0x00, 0xFD, 0xFF, 0xFD, 0xFF, 0x2E, 0x01, 0xFF, 0xFF, 0x00, 0x00, 0x21, 0xFF });

            SetHack_Axes();
            SetHack_Buttons();
            SetHack_Coins();
            if (!_GameConfig.GetBoolOption(TESTMENU_OPTION))
                SetHack_Calibration();

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

        /// <summary>
        /// In the get_backup_gun_adjust_sample() function, returning proper values insted of reading EEPROM
        /// Only enabled in the real game, to keep real value displayed in TEST mode
        /// And not changing data written on EEPROM, also
        /// </summary>
        private void SetHack_Calibration()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);
            
            //mov eax,_CalibrationData_CaveAddress
            CaveMemory.Write_StrBytes("B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_CalibrationData_CaveAddress));
            //ret
            CaveMemory.Write_StrBytes("C3");

            //Inject it
            CaveMemory.InjectToAddress(_Calibration_InjectionStruct, "Calibration");
        }

        #endregion

        #region Outputs Hack

        protected override void Apply_OutputsMemoryHack()
        {
            Create_OutputsDataBank();
            _Damage_CaveAddress = OutputsDatabank_Address;
            _Recoil_Caveaddress = OutputsDatabank_Address + 0x04;

            SetHack_Recoil();
            SetHack_Damage();

            Logger.WriteLog("Outputs Memory Hack complete !");
            Logger.WriteLog("-");
        }

        //Just reading the JVS output bytes miss some bullets recoil on x3 or auto fire
        //Instead, getting the signal directly from the set_gun_reaction() call
        private void SetHack_Recoil()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //push eax
            CaveMemory.Write_StrBytes("50");
            //mov eax,[esp+08]
            CaveMemory.Write_StrBytes("8B 44 24 08");
            //add eax,_Recoil_Caveaddress
            CaveMemory.Write_StrBytes("05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Recoil_Caveaddress));
            //mov byte ptr [eax],01
            CaveMemory.Write_StrBytes("C6 00 01");
            //pop eax
            CaveMemory.Write_StrBytes("58");
            //push ebp
            CaveMemory.Write_StrBytes("55");
            //mov ebp,esp
            CaveMemory.Write_StrBytes("8B EC");
            //sub esp,18
            CaveMemory.Write_StrBytes("83 EC 18");

            //Inject it it
            CaveMemory.InjectToAddress(_Recoil_InjectionStruct, "Recoil");
        }

        //Intercept a call to set_player_damage_internal() function to get dammage event
        //Player Index is in edx
        private void SetHack_Damage()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //push eax
            CaveMemory.Write_StrBytes("50");
            //mov eax,[ebp+08]
            CaveMemory.Write_StrBytes("8B 45 08");
            //add eax,_Damage_CaveAddress
            CaveMemory.Write_StrBytes("05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Damage_CaveAddress));
            //mov byte ptr [eax],01
            CaveMemory.Write_StrBytes("C6 00 01");
            //pop eax
            CaveMemory.Write_StrBytes("58");
            //mov [ebx+000000B8],edx
            CaveMemory.Write_StrBytes("89 93 B8 00 00 00");

            //Inject it it
            CaveMemory.InjectToAddress(_PlayerDamage_InjectionStruct, "Damage");
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

                if (X < 5 || X > 250 || Y < 5 || Y > 250)
                    Apply_OR_ByteMask(_Buttons_CaveAddress + 6 + ((uint)Id * 2), 0x01);
                else
                    Apply_AND_ByteMask(_Buttons_CaveAddress + 6 + ((uint)Id * 2), 0xFE);
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
                else if (e.ButtonName.Equals("CHANGE WEAPON"))
                {
                    Apply_OR_ByteMask(_Buttons_CaveAddress + 7 + (PlayerIndex * 2), 0x40);
                }
                else if (e.ButtonName.Equals("ACTION"))
                {
                    Apply_OR_ByteMask(_Buttons_CaveAddress + 7 + (PlayerIndex * 2), 0x80);
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
                else if (e.ButtonName.Equals("CHANGE WEAPON"))
                {
                    Apply_AND_ByteMask(_Buttons_CaveAddress + 7 + (PlayerIndex * 2), 0xBF);
                }
                else if (e.ButtonName.Equals("ACTION"))
                {
                    Apply_AND_ByteMask(_Buttons_CaveAddress + 7 + (PlayerIndex * 2), 0x7F);
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
            //Gun motor : Is activated for every bullet fired
            _Outputs = new List<GameOutput>
            {
                new GameOutput(OutputId.P1_LmpStart),
                new GameOutput(OutputId.P2_LmpStart),
                new GameOutput(OutputId.P1_LmpHolder),
                new GameOutput(OutputId.P2_LmpHolder),
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
            //Original Outputs
            SetOutputValue(OutputId.P1_LmpStart, ReadByte(_Outputs_Address) >> 7 & 0x01);
            SetOutputValue(OutputId.P2_LmpStart, ReadByte(_Outputs_Address) >> 4 & 0x01);
            SetOutputValue(OutputId.P1_LmpHolder, ReadByte(_Outputs_Address) >> 5 & 0x01);
            SetOutputValue(OutputId.P2_LmpHolder, ReadByte(_Outputs_Address) >> 2 & 0x01);
            SetOutputValue(OutputId.P1_GunRecoil, ReadByte(_Outputs_Address) >> 6 & 0x01);
            SetOutputValue(OutputId.P2_GunRecoil, ReadByte(_Outputs_Address) >> 3 & 0x01);

            for (uint i = 0; i < _GameConfig.PlayerCount; i++)
            {
                _Life[i] = 0;

                UInt32 PlayerState_Address = GameProcess_ModuleBaseAddress + _P1_GameState_Address;
                UInt32 PlayerLife_Ptr_Address = GameProcess_ModuleBaseAddress + _P1_LifePtr_Address;
                if (i == 1)
                {
                    PlayerState_Address = GameProcess_ModuleBaseAddress + _P2_GameState_Address;
                    PlayerLife_Ptr_Address = GameProcess_ModuleBaseAddress + _P2_LifePtr_Address;
                }

                // Checking the Player state to not trigger Damage event during Attract mode:
                // 0x00 = Not playing
                // 0x02 = In-Game
                // 0x08 = Continue
                // 0x10 = Game-Over
                // 0x80 = Attract Mode
                if (ReadByte(PlayerState_Address) == 2)
                {
                    _Life[i] = ReadByte(ReadPtr(PlayerLife_Ptr_Address) + 0xB8);

                    //[Damaged] custom Output                
                    if (ReadByte(_Damage_CaveAddress + i) == 1)
                        SetOutputValue(OutputId.P1_Damaged + i, 1);

                    //[Recoil] custom Output
                    if (ReadByte(_Recoil_Caveaddress + i) == 1)
                        SetOutputValue(OutputId.P1_CtmRecoil + i, 1);
                }
                WriteByte(_Damage_CaveAddress + i, 0);
                WriteByte(_Recoil_Caveaddress + i, 0);

                SetOutputValue(OutputId.P1_Life + i, _Life[0]);
            }

            SetOutputValue(OutputId.Credits, (int)(ReadByte(_Credits_Address)));
        }

        #endregion
    }
}
