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
    public class Game_ArcadepcRobinHood : BaseGame32
    {
        protected override string GAMEDATA_FOLDER { get { return @"Arcadepc\rhood"; } }
        protected override bool PATCH_PEB_DEBUG_FLAG { get { return true; } }

        #region LOADER MEMORY ADDRESSES                
        private UInt32 _SecurityCheck_Patch_Offset = 0x00090DC0;
        //private UInt32 _WindowedModePatch_Offset = 0x0000A694;    //Option in config file
        #endregion

        #region INPUTS MEMORY ADDRESSES    
        private UInt32 _HidDisable_Patch1_Offset = 0x0008F859;
        private UInt32 _HidDisable_Patch2_Offset = 0x0008FFAE;
        private UInt32 _GamepadAxis_Data_Offset = 0x009E6D20;
        private UInt32 _LpKeyStateBuffer_Offset = 0x009E79C8;
        private UInt32 _CoinIn_Offset = 0x00166BF30;
        private InjectionStruct32 _MouseAxis_InjectionStruct = new InjectionStruct32(0x0008F8C0, 8);
        private InjectionStruct32 _Buttons_InjectionStruct = new InjectionStruct32(0x00001365, 12);

        //Custom Input Address
        private UInt32 _CrossbowXAxis_CaveAddress = 0;
        private UInt32 _StartSwitch_CaveAddress = 0;
        private UInt32 _TriggerSwitch_CaveAddress = 0;
        private UInt32 _ServiceSwitch_CaveAddress = 0;
        private UInt32 _TestSwitch_CaveAddress = 0;
        private UInt32 _SelectSwitch_CaveAddress = 0;
        private UInt32 _AdvanceSwitch_CaveAddress = 0;
        private UInt32 _SpareSwitch_CaveAddress = 0;
        #endregion

        #region OUTPUTS MEMORY ADDRESSES
        private UInt32 _Credits_Offset = 0x009B32FC;
        private UInt32 _ShotArrowCount_Offset = 0x009C616C;
        private UInt32 _SceneIndex_Offset = 0x009C6138;
        private UInt32 _Outputs_Struct_Offset = 0x009E6DE8;
        private InjectionStruct32 _Outputs_InjectionStruct = new InjectionStruct32(0x0000137B, 5);
        private enum OutputType : int
        {
            StartLamp = 0x94,
            TicketOut = 0x96,
            Bonus_LowByte = 0x97,
            Bonus_HighByte = 0x98
        }

        //Custom Outputs value
        private UInt32 _OutputsRecopy_CaveAddress = 0;
        #endregion

        public Game_ArcadepcRobinHood(GameConfig Config) : base(Config)
        {
            _KnownMd5Prints.Add("Robin Hood v2.06-A", "6d5b7bcf5211271559ab63403d5d11f2");
            _KnownMd5Prints.Add("Robin Hood v2.04-E", "1bee8a829b18f5e33b0ba22ba467dcc9");
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
        /// This game is only using 1 silgle axis (X)
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

                    double dMinX = -1000.0;
                    double dMaxX = 1000.0;
                    double dRangeX = dMaxX - dMinX + 1;

                    PlayerController.Computed_X = Convert.ToInt32(Math.Round(dRangeX * PlayerController.Computed_X / TotalResX - dRangeX / 2.0));
                    if (PlayerController.Computed_X < (int)dMinX)
                        PlayerController.Computed_X = (int)dMinX;
                    if (PlayerController.Computed_X > (int)dMaxX)
                        PlayerController.Computed_X = (int)dMaxX;

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
            SetHack_SecurityCheck();

            //Removing call to read Game.ini value for FULLSCREEN and setting ou own forced value
            //if (_GameConfig.Windowed)
            //    WriteBytes(GameProcess_ModuleBaseAddress + 0, new byte[] { 0xC7, 0x00, 0x00, 0x00, 0x00, 0x00, 0xB8, 0x01, 0x00, 0x00, 0x00, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90 });
            //else
            //    WriteBytes(GameProcess_ModuleBaseAddress + 0, new byte[] { 0xC7, 0x00, 0x01, 0x00, 0x00, 0x00, 0xB8, 0x01, 0x00, 0x00, 0x00, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90 });
            
            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        /// <summary>
        /// The function is called periodically
        /// Just RET and doing nothing will be good
        /// </summary>
        private void SetHack_SecurityCheck()
        {
            WriteByte((UInt32)_Process_MemoryBaseAddress + _SecurityCheck_Patch_Offset, 0xC3);
        }

        #endregion        

        #region Inputs Hack

        protected override void Apply_InputsMemoryHack()
        {
            Create_InputsDataBank();     
            _CrossbowXAxis_CaveAddress = (UInt32)_InputsDatabank_Address;
            _StartSwitch_CaveAddress = (UInt32)_InputsDatabank_Address + 0x10;
            _TriggerSwitch_CaveAddress = (UInt32)_InputsDatabank_Address + 0x11;
            _ServiceSwitch_CaveAddress = (UInt32)_InputsDatabank_Address + 0x12;
            _TestSwitch_CaveAddress = (UInt32)_InputsDatabank_Address + 0x13;
            _SelectSwitch_CaveAddress = (UInt32)_InputsDatabank_Address + 0x14;
            _AdvanceSwitch_CaveAddress = (UInt32)_InputsDatabank_Address + 0x15;
            _SpareSwitch_CaveAddress = (UInt32)_InputsDatabank_Address + 0x16;

            SetHack_HidDisable();
            SetHack_MouseAxis();
            SetHack_Buttons();    

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        /// <summary>
        /// IO board can be disable by using the [system] -> HIDENABLE=0 setting in the game.ini file
        /// DEfault value is hardcoded to 1, changing it to zero so we can remove it from the config file
        /// </summary>
        private void SetHack_HidDisable()
        {
            WriteByte((UInt32)_Process_MemoryBaseAddress + _HidDisable_Patch1_Offset, 0x00);
            WriteByte((UInt32)_Process_MemoryBaseAddress + _HidDisable_Patch2_Offset, 0x00);
        }

        /// <summary>
        /// By deactivating HID in game setting file, it's using gamepad/direct input to handle axis
        /// Doing this patch will replace gamepad by our mouse handling
        /// </summary>
        private void SetHack_MouseAxis()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //push eax
            CaveMemory.Write_StrBytes("50");
            //mov eax,[_CrossbowXAxis_CaveAddress]
            CaveMemory.Write_StrBytes("8B 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_CrossbowXAxis_CaveAddress));
            //mov [game.exe+_GamepadAxis_Data_Offset], eax
            CaveMemory.Write_StrBytes("A3");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _GamepadAxis_Data_Offset));
            //pop eax
            CaveMemory.Write_StrBytes("58");
            //cvtsi2ss xmm0,[game.exe+_GamepadAxis_Data_Offset]
            CaveMemory.Write_StrBytes("F3 0F 2A 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _GamepadAxis_Data_Offset));

            //Inject it
            CaveMemory.InjectToOffset(_MouseAxis_InjectionStruct, "MouseAxis");
        }

        /// <summary>
        /// Game calls GetKeyboardState in a loop
        /// Simply replacing some values in Buffer instead of calling the API
        /// Double effect :
        /// 1) Disable all keyboard inputs
        /// 2) Enable our inputs
        /// </summary>
        private void SetHack_Buttons()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //push eax
            CaveMemory.Write_StrBytes("50");
            //push ebx
            CaveMemory.Write_StrBytes("53");

            //mov eax, GameProcess_ModuleBaseAddress +_LpKeyStateBuffer_Offset
            CaveMemory.Write_StrBytes("B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _LpKeyStateBuffer_Offset));

            //mov bl, [_TriggerSwitch_CaveAddress] // Left Mouse
            CaveMemory.Write_StrBytes("8A 1D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_TriggerSwitch_CaveAddress));
            //mov [eax+01],ebl
            CaveMemory.Write_StrBytes("88 58 01");

            //mov bl, [_StartSwitch_CaveAddress] // RETURN
            CaveMemory.Write_StrBytes("8A 1D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_StartSwitch_CaveAddress));
            //mov [eax+0D],ebl
            CaveMemory.Write_StrBytes("88 58 0D");

            //mov bl, [_ServiceSwitch_CaveAddress] // S
            CaveMemory.Write_StrBytes("8A 1D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_ServiceSwitch_CaveAddress));
            //mov [eax+53],bl
            CaveMemory.Write_StrBytes("88 58 53");

            //mov bl, [_TestSwitch_CaveAddress] // T
            CaveMemory.Write_StrBytes("8A 1D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_TestSwitch_CaveAddress));
            //mov [eax+54],ebx
            CaveMemory.Write_StrBytes("88 58 54");

            //mov bl, [_SelectSwitch_CaveAddress] // Arrow Down
            CaveMemory.Write_StrBytes("8A 1D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_SelectSwitch_CaveAddress));
            //mov [eax+28],ebx
            CaveMemory.Write_StrBytes("88 58 28");

            //mov bl, [_AdvanceSwitch_CaveAddress] // Arrow Down
            CaveMemory.Write_StrBytes("8A 1D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_AdvanceSwitch_CaveAddress));
            //mov [eax+41],ebx
            CaveMemory.Write_StrBytes("88 58 41");

            //mov bl, [_SpareSwitch_CaveAddress] // Arrow Down
            CaveMemory.Write_StrBytes("8A 1D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_SpareSwitch_CaveAddress));
            //mov [eax+52],ebx
            CaveMemory.Write_StrBytes("88 58 52");

            //pop ebx
            CaveMemory.Write_StrBytes("5B");
            //pop eax
            CaveMemory.Write_StrBytes("58");

            //Inject it
            CaveMemory.InjectToOffset(_Buttons_InjectionStruct, "Buttons");
        }
        #endregion

        #region Outputs Hack

        protected override void Apply_OutputsMemoryHack()
        {           
            Create_OutputsDataBank();

            _OutputsRecopy_CaveAddress = (UInt32)_OutputsDatabank_Address;

            SetHack_RecopyOutputs();

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        /// Outputs are stored in a roll buffer:
        /// [+0] 4 Bytes for next index
        /// [+0x0C] Output Indexes
        /// [+0x8C] Corresponding values
        ///
        /// Index:
        /// 0x94: Start Light (0=OFF, 1=ON, 0x2F=FLASH)
        /// 0x96; Tickets to send
        /// 0x97: Bonus value, low byte
        /// 0x98: Bonus value, High byte         
        private void SetHack_RecopyOutputs()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //push ebx
            CaveMemory.Write_StrBytes("53");
            //push ecx
            CaveMemory.Write_StrBytes("51");
            //xor eax,eax
            CaveMemory.Write_StrBytes("31 C0");
            //Loop:
            //cmp [game.exe+Outputs_Struct_Offset],eax
            CaveMemory.Write_StrBytes("39 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _Outputs_Struct_Offset));
            //jng Exit
            CaveMemory.Write_StrBytes("0F 8E 18 00 00 00");
            //movzx ebx,byte ptr [eax+game.exe+Outputs_ID_Table]
            CaveMemory.Write_StrBytes("0F B6 98");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _Outputs_Struct_Offset + 0x0C));
            //add ebx,_Outputs_CaveAddress
            CaveMemory.Write_StrBytes("81 C3");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_OutputsRecopy_CaveAddress));
            //mov cl,[eax+game.exe+eax+game.exe+Outputs_Values_Table]
            CaveMemory.Write_StrBytes("8A 88");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _Outputs_Struct_Offset + 0x8C));
            //mov [ebx],cl
            CaveMemory.Write_StrBytes("88 0B");
            //inc eax
            CaveMemory.Write_StrBytes("40");
            //jmp Loop
            CaveMemory.Write_StrBytes("EB DC");
            //mov [game.exe+Outputs_Struct_Offset],00000000
            CaveMemory.Write_StrBytes("C7 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _Outputs_Struct_Offset));
            CaveMemory.Write_StrBytes("00 00 00 00");
            //pop ecx
            CaveMemory.Write_StrBytes("59");
            //pop ebx
            CaveMemory.Write_StrBytes("5B");

            //mov eax, 50
            CaveMemory.Write_StrBytes("B8 50 00 00 00");

            //Inject it
            CaveMemory.InjectToOffset(_Outputs_InjectionStruct, "Outputs");
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
                byte CurrentcoinsIn = ReadByte(GameProcess_ModuleBaseAddress + _CoinIn_Offset);
                WriteByte(GameProcess_ModuleBaseAddress + _CoinIn_Offset, (byte)(CurrentcoinsIn + 1));
            }
            else if (e.ButtonName.Equals("SERVICE"))
            {
                WriteByte(_ServiceSwitch_CaveAddress, 0x80);
            }
            else if (e.ButtonName.Equals("TEST"))
            {
                WriteByte(_TestSwitch_CaveAddress, 0x80);
            }
            else if (e.ButtonName.Equals("SELECT"))
            {
                WriteByte(_SelectSwitch_CaveAddress, 0x80);
            }
            else if (e.ButtonName.Equals("ADVANCE"))
            {
                WriteByte(_AdvanceSwitch_CaveAddress, 0x80);
            }
            else if (e.ButtonName.Equals("SPARE"))
            {
                WriteByte(_SpareSwitch_CaveAddress, 0x80);
            }
        }

        protected override void GameController_OnButtonUp(object sender, VirtualGameControler.ButtonDownEventArgs e)
        {
            if (e.ButtonName.Equals("SERVICE"))
            {
                WriteByte(_ServiceSwitch_CaveAddress, 0x00);
            }
            else if (e.ButtonName.Equals("TEST"))
            {
                WriteByte(_TestSwitch_CaveAddress, 0x00);
            }
            else if (e.ButtonName.Equals("SELECT"))
            {
                WriteByte(_SelectSwitch_CaveAddress, 0x00);
            }
            else if (e.ButtonName.Equals("ADVANCE"))
            {
                WriteByte(_AdvanceSwitch_CaveAddress, 0x00);
            }
            else if (e.ButtonName.Equals("SPARE"))
            {
                WriteByte(_SpareSwitch_CaveAddress, 0x00);
            }
        }

        protected override void UpdateAxis(PlayerID Id, int X, int Y)
        {
            if (Id != PlayerID.None)
            {
                WriteBytes(_CrossbowXAxis_CaveAddress, BitConverter.GetBytes(X));
            }
        }

        protected override void PlayerController_OnButtonDown(object sender, VirtualGameControler.ButtonDownEventArgs e)
        {
            if (e.Id != PlayerID.None)
            {
                UInt32 PlayerIndex = (UInt32)e.Id;

                if (e.ButtonName.Equals("TRIGGER"))
                {
                    WriteByte(_TriggerSwitch_CaveAddress, 0x80);
                }
                else if (e.ButtonName.Equals("START"))
                {
                    WriteByte(_StartSwitch_CaveAddress, 0x80);
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
                    WriteByte(_TriggerSwitch_CaveAddress, 0x00);
                }
                else if (e.ButtonName.Equals("START"))
                {
                    WriteByte(_StartSwitch_CaveAddress, 0x00);
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
                new GameOutput(OutputId.BonusDisplay),
                new AsyncGameOutput(OutputId.P1_CtmRecoil, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_ON_DELAY), ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_OFF_DELAY), 0),
                new GameOutput(OutputId.Credits)
            };
        }

        /// <summary>
        /// Update all Outputs values before sending them to MameHooker
        /// </summary>
        public override void UpdateOutputValues()
        {
            byte StartLampState = ReadByte(_OutputsRecopy_CaveAddress + (uint)OutputType.StartLamp);
            switch (StartLampState)
            {
                case 0x00: SetOutputValue(OutputId.P1_CtmLmpStart, 0); break;
                case 0x01: SetOutputValue(OutputId.P1_CtmLmpStart, 1); break;
                case 0x2F: SetOutputValue(OutputId.P1_CtmLmpStart, -1); break;
                default: break;
            }

            //Game is filtering thanks to this value: 0 = Player increase arrow shot, 2/3/4 is attract mode increasing arrow shot
            byte SceneIndex = ReadByte(GameProcess_ModuleBaseAddress + _SceneIndex_Offset);
            _Ammo[0] = ReadInt32(GameProcess_ModuleBaseAddress + _ShotArrowCount_Offset);
            if (SceneIndex == 0)
            {
                if (_Ammo[0] > _LastAmmo[0])
                    SetOutputValue(OutputId.P1_CtmRecoil, 1);
            }
            _LastAmmo[0] = _Ammo[0];

            int Credits = ReadInt32(GameProcess_ModuleBaseAddress + _Credits_Offset);
            int BonusValueDisplay = ReadInt32(_OutputsRecopy_CaveAddress + (uint)OutputType.Bonus_LowByte);

            SetOutputValue(OutputId.BonusDisplay, BonusValueDisplay);
            SetOutputValue(OutputId.Credits, Credits);
        }

        #endregion
    }
}
