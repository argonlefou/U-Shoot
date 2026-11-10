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
    public class Game_LindberghHotd4 : BaseGame32
    {
        protected override string GAMEDATA_FOLDER { get { return @"Lindbergh\hotd4"; } }
        protected override bool PATCH_PEB_DEBUG_FLAG { get { return false; } }

        private const string HOTD4_REVA_DESC = "House of The Dead 4 (SBLC) (Rev.A)";
        private const string HOTD4_REVB_DESC = "House of The Dead 4 (SBLC) (Rev.B)";
        private const string HOTD4_REVC_DESC = "House of The Dead 4 (SBLC) (Rev.C)";
        private const string HOTD4_REVA_TEST_DESC = "House of The Dead 4 (SBLC) (Rev.A) TEST Menu";
        private const string HOTD4_REVB_TEST_DESC = "House of The Dead 4 (SBLC) (Rev.B) TEST Menu";
        private const string HOTD4_REVC_TEST_DESC = "House of The Dead 4 (SBLC) (Rev.C) TEST Menu";

        private const string ELFPATH_OPTION = "Game ELF";
        private const string LOADEROPTIONS_OPTION = "LinuxLoader options";
        private const string TESTMENU_OPTION = "TEST Menu";

        #region LOADER MEMORY ADDRESSES                
        //Rom loaded + Rom version check
        private UInt32 _RomLoaded_Check_Address = 0x08373BD8;
        private UInt32 _TestMenuLoaded_Check_Address = 0x0804E4E3;
        #endregion

        #region INPUTS MEMORY ADDRESSES    
        private InjectionStruct32 _JvsRawAxes_InjectionStruct = new InjectionStruct32(0x0831E5A8, 7);
        private InjectionStruct32 _AdjustedAxes_InjectionStruct = new InjectionStruct32(0x81538D8, 6);
        private InjectionStruct32 _Buttons_InjectionStruct = new InjectionStruct32(0x0831E3C1, 7);
        private InjectionStruct32 _Coins_InjectionStruct = new InjectionStruct32(0x083233CD, 7);

        //Custom Data
        private UInt32 _JvsRawAxes_CaveAddress;
        private UInt32 _AdjustedAxes_CaveAddress;
        private UInt32 _Buttons_CaveAddress;
        private UInt32 _Coins_CaveAddress;
        #endregion

        #region OUTPUTS MEMORY ADDRESSES
        private UInt32 _JvsMgrPtr_Address = 0x0A6F2754;
        private UInt32 _Credits_Address = 0x0A715CC0;
        private UInt32 _GunMgrPtr_Address = 0x0A6F27A8;
        private UInt32 _PlayerMgrPtr_Address = 0x0A6F27F8;
        #endregion

        public Game_LindberghHotd4(GameConfig Config) : base(Config)
        {
            string ElfPath = _GameConfig.GetStringOption(ELFPATH_OPTION + " [hod4M.elf]");

            if (_GameConfig.GetBoolOption(TESTMENU_OPTION))
                ElfPath = ElfPath.Replace("hod4M.elf", "hod4testM.elf");

            _GameLauncher.Set_CmdLine_Args("\"" + ElfPath + "\" " + _GameConfig.GetStringOption(LOADEROPTIONS_OPTION));

            _KnownMd5Prints.Add(HOTD4_REVA_DESC, "afc1c946193c2533eb131a259c9cdb66");
            _KnownMd5Prints.Add(HOTD4_REVB_DESC, "619511471598b5a1e89f3976c76ec83b");
            _KnownMd5Prints.Add(HOTD4_REVC_DESC, "036408020B362255455B84028618352B");
            _KnownMd5Prints.Add(HOTD4_REVA_TEST_DESC, "dd4d5dcf8049d12c7154d7e0a1570ee5");
            _KnownMd5Prints.Add(HOTD4_REVB_TEST_DESC, "c85ce9b4fb9d8084e009ac2479ace3c0");
            _KnownMd5Prints.Add(HOTD4_REVC_TEST_DESC, "1456c39df28b82f3eb2e09b56b141b15");

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
                if (_GameConfig.GetBoolOption(TESTMENU_OPTION))
                {
                    byte[] buffer = ReadBytes(_TestMenuLoaded_Check_Address, 3);
                    if (buffer.SequenceEqual(new byte[] { 0xC0, 0x00, 0x00 }))
                    {
                        Logger.WriteLog(HOTD4_REVA_TEST_DESC + " binary detected");
                        _TargetProcess_Md5Hash = _KnownMd5Prints[HOTD4_REVA_TEST_DESC];
                    }
                    else if (buffer.SequenceEqual(new byte[] { 0xC8, 0xFB, 0x06 }))
                    {
                        Logger.WriteLog(HOTD4_REVB_TEST_DESC + " binary detected");
                        _TargetProcess_Md5Hash = _KnownMd5Prints[HOTD4_REVB_TEST_DESC];
                    }
                    else if (buffer.SequenceEqual(new byte[] { 0xA8, 0xFB, 0x06 }))
                    {
                        Logger.WriteLog(HOTD4_REVC_TEST_DESC + " binary detected");
                        _TargetProcess_Md5Hash = _KnownMd5Prints[HOTD4_REVC_TEST_DESC];
                    }
                    else
                    {
                        Logger.WriteLog("Game not Loaded, waiting...");
                        return;
                    }
                }
                else
                {
                    byte[] buffer = ReadBytes(_RomLoaded_Check_Address, 3);
                    if (buffer.SequenceEqual(new byte[] { 0x04, 0xDE, 0xC1 }))
                    {
                        Logger.WriteLog(HOTD4_REVA_DESC + " binary detected");
                        _TargetProcess_Md5Hash = _KnownMd5Prints[HOTD4_REVA_DESC];
                    }
                    else if (buffer.SequenceEqual(new byte[] { 0x11, 0x0F, 0x57 }))
                    {
                        Logger.WriteLog(HOTD4_REVB_DESC + " binary detected");
                        _TargetProcess_Md5Hash = _KnownMd5Prints[HOTD4_REVB_DESC];
                    }
                    else if (buffer.SequenceEqual(new byte[] { 0x83, 0xEC, 0x14 }))
                    {
                        Logger.WriteLog(HOTD4_REVC_DESC + " binary detected");
                        _TargetProcess_Md5Hash = _KnownMd5Prints[HOTD4_REVC_DESC];
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

            if (_TargetProcess_Md5Hash.Equals(_KnownMd5Prints[HOTD4_REVA_DESC]) || _TargetProcess_Md5Hash.Equals(_KnownMd5Prints[HOTD4_REVA_TEST_DESC]))
            {
                //lea ecx,[eax-B]
                CaveMemory.Write_StrBytes("8D 48 F5");
                //movzx ecx,word ptr [ecx+_Axes_CaveAddress]
                CaveMemory.Write_StrBytes("0F B6 89");
                CaveMemory.Write_Bytes(BitConverter.GetBytes(_JvsRawAxes_CaveAddress));
                //lea eax,[eax+edx+00000102]
                CaveMemory.Write_StrBytes("8D 84 10 02 01 00 00");
                //mov [eax],cx
                CaveMemory.Write_StrBytes("66 89 08");
            }
            else
            {
                //lea ecx,[eax-B]
                CaveMemory.Write_StrBytes("8D 48 F5");
                //movzx ecx,word ptr [ecx+_Axes_CaveAddress]
                CaveMemory.Write_StrBytes("0F B6 89");
                CaveMemory.Write_Bytes(BitConverter.GetBytes(_JvsRawAxes_CaveAddress));
                //lea eax,[eax+esi+00000102]
                CaveMemory.Write_StrBytes("8D 84 30 02 01 00 00");
                //mov [eax],cx
                CaveMemory.Write_StrBytes("66 89 08");
            }

            //Inject it
            CaveMemory.InjectToAddress(_JvsRawAxes_InjectionStruct, "Axes");
        }

        /// <summary>
        /// Replacing GetAdjstGunPos() content
        /// </summary>
        private void SetHack_AdjustedAxes()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //mov ebx,eax
            CaveMemory.Write_StrBytes("8B D8");
            //mov eax,[esp+18]
            CaveMemory.Write_StrBytes("8B 44 24 18");
            //shl eax,1
            CaveMemory.Write_StrBytes("D1 E0");
            //movzx eax,byte ptr [eax+_Buttons_CaveAddress + 6]
            CaveMemory.Write_StrBytes("0F B6 80");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Buttons_CaveAddress + 6));
            //and al,1
            CaveMemory.Write_StrBytes("24 01");
            //test al,al
            CaveMemory.Write_StrBytes("84 C0");
            //je OnScreen
            CaveMemory.Write_StrBytes("74 0F");
            //mov [ebx],000000FF
            CaveMemory.Write_StrBytes("C7 03 FF 00 00 00");
            //mov [edx],000000FF
            CaveMemory.Write_StrBytes("C7 02 FF 00 00 00");
            //xor eax,eax
            CaveMemory.Write_StrBytes("31 C0");
            //ret
            CaveMemory.Write_StrBytes("C3");

            //OnScreen:
            //mov eax,[esp+18]
            CaveMemory.Write_StrBytes("8B 44 24 18");
            //shl eax,1
            CaveMemory.Write_StrBytes("D1 E0");
            //add eax,_AdjustedAxes_CaveAddress
            CaveMemory.Write_StrBytes("05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_AdjustedAxes_CaveAddress));
            //movzx eax,word ptr [eax]
            CaveMemory.Write_StrBytes("0F B7 00");
            //mov [ebx],00000000
            CaveMemory.Write_StrBytes("C7 03 00 00 00 00");
            //mov [edx],00000000
            CaveMemory.Write_StrBytes("C7 02 00 00 00 00");
            //mov [ebx],al
            CaveMemory.Write_StrBytes("88 03");
            //shr eax, 8
            CaveMemory.Write_StrBytes("C1 E8 08");
            //mov [edx],al
            CaveMemory.Write_StrBytes("88 02");
            //mov eax,00000001
            CaveMemory.Write_StrBytes("B8 01 00 00 00");
            //ret
            CaveMemory.Write_StrBytes("C3");

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

                //Nexts channels (Bytes 8-15) have gun acceleration analog data. Default is 0x8000 when not moving, then goes up or down according to the acceleration direction for each axis
                //Not setting these will stop gun shaking from moving
                //For now on, shaking will be activated by shooting of screen until I find a way to perperly compute it
                WriteBytes(_JvsRawAxes_CaveAddress + 0x08 + ((uint)Id * 4), new byte[] { 0x80, 0x00 });
                WriteBytes(_JvsRawAxes_CaveAddress + 0x0A + ((uint)Id * 4), new byte[] { 0x80, 0x00 });

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

                    //Using Reload button to shake guns
                    WriteByte(_JvsRawAxes_CaveAddress + 0x08 + (PlayerIndex * 4), 0x00);
                }
                else if (e.ButtonName.Equals("GRENADE"))
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

                    WriteByte(_JvsRawAxes_CaveAddress + 0x08 + (PlayerIndex * 4), 0x80);
                }
                else if (e.ButtonName.Equals("GRENADE"))
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
            _Outputs = new List<GameOutput>
            {
                new GameOutput(OutputId.P1_LmpStart),
                new GameOutput(OutputId.P2_LmpStart),
                new GameOutput(OutputId.P1_GunMotor),
                new GameOutput(OutputId.P2_GunMotor),
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
            //Original Outputs
            UInt32 Outputs_Address = ReadUInt32(_JvsMgrPtr_Address);
            int P1_Motor_Status = ReadByte(Outputs_Address) >> 6 & 0x01;
            int P2_Motor_Status = ReadByte(Outputs_Address) >> 3 & 0x01;
            SetOutputValue(OutputId.P1_LmpStart, ReadByte(Outputs_Address) >> 7 & 0x01);
            SetOutputValue(OutputId.P2_LmpStart, ReadByte(Outputs_Address) >> 4 & 0x01);
            SetOutputValue(OutputId.P1_GunMotor, P1_Motor_Status);
            SetOutputValue(OutputId.P2_GunMotor, P2_Motor_Status);

            if (!_GameConfig.GetBoolOption(TESTMENU_OPTION))
            {

                //Custom Outputs
                UInt32 GameModePtr = ReadUInt32(ReadPtr(_GunMgrPtr_Address) + 0x2C);
                int GameMode = ReadByte(GameModePtr + 0x38);
                UInt32 PlayersPtr_BaseAddress = ReadPtr(_PlayerMgrPtr_Address);
                for (uint i = 0; i < _GameConfig.PlayerCount; i++)
                {
                    _Life[i] = 0;
                    _Ammo[i] = 0;

                    if (GameMode == 8)
                    {
                        if (PlayersPtr_BaseAddress != 0)
                        {
                            UInt32 Player_StructAddress = ReadPtr(PlayersPtr_BaseAddress + 0x34 + (i * 0x04));

                            //PlayerMode:
                            //4: CutScene
                            //3: InGame
                            //9, 11: Continue
                            //0: GameOver
                            int Player_Mode = ReadByte(Player_StructAddress + 0x38);
                            if (Player_Mode == 3 || Player_Mode == 4)
                            {
                                _Life[i] = ReadByte(Player_StructAddress + 0x3C);
                                _Ammo[i] = ReadByte(Player_StructAddress + 0x274);

                                //[Damaged] custom Output                
                                if (_Life[i] < _LastLife[i])
                                    SetOutputValue(OutputId.P1_Damaged + i, 1);

                                //[Recoil] custom output :
                                //Generate new event for each bullet fired, only while original motor event is ON
                                //(this way, no recoil during attract mode)
                                if (_Ammo[i] < _LastAmmo[i])
                                    SetOutputValue(OutputId.P1_CtmRecoil + i, 1);
                            }
                        }
                    }

                    _LastLife[i] = _Life[i];
                    _LastAmmo[i] = _Ammo[i];

                    SetOutputValue(OutputId.P1_Life + i, _Life[i]);
                    SetOutputValue(OutputId.P1_Ammo + i, _Ammo[i]);
                }
            }

            SetOutputValue(OutputId.Credits, ReadByte(_Credits_Address));
        }

        #endregion
    }
}
