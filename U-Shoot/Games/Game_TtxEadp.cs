using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using UsCore.Config;
using UsCore.Launcher;
using UsCore.Logging;
using UsCore.MameOutput;
using UsCore.Memory;
using UsCore.VirtualControllers;

namespace U_Shoot.Games
{
    public class Game_TtxEadp : BaseGame32
    {
        protected override bool PATCH_PEB_DEBUG_FLAG { get { return false; } }

        private const string LOCAL_SAVE_OPTION = "Local Save";

        private const string JCONFIGDLL_NAME = "JVSEmuEA.dll";
        private UInt32 _JConfigDll_BaseAddress;

        #region LOADER MEMORY ADDRESSES    
        private UInt32 _AdaptersCount_Offset = 0x0020DCD8;
        private UInt32 _ScreenMode_Patch_Offset = 0x00001FF4;
        private UInt32 _WindowedWidth_Offset = 0x1F5110;
        private UInt32 _WindowedHeight_Offset = 0x1F5114;
        private UInt32 _HardcodedPath1_Patch_Offset = 0x0001064E5;
        private UInt32 _HardcodedPath2_Patch_Offset = 0x0001064CE;
        private UInt32 _HardcodedPath3_Patch_Offset = 0x0001064BE;
        private UInt32 _HardcodedPath4_Patch_Offset = 0x0001064A6;
        private UInt32 _HardcodedPath5_Patch_Offset = 0x000101359;
        private UInt32 _D3d_Patch_Offset = 0x00005FAC;
        private InjectionStruct32 _ScreenDetection_InjectionStruct = new InjectionStruct32(0x00005C76, 6);
        private InjectionStruct32 _D3dPresent_InjectionStruct = new InjectionStruct32(0x000016FC, 5);
        private InjectionStruct32 _NoCursor_InjectionStruct = new InjectionStruct32(0x000021F5, 6);

        //Custom Data
        private UInt32 _D3dPresentSrc_CaveAddress;
        private UInt32 _D3dPresentDst_CaveAddress;
        private UInt32 _NvramTxt1_CaveAddress;
        private UInt32 _NvramTxt2_CaveAddress;
        private UInt32 _NvramTxt3_CaveAddress;
        private UInt32 _NvramTxt4_CaveAddress;
        private UInt32 _NvramTxt5_CaveAddress;
        #endregion

        #region INPUTS MEMORY ADDRESSES    
        private UInt32 _EscClose_Patch_Offset = 0x000020F1;
        private UInt32 _InitialCall_Offset = 0x00142226;
        private UInt32 _LoadLibraryA_Ptr_Offset = 0x0018F138;
        private UInt32 _JConfig_SettingsBuffer_Offset = 0x0003E780;
        private UInt32 _JConfig_InputsBuffer_Offset = 0x0003E8B4;
        private InjectionStruct32 _BootLoader_InjectionStruct = new InjectionStruct32(0x00133EB9, 5);
        private InjectionStruct32 _JconfigSettings_InjectionStruct = new InjectionStruct32(0x00001394, 5);
        private InjectionStruct32 _JconfigInputs_InjectionStruct = new InjectionStruct32(0x00006F84, 5);
        private InjectionStruct32 _GunInputs_InjectionStruct = new InjectionStruct32(0x0014F53E, 6);
        private InjectionStruct32 _Inputs_InjectionStruct = new InjectionStruct32(0x000C5E1F, 8);
        private InjectionStruct32 _CoinCredit_InjectionStruct = new InjectionStruct32(0x000C44B0, 7);
        //private InjectionStruct32 _ServiceCredit_InjectionStruct = new InjectionStruct32(0x000C44B0, 7);
        private BreakpointStruct32 _JConfigUnpacked_Breakpointstruct = new BreakpointStruct32(0x4A4ED, 5);

        //custom Data
        private UInt32 _JConfigDll_Path_CaveAddress;
        private UInt32 _JConfigSettingsData_CaveAddress;
        private UInt32 _JConfigInputsData_CaveAddress;
        private UInt32 _GunInputs_CaveAddress;
        private UInt32 _Inputs_CaveAddress;
        private UInt32 _CoinFlag_CaveAddress;
        private UInt32 _ServiceFlag_CaveAddress;
        #endregion

        #region OUTPUTS MEMORY ADDRESSES
        private UInt32 _Recoil_Call_Offset = 0x000C56E0;
        private UInt32 _PlayersStatus_Ptr_Offset = 0x00212CD4;
        private UInt32 _PlayersPlaying_Ptr_Offset = 0x00210A80;
        private UInt32 _Outputs_Base_Offset = 0x00204EBB;
        private InjectionStruct32 _Recoil_InjectionStruct =  new InjectionStruct32(0x0003ED23, 5);
        private InjectionStruct32 _Damage_InjectionStruct = new InjectionStruct32(0x0006E29A, 5);

