using System;
using System.Collections.Generic;
using System.IO;
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
    public class Game_TtxGundam : BaseGame32
    {
        protected override bool PATCH_PEB_DEBUG_FLAG { get { return true; } }

        private const string GAME_MODE_OPTION = "Game Mode";
        private const string P1_PEDALMODE_OPTION = "P1 Pedal mode";
        private const string P2_PEDALMODE_OPTION = "P2 Pedal mode";

        private const string LIBIGDISPLAYDLL_NAME = "libIGDisplay.dll";
        private UInt32 _LibIGDisplay_BaseAddress;

        #region LOADER MEMORY ADDRESSES  
        private UInt32 _Fullscreen_PatchOffset = 0x00175854; 
        private UInt32 _SingleDualScreen_Patch_Offset = 0x00175868;
        private UInt32 _FrameLimiter_Patch_Offset = 0x00175877;
        private UInt32 _LocalSave_PatchOffset = 0x001758D1;
        private UInt32 _IoEmulation_PatchOffset = 0x001758EA;
        private InjectionStruct32 _NoCrosshair_InjectionStruct = new InjectionStruct32(0x000A62B6, 6);
        private InjectionStruct32 _NoCursor_InjectionStruct = new InjectionStruct32(0x00007E01, 5);
        #endregion

        #region INPUTS MEMORY ADDRESSES    
        private UInt32 _P1_X_Offset = 0x0026F990;
        private UInt32 _P1_Y_Offset = 0x0026F992;
        private UInt32 _JVS_Buttons_Offset = 0x0026FA2A;
        private UInt32 _JVS_Trigger_Offset = 0x0026FB0A;
        private UInt32 _ForceUseJvsData_Offset = 0x000D12C8;
        private UInt32 _SetKeyDownFunction_Offset = 0x000D0C60;
        private UInt32 _SetKeyUpFunction_Offset = 0x000D0E30;
        private UInt32 _SetMouseButtonDownFunction_Offset = 0x000D02F0;
        private UInt32 _SetMouseButtonUpFunction_Offset = 0x000D0320;
        private UInt32 _SetMouseMotionFunction_Offset = 0x000D0350;
        private UInt32 _JVS_AxisUpdate_Offset = 0x000D06E7;
        private InjectionStruct32 _Border_Check1_InjectionStruct = new InjectionStruct32(0x000A7A50, 46);
        private InjectionStruct32 _Border_Check2_InjectionStruct = new InjectionStruct32(0x000A7AEE, 30);
        private NopStruct32 _Nop_JvsCrashingFunction = new NopStruct32(0x000D135B, 5);

        //PedalMode Codecave
        private Codecave32 _Cave_Check1, _Cave_Check2;
        private bool[] _IsPedalPushed = new bool[] { false, false};
        #endregion

        #region OUTPUTS MEMORY ADDRESSES
        private UInt32 _P1_GunMotorOffset = 0x0026FB1C;
        private UInt32 _P2_GunMotorOffset = 0x0026FB20;
        //dword_66FB24 also set to 0 and 1 at the same time than motors sometimes ??
        private UInt32 _P1_Playing_Offset = 0x0026FB9C;
        private UInt32 _P1_Lamp_Offset = 0x0026FA49;
        private UInt32 _P2_Lamp_Offset = 0x0026FA4B;
        private UInt32 _Credits_Offset = 0x0026FB88;
        private UInt32 _BookKeeping_Credits_Offset = 0x0026FBC8;
        //Sets value to 1 when active, 0 un-initialized, 2 otherwise
        private UInt32 _SetRumbleGunShot_Offset = 0x000A60FF;
        private UInt32 _SetRumbleCanonShot_Offset = 0x000A7649;
        private UInt32 _SetRumbleOnHit_Offset = 0x009B06B;
        private UInt32 _SetRumbleUnkownReason01_Offset = 0x000A70F3;
        private UInt32 _SetRumbleUnkownReason02_Offset = 0x000E19CA;
        private UInt32 _SetRumbleUnkownReason03_Offset = 0x000E1A31;
        private UInt32 _SetRumbleStateFuntionOffset = 0x000D0B40;
        private InjectionStruct32 _Ammo_InjectionStruct = new InjectionStruct32(0x000A7BA3, 6);
        private InjectionStruct32 _Life_InjectionStruct = new InjectionStruct32(0x00099964, 6);

        //Custom Data
        private UInt32 _P1_RecoilEnabled_CustomAddress;
        private UInt32 _P2_RecoilEnabled_CustomAddress;
        private UInt32 _P1_DamagedEnabled_CustomAddress;
        private UInt32 _P2_DamagedEnabled_CustomAddress;
        private UInt32 _P1_Ammo_CustomAddress;
        private UInt32 _P2_Ammo_CustomAddress;
        private UInt32 _P1_Life_CustomAddress;
        private UInt32 _P2_Life_CustomAddress;
        #endregion

        public Game_TtxGundam(GameConfig Config) : base(Config)
        {
            _KnownMd5Prints.Add("Gundam : SoZ v1.01 - Dual Player, Unpatched I/O", "70af03a21a42d9042065fc65b7eb56f9");
            _KnownMd5Prints.Add("Gundam : SoZ v1.01 - Single Player, Pathched I/O", "d8cd539967cc3c23f620139ab4669d30");
            _KnownMd5Prints.Add("Gundam : SoZ v1.01 - Dual Player, Patched I/O", "c31187a57fd5ab6864b78eaa755ae3f0");
            if(_GameConfig.HideConsole)
            {
                _GameLauncher.RedirectOutputsToNull = true;
                _GameLauncher.CreateNoWindow = true;
            }
        }

        protected override void AttachDebuggerEvents()
        {
            _GameLauncher.OnLoadedDll += LoadDllEvent;
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
            x86_x64_Debugger Dbg = (x86_x64_Debugger)sender;
            IntPtr lpBaseOfDllLoad = e.LoadDllInfo.lpBaseOfDll;

            if (lpBaseOfDllLoad != IntPtr.Zero)
            {
                if (string.Equals(Path.GetFileName(e.DllPath), LIBIGDISPLAYDLL_NAME, StringComparison.OrdinalIgnoreCase))
                {
                    Logger.WriteLog("==> Wanted DLL found !");
                    //HANDLE should always be in 32Bits range (https://learn.microsoft.com/fr-fr/windows/win32/winprog64/interprocess-communication?redirectedfrom=MSDN)
                    Logger.WriteLog("  + hModule: 0x" + e.LoadDllInfo.hFile.ToString("X8"));
                    Logger.WriteLog("  + fUnicode: " + e.LoadDllInfo.fUnicode.ToString());

                    _LibIGDisplay_BaseAddress = (UInt32)e.LoadDllInfo.lpBaseOfDll;
                }
            }
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

                    if (_GameConfig.GetStringOption(GAME_MODE_OPTION).ToLower().Equals("2 players"))
                    {
                        //Dual Screen mode (fullscreen or windowed) X axis is changed because of the double screen:
                        dMaxX = 1280.0;
                        dMaxY = 480.0;

                        //Side by Side dual screen, each one 640x480 max coordonates
                        //So total X is 1280
                        PlayerController.Computed_X = Convert.ToInt16(Math.Round(dMaxX * PlayerController.Computed_X / TotalResX));

                        //Player One will have X value cut-off to [0-639] next
                        //For player 2 we first shift value to the left
                        if (PlayerController.PlayerId == PlayerID.Player2)
                            PlayerController.Computed_X -= 640;

                        //Y axis will only change in fullscreen mode because of the black bars
                        if (_GameConfig.Fullscreen)
                        {
                            double Ratio = TotalResX / dMaxX;
                            double GameHeight = Ratio * dMaxY;
                            double Border = (TotalResY - GameHeight) / 2.0;
                            double y = PlayerController.Computed_Y - Border;
                            double percent = y * 100.0 / GameHeight;
                            PlayerController.Computed_Y = (int)(percent * dMaxY / 100.0);
                        }
                        else
                        {
                            PlayerController.Computed_Y = Convert.ToInt32(Math.Round(dRangeY * PlayerController.Computed_Y / TotalResY));
                        }
                    }
                    else if (_GameConfig.GetStringOption(GAME_MODE_OPTION).ToLower().Equals("1 player"))
                    {
                        dMaxX = 640.0;
                        PlayerController.Computed_X = Convert.ToInt32(Math.Round(dMaxX * PlayerController.Computed_X / TotalResX));
                        PlayerController.Computed_Y = Convert.ToInt32(Math.Round(dMaxY * PlayerController.Computed_Y / TotalResY));
                    }

                    if (PlayerController.Computed_X < 1)
                        PlayerController.Computed_X = 1;
                    if (PlayerController.Computed_X > 639)
                        PlayerController.Computed_X = 639;
                    if (PlayerController.Computed_Y < 1)
                        PlayerController.Computed_Y = 1;
                    if (PlayerController.Computed_Y > (int)dMaxY - 1)
                        PlayerController.Computed_Y = (int)dMaxY - 1;

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
            //Fullscreen / Windowed
            if (_GameConfig.Windowed)
            {
                ReplaceReadOnlyString(GameProcess_ModuleBaseAddress, new UInt32[] { _Fullscreen_PatchOffset }, Encoding.ASCII.GetBytes("FALSE"));
            }
            else if (_GameConfig.Fullscreen)
            {
                ReplaceReadOnlyString(GameProcess_ModuleBaseAddress, new UInt32[] { _Fullscreen_PatchOffset }, Encoding.ASCII.GetBytes(" TRUE"));
            }

            //Setting the screen number : 
            //For solo player, Only one 640x480 window will be displayed
            //For dual screen, one 1280x640 window will be displayed to host both player screens
            byte ScreenNumber = 1;
            if (_GameConfig.GetStringOption(GAME_MODE_OPTION).ToLower().Equals("2 players"))
                ScreenNumber = 0x32;
            else if (_GameConfig.GetStringOption(GAME_MODE_OPTION).ToLower().Equals("1 player"))
                ScreenNumber = 0x31;
            ReplaceReadOnlyString(GameProcess_ModuleBaseAddress, new UInt32[] { _SingleDualScreen_Patch_Offset }, new byte[] { ScreenNumber });

            //Changing original save dir (D:/save) to local folder (./save)
            ReplaceReadOnlyString(GameProcess_ModuleBaseAddress, new UInt32[] { _LocalSave_PatchOffset }, Encoding.ASCII.GetBytes("=./sv"));

            //Changing frame_limit from TRUE to FALSE
            ReplaceReadOnlyString(GameProcess_ModuleBaseAddress, new UInt32[] {_FrameLimiter_Patch_Offset }, Encoding.ASCII.GetBytes("FALSE"));

            //Changing ioemulation from FALSE to TRUE
            ReplaceReadOnlyString(GameProcess_ModuleBaseAddress, new UInt32[] { _IoEmulation_PatchOffset }, Encoding.ASCII.GetBytes(" TRUE"));

            //Removing the cursor by setting the Handle to our transparent one when calling RegisterClass() 
            if (_GameConfig.NoCursor)
            {         
                if (CreateTransparentCursor())
                    SetHack_NoCursor();
            }

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        private void SetHack_NoCursor()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, _LibIGDisplay_BaseAddress);

            //push edx
            CaveMemory.Write_StrBytes("52");
            //mov eax, _CustomTransparentCursorHandle
            CaveMemory.Write_StrBytes("B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes((UInt32)_CustomTransparentCursorHandle));
            //mov [esp+24],eax
            CaveMemory.Write_StrBytes("89 44 24 24");            

            //Inject it
            CaveMemory.InjectToOffset(_LibIGDisplay_BaseAddress, _NoCursor_InjectionStruct, "No Cursor");
        }

        #endregion        

        #region Inputs Hack

        protected override void Apply_InputsMemoryHack()
        {
            //Force to use JVS data for buttons and axis
            SetNops(GameProcess_ModuleBaseAddress, _Nop_JvsCrashingFunction);
            WriteByte(GameProcess_ModuleBaseAddress + _ForceUseJvsData_Offset, 0x85);

            //Disable all MouseEvents and Keyevents funtions (direct return without doing anything)
            WriteByte(GameProcess_ModuleBaseAddress + _SetKeyDownFunction_Offset, 0xC3);
            WriteByte(GameProcess_ModuleBaseAddress + _SetKeyUpFunction_Offset, 0xC3);
            WriteByte(GameProcess_ModuleBaseAddress + _SetMouseButtonDownFunction_Offset, 0xC3);
            WriteByte(GameProcess_ModuleBaseAddress + _SetMouseButtonUpFunction_Offset, 0xC3);
            WriteByte(GameProcess_ModuleBaseAddress + _SetMouseMotionFunction_Offset, 0xC3);

            //Disabling Axis update originally done by reading data
            WriteBytes(GameProcess_ModuleBaseAddress + _JVS_AxisUpdate_Offset, new byte[] { 0xE9, 0x39, 0x01, 0x00, 0x00, 0x90, 0x90, 0x90, 0x90 });

            SetHack_PedalMode();

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        private void SetHack_PedalMode()
        {
            /***
             * If neither Pedal1 nor Pedal2 are enabled, no need for a codecave (default game).
             * Else, We need to make 2 codecave for the 2 checking procedure (X-Y min-max)
             * Each one will have to be split for P1 and P2 separatelly
             ***/
            if (_GameConfig.GetBoolOption(P1_PEDALMODE_OPTION) || _GameConfig.GetBoolOption(P2_PEDALMODE_OPTION))
            {
                _Cave_Check1 = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

                //cmp [esi+1C], 0
                _Cave_Check1.Write_StrBytes("83 7E 1C 00");
                //je @player1
                _Cave_Check1.Write_StrBytes("0F 84 0A 00 00 00");
                //cmp [esi+1C], 1
                _Cave_Check1.Write_StrBytes("83 7E 1C 01");
                //je @player2
                _Cave_Check1.Write_StrBytes("0F 84 33 00 00 00");

                //player1:
                if (_GameConfig.GetBoolOption(P1_PEDALMODE_OPTION))
                    //cmp di, 00
                    _Cave_Check1.Write_StrBytes("66 83 FF 00");
                else
                    //cmp di, 10
                    _Cave_Check1.Write_StrBytes("66 83 FF 10");
                //jng game.exe+A7B85
                _Cave_Check1.Write_jng(GameProcess_ModuleBaseAddress + 0xA7B85);
                if (_GameConfig.GetBoolOption(P1_PEDALMODE_OPTION))
                    //cmp di, 00
                    _Cave_Check1.Write_StrBytes("66 81 FF 00 00");
                else
                    //cmp di, 0270
                    _Cave_Check1.Write_StrBytes("66 81 FF 70 02");
                //jnl game.exe+A7B85
                _Cave_Check1.Write_jnl(GameProcess_ModuleBaseAddress + 0xA7B85);
                //mov eax, edi
                _Cave_Check1.Write_StrBytes("8B C7");
                //shr eax, 10
                _Cave_Check1.Write_StrBytes("C1 E8 10");
                if (_GameConfig.GetBoolOption(P1_PEDALMODE_OPTION))
                    //cmp ax, 00
                    _Cave_Check1.Write_StrBytes("66 3D 00 00");
                else
                    //cmp ax, 10
                    _Cave_Check1.Write_StrBytes("66 3D 10 00");
                //jng game.exe+A7B85
                _Cave_Check1.Write_jng(GameProcess_ModuleBaseAddress + 0xA7B85);
                if (_GameConfig.GetBoolOption(P1_PEDALMODE_OPTION))
                    //cmp ax, 00
                    _Cave_Check1.Write_StrBytes("66 3D E0 01");
                else
                    //cmp ax, 01D0
                    _Cave_Check1.Write_StrBytes("66 3D D0 01");
                //jnl game.exe+A7B85
                _Cave_Check1.Write_jnl(GameProcess_ModuleBaseAddress + 0xA7B85);
                //jmp EXIT
                _Cave_Check1.Write_StrBytes("E9 2E 00 00 00");

                //player2:
                if (_GameConfig.GetBoolOption(P2_PEDALMODE_OPTION))
                    //cmp di, 00
                    _Cave_Check1.Write_StrBytes("66 83 FF 00");
                else
                    //cmp di, 10
                    _Cave_Check1.Write_StrBytes("66 83 FF 10");
                //jng game.exe+A7B85
                _Cave_Check1.Write_jng(GameProcess_ModuleBaseAddress + 0xA7B85);
                if (_GameConfig.GetBoolOption(P2_PEDALMODE_OPTION))
                    //cmp di, 00
                    _Cave_Check1.Write_StrBytes("66 81 FF 00 00");
                else
                    //cmp di, 0270
                    _Cave_Check1.Write_StrBytes("66 81 FF 70 02");
                //jnl game.exe+A7B85
                _Cave_Check1.Write_jnl(GameProcess_ModuleBaseAddress + 0xA7B85);
                //mov eax, edi
                _Cave_Check1.Write_StrBytes("8B C7");
                //shr eax, 10
                _Cave_Check1.Write_StrBytes("C1 E8 10");
                if (_GameConfig.GetBoolOption(P2_PEDALMODE_OPTION))
                    //cmp ax, 00
                    _Cave_Check1.Write_StrBytes("66 3D 00 00");
                else
                    //cmp ax, 10
                    _Cave_Check1.Write_StrBytes("66 3D 10 00");
                //jng game.exe+A7B85
                _Cave_Check1.Write_jng(GameProcess_ModuleBaseAddress + 0xA7B85);
                if (_GameConfig.GetBoolOption(P2_PEDALMODE_OPTION))
                    //cmp ax, 00
                    _Cave_Check1.Write_StrBytes("66 3D E0 01");
                else
                    //cmp ax, 01D0
                    _Cave_Check1.Write_StrBytes("66 3D D0 01");
                //jnl game.exe+A7B85
                _Cave_Check1.Write_jnl(GameProcess_ModuleBaseAddress + 0xA7B85);

                //Inject it
                _Cave_Check1.InjectToOffset(_Border_Check1_InjectionStruct, "Border check 1");


                _Cave_Check2 = new Codecave32(_Process, GameProcess_ModuleBaseAddress);
                //cmp [esi+1C], 0
                _Cave_Check2.Write_StrBytes("83 7E 1C 00");
                //je @player1
                _Cave_Check2.Write_StrBytes("0F 84 0A 00 00 00");
                //cmp [esi+1C], 1
                _Cave_Check2.Write_StrBytes("83 7E 1C 01");
                //je @player2
                _Cave_Check2.Write_StrBytes("0F 84 33 00 00 00");
                //player1:
                if (_GameConfig.GetBoolOption(P1_PEDALMODE_OPTION))
                    //cmp di, 00
                    _Cave_Check2.Write_StrBytes("66 83 FF 00");
                else
                    //cmp di, 10
                    _Cave_Check2.Write_StrBytes("66 83 FF 10");
                //jl game.exe+A7B85
                _Cave_Check2.Write_jl(GameProcess_ModuleBaseAddress + 0xA7B0C);
                if (_GameConfig.GetBoolOption(P1_PEDALMODE_OPTION))
                    //cmp di, 00
                    _Cave_Check2.Write_StrBytes("66 81 FF 00 00");
                else
                    //cmp di, 0270
                    _Cave_Check2.Write_StrBytes("66 81 FF 70 02");
                //jg game.exe+A7B85
                _Cave_Check2.Write_jg(GameProcess_ModuleBaseAddress + 0xA7B0C);
                //mov eax, [esp+26]
                _Cave_Check2.Write_StrBytes("66 8B 44 24 26");
                if (_GameConfig.GetBoolOption(P1_PEDALMODE_OPTION))
                    //cmp ax, 00
                    _Cave_Check2.Write_StrBytes("66 3D 00 00");
                else
                    //cmp ax, 10
                    _Cave_Check2.Write_StrBytes("66 3D 10 00");
                //jl game.exe+A7B85
                _Cave_Check2.Write_jl(GameProcess_ModuleBaseAddress + 0xA7B0C);
                if (_GameConfig.GetBoolOption(P1_PEDALMODE_OPTION))
                    //cmp ax, 00
                    _Cave_Check2.Write_StrBytes("66 3D E0 01");
                else
                    //cmp ax, 01D0
                    _Cave_Check2.Write_StrBytes("66 3D D0 01");
                //jle game.exe+A7B85
                _Cave_Check2.Write_jng(GameProcess_ModuleBaseAddress + 0xA7B85);
                //jmp EXIT
                _Cave_Check2.Write_StrBytes("E9 2E 00 00 00");

                //player2:
                if (_GameConfig.GetBoolOption(P2_PEDALMODE_OPTION))
                    //cmp di, 00
                    _Cave_Check2.Write_StrBytes("66 83 FF 00");
                else
                    //cmp di, 10
                    _Cave_Check2.Write_StrBytes("66 83 FF 10");
                //jl game.exe+A7B85
                _Cave_Check2.Write_jl(GameProcess_ModuleBaseAddress + 0xA7B0C);
                if (_GameConfig.GetBoolOption(P2_PEDALMODE_OPTION))
                    //cmp di, 00
                    _Cave_Check2.Write_StrBytes("66 81 FF 00 00");
                else
                    //cmp di, 0270
                    _Cave_Check2.Write_StrBytes("66 81 FF 70 02");
                //jg game.exe+A7B85
                _Cave_Check2.Write_jg(GameProcess_ModuleBaseAddress + 0xA7B0C);
                //mov eax, [esp+26]
                _Cave_Check2.Write_StrBytes("66 8B 44 24 26");
                if (_GameConfig.GetBoolOption(P2_PEDALMODE_OPTION))
                    //cmp ax, 00
                    _Cave_Check2.Write_StrBytes("66 3D 00 00");
                else
                    //cmp ax, 10
                    _Cave_Check2.Write_StrBytes("66 3D 10 00");
                //jl game.exe+A7B85
                _Cave_Check2.Write_jl(GameProcess_ModuleBaseAddress + 0xA7B0C);
                if (_GameConfig.GetBoolOption(P2_PEDALMODE_OPTION))
                    //cmp ax, 00
                    _Cave_Check2.Write_StrBytes("66 3D E0 01");
                else
                    //cmp ax_Cave_Check2 01D0
                    _Cave_Check2.Write_StrBytes("66 3D D0 01");
                //jle game.exe+A7B85
                _Cave_Check2.Write_jng(GameProcess_ModuleBaseAddress + 0xA7B85);

                //Inject it
                _Cave_Check2.InjectToOffset(_Border_Check2_InjectionStruct, "Border check2");
            }
        }

        #endregion

        #region Outputs Hack

        protected override void Apply_OutputsMemoryHack()
        {
            Create_OutputsDataBank();
            _P1_RecoilEnabled_CustomAddress = OutputsDatabank_Address;
            _P2_RecoilEnabled_CustomAddress = OutputsDatabank_Address + 0x04;
            _P1_DamagedEnabled_CustomAddress = OutputsDatabank_Address + 0x08;
            _P2_DamagedEnabled_CustomAddress = OutputsDatabank_Address + 0x0C;
            _P1_Ammo_CustomAddress = OutputsDatabank_Address + 0x10;
            _P2_Ammo_CustomAddress = OutputsDatabank_Address + 0x14;
            _P1_Life_CustomAddress = OutputsDatabank_Address + 0x18;
            _P2_Life_CustomAddress = OutputsDatabank_Address + 0x1C;

            GetRecoilEvent(_SetRumbleGunShot_Offset);
            GetRecoilEvent(_SetRumbleCanonShot_Offset);
            GetDamagedEvent(_SetRumbleOnHit_Offset);

            //Not knowing when these these part of code are called to vibrate, we will put it in the "Damage" output (less likely to be used than an unwanted recoil)
            GetDamagedEvent(_SetRumbleUnkownReason01_Offset);
            GetDamagedEvent(_SetRumbleUnkownReason02_Offset);
            GetDamagedEvent(_SetRumbleUnkownReason03_Offset);

            SetHack_GetAmmo();
            SetHack_GetLife();

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        /// <summary>
        /// The function to set Rumble state Up is called at many places
        /// These one will catch when the Rumble is called because of gun fire, or cannon fire
        /// and put a flag in out data bank
        private void GetRecoilEvent(UInt32 Injection_Offset)
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //push eax
            CaveMemory.Write_StrBytes("50");
            //mov eax, [esp+4]
            CaveMemory.Write_StrBytes("8B 44 24 04");
            //shl eax, 2
            CaveMemory.Write_StrBytes("C1 E0 02");
            //add eax, _P1_RecoilEnabled_customAddress
            CaveMemory.Write_StrBytes("05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_RecoilEnabled_CustomAddress));
            //mov [eax],00000001
            CaveMemory.Write_StrBytes("C7 00 01 00 00 00");
            //pop eax
            CaveMemory.Write_StrBytes("58");
            //call game.exe+D0B40
            CaveMemory.Write_call(GameProcess_ModuleBaseAddress + _SetRumbleStateFuntionOffset);
            //jmp return
            CaveMemory.Write_jmp(GameProcess_ModuleBaseAddress + Injection_Offset + 5);

            Logger.WriteLog("Adding Recoil Detetcion Codecave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            UInt32 jumpTo = CaveMemory.CaveAddress - (GameProcess_ModuleBaseAddress + Injection_Offset) - 5;
            List<byte> Buffer = new List<byte>
            {
                0xE9
            };
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            WriteBytes(GameProcess_ModuleBaseAddress + Injection_Offset, Buffer.ToArray());
        }

        /// <summary>
        /// Same thing but injected at the moment the game is setting rumble on Damage Received
        private void GetDamagedEvent(UInt32 Injection_Offset)
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);            
            
            //push eax
            CaveMemory.Write_StrBytes("50");
            //mov eax, [esp+4]
            CaveMemory.Write_StrBytes("8B 44 24 04");
            //shl eax, 2
            CaveMemory.Write_StrBytes("C1 E0 02");
            //add eax, _P1_RecoilEnabled_customAddress
            CaveMemory.Write_StrBytes("05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_DamagedEnabled_CustomAddress));
            //mov [eax],00000001
            CaveMemory.Write_StrBytes("C7 00 01 00 00 00");
            //pop eax
            CaveMemory.Write_StrBytes("58");
            //call game.exe+D0B40
            CaveMemory.Write_call(GameProcess_ModuleBaseAddress + _SetRumbleStateFuntionOffset);
            //jmp return
            CaveMemory.Write_jmp(GameProcess_ModuleBaseAddress + Injection_Offset + 5);

            Logger.WriteLog("Adding Damage Detetcion Codecave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            UInt32 jumpTo = CaveMemory.CaveAddress - (GameProcess_ModuleBaseAddress + Injection_Offset) - 5;
            List<byte> Buffer = new List<byte>
            {
                0xE9
            };
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            WriteBytes(GameProcess_ModuleBaseAddress + Injection_Offset, Buffer.ToArray());
        }

        //Weapon Struct:
        //+ 0x08 -> Weapon Type (0 = Gaitling, 1 = Canon, 2 = Mines)
        //+ 0x2C -> Gaitling Ammo
        //+ 0x30 -> Canon Ammo
        //+ 0x34 -> Mines Ammo
        //+ 0xC0 -> Player ID
        //We will read values at a time when the Struct Pointer created and is used by the game during playthrough
        private void SetHack_GetAmmo()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //mov ecx,[esi+54]
            CaveMemory.Write_StrBytes("8B 4E 54");
            //mov [edx+0C],eax
            CaveMemory.Write_StrBytes("89 42 0C");
            //push eax
            CaveMemory.Write_StrBytes("50");
            //push ebx
            CaveMemory.Write_StrBytes("53");
            //eax,[ecx+000000C0]
            CaveMemory.Write_StrBytes("8B 81 C0 00 00 00");
            //shl eax,02
            CaveMemory.Write_StrBytes("C1 E0 02");
            //add eax, _P1_Ammo_CustomAddress
            CaveMemory.Write_StrBytes("05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_Ammo_CustomAddress));
            //mov ebx,[ecx+8]
            CaveMemory.Write_StrBytes("8B 59 08");
            //shl ebx, 2
            CaveMemory.Write_StrBytes("C1 E3 02");
            //add ebx, 2C
            CaveMemory.Write_StrBytes("83 C3 2C");
            //add ebx, ecx
            CaveMemory.Write_StrBytes("01 CB");
            //mov ebx, [ebx]
            CaveMemory.Write_StrBytes("8B 1B");
            //mov [eax],ebx
            CaveMemory.Write_StrBytes("89 18");
            //pop ebx
            CaveMemory.Write_StrBytes("5B");
            //pop eax
            CaveMemory.Write_StrBytes("58");

            //Inject it
            CaveMemory.InjectToOffset(_Ammo_InjectionStruct, "Ammo");
        }

        //Struct :
        //+ 0x1C -> Player ID
        //+ 0x4C -> Life
        private void SetHack_GetLife()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //mov eax,[esi+4C]
            CaveMemory.Write_StrBytes("8B 46 4C");
            //ecx,[esi+1C]
            CaveMemory.Write_StrBytes("8B 4E 1C");
            //shl ecx,02
            CaveMemory.Write_StrBytes("C1 E1 02");
            //add ecx, _P1_Life_CustomAddress
            CaveMemory.Write_StrBytes("81 C1");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_Life_CustomAddress));
            //mov [ecx], eax
            CaveMemory.Write_StrBytes("89 01");
            //mov ecx,[esi+50]
            CaveMemory.Write_StrBytes("8B 4E 50");

            //Inject it
            CaveMemory.InjectToOffset(_Life_InjectionStruct, "Life");
        }
        #endregion

        #region No Crosshair Hack

        /// <summary>
        /// Replacing coordinates pushed to create/display the crosshair texture, to some negative values 
        /// Another possibility is to change crosshair texture filename to something that does not exits (+A57C2, etc..), but the game is displaying an empty texture with borders instead :/
        /// </summary>
        protected override void Apply_NoCrosshairMemoryHack()
        {            
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //mov ebx,[esp+18]
            CaveMemory.Write_StrBytes("8B 5C 24 18");
            //push C2C80000
            CaveMemory.Write_StrBytes("68 00 00 C8 C2"); //-100.0
            //push C2C80000
            CaveMemory.Write_StrBytes("68 00 00 C8 C2"); //-100.0

            //Inject it
            CaveMemory.InjectToOffset(_NoCrosshair_InjectionStruct, "NoCrosshair");

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
                //Adding 1 Credits to the count
                byte iCredits = ReadByte(GameProcess_ModuleBaseAddress + _Credits_Offset);
                iCredits++;
                WriteByte(GameProcess_ModuleBaseAddress + _Credits_Offset, iCredits);
                //And saving it for BookKeepings
                iCredits = ReadByte(GameProcess_ModuleBaseAddress + _BookKeeping_Credits_Offset);
                iCredits++;
                WriteByte(GameProcess_ModuleBaseAddress + _BookKeeping_Credits_Offset, iCredits);
            }
            else if (e.ButtonName.Equals("TEST"))
            {
                Apply_OR_ByteMask(GameProcess_ModuleBaseAddress + _JVS_Buttons_Offset + 1, 0x80);
            }
            else if (e.ButtonName.Equals("SERVICE"))
            {
                Apply_OR_ByteMask(GameProcess_ModuleBaseAddress + _JVS_Buttons_Offset, 0x40);
            }
        }

        protected override void GameController_OnButtonUp(object sender, VirtualGameControler.ButtonDownEventArgs e)
        {
            if (e.ButtonName.Equals("TEST"))
            {
                Apply_AND_ByteMask(GameProcess_ModuleBaseAddress + _JVS_Buttons_Offset + 1, 0x7F);
            }
            else if (e.ButtonName.Equals("SERVICE"))
            {
                Apply_AND_ByteMask(GameProcess_ModuleBaseAddress + _JVS_Buttons_Offset, 0xBF);
            }
        }

        protected override void UpdateAxis(PlayerID Id, int X, int Y)
        {
            if (Id != PlayerID.None)
            {
                byte[] bufferX = BitConverter.GetBytes(X);
                byte[] bufferY = BitConverter.GetBytes(Y);

                WriteBytes(GameProcess_ModuleBaseAddress + _P1_X_Offset + (uint)Id * 0x10, bufferX);
                WriteBytes(GameProcess_ModuleBaseAddress + _P1_Y_Offset + (uint)Id * 0x10, bufferY);
            }
        }

        protected override void PlayerController_OnButtonDown(object sender, VirtualGameControler.ButtonDownEventArgs e)
        {
            if (e.Id != PlayerID.None)
            {
                UInt32 PlayerIndex = (UInt32)e.Id;

                if (e.ButtonName.Equals("TRIGGER"))
                {
                    int iMask = 1 << (5 - (int)PlayerIndex);
                    Apply_OR_ByteMask(GameProcess_ModuleBaseAddress + _JVS_Trigger_Offset, (byte)iMask);
                }
                else if (e.ButtonName.Equals("CHANGE WEAPON"))
                {
                    int iMask = 1 << (3 - (int)PlayerIndex);
                    Apply_OR_ByteMask(GameProcess_ModuleBaseAddress + _JVS_Buttons_Offset, (byte)iMask);
                }
                else if (e.ButtonName.Equals("OFFSCREEN SHOOT"))
                {
                    bool PlayerPedalOption = e.Id == PlayerID.Player1 ? _GameConfig.GetBoolOption(P1_PEDALMODE_OPTION) : _GameConfig.GetBoolOption(P2_PEDALMODE_OPTION);
                    //With "Pedal mode", enable right-click to shoot only when hiding
                    if (PlayerPedalOption)
                    {
                        if (!_IsPedalPushed[PlayerIndex])
                        {
                            int iMask = 1 << (5 - (int)PlayerIndex);
                            Apply_OR_ByteMask(GameProcess_ModuleBaseAddress + _JVS_Trigger_Offset, (byte)iMask);
                        }
                    }
                    //Without "Pedal Mode", enable right click only if the player is not pointing in the screen boundaries
                    else
                    {
                        VirtualPlayerController Controller = (VirtualPlayerController)sender;
                        if (Controller.Computed_X < 17 || Controller.Computed_X > 624 || Controller.Computed_Y < 17 || Controller.Computed_Y > 464)
                        {
                            int iMask = 1 << (5 - (int)PlayerIndex);
                            Apply_OR_ByteMask(GameProcess_ModuleBaseAddress + _JVS_Trigger_Offset, (byte)iMask);
                        }
                    }
                }
                else if (e.ButtonName.Equals("START"))
                {
                    int iMask = 1 << (5 - (int)PlayerIndex);
                    Apply_OR_ByteMask(GameProcess_ModuleBaseAddress + _JVS_Buttons_Offset, (byte)iMask);
                }
                else if (e.ButtonName.Equals("PEDAL"))
                {
                    if (e.Id == PlayerID.Player1 && _GameConfig.GetBoolOption(P1_PEDALMODE_OPTION))
                    {
                        _IsPedalPushed[0] = true;
                        WriteBytes(_Cave_Check1.CaveAddress + 0x21, new byte[] { 0x80, 0x02 }); //640
                        WriteBytes(_Cave_Check2.CaveAddress + 0x21, new byte[] { 0x80, 0x02 }); //640
                    }
                    else if (e.Id == PlayerID.Player2 && _GameConfig.GetBoolOption(P2_PEDALMODE_OPTION))
                    {
                        _IsPedalPushed[1] = true;
                        WriteBytes((UInt32)_Cave_Check1.CaveAddress + 0x54, new byte[] { 0x80, 0x02 }); //640
                        WriteBytes((UInt32)_Cave_Check2.CaveAddress + 0x54, new byte[] { 0x80, 0x02 }); //640
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
                    int iMask = 1 << (5 - (int)PlayerIndex);
                    Apply_AND_ByteMask(GameProcess_ModuleBaseAddress + _JVS_Trigger_Offset, (byte)~iMask);
                }
                else if (e.ButtonName.Equals("CHANGE WEAPON"))
                {
                    int iMask = 1 << (3 - (int)PlayerIndex);
                    Apply_AND_ByteMask(GameProcess_ModuleBaseAddress + _JVS_Buttons_Offset, (byte)~iMask);
                }
                else if (e.ButtonName.Equals("OFFSCREEN SHOOT"))
                {
                    int iMask = 1 << (5 - (int)PlayerIndex);
                    Apply_AND_ByteMask(GameProcess_ModuleBaseAddress + _JVS_Trigger_Offset, (byte)~iMask);
                }
                else if (e.ButtonName.Equals("START"))
                {
                    int iMask = 1 << (5 - (int)PlayerIndex);
                    Apply_AND_ByteMask(GameProcess_ModuleBaseAddress + _JVS_Buttons_Offset, (byte)~iMask);
                }
                else if (e.ButtonName.Equals("PEDAL"))
                {
                    if (e.Id == PlayerID.Player1 && _GameConfig.GetBoolOption(P1_PEDALMODE_OPTION))
                    {
                        _IsPedalPushed[0] = false;
                        WriteBytes(_Cave_Check1.CaveAddress + 0x21, new byte[] { 0x00, 0x00 }); //0
                        WriteBytes(_Cave_Check2.CaveAddress + 0x21, new byte[] { 0x00, 0x00 }); //0
                    }
                    else if (e.Id == PlayerID.Player2 && _GameConfig.GetBoolOption(P2_PEDALMODE_OPTION))
                    {
                        _IsPedalPushed[1] = false;
                        WriteBytes((UInt32)_Cave_Check1.CaveAddress + 0x54, new byte[] { 0x00, 0x00 }); //0
                        WriteBytes((UInt32)_Cave_Check2.CaveAddress + 0x54, new byte[] { 0x00, 0x00 }); //0
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
            SetOutputValue(OutputId.P1_LmpStart, ReadByte(GameProcess_ModuleBaseAddress + _P1_Lamp_Offset) >> 7 & 0x01);
            SetOutputValue(OutputId.P2_LmpStart, ReadByte(GameProcess_ModuleBaseAddress + _P2_Lamp_Offset) >> 7 & 0x01);
            //Motor byte value is 1 if enabled, 0 or 2 if disabled
            SetOutputValue(OutputId.P1_GunMotor, ReadByte(GameProcess_ModuleBaseAddress + _P1_GunMotorOffset) & 0x01);
            SetOutputValue(OutputId.P2_GunMotor, ReadByte(GameProcess_ModuleBaseAddress + _P2_GunMotorOffset) & 0x01);

            //Gun motor is activated on shoot and dammage, and stays activated on dammaged untill the player hides
            //So Custom outputs will be based on game values (ammo, life)

            for (uint i = 0; i < _GameConfig.PlayerCount; i++)
            {
                _Life[i] = 0;
                _Ammo[i] = 0;

                if (ReadByte(GameProcess_ModuleBaseAddress + _P1_Playing_Offset + i) == 1)
                {
                    if (ReadByte(_P1_RecoilEnabled_CustomAddress + (4 * i)) == 1)
                    {
                        SetOutputValue(OutputId.P1_CtmRecoil + i, 1);
                        WriteByte(_P1_RecoilEnabled_CustomAddress + (4 * i), 0x00);
                    }

                    if (ReadByte(_P1_DamagedEnabled_CustomAddress + (4 * i)) == 1)
                    {
                        SetOutputValue(OutputId.P1_Damaged + i, 1);
                        WriteByte(_P1_DamagedEnabled_CustomAddress + (4 * i), 0x00);
                    }

                    _Ammo[i] = ReadByte(_P1_Ammo_CustomAddress + (4 * i));
                    _Life[i] = ReadByte(_P1_Life_CustomAddress + (4 * i));
                }

                //@game.exe+a6051 -> decrease ammo (mov, ESI+2C)
                //Pointer address + 2c = ammo
                //pointer address + c0 = player ID
                //Need to separate betwen gameplay and attract mode :(
                SetOutputValue(OutputId.P1_Ammo + i, _Ammo[i]);
                SetOutputValue(OutputId.P1_Life + i, _Life[i]);
            }

            SetOutputValue(OutputId.Credits, ReadByte(GameProcess_ModuleBaseAddress + _Credits_Offset));
        }

        #endregion
    }
}
