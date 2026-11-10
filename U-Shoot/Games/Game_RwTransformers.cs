using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
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
    public class Game_RwTransformers : BaseGame32
    {
        protected override bool PATCH_PEB_DEBUG_FLAG { get { return false; } }

        private const string RING_DATA_FOLDER = "sv";
        private const string RINGIODLL_NAME = "Ring_IO.dll";
        private UInt32 _RingIoDll_BaseAddress;

        #region LOADER MEMORY ADDRESSES                
        private UInt32 _WindowedMode_Patch_Offset = 0x0006752B;
        private InjectionStruct32 _NoCursor_InjectionStruct = new InjectionStruct32(0x00420358, 6);
        private InjectionStruct32 _NoCrosshair_InjectionStruct = new InjectionStruct32(0x000162FE7, 8);
        #endregion

        #region INPUTS MEMORY ADDRESSES    
        private UInt32 _InitialCall_Offset = 0x00440BEF;
        private UInt32 _LoadLibraryA_Ptr_Offset = 0x005090EC;
        private UInt32 _RingIo_DataConfigBuffer_Offset = 0x00054F10;
        private UInt32 _RIngIo_CreditsMod_Offset = 0x0001F450;
        private UInt32 _CalibrationData_Offset = 0x009BB890;
        private InjectionStruct32 _BootLoader_InjectionStruct = new InjectionStruct32(0x0042DD6B, 5);
        private InjectionStruct32 _RingConfig_InjectionStruct = new InjectionStruct32(0x000014D2, 5);
        private InjectionStruct32 _Calibration_InjectionStruct = new InjectionStruct32(0x0002CEEB, 7);
        private InjectionStruct32 _Axes_InjectionStruct = new InjectionStruct32(0x0029032B, 5);
        private InjectionStruct32 _Buttons_InjectionStruct = new InjectionStruct32(0x00290127, 7);
        private InjectionStruct32 _Credits_InjectionStruct = new InjectionStruct32(0x00290240, 5);
        private BreakpointStruct32 _RingIoUnpacked_Breakpointstruct = new BreakpointStruct32(0x634DD, 5);

        //Custom Data
        private UInt32 _Axes_CaveAddress;
        private UInt32 _Buttons_CaveAddress;
        private UInt32 _Credits_CaveAddress;
        private UInt32 _RingIoDll_Path_CaveAddress;
        private UInt32 _RingConfigData_CaveAddress;
        #endregion

        #region OUTPUTS MEMORY ADDRESSES
        private UInt32 _OutputsPtr_Offset = 0x1A3CBA0;
        private UInt32 _Credits_Offset = 0x00964E60;
        private UInt32 _PlayersPtr_Offset = 0x1A3CC1C;
        private InjectionStruct32 _P1_Recoil_InjectionStruct = new InjectionStruct32(0x0002D51D, 5);
        private InjectionStruct32 _P2_Recoil_InjectionStruct = new InjectionStruct32(0x0002D54D, 5);

        //Custom Data
        private UInt32 _RecoilStatus_CaveAddress;
        #endregion

        public Game_RwTransformers(GameConfig Config) : base(Config)
        {
            _KnownMd5Prints.Add("Transformers Human Alliance  - Original Dump", "7e11f7e78ed566a277edba1a8aab0749");
            _KnownMd5Prints.Add("Transformers Human Alliance  - For JConfig", "0d23fead523ea91eaea5047e652dff69");

            //RConfig needs "sv\E2PROM.bin" and "sv\SRAM.bin" or the game won't start.
            //Those file will not be created by the game if not present, thus they need to be copied if missing
            string E2promPath = _GameConfig.TargetPath.Replace(Path.GetFileName(_GameConfig.TargetFileName), RING_DATA_FOLDER + "\\E2PROM.bin");
            string SramPath = _GameConfig.TargetPath.Replace(Path.GetFileName(_GameConfig.TargetFileName), RING_DATA_FOLDER + "\\SRAM.bin");

            if (!File.Exists(E2promPath) || !File.Exists(SramPath))
            {
                try
                {
                    Logger.WriteLog("RConfig E2PROM or SRAM not found, copying default ones....");
                    string ZipPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Properties.Resources.DATA_PAYLOADS_FOLDER, "RConfig", "tha.zip");
                    using (ZipArchive source = ZipFile.Open(ZipPath, ZipArchiveMode.Read, null))
                    {
                        foreach (ZipArchiveEntry entry in source.Entries)
                        {
                            string fullPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(_GameConfig.TargetPath), entry.FullName));

                            //Creating "sv" folder if missing
                            if (string.IsNullOrEmpty(entry.Name))
                            {
                                if (!Directory.Exists(fullPath))
                                    Directory.CreateDirectory(fullPath);

                                continue;
                            }

                            //Only extracting missing files to keep older ones
                            if (!File.Exists(fullPath))
                                entry.ExtractToFile(fullPath, true);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.WriteLog("Extraction error : " + ex.Message.ToString());
                }
                Logger.WriteLog("Extraction complete !");
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
                    if ((UInt32)e.ExceptionInfo.ExceptionRecord.ExceptionAddress == _RingIoDll_BaseAddress + _RingIoUnpacked_Breakpointstruct.MemoryOffset)
                    {
                        Logger.WriteLog(" > RingIO.dll unpacked breakpoint reached");
                        if (!RemoveBreakpoint(_RingIoUnpacked_Breakpointstruct, e.dwThreadId, _RingIoDll_BaseAddress))
                            return;

                        e.Handled = true;
                        SetHack_RingIOConfig();
                        //When overriding the EEPROM functions, RConfig replaces credits informations from the EEPROM by it's own
                        //Jumping over that so that genuine EEPROM data can be used (Freeplay, Credits to start, etc..)
                        WriteBytes(_RingIoDll_BaseAddress + _RIngIo_CreditsMod_Offset, new byte[] { 0x90, 0xE9 });
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
                if (string.Equals(Path.GetFileName(e.DllPath), RINGIODLL_NAME, StringComparison.OrdinalIgnoreCase))
                {
                    Logger.WriteLog("==> Wanted DLL found !");
                    //HANDLE should always be in 32Bits range (https://learn.microsoft.com/fr-fr/windows/win32/winprog64/interprocess-communication?redirectedfrom=MSDN)
                    Logger.WriteLog("  + hModule: 0x" + e.LoadDllInfo.hFile.ToString("X8"));
                    Logger.WriteLog("  + fUnicode: " + e.LoadDllInfo.fUnicode.ToString());

                    //At that point, the loaded dll has only the UPX stub that will unpack the original code
                    //Setting a breakpoint at the end of the unpacking process will allow to access original code bytes and mod it
                    _RingIoDll_BaseAddress = (UInt32)e.LoadDllInfo.lpBaseOfDll;
                    SetBreakpoint(_RingIoUnpacked_Breakpointstruct, _RingIoDll_BaseAddress);
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

                    //Player 1 and 2 axis limits are different, and we can't access the TEST menu to do calibration
                    //Choosen solution is to force Calibration Values for Min-Max axis to [0x00-0xFF] when we write axis values in memory
                    //So we can safely use full range of values now :
                    double dMinX = 0;
                    double dMinY = 0;
                    double dMaxX = 255.0;
                    double dMaxY = 255.0;
                    double dRangeX = dMaxX - dMinX + 1;
                    double dRangeY = dMaxY - dMinY + 1;

                    PlayerController.Computed_X = Convert.ToInt32(dMaxX - Math.Round(dRangeX * PlayerController.Computed_X / TotalResX));
                    PlayerController.Computed_Y = Convert.ToInt32(dMaxY - Math.Round(dRangeY * PlayerController.Computed_Y / TotalResY));
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

            //GameHooked event will be triggered once the RConfig DLL has been loaded if Input support is checked
            //Overwise, it needs to be done now
            if (_GameConfig.NoInputs)
                RaiseGameHookedEvent();
        }

        #region Loader Hack

        protected override void Apply_LoaderMemoryHack()
        {
            if (_GameConfig.Windowed)
            {
                //Flag to set window as WS_POPUP (fullscreen) is hardcoded to 0, just changing that to "1"
                //With that flag set to 1, dWStyle will be usual WS_OVERLAPPEDWINDOW
                WriteByte(GameProcess_ModuleBaseAddress + _WindowedMode_Patch_Offset, 0x01);
            }

            if (_GameConfig.Fullscreen)
            {
                //DirectX initialization forces the game to use WINDOWED mode (borderless window) on a popup window
                //Changing it to FULLSCREEN is possible but the game will not scale.....
            }

            if (_GameConfig.NoCursor)
            {
                if (CreateTransparentCursor())
                    SetHack_NoCursor();
            }

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        /// <summary>
        /// Replacing call to LoadCursor() by  setting hCursor value in EAX
        /// </summary>
        private void SetHack_NoCursor()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //mov eax, _CustomTransparentCursorHandle
            CaveMemory.Write_StrBytes("B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes((UInt32)_CustomTransparentCursorHandle));
            //add esp, 8
            CaveMemory.Write_StrBytes("83 C4 08");

            //Inject it
            CaveMemory.InjectToOffset(_NoCursor_InjectionStruct, "No Cursor");
        }

        #endregion        

        #region Inputs Hack

        protected override void Apply_InputsMemoryHack()
        {
            Create_InputsDataBank();
            _Axes_CaveAddress = InputsDatabank_Address;     //Game is reading for each player, 2x2bytes for each (= 16 in total)
            _Buttons_CaveAddress = InputsDatabank_Address + 0x10;
            _Credits_CaveAddress = InputsDatabank_Address + 0x18;
            _RingIoDll_Path_CaveAddress = InputsDatabank_Address + 0x20;   //char[255]
            _RingConfigData_CaveAddress = InputsDatabank_Address + 0x120; //244 Bytes

            //Writing the DLL path to load it with LoadLibraryA();
            string RingIoDllPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Properties.Resources.DATA_PAYLOADS_FOLDER, @"RConfig\Ring_IO.dll");
            WriteBytes(_RingIoDll_Path_CaveAddress, Encoding.ASCII.GetBytes(RingIoDllPath));

            //Fedding the content of ring_config.bin directly to the DLL, removing the need of the file
            //This is almost "no options activated at all" config for RConfig.
            WriteBytes(_RingConfigData_CaveAddress, new byte[]{
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFA, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x3C, 0x00, 0x00, 0x00, 0x28, 0x00, 0x00, 0x00, 0x10, 0x00, 0x00, 0x00, 0x00, 0x05, 0x00, 0x00, 0xD0, 0x02, 0x00, 0x00, 0x00, 0x05, 0x00, 0x00, 0xD0, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x06, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x1B, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
            });

            SetHack_Bootloader();
            SetHack_Calibration();
            SetHack_Axis();
            SetHack_Buttons();
            SetHack_Credits();

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        /// <summary>
        /// At .exe entry, adding a small codecave to call LoadLibraryA() with RConfig dll
        /// </summary>
        private void SetHack_Bootloader()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //push _RingIoDll_Path_CaveAddress
            CaveMemory.Write_StrBytes("68");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_RingIoDll_Path_CaveAddress));
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
        private void SetHack_RingIOConfig()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, _RingIoDll_BaseAddress);

            //push edi
            CaveMemory.Write_StrBytes("57");
            //push ecx
            CaveMemory.Write_StrBytes("51");
            //mov esi,_RingConfigData_CaveAddress
            CaveMemory.Write_StrBytes("BE");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_RingConfigData_CaveAddress));
            //mov edi,LGI.exe+_RingIo_DataConfigBuffer_Offset
            CaveMemory.Write_StrBytes("BF");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_RingIoDll_BaseAddress + _RingIo_DataConfigBuffer_Offset));
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
            CaveMemory.InjectToOffset(_RingConfig_InjectionStruct, "RingConfig data");
        }

        /// <summary>
        /// After the game has loaded backup SRAM data, we can safely override Calibration values to make it the full 00-FF range available from JVS
        /// </summary>
        private void SetHack_Calibration()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //mov [TF_Gun_R_Ring_dumped.exe+9BB890],FF00FF00
            CaveMemory.Write_StrBytes("C7 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _CalibrationData_Offset));
            CaveMemory.Write_StrBytes("00 FF 00 FF");
            //mov [TF_Gun_R_Ring_dumped.exe+9BB898],FF00FF00
            CaveMemory.Write_StrBytes("C7 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _CalibrationData_Offset + 8));
            CaveMemory.Write_StrBytes("00 FF 00 FF");
            //mov edx,[edi+10]
            CaveMemory.Write_StrBytes("8B 57 10");
            //push edx
            CaveMemory.Write_StrBytes("52");
            //lea ebx,[edi+48]
            CaveMemory.Write_StrBytes("8D 5F 48");

            //Inject it
            CaveMemory.InjectToOffset(_Calibration_InjectionStruct, "Calibration");
        }

        /// <summary>
        /// Instead of reading the JVS buffer input, reading custom buffer
        /// </summary>
        private void SetHack_Axis()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //mov ecx,[esp]
            CaveMemory.Write_StrBytes("8B 0C 24");
            //shl ecx,1
            CaveMemory.Write_StrBytes("D1 E1");
            //add ecx,_Axes_CaveAddress
            CaveMemory.Write_StrBytes("81 C1");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Axes_CaveAddress));
            //mov ecx,[ecx]
            CaveMemory.Write_StrBytes("8B 09");
            //and ecx,0000FFFF
            CaveMemory.Write_StrBytes("81 E1 FF FF 00 00");
            //mov [ebx],cx
            CaveMemory.Write_StrBytes("66 89 0B");
            //xor eax,eax
            CaveMemory.Write_StrBytes("31 C0");

            //Inject it
            CaveMemory.InjectToOffset(_Axes_InjectionStruct, "Axes");
        }

        /// <summary>
        /// Instead of reading the JVS buffer input, reading custom buffer
        /// </summary>
        private void SetHack_Buttons()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //lea ecx,[esi-5]
            CaveMemory.Write_StrBytes("8D 4E FB");
            //add ecx,_Buttons_CaveAddress
            CaveMemory.Write_StrBytes("81 C1");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Buttons_CaveAddress));

            //Inject it
            CaveMemory.InjectToOffset(_Buttons_InjectionStruct, "Buttons");
        }

        /// <summary>
        /// Instead of reading the JVS buffer input, reading custom buffer
        /// </summary>
        private void SetHack_Credits()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //mov eax,[esp+10]
            CaveMemory.Write_StrBytes("8B 44 24 10");
            //sub eax,01
            CaveMemory.Write_StrBytes("83 E8 01");
            //shl eax,02
            CaveMemory.Write_StrBytes("C1 E0 02");
            //add eax,_Credits_CaveAddress
            CaveMemory.Write_StrBytes("05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Credits_CaveAddress));
            //mov eax,[eax]
            CaveMemory.Write_StrBytes("8B 00");
            //mov [ebx],ax
            CaveMemory.Write_StrBytes("66 89 03");
            //xor eax,eax
            CaveMemory.Write_StrBytes("31 C0");

            //Inject it
            CaveMemory.InjectToOffset(_Credits_InjectionStruct, "Credits");
        }

        #endregion

        #region Outputs Hack

        protected override void Apply_OutputsMemoryHack()
        {
            Create_OutputsDataBank();
            _RecoilStatus_CaveAddress = OutputsDatabank_Address;

            SetHack_Recoil_P1();
            SetHack_Recoil_P2();

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        /// <summary>
        /// The game is checking if Recoil is enabled for each bullet fired.
        /// Using this request call, we can generate the start of our own CustomRecoil output event
        /// </summary>
        private void SetHack_Recoil_P1()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            List<Byte> Buffer = new List<Byte>();
            //mov byte ptr[_RecoilStatus_CaveAddress], 1
            CaveMemory.Write_StrBytes("C6 05");
            byte[] b = BitConverter.GetBytes(_RecoilStatus_CaveAddress);
            CaveMemory.Write_Bytes(b);
            CaveMemory.Write_StrBytes("01");
            //mov eax,[ecx+34]
            CaveMemory.Write_StrBytes("8B 41 34");
            //test eax, eax
            CaveMemory.Write_StrBytes("85 C0");

            //Inject it
            CaveMemory.InjectToOffset(_P1_Recoil_InjectionStruct, "P1 Recoil");

        }
        private void SetHack_Recoil_P2()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //mov byte ptr[_RecoilStatus_CaveAddress + 1], 1
            CaveMemory.Write_StrBytes("C6 05");
            byte[] b = BitConverter.GetBytes(_RecoilStatus_CaveAddress + 1);
            CaveMemory.Write_Bytes(b);
            CaveMemory.Write_StrBytes("01");
            //mov eax,[ecx+38]
            CaveMemory.Write_StrBytes("8B 41 38");
            //test eax, eax
            CaveMemory.Write_StrBytes("85 C0");

            //Inject it
            CaveMemory.InjectToOffset(_P2_Recoil_InjectionStruct, "P1 Recoil");
        }

        #endregion

        #region No Crosshair Hack

        /// <summary>
        /// Replacing the float computed coordinate for X crosshair position by -100.0 to tremove it from screen
        /// </summary>
        protected override void Apply_NoCrosshairMemoryHack()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //fstp dword ptr [esp+04]
            CaveMemory.Write_StrBytes("D9 5C 24 04");
            //fld dword ptr [esp+18]
            CaveMemory.Write_StrBytes("D9 44 24 18");
            //mov [esp+04],C2C80000
            CaveMemory.Write_StrBytes("C7 44 24 04 00 00 C8 C2");

            //Inject it
            CaveMemory.InjectToOffset(_NoCrosshair_InjectionStruct, "No Crosshair");

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
            else if (e.ButtonName.Equals("COIN MECH. 1"))
            {
                byte b = ReadByte(_Credits_CaveAddress);
                WriteByte(_Credits_CaveAddress, (byte)(b + 1));
            }
            else if (e.ButtonName.Equals("COIN MECH. 2"))
            {
                byte b = ReadByte(_Credits_CaveAddress + 0x04);
                WriteByte(_Credits_CaveAddress + 4, (byte)(b + 1));
            }
            else if (e.ButtonName.Equals("SERVICE"))
            {
                Apply_OR_ByteMask(_Buttons_CaveAddress + 0x01, 0x40);
            }
            else if (e.ButtonName.Equals("TEST"))
            {

            }
        }

        protected override void GameController_OnButtonUp(object sender, VirtualGameControler.ButtonDownEventArgs e)
        {
            if (e.ButtonName.Equals("SERVICE"))
            {
                Apply_AND_ByteMask(_Buttons_CaveAddress + 0x01, 0xBF);
            }
        }

        protected override void UpdateAxis(PlayerID Id, int X, int Y)
        {
            if (Id != PlayerID.None)
            {
                byte[] bufferX = BitConverter.GetBytes(X);
                byte[] bufferY = BitConverter.GetBytes(Y);

                WriteByte(_Axes_CaveAddress + 3 + ((uint)Id * 0x04), bufferX[0]);
                WriteByte(_Axes_CaveAddress + 1 + ((uint)Id * 0x04), bufferY[0]);
            }
        }

        protected override void PlayerController_OnButtonDown(object sender, VirtualGameControler.ButtonDownEventArgs e)
        {
            if (e.Id != PlayerID.None)
            {
                UInt32 PlayerIndex = (UInt32)e.Id;

                if (e.ButtonName.Equals("TRIGGER"))
                {
                    Apply_OR_ByteMask(_Buttons_CaveAddress + 1 + (PlayerIndex * 0x02), 0x02);
                }
                else if (e.ButtonName.Equals("START"))
                {
                    Apply_OR_ByteMask(_Buttons_CaveAddress + 1 + (PlayerIndex * 0x02), 0x80);
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
                    Apply_AND_ByteMask(_Buttons_CaveAddress + 1 + (PlayerIndex * 0x02), 0xFD);
                }
                else if (e.ButtonName.Equals("START"))
                {
                    Apply_AND_ByteMask(_Buttons_CaveAddress + 1 + (PlayerIndex * 0x02), 0x7F);
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
                new GameOutput(OutputId.P1_LmpGun),
                new GameOutput(OutputId.P2_LmpGun),
                new GameOutput(OutputId.LmpBillboard),
                new GameOutput(OutputId.P1_LmpBreak),
                new GameOutput(OutputId.P2_LmpBreak),
                new GameOutput(OutputId.P1_GunMotor),
                new GameOutput(OutputId.P2_GunMotor),
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
            UInt32 _Outputs_Address = ReadPtr(GameProcess_ModuleBaseAddress + _OutputsPtr_Offset) + 0x08;
            SetOutputValue(OutputId.P1_LmpStart, ReadByte(_Outputs_Address) >> 7 & 0x01);
            SetOutputValue(OutputId.P2_LmpStart, ReadByte(_Outputs_Address) >> 4 & 0x01);
            SetOutputValue(OutputId.P1_LmpGun, ReadByte(_Outputs_Address) >> 1 & 0x01);
            SetOutputValue(OutputId.P2_LmpGun, ReadByte(_Outputs_Address) & 0x01);
            SetOutputValue(OutputId.LmpBillboard, ReadByte(_Outputs_Address + 1) >> 2 & 0x01);
            SetOutputValue(OutputId.P1_LmpBreak, ReadByte(_Outputs_Address + 1) >> 7 & 0x01);
            SetOutputValue(OutputId.P2_LmpBreak, ReadByte(_Outputs_Address + 1) >> 6 & 0x01);
            //There are others lamps (Logo, Sides ? RGB Leds ?) coded on [Outputs_Address + 1] 0x20, 0x10, 0x08
            //But without Test menu I can't know how these one are working
            //Besides, lamps name are different on the TestMode binary than on any Arcade Manual I could find...
            SetOutputValue(OutputId.P1_GunMotor, ReadByte(_Outputs_Address) >> 6 & 0x01);
            SetOutputValue(OutputId.P2_GunMotor, ReadByte(_Outputs_Address) >> 3 & 0x01);

            //custom Outputs  
            for (uint i = 0; i < _GameConfig.PlayerCount; i++)
            {
                _Life[i] = 0;

                UInt32 PlayersPtr_BaseAddress = ReadPtr(GameProcess_ModuleBaseAddress + _PlayersPtr_Offset);
                if (PlayersPtr_BaseAddress != 0)
                {
                    UInt32 Player_StructAddress = ReadPtr(PlayersPtr_BaseAddress + 0x34 + (i * 0x04));
                    //Player Status :
                    //3: Game
                    //4: Cutscene
                    //15: Contnue
                    int Player_Status = ReadByte(Player_StructAddress + 0x5C);

                    if (Player_Status == 3 || Player_Status == 4)
                    {
                        _Life[i] = ReadByte(Player_StructAddress + 0x60);

                        //[Damaged] custom Output                
                        if (_Life[i] < _LastLife[i])
                            SetOutputValue(OutputId.P1_Damaged + i, 1);
                    }

                    //Custom Recoil will simply be read on memory and reset
                    //the codecave injected will update it for the "ON" state
                    //Custom Recoil will be read by our own codecave
                    if (ReadByte(_RecoilStatus_CaveAddress + i) == 1)
                    {
                        SetOutputValue(OutputId.P1_CtmRecoil + i, 1);
                        WriteByte(_RecoilStatus_CaveAddress + i, 0);
                    }
                }

                _LastLife[i] = _Life[i];

                SetOutputValue(OutputId.P1_Life + i, _Life[i]);
            }

            SetOutputValue(OutputId.Credits, ReadByte(GameProcess_ModuleBaseAddress + _Credits_Offset));
        }

        #endregion
    }
}
