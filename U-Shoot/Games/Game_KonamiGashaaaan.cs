using System;
using System.Collections.Generic;
using System.IO;
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
    public class Game_KonamiGashaaaan : BaseGame32
    {
        protected override string GAMEDATA_FOLDER { get { return @"konami\gashaaaan"; } }
        protected override bool PATCH_PEB_DEBUG_FLAG { get { return false; } }

        private const double ORIGINAL_GAME_WIDTH = 1248.0;
        private const double ORIGINAL_GAME_HEIGHT = 468.0;

        //Dll Base Address
        private UInt32 _SystemDll_BaseAddress;
        private UInt32 _IobDll_BaseAddress;
        private UInt32 _GsoundDll_BaseAddress;
        private UInt32 _BallIoDll_BaseAddress;
        private UInt32 _UtilDll_BaseAddress;
        private UInt32 _SoftConfigDll_BaseAddress;
        private UInt32 _FglDll_BaseAddress;

        #region LOADER MEMORY ADDRESSES                
        private UInt32 _SoftConfig_TypeDatRead_Offset = 0x00001791;
        private UInt32 _Gsound_GetBootSetHack_Offset = 0x0000109A;
        private UInt32 _System_ReadNvramDir_Offset = 0x00001EF7;
        private UInt32 _System_ReadRawDir_Offset = 0x00001F22;
        private UInt32 _System_CreateWindowTitleBarFlag_Offset = 0x00001878;
        private UInt32 _System_DualScreenFlag_Offset = 0x00001886;
        private UInt32 _System_WindowSizeFlag_Offset = 0x0000187F;
        private UInt32 _System_FglFullScreenParam_Offset = 0x00002B19;
        private UInt32 _System_CreateWindowExSize_Offset = 0x00001AF5;
        private InjectionStruct32 _Fgl_SetWindowPos_InjectionStruct = new InjectionStruct32(0x00034988, 5);
        private InjectionStruct32 _Fgl_DrawFrameBegin_InjectionStruct = new InjectionStruct32(0x000338D0, 5);

        //Custom Data
        private UInt32 _TypeDatPath_CaveAddress = 0;
        #endregion

        #region INPUTS MEMORY ADDRESSES
        private UInt32 _BallIo_StepForceStage4_Offset = 0x000008349;
        private UInt32 _BallIo_ComConnectPatch_Offset = 0x00002B51;
        private UInt32 _Iob_GetStatus_Offset = 0x0000E120;
        private UInt32 _Iob_CheckPlug_Offset = 0x0000F4A7;
        private UInt32 _Iob_BalloutStatusReq_Offset = 0x0000F3BF;
        private UInt32 _Iob_BalloutStatusSet_Offset = 0x0000F42F;
        private UInt32 _Util_CheckKbdTrg_Offset = 0x00003CB0;
        private UInt32 _Util_MouseUtilGetPoint_Offset = 0x00004060;
        private UInt32 _System_RemoveCalibrationAtBoot_Offset = 0x00005327;
        private UInt32 _Iob_InputBufferButtons_Offset = 0x0003B1AA;
        private UInt32 _Iob_InputBufferTestService_Offset = 0x0003B1A1;
        private UInt32 _Iob_CoinCount_Offset = 0x0003B1A5;
        private UInt32 _Iob_ComConnectPatch_Offset = 0x0000A225;
        private InjectionStruct32 _Iob_GetNetworkId_InjectionStruct = new InjectionStruct32(0x0000F5F1, 67);
        private InjectionStruct32 _Iob_GetFirmware_InjectionStruct = new InjectionStruct32(0x0000F724, 100);

        //Custom Databank
        private UInt32 _Custom_HitAvailableFlag_Address = 0;
        private UInt32 _Custom_HitXValueAsWord_Address = 0;
        private UInt32 _Custom_HitYValueAsWord_Address = 0;
        private UInt32 _NetworkId_CaveAddress = 0;
        private UInt32 _FirmwareVersion_CaveAddress = 0;
        #endregion

        #region OUTPUTS MEMORY ADDRESSES
        private UInt32 _Iob_Outputs_Offset = 0x0003B308;
        private UInt32 _SystemDll_Credits_Offset = 0x00584CF0;
        //private UInt32 _GcoinDll_Credits_Offset = 0x0000E044;
        #endregion

        //Default game resolution is 1248x468
        private int _ResolutionWidth = (int)ORIGINAL_GAME_WIDTH;
        private int _ResolutionHeight = (int)ORIGINAL_GAME_HEIGHT;
        private double _OriginalRatio = 0.0;  //-->468.0 / 1248.0

        //Vanilla launcher had 2 modes (1 and 2)
        //Let's keep mode 1 for a start, see later if it's a problem and add an option to choose between the 2
        private int _InputHackMode = 1;

        public Game_KonamiGashaaaan(GameConfig Config) : base(Config)
        {
            _KnownMd5Prints.Add("Gashaaaan 20060120", "8c713eb4d24116c596c53abde168d3a3");
            _KnownMd5Prints.Add("Gashaaaan 20060323", "0a9017f03f349956b6b55e8eb61602f8");

            //Windows name will be Japanese characters
            _IsAnyWindowTitlePossible = true;

            //Using default original values to compute ratio before replacing these values
            _OriginalRatio = ORIGINAL_GAME_HEIGHT / ORIGINAL_GAME_WIDTH;

            if (_GameConfig.Fullscreen)
            {
                _ResolutionWidth = _DesktopScreenWidth;
                _ResolutionHeight = _DesktopScreenHeight;
            }
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
                if (string.Equals(Path.GetFileName(e.DllPath), "iob.dll", StringComparison.OrdinalIgnoreCase))
                {
                    _IobDll_BaseAddress = (UInt32)lpBaseOfDllLoad;
                }
                else if (string.Equals(Path.GetFileName(e.DllPath), "ballio.dll", StringComparison.OrdinalIgnoreCase))
                {
                    _BallIoDll_BaseAddress = (UInt32)lpBaseOfDllLoad;
                }
                else if (string.Equals(Path.GetFileName(e.DllPath), "util.dll", StringComparison.OrdinalIgnoreCase))
                {
                    _UtilDll_BaseAddress = (UInt32)lpBaseOfDllLoad;
                }
                else if (string.Equals(Path.GetFileName(e.DllPath), "gsound.dll", StringComparison.OrdinalIgnoreCase))
                {
                    _GsoundDll_BaseAddress = (UInt32)lpBaseOfDllLoad;
                }
                else if (string.Equals(Path.GetFileName(e.DllPath), "system.dll", StringComparison.OrdinalIgnoreCase))
                {
                    _SystemDll_BaseAddress = (UInt32)lpBaseOfDllLoad;
                }
                else if (string.Equals(Path.GetFileName(e.DllPath), "soft_config.dll", StringComparison.OrdinalIgnoreCase))
                {
                    _SoftConfigDll_BaseAddress = (UInt32)lpBaseOfDllLoad;
                }
                else if (string.Equals(Path.GetFileName(e.DllPath), "fgl.dll", StringComparison.OrdinalIgnoreCase))
                {
                    _FglDll_BaseAddress = (UInt32)lpBaseOfDllLoad;
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
                    double dMaxX = 1248.0;
                    double dMaxY = 468.0;
                    double dRangeX = dMaxX - dMinX + 1;
                    double dRangeY = dMaxY - dMinY + 1;

                    PlayerController.Computed_X = Convert.ToInt32(Math.Round(dRangeX * PlayerController.Computed_X / TotalResX));

                    //Y axis will only change in fullscreen mode because of the black bars
                    if (_GameConfig.Fullscreen)
                    {
                        //For Y we need to change reference from Screen[X, Y] to real Game [X, Y] with black border;
                        double GameHeight = _OriginalRatio * TotalResX;
                        double Border = (TotalResY - GameHeight) / 2.0;
                        PlayerController.Computed_Y = Convert.ToInt32(Math.Round((double)(PlayerController.Computed_Y - Border)) * dMaxY / GameHeight);
                    }
                    else
                    {
                        PlayerController.Computed_Y = Convert.ToInt32(Math.Round(dRangeY * PlayerController.Computed_Y / TotalResY));
                    }


                    /*

                         double dViewportHeight = _OriginalRatio * dClientWidth;
                         double SideBarsHeight = (_WindowedHeight - dViewportHeight) / 2;
                         _MouseClientPosition.Y -= (int)SideBarsHeight;

                         _MouseClientPosition.Y = Convert.ToInt32(Math.Round((double)_MouseClientPosition.Y) * dMaxY / dViewportHeight);
                     */

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
            _TypeDatPath_CaveAddress = LoaderDatabank_Address;  //17 bytes

            SetHack_SystemForceLocalNvram();
            SetHack_GsoundGetBoot();
            SetHack_SecurityForceLocalTypeDat();

            //Forcing the Flag for Dual Screen drawing, enable control over the frames to redraw each half
            SetHack_SetDualScreenState(0x01);

            //Forcing windowed Mode
            if (_GameConfig.Fullscreen)
                SetHack_SetDisplayWindowed(true);
            else
                SetHack_SetDisplayWindowed(false);

            //Set window size and rendering  
            SetHack_SetWindowSize();
            
            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        /// <summary>
        /// Game is reading the BALL.INI to write NVRAM files on wanted folder
        /// Forcing it to use default local values in game's folder (which is used when entry are not in the ini file)
        /// </summary>
        private void SetHack_SystemForceLocalNvram()
        {
            WriteBytes(_SystemDll_BaseAddress + _System_ReadNvramDir_Offset, new byte[] { 0x90, 0x90 });
            WriteBytes(_SystemDll_BaseAddress + _System_ReadRawDir_Offset, new byte[] { 0x90, 0x90 });
        }

        /// <summary>
        /// Force return 1 do the function call, used in system.dll to boot the game correctly
        /// </summary>
        private void SetHack_GsoundGetBoot()
        {
            WriteBytes(_GsoundDll_BaseAddress + _Gsound_GetBootSetHack_Offset, new byte[] { 0xB0, 0x01, 0xC3 });
        }        

        /// <summary>
        /// TYPE.DAT path must be set in the BALL.INI file
        /// Forcing the use of our own path instead of calling the parameter read
        /// Forcing it to the local patch "..\..\TYPE.DAT"
        /// </summary>
        private void SetHack_SecurityForceLocalTypeDat()
        {
            //String to write in our custom pointer -> "..\\..\\TYPE.DAT" 
            WriteBytes(_TypeDatPath_CaveAddress, new byte[] { 0x2E, 0x2E, 0x5C, 0x5C, 0x2E, 0x2E, 0x5C, 0x5C, 0x54, 0x59, 0x50, 0x45, 0x2E, 0x42, 0x49, 0x4E, 0x00 });

            //mov eax, _Custom_TypeDatPath_Address
            List<byte> Code = new List<byte>{                
                0xB8
            };
            Code.AddRange(BitConverter.GetBytes(_TypeDatPath_CaveAddress));
            WriteBytes((UInt32)_SoftConfigDll_BaseAddress + _SoftConfig_TypeDatRead_Offset, Code.ToArray());
        }

       
        /// <summary>
        /// Default mode is windowed, to be able to display weird resolution, or display window on 2 screens at one to simulate dual screen
        /// </summary>
        private void SetHack_SetDisplayWindowed(bool BorderlessWindow)
        {
            WriteByte(_SystemDll_BaseAddress + _System_CreateWindowTitleBarFlag_Offset, BorderlessWindow ? (byte)0 : (byte)1);   //Show Title bar
            WriteByte(_SystemDll_BaseAddress + _System_FglFullScreenParam_Offset, 0x00);   //No fullscreen
        }

        /// <summary>
        /// The game set a few graphics values here for CreateWindowEx later call :
        /// - Display Top Window BAr ?
        /// - ?
        /// - Dual Screen
        /// - Custom Window Name Pointer
        /// - Default Window Name Pointer
        /// Setting Dual Screen will create somme vertical distortion/offset at the middle junction so that the screens separation will not mask all graphics
        /// But this will also enable some functions loopipng in the game to access each half drawing, and thus create our own renderer...
        /// </summary>
        /// <param name="IsDualScreen"></param>
        private void SetHack_SetDualScreenState(byte IsDualScreen)
        {
            WriteByte(_SystemDll_BaseAddress + _System_DualScreenFlag_Offset, IsDualScreen);
            WriteByte(_SystemDll_BaseAddress + _System_WindowSizeFlag_Offset, 0x01);   //Good Size
        }

        /// <summary>
        /// system.dll is creating the window first (CreatewindowEx)
        /// But fgl.dll is resizing it afterwards to match game's resolution
        /// So patching initial creation only is useless, we will also patch the resize call 
        /// </summary>
        /// <param name="mWidth"></param>
        /// <param name="mHeight"></param>
        private void SetHack_SetWindowSize()
        {
            //METHOD 1 :
            //CreatewindowEx call from system.dll:
            //Just changinf the API width/height parameters
            //Changing the whole procedure calculated height/width causes trouble as it is used later for OPENGL

            //NEW : if required window is smaller than 1248*480, we will create a 1248*480 window and draw smaller image inside
            //Creating smaller window will result in display bug if lower than native size :(
            WriteByte(_SystemDll_BaseAddress + _System_CreateWindowExSize_Offset, 0xB9);
            WriteBytes(_SystemDll_BaseAddress + _System_CreateWindowExSize_Offset + 1, BitConverter.GetBytes(_ResolutionHeight));
            WriteByte(_SystemDll_BaseAddress + _System_CreateWindowExSize_Offset + 5, 0x90);
            WriteByte(_SystemDll_BaseAddress + _System_CreateWindowExSize_Offset + 7, 0xB9);
            WriteBytes(_SystemDll_BaseAddress + _System_CreateWindowExSize_Offset + 8, BitConverter.GetBytes(_ResolutionWidth));
            WriteByte(_SystemDll_BaseAddress + _System_CreateWindowExSize_Offset + 12, 0x90);

            //fgl.dll call to SetWindowPos to resize the window
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);
            //mov [esp+08], WindowWidth
            CaveMemory.Write_StrBytes("C7 44 24 04");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_ResolutionWidth));
            //mov [esp + 4], ResWidth
            CaveMemory.Write_StrBytes("C7 44 24 08");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_ResolutionHeight));
            //Push 00
            CaveMemory.Write_StrBytes("6A 00");
            //Push 00
            CaveMemory.Write_StrBytes("6A 00");
            //Push esi
            CaveMemory.Write_StrBytes("56");

            //Inject it
            CaveMemory.InjectToOffset(_FglDll_BaseAddress, _Fgl_SetWindowPos_InjectionStruct, "Fgl SetwindowPos");

           
            //Forcing "dual screen mode" allow us to have control over the 2 separate drawings on the canvas
            //Disabling it force the game to not run into these functions :-'(
            //So the idea is simple : 
            //1 - remove the 2n half of the screen (right side)
            //2 - change drawing of the first half so it fills the window
            //3 - set screen width for the 3D to render to the fill width

            //1 : Disabling the right half drawing :
            WriteBytes(_SystemDll_BaseAddress + 0x4065, new byte[] { 0x90, 0x90, 0x90, 0x90, 0x90, 0x90 });

            //2: Changing the drawing size :
            //Change Call to fgl_draw_frame like this -> (0, 0, windowWidth, WindowHeignt, 0, 0, 1248 (= 'World' with to display in the desired area'), 468 (World 'Height')

            int w = _ResolutionWidth;
            int h = _ResolutionHeight;

            WriteBytes(_SystemDll_BaseAddress + 0x3FF0, BitConverter.GetBytes(468));
            WriteBytes(_SystemDll_BaseAddress + 0x3FF5, BitConverter.GetBytes(1248));
            WriteBytes(_SystemDll_BaseAddress + 0x3FF9, new byte[] { 0x31, 0xD2, 0x52, 0x31, 0xC0, 0x50, 0x68 });

            //Forcing Keep-Aspect-Ratio format
            double ViewHeight = _OriginalRatio * w;
            double BlackBorder = (_ResolutionHeight - ViewHeight) / 2;
            WriteBytes((UInt32)_SystemDll_BaseAddress + 0x4000, BitConverter.GetBytes((int)ViewHeight));
            WriteByte((UInt32)_SystemDll_BaseAddress + 0x4009, 0x68);
            WriteBytes((UInt32)_SystemDll_BaseAddress + 0x400A, BitConverter.GetBytes((int)BlackBorder));
            
            WriteByte((UInt32)_SystemDll_BaseAddress + 0x4004, 0x68);
            WriteBytes((UInt32)_SystemDll_BaseAddress + 0x4005, BitConverter.GetBytes(w));
            WriteBytes((UInt32)_SystemDll_BaseAddress + 0x400E, new byte[] { 0x90, 0x90, 0x90, 0x90, 0x90 });

            //3 : Changing display width for Opengl:
            //Can't do it at start before game has booted because it result in glithc ??? while changing later while drawing makes things good....
            /*WriteByte((UInt32)_FglDll_BaseAddress + 0x348E4, 0xBA);
            WriteBytes((UInt32)_FglDll_BaseAddress + 0x348E4 + 1, BitConverter.GetBytes(_WindowedWidth));
            WriteBytes((UInt32)_FglDll_BaseAddress + 0x348E4 + 5, new byte[] { 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90 });   //mov edx, WindowWidth

            WriteByte((UInt32)_FglDll_BaseAddress + 0x348F1, 0xB8);
            WriteBytes((UInt32)_FglDll_BaseAddress + 0x348F1 + 1, BitConverter.GetBytes(_WindowedHeight));  //mov eax, WindowHeight*/

            //4 : Forcing Viewport and Scissors calls with good values
            WriteBytes((UInt32)_SystemDll_BaseAddress + 0x19F0, BitConverter.GetBytes(1248));
            WriteBytes((UInt32)_SystemDll_BaseAddress + 0x1A0B, BitConverter.GetBytes(1248));
            WriteBytes((UInt32)_SystemDll_BaseAddress + 0x19FA, BitConverter.GetBytes(468));
            WriteBytes((UInt32)_SystemDll_BaseAddress + 0x1A16, BitConverter.GetBytes(468));

            SetHack_DrawFrameBegin();

        }
        private void SetHack_DrawFrameBegin()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //mov [fgl.dll+6a3c8], WindowedWidth
            CaveMemory.Write_StrBytes("C7 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_FglDll_BaseAddress + 0x6A3C8));
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_ResolutionWidth));
            //mov [fgl.dll+6a3c8], WindowedHeight
            CaveMemory.Write_StrBytes("C7 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_FglDll_BaseAddress + 0x6A3CC));
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_ResolutionHeight));
            //mov eax,[fgl.dll+6A594]
            CaveMemory.Write_StrBytes("A1");
            CaveMemory.Write_Bytes(BitConverter.GetBytes((UInt32)_FglDll_BaseAddress + 0x6A594));

            //Inject it
            CaveMemory.InjectToOffset(_FglDll_BaseAddress, _Fgl_DrawFrameBegin_InjectionStruct, "DrawFrameBegin");
        }

        #endregion

        #region Inputs Hack

        protected override void Apply_InputsMemoryHack()
        {
            Create_InputsDataBank();
            _Custom_HitAvailableFlag_Address = InputsDatabank_Address;
            _Custom_HitXValueAsWord_Address = InputsDatabank_Address + 0x04;
            _Custom_HitYValueAsWord_Address = InputsDatabank_Address + 0x08;
            _FirmwareVersion_CaveAddress = LoaderDatabank_Address + 0x10;   //32 Bytes max
            _NetworkId_CaveAddress = LoaderDatabank_Address + 0x30;   //32 Bytes max

            SetHack_SystemInit();
            SetHack_Iob_Boot();
            SetHack_Iob_GetNetworkId();
            SetHack_Iob_GetFirmwareVersion();
            SetHack_BallIoForceStepToStage4();
            SetHack_KeyboardTrigger();
            SetHack_IobBalloutStatus();
            SetHack_MouseUtilGetPoint();
            SetHack_RemoveComConnect();
            if (_InputHackMode == 1)
            {
                WriteByte(_SystemDll_BaseAddress + 0x5312, 0x01);
                SetHack_IobForceReadButtons();        //---> causes game to crash when a game starts if not set correctly (see function comments)
            }

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }


        /// <summary>
        /// Changeing 2 values :
        /// - Removing calibration on stat sequence
        /// - Removing some security check flag (can make the game crash)
        /// </summary>
        private void SetHack_SystemInit()
        {
            //WriteByte(_SystemDll_BaseAddress + _System_RemoveSecurityCheckAtBoot_Offset, 0x00);
            WriteByte(_SystemDll_BaseAddress + _System_RemoveCalibrationAtBoot_Offset, 0x00);
        }

        /// <summary>
        /// Force checks good
        /// </summary>
        private void SetHack_Iob_Boot()
        {
            WriteBytes(_IobDll_BaseAddress + _Iob_GetStatus_Offset, new byte[] { 0x90, 0x90, 0xB8, 0x03 });
            WriteBytes(_IobDll_BaseAddress + _Iob_CheckPlug_Offset, new byte[] { 0xB0, 0x01, 0xC3 });
        }

        /// <summary>
        /// Force reading custom string
        /// </summary>
        private void SetHack_Iob_GetNetworkId()
        {
            string NetwordId = Properties.Resources.APP_NAME;
            WriteBytes(_NetworkId_CaveAddress, Encoding.ASCII.GetBytes(NetwordId));

            Codecave32 CaveMemory = new Codecave32(_Process, _IobDll_BaseAddress);

            //push ecx
            CaveMemory.Write_StrBytes("51");
            //push edi
            CaveMemory.Write_StrBytes("57");
            //push esi
            CaveMemory.Write_StrBytes("56");
            //mov esi,_NetworkId_CaveAddress
            CaveMemory.Write_StrBytes("BE");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_NetworkId_CaveAddress));
            //mov edi,[ebp+08]
            CaveMemory.Write_StrBytes("8B 7D 08");
            //mov ecx, FwVersion.Length
            CaveMemory.Write_StrBytes("B9");
            CaveMemory.Write_Bytes(BitConverter.GetBytes((UInt32)NetwordId.Length));
            //repe movsb
            CaveMemory.Write_StrBytes("F3 A4");
            //pop esi
            CaveMemory.Write_StrBytes("5E");
            //pop edi
            CaveMemory.Write_StrBytes("5F");
            //pop ecx
            CaveMemory.Write_StrBytes("59");
            //mov obyte ptr[ebp+01], 1
            CaveMemory.Write_StrBytes("C6 45 FF 01");

            //Inject it
            CaveMemory.InjectToOffset(_Iob_GetNetworkId_InjectionStruct, "Network Id");
        }

        /// <summary>
        /// Force reading custom string
        /// </summary>
        private void SetHack_Iob_GetFirmwareVersion()
        {
            string FwVersion = Application.ProductVersion;
            WriteBytes(_FirmwareVersion_CaveAddress, Encoding.ASCII.GetBytes(FwVersion));

            Codecave32 CaveMemory = new Codecave32(_Process, _IobDll_BaseAddress);

            //push ecx
            CaveMemory.Write_StrBytes("51");
            //push edi
            CaveMemory.Write_StrBytes("57");
            //push esi
            CaveMemory.Write_StrBytes("56");
            //mov esi,_FirmwareVersion_CaveAddress
            CaveMemory.Write_StrBytes("BE");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_FirmwareVersion_CaveAddress));
            //mov edi,[ebp+0C]
            CaveMemory.Write_StrBytes("8B 7D 0C");
            //mov ecx, FwVersion.Length
            CaveMemory.Write_StrBytes("B9");
            CaveMemory.Write_Bytes(BitConverter.GetBytes((UInt32)FwVersion.Length));
            //repe movsb
            CaveMemory.Write_StrBytes("F3 A4");
            //pop esi
            CaveMemory.Write_StrBytes("5E");
            //pop edi
            CaveMemory.Write_StrBytes("5F");
            //pop ecx
            CaveMemory.Write_StrBytes("59");

            //Inject it
            CaveMemory.InjectToOffset(_Iob_GetFirmware_InjectionStruct, "Firmware Version");
        }

        /// <summary>
        /// Ballio.dll step() functions hangs on step 2 , force it to go directly from 1 to 4 to get control pulling
        /// </summary> 
        private void SetHack_BallIoForceStepToStage4()
        {
            WriteByte(_BallIoDll_BaseAddress + _BallIo_StepForceStage4_Offset, 0x04);
        }

        /// <summary>
        /// Disabled by code (empty function), will be replaced by a trigger of our own
        /// Resetting value once read prevent multiple inputs at one time
        /// </summary>
        private void SetHack_KeyboardTrigger()
        {
            List<byte> NewFunc = new List<byte>();

            //cmp dword ptr [_Custom_HitAvailableFlag_Address],01
            NewFunc.AddRange(new byte[] { 0x83, 0x3D });
            NewFunc.AddRange(BitConverter.GetBytes(_Custom_HitAvailableFlag_Address));
            NewFunc.Add(0x01);
            //je Trigger
            //xor eax, eax
            //ret
            NewFunc.AddRange(new byte[] { 0x74, 0x03, 0x31, 0xC0, 0xC3 });
            //Trigger:
            //mov [_Custom_HitAvailableFlag_Address],00
            NewFunc.AddRange(new byte[] { 0xC7, 0x05 });
            NewFunc.AddRange(BitConverter.GetBytes(_Custom_HitAvailableFlag_Address));
            NewFunc.AddRange(new byte[] { 0x00, 0x00, 0x00, 0x00 });
            //mov eax, 1
            //ret
            NewFunc.AddRange(new byte[] { 0xB8, 0x01, 0x00, 0x00, 0x00, 0xC3 });

            WriteBytes(_UtilDll_BaseAddress + _Util_CheckKbdTrg_Offset, NewFunc.ToArray());
        }

        /// <summary>
        /// Mouse event from util.dll are disabled by code 
        /// We will force returning our own value
        /// </summary>
        private void SetHack_MouseUtilGetPoint()
        {
            List<byte> NewFunc = new List<byte>();
            //push eax
            //push ecx
            NewFunc.AddRange(new byte[] { 0x50, 0x051 });
            //mov eax, [_Custom_HitXValueAsWord_Address]
            NewFunc.AddRange(new byte[] { 0x8B, 0x05 });
            NewFunc.AddRange(BitConverter.GetBytes(_Custom_HitXValueAsWord_Address));
            //mov ecx[esp+C]
            //mov [ecx], eax
            NewFunc.AddRange(new byte[] { 0x8B, 0x4C, 0x24, 0x0C, 0x89, 0x01 });
            //mov eax, [_Custom_HitYValueAsWord_Address]
            NewFunc.AddRange(new byte[] { 0x8B, 0x05 });
            NewFunc.AddRange(BitConverter.GetBytes(_Custom_HitYValueAsWord_Address));
            //mov ecx[esp+C]
            //mov [ecx], eax
            NewFunc.AddRange(new byte[] { 0x8B, 0x4C, 0x24, 0x10, 0x89, 0x01 });
            //pop ecx
            //pop eax
            //ret
            NewFunc.AddRange(new byte[] { 0x59, 0x58, 0xC3 });

            WriteBytes(_UtilDll_BaseAddress + _Util_MouseUtilGetPoint_Offset, NewFunc.ToArray());
        }

        /// <summary>
        /// Force those 2 functions to return 1 directly
        /// going deep into the function will cause the game to simply crash 
        /// </summary>
        private void SetHack_IobBalloutStatus()
        {
            WriteBytes(_IobDll_BaseAddress + _Iob_BalloutStatusReq_Offset, new byte[] { 0x90, 0x90 });
            WriteBytes(_IobDll_BaseAddress + _Iob_BalloutStatusSet_Offset, new byte[] { 0x90, 0x90 });
        }

        /// <summary>
        /// Remove instructions trying to open COM ports for iob.dll and ballio.dll
        /// Force return connection error, to let COM port available for other devices
        /// </summary>
        private void SetHack_RemoveComConnect()
        {
            WriteBytes(_IobDll_BaseAddress + _Iob_ComConnectPatch_Offset, new byte[] { 0x31, 0xC0, 0x90, 0x90, 0x90 });
            WriteBytes(_BallIoDll_BaseAddress + _BallIo_ComConnectPatch_Offset, new byte[] { 0x31, 0xC0, 0x90, 0x90, 0x90 });
        }

        /// <summary>
        /// Iob.step() is stuck at step 1
        /// Forcing it to step 4 makes the game read the Button buffer correctly (no key sitting pressed when input) but game crash at start
        /// Forcing it to step 6 makes the game work fine (still not finished the game to test)
        /// May not work on some computer ?? Nedd some byte to be forced to 1 but crash ??
        /// </summary>
        private void SetHack_IobForceReadButtons()
        {
            WriteBytes(_IobDll_BaseAddress + 0xE578, new byte[] { 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0xEB });
            WriteByte(_IobDll_BaseAddress + 0xE66E, 0x05);  //--> step 6
        }

        #endregion

        #region Outputs Hack

        protected override void Apply_OutputsMemoryHack()
        {
            Create_OutputsDataBank();

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
                Apply_OR_ByteMask(_IobDll_BaseAddress + _Iob_InputBufferTestService_Offset, 0x04);
                byte bCoin = ReadByte(_IobDll_BaseAddress + _Iob_CoinCount_Offset);
                WriteByte(_IobDll_BaseAddress + _Iob_CoinCount_Offset, (byte)(bCoin + 1));
            }
            else if (e.ButtonName.Equals("SERVICE"))
            {
                Apply_OR_ByteMask(_IobDll_BaseAddress + _Iob_InputBufferTestService_Offset, 0x10);
            }
            if (e.ButtonName.Equals("TEST"))
            {
                Apply_OR_ByteMask(_IobDll_BaseAddress + _Iob_InputBufferTestService_Offset, 0x20);
            }
        }

        protected override void GameController_OnButtonUp(object sender, VirtualGameControler.ButtonDownEventArgs e)
        {
            if (e.ButtonName.Equals("EXIT"))
            {
                Close();
            }
            else if (e.ButtonName.Equals("COIN"))
            {
                Apply_AND_ByteMask(_IobDll_BaseAddress + _Iob_InputBufferTestService_Offset, 0xFB);
            }
            else if (e.ButtonName.Equals("SERVICE"))
            {
                Apply_AND_ByteMask(_IobDll_BaseAddress + _Iob_InputBufferTestService_Offset, 0xEF);
            }
            if (e.ButtonName.Equals("TEST"))
            {
                Apply_AND_ByteMask(_IobDll_BaseAddress + _Iob_InputBufferTestService_Offset, 0xDF);
            }
        }

        protected override void UpdateAxis(PlayerID Id, int X, int Y)
        {            
        }

        //Trigger button, on the other hand, will need some tests to handle auto-fire when Trigger stays pressed
        //This is why a separate byte will receive the trigger state, and this will be computed to create the data copied in the gun data struct
        protected override void PlayerController_OnButtonDown(object sender, VirtualGameControler.ButtonDownEventArgs e)
        {
            if (e.Id != PlayerID.None)
            {
                UInt32 PlayerIndex = (UInt32)e.Id;

                if (e.ButtonName.Equals("TRIGGER"))
                {
                    VirtualPlayerController c = (VirtualPlayerController)sender;
                    WriteBytes(_Custom_HitXValueAsWord_Address, BitConverter.GetBytes((UInt32)c.Computed_X));
                    WriteBytes(_Custom_HitYValueAsWord_Address, BitConverter.GetBytes((UInt32)c.Computed_Y));
                    WriteByte(_Custom_HitAvailableFlag_Address, 0x01);
                }
                else if (e.ButtonName.Equals("START"))
                {
                    int iMask = 0x80 >> (int)PlayerIndex;
                    Apply_OR_ByteMask(_IobDll_BaseAddress + _Iob_InputBufferButtons_Offset, (byte)iMask);
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
                else if (e.ButtonName.Equals("START"))
                {
                    int iMask = 0x80 >> (int)PlayerIndex;
                    Apply_AND_ByteMask(_IobDll_BaseAddress + _Iob_InputBufferButtons_Offset, (byte)~iMask);
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
                new GameOutput(OutputId.P3_LmpStart),
                new GameOutput(OutputId.P4_LmpStart),
                new GameOutput(OutputId.P1_LmpFront),
                new GameOutput(OutputId.P2_LmpFront),
                new GameOutput(OutputId.P3_LmpFront),
                new GameOutput(OutputId.P4_LmpFront),
                new GameOutput(OutputId.MotorUnit_1),
                new GameOutput(OutputId.MotorUnit_2),
                new GameOutput(OutputId.CoinBlocker_1),
                new GameOutput(OutputId.Credits)
            };
        }

        /// <summary>
        /// Update all Outputs values before sending them to MameHooker
        /// </summary>
        public override void UpdateOutputValues()
        {
            //Genuine Outputs
            SetOutputValue(OutputId.P1_LmpStart, ReadByte(_IobDll_BaseAddress + _Iob_Outputs_Offset + 0x1A) >> 3 & 0x01);
            SetOutputValue(OutputId.P2_LmpStart, ReadByte(_IobDll_BaseAddress + _Iob_Outputs_Offset + 0x1A) >> 2 & 0x01);
            SetOutputValue(OutputId.P3_LmpStart, ReadByte(_IobDll_BaseAddress + _Iob_Outputs_Offset + 0x1A) >> 1 & 0x01);
            SetOutputValue(OutputId.P4_LmpStart, ReadByte(_IobDll_BaseAddress + _Iob_Outputs_Offset + 0x1A) & 0x01);
            SetOutputValue(OutputId.P1_LmpFront, ReadByte(_IobDll_BaseAddress + _Iob_Outputs_Offset + 0x1B) >> 7 & 0x01);
            SetOutputValue(OutputId.P2_LmpFront, ReadByte(_IobDll_BaseAddress + _Iob_Outputs_Offset + 0x1B) >> 6 & 0x01);
            SetOutputValue(OutputId.P3_LmpFront, ReadByte(_IobDll_BaseAddress + _Iob_Outputs_Offset + 0x1B) >> 5 & 0x01);
            SetOutputValue(OutputId.P4_LmpFront, ReadByte(_IobDll_BaseAddress + _Iob_Outputs_Offset + 0x1B) >> 4 & 0x01);
            SetOutputValue(OutputId.MotorUnit_1, ReadByte(_IobDll_BaseAddress + _Iob_Outputs_Offset + 0x3C));
            SetOutputValue(OutputId.MotorUnit_2, ReadByte(_IobDll_BaseAddress + _Iob_Outputs_Offset + 0x4C));
            SetOutputValue(OutputId.CoinBlocker_1, ReadByte(_IobDll_BaseAddress + _Iob_Outputs_Offset + 0x1C) >> 6 & 0x01);
            SetOutputValue(OutputId.Credits, ReadByte(_SystemDll_BaseAddress + _SystemDll_Credits_Offset));
        }

        #endregion
    }
}
