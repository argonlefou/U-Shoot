using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using UsCore.Config;
using UsCore.Launcher;
using UsCore.Logging;
using UsCore.MameOutput;
using UsCore.Memory;
using UsCore.VirtualControllers;

namespace U_Shoot.Games
{
    public class Game_KonamiLe3 : BaseGame32
    {
        protected override bool PATCH_PEB_DEBUG_FLAG { get { return false; } }

        private const string DISPLAY_TYPE_OPTION = "Game Mode";
        private const string PEDALMODE_OPTION = "Pedal Mode";

        private const string DISPLAY_TYPE_P1 = "Player 1";
        private const string DISPLAY_TYPE_P2 = "Player 2";
        private const string DISPLAY_TYPE_P1P2 = "P1 + P2";

        private int _GameClientWidth = 640;
        private int _GameClientHeight = 480;

        #region LOADER MEMORY ADDRESSES    
        private UInt32 _CreateFileA_FunctionPtr_Offset = 0x00227064;
        private UInt32 _WndClassBackColor_Patch_Offset = 0x0000A437;
        private UInt32 _CreateWindow_Patch_Offset = 0x00000A290;
        private UInt32 _SetwindowPosFlag_Offset = 0x0000A5C6;
        private UInt32 _GetAdaptercount_Patch_Offset = 0xA4F3;
        private UInt32 _HardwareId_Patch_Offset = 0x00136558;
        private InjectionStruct32 _D3dCreateDevice_InjectionStruct = new InjectionStruct32(0x0002AF03, 8);
        private InjectionStruct32 _D3dPresent_InjectionStruct = new InjectionStruct32(0x0002ADF3, 5);

        //Custom Data
        private UInt32 _D3dPresentSrcLeft_CaveAddress;
        private UInt32 _D3dPresentDstLeft_CaveAddress;
        private UInt32 _D3dPresentSrcRight_CaveAddress;
        private UInt32 _D3dPresentDstRight_CaveAddress;
        private UInt32 _HardwareId_CaveAddress = 0;
        #endregion

        #region INPUTS MEMORY ADDRESSES  
        private UInt32 IobEnd_Ptr_Offset = 0x002272C8;
        private UInt32 IobPutStartLamp_Ptr_Offset = 0x002272CC;
        private UInt32 IobSet1pBassShakerInit_Ptr_Offset = 0x002272D0;
        private UInt32 IobSetFootledInit_Ptr_Offset = 0x002272D4;
        private UInt32 IobSetLightInit_Ptr_Offset = 0x002272D8;
        private UInt32 IobSetGunReactionInit_Ptr_Offset = 0x002272DC;
        private UInt32 IobModifyGunReactionInit_Ptr_Offset = 0x002272E0;
        private UInt32 IobAutoGunReactionOff_Ptr_Offset = 0x002272E4;
        private UInt32 IobAutoGunReactionOn_Ptr_Offset = 0x002272E8;
        private UInt32 IobSet2pBassShakerInit_Ptr_Offset = 0x002272EC;
        private UInt32 IobSetGunReaction_Ptr_Offset = 0x002272F0;
        private UInt32 IobModifyGunReaction_Ptr_Offset = 0x002272F4;
        private UInt32 IobSet2pBassShaker_Ptr_Offset = 0x002272F8;
        private UInt32 IobSet1pBassShaker_Ptr_Offset = 0x002272FC;
        private UInt32 IobSetNowGaming_Ptr_Offset = 0x00227300;
        private UInt32 IobResetNowGaming_Ptr_Offset = 0x00227304;
        private UInt32 IobSetGameContinueStart_Ptr_Offset = 0x00227308;
        private UInt32 IobFirmUpdateWait_Ptr_Offset = 0x0022730C;
        private UInt32 IobFirmUpdateStart_Ptr_Offset = 0x00227310;
        private UInt32 IobSetLight_Ptr_Offset = 0x00227314;
        private UInt32 IobSetFootled_Ptr_Offset = 0x00227318;
        private UInt32 IobCheckInitialSecurityChecked_Ptr_Offset = 0x0022731C;
        private UInt32 IobCheckNeedFirmUpdate_Ptr_Offset = 0x00227320;
        private UInt32 IobBootWait_Ptr_Offset = 0x00227324;
        private UInt32 IobInit_Ptr_Offset = 0x00227328;
        private UInt32 IobLoopKeeperInit_Ptr_Offset = 0x0022732C;
        private UInt32 IobLoopKeeperMain_Ptr_Offset = 0x00227330;
        private UInt32 IobCheckError_Ptr_Offset = 0x00227334;
        private UInt32 IobGetDigitalIn_Ptr_Offset = 0x00227338;
        private UInt32 IobSetCoinctrDisable_Ptr_Offset = 0x0022733C;
        private UInt32 IobSetCoinctrEnable_Ptr_Offset = 0x00227340;
        private UInt32 IobCheckCoinctrEnable_Ptr_Offset = 0x00227344;
        private UInt32 IobGetId_Ptr_Offset = 0x00227348;
        private UInt32 IobAutoExInOutCheckOff_Ptr_Offset = 0x0022734C;
        private UInt32 IobAutoExInOutCheckOn_Ptr_Offset = 0x00227350;
        private UInt32 IobPutCkl_Ptr_Offset = 0x00227354;
        private UInt32 IobGetBootError_Ptr_Offset = 0x00227358;
        private UInt32 IobStart_Ptr_Offset = 0x0022735C;

        private UInt32 GunioGetDataErrorCount_Ptr_Offset = 0x0022728C;
        private UInt32 GunioReadData_Ptr_Offset = 0x00227290;
        private UInt32 GunioGetLedcheckData_Ptr_Offset = 0x00227294;
        private UInt32 GunioUpdateLedcheckData_Ptr_Offset = 0x00227298;
        private UInt32 GunioGetErrorcode_Ptr_Offset = 0x0022729C;
        private UInt32 GunioCheckError_Ptr_Offset = 0x002272A0;
        private UInt32 GunioUpdateDipswData_Ptr_Offset = 0x002272A4;
        private UInt32 GunioChangeFreqId_Ptr_Offset = 0x002272A8;
        private UInt32 GunioBootStatus_Ptr_Offset = 0x002272AC;
        private UInt32 GunioInit_Ptr_Offset = 0x002272B0;
        private UInt32 GunioDipswData_Ptr_Offset = 0x002272B4;
        private UInt32 GunioFinish_Ptr_Offset = 0x002272B8;
        private UInt32 GunioGetSerialErrorCount_Ptr_Offset = 0x002272BC;
        private UInt32 GunioGetVer_Ptr_Offset = 0x002272C0;

        private UInt32 _CreditsToAdd_Ptr_Offset = 0x294150;
        private UInt32 _AxisSquareRoot_Patch_OFfset = 0x11D661;
        private NopStruct32 _Nop_XCalibration = new NopStruct32(0x0011D5C4, 2);
        private NopStruct32 _Nop_YCalibration = new NopStruct32(0x0011D610, 3);

        //Custom Data
        private UInt32 _IobDigitalIn_CaveAddress = 0;
        private UInt32 _GunioInData_CaveAddress = 0;
        #endregion

        #region OUTPUTS MEMORY ADDRESSES
        private UInt32 _BaseData_PtrOffset = 0x00294150;
        private UInt32 _Outputs_Offset = 0xE4BC; //Based on BaseData address
        private UInt32 _PlayerInfo_BasePtr_Offset = 0x0029423C;
        private UInt32 _GameScene_Offset = 0x00294234;  //17 = in game (not in attract)
        private UInt32 _Player1_Playing = 0x002945D0;
        //private UInt32 _Player2_Playing = 0x002945D4;
        private UInt32 _CreditsTotal_Offset = 0x0037462C;

