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
using static U_Shoot.Games.Game_WndHod3pc;

namespace U_Shoot.Games
{
    public class Game_LindberghRambo : BaseGame32
    {
        protected override string GAMEDATA_FOLDER { get { return @"Lindbergh\rambo"; } }
        protected override bool PATCH_PEB_DEBUG_FLAG { get { return false; } }

        private const string RAMBOD_SBQL_DESC = "ramboD.elf (SBQL)";
        private const string RAMBOM_SBQL_DESC = "ramboM.elf (SBQL)";
        private const string RAMBOM_SBSS_DESC = "ramboM.elf (SBSS)";

        private const string ELFPATH_OPTION = "Game ELF";
        private const string LOADEROPTIONS_OPTION = "LinuxLoader options";
        private const string TESTMENU_OPTION = "TEST Menu";

        #region LOADER MEMORY ADDRESSES                
        //Check instruction for game loaded
        private UInt32 _RomLoaded_Check_Address = 0x08073BC7;
        private UInt32 _DrawSightFlag_Address = 0x85A3BE8;
        #endregion

        #region INPUTS MEMORY ADDRESSES    
        private InjectionStruct32 _JvsRawAxes_InjectionStruct = new InjectionStruct32(0x083D696A, 7);
        private InjectionStruct32 _AdjustedAxes_InjectionStruct = new InjectionStruct32(0x08075288, 6);
        private InjectionStruct32 _Buttons_InjectionStruct = new InjectionStruct32(0x083D6729, 7);
        private InjectionStruct32 _Coins_InjectionStruct = new InjectionStruct32(0x082C6F2F, 7);

        //Custom Data
        private UInt32 _JvsRawAxes_CaveAddress;
        private UInt32 _AdjustedAxes_CaveAddress;
        private UInt32 _Buttons_CaveAddress;
        private UInt32 _Coins_CaveAddress;
        #endregion

        #region OUTPUTS MEMORY ADDRESSES
        private UInt32 _JvsMgrPtr_Address = 0x085DD158;
        private UInt32 _CreditsMgrPtr_Address = 0x085DC2B0;
        private UInt32 _PlayerMgrPtr_Address = 0x85CE9B0;
        #endregion

