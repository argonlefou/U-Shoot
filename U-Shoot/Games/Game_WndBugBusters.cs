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
    public class Game_WndBugBusters : BaseGame32
    {
        protected override bool PATCH_PEB_DEBUG_FLAG { get { return true; } }

        #region LOADER MEMORY ADDRESSES                
        private UInt32 _NoCd_Patch_Offset = 0x00009F46;
        private NopStruct32 _Nop_InitCoinsCounter = new NopStruct32(0x0004091F, 10);
        private NopStruct32 _Nop_ShowCrosshairInGame = new NopStruct32(0x000263C1, 2);
        #endregion

        #region INPUTS MEMORY ADDRESSES    
        private UInt32 _TestMenu_P1Start_Offset = 0x0009E880;
        private UInt32 _TestMenu_BtnSelect_Offset = 0x00060C58;
        private UInt32 _TestMenu_BtnEnter_Offset = 0x00060C5C;
        private UInt32 _TestMenu_CoinsCounter_Offset = 0x000C3488;        
        private UInt32 _P1_X_Offset = 0x0005FBA8;
        private UInt32 _P1_Y_Offset = 0x0005FBAC;
        private UInt32 _P1_Trigger_Offset = 0x0005FBB0;
        private UInt32 _P1_Start_Offset = 0x00060C50; 
        private UInt32 _KeyboardKeyPress_Patch_OFfset = 0x0000A3C9;
        private NopStruct32 _Nop_LbuttonDown = new NopStruct32(0x0000A3FF, 2);
        private NopStruct32 _Nop_LbuttonUp = new NopStruct32(0x0000A425, 2);
        private NopStruct32 _Nop_RbuttonDown = new NopStruct32(0x0000A44B, 2);
        private NopStruct32 _Nop_RbuttonUp = new NopStruct32(0x0000A572, 2);
        private NopStruct32 _Nop_MouseMove = new NopStruct32(0x0000A598, 2);
        private NopStruct32 _Nop_GamePadLoop = new NopStruct32(0x00010DD9, 2);
        private NopStruct32 _Nop_HidLoopTrigger_1 = new NopStruct32(0x0001139C, 10);
        private NopStruct32 _Nop_HidLoopTrigger_2 = new NopStruct32(0x000113AE, 10);
        #endregion

        #region OUTPUTS MEMORY ADDRESSES
        private UInt32 _GameMode_Offset = 0x00060BF8;
        private UInt32 _P1_Life_Offset = 0x00055094;
        private UInt32 _P1_Ammo_Offset = 0x0005509C;
        private UInt32 _GameCost_Offset = 0x00055090;
        private UInt32 _TotalCoins_Offset = 0x00060C1C;
        private UInt32 _P1_Lamp_Offset = 0x0009553C;
        private UInt32 _P2_Lamp_Offset = 0x00095540;
        private UInt32 _P1_Air_Offset = 0x00095418;
        private UInt32 _P2_Air_Offset = 0x0009541C;
        private UInt32 _P1_Playing_Offset = 0x00060C20;
        #endregion

        public Game_WndBugBusters(GameConfig Config) : base(Config)
        {
            _KnownMd5Prints.Add("Bug Buster v1.0.0.1 - Original exe", "832f2fa9ef018d390b7b477f4240ca8e");
            _KnownMd5Prints.Add("Bug Buster v1.0.0.1 - NO-CD patched", "7d4195bdfbfa843cac8af85616abbc21");
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

        #region Loader Hack
        protected override void Apply_LoaderMemoryHack()
        {
            //Jmp over the disc check
            WriteByte(GameProcess_ModuleBaseAddress + _NoCd_Patch_Offset, 0xEB);

            //By default, this PC-version force the coins counter to 9 when displaying the start screen. Removing it....
            SetNops(GameProcess_ModuleBaseAddress, _Nop_InitCoinsCounter);

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }
        #endregion        

        #region Inputs Hack

        protected override void Apply_InputsMemoryHack()
        {
            //Removing call to analyse WndProc WM_KEYDOWN. This removes ENTER, 1, 2 and F12 Keys
            WriteBytes(GameProcess_ModuleBaseAddress + _KeyboardKeyPress_Patch_OFfset, new byte[] { 0xEB, 0x28, 0x90 });

            //Removing Mouse events
            SetNops(GameProcess_ModuleBaseAddress, _Nop_LbuttonDown);
            SetNops(GameProcess_ModuleBaseAddress, _Nop_LbuttonUp);
            SetNops(GameProcess_ModuleBaseAddress, _Nop_RbuttonDown);
            SetNops(GameProcess_ModuleBaseAddress, _Nop_RbuttonUp);
            SetNops(GameProcess_ModuleBaseAddress, _Nop_MouseMove);

            //If a gamepad is detected, looks like it's looping and overriding values
            //This removes the whole loop, maybe just noping +10F5E / +10F70 is enough and less risky ?
            SetNops(GameProcess_ModuleBaseAddress, _Nop_GamePadLoop);

            //Some other kind of devices reset buttons states too
            SetNops(GameProcess_ModuleBaseAddress, _Nop_HidLoopTrigger_1);
            SetNops(GameProcess_ModuleBaseAddress, _Nop_HidLoopTrigger_2);           

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        #endregion

        #region Outputs Hack

        protected override void Apply_OutputsMemoryHack()
        { }

        #endregion

        #region No Crosshair Hack

        protected override void Apply_NoCrosshairMemoryHack()
        {
            SetNops(GameProcess_ModuleBaseAddress, _Nop_ShowCrosshairInGame);

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
                if (GetGameMode() != 12)
                {
                    //Add a coins in the game
                    int Coins = ReadInt32(GameProcess_ModuleBaseAddress + _TotalCoins_Offset);
                    Coins++;
                    WriteBytes(GameProcess_ModuleBaseAddress + _TotalCoins_Offset, BitConverter.GetBytes(Coins));
                }
                else
                {
                    //Write the coins entry flag, to increment coins counter
                    WriteByte(GameProcess_ModuleBaseAddress + _TestMenu_CoinsCounter_Offset, 0x01);
                }
            }
            else if (e.ButtonName.Equals("TEST"))
            {
                if (GetGameMode() != 12)
                    WriteByte(GameProcess_ModuleBaseAddress + _GameMode_Offset, 12);
                else
                    WriteByte(GameProcess_ModuleBaseAddress + _TestMenu_BtnSelect_Offset, 0x01);
            }
            else if (e.ButtonName.Equals("SERVICE"))
            {
                if (GetGameMode() == 12)
                    WriteByte(GameProcess_ModuleBaseAddress + _TestMenu_BtnEnter_Offset, 0x01);
            }
        }

        protected override void GameController_OnButtonUp(object sender, VirtualGameControler.ButtonDownEventArgs e)
        {
            if (e.ButtonName.Equals("TEST"))
            {
                if (GetGameMode() == 12)
                    WriteByte(GameProcess_ModuleBaseAddress + _TestMenu_BtnSelect_Offset, 0x00);
            }
            else if (e.ButtonName.Equals("SERVICE"))
            {
                if (GetGameMode() == 12)
                    WriteByte(GameProcess_ModuleBaseAddress + _TestMenu_BtnEnter_Offset, 0x00);
            }
        }

        protected override void UpdateAxis(PlayerID Id, int X, int Y)
        {
            if (Id != PlayerID.None)
            {
                byte[] bufferX = BitConverter.GetBytes(X);
                byte[] bufferY = BitConverter.GetBytes(Y);

                WriteBytes(GameProcess_ModuleBaseAddress + _P1_X_Offset + ((uint)Id * 0x1C), bufferX);
                WriteBytes(GameProcess_ModuleBaseAddress + _P1_Y_Offset + ((uint)Id * 0x1C), bufferY);
            }
        }

        protected override void PlayerController_OnButtonDown(object sender, VirtualGameControler.ButtonDownEventArgs e)
        {
            if (e.Id != PlayerID.None)
            {
                UInt32 PlayerIndex = (UInt32)e.Id;

                if (e.ButtonName.Equals("TRIGGER"))
                {
                    WriteByte(GameProcess_ModuleBaseAddress + _P1_Trigger_Offset + (PlayerIndex * 0x1C), 0x01);
                }
                else if (e.ButtonName.Equals("RELOAD"))
                {
                    //To reload, set back Gaz tank capacity to 1.0f (full)
                    WriteBytes(GameProcess_ModuleBaseAddress + _P1_Ammo_Offset + (PlayerIndex * 0x04), new byte[] { 0x00, 0x00, 0x80, 0x3F }); //1.0f
                }
                else if (e.ButtonName.Equals("START"))
                {
                    WriteByte(GameProcess_ModuleBaseAddress + _P1_Start_Offset + (PlayerIndex * 0x04), 0x01);
                    if (GetGameMode() == 12)
                    {
                        WriteByte(GameProcess_ModuleBaseAddress + _TestMenu_P1Start_Offset + (PlayerIndex * 0x204), 0x01);
                    }
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
                    WriteByte(GameProcess_ModuleBaseAddress + _P1_Trigger_Offset + (PlayerIndex * 0x1C), 0x00);
                }
                else if (e.ButtonName.Equals("START"))
                {
                    //Game automatically reset button when set
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
                new GameOutput(OutputId.P1_AirFront),
                new GameOutput(OutputId.P2_AirFront),
                new GameOutput(OutputId.P1_Ammo),
                new GameOutput(OutputId.P2_Ammo),
                new GameOutput(OutputId.P1_GunMotor),
                new GameOutput(OutputId.P2_GunMotor),
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
            SetOutputValue(OutputId.P1_LmpStart, ReadByte(GameProcess_ModuleBaseAddress + _P1_Lamp_Offset));
            SetOutputValue(OutputId.P2_LmpStart, ReadByte(GameProcess_ModuleBaseAddress + _P2_Lamp_Offset));
            SetOutputValue(OutputId.P1_AirFront, ReadByte(GameProcess_ModuleBaseAddress + _P1_Air_Offset));
            SetOutputValue(OutputId.P2_AirFront, ReadByte(GameProcess_ModuleBaseAddress + _P2_Air_Offset));

            for (uint i = 0; i < _GameConfig.PlayerCount; i++)
            {
                _Ammo[i] = 0;
                _Life[i] = 0;

                if (GetGameMode() == 5)
                {
                    if (ReadByte(GameProcess_ModuleBaseAddress + _P1_Playing_Offset + (4 * i)) == 1)
                    {
                        _Ammo[i] = (int)(ReadFloat(GameProcess_ModuleBaseAddress + _P1_Ammo_Offset + (4 * i)) * 100);
                        _Life[0] = (int)(ReadFloat(GameProcess_ModuleBaseAddress + _P1_Life_Offset + (4 * i)) * 100);
                        if (_Life[i] < 0)
                            _Life[i] = 0;

                        //[Damaged] custom Output                
                        if (_Life[0] < _LastLife[i])
                            SetOutputValue(OutputId.P1_Damaged + i, 1);
                    }
                }

                _LastLife[i] = _Life[i];
                _LastAmmo[i] = _Ammo[i];

                SetOutputValue(OutputId.P1_Ammo + i, _Ammo[i]);
                SetOutputValue(OutputId.P1_Life + i, _Life[i]);

                //Rumble created based on Air activation
                //Fan value can be 0 or 2 (when fire spary is activated), so for rumble we will convert it to 0/1
                if (ReadByte(GameProcess_ModuleBaseAddress + _P1_Air_Offset + (4 * i)) != 0)
                    SetOutputValue(OutputId.P1_GunMotor + i, 1);
                else
                    SetOutputValue(OutputId.P1_GunMotor + i, 0);
            }
                       
            //Credits
            int Cost = ReadInt32(GameProcess_ModuleBaseAddress + _GameCost_Offset);
            if (Cost == 0)
            {
                SetOutputValue(OutputId.Credits, 0); // FREEPLAY
            }
            else
            {
                int Coins = ReadInt32(GameProcess_ModuleBaseAddress + _TotalCoins_Offset);
                SetOutputValue(OutputId.Credits, Coins / Cost);
            }
        }

        #endregion

        /// <summary>
        /// Return Game mode
        /// 0 = ?
        /// 1 = Title Screen
        /// 2 = Attract Mode
        /// 3 = Level select
        /// 4 = Result
        /// 5 = In Game
        /// 6 = 
        /// 7 = Ranking
        /// 8 = How To Play
        /// 9 = 
        /// 10 = Credits
        /// 11 = Mode Select (Story / Free)
        /// 12 = Settings Menu
        /// 13 = Initial value on boot, then goes to 1 ?
        /// </summary>
        /// <returns></returns>
        private int GetGameMode()
        {
            return ReadByte(GameProcess_ModuleBaseAddress + _GameMode_Offset);
        }
    }
}
