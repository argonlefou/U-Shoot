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
    public class Game_GameWaxAkuma : BaseGame32
    {
        protected override bool PATCH_PEB_DEBUG_FLAG { get { return true; } }

        #region LOADER MEMORY ADDRESSES                
        private UInt32 _Dk2Driver_Patch_Offset = 0x00001485;
        private UInt32 _Dk2ErrorCheck_Patch_Offset = 0x00002213;
        private InjectionStruct32 _WindowedMode_InjectionStruct = new InjectionStruct32(0x00001293, 6);
        #endregion

        #region INPUTS MEMORY ADDRESSES    
        private UInt32 _PlayersInfo_BaseOffset = 0x00190CE8;
        private UInt32 _Player1_X_Offset = 0x30;
        private UInt32 _Player1_Y_Offset = 0x32;
        private UInt32 _Player1_ForceNullAxis_Offset = 0x38;
        private UInt32 _Player1_ActivateAim_Offset = 0x39;
        private UInt32 _ExitKEy_Patch_Offset = 0x0000109B;
        private NopStruct32 _Axis_Nop_Offset = new NopStruct32(0x000158D4, 5);
        private InjectionStruct32 _Buttons_InjectionStruct = new InjectionStruct32(0x000157F1, 6);

        //Custom Data
        private UInt32 DInputBuffer_CaveAddress;
        #endregion

        #region OUTPUTS MEMORY ADDRESSES
        private UInt32 _Outputs_Offset = 0x78;
        private UInt32 _Player1_InGame_Offset = 0x7E;
        private UInt32 _Player1_SHotCount_Offset = 0x5E3B48; //Also writing in 0x5E3B4C, 0x5E3B64, 0x5E3B68
        private UInt32 _Player1_Life_Offset = 0x5E3B08;
        private UInt32 _Credits_Offset = 0x00190F02;
        #endregion

        int[] _ShotCount;
        int[] _LastShotCount;

        public Game_GameWaxAkuma(GameConfig Config) : base(Config)
        {
            _KnownMd5Prints.Add("Akuma Mortis Immortal v81.07 - Original Dump", "4143b362a0cced0f6633974c710d91f9");
            _KnownMd5Prints.Add("Akuma Mortis Immortal v81.07 - Boot patched v1", "a8deac2b2c90187cce90fb09f767a695");

            _ShotCount = new int[_GameConfig.PlayerCount];
            _LastShotCount = new int[_GameConfig.PlayerCount];
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
                    double dMaxX = 639.0;
                    double dMaxY = 479.0;
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
            //Skip DK2 Driver check
            WriteByte(GameProcess_ModuleBaseAddress + _Dk2Driver_Patch_Offset, 0xEB);
            //Skip Error Code check if any was set before that point
            WriteBytes(GameProcess_ModuleBaseAddress + _Dk2ErrorCheck_Patch_Offset, new byte[] { 0x90, 0x90 });

            if (_GameConfig.Windowed)
                SetHack_WindowedMode();

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        /// <summary>
        /// Overriding the D3DPRESENT_PARAMETERS::Windowed field from IDirect3D9::CreateDevice() call
        /// </summary>
        private void SetHack_WindowedMode()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //mov [esi+20],00000001
            CaveMemory.Write_StrBytes("C7 46 20 01 00 00 00");
            //mov [esi],00000280
            CaveMemory.Write_StrBytes("C7 06 80 02 00 00");

            //Inject it
            CaveMemory.InjectToOffset(_WindowedMode_InjectionStruct, "Windowed Mode");

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");            
        }

        #endregion        

        #region Inputs Hack

        protected override void Apply_InputsMemoryHack()
        {
            Create_InputsDataBank();
            DInputBuffer_CaveAddress = InputsDatabank_Address;

            // Wndroc is exiting the game if WM_KEYDOWN is receined with HOME button. Skipping that
            WriteByte(GameProcess_ModuleBaseAddress + _ExitKEy_Patch_Offset, 0xEB);

            //One of the existing package has a modified inpout32.dll acting on axis values with Mouse
            //Original dump does nothing
            //So to be sure values are clean for Demulshooter, disabling the call to the original functions filling the values
            SetNops(GameProcess_ModuleBaseAddress, _Axis_Nop_Offset);

            SetHack_Buttons();

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        /// <summary>
        /// After the game as received the buffer with some DInput call, we will replace the buffer content with one we created to put our own inputs
        /// </summary>
        private void SetHack_Buttons()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //mov [esp+14],edx
            CaveMemory.Write_StrBytes("89 54 24 14");
            //push esi
            CaveMemory.Write_StrBytes("56");
            //push edi
            CaveMemory.Write_StrBytes("57");
            //mov ecx,00000030
            CaveMemory.Write_StrBytes("B9 30 00 00 00");
            //mov esi, _DInputBuffer_CaveAddress + 8
            CaveMemory.Write_StrBytes("BE");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(DInputBuffer_CaveAddress + 0x8));
            //mov edi,esp
            CaveMemory.Write_StrBytes("8B FC");
            //add edi, 10
            CaveMemory.Write_StrBytes("83 C7 10");
            //repe movsd
            CaveMemory.Write_StrBytes("F3 A5");           
            //pop edi
            CaveMemory.Write_StrBytes("5F");
            //pop esi
            CaveMemory.Write_StrBytes("5E");
            //xor eax, eax
            CaveMemory.Write_StrBytes("33 C0");

            //Inject it
            CaveMemory.InjectToOffset(_Buttons_InjectionStruct, "Buttons");
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
                WriteByte(DInputBuffer_CaveAddress + 0x37, 0x80);
            }
            else if (e.ButtonName.Equals("SERVICE"))
            {
                WriteByte(DInputBuffer_CaveAddress + 0x37, 0x80);
            }
            else if (e.ButtonName.Equals("TEST"))
            {
                WriteByte(DInputBuffer_CaveAddress + 0x2C, 0x80);
            }
        }

        protected override void GameController_OnButtonUp(object sender, VirtualGameControler.ButtonDownEventArgs e)
        {
            if (e.ButtonName.Equals("COIN"))
            {
                WriteByte(DInputBuffer_CaveAddress + 0x37, 0x00);
            }
            else if (e.ButtonName.Equals("SERVICE"))
            {
                WriteByte(DInputBuffer_CaveAddress + 0x37, 0x00);
            }
            else if (e.ButtonName.Equals("TEST"))
            {
                WriteByte(DInputBuffer_CaveAddress + 0x2C, 0x00);
            }
        }

        protected override void UpdateAxis(PlayerID Id, int X, int Y)
        {
            if (Id != PlayerID.None)
            {
                byte[] bufferX = BitConverter.GetBytes((UInt16)X);
                byte[] bufferY = BitConverter.GetBytes((UInt16)Y);

                WriteBytes(GameProcess_ModuleBaseAddress + _PlayersInfo_BaseOffset + _Player1_X_Offset + ((uint)Id * 0x20), bufferX);
                WriteBytes(GameProcess_ModuleBaseAddress + _PlayersInfo_BaseOffset + _Player1_Y_Offset + ((uint)Id * 0x20), bufferY);
            }
        }

        protected override void PlayerController_OnButtonDown(object sender, VirtualGameControler.ButtonDownEventArgs e)
        {
            if (e.Id != PlayerID.None)
            {
                UInt32 PlayerIndex = (UInt32)e.Id;

                if (e.ButtonName.Equals("TRIGGER"))
                {
                    WriteByte(GameProcess_ModuleBaseAddress + _PlayersInfo_BaseOffset + _Player1_ActivateAim_Offset + (PlayerIndex * 0x20), 0x01);
                    WriteByte(GameProcess_ModuleBaseAddress + _PlayersInfo_BaseOffset + _Player1_ForceNullAxis_Offset + (PlayerIndex * 0x20), 0x00);
                    WriteByte(DInputBuffer_CaveAddress + 0x36 + (PlayerIndex * 2), 0x80);
                }
                else if (e.ButtonName.Equals("START"))
                {
                    WriteByte(DInputBuffer_CaveAddress + 0x28 + (PlayerIndex * 2), 0x80);
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
                    WriteByte(DInputBuffer_CaveAddress + 0x36 + (PlayerIndex * 2), 0x00);
                }
                else if (e.ButtonName.Equals("START"))
                {
                    WriteByte(DInputBuffer_CaveAddress + 0x28 + (PlayerIndex * 2), 0x00);
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
                new GameOutput(OutputId.LmpLeft),
                new GameOutput(OutputId.LmpRight),
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
            //Genuine Outputs
            byte OutputData = ReadByte(GameProcess_ModuleBaseAddress + _PlayersInfo_BaseOffset + _Outputs_Offset);
            SetOutputValue(OutputId.P1_LmpStart, OutputData & 0x01);
            SetOutputValue(OutputId.P2_LmpStart, OutputData >> 1 & 0x01);
            SetOutputValue(OutputId.LmpLeft, OutputData >> 4 & 0x01);
            SetOutputValue(OutputId.LmpRight, OutputData >> 5 & 0x01);
            SetOutputValue(OutputId.P1_GunRecoil, OutputData >> 6 & 0x01);
            SetOutputValue(OutputId.P2_GunRecoil, OutputData >> 7 & 0x01);

            for (uint i = 0; i < _GameConfig.PlayerCount; i++)
            {
                _Life[i] = 0;
                _ShotCount[i] = 0;

                //Custom Outputs
                if (ReadByte(GameProcess_ModuleBaseAddress + _PlayersInfo_BaseOffset + _Player1_InGame_Offset + i) == 1)
                {
                    //Recoil byte don't move a lot (except sometimes during TEST menu)
                    //To compute custom Recoil, we can read the shot number increasing value with each bullet
                    _ShotCount[i] = ReadInt32(GameProcess_ModuleBaseAddress + _Player1_SHotCount_Offset + (i * 0x94));
                    if (_ShotCount[i] > _LastShotCount[i])
                        SetOutputValue(OutputId.P1_CtmRecoil + i, 1);

                    _Life[i] = ReadInt32(GameProcess_ModuleBaseAddress + _Player1_Life_Offset + (i * 0x94));
                    if (_Life[i] < _LastLife[i])
                        SetOutputValue(OutputId.P1_Damaged + i, 1);
                }

                _LastLife[i] = _Life[i];
                _LastShotCount[i] = _ShotCount[i];

                SetOutputValue(OutputId.P1_Life + i, _Life[i]);
            }

            SetOutputValue(OutputId.Credits, ReadInt32(GameProcess_ModuleBaseAddress + _Credits_Offset));
        }

        #endregion
    }
}