        //Custom data
        private UInt32 _Recoil_CaveAddress;
        private UInt32 _BassShaker_CaveAddress;
        #endregion

        public Game_KonamiLe3(GameConfig Config) : base(Config)
        {
            _KnownMd5Prints.Add("Lethal Enforcer 3 v2005-04-15-1 - Original game.exe", "1d338c452c7b087bc7aad823a74bb023");
            _KnownMd5Prints.Add("Lethal Enforcer 3 v2005-04-15-1 - DemulShooter compatible game.exe", "ce1359efe2dde0ac02280bfe8c96681d");
        }

        private UInt32 _Custom_VoidStub_Address = 0;
        private UInt32 _Custom_TrueStub_Address = 0;
        private UInt32 _Custom_FalseStub_Address = 0;

        private enum ButtonsSlot : uint
        {
            Test = 0x04,
            CoinMech = 0x05,
            Service = 0x07,
            P1_Start = 0x08,
            P2_Start,
            P1_Trigger,
            P2_Trigger,
            P1_Option,
            P2_Option
        }

        private int[] _Weapon = new int[2];
        private int[] _LastWeapon = new int[2];

        protected override void AttachDebuggerEvents()
        { }

        #region Debugger Events

        #region EXCEPTION_DEBUG_EVENT

        protected override void ExceptionEvent(object sender, x86_x64_Debugger.ExceptionDebugEventArgs e)
        {
            //Using Generic Exception event Handling to set Input/Outputs Hacks
            base.ExceptionEvent(sender, e);
        }

        //protected override void ExceptionEvent(object sender, x86_x64_Debugger.ExceptionDebugEventArgs e)
        //{
        //    x86_x64_Debugger Dbg = (x86_x64_Debugger)sender;
        //    uint dwThreadId = e.dwThreadId;
        
        //    //When started by X64 process, X86 target process run in Wow64 emulator for X86 and Exceptions events are not the same :
        //    //STATUS_BREAKPOINT is now replaced by STATUS_WX86_BREAKPOINT
        //    if (e.ExceptionInfo.ExceptionRecord.ExceptionCode == Win32.STATUS_WX86_BREAKPOINT)
        //    {
        //        Logger.WriteLog(" > ExceptionCode: STATUS_WX86_BREAKPOINT");
        //        Logger.WriteLog("ExceptionAddress = 0x" + e.ExceptionInfo.ExceptionRecord.ExceptionAddress.ToString(Dbg.HexStringFormat));

        //        if (_IsFirstException)
        //        {
        //            Logger.WriteLog(" > Initital Breakpoint");
        //            Apply_MemoryHacks();
        //            _IsFirstException = false;
        //        }
        //        else
        //        {
        //            if ((UInt32)e.ExceptionInfo.ExceptionRecord.ExceptionAddress == GameProcess_ModuleBaseAddress + bp.MemoryOffset)
        //            {
        //                Logger.WriteLog(" > JVSEmuEA.dll unpacked breakpoint reached");
        //                ThreadContextWow64 Context32 = (ThreadContextWow64)_GameLauncher.GetThreadContext(e.dwThreadId);

        //                Context32.Context.Eip--;
        //                if (!_GameLauncher.SetThreadContext(dwThreadId, Context32))
        //                    Logger.WriteLog(" > SetThreadContext() error !");
        //                else
        //                    RemoveBreakpoint(bp);

        //                RaiseGameHookedEvent();
        //            }
        //        }
        //    }
        //}

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

                    double dMinX = 1;
                    double dMinY = 1;
                    double dMaxX = 639.0;
                    double dMaxY = 479.0;
                    double dRangeX = dMaxX - dMinX + 1;
                    double dRangeY = dMaxY - dMinY + 1;
                    
                    //X or Y calculation will differ if fullscreen dual player
                    if (_GameConfig.GetStringOption(DISPLAY_TYPE_OPTION).Equals(DISPLAY_TYPE_P1P2))
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
                            //For Y we need to change reference from Screen[X, Y] to real Game [X, Y] with black border;
                            double Ratio = TotalResX / 1280.0;
                            double GameHeight = Ratio * 480.0;
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
                    else
                    {
                        //Single Screen, no adaptation
                        PlayerController.Computed_X = Convert.ToInt32(Math.Round(dRangeX * PlayerController.Computed_X / TotalResX));
                        PlayerController.Computed_Y = Convert.ToInt32(Math.Round(dRangeY * PlayerController.Computed_Y / TotalResY));
                    }
                        
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
            Create_LoaderDataBank();
            _D3dPresentSrcLeft_CaveAddress = LoaderDatabank_Address;            //16 bytes (rect struct)
            _D3dPresentDstLeft_CaveAddress = LoaderDatabank_Address + 0x10;     //16 bytes (rect struct)
            _D3dPresentSrcRight_CaveAddress = LoaderDatabank_Address + 0x20;    //16 bytes (rect struct)
            _D3dPresentDstRight_CaveAddress = LoaderDatabank_Address + 0x30;    //16 bytes (rect struct)
            _HardwareId_CaveAddress = LoaderDatabank_Address + 0x40;

            WriteBytes(_HardwareId_CaveAddress, Encoding.ASCII.GetBytes("U-SHOOT " + Application.ProductVersion));

            SetHack_Display();

            //Disabled : for movies, the game is passing the string to another dll and later calls CreateFileW()
            //Patching all strings in the file will be better
            //SetHack_CreateFileA();

            WriteByte(GameProcess_ModuleBaseAddress + _HardwareId_Patch_Offset, 0xBA);
            WriteBytes(GameProcess_ModuleBaseAddress + _HardwareId_Patch_Offset + 1, BitConverter.GetBytes(_HardwareId_CaveAddress));
            WriteByte(GameProcess_ModuleBaseAddress + _HardwareId_Patch_Offset + 5, 0x90);


            //CRC check bypass, not needed anymore as patch is done dynamically and file is not changed
            //WriteBytes(0x0536BB3, new byte[] { 0x90, 0x90 } );

            ReplaceReadOnlyString(new UInt32[] { 0x246994, 0x2469B4, 0x246AB4, 0x246DAC, 0x24AF38}, Encoding.ASCII.GetBytes("SAVE\0"));
            ReplaceReadOnlyString(new UInt32[] { 0x24AF48, 0x246AC0 }, Encoding.ASCII.GetBytes("SAVE\\\0"));

