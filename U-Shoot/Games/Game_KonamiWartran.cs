using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UsCore.Config;
using UsCore.Launcher;
using UsCore.Logging;
using UsCore.MameOutput;
using UsCore.Memory;
using UsCore.VirtualControllers;

namespace U_Shoot.Games
{
    public class Game_KonamiWartran : BaseGame32
    {
        protected override bool PATCH_PEB_DEBUG_FLAG { get { return false; } }

        #region LOADER MEMORY ADDRESSES   
        private UInt32 _LocalCfg_Patch_Offset = 0x012D288;
        private NopStruct32 _Nop_LocalCfg_DrivePatch= new NopStruct32(0x000B4508, 21);
        #endregion

        #region INPUTS MEMORY ADDRESSES    
        private UInt32 _OpenIo_Patch_Offset = 0x0007A3E0;
        private UInt32 _CloseIo_Patch_Offset = 0x0007A4B0;
        private UInt32 _WriteIo_Patch_Offset = 0x0007A4D0;
        private UInt32 _ReadIo_Patch_Offset = 0x0007A4500;
        private UInt32 _FlushIo_Patch_Offset = 0x0007A560;
        private UInt32 _EscapeCommIo_Patch_Offset = 0x0007A570;
        #endregion

        #region OUTPUTS MEMORY ADDRESSES

        #endregion

        public Game_KonamiWartran(GameConfig Config) : base(Config)
        {
            _KnownMd5Prints.Add("Wartran Trooper", "4c987ea93e8dbd5a6aa624e504a4706c");
        }

        protected override void AttachDebuggerEvents()
        {

        }

        #region Debugger Events

        #region EXCEPTION_DEBUG_EVENT

        protected override void ExceptionEvent(object sender, x86_x64_Debugger.ExceptionDebugEventArgs e)
        {
            //Using Generic Exception event Handling to set Input/Outputs Hacks
            base.ExceptionEvent(sender, e);
        }

        #endregion

        #region LOAD_DLL_DEBUG_EVENT

        protected override void LoadDllEvent(object sender, x86_x64_Debugger.LoadDllDebugEventArgs e)
        {

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
                    double dMaxX = 1919.0;
                    double dMaxY = 1079.0;
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
            //Remove setting a drive letter at the start of the Path name
            SetNops(GameProcess_ModuleBaseAddress, _Nop_LocalCfg_DrivePatch);

            //Original string has a D00 folder in the path, removing it to make it local from the exe
            ReplaceReadOnlyString(new UInt32[] { _LocalCfg_Patch_Offset }, Encoding.ASCII.GetBytes("GxD00\0"));

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        #endregion        

        #region Inputs Hack

        protected override void Apply_InputsMemoryHack()
        {
            Create_InputsDataBank();



            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        #endregion

        #region Outputs Hack

        protected override void Apply_OutputsMemoryHack()
        {
            Create_OutputsDataBank();

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
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

            }
            else if (e.ButtonName.Equals("TEST"))
            {

            }
        }

        protected override void GameController_OnButtonUp(object sender, VirtualGameControler.ButtonDownEventArgs e)
        {
            if (e.ButtonName.Equals("TEST"))
            {

            }
        }

        protected override void UpdateAxis(PlayerID Id, int X, int Y)
        {
            if (Id != PlayerID.None)
            {
                byte[] bufferX = BitConverter.GetBytes(X);
                byte[] bufferY = BitConverter.GetBytes(Y);
            }
        }

        protected override void PlayerController_OnButtonDown(object sender, VirtualGameControler.ButtonDownEventArgs e)
        {
            if (e.Id != PlayerID.None)
            {
                UInt32 PlayerIndex = (UInt32)e.Id;

                if (e.ButtonName.Equals("TRIGGER"))
                {

                }
                else if (e.ButtonName.Equals("RELOAD"))
                {

                }
                else if (e.ButtonName.Equals("START"))
                {

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

                }
                else if (e.ButtonName.Equals("RELOAD"))
                {

                }
                else if (e.ButtonName.Equals("START"))
                {

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
            _Outputs = new List<GameOutput>();
        }

        /// <summary>
        /// Update all Outputs values before sending them to MameHooker
        /// </summary>
        public override void UpdateOutputValues()
        {
        }

        #endregion
    }
}