        public Game_LindberghRambo(GameConfig Config) : base(Config)
        {
            string sLoaderOptions = _GameConfig.GetStringOption(LOADEROPTIONS_OPTION);
            if (_GameConfig.GetBoolOption(TESTMENU_OPTION))
                sLoaderOptions = "-t " + sLoaderOptions;

            _GameLauncher.Set_CmdLine_Args("\"" + _GameConfig.GetStringOption(ELFPATH_OPTION + " [ramboM.elf]") + "\" " + sLoaderOptions);

            _KnownMd5Prints.Add(RAMBOD_SBQL_DESC, "cad71f7fa562b285feee195ee1ff32bf");
            _KnownMd5Prints.Add(RAMBOM_SBQL_DESC, "e3ce5cd7aa18be0c0a3b9d6a7928b122");
            _KnownMd5Prints.Add(RAMBOM_SBSS_DESC, "4590a750488712001d230daf69dc7fab");

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
                //And this instruction is also helping us detecting whether the game file is SBQL or SBSS binary, to call the corresponding hack
                byte[] buffer = ReadBytes(_RomLoaded_Check_Address, 3);
                if (buffer.SequenceEqual(new byte[] { 0xE8, 0x76, 0x01 }))
                {
                    Logger.WriteLog(RAMBOD_SBQL_DESC + " binary detected");
                    _TargetProcess_Md5Hash = _KnownMd5Prints[RAMBOD_SBQL_DESC];
                }
                else if (buffer.SequenceEqual(new byte[] { 0x89, 0x1C, 0x24 }))
                {
                    Logger.WriteLog(RAMBOM_SBQL_DESC + " binary detected");
                    _TargetProcess_Md5Hash = _KnownMd5Prints[RAMBOM_SBQL_DESC];
                }
                else if (buffer.SequenceEqual(new byte[] { 0xE9, 0x86, 0xEF }))
                {
                    Logger.WriteLog(RAMBOM_SBSS_DESC + " binary detected");
                    _TargetProcess_Md5Hash = _KnownMd5Prints[RAMBOM_SBSS_DESC];
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
            _AdjustedAxes_CaveAddress = InputsDatabank_Address + 0x10;
            _Buttons_CaveAddress = InputsDatabank_Address + 0x20;
            _Coins_CaveAddress = InputsDatabank_Address + 0x28;

            SetHack_JvsRawAxes();
            SetHack_AdjustedAxes();
            SetHack_Buttons();
            SetHack_Coins();

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        /// <summary>
        /// At the end of amJvspAckAnalogInput(), replacing Axis value before memory copy
        /// </summary>
        private void SetHack_JvsRawAxes()
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
        /// Replacing GetAdjstGunPos() content
        /// </summary>
        private void SetHack_AdjustedAxes()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

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
            CaveMemory.Write_StrBytes("74 11");
            //mov eax,[esp+04]
            CaveMemory.Write_StrBytes("8B 44 24 04");
            //mov byte ptr [eax],
            CaveMemory.Write_StrBytes("C6 00 FF");
            //mov eax,[esp+08]
            CaveMemory.Write_StrBytes("8B 44 24 08");
            //mov byte ptr [eax],
            CaveMemory.Write_StrBytes("C6 00 FF");
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
            //mov edx,[esp+04]
            CaveMemory.Write_StrBytes("8B 54 24 04");
            //mov byte ptr[edx],al
            CaveMemory.Write_StrBytes("88 02");
            //mov edx,[esp+08]
            CaveMemory.Write_StrBytes("8B 54 24 08");
            //shr eax, 8
            CaveMemory.Write_StrBytes("C1 E8 08");
            //mov byte ptr[edx],al
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

        /// <summary>
        /// ramboD.elf has a switch to draw sight, but ramboM does not
        /// </summary>
        protected override void Apply_NoCrosshairMemoryHack()
        {
            if (_TargetProcess_Md5Hash.Equals(_KnownMd5Prints[RAMBOD_SBQL_DESC]))
                WriteByte(_DrawSightFlag_Address, 0);
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
                if (e.ButtonName.Equals("RELOAD"))
                {
                    Apply_OR_ByteMask(_Buttons_CaveAddress + 6 + (PlayerIndex * 2), 0x01);
                }
                if (e.ButtonName.Equals("RAGE"))
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
                if (e.ButtonName.Equals("RELOAD"))
                {
                    Apply_AND_ByteMask(_Buttons_CaveAddress + 6 + (PlayerIndex * 2), 0xFE);
                }
                if (e.ButtonName.Equals("RAGE"))
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
                new GameOutput(OutputId.P1_GunRecoil),
                new GameOutput(OutputId.P2_GunRecoil),
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
            UInt32 Outputs_Address = ReadPtr(_JvsMgrPtr_Address);
            SetOutputValue(OutputId.P1_LmpStart, ReadByte(Outputs_Address) & 0x01);
            SetOutputValue(OutputId.P2_LmpStart, ReadByte(Outputs_Address) >> 1 & 0x01);
            SetOutputValue(OutputId.P1_GunRecoil, ReadByte(Outputs_Address) >> 2 & 0x01);
            SetOutputValue(OutputId.P2_GunRecoil, ReadByte(Outputs_Address) >> 3 & 0x01);

            //Custom Outputs
            UInt32 PlayersPtr_BaseAddress = ReadPtr(_PlayerMgrPtr_Address);
            for (uint i = 0; i < _GameConfig.PlayerCount; i++)
            {
                _Life[i] = 0;
                _Ammo[i] = 0;

                if (PlayersPtr_BaseAddress != 0)
                {
                    UInt32 PlayerStructAddress = ReadPtr(PlayersPtr_BaseAddress + 0x34 + (4 * i));
                    //PlayerMode:
                    //0: Standby
                    //3: Game
                    //4: Hold
                    //A, C: Continue
                    int Status = ReadByte(PlayerStructAddress + 0x38);

                    if (Status == 3 || Status == 4)
                    {
                        _Life[i] = ReadByte(PlayerStructAddress + 0x3C);
                        _Ammo[i] = ReadByte(PlayerStructAddress + 0x400);

                        //[Damaged] custom Output                
                        if (_Life[i] < _LastLife[i])
                            SetOutputValue(OutputId.P1_Damaged + i, 1);
                    }
                }

                _LastLife[i] = _Life[i];
                _LastAmmo[i] = _Ammo[i];

                SetOutputValue(OutputId.P1_Life + i, _Life[i]);
                SetOutputValue(OutputId.P1_Ammo + i, _Ammo[i]);
            }   

            //Custom recoil will be recoil just like the original one
            SetOutputValue(OutputId.P1_CtmRecoil, ReadByte(Outputs_Address) >> 2 & 0x01);
            SetOutputValue(OutputId.P2_CtmRecoil, ReadByte(Outputs_Address) >> 3 & 0x01);

            UInt32 CreditsMgr = ReadPtr(_CreditsMgrPtr_Address);
            SetOutputValue(OutputId.Credits, ReadByte(CreditsMgr + 0x38));
        }

        #endregion
    }
}