            //Change D:\\D22\.... hardcoded values
            ReplaceReadOnlyString(new UInt32[] { 0x246D18 }, Encoding.ASCII.GetBytes("DATA\\movies\\%s\0"));
            ReplaceReadOnlyString(new UInt32[] { 0x249E00, 0x24D148 }, Encoding.ASCII.GetBytes("DATA\\check.cc\0"));
            ReplaceReadOnlyString(new UInt32[] { 0x249E18, 0x24D160 }, Encoding.ASCII.GetBytes("DATA\\check.a\0"));
            ReplaceReadOnlyString(new UInt32[] { 0x249F34, 0x24D24C }, Encoding.ASCII.GetBytes("DATA\\testmode.a\0"));
            ReplaceReadOnlyString(new UInt32[] { 0x249E40 }, Encoding.ASCII.GetBytes("game.exe\0"));
            ReplaceReadOnlyString(new UInt32[] { 0x249E64 }, Encoding.ASCII.GetBytes("iob.dll\0"));
            ReplaceReadOnlyString(new UInt32[] { 0x249E88 }, Encoding.ASCII.GetBytes("gunio.dll\0"));
            ReplaceReadOnlyString(new UInt32[] { 0x249EB0 }, Encoding.ASCII.GetBytes("pmds.dll\0"));
            ReplaceReadOnlyString(new UInt32[] { 0x249ED8 }, Encoding.ASCII.GetBytes("msvcr71.dll\0"));
            ReplaceReadOnlyString(new UInt32[] { 0x24D188 }, Encoding.ASCII.GetBytes("game.exe\0"));
            ReplaceReadOnlyString(new UInt32[] { 0x24D1AC }, Encoding.ASCII.GetBytes("iob.dll\0"));
            ReplaceReadOnlyString(new UInt32[] { 0x24D1D0 }, Encoding.ASCII.GetBytes("gunio.dll\0"));
            ReplaceReadOnlyString(new UInt32[] { 0x24D1F8 }, Encoding.ASCII.GetBytes("pmds.dll\0"));
            ReplaceReadOnlyString(new UInt32[] { 0x24D220 }, Encoding.ASCII.GetBytes("msvcr71.dll\0"));
            ReplaceReadOnlyString(new UInt32[] { 0x24D278 }, Encoding.ASCII.GetBytes("DATA\\DATA.a\0"));
            ReplaceReadOnlyString(new UInt32[] { 0x24D2A4 }, Encoding.ASCII.GetBytes("DATA\\movies\\demo_a.m1v\0"));
            ReplaceReadOnlyString(new UInt32[] { 0x24D2DC }, Encoding.ASCII.GetBytes("DATA\\movies\\demo_b.m1v\0"));
            ReplaceReadOnlyString(new UInt32[] { 0x24D314 }, Encoding.ASCII.GetBytes("DATA\\movies\\demo_c.m1v\0"));
            ReplaceReadOnlyString(new UInt32[] { 0x24D34C }, Encoding.ASCII.GetBytes("DATA\\movies\\demo_d.m1v\0"));
            ReplaceReadOnlyString(new UInt32[] { 0x24D384 }, Encoding.ASCII.GetBytes("DATA\\movies\\demo_e.m1v\0"));
            ReplaceReadOnlyString(new UInt32[] { 0x24D3BC }, Encoding.ASCII.GetBytes("DATA\\movies\\demo_f.m1v\0"));
            ReplaceReadOnlyString(new UInt32[] { 0x24D3FC }, Encoding.ASCII.GetBytes("DATA\\movies\\KP3_OP_ALL_640.m1v\0"));
            ReplaceReadOnlyString(new UInt32[] { 0x24D438 }, Encoding.ASCII.GetBytes("DATA\\movies\\StA01.m1v\0"));
            ReplaceReadOnlyString(new UInt32[] { 0x24D46C }, Encoding.ASCII.GetBytes("DATA\\movies\\sta02.m1v\0"));
            ReplaceReadOnlyString(new UInt32[] { 0x24D4A0 }, Encoding.ASCII.GetBytes("DATA\\movies\\sta03.m1v\0"));
            ReplaceReadOnlyString(new UInt32[] { 0x24D4D4 }, Encoding.ASCII.GetBytes("DATA\\movies\\STA05.m1v\0"));
            ReplaceReadOnlyString(new UInt32[] { 0x24D508 }, Encoding.ASCII.GetBytes("DATA\\movies\\STB01.m1v\0"));
            ReplaceReadOnlyString(new UInt32[] { 0x24D53C }, Encoding.ASCII.GetBytes("DATA\\movies\\stb02.m1v\0"));
            ReplaceReadOnlyString(new UInt32[] { 0x24D570 }, Encoding.ASCII.GetBytes("DATA\\movies\\stb03.m1v\0"));
            ReplaceReadOnlyString(new UInt32[] { 0x24D5A4 }, Encoding.ASCII.GetBytes("DATA\\movies\\stb05.m1v\0"));
            ReplaceReadOnlyString(new UInt32[] { 0x24D5D8 }, Encoding.ASCII.GetBytes("DATA\\movies\\stc01.m1v\0"));
            ReplaceReadOnlyString(new UInt32[] { 0x24D60C }, Encoding.ASCII.GetBytes("DATA\\movies\\stc02.m1v\0"));
            ReplaceReadOnlyString(new UInt32[] { 0x24D640 }, Encoding.ASCII.GetBytes("DATA\\movies\\stc03.m1v\0"));
            ReplaceReadOnlyString(new UInt32[] { 0x24D674 }, Encoding.ASCII.GetBytes("DATA\\movies\\STC05.m1v\0"));
            ReplaceReadOnlyString(new UInt32[] { 0x24D6A8 }, Encoding.ASCII.GetBytes("DATA\\movies\\STD01.m1v\0"));
            ReplaceReadOnlyString(new UInt32[] { 0x24D6DC }, Encoding.ASCII.GetBytes("DATA\\movies\\std02.m1v\0"));
            ReplaceReadOnlyString(new UInt32[] { 0x24D710 }, Encoding.ASCII.GetBytes("DATA\\movies\\std03.m1v\0"));
            ReplaceReadOnlyString(new UInt32[] { 0x24D744 }, Encoding.ASCII.GetBytes("DATA\\movies\\std05.m1v\0"));
            ReplaceReadOnlyString(new UInt32[] { 0x24D778 }, Encoding.ASCII.GetBytes("DATA\\movies\\ste01.m1v\0"));
            ReplaceReadOnlyString(new UInt32[] { 0x24D7AC }, Encoding.ASCII.GetBytes("DATA\\movies\\stE02.m1v\0"));
            ReplaceReadOnlyString(new UInt32[] { 0x24D7E0 }, Encoding.ASCII.GetBytes("DATA\\movies\\stE03.m1v\0"));
            ReplaceReadOnlyString(new UInt32[] { 0x24D814 }, Encoding.ASCII.GetBytes("DATA\\movies\\ste05.m1v\0"));
            ReplaceReadOnlyString(new UInt32[] { 0x24D848 }, Encoding.ASCII.GetBytes("DATA\\movies\\stf01.m1v\0"));
            ReplaceReadOnlyString(new UInt32[] { 0x24D87C }, Encoding.ASCII.GetBytes("DATA\\movies\\stf02.m1v\0"));
            ReplaceReadOnlyString(new UInt32[] { 0x24D8B0 }, Encoding.ASCII.GetBytes("DATA\\movies\\stf03.m1v\0"));
            ReplaceReadOnlyString(new UInt32[] { 0x24D8E4 }, Encoding.ASCII.GetBytes("DATA\\movies\\stf06.m1v\0"));
            ReplaceReadOnlyString(new UInt32[] { 0x24D918 }, Encoding.ASCII.GetBytes("DATA\\movies\\Stf09.m1v\0"));

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        /// <summary>
        /// the game is originally using 2 adapters to display 2 sides of the game (P1 / P2) into the same Window.
        /// Changing a bit the procedure, by forcing the renderer to use Windowed mode, it is possible to alter size and position of each player Viewport
        /// </summary>
        private void SetHack_Display()
        {
            //Forcing BLACK background in WNDCLASS struct 
            IntPtr BlackBrush = Win32.GetStockObject(Win32.BLACK_BRUSH);
            WriteBytes(GameProcess_ModuleBaseAddress + _WndClassBackColor_Patch_Offset, BitConverter.GetBytes((int)BlackBrush));

            //Src rect is original drawing canvas (0,0,640,480) for both sides (Present() function)
            WriteBytes(_D3dPresentSrcLeft_CaveAddress, BitConverter.GetBytes((UInt32)0));
            WriteBytes(_D3dPresentSrcLeft_CaveAddress + 0x04, BitConverter.GetBytes((UInt32)0));
            WriteBytes(_D3dPresentSrcLeft_CaveAddress + 0x08, BitConverter.GetBytes((UInt32)640));
            WriteBytes(_D3dPresentSrcLeft_CaveAddress + 0x0C, BitConverter.GetBytes((UInt32)480));
            WriteBytes(_D3dPresentSrcRight_CaveAddress, BitConverter.GetBytes((UInt32)0));
            WriteBytes(_D3dPresentSrcRight_CaveAddress + 0x04, BitConverter.GetBytes((UInt32)0));
            WriteBytes(_D3dPresentSrcRight_CaveAddress + 0x08, BitConverter.GetBytes((UInt32)640));
            WriteBytes(_D3dPresentSrcRight_CaveAddress + 0x0C, BitConverter.GetBytes((UInt32)480));

            //Dst RECT values depends on Screen size and screen mode
            //Default CLIENT size is 640x480 (solo display, Windowed)
            if (_GameConfig.Fullscreen)
            {
                _GameClientWidth = _DesktopScreenWidth;
                _GameClientHeight = _DesktopScreenHeight;             


                //Changing params of SetwindowPos()
                WriteByte(GameProcess_ModuleBaseAddress + _SetwindowPosFlag_Offset, 0x42);

                if (_GameConfig.GetStringOption(DISPLAY_TYPE_OPTION, DISPLAY_TYPE_P1).Equals(DISPLAY_TYPE_P1P2))
                {
                    double dPlayerViewportWidth = (double)_DesktopScreenWidth / 2;
                    double dPlayerViewportRatio = dPlayerViewportWidth / 640;
                    double dPlayerViewportHeight = dPlayerViewportRatio * 480;
                    double dBorderHeight = (_DesktopScreenHeight - dPlayerViewportHeight) / 2;

                    WriteBytes(_D3dPresentDstLeft_CaveAddress, BitConverter.GetBytes(0));
                    WriteBytes(_D3dPresentDstLeft_CaveAddress + 0x04, BitConverter.GetBytes((int)dBorderHeight));
                    WriteBytes(_D3dPresentDstLeft_CaveAddress + 0x08, BitConverter.GetBytes(_DesktopScreenWidth / 2));
                    WriteBytes(_D3dPresentDstLeft_CaveAddress + 0x0C, BitConverter.GetBytes(_DesktopScreenHeight - (int)dBorderHeight));

                    WriteBytes(_D3dPresentDstRight_CaveAddress, BitConverter.GetBytes(_DesktopScreenWidth / 2));
                    WriteBytes(_D3dPresentDstRight_CaveAddress + 0x04, BitConverter.GetBytes((int)dBorderHeight));
                    WriteBytes(_D3dPresentDstRight_CaveAddress + 0x08, BitConverter.GetBytes(_DesktopScreenWidth));
                    WriteBytes(_D3dPresentDstRight_CaveAddress + 0x0C, BitConverter.GetBytes(_DesktopScreenHeight - (int)dBorderHeight));

                    
                }
                else
                {
                    double dPlayerViewportRatio = (double)_DesktopScreenHeight / 480;
                    double dPlayerViewportWidth = dPlayerViewportRatio * 640;
                    double dBorderWidth = (_DesktopScreenWidth - dPlayerViewportWidth) / 2;

                    if (_GameConfig.GetStringOption(DISPLAY_TYPE_OPTION, DISPLAY_TYPE_P1).Equals(DISPLAY_TYPE_P1))
                    {
                        WriteBytes(_D3dPresentDstLeft_CaveAddress, BitConverter.GetBytes((int)dBorderWidth));
                        WriteBytes(_D3dPresentDstLeft_CaveAddress + 0x04, BitConverter.GetBytes(0));
                        WriteBytes(_D3dPresentDstLeft_CaveAddress + 0x08, BitConverter.GetBytes(_DesktopScreenWidth - (int)dBorderWidth));
                        WriteBytes(_D3dPresentDstLeft_CaveAddress + 0x0C, BitConverter.GetBytes(_DesktopScreenHeight));

                        WriteBytes(_D3dPresentDstRight_CaveAddress, BitConverter.GetBytes(0));
                        WriteBytes(_D3dPresentDstRight_CaveAddress + 0x04, BitConverter.GetBytes(0));
                        WriteBytes(_D3dPresentDstRight_CaveAddress + 0x08, BitConverter.GetBytes(1));
                        WriteBytes(_D3dPresentDstRight_CaveAddress + 0x0C, BitConverter.GetBytes(1));
                    }
                    else
                    {
                        WriteBytes(_D3dPresentDstLeft_CaveAddress, BitConverter.GetBytes(0));
                        WriteBytes(_D3dPresentDstLeft_CaveAddress + 0x04, BitConverter.GetBytes(0));
                        WriteBytes(_D3dPresentDstLeft_CaveAddress + 0x08, BitConverter.GetBytes(1));
                        WriteBytes(_D3dPresentDstLeft_CaveAddress + 0x0C, BitConverter.GetBytes(1));

                        WriteBytes(_D3dPresentDstRight_CaveAddress, BitConverter.GetBytes((int)dBorderWidth));
                        WriteBytes(_D3dPresentDstRight_CaveAddress + 0x04, BitConverter.GetBytes(0));
                        WriteBytes(_D3dPresentDstRight_CaveAddress + 0x08, BitConverter.GetBytes(_DesktopScreenWidth - (int)dBorderWidth));
                        WriteBytes(_D3dPresentDstRight_CaveAddress + 0x0C, BitConverter.GetBytes(_DesktopScreenHeight));
                    }

                    //Forcing X Scaling because of black borders on the side
                    double dScale = _DesktopScreenHeight / (double)480;
                    double dViewportWidth = 640 * dScale;
                    _AxisX_ScaleFactor = _DesktopScreenWidth / dViewportWidth;
                }
                
            }
            else if (_GameConfig.Windowed)
            {
                //Changing the style of CreateWindowEx
                WriteBytes(GameProcess_ModuleBaseAddress + _CreateWindow_Patch_Offset + 0x1C, BitConverter.GetBytes(0x00CA0000)); //CAPTION | MENUSYSTEM
                WriteBytes(GameProcess_ModuleBaseAddress + _CreateWindow_Patch_Offset + 0x2B, BitConverter.GetBytes(0x00000300)); //Borders

                //Changing params of SetwindowPos()
                WriteByte(GameProcess_ModuleBaseAddress + _SetwindowPosFlag_Offset, 0x40);

                if (_GameConfig.GetStringOption(DISPLAY_TYPE_OPTION, DISPLAY_TYPE_P1).Equals(DISPLAY_TYPE_P1P2))
                {
                    _GameClientWidth = 1280;

                    WriteBytes(_D3dPresentDstLeft_CaveAddress, BitConverter.GetBytes(0));
                    WriteBytes(_D3dPresentDstLeft_CaveAddress + 0x04, BitConverter.GetBytes(0));
                    WriteBytes(_D3dPresentDstLeft_CaveAddress + 0x08, BitConverter.GetBytes((UInt32)_GameClientWidth / 2));
                    WriteBytes(_D3dPresentDstLeft_CaveAddress + 0x0C, BitConverter.GetBytes((UInt32)_GameClientHeight));

                    WriteBytes(_D3dPresentDstRight_CaveAddress, BitConverter.GetBytes((UInt32)_GameClientWidth / 2));
                    WriteBytes(_D3dPresentDstRight_CaveAddress + 0x04, BitConverter.GetBytes(0));
                    WriteBytes(_D3dPresentDstRight_CaveAddress + 0x08, BitConverter.GetBytes((UInt32)_GameClientWidth));
                    WriteBytes(_D3dPresentDstRight_CaveAddress + 0x0C, BitConverter.GetBytes((UInt32)_GameClientHeight));
                }
                else if (_GameConfig.GetStringOption(DISPLAY_TYPE_OPTION, DISPLAY_TYPE_P1).Equals(DISPLAY_TYPE_P1))
                {
                    WriteBytes(_D3dPresentDstLeft_CaveAddress, BitConverter.GetBytes(0));
                    WriteBytes(_D3dPresentDstLeft_CaveAddress + 0x04, BitConverter.GetBytes(0));
                    WriteBytes(_D3dPresentDstLeft_CaveAddress + 0x08, BitConverter.GetBytes((UInt32)_GameClientWidth));
                    WriteBytes(_D3dPresentDstLeft_CaveAddress + 0x0C, BitConverter.GetBytes((UInt32)_GameClientHeight));

                    WriteBytes(_D3dPresentDstRight_CaveAddress, BitConverter.GetBytes(0));
                    WriteBytes(_D3dPresentDstRight_CaveAddress + 0x04, BitConverter.GetBytes(0));
                    WriteBytes(_D3dPresentDstRight_CaveAddress + 0x08, BitConverter.GetBytes((UInt32)1));
                    WriteBytes(_D3dPresentDstRight_CaveAddress + 0x0C, BitConverter.GetBytes((UInt32)1));
                }
                else
                {
                    WriteBytes(_D3dPresentDstLeft_CaveAddress, BitConverter.GetBytes(0));
                    WriteBytes(_D3dPresentDstLeft_CaveAddress + 0x04, BitConverter.GetBytes(0));
                    WriteBytes(_D3dPresentDstLeft_CaveAddress + 0x08, BitConverter.GetBytes((UInt32)1));
                    WriteBytes(_D3dPresentDstLeft_CaveAddress + 0x0C, BitConverter.GetBytes((UInt32)1));

                    WriteBytes(_D3dPresentDstRight_CaveAddress, BitConverter.GetBytes((UInt32)_GameClientWidth));
                    WriteBytes(_D3dPresentDstRight_CaveAddress + 0x04, BitConverter.GetBytes(0));
                    WriteBytes(_D3dPresentDstRight_CaveAddress + 0x08, BitConverter.GetBytes((UInt32)_GameClientWidth));
                    WriteBytes(_D3dPresentDstRight_CaveAddress + 0x0C, BitConverter.GetBytes((UInt32)_GameClientHeight));
                }
            }

            //Forcing AdapterCount result to 2 (mov EAX, 2) instead of calling function
            //That way, forcing the game to render both sides. It will be decided later which one is displayed and where
            WriteBytes(GameProcess_ModuleBaseAddress + _GetAdaptercount_Patch_Offset, new byte[] { 0xB8, 0x02, 0x00, 0x00, 0x00 });

            //Changing the final size of the window set by SetWindowPos()
            WriteBytes(GameProcess_ModuleBaseAddress + _SetwindowPosFlag_Offset + 2, BitConverter.GetBytes(_GameClientHeight));
            WriteBytes(GameProcess_ModuleBaseAddress + _SetwindowPosFlag_Offset + 7, BitConverter.GetBytes(_GameClientWidth));

            SetHack_D3dCreateDevice();
            SetHack_D3dPresent();
        }