        //Custom Data
        private UInt32 _RecoilStatus_CaveAddress = 0;
        private UInt32 _DamageStatus_CaveAddress = 0;
        #endregion

        public Game_TtxEadp(GameConfig Config) : base(Config)
        {
            _KnownMd5Prints.Add("Elevator Action Dead Parade 1.03 USA - Original", "59dbaf264bb96323cb3127dee0f809ca");
            _KnownMd5Prints.Add("Elevator Action Dead Parade 1.03 USA - For JConfig", "1bc36915ac4a4658feeca80ca1b6ca10");
            _GameLauncher.DisableDebugger = true;
        }

        protected override void AttachDebuggerEvents()
        {
            if (!_GameConfig.NoInputs)
                _GameLauncher.OnLoadedDll += LoadDllEvent;
        }

        #region Debugger Events

        #region EXCEPTION_DEBUG_EVENT

        protected override void ExceptionEvent(object sender, x86_x64_Debugger.ExceptionDebugEventArgs e)
        {
            x86_x64_Debugger Dbg = (x86_x64_Debugger)sender;
            uint dwThreadId = e.dwThreadId;
            
            //When started by X64 process, X86 target process run in Wow64 emulator for X86 and Exceptions events are not the same :
            //STATUS_BREAKPOINT is now replaced by STATUS_WX86_BREAKPOINT
            if (e.ExceptionInfo.ExceptionRecord.ExceptionCode == Win32.STATUS_WX86_BREAKPOINT)
            {
                Logger.WriteLog(" > ExceptionCode: STATUS_WX86_BREAKPOINT");
                Logger.WriteLog("ExceptionAddress = 0x" + e.ExceptionInfo.ExceptionRecord.ExceptionAddress.ToString(Dbg.HexStringFormat));

                if (_IsFirstException)
                {
                    Logger.WriteLog(" > Initital Breakpoint");
                    Apply_MemoryHacks();
                    _IsFirstException = false;
                }
                else
                {
                    if ((UInt32)e.ExceptionInfo.ExceptionRecord.ExceptionAddress == _JConfigDll_BaseAddress + _JConfigUnpacked_Breakpointstruct.MemoryOffset)
                    {
                        Logger.WriteLog(" > JVSEmuEA.dll unpacked breakpoint reached");
                        if (!RemoveBreakpoint(_JConfigUnpacked_Breakpointstruct, e.dwThreadId, _JConfigDll_BaseAddress))
                            return;

                        e.Handled = true;
                        SetHack_JConfig();
                        RaiseGameHookedEvent();
                    }
                }
            }
        }

        #endregion

        #region LOAD_DLL_DEBUG_EVENT

