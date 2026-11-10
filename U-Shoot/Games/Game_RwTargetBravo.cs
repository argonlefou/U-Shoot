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
    public class Game_RwTargetBravo : BaseGame32
    {
        protected override bool PATCH_PEB_DEBUG_FLAG { get { return false; } }

        private const string FREEPLAY_OPTION = "Freeplay";
        private const string CREDITS_START_OPTION = "Credits to Start";
        private const string CREDITS_CONTINUE_OPTION = "Credits to Continue";
        private const string COINPERCREDIT_OPTION = "Coins per Credit";

        #region LOADER MEMORY ADDRESSES                
        private UInt32 _AmLibData_Ptr_Offset = 0x02671580;
        private InjectionStruct32 _Credits_InjectionStruct = new InjectionStruct32(0x0001B62D, 5);
        private InjectionStruct32 _NoCursor_InjectionStruct = new InjectionStruct32(0x00117D20, 6);
        #endregion

        #region INPUTS MEMORY ADDRESSES    
        private UInt32 _EnableJvs_Patch_Offset = 0x000133AD;
        private UInt32 _EnableAmLib_Patch_Offset = 0x00013416;
        private UInt32 _EnableLedBoard_Patch_Offset = 0x000133E0;
        private UInt32 _P2Axis_Address_Ptr_Offset = 0x026669C4;
        private NopStruct32 _Nop_InputDevices = new NopStruct32(0x1C5CA, 6);
        private NopStruct32 _Nop_P2Axis_X = new NopStruct32(0x000A1A84, 3);
        private NopStruct32 _Nop_P2Axis_Y = new NopStruct32(0x000A1A62, 3);
        private InjectionStruct32 _Axes_InjectionStruct = new InjectionStruct32(0x000A195E, 6);
        private InjectionStruct32 _Buttons_InjectionStruct = new InjectionStruct32(0x000A2905, 7);
        private InjectionStruct32 _P1_Reload_InjectionStruct = new InjectionStruct32(0x000A196A, 6);

        //Custom Data
        private UInt32 _P1_X_CaveAddress;
        private UInt32 _P1_Y_CaveAddress;
        private UInt32 _P1_Reload_CaveAddress;
        private UInt32 _LpKeyState_CaveAddress;
        #endregion

        #region OUTPUTS MEMORY ADDRESSES
        private UInt32 _InternalLedOutputs_Ptr_Offset = 0x006669AC;
        private InjectionStruct32 _InternalRecoil_InjectionStruct = new InjectionStruct32(0x000318D0, 5);

        //Custom Data
        private UInt32 _RecoilStatus_CaveAddress;
        #endregion

        public Game_RwTargetBravo(GameConfig Config) : base(Config)
        {
            //Windows name will be LUA file loaded with '/' in path name
            _IsAnyWindowTitlePossible = true;

            _KnownMd5Prints.Add("Target Bravo - Original Dump", "a93befe8fa764059b96296f2d40bb5db");
            _KnownMd5Prints.Add("Target Bravo - 1080p patched", "3b508229088a65baf0329469e1aedab3");

            //_GameLauncher.SetArguments("-testmode");
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

                    double dMinX = 0;
                    double dMinY = 0;
                    double dMaxX = 1024.0;
                    double dMaxY = 600.0;
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

        #region Loader Hack

        protected override void Apply_LoaderMemoryHack()
        {
            SetHack_Credits();

            if (_GameConfig.NoCursor)
            {
                if (CreateTransparentCursor())
                    SetHack_NoCursor();
            }

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        /// <summary>
        /// Replacing call to LoadCursor() by  setting hCursor value in EAX
        /// </summary>
        private void SetHack_NoCursor()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //mov eax, _CustomTransparentCursorHandle
            CaveMemory.Write_StrBytes("B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes((UInt32)_CustomTransparentCursorHandle));
            //add esp, 8
            CaveMemory.Write_StrBytes("83 C4 08");

            //Inject it
            CaveMemory.InjectToOffset(_NoCursor_InjectionStruct, "No Cursor");
        }

        /// <summary>
        /// Just after the game has loaded the AmLibDataStruct, overriding needed values
        /// </summary>
        private void SetHack_Credits()
        {
            byte bFreeplay = _GameConfig.GetBoolOption(FREEPLAY_OPTION, false) ? (byte)0x01 : (byte)0x00;
            byte bStart = 1;
            byte bContinue = 1;
            byte bCoinsCredits = 1;
            byte.TryParse(_GameConfig.GetStringOption(CREDITS_START_OPTION, "1"), out bStart);
            byte.TryParse(_GameConfig.GetStringOption(CREDITS_CONTINUE_OPTION, "1"), out bContinue);
            byte.TryParse(_GameConfig.GetStringOption(COINPERCREDIT_OPTION, "1"), out bCoinsCredits);

            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //[gs2.exe+_AmLibData_Ptr_Offset], eax
            CaveMemory.Write_StrBytes("A3");
            byte[] b = BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _AmLibData_Ptr_Offset);
            CaveMemory.Write_Bytes(b);
            //mov byte ptr [eax+000001C8], FREEPLAY
            CaveMemory.Write_StrBytes("C6 80 C8 01 00 00");
            CaveMemory.Write_Byte(bFreeplay);
            //mov byte ptr [eax+000001CC], CREDITS TO START
            CaveMemory.Write_StrBytes("C6 80 CC 01 00 00");
            CaveMemory.Write_Byte(bStart);
            //mov byte ptr [eax+000001D0], CREDITS TO CONTINUE
            CaveMemory.Write_StrBytes("C6 80 D0 01 00 00");
            CaveMemory.Write_Byte(bContinue);
            //mov byte ptr [eax+000001D4], COINS PER CREDIT
            CaveMemory.Write_StrBytes("C6 80 D4 01 00 00");
            CaveMemory.Write_Byte(bCoinsCredits);
            //mov byte ptr [eax+000001D8], COINS PER CREDIT
            CaveMemory.Write_StrBytes("C6 80 D8 01 00 00");
            CaveMemory.Write_Byte(bCoinsCredits);

            //Inject it
            CaveMemory.InjectToOffset(_Credits_InjectionStruct, "Credits");
        }

        #endregion        

        #region Inputs Hack

        protected override void Apply_InputsMemoryHack()
        {
            Create_InputsDataBank();
            _P1_X_CaveAddress = InputsDatabank_Address;
            _P1_Y_CaveAddress = InputsDatabank_Address + 0x04;
            _P1_Reload_CaveAddress = InputsDatabank_Address + 0x10;
            _LpKeyState_CaveAddress = InputsDatabank_Address + 0x20;

            //Force EnableJvs = FALSE after reading ini file
            //xor eax, eax
            WriteBytes(GameProcess_ModuleBaseAddress + _EnableJvs_Patch_Offset, new byte[] { 0x31, 0xC0, 0x90 });

            //Force EnableAmLib = FALSE after reading ini file
            //xor eax, eax
            WriteBytes(GameProcess_ModuleBaseAddress + _EnableAmLib_Patch_Offset, new byte[] { 0x31, 0xC0, 0x90 });

            //Force EnableLedBoard = FALSE after reading ini file
            //xor eax, eax
            WriteBytes(GameProcess_ModuleBaseAddress + _EnableLedBoard_Patch_Offset, new byte[] { 0x31, 0xC0, 0xEB, 0x0A, 0x90, 0x90, 0x90 });

            //NOP jump if EnableInputDevices is not set in config
            SetNops(GameProcess_ModuleBaseAddress, _Nop_InputDevices);

            SetHack_Axis();
            SetHack_Buttons();
            SetHack_P1_Reload();

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        /// <summary>
        /// For this hack we will override the writing of X and Y data issued from
        /// the legit ScrenToClient call, with our own calculated values
        /// </summary>
        private void SetHack_Axis()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //mov ecx, [_P1_X_Address]
            CaveMemory.Write_StrBytes("8B 0D");
            byte[] b = BitConverter.GetBytes(_P1_X_CaveAddress);
            CaveMemory.Write_Bytes(b);
            //mov edx, [_P1_Y_Address]
            CaveMemory.Write_StrBytes("8B 15");
            b = BitConverter.GetBytes(_P1_Y_CaveAddress);
            CaveMemory.Write_Bytes(b);

            //Inject it
            CaveMemory.InjectToOffset(_Axes_InjectionStruct, "P1 Axes");

            //Noping procedures for P2
            SetNops(GameProcess_ModuleBaseAddress, _Nop_P2Axis_X);
            SetNops(GameProcess_ModuleBaseAddress, _Nop_P2Axis_Y);
        }

        /// <summary>
        /// Replacing GetKeyboardState call, filling the buffer with our own values (see WINUSER.H for virtualkey codes)
        /// </summary>
        private void SetHack_Buttons()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //push ecx
            CaveMemory.Write_StrBytes("51");
            //push esi
            CaveMemory.Write_StrBytes("56");
            //push edi
            CaveMemory.Write_StrBytes("57");
            //mov esi,_LpKeyState_CaveAddress
            CaveMemory.Write_StrBytes("BE");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_LpKeyState_CaveAddress));
            //mov edi,ecx
            CaveMemory.Write_StrBytes("8B F9");
            //mov ecx,00000040
            CaveMemory.Write_StrBytes("B9 40 00 00 00");
            //repe movsd
            CaveMemory.Write_StrBytes("F3 A5");
            //pop edi
            CaveMemory.Write_StrBytes("5F");
            //pop esi
            CaveMemory.Write_StrBytes("5E");
            //pop ecx
            CaveMemory.Write_StrBytes("59");

            //Inject it
            CaveMemory.InjectToOffset(_Buttons_InjectionStruct, "Buttons");
        }

        /// <summary>
        /// Only way to reload with P1 using non-JVS controls os to point to the border of the screen
        /// Inserting a way to reload by pressing a button by forcing axis to be 0
        /// </summary>
        private void SetHack_P1_Reload()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //cmp byte ptr [_P1_Reload_CaveAddress],01
            CaveMemory.Write_StrBytes("80 3D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_Reload_CaveAddress));
            CaveMemory.Write_StrBytes("01");
            //jne OriginalCode
            CaveMemory.Write_StrBytes("75 05");
            //mov ecx,00000000
            CaveMemory.Write_StrBytes("B9 00 00 00 00");
            //OriginalCode:
            //cmp ecx,01
            CaveMemory.Write_StrBytes("83 F9 01");
            //mov [eax+28],ecx
            CaveMemory.Write_StrBytes("89 48 28");

            //Inject it
            CaveMemory.InjectToOffset(_P1_Reload_InjectionStruct, "P1 Reload");
        }

        #endregion

        #region Outputs Hack

        protected override void Apply_OutputsMemoryHack()
        {
            Create_OutputsDataBank();
            _RecoilStatus_CaveAddress = OutputsDatabank_Address;

            SetHack_Recoil();

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        /// <summary>
        /// In that function 'RequestRecoil()' the game calls a procedure to get the WeaponManager corresponding to the event
        /// When JVS emulation is enabled, it returns 3(P1) or 4(P2)
        /// When JVS emulation is disabled, it returns 1(P1) or 2(P2)
        /// When JVS emulation is enabled, output can be obtained by the JVS report bytes, so this injection will only be needed for non-JVS
        /// </summary>
        private void SetHack_Recoil()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //mov eax,[eax]
            CaveMemory.Write_StrBytes("8B 00");
            //push eax
            CaveMemory.Write_StrBytes("50");
            //sub eax,01
            CaveMemory.Write_StrBytes("83 E8 01");
            //add eax,_Recoil_CaveAddress
            CaveMemory.Write_StrBytes("05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_RecoilStatus_CaveAddress));
            //mov byte ptr [eax],01
            CaveMemory.Write_StrBytes("C6 00 01");
            //pop eax
            CaveMemory.Write_StrBytes("58");
            //sub eax,03
            CaveMemory.Write_StrBytes("83 E8 03");

            //Inject it
            CaveMemory.InjectToOffset(_InternalRecoil_InjectionStruct, "Recoil");
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
            else if (e.ButtonName.Equals("COIN"))
            {
                //Without JVS, credits are sent by presing CTRL+F1, which is a shortcut to a menu -> insert coin
                //This send a WM_COMMAND (msg = 0x111) to the game with wParam = 0xA413
                Win32.PostMessage(GetGameWindowHandle(), 0x111, new IntPtr(0xA413), IntPtr.Zero);
            }
        }

        protected override void GameController_OnButtonUp(object sender, VirtualGameControler.ButtonDownEventArgs e)
        { }

        protected override void UpdateAxis(PlayerID Id, int X, int Y)
        {
            if (Id != PlayerID.None)
            {
                byte[] bufferX = BitConverter.GetBytes(X);
                byte[] bufferY = BitConverter.GetBytes(Y);

                switch (Id)
                {
                    case PlayerID.Player1:
                        {
                            WriteBytes(_P1_X_CaveAddress, bufferX);
                            WriteBytes(_P1_Y_CaveAddress, bufferY);
                            break;
                        }
                    case PlayerID.Player2:
                        {
                            WriteBytes(ReadPtr(GameProcess_ModuleBaseAddress + _P2Axis_Address_Ptr_Offset) + 0x28, bufferX);
                            WriteBytes(ReadPtr(GameProcess_ModuleBaseAddress + _P2Axis_Address_Ptr_Offset) + 0x2C, bufferY);
                            WriteBytes(_P1_Y_CaveAddress, bufferY);
                            break;
                        }
                    default: break;
                }
            }
        }

        protected override void PlayerController_OnButtonDown(object sender, VirtualGameControler.ButtonDownEventArgs e)
        {
            if (e.Id != PlayerID.None)
            {
                if (e.ButtonName.Equals("TRIGGER"))
                {
                    switch (e.Id)
                    {
                        case PlayerID.Player1: WriteByte(_LpKeyState_CaveAddress + 0x01, 0x80); break;
                        case PlayerID.Player2: WriteByte(_LpKeyState_CaveAddress + 0x65, 0x80); break;
                        default: break;
                    }
                }
                else if (e.ButtonName.Equals("RELOAD"))
                {
                    switch (e.Id)
                    {
                        case PlayerID.Player1: WriteByte(_P1_Reload_CaveAddress, 0x01); break;
                        case PlayerID.Player2: WriteByte(_LpKeyState_CaveAddress + 0x60, 0x80); break;
                        default: break;
                    }
                }
                else if (e.ButtonName.Equals("CHANGE"))
                {
                    switch (e.Id)
                    {
                        case PlayerID.Player1: WriteByte(_LpKeyState_CaveAddress + 0x02, 0x80); break;
                        case PlayerID.Player2: WriteByte(_LpKeyState_CaveAddress + 0x6E, 0x80); break;
                        default: break;
                    }
                }
                else if (e.ButtonName.Equals("ACTION"))
                {
                    switch (e.Id)
                    {
                        case PlayerID.Player1: WriteByte(_LpKeyState_CaveAddress + 0x04, 0x80); break;
                        case PlayerID.Player2: WriteByte(_LpKeyState_CaveAddress + 0x6D, 0x80); break;
                        default: break;
                    }
                }
                else if (e.ButtonName.Equals("START"))
                {
                    switch (e.Id)
                    {
                        case PlayerID.Player1: WriteByte(_LpKeyState_CaveAddress + 0x0D, 0x80); break;
                        case PlayerID.Player2: WriteByte(_LpKeyState_CaveAddress + 0x6B, 0x80); break;
                        default: break;
                    }
                }
            }
        }
        protected override void PlayerController_OnButtonUp(object sender, VirtualGameControler.ButtonDownEventArgs e)
        {
            if (e.Id != PlayerID.None)
            {
                if (e.ButtonName.Equals("TRIGGER"))
                {
                    switch (e.Id)
                    {
                        case PlayerID.Player1: WriteByte(_LpKeyState_CaveAddress + 0x01, 0x00); break;
                        case PlayerID.Player2: WriteByte(_LpKeyState_CaveAddress + 0x65, 0x00); break;
                        default: break;
                    }
                }
                else if (e.ButtonName.Equals("RELOAD"))
                {
                    switch (e.Id)
                    {
                        case PlayerID.Player1: WriteByte(_P1_Reload_CaveAddress, 0x00); break;
                        case PlayerID.Player2: WriteByte(_LpKeyState_CaveAddress + 0x60, 0x00); break;
                        default: break;
                    }
                }
                else if (e.ButtonName.Equals("CHANGE"))
                {
                    switch (e.Id)
                    {
                        case PlayerID.Player1: WriteByte(_LpKeyState_CaveAddress + 0x02, 0x00); break;
                        case PlayerID.Player2: WriteByte(_LpKeyState_CaveAddress + 0x6E, 0x00); break;
                        default: break;
                    }
                }
                else if (e.ButtonName.Equals("ACTION"))
                {
                    switch (e.Id)
                    {
                        case PlayerID.Player1: WriteByte(_LpKeyState_CaveAddress + 0x04, 0x00); break;
                        case PlayerID.Player2: WriteByte(_LpKeyState_CaveAddress + 0x6D, 0x00); break;
                        default: break;
                    }
                }
                else if (e.ButtonName.Equals("START"))
                {
                    switch (e.Id)
                    {
                        case PlayerID.Player1: WriteByte(_LpKeyState_CaveAddress + 0x0D, 0x00); break;
                        case PlayerID.Player2: WriteByte(_LpKeyState_CaveAddress + 0x6B, 0x00); break;
                        default: break;
                    }
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
                new GameOutput(OutputId.LmpBillboard),
                new GameOutput(OutputId.P1_LmpHolder),
                new GameOutput(OutputId.P2_LmpHolder),
                new GameOutput(OutputId.P1_GunRecoil),
                new GameOutput(OutputId.P2_GunRecoil),
                new AsyncGameOutput(OutputId.P1_CtmRecoil, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_ON_DELAY), ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_OFF_DELAY), 0),
                new AsyncGameOutput(OutputId.P2_CtmRecoil, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_ON_DELAY), ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_OFF_DELAY), 0),
                new GameOutput(OutputId.Credits)
            };
        }

        /// <summary>
        /// Update all Outputs values before sending them to MameHooker
        /// </summary>
        public override void UpdateOutputValues()
        {
            //ReadPtr() + 15E0 = Outputs
            //1 Byte per output (0 / 1)
            // +0 = Billboard
            // +1 = P1 Holder
            // +2 = P2 Holder
            // +3 = Unused --
            // +4 = P1 START
            // +5 = P2 START
            UInt32 LedStatusAddress = ReadPtr(GameProcess_ModuleBaseAddress + _InternalLedOutputs_Ptr_Offset) + 0x15E0;
            SetOutputValue(OutputId.P1_LmpStart, ReadByte(LedStatusAddress + 4));
            SetOutputValue(OutputId.P2_LmpStart, ReadByte(LedStatusAddress + 5));
            SetOutputValue(OutputId.LmpBillboard, ReadByte(LedStatusAddress));
            SetOutputValue(OutputId.P1_LmpHolder, ReadByte(LedStatusAddress + 1));
            SetOutputValue(OutputId.P2_LmpHolder, ReadByte(LedStatusAddress + 2));

            for (uint i = 0; i < _GameConfig.PlayerCount; i++)
            {
                if (ReadByte(_RecoilStatus_CaveAddress + i) == 1)
                {
                    SetOutputValue(OutputId.P1_CtmRecoil + i, 1);
                    WriteByte(_RecoilStatus_CaveAddress + i, 0x00);
                }
            }

            //Credits will be calculated by using the formula  : Credits = Coins / CoinsByCredits
            //Warning : Need to handle "Divide by 0" error if game is closed brutally ! 
            int Credits = 0;
            UInt32 AmLibData_Address = ReadPtr(GameProcess_ModuleBaseAddress + _AmLibData_Ptr_Offset);
            try
            {
                if (AmLibData_Address != 0)
                    Credits = ReadByte(AmLibData_Address + 0x1E0) / ReadByte(AmLibData_Address + 0x1D4);
            }
            catch { }
            SetOutputValue(OutputId.Credits, Credits);
        }

        #endregion
    }
}