        /// <summary>
        /// Changing Presentation Parameters to allow d3d8 to use a WINDOWED mode
        /// That way, depending on the CreatewindowEx() style, it is possible to get a windowed or a fullscreen-window mode
        /// </summary>
        private void SetHack_D3dCreateDevice()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //mov ecx,[ebp+14]
            CaveMemory.Write_StrBytes("8B 4D 14");
            //add ecx, 10
            CaveMemory.Write_StrBytes("83 C1 10");
            //mov [ecx+08],00000015
            CaveMemory.Write_StrBytes("C7 41 08 15 00 00 00");
            //mov [ecx+0C],00000001
            CaveMemory.Write_StrBytes("C7 41 0C 01 00 00 00");
            //mov [ecx+14],00000003
            CaveMemory.Write_StrBytes("C7 41 14 03 00 00 00");
            //mov [ecx+1C],00000001
            CaveMemory.Write_StrBytes("C7 41 1C 01 00 00 00");
            //mov [ecx+28],00000000
            CaveMemory.Write_StrBytes("C7 41 28 00 00 00 00");
            //mov [ecx+2C],00000000
            CaveMemory.Write_StrBytes("C7 41 2C 00 00 00 00");
            //mov [ecx+30],00000000
            CaveMemory.Write_StrBytes("C7 41 30 00 00 00 00");
            //mov ecx,[ebp+14]
            CaveMemory.Write_StrBytes("8B 4D 14");
            //mov ecx,[ecx+48]
            CaveMemory.Write_StrBytes("8B 49 48");
            //push ecx
            CaveMemory.Write_StrBytes("51");
            //push 00
            CaveMemory.Write_StrBytes("6A 00");