        protected override void LoadDllEvent(object sender, x86_x64_Debugger.LoadDllDebugEventArgs e)
        {
            x86_x64_Debugger Dbg = (x86_x64_Debugger)sender;
            IntPtr lpBaseOfDllLoad = e.LoadDllInfo.lpBaseOfDll;
            
            if (lpBaseOfDllLoad != IntPtr.Zero)
            {               
                if (string.Equals(Path.GetFileName(e.DllPath), JCONFIGDLL_NAME, StringComparison.OrdinalIgnoreCase))
                {
                    Logger.WriteLog("==> Wanted DLL found !");
                    //HANDLE should always be in 32Bits range (https://learn.microsoft.com/fr-fr/windows/win32/winprog64/interprocess-communication?redirectedfrom=MSDN)
                    Logger.WriteLog("  + hModule: 0x" + e.LoadDllInfo.hFile.ToString("X8"));
                    Logger.WriteLog("  + fUnicode: " + e.LoadDllInfo.fUnicode.ToString());

                    //At that point, the loaded dll has only the UPX stub that will unpack the original code
                    //Setting a breakpoint at the end of the unpacking process will allow to access original code bytes and mod it
                    _JConfigDll_BaseAddress = (UInt32)e.LoadDllInfo.lpBaseOfDll;
                    SetBreakpoint(_JConfigUnpacked_Breakpointstruct, _JConfigDll_BaseAddress);
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
                    double dMaxX = 1023.0;
                    double dMaxY = 1023.0;
                    double dRangeX = dMaxX - dMinX + 1;
                    double dRangeY = dMaxY - dMinY + 1;

                    PlayerController.Computed_X = Convert.ToInt32(Math.Round(dRangeX * PlayerController.Computed_X / TotalResX));
                    PlayerController.Computed_Y = Convert.ToInt32(Math.Round(dMaxY - (dRangeY * PlayerController.Computed_Y / TotalResY)));
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
            //GameHooked event will be raised after a Breakpoint, once the DLL is fully loaded
            base.Apply_MemoryHacks();
        }

        #region Loader Hack

        protected override void Apply_LoaderMemoryHack()
        {
            Create_LoaderDataBank();
            _D3dPresentSrc_CaveAddress = LoaderDatabank_Address;        //16 bytes (rect struct)
            _D3dPresentDst_CaveAddress = LoaderDatabank_Address + 0x10; //16 bytes (rect struct)
            _NvramTxt1_CaveAddress = LoaderDatabank_Address + 0x20;
            _NvramTxt2_CaveAddress = LoaderDatabank_Address + 0x30;
            _NvramTxt3_CaveAddress = LoaderDatabank_Address + 0x40;
            _NvramTxt4_CaveAddress = LoaderDatabank_Address + 0x50;
            _NvramTxt5_CaveAddress = LoaderDatabank_Address + 0x60;

            //At start the game is calling GetDeviceCaps() and look for the number of available adapters.
            //If 2, it will try to create a second window and this is buggy, so forcing it to 1
            SetHack_GetDeviceCaps();

            //Default behaviour is Fullscreen (game.exe + 0x210A33 = 1)
            if (_GameConfig.Windowed)
            {
                WriteByte(GameProcess_ModuleBaseAddress + _ScreenMode_Patch_Offset, 0x00);
                WriteBytes(GameProcess_ModuleBaseAddress + _WindowedWidth_Offset, BitConverter.GetBytes(720));
                WriteBytes(GameProcess_ModuleBaseAddress + _WindowedHeight_Offset, BitConverter.GetBytes(1280));
            }
            else
            {
                /** 1st Step for fullscreen : **/
                //For fullscreen, the goal is to keep the full sized Window created by CreateWindowEx and change some D3D Presentation Parameters to make it draw
                //the original 720x1280 viewport on it
                //So:
                //mov[Game.exe + 1F52B0],00000001   <- WINDOWED = TRUE
                //mov[Game.exe + 1F5298],eax
                //mov[Game.exe + 1F5290],000002D0   <- BAckBufferWidth = 720
                //mov[Game.exe + 1F5294],00000500   <- BackbufferHeight = 1280
                //nop                               <- RefreshRate stays 0 (instead of 59 for FULLSCREEN)
                //nop
                //nop
                WriteBytes(GameProcess_ModuleBaseAddress + _D3d_Patch_Offset, new byte[] { 0xC7, 0x05, 0xB0, 0x52, 0x5F, 0x00, 0x01, 0x00, 0x00, 0x00, 0x89, 0x05, 0x98, 0x52, 0x5F, 0x00, 0xC7, 0x05, 0x90, 0x52, 0x5F, 0x00, 0xD0, 0x02, 0x00, 0x00, 0xC7, 0x05, 0x94, 0x52, 0x5F, 0x00, 0x00, 0x05, 0x00, 0x00, 0x90, 0x90, 0x90 });

                /** 2nd Step for Fullscren **/
                //Once it gets painted in the canvas, editing the Present() call parameters to scale the drawing to a normaly scaled zone in the middle of the Window

                //Changing D3DSWAPEFFECT_DISCARD to D3DSWAPEFFECT_COPY
                WriteByte(GameProcess_ModuleBaseAddress + _D3d_Patch_Offset + 0x43, 0x03);

                //Src rect is original drawing canvas (0,0,720,1280)
                WriteBytes(_D3dPresentSrc_CaveAddress, BitConverter.GetBytes((UInt32)0));
                WriteBytes(_D3dPresentSrc_CaveAddress + 0x04, BitConverter.GetBytes((UInt32)0));
                WriteBytes(_D3dPresentSrc_CaveAddress + 0x08, BitConverter.GetBytes((UInt32)720));
                WriteBytes(_D3dPresentSrc_CaveAddress + 0x0C, BitConverter.GetBytes((UInt32)1280));

                //Dst Canvas needs a bit of calculation
                float fScale = _DesktopScreenHeight / 1280.0f;
                float DstWidth = 720.0f * fScale;
                float fXoffset = (_DesktopScreenWidth - DstWidth) / 2.0f;
                WriteBytes(_D3dPresentDst_CaveAddress, BitConverter.GetBytes((UInt32)fXoffset));
                WriteBytes(_D3dPresentDst_CaveAddress + 0x04, BitConverter.GetBytes((UInt32)0));
                WriteBytes(_D3dPresentDst_CaveAddress + 0x08, BitConverter.GetBytes((UInt32)(fXoffset + DstWidth)));
                WriteBytes(_D3dPresentDst_CaveAddress + 0x0C, BitConverter.GetBytes((UInt32)1080));

                SetHack_D3dPresent();
            }

            if (_GameConfig.NoCursor)
            {
                if (CreateTransparentCursor())
                {
                    SetHack_NoCursor();
                }
            }

            //Replacing use of hardcoded D:\**** strings
            // >-- Used if Standalone, but JConfig does save it in ./sv so it is not used for now --<
            if (_GameConfig.GetBoolOption(LOCAL_SAVE_OPTION))
            {                
                WriteBytes(_NvramTxt1_CaveAddress, Encoding.ASCII.GetBytes(@".\NVRAM\%s"));
                WriteBytes(_NvramTxt2_CaveAddress, Encoding.ASCII.GetBytes(@".\NVRAM\%s/*"));
                WriteBytes(_NvramTxt3_CaveAddress, Encoding.ASCII.GetBytes(@".\NVRAM\%s/%s"));
                WriteBytes(_NvramTxt4_CaveAddress, Encoding.ASCII.GetBytes(@".\NVRAM\%s/%s/*"));
                WriteBytes(_NvramTxt5_CaveAddress, Encoding.ASCII.GetBytes(@".\NVRAM\%s%04d%02d%02d_%02d%02d%02d_"));
                WriteBytes(GameProcess_ModuleBaseAddress + _HardcodedPath1_Patch_Offset, BitConverter.GetBytes(_NvramTxt1_CaveAddress));
                WriteBytes(GameProcess_ModuleBaseAddress + _HardcodedPath2_Patch_Offset, BitConverter.GetBytes(_NvramTxt2_CaveAddress));
                WriteBytes(GameProcess_ModuleBaseAddress + _HardcodedPath3_Patch_Offset, BitConverter.GetBytes(_NvramTxt3_CaveAddress));
                WriteBytes(GameProcess_ModuleBaseAddress + _HardcodedPath4_Patch_Offset, BitConverter.GetBytes(_NvramTxt4_CaveAddress));
                WriteBytes(GameProcess_ModuleBaseAddress + _HardcodedPath5_Patch_Offset, BitConverter.GetBytes(_NvramTxt5_CaveAddress));
            }

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        /// <summary>
        /// Replacing the values pushed after LoadCursor() call by a custom Handle
        /// Note: replacing eax directly without doing the LoadCursor() call make game crash 
        /// </summary>
        private void SetHack_NoCursor()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //push 04
            CaveMemory.Write_StrBytes("6A 04");
            //mov [esp+44], _CustomTransparentCursorHandle
            CaveMemory.Write_StrBytes("C7 44 24 44");
            CaveMemory.Write_Bytes(BitConverter.GetBytes((UInt32)_CustomTransparentCursorHandle));

            //Inject it
            CaveMemory.InjectToOffset(_NoCursor_InjectionStruct, "No Cursor");
        }

        /// <summary>
        /// Forcing detected adapter count to 1 after call to GetDeviceCaps()
        /// </summary>
        private void SetHack_GetDeviceCaps()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //mov dword ptr[game.exe + _AdaptersCount_Offset], 01
            CaveMemory.Write_StrBytes("C7 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _AdaptersCount_Offset));
            CaveMemory.Write_StrBytes("01 00 00 00");
            //mov edi, game.exe + _AdaptersCount_Offset
            CaveMemory.Write_StrBytes("8B 3D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _AdaptersCount_Offset));

            //Inject it
            CaveMemory.InjectToOffset(_ScreenDetection_InjectionStruct, "Adapter Count");

        }

        /// <summary>
        /// Present() is originally called with (0,0,0,0) parameters.
        /// Pushing custom RECT struct will allow to draw original size viewport in a custom zone on horizontal screen
        /// </summary>
        private void SetHack_D3dPresent()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //push ebx
            CaveMemory.Write_StrBytes("53");
            //push ebx
            CaveMemory.Write_StrBytes("53");
            //push _D3dPresentDst_CaveAddress
            CaveMemory.Write_StrBytes("68");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_D3dPresentDst_CaveAddress));
            //push _D3dPresentSrc_CaveAddress
            CaveMemory.Write_StrBytes("68");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_D3dPresentSrc_CaveAddress));
            //push esi
            CaveMemory.Write_StrBytes("56");

