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
    public class Game_WndMadBullets : BaseGame32
    {
        protected override bool PATCH_PEB_DEBUG_FLAG { get { return true; } }

        #region LOADER MEMORY ADDRESSES                
        private InjectionStruct32 _NoCrosshair_InjectionStruct = new InjectionStruct32(0x00100D0C, 6);
        #endregion

        #region INPUTS MEMORY ADDRESSES                
        private InjectionStruct32 _WndProc_InjectionStruct = new InjectionStruct32(0x00100228, 7);
        #endregion

        #region OUTPUTS MEMORY ADDRESSES
        private UInt32 _PlayerPlayingPtr_Offset = 0x001D55E8;
        private UInt32 _LifePtr_Offset = 0x001D28C0;
        private UInt32 _AmmoPtr_Offset = 0x001D2890;
        private InjectionStruct32 _Recoil_InjectionStruct = new InjectionStruct32(0x0004FFA2, 7);
        private InjectionStruct32 _Damage_InjectionStruct = new InjectionStruct32(0x000B1E92, 6);

        //Custom Data
        private UInt32 _Recoil_CaveAddress;
        private UInt32 _Damage_CaveAddress;
        #endregion
       
        public Game_WndMadBullets(GameConfig Config) : base(Config)
        {
            _KnownMd5Prints.Add("Mad Bullets v1.0.2", "69807fe94a14ada9c792f20017d9a2a8");
            //Not sure about the version, mine is from IGG-Games
            
            if (_GameConfig.Windowed)
                _GameLauncher.Add_CmdLine_Arg("-windowed"); 

            int Width = _GameConfig.ResolutionWidth != 0 ? _GameConfig.ResolutionWidth : 1920;
            int Height = _GameConfig.ResolutionHeight != 0 ? _GameConfig.ResolutionHeight : 1080;
            _GameLauncher.Add_CmdLine_Arg("-width " + Width + " -height " + Height);
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
                    double dMaxX = TotalResX;
                    double dMaxY = TotalResY;
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
            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }
        #endregion

        #region Inputs Hack

        protected override void Apply_InputsMemoryHack()
        {
            Create_InputsDataBank();
                        
            SetHack_WndProc();
            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        /// <summary>
        /// Game is using WM_LBUTTONDOWN, etc.... to handle controls
        /// At the start of the WndProc, changing original messages ID (0x201, etc...) to WM_NULL to block all mouse inputs
        /// Then, Using user Windows Messages (https://learn.microsoft.com/en-us/windows/win32/winmsg/wm-user) to convert our own to 0x201, etc... to simulate mouse click
        /// 
        /// Custom messages will be :
        /// TRIGGER_DOWN    = 0x8201
        /// TRIGGER_UP      = 0x8202
        /// RELOAD_DOWN     = 0x8204
        /// RELOAD_UP       = 0x8205
        /// </summary>
        private void SetHack_WndProc()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //cmp [ebp+0C],00000200
            CaveMemory.Write_StrBytes("81 7D 0C 00 02 00 00");
            //jl Next
            CaveMemory.Write_StrBytes("7C 10");
            //cmp [ebp+0C],00000209
            CaveMemory.Write_StrBytes("81 7D 0C 09 02 00 00");
            //ja Next
            CaveMemory.Write_StrBytes("77 07");
            //mov [ebp+0C],00000000
            CaveMemory.Write_StrBytes("C7 45 0C 00 00 00 00");

            //Next:
            //cmp [ebp+0C],00008200
            CaveMemory.Write_StrBytes("81 7D 0C 00 82 00 00");
            //jl originalcode
            CaveMemory.Write_StrBytes("7C 15");
            //cmp [ebp+0C],00008209
            CaveMemory.Write_StrBytes("81 7D 0C 09 82 00 00");
            //ja originalcode
            CaveMemory.Write_StrBytes("77 0C");
            //mov ecx,[ebp+0C]
            CaveMemory.Write_StrBytes("8B 4D 0C");
            //sub ecx,00008000
            CaveMemory.Write_StrBytes("81 E9 00 80 00 00");
            //mov [ebp+0C],ecx
            CaveMemory.Write_StrBytes("89 4D 0C");

            //originalcode:
            //mov ecx,[ebp+0C]
            CaveMemory.Write_StrBytes("8B 4D 0C");
            //mov [esp+14],edi
            CaveMemory.Write_StrBytes("89 7C 24 14");

            //Inject
            CaveMemory.InjectToOffset(_WndProc_InjectionStruct, "WndProc Inputs");
        }

        #endregion

        #region Outputs Hack

        protected override void Apply_OutputsMemoryHack()
        {
            Create_OutputsDataBank();
            _Recoil_CaveAddress = OutputsDatabank_Address;
            _Damage_CaveAddress = OutputsDatabank_Address + 0x04;

            SetHack_Recoil();
            SetHack_Damage();

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        /// <summary>
        /// Using Ammo count to compute recoil does not work :
        /// - Game has unlimited ammo bonus, and the number does not change during it
        /// Using this codecave, we can intercept call in function when the bullet is fired
        /// </summary>
        private void SetHack_Recoil()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //cmp dword ptr[edi+0F8h], 0
            CaveMemory.Write_StrBytes("83 BF F8 00 00 00 00");
            //mov byte ptr [_Recoil_CaveAddress],01
            CaveMemory.Write_StrBytes("C6 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Recoil_CaveAddress));
            CaveMemory.Write_StrBytes("01");

            //Inject it
            CaveMemory.InjectToOffset(_Recoil_InjectionStruct, "Recoil");
        }

        /// <summary>
        /// Intercepting call in function where player health should be decreased (or not) according to the game mode
        /// </summary>
        private void SetHack_Damage()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //mov al, [ecx+189h]
            CaveMemory.Write_StrBytes("8A 81 89 01 00 00");
            //mov byte ptr [_Damage_CaveAddress],01
            CaveMemory.Write_StrBytes("C6 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Damage_CaveAddress));
            CaveMemory.Write_StrBytes("01");

            //Inject it
            CaveMemory.InjectToOffset(_Damage_InjectionStruct, "Damage");
        }

        #endregion

        #region No Crosshair Hack

        protected override void Apply_NoCrosshairMemoryHack()
        {
            if (CreateTransparentCursor())
                SetHack_NoCrosshair();

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        /// <summary>
        /// Replacing the cursor handle set in RegisterClassA by out own handle of transparent cursor
        /// </summary>
        private void SetHack_NoCrosshair()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //mov eax, _CustomCursorHandle
            CaveMemory.Write_StrBytes("B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes((UInt32)_CustomTransparentCursorHandle));
            //mov [ebp-108], eax
            CaveMemory.Write_StrBytes("89 85 F8 FE FF FF");

            //Inject it
            CaveMemory.InjectToOffset(_NoCrosshair_InjectionStruct, "No Crosshair");
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
        }

        protected override void UpdateAxis(PlayerID Id, int X, int Y)
        {
            if (Id == PlayerID.Player1)
            {
                IntPtr lParam = new IntPtr(X + Y << 16);
                //Win32.PostMessage(GetGameWindowHandle(), 0x8200, IntPtr.Zero, lParam);
            }
        }

        protected override void PlayerController_OnButtonDown(object sender, VirtualGameControler.ButtonDownEventArgs e)
        {
            if (e.Id == PlayerID.Player1)
            {
                VirtualPlayerController Vpc = (VirtualPlayerController)sender;
                IntPtr lParam = new IntPtr(Vpc.Computed_X + (Vpc.Computed_Y << 16));

                if (e.ButtonName.Equals("TRIGGER"))
                {
                    Win32.PostMessage(GetGameWindowHandle(), 0x8201, IntPtr.Zero, lParam);
                }
                else if (e.ButtonName.Equals("RELOAD"))
                {
                    Win32.PostMessage(GetGameWindowHandle(), 0x8204, IntPtr.Zero, IntPtr.Zero);
                }
            }
        }
        protected override void PlayerController_OnButtonUp(object sender, VirtualGameControler.ButtonDownEventArgs e)
        {
            if (e.Id == PlayerID.Player1)
            {
                if (e.ButtonName.Equals("TRIGGER"))
                {
                    Win32.PostMessage(GetGameWindowHandle(), 0x8202, IntPtr.Zero, IntPtr.Zero);
                }
                else if (e.ButtonName.Equals("RELOAD"))
                {
                    Win32.PostMessage(GetGameWindowHandle(), 0x8205, IntPtr.Zero, IntPtr.Zero);
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
                new GameOutput(OutputId.P1_Ammo),
                new AsyncGameOutput(OutputId.P1_CtmRecoil, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_ON_DELAY), ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_OFF_DELAY), 0),
                new GameOutput(OutputId.P1_Life),
                new AsyncGameOutput(OutputId.P1_Damaged, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_DAMAGED_DELAY), 100, 0),
            };
        }

        /// <summary>
        /// Update all Outputs values before sending them to MameHooker
        /// </summary>
        public override void UpdateOutputValues()
        {
            //Player status :
            //[0] = Not Playing
            //[1] = Playing
            UInt32 P1_Status = ReadByte(ReadPtr(GameProcess_ModuleBaseAddress + _PlayerPlayingPtr_Offset) + 0x1AC);
            _Ammo[0] = 0;
            _Life[0] = 0;

            if (P1_Status != 0)
            {
                _Ammo[0] = ReadByte(ReadPtr(GameProcess_ModuleBaseAddress + _AmmoPtr_Offset) + 0xD7);
                _Life[0] = ReadByte(ReadPtr(GameProcess_ModuleBaseAddress + _LifePtr_Offset) + 0x24);

                //Custom Recoil
                if (ReadByte(_Recoil_CaveAddress) == 1)
                {
                    SetOutputValue(OutputId.P1_CtmRecoil, 1);
                    WriteByte(_Recoil_CaveAddress, 0);
                }

                //[Damaged] custom Output                
                if (ReadByte(_Damage_CaveAddress) == 1)
                {
                    SetOutputValue(OutputId.P1_Damaged, 1);
                    WriteByte(_Damage_CaveAddress, 0);
                }
            }

            SetOutputValue(OutputId.P1_Ammo, _Ammo[0]);
            SetOutputValue(OutputId.P1_Life, _Life[0]);
        }

        #endregion
    }
}