            //inject it
            CaveMemory.InjectToOffset(_D3dCreateDevice_InjectionStruct, "CreateDevice()");
        }

        /// <summary>
        /// Changint the Direct8 Present() rect parameters to redraw left and right images in the canvas
        /// without that patch, both left and right images are presented in the full Window client area on top of each other
        /// </summary>
        private void SetHack_D3dPresent()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //push 00
            CaveMemory.Write_StrBytes("6A 00");
            //push 00
            CaveMemory.Write_StrBytes("6A 00");
            //push eax
            CaveMemory.Write_StrBytes("50");
            //cmp esi,00
            CaveMemory.Write_StrBytes("83 FE 00");
            //je LeftSide
            CaveMemory.Write_StrBytes("74 12");
            //mov [esp+04],;
            CaveMemory.Write_StrBytes("C7 44 24 04");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_D3dPresentSrcRight_CaveAddress));
            //mov [esp+08],_D3dPresentSrcRight_CaveAddress
            CaveMemory.Write_StrBytes("C7 44 24 08");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_D3dPresentDstRight_CaveAddress));
            //jmp Exit
            CaveMemory.Write_StrBytes("EB 10");

            //LeftSide:
            //mov [esp+04],_D3dPresentSrcLeft_CaveAddress;
            CaveMemory.Write_StrBytes("C7 44 24 04");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_D3dPresentSrcLeft_CaveAddress));
            //mov [esp+08],_D3dPresentSrcRight_CaveAddress
            CaveMemory.Write_StrBytes("C7 44 24 08");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_D3dPresentDstLeft_CaveAddress));

            //Inject it
            CaveMemory.InjectToOffset(_D3dPresent_InjectionStruct, "Present()");
        }

        /// <summary>
        /// Hook to CreatefileA() to replace all FileNames starting with D:\D22\ with local path
        /// </summary>
        private void SetHack_CreateFileA()
        {
            UInt32 CreateFileA_function_Address = ReadUInt32(GameProcess_ModuleBaseAddress + _CreateFileA_FunctionPtr_Offset);

            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);
            //mov eax,[esp+04]
            CaveMemory.Write_StrBytes("8B 44 24 04");
            //cmp [eax],445C3A44
            CaveMemory.Write_StrBytes("81 38 44 3A 5C 44");
            //jne 02970032
            CaveMemory.Write_StrBytes("75 1A");
            //mov eax,[eax+04]
            CaveMemory.Write_StrBytes("8B 40 04");
            //and eax,00FFFFFF
            CaveMemory.Write_StrBytes("25 FF FF FF 00");
            //cmp eax,game.exe+1C3232
            CaveMemory.Write_StrBytes("3D 32 32 5C 00");
            //jne 02970032
            CaveMemory.Write_StrBytes("75 0B");
            //mov eax,[esp+04]
            CaveMemory.Write_StrBytes("8B 44 24 04");
            //add eax,07
            CaveMemory.Write_StrBytes("83 C0 07");
            //mov [esp+04],eax
            CaveMemory.Write_StrBytes("89 44 24 04");
            //jmp dword ptr [game.exe+227064]
            CaveMemory.Write_jmp(CreateFileA_function_Address);

            ReplaceReadOnlyString(GameProcess_ModuleBaseAddress, new UInt32[] { _CreateFileA_FunctionPtr_Offset }, BitConverter.GetBytes(CaveMemory.CaveAddress));
        }

        #endregion        

        #region Inputs Hack

        protected override void Apply_InputsMemoryHack()
        {
            Create_InputsDataBank();
            _IobDigitalIn_CaveAddress = InputsDatabank_Address;         //IOB_DIGITAL_IN struct length = 0x2C
            _GunioInData_CaveAddress = InputsDatabank_Address + 0x30;   //6*4 bytes per player, P2 starts at index +0x20

            CreateVoidStub();
            CreateTrueStub();
            CreateFalseStub();
            Apply_IobDll_Hacks();
            Apply_GunioDllHacks();

            //Removing calibration calculation based on data given in the struct
            SetNops(GameProcess_ModuleBaseAddress, _Nop_XCalibration);
            SetNops(GameProcess_ModuleBaseAddress, _Nop_YCalibration);
            //And that one changes the axes based on the Sqrt (test long/short move ??). Just jumping over
            WriteByte(GameProcess_ModuleBaseAddress + _AxisSquareRoot_Patch_OFfset, 0xEB);

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        private void Apply_IobDll_Hacks()
        {
            ReplaceReadOnlyString(GameProcess_ModuleBaseAddress, new UInt32[] { 
                IobEnd_Ptr_Offset,
                IobPutStartLamp_Ptr_Offset,
                IobSet1pBassShakerInit_Ptr_Offset,
                IobSetFootledInit_Ptr_Offset,
                IobSetLightInit_Ptr_Offset,
                IobSetGunReactionInit_Ptr_Offset,
                IobModifyGunReactionInit_Ptr_Offset,
                IobAutoGunReactionOff_Ptr_Offset,
                IobAutoGunReactionOn_Ptr_Offset,
                IobSet2pBassShakerInit_Ptr_Offset,
                IobInit_Ptr_Offset,
                IobLoopKeeperInit_Ptr_Offset,
                IobLoopKeeperMain_Ptr_Offset,
                IobSetCoinctrDisable_Ptr_Offset,
                IobSetCoinctrEnable_Ptr_Offset,
                IobAutoExInOutCheckOff_Ptr_Offset,
                IobAutoExInOutCheckOn_Ptr_Offset,
                IobStart_Ptr_Offset
            }, BitConverter.GetBytes(_Custom_VoidStub_Address));
            
            ReplaceReadOnlyString(GameProcess_ModuleBaseAddress, new UInt32[] {
                IobModifyGunReaction_Ptr_Offset,
                IobSetLight_Ptr_Offset,
                IobSetFootled_Ptr_Offset,
                IobCheckInitialSecurityChecked_Ptr_Offset,
                IobCheckNeedFirmUpdate_Ptr_Offset,
                IobCheckError_Ptr_Offset,
                IobPutCkl_Ptr_Offset
            }, BitConverter.GetBytes(_Custom_FalseStub_Address));

            ReplaceReadOnlyString(GameProcess_ModuleBaseAddress, new UInt32[] { IobBootWait_Ptr_Offset }, BitConverter.GetBytes(_Custom_TrueStub_Address));

            ReplaceReadOnlyString(GameProcess_ModuleBaseAddress, new UInt32[] { IobGetDigitalIn_Ptr_Offset }, BitConverter.GetBytes(CreateCustomIobDigitalData()));

            //ReplaceReadOnlyString(GameProcess_ModuleBaseAddress, new UInt32[]{ IobSetNowGaming_Ptr_Offset }, BitConverter.GetBytes());
            //ReplaceReadOnlyString(GameProcess_ModuleBaseAddress, new UInt32[]{ IobResetNowGaming_Ptr_Offset }, BitConverter.GetBytes());
            //ReplaceReadOnlyString(GameProcess_ModuleBaseAddress, new UInt32[]{ IobSetGameContinueStart_Ptr_Offset }, BitConverter.GetBytes());
            //ReplaceReadOnlyString(GameProcess_ModuleBaseAddress, new UInt32[]{ IobFirmUpdateWait_Ptr_Offset }, BitConverter.GetBytes());
            //ReplaceReadOnlyString(GameProcess_ModuleBaseAddress, new UInt32[]{ IobFirmUpdateStart_Ptr_Offset }, BitConverter.GetBytes());
            //ReplaceReadOnlyString(GameProcess_ModuleBaseAddress, new UInt32[]{ IobCheckCoinctrEnable_Ptr_Offset }, BitConverter.GetBytes());
            //ReplaceReadOnlyString(GameProcess_ModuleBaseAddress, new UInt32[]{ IobGetId_Ptr_Offset }, BitConverter.GetBytes());
            //ReplaceReadOnlyString(GameProcess_ModuleBaseAddress, new UInt32[]{ IobGetBootError_Ptr_Offset }, BitConverter.GetBytes(_Custom_FalseStub_Address));
        }

        private void Apply_GunioDllHacks()
        {
            ReplaceReadOnlyString(GameProcess_ModuleBaseAddress, new UInt32[] {
                GunioUpdateLedcheckData_Ptr_Offset,
                GunioUpdateDipswData_Ptr_Offset,
                GunioChangeFreqId_Ptr_Offset,
                GunioInit_Ptr_Offset,
                GunioFinish_Ptr_Offset,
            }, BitConverter.GetBytes(_Custom_VoidStub_Address));

            ReplaceReadOnlyString(GameProcess_ModuleBaseAddress, new UInt32[] {
                GunioGetDataErrorCount_Ptr_Offset,
                GunioGetErrorcode_Ptr_Offset,
                GunioCheckError_Ptr_Offset,
                GunioGetSerialErrorCount_Ptr_Offset,
                GunioGetVer_Ptr_Offset,
                GunioDipswData_Ptr_Offset
            }, BitConverter.GetBytes(_Custom_FalseStub_Address));

            ReplaceReadOnlyString(GameProcess_ModuleBaseAddress, new UInt32[] { GunioBootStatus_Ptr_Offset }, BitConverter.GetBytes(_Custom_TrueStub_Address));

            ReplaceReadOnlyString(GameProcess_ModuleBaseAddress, new UInt32[] { GunioReadData_Ptr_Offset }, BitConverter.GetBytes(CreateCustomIogunReadData()));                              
            ReplaceReadOnlyString(GameProcess_ModuleBaseAddress, new UInt32[] { GunioGetLedcheckData_Ptr_Offset }, BitConverter.GetBytes(CreateCustomGetLed())); 
        }

        /// <summary>
        /// Replace the original iob_get_digital_in() function by a custom one copying the values from a custom storage memory
        /// </summary>
        private UInt32 CreateCustomIobDigitalData()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //push esi
            CaveMemory.Write_StrBytes("56");
            //push edi
            CaveMemory.Write_StrBytes("57");
            //push ecx
            CaveMemory.Write_StrBytes("51");
            //mov esi,_IobDigitalIn_CaveAddress
            CaveMemory.Write_StrBytes("BE");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_IobDigitalIn_CaveAddress));
            //mov edi,[esp+10]
            CaveMemory.Write_StrBytes("8B 7C 24 10");
            //mov ecx,0000000B
            CaveMemory.Write_StrBytes("B9 0B 00 00 00");
            //repe movsd
            CaveMemory.Write_StrBytes("F3 A5");
            //pop ecx
            CaveMemory.Write_StrBytes("59");
            //pop edi
            CaveMemory.Write_StrBytes("5F");
            //pop esi
            CaveMemory.Write_StrBytes("5E");
            //ret
            CaveMemory.Write_StrBytes("C3");

            return CaveMemory.CaveAddress;
        }

        /// <summary>
        /// Replace the original guinio_read_data() function by a custom one copying the values from a custom storage memory
        /// </summary>
        private UInt32 CreateCustomIogunReadData()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //push esi
            CaveMemory.Write_StrBytes("56");
            //push edi
            CaveMemory.Write_StrBytes("57");
            //push ecx
            CaveMemory.Write_StrBytes("51");
            //mov esi,[esp+10]
            CaveMemory.Write_StrBytes("8B 74 24 10");
            //shl esi,05
            CaveMemory.Write_StrBytes("C1 E6 05");
            //add esi,_GunioInData_CaveAddress
            CaveMemory.Write_StrBytes("81 C6");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_GunioInData_CaveAddress));
            //mov edi,[esp+14]
            CaveMemory.Write_StrBytes("8B 7C 24 14");
            //mov ecx,00000005
            CaveMemory.Write_StrBytes("B9 05 00 00 00");
            //repe movsd
            CaveMemory.Write_StrBytes("F3 A5");
            //pop ecx
            CaveMemory.Write_StrBytes("59");
            //pop edi
            CaveMemory.Write_StrBytes("5F");
            //pop esi
            CaveMemory.Write_StrBytes("5E");
            //ret
            CaveMemory.Write_StrBytes("C3");

            return CaveMemory.CaveAddress;
        }

        /// <summary>
        /// Lower bits are 1 for each LED
        /// </summary>
        private UInt32 CreateCustomGetLed()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);
            //mov [esp+08],0000003F
            CaveMemory.Write_StrBytes("C7 44 24 08 3F 00 00 00");
            //ret
            CaveMemory.Write_StrBytes("C3");

            return CaveMemory.CaveAddress;
        }

        private void CreateVoidStub()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);
            //ret
            CaveMemory.Write_StrBytes("C3");

            _Custom_VoidStub_Address = CaveMemory.CaveAddress;
        }
        private void CreateTrueStub()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);
            //mov eax, 1
            CaveMemory.Write_StrBytes("B8 01 00 00 00");
            //ret
            CaveMemory.Write_StrBytes("C3");

            _Custom_TrueStub_Address = CaveMemory.CaveAddress;
        }
        private void CreateFalseStub()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);
            
            //xor eax, eax
            CaveMemory.Write_StrBytes("31 C0");
            //ret
            CaveMemory.Write_StrBytes("C3");

            _Custom_FalseStub_Address = CaveMemory.CaveAddress;
        }

        #endregion

        #region Outputs Hack

        protected override void Apply_OutputsMemoryHack()
        {
            Create_OutputsDataBank();
            _Recoil_CaveAddress = OutputsDatabank_Address;
            _BassShaker_CaveAddress = OutputsDatabank_Address + 4;

            ReplaceReadOnlyString(GameProcess_ModuleBaseAddress, new UInt32[] { IobSetGunReaction_Ptr_Offset }, BitConverter.GetBytes(CreateSetGunReaction()));
            ReplaceReadOnlyString(GameProcess_ModuleBaseAddress, new UInt32[] { IobSet2pBassShaker_Ptr_Offset }, BitConverter.GetBytes(CreateSetBassShaker(_BassShaker_CaveAddress + 1)));
            ReplaceReadOnlyString(GameProcess_ModuleBaseAddress, new UInt32[] { IobSet1pBassShaker_Ptr_Offset }, BitConverter.GetBytes(CreateSetBassShaker(_BassShaker_CaveAddress)));

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        private UInt32 CreateSetGunReaction()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //mov eax, [esp+4]
            CaveMemory.Write_StrBytes("8B 44 24 04");
            //add eax, OutputBaseAddress
            CaveMemory.Write_StrBytes("05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Recoil_CaveAddress));
            //mov byte ptr[eax], 1
            CaveMemory.Write_StrBytes("C6 00 01");
            //xor eax, eax
            CaveMemory.Write_StrBytes("31 C0");
            //ret
            CaveMemory.Write_StrBytes("C3");

            return CaveMemory.CaveAddress;
        }

        private UInt32 CreateSetBassShaker(UInt32 FlagAddress)
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //mov byte ptr[FlagAddress], 1
            CaveMemory.Write_StrBytes("C6 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(FlagAddress));
            CaveMemory.Write_StrBytes("01");
            //xor eax, eax
            CaveMemory.Write_StrBytes("31 C0");
            //ret
            CaveMemory.Write_StrBytes("C3");

            return CaveMemory.CaveAddress;
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
                WriteByte(_IobDigitalIn_CaveAddress + (uint)ButtonsSlot.CoinMech, 0x01);
                WriteByte(ReadPtr(GameProcess_ModuleBaseAddress + _CreditsToAdd_Ptr_Offset) + 0xE510, 0x01);
            }
            else if (e.ButtonName.Equals("SERVICE"))
            {
                WriteByte(_IobDigitalIn_CaveAddress + (uint)ButtonsSlot.Service, 0x01);
            }
            else if (e.ButtonName.Equals("TEST"))
            {
                WriteByte(_IobDigitalIn_CaveAddress + (uint)ButtonsSlot.Test, 0x01);
            }
        }

        protected override void GameController_OnButtonUp(object sender, VirtualGameControler.ButtonDownEventArgs e)
        {
            if (e.ButtonName.Equals("COIN"))
            {
                WriteByte(_IobDigitalIn_CaveAddress + (uint)ButtonsSlot.CoinMech, 0x00);
            }
            else if (e.ButtonName.Equals("SERVICE"))
            {
                WriteByte(_IobDigitalIn_CaveAddress + (uint)ButtonsSlot.Service, 0x00);
            }
            else if (e.ButtonName.Equals("TEST"))
            {
                WriteByte(_IobDigitalIn_CaveAddress + (uint)ButtonsSlot.Test, 0x00);
            }
        }

        protected override void UpdateAxis(PlayerID Id, int X, int Y)
        {
            if (Id != PlayerID.None)
            {
                if (!_GameConfig.GetBoolOption(PEDALMODE_OPTION))
                {
                    //Shield action is originally done by pointing the gun out of the screen, making coordinates go below 0.0 of past 640.0/480.0
                    //With computer lightguns, tracking usually does not report out of boundaries so changing the behavior so that border of the screen will trigger the shield
                    if (X < 5 || X > 635 || Y < 5 || Y > 475)
                        WriteByte(_GunioInData_CaveAddress + 0x08 + ((uint)Id * 0x20), 0x01);
                    else
                        WriteByte(_GunioInData_CaveAddress + 0x08 + ((uint)Id * 0x20), 0x00);
                }

                byte[] bufferX = BitConverter.GetBytes((float)X);
                byte[] bufferY = BitConverter.GetBytes((float)Y);
                WriteBytes(_GunioInData_CaveAddress + ((uint)Id * 0x20), bufferX);
                WriteBytes(_GunioInData_CaveAddress + 0x04 + ((uint)Id * 0x20), bufferY);

                //This is just used to display Hex positionning in TEST mode
                float fMenuX = 255.0f * X / 640.0f;
                float fMenuY = 255.0f * Y / 480.0f;
                WriteBytes(_GunioInData_CaveAddress + 0x0C + ((uint)Id * 0x20), BitConverter.GetBytes((UInt32)fMenuX));
                WriteBytes(_GunioInData_CaveAddress + 0x10 + ((uint)Id * 0x20), BitConverter.GetBytes((UInt32)fMenuY));
            }
        }

        protected override void PlayerController_OnButtonDown(object sender, VirtualGameControler.ButtonDownEventArgs e)
        {
            if (e.Id != PlayerID.None)
            {
                UInt32 PlayerIndex = (UInt32)e.Id;

                if (e.ButtonName.Equals("TRIGGER"))
                {
                    WriteByte(_IobDigitalIn_CaveAddress + (uint)ButtonsSlot.P1_Trigger + PlayerIndex, 0x10);
                    SetButtonBistable(_IobDigitalIn_CaveAddress, ButtonsSlot.P1_Trigger + PlayerIndex);
                }
                else if (e.ButtonName.Equals("OPTION"))
                {
                    WriteByte(_IobDigitalIn_CaveAddress + (uint)ButtonsSlot.P1_Option + PlayerIndex, 0x10);
                    SetButtonBistable(_IobDigitalIn_CaveAddress, ButtonsSlot.P1_Option + PlayerIndex);
                }
                else if (e.ButtonName.Equals("START"))
                {
                    WriteByte(_IobDigitalIn_CaveAddress + (uint)ButtonsSlot.P1_Start + PlayerIndex, 0x10);
                    SetButtonBistable(_IobDigitalIn_CaveAddress, ButtonsSlot.P1_Start + PlayerIndex);
                }
                else if (e.ButtonName.Equals("PEDAL"))
                {
                    //Only activating OUT OF SCREEN by pedal during gameplay to keep gun active during menus
                    if (_GameConfig.GetBoolOption(PEDALMODE_OPTION))
                    {
                        if (ReadByte(GameProcess_ModuleBaseAddress + _GameScene_Offset) == 17)
                        {
                            if (ReadByte(GameProcess_ModuleBaseAddress + _Player1_Playing + (4 * PlayerIndex)) == 1)
                            {
                                WriteByte(_GunioInData_CaveAddress + 0x08 + (PlayerIndex * 0x20), 0x00);
                            }
                        }
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
                    WriteByte(_IobDigitalIn_CaveAddress + (uint)ButtonsSlot.P1_Trigger + PlayerIndex, 0x00);                    
                }
                else if (e.ButtonName.Equals("OPTION"))
                {
                    WriteByte(_IobDigitalIn_CaveAddress + (uint)ButtonsSlot.P1_Option + PlayerIndex, 0x00);
                }
                else if (e.ButtonName.Equals("START"))
                {
                    WriteByte(_IobDigitalIn_CaveAddress + (uint)ButtonsSlot.P1_Start + PlayerIndex, 0x00);
                }
                else if (e.ButtonName.Equals("PEDAL"))
                {
                    //Only activating OUT OF SCREEN by pedal during gameplay to keep gun active during menus
                    if (_GameConfig.GetBoolOption(PEDALMODE_OPTION))
                    {
                        if (ReadByte(GameProcess_ModuleBaseAddress + _GameScene_Offset) == 17)
                        {
                            if (ReadByte(GameProcess_ModuleBaseAddress + _Player1_Playing + (4 * PlayerIndex)) == 1)
                            {
                                WriteByte(_GunioInData_CaveAddress + 0x08 + (PlayerIndex * 0x20), 0x01);
                            }
                        }
                    }
                }
            }
        }

        //On this game a button event changes the byte value (we can do it by cycling between 1/0)
        private void SetButtonBistable(UInt32 DataBaseAddress, ButtonsSlot ButtonOffset)
        {
            byte ButtonValue = ReadByte(DataBaseAddress + (uint)ButtonOffset + 6);
            WriteByte(DataBaseAddress + (uint)ButtonOffset + 6, (byte)(1 - ButtonValue));
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
                new GameOutput(OutputId.P1_LmpHead),
                new GameOutput(OutputId.P2_LmpHead),
                new GameOutput(OutputId.P1_LmpFoot),
                new GameOutput(OutputId.P2_LmpFoot),
                new GameOutput(OutputId.P1_LmpFront),
                new GameOutput(OutputId.P2_LmpFront),
                new GameOutput(OutputId.P1_Ammo),
                new GameOutput(OutputId.P2_Ammo),
                new AsyncGameOutput(OutputId.P1_CtmRecoil, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_ON_DELAY), ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_OFF_DELAY), 0),
                new AsyncGameOutput(OutputId.P2_CtmRecoil, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_ON_DELAY), ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_OFF_DELAY), 0),
                new AsyncGameOutput(OutputId.P1_BassShaker, 200, 100, 0),
                new AsyncGameOutput(OutputId.P2_BassShaker, 200, 100, 0),
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
            UInt32 BaseData_Address = ReadPtr(GameProcess_ModuleBaseAddress + _BaseData_PtrOffset);
            SetOutputValue(OutputId.P1_LmpStart, (ReadByte(BaseData_Address + _Outputs_Offset) >> 1) & 0x01);
            SetOutputValue(OutputId.P2_LmpStart, ReadByte(BaseData_Address + _Outputs_Offset) & 0x01);
            SetOutputValue(OutputId.P1_LmpHead, (ReadByte(BaseData_Address + _Outputs_Offset + 1) >> 1) & 0x01);
            SetOutputValue(OutputId.P2_LmpHead, ReadByte(BaseData_Address + _Outputs_Offset + 1) & 0x01);
            SetOutputValue(OutputId.P1_LmpFoot, ReadByte(BaseData_Address + _Outputs_Offset + 2));
            SetOutputValue(OutputId.P2_LmpFoot, ReadByte(BaseData_Address + _Outputs_Offset + 3));
            SetOutputValue(OutputId.P1_LmpFront, ReadByte(BaseData_Address + _Outputs_Offset + 12));
            SetOutputValue(OutputId.P2_LmpFront, ReadByte(BaseData_Address + _Outputs_Offset + 13));

            for (uint i = 0; i < _GameConfig.PlayerCount; i++)
            {
                if (ReadByte(_BassShaker_CaveAddress + i) == 1)
                {
                    SetOutputValue(OutputId.P1_BassShaker + i, 1);
                    WriteByte(_BassShaker_CaveAddress + i, 0);
                }

                if (ReadByte(_Recoil_CaveAddress + i) == 1)
                {
                    SetOutputValue(OutputId.P1_CtmRecoil + i, 1);
                    WriteByte(_Recoil_CaveAddress + i, 0);
                }

                _Life[i] = 0;
                _Ammo[i] = 0;

                //To be sure that outputs are not activated in Attract Mode, we can check the game scene ID and activate them only in gameplay 
                if (ReadByte(GameProcess_ModuleBaseAddress + _GameScene_Offset) == 17)
                {
                    //Now, players data have some information even if they are not playing (game over or not started)
                    if (ReadByte(GameProcess_ModuleBaseAddress + _Player1_Playing + (4 * i)) == 1)
                    {
                        UInt32 PlayerInfoAddress = ReadPtr(GameProcess_ModuleBaseAddress + _PlayerInfo_BasePtr_Offset + 0x38C + (4 * i));
                        _Weapon[i] = (int)ReadPtrChain(PlayerInfoAddress + 0x40, new UInt32[] { 0x15C, 0x24 });

                        if (_Weapon[i] == _LastWeapon[i])
                        {
                            _Ammo[i] = (int)ReadPtrChain(PlayerInfoAddress + 0x40, new UInt32[] { 0x15C, (UInt32)(4 * _Weapon[i]), 0xE8 });

                            //Custom Recoil
                            //if (_Ammo[i] < _LastAmmo[i])
                            //    SetOutputValue(OutputId.P1_CtmRecoil + i, 1);
                        }

                        _Life[i] = (int)ReadPtr(PlayerInfoAddress + 0x6C);

                        //Custom Dammage
                        if (_Life[i] < _LastLife[i])
                            SetOutputValue(OutputId.P1_Damaged, 1);
                    }
                }

                _LastLife[i] = _Life[i];
                _LastAmmo[i] = _Ammo[i];
                _LastWeapon[i] = _Weapon[i];
                SetOutputValue(OutputId.P1_Life + i, _Life[i]);
                SetOutputValue(OutputId.P1_Ammo + i, _Ammo[i]);
            }

            SetOutputValue(OutputId.Credits, ReadByte(GameProcess_ModuleBaseAddress + _CreditsTotal_Offset));
        }

        //Seems like there's a memory field to set Lamp to state 0/1/2 and another one [0->0x64] to set analog intensity
        private byte GetLampAnalogValue(UInt32 OutputLampAddress)
        {
            if (ReadByte(OutputLampAddress) != 0)
            {
                return (ReadByte(OutputLampAddress - 0x30));
            }
            else
                return 0;
        }

        #endregion
    }
}
