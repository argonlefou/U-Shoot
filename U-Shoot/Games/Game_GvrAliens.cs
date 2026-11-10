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
    public class Game_GvrAliens : BaseGame32
    {
        protected override bool PATCH_PEB_DEBUG_FLAG { get { return false; } }
        private const long TARGET_FRAMERATE = 60;    //60 FPS

        private const string NOAVI_OPTION = "No Avi";

        //Default game resolution is 800x600
        private int _ResolutionWidth = 800;
        private int _ResolutionHeight = 600;

        //For fixing FPS
        private long _TickFrequency = 0;

        #region LOADER MEMORY ADDRESSES  
        private UInt32 _Sleep_FunctionPtr_Offset = 0x05589144;
        private UInt32 _QuerryPerformanceCounter_FunctionPtr_Offset = 0x0558905C;
        private UInt32 _DriveCheck_Patch_Offset = 0x0003E83C;
        private UInt32 _KeyboardLockCheck_Patch_Offset = 0x0002F467;
        private UInt32 _ScrollLock_Patch_Offset = 0x0002F840;
        private UInt32 _AudioPath_Offset = 0x0045EBFC;
        private UInt32 _DataPath_Offset = 0x0045ECE0;
        private UInt32 _AbhFilePath_Offset = 0x0046EB0C;
        private UInt32 _ShaddersPath_Offset = 0x00474648;
        private UInt32 _Movie1Path_Offset = 0x00474F04;
        private UInt32 _Movie2Path_Offset = 0x00474F2C;
        private UInt32 _Movie3Path_Offset = 0x00474F4C;
        private UInt32 _Movie4Path_Offset = 0x00474F6C;
        private UInt32 _Movie5Path_Offset = 0x00474F8C;
        private UInt32 _Level1Path_Offset = 0x00474FC0;
        private UInt32 _Level2Path_Offset = 0x00474FE0;
        private UInt32 _Level3Path_Offset = 0x00475000;
        private UInt32 _Level4Path_Offset = 0x00475020;
        private UInt32 _GlViewport1_Patch_Offset = 0x0002E919;
        private UInt32 _GlInitwindowSize_Offset = 0x0002EA69;
        private UInt32 _GlInitPrintf_Patch_ = 0x0002EAA1;
        private UInt32 _GlutSwappBuffer_Jmp_Offset = 0x00053C70;
        private UInt32 _MenuStrings_GameName_Patch_Offset = 0x0002FA51;
        private UInt32 _MenuStrings_OsName_Patch_Offset = 0x0002FA61;
        private UInt32 _MenuStrings_OsDate_Patch_Offset = 0x0002FA81;
        private UInt32 _MenuStrings_ChipsetName_Patch_Offset = 0x0002FA71;
        private UInt32 _MenuStrings_GunBoardVersion_Patch_Offset = 0x0002ED20;
        private UInt32 _MenuStrings_IoBoardVersion_Patch_Offset = 0x0002ED50;
        private UInt32 _PermHqTga_Buffer_Offset = 0x04C98208;
        private NopStruct32 _Nop_GvrioCheckError = new NopStruct32(0x00049302, 2);
        private NopStruct32 _Nop_AlienGunsCheckError = new NopStruct32(0x00049372, 2);
        private InjectionStruct32 _GlutSwappBuffer_InjectionStruct = new InjectionStruct32(0x0002EC60, 5);
        private InjectionStruct32 _NoCrosshair_InjectionStruct = new InjectionStruct32(0x0003AF92, 5);
        private InjectionStruct32 _XRayPatch_InjectionStruct = new InjectionStruct32(0x00449BD, 5);

        //Custom Data
        private UInt32 _WidthRadio_CaveAddress = 0;
        private UInt32 _HeightRadio_CaveAddress = 0;
        private UInt32 _TargetTicks_CaveAddress = 0;
        private UInt32 _LastTickcount_CaveAddress = 0;
        private UInt32 _CurrentTickcount_CaveAddress = 0;
        private UInt32 _Menu_GameName_CaveAddress = 0;
        private UInt32 _Menu_OsName_CaveAddress = 0;
        private UInt32 _Menu_OsDate_CaveAddress = 0;
        private UInt32 _Menu_Chipset_CaveAddress = 0;
        private UInt32 _Menu_BoardVersion_CaveAddress = 0;

        #endregion

        #region INPUTS MEMORY ADDRESSES  
        private UInt32 _CoinSignal1_Offset = 0x004E79F80;
        private UInt32 _CoinSignal2_Offset = 0x004E79F98;
        private UInt32 _CoinSignal3_Offset = 0x004E79FB0;
        private UInt32 _CoinSignal4_Offset = 0x004E79FC8;
        private UInt32 _GameData_Offset = 0x00467550;
        private UInt32 _SetDataValue_Function_Offset = 0x0003E4F0;
        private NopStruct32 _NopGunButtons = new NopStruct32(0x00048851, 5);
        private NopStruct32 _NopPanelButtons = new NopStruct32(0x0002F7F7, 2);
        private InjectionStruct32 _Inputs_InjectionStruct = new InjectionStruct32(0x0002F790, 6);
        private InjectionStruct32 _AutoCalibration_InjectionStruct = new InjectionStruct32(0x0003E880, 5);

        //Custom Data
        private UInt32 _Buttons_CaveAddress = 0;
        private UInt32 _Players_X_CaveAddress = 0;
        private UInt32 _Players_Y_CaveAddress = 0;
        #endregion

        #region OUTPUTS MEMORY ADDRESSES
        private UInt32 _GameStatus_Offset = 0x0047C960;
        private UInt32 _P1_Status_Offset = 0x0556C844;
        private UInt32 _P2_Status_Offset = 0x0556CBD8;
        private UInt32 _P1_Life_Offset = 0x0556C884;
        private UInt32 _P2_Life_Offset = 0x0556CC18;
        private UInt32 _P1_Ammo_Offset = 0x0556CB08;
        private UInt32 _P2_Ammo_Offset = 0x00556CE9C;
        private UInt32 _Outputs_Offset = 0x04E7AEA0;
        private UInt32 _CreditsBase_Offset = 0x0046D748;
        private InjectionStruct32 _Recoil_InjectionStruct = new InjectionStruct32(0x00048D6C, 6);

        //Custom Data
        private UInt32 _Recoil_CaveAddress;
        #endregion

        public Game_GvrAliens(GameConfig Config) : base(Config)
        {
            _KnownMd5Prints.Add("Aliens Extermination v1.03 US - Dehasped + No StaticPath patched", "350d4ad401912003434dcb4fee8fd0bc");
            _KnownMd5Prints.Add("Aliens Extermination v1.03 US - Dehasped", "9ed286aafe474a16c07f7d62b5c06ecb");

            if (_GameConfig.Fullscreen)
                _GameLauncher.Add_CmdLine_Arg("-full");
            else
                _GameLauncher.Add_CmdLine_Arg("-win");

            if (_GameConfig.GetBoolOption(NOAVI_OPTION))
                _GameLauncher.Add_CmdLine_Arg("-noavi");
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
                    double dMaxX = 65535.0;
                    double dMaxY = 65535.0;
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
            Create_LoaderDataBank();
            _WidthRadio_CaveAddress = LoaderDatabank_Address;
            _HeightRadio_CaveAddress = LoaderDatabank_Address + 0x04;
            _TargetTicks_CaveAddress = LoaderDatabank_Address + 0x08;
            _LastTickcount_CaveAddress = LoaderDatabank_Address + 0x10;
            _CurrentTickcount_CaveAddress = LoaderDatabank_Address + 0x18;
            _Menu_GameName_CaveAddress = LoaderDatabank_Address + 0x20;
            _Menu_OsName_CaveAddress = LoaderDatabank_Address + 0x70;
            _Menu_OsDate_CaveAddress = LoaderDatabank_Address + 0x170;
            _Menu_Chipset_CaveAddress = LoaderDatabank_Address + 0x200;
            _Menu_BoardVersion_CaveAddress = LoaderDatabank_Address + 0x210;

            WriteBytes(_Menu_GameName_CaveAddress, Encoding.ASCII.GetBytes("Aliens Extermination"));
            WriteBytes(_Menu_OsName_CaveAddress, Encoding.ASCII.GetBytes(System.Runtime.InteropServices.RuntimeInformation.OSDescription.ToString()));
            WriteBytes(_Menu_OsDate_CaveAddress, Encoding.ASCII.GetBytes(DateTime.Now.ToString("MMM dd yyyy HH:mm:ss")));
            WriteBytes(_Menu_Chipset_CaveAddress, Encoding.ASCII.GetBytes("U-Shoot"));
            WriteBytes(_Menu_BoardVersion_CaveAddress, Encoding.ASCII.GetBytes(System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString()));

            //Changing displayed data in TEST menu
            SetHack_MenuStrings();

            //Remove drive check on C: file
            WriteByte(GameProcess_ModuleBaseAddress + _DriveCheck_Patch_Offset, 0xEB);

            //At start, the game looks for Caps Lock, Scroll Lock and Num Lock status on the computer to deactivate them
            //Disabling that...
            WriteBytes(GameProcess_ModuleBaseAddress + _KeyboardLockCheck_Patch_Offset, new byte[] { 0xEB, 0x55 });
            //During the game, it constantly On/Off scrool lock. Just ret in the function before doing anything
            WriteByte(GameProcess_ModuleBaseAddress + _ScrollLock_Patch_Offset, 0xC3);

            //Change all C:\etc.... hardcoded strings to ..\ to allow "portable use"
            ReplaceReadOnlyString(new uint[] { _AudioPath_Offset }, Encoding.ASCII.GetBytes("..\\AUDIO\\%s\0"));
            ReplaceReadOnlyString(new uint[] { _DataPath_Offset }, Encoding.ASCII.GetBytes(".\\%s\0"));
            ReplaceReadOnlyString(new uint[] { _AbhFilePath_Offset }, Encoding.ASCII.GetBytes(".\\ABH.ADJ\0"));
            ReplaceReadOnlyString(new uint[] { _ShaddersPath_Offset }, Encoding.ASCII.GetBytes("..\\SHADERS\\%s\0"));

            ReplaceReadOnlyString(new uint[] { _Movie1Path_Offset }, Encoding.ASCII.GetBytes(".\\AliensExCredits.avi\0"));
            ReplaceReadOnlyString(new uint[] { _Movie2Path_Offset }, Encoding.ASCII.GetBytes(".\\endmovie2.avi\0"));
            ReplaceReadOnlyString(new uint[] { _Movie3Path_Offset }, Encoding.ASCII.GetBytes(".\\select.avi\0"));
            ReplaceReadOnlyString(new uint[] { _Movie4Path_Offset }, Encoding.ASCII.GetBytes(".\\gunmovie.avi\0"));
            ReplaceReadOnlyString(new uint[] { _Movie5Path_Offset }, Encoding.ASCII.GetBytes(".\\attract.avi\0"));

            ReplaceReadOnlyString(new uint[] { _Level1Path_Offset }, Encoding.ASCII.GetBytes(".\\level1.zio\0"));
            ReplaceReadOnlyString(new uint[] { _Level2Path_Offset }, Encoding.ASCII.GetBytes(".\\level2.zio\0"));
            ReplaceReadOnlyString(new uint[] { _Level3Path_Offset }, Encoding.ASCII.GetBytes(".\\level3.zio\0"));
            ReplaceReadOnlyString(new uint[] { _Level4Path_Offset }, Encoding.ASCII.GetBytes(".\\level4.zio\0"));

            //Removing RED string ERROR on start (IO check)
            SetNops(GameProcess_ModuleBaseAddress, _Nop_GvrioCheckError);
            SetNops(GameProcess_ModuleBaseAddress, _Nop_AlienGunsCheckError);

            //Fixing initial Glut functions (create window, etc...)
            if (_GameConfig.ResolutionWidth > 0)
                _ResolutionWidth = _GameConfig.ResolutionWidth;
            if (_GameConfig.ResolutionHeight > 0)
                _ResolutionHeight = _GameConfig.ResolutionHeight;

            WriteBytes(GameProcess_ModuleBaseAddress + _GlInitwindowSize_Offset + 1, BitConverter.GetBytes(_ResolutionHeight));
            WriteBytes(GameProcess_ModuleBaseAddress + _GlInitwindowSize_Offset + 6, BitConverter.GetBytes(_ResolutionWidth));
            WriteBytes(GameProcess_ModuleBaseAddress + _GlInitPrintf_Patch_ + 1, BitConverter.GetBytes(_ResolutionHeight));
            WriteBytes(GameProcess_ModuleBaseAddress + _GlInitPrintf_Patch_ + 6, BitConverter.GetBytes(_ResolutionWidth));

            WriteBytes(GameProcess_ModuleBaseAddress + _GlViewport1_Patch_Offset + 1, BitConverter.GetBytes(_ResolutionHeight));
            WriteBytes(GameProcess_ModuleBaseAddress + _GlViewport1_Patch_Offset + 6, BitConverter.GetBytes(_ResolutionWidth));

            //If Screen has a different resolution than original 800x600, the X-Ray display will be messed-up because the coordinates are not scaled
            float fWidth = _ResolutionWidth / 800.0f;
            float fHeight = _ResolutionHeight / 600.0f;
            WriteBytes(_WidthRadio_CaveAddress, BitConverter.GetBytes(fWidth));
            WriteBytes(_HeightRadio_CaveAddress, BitConverter.GetBytes(fHeight));
            SetHack_XrayCorrection();

            //To fix FPS, we first need to get the TickFrequency (how many ticks for a second), and then set the desired frame duration and
            // corresponding tick count
            Win32.QueryPerformanceFrequency(out _TickFrequency);
            if (_TickFrequency > 0)
            {
                //25 fps : 40ms
                //30 fps : 33,3 ms
                //60 fps : 16,7 ms
                long FrameDuration = 1000 / TARGET_FRAMERATE;

                long TargetTicks = _TickFrequency * FrameDuration / 1000;
                WriteBytes(_TargetTicks_CaveAddress, BitConverter.GetBytes(TargetTicks));

                //Create codecave to use this
                SetHack_FixFps();
            }

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        //The game is calling glutSwapBuffer once each frame, but FPS is too high (especially in windowed mode)
        //Playing with millisecond and adding some sleep before each call may stabilize the FPS as we want
        //Also, ticks beeing 64bits, it needs EAX+EDX registers to do 64bits Add/Substract
        private void SetHack_FixFps()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //CheckTime:
            //push _CurrentTickcount_CaveAddress
            CaveMemory.Write_StrBytes("68");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_CurrentTickcount_CaveAddress));
            //call dword ptr ["aliens dehasped.exe" + _QuerryPerformanceCounter_FunctionPtr_Offset]
            CaveMemory.Write_StrBytes("FF 15");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _QuerryPerformanceCounter_FunctionPtr_Offset));
            //mov eax,[_CurrentTickcount_CaveAddress]
            CaveMemory.Write_StrBytes("8B 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_CurrentTickcount_CaveAddress));
            //mov edx,[_CurrentTickcount_CaveAddress + 4]
            CaveMemory.Write_StrBytes("8B 15");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_CurrentTickcount_CaveAddress + 4));
            //sub eax,[_LastTickcount_CaveAddress]
            CaveMemory.Write_StrBytes("2B 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_LastTickcount_CaveAddress));
            //sub edx,[_LastTickcount_CaveAddress + 4]
            CaveMemory.Write_StrBytes("2B 15");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_LastTickcount_CaveAddress + 4));
            //cmp edx,[_TargetTicks_CaveAddress + 4]
            CaveMemory.Write_StrBytes("3B 15");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_TargetTicks_CaveAddress + 4));
            //jl Sleep
            CaveMemory.Write_StrBytes("7C 0A");
            //jg OriginalCode
            CaveMemory.Write_StrBytes("7F 12");
            //cmp eax,[_TargetTicks_CaveAddress]
            CaveMemory.Write_StrBytes("3B 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_TargetTicks_CaveAddress));
            //jae DoneWaiting
            CaveMemory.Write_StrBytes("73 0A");

            //Sleep:
            //push 01
            CaveMemory.Write_StrBytes("6A 01");
            //call dword ptr ["aliens dehasped.exe"+ _Sleep_FunctionPtr_Offset]
            CaveMemory.Write_StrBytes("FF 15");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _Sleep_FunctionPtr_Offset));
            //jmp CheckTime
            CaveMemory.Write_StrBytes("EB C1");

            //DoneWaiting:
            //mov eax,[_CurrentTickcount_CaveAddress]
            CaveMemory.Write_StrBytes("8B 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_CurrentTickcount_CaveAddress));
            //mov edx,[_CurrentTickcount_CaveAddress + 4]
            CaveMemory.Write_StrBytes("8B 15");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_CurrentTickcount_CaveAddress + 4));
            //mov [_LastTickcount_CaveAddress],eax
            CaveMemory.Write_StrBytes("89 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_LastTickcount_CaveAddress));
            //mov [_LastTickcount_CaveAddress + 4],edx
            CaveMemory.Write_StrBytes("89 15");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_LastTickcount_CaveAddress + 4));
            //jmp to call glutSwapBuffer
            CaveMemory.Write_jmp(GameProcess_ModuleBaseAddress + _GlutSwappBuffer_Jmp_Offset);

            //Inject it
            CaveMemory.InjectToOffset(_GlutSwappBuffer_InjectionStruct, "glutSwapBuffer");
        }

        /// <summary>
        /// Totally useless, but changing the strings displayed in the Main TEST menu screen
        /// </summary>
        private void SetHack_MenuStrings()
        {
            WriteBytes(GameProcess_ModuleBaseAddress + _MenuStrings_GameName_Patch_Offset, BitConverter.GetBytes(_Menu_GameName_CaveAddress));
            WriteBytes(GameProcess_ModuleBaseAddress + _MenuStrings_OsName_Patch_Offset, BitConverter.GetBytes(_Menu_OsName_CaveAddress));
            WriteBytes(GameProcess_ModuleBaseAddress + _MenuStrings_OsDate_Patch_Offset, BitConverter.GetBytes(_Menu_OsDate_CaveAddress));
            WriteBytes(GameProcess_ModuleBaseAddress + _MenuStrings_ChipsetName_Patch_Offset, BitConverter.GetBytes(_Menu_Chipset_CaveAddress));
            WriteBytes(GameProcess_ModuleBaseAddress + _MenuStrings_GunBoardVersion_Patch_Offset, new byte[] { 0xB8, 0x02, 0x00, 0x00, 0x00, 0xC3 });
            WriteBytes(GameProcess_ModuleBaseAddress + _MenuStrings_IoBoardVersion_Patch_Offset, new byte[] { 0xB8, 0x03, 0x00, 0x00, 0x00, 0xC3 });
        }

        /// <summary>
        /// When calling the function for the "scanrect", coordinates are not scaled by glut system if resolution is not original
        /// To fix it, simply multiply each of the coordinates by the ratio between new rez/ old rez
        /// </summary>
        private void SetHack_XrayCorrection()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);
            //mov ecx,[ecx]
            CaveMemory.Write_StrBytes("8B 09");
            //push edx
            CaveMemory.Write_StrBytes("52");
            //push ecx
            CaveMemory.Write_StrBytes("51");
            //push eax
            CaveMemory.Write_StrBytes("50");

            //fld dword ptr [esp+04]
            CaveMemory.Write_StrBytes("D9 44 24 04");
            //fmul dword ptr [_WidthRadio_CaveAddress]
            CaveMemory.Write_StrBytes("D8 0D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_WidthRadio_CaveAddress));
            //fstp dword ptr [esp+04]
            CaveMemory.Write_StrBytes("D9 5C 24 04");

            //fld dword ptr [esp+08]
            CaveMemory.Write_StrBytes("D9 44 24 08");
            //fmul dword ptr [_HeightRadio_CaveAddress]
            CaveMemory.Write_StrBytes("D8 0D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_HeightRadio_CaveAddress));
            //fstp dword ptr [esp+08]
            CaveMemory.Write_StrBytes("D9 5C 24 08");

            //fld dword ptr [esp+0C]
            CaveMemory.Write_StrBytes("D9 44 24 0C");
            //fmul dword ptr [_WidthRadio_CaveAddress]
            CaveMemory.Write_StrBytes("D8 0D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_WidthRadio_CaveAddress));
            //fstp dword ptr [esp+0C]
            CaveMemory.Write_StrBytes("D9 5C 24 0C");

            //fld dword ptr [esp+10]
            CaveMemory.Write_StrBytes("D9 44 24 10");
            //fmul dword ptr [_HeightRadio_CaveAddress]
            CaveMemory.Write_StrBytes("D8 0D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_HeightRadio_CaveAddress));
            //fstp dword ptr [esp+10]
            CaveMemory.Write_StrBytes("D9 5C 24 10");

            //Inject it
            CaveMemory.InjectToOffset(_XRayPatch_InjectionStruct, "XRay");
        }

        #endregion        

        #region Inputs Hack

        protected override void Apply_InputsMemoryHack()
        {
            Create_InputsDataBank();
            _Buttons_CaveAddress = InputsDatabank_Address;  //8 Bytes
            _Players_X_CaveAddress = InputsDatabank_Address + 0x10; //8 Bytes (4 for P1, 4 for P2)
            _Players_Y_CaveAddress = InputsDatabank_Address + 0x18; //8 Bytes (4 for P1, 4 for P2)

            //Removing calls to native joystick reading, and removing GUN text error on screen
            SetNops(GameProcess_ModuleBaseAddress, _NopGunButtons);

            //Data block has default value 4 bytes after current values
            //Changing default calibration values so that the game is loading correct ones when creating default file if it is not on the disk
            //P1 Left, Right, Top, Bottom
            WriteBytes(GameProcess_ModuleBaseAddress + _GameData_Offset + 0x6B5C, BitConverter.GetBytes((UInt32)0));
            WriteBytes(GameProcess_ModuleBaseAddress + _GameData_Offset + 0x6BD4, BitConverter.GetBytes((UInt32)65535));
            WriteBytes(GameProcess_ModuleBaseAddress + _GameData_Offset + 0x6C4C, BitConverter.GetBytes((UInt32)0));
            WriteBytes(GameProcess_ModuleBaseAddress + _GameData_Offset + 0x6CC4, BitConverter.GetBytes((UInt32)65535));
            //P2
            WriteBytes(GameProcess_ModuleBaseAddress + _GameData_Offset + 0x6D3C, BitConverter.GetBytes((UInt32)0));
            WriteBytes(GameProcess_ModuleBaseAddress + _GameData_Offset + 0x6DB4, BitConverter.GetBytes((UInt32)65535));
            WriteBytes(GameProcess_ModuleBaseAddress + _GameData_Offset + 0x6E2C, BitConverter.GetBytes((UInt32)0));
            WriteBytes(GameProcess_ModuleBaseAddress + _GameData_Offset + 0x6EA4, BitConverter.GetBytes((UInt32)65535));

            SetHack_AutoCalibration();

            SetHack_ButtonsAndAxes();

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        /// <summary>
        /// If a data file already exists, it will not create default settings but load the data from the file
        /// Once loaded, changing the calibration values as needed. This should also make the game save the data file afterwards
        /// </summary>
        private void SetHack_AutoCalibration()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //push ecx
            CaveMemory.Write_StrBytes("51");

            //push 00
            CaveMemory.Write_StrBytes("6A 00");
            //push 000000E5
            CaveMemory.Write_StrBytes("68 E5 00 00 00");
            //call SetDataValue_Function()
            CaveMemory.Write_call(GameProcess_ModuleBaseAddress + _SetDataValue_Function_Offset);


            //push 0000FFFF
            CaveMemory.Write_StrBytes("68 FF FF 00 00");
            //push 000000E6
            CaveMemory.Write_StrBytes("68 E6 00 00 00");
            //call SetDataValue_Function()
            CaveMemory.Write_call(GameProcess_ModuleBaseAddress + _SetDataValue_Function_Offset);

            //push 00
            CaveMemory.Write_StrBytes("6A 00");
            //push 000000E7
            CaveMemory.Write_StrBytes("68 E7 00 00 00");
            //call SetDataValue_Function()
            CaveMemory.Write_call(GameProcess_ModuleBaseAddress + _SetDataValue_Function_Offset);

            //push 0000FFFF
            CaveMemory.Write_StrBytes("68 FF FF 00 00");
            //push 000000E8
            CaveMemory.Write_StrBytes("68 E8 00 00 00");
            //call SetDataValue_Function()
            CaveMemory.Write_call(GameProcess_ModuleBaseAddress + _SetDataValue_Function_Offset);

            //push 00
            CaveMemory.Write_StrBytes("6A 00");
            //push 000000E9
            CaveMemory.Write_StrBytes("68 E9 00 00 00");
            //call SetDataValue_Function()
            CaveMemory.Write_call(GameProcess_ModuleBaseAddress + _SetDataValue_Function_Offset);

            //push 0000FFFF
            CaveMemory.Write_StrBytes("68 FF FF 00 00");
            //push 000000EA
            CaveMemory.Write_StrBytes("68 EA 00 00 00");
            //call SetDataValue_Function()
            CaveMemory.Write_call(GameProcess_ModuleBaseAddress + _SetDataValue_Function_Offset);

            //push 00
            CaveMemory.Write_StrBytes("6A 00");
            //push 000000EB
            CaveMemory.Write_StrBytes("68 EB 00 00 00");
            //call SetDataValue_Function()
            CaveMemory.Write_call(GameProcess_ModuleBaseAddress + _SetDataValue_Function_Offset);

            //push 0000FFFF
            CaveMemory.Write_StrBytes("68 FF FF 00 00");
            //push 000000EC
            CaveMemory.Write_StrBytes("68 EC 00 00 00");
            //call SetDataValue_Function()
            CaveMemory.Write_call(GameProcess_ModuleBaseAddress + _SetDataValue_Function_Offset);

            //add esp,40
            CaveMemory.Write_StrBytes("83 C4 40");  //Need to adjust ESP after all these push and calls

            //pop ecx
            CaveMemory.Write_StrBytes("59");

            //xor eax,eax
            CaveMemory.Write_StrBytes("31 C0");
            //add esp,40
            CaveMemory.Write_StrBytes("83 C4 40");

            //Inject it
            CaveMemory.InjectToOffset(_AutoCalibration_InjectionStruct, "Auto Calibration");
        }

        /// <summary>
        /// Game checks keyboard keys with getAsyncKeyState and do bit operation depending on the results
        /// Injecting at the beginning of the function will allow to set all players + panels inputs
        /// Then removing the original bitwise operation instruction
        /// 
        /// That function cannot be totally removed as the end is handling credits signals input
        /// </summary>
        private void SetHack_ButtonsAndAxes()
        {
            //Nopping original button bit operation
            WriteBytes(GameProcess_ModuleBaseAddress + 0x2F7F7, new byte[] { 0x90, 0x90 });

            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //cmp ebp,02
            CaveMemory.Write_StrBytes("83 FD 02");
            //jnl OriginalCode
            CaveMemory.Write_StrBytes("7D 23");

            //mov eax,[ebp*4 + _Buttons_CaveAddress]
            CaveMemory.Write_StrBytes("8B 04 AD");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Buttons_CaveAddress));
            //mov [esi],eax
            CaveMemory.Write_StrBytes("89 06");

            //mov eax,[ebp*4 + _Players_X_CaveAddress]
            CaveMemory.Write_StrBytes("8B 04 AD");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Players_X_CaveAddress));
            //mov [esi+000001EC],eax
            CaveMemory.Write_StrBytes("89 86 EC 01 00 00");

            //mov eax,[ebp*4 + _Players_Y_CaveAddress]
            CaveMemory.Write_StrBytes("8B 04 AD");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Players_Y_CaveAddress));
            //mov [esi+00000F0],eax
            CaveMemory.Write_StrBytes("89 86 F0 01 00 00");

            //OriginalCode:
            //mov eax,[esp+04]
            CaveMemory.Write_StrBytes("8B 44 24 04");
            //test eax,eax
            CaveMemory.Write_StrBytes("85 C0");

            //Inject it
            CaveMemory.InjectToOffset(_Inputs_InjectionStruct, "Inputs");
        }
       
        #endregion

        #region Outputs Hack

        protected override void Apply_OutputsMemoryHack()
        {
            Create_OutputsDataBank();
            _Recoil_CaveAddress = OutputsDatabank_Address;

            SetHack_CustomRecoilOutput();

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        /// <summary>
        /// This codecave will intercept the game's procedure changing the output values
        /// Genuine recoil is set by the game in memory (0x50909E0) with values 0x05 for P1 and 0x0A for P2.
        /// But I have no clue how it is unset ("Px Gun Power" setting not used) to be sure it's long enough to work, so this method will ensure a custom recoil not missed
        /// </summary>
        private void SetHack_CustomRecoilOutput()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //or [_Recoil_CaveAddress], eax
            CaveMemory.Write_StrBytes("09 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Recoil_CaveAddress));
            //mov ["aliens dehasped.exe"+4C909E0],ecx
            CaveMemory.Write_StrBytes("89 0D E0 09 09 05");

            //Inject it
            CaveMemory.InjectToOffset(_Recoil_InjectionStruct, "Recoil");
        }

        #endregion

        #region No Crosshair Hack

        protected override void Apply_NoCrosshairMemoryHack()
        {
            SetHack_NoCrosshair();

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        /// <summary>
        /// When the game is loading a TGA texture files, waiting for it to be PermHQ.TGA (contains crosshair sprite)
        /// When it is, altering the alpha bytes from the part of the raw read TGA file to remove the crosshair visibility
        /// </summary>
        private void SetHack_NoCrosshair()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //test eax,eax
            CaveMemory.Write_StrBytes("85 C0");
            //jne originalcode
            CaveMemory.Write_StrBytes("0F 85 4B 00 00 00");
            //pushad
            CaveMemory.Write_StrBytes("60");
            //mov esi,[esp+24]
            CaveMemory.Write_StrBytes("8B 74 24 24");
            //cmp esi,"aliens dehasped.exe"+_PermHqTga_Buffer_OFfset
            CaveMemory.Write_StrBytes("81 FE");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _PermHqTga_Buffer_Offset));
            //jne skip
            CaveMemory.Write_StrBytes("0F 85 39 00 00 00");
            //mov edi,[esi+58]
            CaveMemory.Write_StrBytes("8B 7E 58");
            //mov eax,[esi+0C]
            CaveMemory.Write_StrBytes("8B 46 0C");
            //shl eax,02
            CaveMemory.Write_StrBytes("C1 E0 02");
            //mov ecx,eax
            CaveMemory.Write_StrBytes("8B C8");
            //mov eax,000003FF
            CaveMemory.Write_StrBytes("B8 FF 03 00 00");
            //sub eax,00000217
            CaveMemory.Write_StrBytes("2D 17 02 00 00");
            //mov edx,00000041
            CaveMemory.Write_StrBytes("BA 41 00 00 00");

            //RowLoop:
            //push eax
            CaveMemory.Write_StrBytes("50");
            //imul eax,ecx
            CaveMemory.Write_StrBytes("0F AF C1");
            //add eax,edi
            CaveMemory.Write_StrBytes("01 F8");
            //add eax,00000DE8
            CaveMemory.Write_StrBytes("05 E8 0D 00 00");
            //mov ebx,00000041
            CaveMemory.Write_StrBytes("BB 41 00 00 00");

            //ColumnLoop:
            //mov byte ptr [eax+03],00
            CaveMemory.Write_StrBytes("C6 40 03 00");
            //add eax,04
            CaveMemory.Write_StrBytes("83 C0 04");
            //dec ebx
            CaveMemory.Write_StrBytes("4B");
            //jne ColumnLoop
            CaveMemory.Write_StrBytes("75 F6");
            //pop eax
            CaveMemory.Write_StrBytes("58");
            //dec eax
            CaveMemory.Write_StrBytes("48");
            //dec edx
            CaveMemory.Write_StrBytes("4A");
            //jne RowLoop
            CaveMemory.Write_StrBytes("75 E1");

            //skip:
            //popad
            CaveMemory.Write_StrBytes("61");

            //originalcode:
            //add esp,08
            CaveMemory.Write_StrBytes("83 C4 08");
            //test eax,eax
            CaveMemory.Write_StrBytes("85 C0");

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
            else if (e.ButtonName.Equals("SELECT"))
            {
                Apply_OR_ByteMask(_Buttons_CaveAddress + 0x02, 0x20);
            }
            else if (e.ButtonName.Equals("TEST"))
            {
                Apply_OR_ByteMask(_Buttons_CaveAddress + 0x02, 0x10);
            }
            else if (e.ButtonName.Equals("VOLUME UP"))
            {
                Apply_OR_ByteMask(_Buttons_CaveAddress + 0x02, 0x40);
            }
            else if (e.ButtonName.Equals("VOLUME DOWN"))
            {
                Apply_OR_ByteMask(_Buttons_CaveAddress + 0x02, 0x80);
            }
            //To enable coin detection, a flag must me set to "1" (will be reset by the game once read)
            else if (e.ButtonName.Equals("COIN 1"))
            {
                Apply_OR_ByteMask(_Buttons_CaveAddress + 0x03, 0x01);
                WriteByte(GameProcess_ModuleBaseAddress + _CoinSignal1_Offset, 0x01);
            }
            else if (e.ButtonName.Equals("COIN 2"))
            {
                Apply_OR_ByteMask(_Buttons_CaveAddress + 0x03, 0x02);
                WriteByte(GameProcess_ModuleBaseAddress + _CoinSignal2_Offset, 0x01);
            }
            else if (e.ButtonName.Equals("COIN 3"))
            {
                Apply_OR_ByteMask(_Buttons_CaveAddress + 0x03, 0x04);
                WriteByte(GameProcess_ModuleBaseAddress + _CoinSignal3_Offset, 0x01);
            }
            else if (e.ButtonName.Equals("COIN 4"))
            {
                Apply_OR_ByteMask(_Buttons_CaveAddress + 0x03, 0x08);
                WriteByte(GameProcess_ModuleBaseAddress + _CoinSignal4_Offset, 0x01);
            }
        }

        protected override void GameController_OnButtonUp(object sender, VirtualGameControler.ButtonDownEventArgs e)
        {
            if (e.ButtonName.Equals("SELECT"))
            {
                Apply_AND_ByteMask(_Buttons_CaveAddress + 0x02, 0xDF);
            }
            else if (e.ButtonName.Equals("TEST"))
            {
                Apply_AND_ByteMask(_Buttons_CaveAddress + 0x02, 0xEF);
            }
            else if (e.ButtonName.Equals("VOLUME UP"))
            {
                Apply_AND_ByteMask(_Buttons_CaveAddress + 0x02, 0xBF);
            }
            else if (e.ButtonName.Equals("VOLUME DOWN"))
            {
                Apply_AND_ByteMask(_Buttons_CaveAddress + 0x02, 0x7F);
            }
            else if (e.ButtonName.Equals("COIN 1"))
            {
                Apply_AND_ByteMask(_Buttons_CaveAddress + 0x03, 0xFE);
            }
            else if (e.ButtonName.Equals("COIN 2"))
            {
                Apply_AND_ByteMask(_Buttons_CaveAddress + 0x03, 0xFD);
            }
            else if (e.ButtonName.Equals("COIN 3"))
            {
                Apply_AND_ByteMask(_Buttons_CaveAddress + 0x03, 0xFB);
            }
            else if (e.ButtonName.Equals("COIN 4"))
            {
                Apply_AND_ByteMask(_Buttons_CaveAddress + 0x03, 0xF7);
            }
        }

        protected override void UpdateAxis(PlayerID Id, int X, int Y)
        {
            if (Id != PlayerID.None)
            {
                byte[] bufferX = BitConverter.GetBytes(X);
                byte[] bufferY = BitConverter.GetBytes(Y);

                WriteBytes(_Players_X_CaveAddress + ((uint)Id * 4), bufferX);
                WriteBytes(_Players_Y_CaveAddress + ((uint)Id * 4), bufferY);
            }
        }

        protected override void PlayerController_OnButtonDown(object sender, VirtualGameControler.ButtonDownEventArgs e)
        {
            if (e.Id != PlayerID.None)
            {
                UInt32 PlayerIndex = (UInt32)e.Id;

                if (e.ButtonName.Equals("TRIGGER"))
                {
                    Apply_OR_ByteMask(_Buttons_CaveAddress + (4 * PlayerIndex), 0x10);
                }
                else if (e.ButtonName.Equals("SPECIAL"))
                {
                    Apply_OR_ByteMask(_Buttons_CaveAddress + (4 * PlayerIndex), 0x20);
                }
                else if (e.ButtonName.Equals("FLAME"))
                {
                    Apply_OR_ByteMask(_Buttons_CaveAddress + (4 * PlayerIndex), 0x40);
                }
                else if (e.ButtonName.Equals("START"))
                {
                    int iMask = 1 << (int)PlayerIndex;
                    Apply_OR_ByteMask(_Buttons_CaveAddress + 0x02, (byte)iMask);
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
                    Apply_AND_ByteMask(_Buttons_CaveAddress + (4 * PlayerIndex), 0xEF);
                }
                else if (e.ButtonName.Equals("SPECIAL"))
                {
                    Apply_AND_ByteMask(_Buttons_CaveAddress + (4 * PlayerIndex), 0xDF);
                }
                else if (e.ButtonName.Equals("FLAME"))
                {
                    Apply_AND_ByteMask(_Buttons_CaveAddress + (4 * PlayerIndex), 0xBF);
                }
                else if (e.ButtonName.Equals("START"))
                {
                    int iMask = 1 << (int)PlayerIndex;
                    Apply_AND_ByteMask(_Buttons_CaveAddress + 0x02, (byte)~iMask);
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
                new GameOutput(OutputId.P1_GunRecoil),
                new GameOutput(OutputId.P2_GunRecoil),
                new GameOutput(OutputId.P1_LedAmmo1),
                new GameOutput(OutputId.P1_LedAmmo2),
                new GameOutput(OutputId.P2_LedAmmo1),
                new GameOutput(OutputId.P2_LedAmmo2),
                new SyncBlinkingGameOutput(OutputId.P1_CtmLmpStart, 500),
                new SyncBlinkingGameOutput(OutputId.P2_CtmLmpStart, 500),
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
            //Original Outputs
            SetOutputValue(OutputId.P1_LedAmmo1, ReadByte(GameProcess_ModuleBaseAddress + _Outputs_Offset + 1));
            SetOutputValue(OutputId.P1_LedAmmo2, ReadByte(GameProcess_ModuleBaseAddress + _Outputs_Offset));
            SetOutputValue(OutputId.P2_LedAmmo1, ReadByte(GameProcess_ModuleBaseAddress + _Outputs_Offset + 3));
            SetOutputValue(OutputId.P2_LedAmmo2, ReadByte(GameProcess_ModuleBaseAddress + _Outputs_Offset + 2));
            //Gun recoil : values are set when bullet is fired, but I have no clue when the game is setting it back to 0
            //Gun recoil force adjustment doesn't seem to play here, mean time between ON and OFF state seems to be ~30 / ~35 ms
            //Which may be too quick to work good with recoil...
            //But the Enable/Disable kickback in TEST menu will work here
            SetOutputValue(OutputId.P1_GunRecoil, ReadByte(GameProcess_ModuleBaseAddress + _Outputs_Offset + 4) >> 2 & 0x01);
            SetOutputValue(OutputId.P2_GunRecoil, ReadByte(GameProcess_ModuleBaseAddress + _Outputs_Offset + 5) >> 2 & 0x01);

            //Custom Outputs
            for (uint i = 0; i < _GameConfig.PlayerCount; i++)
            {
                _Life[i] = 0;
                _Ammo[i] = 0;
            }

            //Game status :
            //[2] = Menu / Continue screen
            //[4] = In game
            //[64] = Attract demo
            // We will use these values to compute ourselve Recoil and P1/P2 Start Button Lights
            byte GameStatus = ReadByte(GameProcess_ModuleBaseAddress + _GameStatus_Offset);
            if (GameStatus == 4)
            {
                //Playr status:
                //1: GameOver
                //4: Ingame
                //8: Continue screen
                int P1_Status = ReadByte(GameProcess_ModuleBaseAddress + _P1_Status_Offset);
                int P2_Status = ReadByte(GameProcess_ModuleBaseAddress + _P2_Status_Offset);

                if (P1_Status == 4)
                {
                    //Force Start Lamp to Off
                    SetOutputValue(OutputId.P1_CtmLmpStart, 0);

                    _Life[0] = (int)(100 * ReadFloat(GameProcess_ModuleBaseAddress + _P1_Life_Offset));

                    //Can't use original digits for Ammo because the game use it to display continue countdown                
                    _Ammo[0] = (int)ReadFloat(GameProcess_ModuleBaseAddress + _P1_Ammo_Offset);

                    //[Damaged] custom Output                
                    if (_Life[0] < _LastLife[0])
                        SetOutputValue(OutputId.P1_Damaged, 1);
                }
                else
                {
                    //Enable Start Lamp Blinking
                    SetOutputValue(OutputId.P1_CtmLmpStart, -1);
                }

                if (P2_Status == 4)
                {
                    //Force Start Lamp to Off
                    SetOutputValue(OutputId.P2_CtmLmpStart, 0);

                    _Life[1] = (int)(100 * ReadFloat(GameProcess_ModuleBaseAddress + _P2_Life_Offset));

                    //Can't use original digits for Ammo because the game use it to display continue countdown                
                    _Ammo[1] = (int)ReadFloat(GameProcess_ModuleBaseAddress + _P2_Ammo_Offset);

                    //[Damaged] custom Output                
                    if (_Life[1] < _LastLife[1])
                        SetOutputValue(OutputId.P2_Damaged, 1);
                }
                else
                {
                    //Enable Start Lamp Blinking
                    SetOutputValue(OutputId.P2_CtmLmpStart, -1);
                }
            }

            //Custom Gun Recoil output
            //Based on genuine output but sure not to be missed because of too short pulse timing
            if ((byte)(ReadByte(_Recoil_CaveAddress) & 0x01) == 1)
            {
                SetOutputValue(OutputId.P1_CtmRecoil, 1);
                Apply_AND_ByteMask(_Recoil_CaveAddress, 0x0A);
            }

            if ((byte)(ReadByte(_Recoil_CaveAddress) >> 1 & 0x01) == 1)
            {
                SetOutputValue(OutputId.P2_CtmRecoil, 1);
                Apply_AND_ByteMask(_Recoil_CaveAddress, 0x05);
            }

            for (uint i = 0; i < _GameConfig.PlayerCount; i++)
            {
                _LastLife[i] = _Life[i];
                _LastAmmo[i] = _Ammo[i];

                SetOutputValue(OutputId.P1_Ammo + i, _Ammo[i]);
                SetOutputValue(OutputId.P1_Life + i, _Life[i]);
            }

            //Credits are dispatched between 4 coin entries
            int Credits = 0;
            for (uint i = 0; i < 4; i++)
            {
                byte b = ReadByte(GameProcess_ModuleBaseAddress + _CreditsBase_Offset + (i * 0x78));
                Credits += b;
            }
            SetOutputValue(OutputId.Credits, Credits);
        }

        #endregion
    }
}
