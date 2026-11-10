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
    public class Game_WndArtIsDead : BaseGame32
    {
        protected override bool PATCH_PEB_DEBUG_FLAG { get { return true; } }

        #region LOADER MEMORY ADDRESSES                
        private UInt32 _NoCrosshair_Patch_Offset = 0x0002094E;
        #endregion

        #region INPUTS MEMORY ADDRESSES    
        private UInt32 _MenuButtons_Offset = 0x000DB02C;
        private UInt32 _MenuX_Offset = 0x000DF4F8;
        private UInt32 _MenuY_Offset = 0x000DF4FC;
        private UInt32 _ForceSoloGun_Patch_Offset = 0x0002123B;
        private UInt32 _ForceDualGun1_Patch_Offset = 0x00020F2F;
        private UInt32 _ForceDualGun2_Patch_Offset = 0x00021063;

        private UInt32 _P1Gun_X_Offset = 0x000E0EE0;
        private UInt32 _P1Gun_Y_Offset = 0x000E0EE4;
        private UInt32 _P1Gun_Trigger_Offset = 0x000E0EE8;
        //private UInt32 _P2Gun_X_Offset = 0x000E0F70;
        //private UInt32 _P2Gun_Y_Offset = 0x000E0F74;
        //private UInt32 _P2Gun_Trigger_Offset = 0x000E0F78;
        private NopStruct32 _Nop_LButtonDown = new NopStruct32(0x0000A13F, 5);
        private NopStruct32 _Nop_LButtonUp = new NopStruct32(0x0000A169, 5);
        private NopStruct32 _Nop_MenuMouseX = new NopStruct32(0x0000A18E, 6);
        private NopStruct32 _Nop_MenuMouseY = new NopStruct32(0x0000A197, 5);
        #endregion

        #region OUTPUTS MEMORY ADDRESSES
        private InjectionStruct32 _P1Recoil_InjectionStruct = new InjectionStruct32(0x00020FAE, 7);
        private InjectionStruct32 _P2Recoil_InjectionStruct = new InjectionStruct32(0x000210E9, 7);
        private InjectionStruct32 _P1SoloRecoil_InjectionStruct = new InjectionStruct32(0x000212AF, 7);

        //Custom Data
        private UInt32 _P1Recoil_CaveAddress;
        private UInt32 _P2Recoil_CaveAddress;
        #endregion

        public Game_WndArtIsDead(GameConfig Config) : base(Config)
        {
            _KnownMd5Prints.Add("Art Is Dead", "4cc4e814647a1d9f315fa1be4ac3d92a");
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
                    double dMaxX = 640.0;
                    double dMaxY = 480.0;
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
            //Game is reading mouse inputs to navigate in menu, replacing it so that both guns can control cursor in Menus
            //Note that this is also used for player1 if the device is set to MOUSE in options
            SetNops(GameProcess_ModuleBaseAddress, _Nop_LButtonDown);
            SetNops(GameProcess_ModuleBaseAddress, _Nop_LButtonUp);
            SetNops(GameProcess_ModuleBaseAddress, _Nop_MenuMouseX);
            SetNops(GameProcess_ModuleBaseAddress, _Nop_MenuMouseY);

            //Game normally read @4DF4F4 to get mouse/gun for solo player mode
            //Forcing it to 1 (= GUN) so that MenuMouse data will not interfere (so that only P1 device can shoot)
            WriteBytes(GameProcess_ModuleBaseAddress + _ForceSoloGun_Patch_Offset, new byte[] { 0xB8, 0x01, 0x00, 0x00, 0x00, 0x90, 0x90 });

            //Same things for 2P where Game is checking if each player is gun or mouse
            //More exactly, based on 2 bytes vlaues it's checking the guns slots for P1 or P2
            //Forcing it so that Gun sLot 1 = ¨Player 1 and Gun Slot 2 = Player 2 so that we can fill-in data at correct places
            WriteBytes(GameProcess_ModuleBaseAddress + _ForceDualGun1_Patch_Offset, new byte[] { 0xB8, 0x01, 0x00, 0x00, 0x00, 0x90 });
            WriteBytes(GameProcess_ModuleBaseAddress + _ForceDualGun2_Patch_Offset, new byte[] { 0xB8, 0x02, 0x00, 0x00, 0x00, 0x90 });

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        #endregion

        #region Outputs Hack

        protected override void Apply_OutputsMemoryHack()
        {
            Create_OutputsDataBank();
            _P1Recoil_CaveAddress = OutputsDatabank_Address;
            _P2Recoil_CaveAddress = OutputsDatabank_Address + 0x01;

            SetHack_Recoil(_P1Recoil_InjectionStruct, (UInt32)PlayerID.Player1);
            SetHack_Recoil(_P2Recoil_InjectionStruct, (UInt32)PlayerID.Player2);
            SetHack_Recoil(_P1SoloRecoil_InjectionStruct, (UInt32)PlayerID.Player1);

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        /// <summary>
        /// Intercepting just before call is made to draw P1 bullet impact sprite during a level
        /// 3 Injection are needed for each state of play (P1 solo, P1 / P2 dual)
        /// </summary>
        private void SetHack_Recoil(InjectionStruct32 TargetInjectionStruct, UInt32 PlayerId)
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //fstp dword ptr [esp]
            CaveMemory.Write_StrBytes("D9 1C 24");
            //fild qword ptr [esp+20]
            CaveMemory.Write_StrBytes("DF 6C 24 20");
            //move byte ptr[_PXRecoil_CaveAddress], 1
            CaveMemory.Write_StrBytes("C6 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1Recoil_CaveAddress + PlayerId));
            CaveMemory.Write_StrBytes("01");

            //Inject it
            CaveMemory.InjectToOffset(TargetInjectionStruct, "P" + PlayerId.ToString() + " Recoil");
        }                

        #endregion

        #region No Crosshair Hack

        protected override void Apply_NoCrosshairMemoryHack()
        {
            //Game does not display crosshairs if GUNS are selected as controller...forcing it to believe to
            WriteByte(GameProcess_ModuleBaseAddress + _NoCrosshair_Patch_Offset, 0xEB);

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        #endregion

        #endregion

        #region Inputs

        protected override void GameController_OnButtonDown(object sender, VirtualGameControler.ButtonDownEventArgs e)
        {  }

        protected override void GameController_OnButtonUp(object sender, VirtualGameControler.ButtonDownEventArgs e)
        {  }

        protected override void UpdateAxis(PlayerID Id, int X, int Y)
        {
            if (Id != PlayerID.None)
            {
                byte[] bufferX = BitConverter.GetBytes((Int16)X);
                byte[] bufferY = BitConverter.GetBytes((Int16)Y);

                WriteBytes(GameProcess_ModuleBaseAddress + _MenuX_Offset, bufferX);
                WriteBytes(GameProcess_ModuleBaseAddress + _MenuY_Offset, bufferY);

                WriteBytes(GameProcess_ModuleBaseAddress + _P1Gun_X_Offset + ((uint)Id * 0x90), bufferX);
                WriteBytes(GameProcess_ModuleBaseAddress + _P1Gun_Y_Offset + ((uint)Id * 0x90), bufferY);
            }
        }

        protected override void PlayerController_OnButtonDown(object sender, VirtualGameControler.ButtonDownEventArgs e)
        {
            if (e.Id != PlayerID.None)
            {
                UInt32 PlayerIndex = (UInt32)e.Id;

                if (e.ButtonName.Equals("TRIGGER"))
                {
                    WriteByte(GameProcess_ModuleBaseAddress + _P1Gun_Trigger_Offset + (PlayerIndex * 0x90), 0x01);
                    Apply_OR_ByteMask(GameProcess_ModuleBaseAddress + _MenuButtons_Offset, 0x01);
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
                    WriteByte(GameProcess_ModuleBaseAddress + _P1Gun_Trigger_Offset + (PlayerIndex * 0x90), 0x00);
                    Apply_AND_ByteMask(GameProcess_ModuleBaseAddress + _MenuButtons_Offset, 0xFE);
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
            _Outputs = new List<GameOutput>()
            {
                new AsyncGameOutput(OutputId.P1_CtmRecoil, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_ON_DELAY), ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_OFF_DELAY), 0),
                new AsyncGameOutput(OutputId.P2_CtmRecoil, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_ON_DELAY), ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_OFF_DELAY), 0)
            };
        }

        /// <summary>
        /// Update all Outputs values before sending them to MameHooker
        /// </summary>
        public override void UpdateOutputValues()
        {
            for (uint i = 0; i < _GameConfig.PlayerCount; i++)
            {
                if (ReadByte(_P1Recoil_CaveAddress + i) == 1)
                {
                    SetOutputValue(OutputId.P1_CtmRecoil + i, 1);
                    WriteByte(_P1Recoil_CaveAddress + i, 0);
                }
            }
        }

        #endregion
    }
}