            //Inject it
            CaveMemory.InjectToOffset(_D3dPresent_InjectionStruct, "D3D Present()");
        }

        #endregion        

        #region Inputs Hack

        protected override void Apply_InputsMemoryHack()
        {
            Create_InputsDataBank();
            _GunInputs_CaveAddress = InputsDatabank_Address;                    //10 Bytes
            _Inputs_CaveAddress = InputsDatabank_Address + 0x10;                //4 Bytes
            _CoinFlag_CaveAddress = InputsDatabank_Address + 0x18;
            _ServiceFlag_CaveAddress = InputsDatabank_Address + 0x19;
            _JConfigDll_Path_CaveAddress = InputsDatabank_Address + 0x20;       //char[255]
            _JConfigSettingsData_CaveAddress = InputsDatabank_Address + 0x120;  //244 Bytes
            _JConfigInputsData_CaveAddress = InputsDatabank_Address + 0x220;    //408 Bytes

            //Writing the DLL path to load it with LoadLibraryA();
            string JconfigDllPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Properties.Resources.DATA_PAYLOADS_FOLDER, "JConfig", JCONFIGDLL_NAME);
            WriteBytes(_JConfigDll_Path_CaveAddress, Encoding.ASCII.GetBytes(JconfigDllPath));

            //Fedding the content of ring_config.bin directly to the DLL, removing the need of the file
            //This is almost "no options activated at all" config for JConfig.
            WriteBytes(_JConfigSettingsData_CaveAddress, new byte[]{
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x3C, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xD0, 0x02, 0x00, 0x00, 0x00, 0x05, 0x00, 0x00, 0xD0, 0x02, 0x00, 0x00, 0x00, 0x05, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x64, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
            });

            SetHack_Bootloader();

            //ESC key closes game, if U-Shoot handles input, skipping it so that only desired U-Shoot key is working
            WriteBytes(GameProcess_ModuleBaseAddress + _EscClose_Patch_Offset, new byte[] { 0xE9, 0xA1, 0x00, 0x00, 0x00, 0x90 });

            SetHack_Inputs();
            SetHack_GunInputs();
            SetHack_CoinCredits();

            //In full screen, the game wull be displayed in a vertical centered Viewport, so Axis must be adapted by forcing the _XscaleFactor
            if (_GameConfig.Fullscreen)
            {
                double dScale = _DesktopScreenHeight / (double)1280;
                double dViewportWidth = 720 * dScale;
                _AxisX_ScaleFactor = _DesktopScreenWidth / dViewportWidth;
            }

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        /// <summary>
        /// At .exe entry, adding a small codecave to call LoadLibraryA() with JConfig dll
        /// </summary>
        private void SetHack_Bootloader()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //push _JConfigDll_Path_CaveAddress
            CaveMemory.Write_StrBytes("68");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_JConfigDll_Path_CaveAddress));
            //call _LoadLibraryA();
            CaveMemory.Write_StrBytes("FF 15");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _LoadLibraryA_Ptr_Offset));
            //call InitialCallAddress
            CaveMemory.Write_call(GameProcess_ModuleBaseAddress + _InitialCall_Offset);

            //Inject it
            CaveMemory.InjectToOffset(_BootLoader_InjectionStruct, "Bootloader");
        }

        /// <summary>
        /// Editing Ring_IO.dll so that the data normaally read in the local ring_config.bin is relaced by some data directly by U-Shoot
        /// ring_config has all kind of options for the emulator layer and data is mandatory
        /// </summary>
        private void SetHack_JConfig()
        {
            SetHack_JConfigInputs();
            SetHack_JConfigSettings();
        }

        /// <summary>
        /// Replace reading "monitor_misc.bin" by a custom buffer
        /// </summary>
        private void SetHack_JConfigSettings()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, _JConfigDll_BaseAddress);

            //push edi
            CaveMemory.Write_StrBytes("57");
            //push ecx
            CaveMemory.Write_StrBytes("51");
            //mov esi,_RingConfigData_CaveAddress
            CaveMemory.Write_StrBytes("BE");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_JConfigSettingsData_CaveAddress));
            //mov edi,RingIo.Dll+_RingIo_DataConfigBuffer_Offset
            CaveMemory.Write_StrBytes("BF");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_JConfigDll_BaseAddress + _JConfig_SettingsBuffer_Offset));
            //mov ecx,0000003D
            CaveMemory.Write_StrBytes("B9 3D 00 00 00");
            //repe movsd
            CaveMemory.Write_StrBytes("F3 A5");
            //mov esi,00000000
            CaveMemory.Write_StrBytes("BE 00 00 00 00");
            //pop ecx
            CaveMemory.Write_StrBytes("59");
            //pop edi
            CaveMemory.Write_StrBytes("5F");

            //Inject it
            CaveMemory.InjectToOffset(_JconfigSettings_InjectionStruct, "JConfig Settings");
        }

        /// <summary>
        /// Replace reading "monitor_input.bin" by a custom buffer
        /// </summary>
        private void SetHack_JConfigInputs()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, _JConfigDll_BaseAddress);

            //push edi
            CaveMemory.Write_StrBytes("57");
            //push ecx
            CaveMemory.Write_StrBytes("51");
            //mov esi,_RingConfigData_CaveAddress
            CaveMemory.Write_StrBytes("BE");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_JConfigInputsData_CaveAddress));
            //mov edi,RingIo.Dll+_RingIo_DataConfigBuffer_Offset
            CaveMemory.Write_StrBytes("BF");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_JConfigDll_BaseAddress + _JConfig_InputsBuffer_Offset));
            //mov ecx,00000066
            CaveMemory.Write_StrBytes("B9 66 00 00 00");
            //repe movsd
            CaveMemory.Write_StrBytes("F3 A5");
            //mov esi,00000000
            CaveMemory.Write_StrBytes("BE 00 00 00 00");
            //pop ecx
            CaveMemory.Write_StrBytes("59");
            //pop edi
            CaveMemory.Write_StrBytes("5F");

            //Inject it
            CaveMemory.InjectToOffset(_JconfigInputs_InjectionStruct, "JConfig Settings");
        }

        /// <summary>
        /// Replacing the buffer set from Reading IO Board.
        /// This has only axes and Trigger data
        /// </summary>
        private void SetHack_GunInputs()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //push esi
            CaveMemory.Write_StrBytes("56");
            //push edi
            CaveMemory.Write_StrBytes("57");
            //push ecx
            CaveMemory.Write_StrBytes("51");
            //mov esi,_GunInputs_CaveAddress
            CaveMemory.Write_StrBytes("BE");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_GunInputs_CaveAddress));
            //lea edi,[ebp
            CaveMemory.Write_StrBytes("8D 7D AC");
            //mov ecx,00000003
            CaveMemory.Write_StrBytes("B9 03 00 00 00");
            //repe movsd
            CaveMemory.Write_StrBytes("F3 A5");
            //pop ecx
            CaveMemory.Write_StrBytes("59");
            //pop edi
            CaveMemory.Write_StrBytes("5F");
            //pop esi
            CaveMemory.Write_StrBytes("5E");
            //mov dl,[ebp
            CaveMemory.Write_StrBytes("8A 55 AC");
            //mov [ebp
            CaveMemory.Write_StrBytes("88 55 AB");

            //Inject it
            CaveMemory.InjectToOffset(_GunInputs_InjectionStruct, "Gun Inputs");
        }

        /// <summary>
        /// Ater getting the small key buffer in EAX from a call, replacing the values in EAX
        /// </summary>
        private void SetHack_Inputs()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //mov eax,[_Inputs_CaveAddress]
            CaveMemory.Write_StrBytes("8B 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Inputs_CaveAddress));
            //mov [esi+0C],eax
            CaveMemory.Write_StrBytes("89 46 0C");
            //and eax,FFFF7FE0
            CaveMemory.Write_StrBytes("25 E0 7F FF FF");

            //Inject it
            CaveMemory.InjectToOffset(_Inputs_InjectionStruct, "Inputs");

        }

        /// <summary>
        /// HAcking the switch buffer is not enough to get COIN to work, adding a small flag to force the game adding it when needed
        /// </summary>
        private void SetHack_CoinCredits()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //cmp byte ptr [_CoinFlag_CaveAddress],00
            CaveMemory.Write_StrBytes("80 3D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_CoinFlag_CaveAddress));
            CaveMemory.Write_StrBytes("00");
            //je Next
            CaveMemory.Write_StrBytes("74 0C");
            //mov byte ptr [_CoinFlag_CaveAddress],00
            CaveMemory.Write_StrBytes("C6 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_CoinFlag_CaveAddress));
            CaveMemory.Write_StrBytes("00");
            //mov eax,00000001
            CaveMemory.Write_StrBytes("B8 01 00 00 00");

            //Next:
            //mov edi,eax
            CaveMemory.Write_StrBytes("8B F8");
            //and eax,0000C000
            CaveMemory.Write_StrBytes("25 00 C0 00 00");

            //Injecy It
            CaveMemory.InjectToOffset(_CoinCredit_InjectionStruct, "Coin Credit");
        }

        #endregion

        #region Outputs Hack

        protected override void Apply_OutputsMemoryHack()
        {
            Create_OutputsDataBank();
            _RecoilStatus_CaveAddress = OutputsDatabank_Address;
            _DamageStatus_CaveAddress = OutputsDatabank_Address + 0x04;

            SetHack_Damage();
            SetHack_Recoil();

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        /// <summary>
        /// Code injection where the game is calling for rumble because of damage.
        /// That way we can known when a player is damaged and make our own output.
        /// </summary>
        private void SetHack_Damage()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //mov byte ptr[edx + _DamageStatus_CaveAddress], 1
            CaveMemory.Write_StrBytes("C6 82");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_DamageStatus_CaveAddress));
            CaveMemory.Write_StrBytes("01");
            //push 14
            CaveMemory.Write_StrBytes("6A 14");
            //push 01
            CaveMemory.Write_StrBytes("6A 01");
            //push edx
            CaveMemory.Write_StrBytes("52");

            //Inject it
            CaveMemory.InjectToOffset(_Damage_InjectionStruct, "Damage");
        }

        /// <summary>
        /// Code injection where the game is calling for rumble because of Recoil
        /// That way we can knwo when a bullet is fired and create our own output
        /// </summary>
        private void SetHack_Recoil()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //mov byte ptr[edx + _RecoilStatus_CaveAddress], 1
            CaveMemory.Write_StrBytes("C6 82");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_RecoilStatus_CaveAddress));
            CaveMemory.Write_StrBytes("01");
            //call Game.exe+C56E0
            CaveMemory.Write_call(GameProcess_ModuleBaseAddress + _Recoil_Call_Offset);

            //Inject it
            CaveMemory.InjectToOffset(_Recoil_InjectionStruct, "Recoil");
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
                Apply_OR_ByteMask(_Inputs_CaveAddress, 0x40);
                WriteByte(_CoinFlag_CaveAddress, 0x01);
            }
            else if (e.ButtonName.Equals("SERVICE"))
            {
                Apply_OR_ByteMask(_Inputs_CaveAddress + 2, 0x40);
            }
            else if (e.ButtonName.Equals("TEST"))
            {
                Apply_OR_ByteMask(_Inputs_CaveAddress + 3, 0x80);
            }
            else if (e.ButtonName.Equals("UP"))
            {
                Apply_OR_ByteMask(_Inputs_CaveAddress + 2, 0x20);
            }
            else if (e.ButtonName.Equals("DOWN"))
            {
                Apply_OR_ByteMask(_Inputs_CaveAddress + 2, 0x10);
            }
            else if (e.ButtonName.Equals("ACTION"))
            {
                Apply_OR_ByteMask(_Inputs_CaveAddress + 2, 0x08);
            }
            else if (e.ButtonName.Equals("SELECT"))
            {
                Apply_OR_ByteMask(_Inputs_CaveAddress + 1, 0x08);
            }
            else if (e.ButtonName.Equals("ENTER"))
            {
                Apply_OR_ByteMask(_Inputs_CaveAddress + 1, 0x04);
            }
        }

        protected override void GameController_OnButtonUp(object sender, VirtualGameControler.ButtonDownEventArgs e)
        {
            if (e.ButtonName.Equals("COIN"))
            {
                Apply_AND_ByteMask(_Inputs_CaveAddress, 0xBF);
            }
            else if (e.ButtonName.Equals("SERVICE"))
            {
                Apply_AND_ByteMask(_Inputs_CaveAddress + 2, 0xBF);
            }
            else if (e.ButtonName.Equals("TEST"))
            {
                Apply_AND_ByteMask(_Inputs_CaveAddress + 3, 0x7F);
            }
            else if (e.ButtonName.Equals("UP"))
            {
                Apply_AND_ByteMask(_Inputs_CaveAddress + 2, 0xDF);
            }
            else if (e.ButtonName.Equals("DOWN"))
            {
                Apply_AND_ByteMask(_Inputs_CaveAddress + 2, 0xEF);
            }
            else if (e.ButtonName.Equals("ACTION"))
            {
                Apply_AND_ByteMask(_Inputs_CaveAddress + 2, 0xF7);
            }
            else if (e.ButtonName.Equals("SELECT"))
            {
                Apply_AND_ByteMask(_Inputs_CaveAddress + 1, 0xF7);
            }
            else if (e.ButtonName.Equals("ENTER"))
            {
                Apply_AND_ByteMask(_Inputs_CaveAddress + 1, 0xFB);
            }
        }

        protected override void UpdateAxis(PlayerID Id, int X, int Y)
        {
            if (Id != PlayerID.None)
            {
                byte[] bufferX = BitConverter.GetBytes((UInt16)X);
                byte[] bufferY = BitConverter.GetBytes((UInt16)Y);

                //Axis are encoded as 2 Bytes BigEndian
                Array.Reverse(bufferX);
                Array.Reverse(bufferY);
                WriteBytes(_GunInputs_CaveAddress + (4 * (uint)Id), bufferX);
                WriteBytes(_GunInputs_CaveAddress +  2 + (4 * (uint)Id), bufferY);
            }
        }

        protected override void PlayerController_OnButtonDown(object sender, VirtualGameControler.ButtonDownEventArgs e)
        {
            if (e.Id != PlayerID.None)
            {
                UInt32 PlayerIndex = (UInt32)e.Id;

                if (e.ButtonName.Equals("TRIGGER"))
                {
                    int iMask = 0x20 >> (int)PlayerIndex;
                    Apply_OR_ByteMask(_GunInputs_CaveAddress + 8, (byte)iMask);
                }
                else if (e.ButtonName.Equals("RELOAD"))
                {
                    int iMask = 0x80 >> (int)PlayerIndex;
                    Apply_OR_ByteMask(_GunInputs_CaveAddress + 8, (byte)iMask);
                    iMask = 0x20 >> (int)PlayerIndex;
                    Apply_OR_ByteMask(_GunInputs_CaveAddress + 8, (byte)iMask);
                }
                else if (e.ButtonName.Equals("GRENADE"))
                {
                    int iMask = 0x02 >> (int)PlayerIndex;
                    Apply_OR_ByteMask(_Inputs_CaveAddress + 2, (byte)iMask);
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
                    int iMask = 0x20 >> (int)PlayerIndex;
                    Apply_AND_ByteMask(_GunInputs_CaveAddress + 8, (byte)~iMask);
                }
                else if (e.ButtonName.Equals("RELOAD"))
                {
                    int iMask = 0x80 >> (int)PlayerIndex;
                    Apply_AND_ByteMask(_GunInputs_CaveAddress + 8, (byte)~iMask);
                    iMask = 0x20 >> (int)PlayerIndex;
                    Apply_AND_ByteMask(_GunInputs_CaveAddress + 8, (byte)~iMask);
                }
                else if (e.ButtonName.Equals("GRENADE"))
                {
                    int iMask = 0x02 >> (int)PlayerIndex;
                    Apply_AND_ByteMask(_Inputs_CaveAddress + 2, (byte)~iMask);
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
                new GameOutput(OutputId.LmpSide_R),
                new GameOutput(OutputId.LmpSide_G),
                new GameOutput(OutputId.LmpSide_B),
                new GameOutput(OutputId.LmpUpBtn),
                new GameOutput(OutputId.LmpDownBtn),
                new GameOutput(OutputId.LmpCloseBtn),
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
            };
        }

        /// <summary>
        /// Update all Outputs values before sending them to MameHooker
        /// </summary>
        public override void UpdateOutputValues()
        {
            byte Outputs1 = ReadByte(GameProcess_ModuleBaseAddress + _Outputs_Base_Offset + 1);
            byte Outputs2 = ReadByte(GameProcess_ModuleBaseAddress + _Outputs_Base_Offset + 3);
            byte Outputs3 = ReadByte(GameProcess_ModuleBaseAddress + _Outputs_Base_Offset);

            //Original Outputs
            SetOutputValue(OutputId.LmpSide_R, Outputs2 >> 6 & 0x01);
            SetOutputValue(OutputId.LmpSide_G, Outputs2 >> 5 & 0x01);
            SetOutputValue(OutputId.LmpSide_B, Outputs2 >> 4 & 0x01);
            SetOutputValue(OutputId.LmpUpBtn, Outputs1 >> 6 & 0x01);
            SetOutputValue(OutputId.LmpDownBtn, Outputs1 >> 5 & 0x01);
            SetOutputValue(OutputId.LmpCloseBtn, Outputs1 >> 4 & 0x01);
            SetOutputValue(OutputId.P1_GunMotor, Outputs3 >> 4 & 0x01);
            SetOutputValue(OutputId.P2_GunMotor, Outputs3 >> 3 & 0x01);

            //Custom Outputs
            //Ammo + Life
            UInt32 pPlayers = ReadPtr(GameProcess_ModuleBaseAddress + _PlayersStatus_Ptr_Offset);

            for (uint i = 0; i < _GameConfig.PlayerCount; i++)
            {
                _Life[i] = 0;
                _Ammo[i] = 0;

                if (ReadByte(_DamageStatus_CaveAddress + i) == 1)
                {
                    SetOutputValue(OutputId.P1_Damaged + i, 1);
                    WriteByte(_DamageStatus_CaveAddress + i, 0x00);
                }

                if (ReadByte(_RecoilStatus_CaveAddress + i) == 1)
                {
                    SetOutputValue(OutputId.P1_CtmRecoil + i, 1);
                    WriteByte(_RecoilStatus_CaveAddress + i, 0x00);
                }

                if (pPlayers != 0)
                {
                    //Status Byte, 0x01 if playing, else 0x00
                    if (ReadByte(ReadPtr(GameProcess_ModuleBaseAddress + _PlayersPlaying_Ptr_Offset) + 0x1794C + (i * 0xEC)) != 0)
                    {
                        _Ammo[i] = ReadByte(ReadPtr(pPlayers + 0x54 + (4 * i)) + 0x150);
                        _Life[i] = ReadByte(ReadPtr(pPlayers + 0x4C + (4 * i)) + 0x150);
                    }
                }

                SetOutputValue(OutputId.P1_Ammo + i, _Ammo[i]);
                SetOutputValue(OutputId.P1_Life + i, _Life[i]);
            }
        }

    }

    #endregion
}
