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
    public class Game_NuLuigiMansion : BaseGame64
    {
        //protected override string GAMEDATA_FOLDER { get { return @"XXXXX\YYYYY"; } }
        protected override bool PATCH_PEB_DEBUG_FLAG { get { return false; } }

        private const string JCONFIG_DATA_FOLDER = "sv";
        private const string JCONFIGDLL_NAME = "JVSEmuLM.dll";
        private UInt64 _JConfigDll_BaseAddress;

        #region LOADER MEMORY ADDRESSES     
        private UInt64 _Resolution_Patch_Offset = 0x0015FF71;
        private UInt64 _LoadedResolution_Patch_Offset = 0x0015FE29;
        private UInt64 _FullscreenDisabled_Patch_OFfset = 0x00173E41;
        private UInt64 _WindowedMode_Patch_Offset = 0x00173E89;
        //sub_14011DAD0() => check byte for screen status
        //sub_14011D5A0() => sets the flag to "1" for fullscreen --> call sub_1401736C0() => sets the flag to "1" --> call sub_14015FF20() to create memory and +15FE00() to modify it
        //--------->+15FF6E = Default resolution
        #endregion

        #region INPUTS MEMORY ADDRESSES    
        private UInt64 _CoinIn_Ptr_Offset = 0x004F6C90;
        private UInt64 _InitialCall_Offset = 0x00327F68;
        private UInt64 _LoadLibraryW_Ptr_Offset = 0x00357268; 
        private UInt64 _JConfig_SettingsBuffer_Offset = 0x00046C30; 
        private UInt64 _JConfig_InputsBuffer_Offset = 0x00046D90;
        private InjectionStruct64 _BootLoader_InjectionStruct = new InjectionStruct64(0x356AB2, 17);/*0x0030FB70*/
        private InjectionStruct64 _JconfigSettings_InjectionStruct = new InjectionStruct64(0x0000152C, 27);
        private InjectionStruct64 _JconfigInputs_InjectionStruct = new InjectionStruct64(0x000041E0, 17);
        private BreakpointStruct64 _JConfigUnpacked_Breakpointstruct = new BreakpointStruct64(0x52A53, 5);

        //custom Data
        private UInt64 _JConfigDll_Path_CaveAddress;
        private UInt64 _JConfigSettingsData_CaveAddress;
        private UInt64 _JConfigInputsData_CaveAddress;
        private UInt64 _SystemButtons_CaveAddress = 0;
        private UInt64 _GunButtons_CaveAddress = 0;
        private UInt64 _CoinInput_CaveAddress = 0;
        private UInt64 _InScreen_CaveAddress = 0;
        private UInt64 _Axes_CaveAddress = 0;
        private InjectionStruct64 _Inputs_InjectionStruct = new InjectionStruct64(0x00017E70, 20);
        #endregion

        #region OUTPUTS MEMORY ADDRESSES
        private UInt64 _OutputsBasePtr_Offset = 0x004F6C98;
        private UInt64 _LiveOutputsPtr_Offset = 0x004FB0C0;
        private UInt64 _Credits_Offset = 0x004E19A8;
        #endregion

        //Default game resolution is 1920x1080
        private int _ResolutionWidth = 1920;
        private int _ResolutionHeight = 1080;

        public Game_NuLuigiMansion(GameConfig Config) : base(Config)
        {
            _KnownMd5Prints.Add("VACUUM.EXE - Original Dump", "5120bbe464b35f4cc894238bd9f9e11b");
            _KnownMd5Prints.Add("VACUUM.EXE - 'SpeedFix'", "8ddfab1cd2140670d9437738c9c331c8");
            _KnownMd5Prints.Add("VACUUM.EXE - For JConfig", "63c70cf8b080c1972e9e753f258e9507");

            //JConfig needs "sv\E2PROM.bin" and "sv\SRAM.bin" or the game won't start.
            //Those file will not be created by the game if not present, thus they need to be copied if missing
            string E2promPath = _GameConfig.TargetPath.Replace(Path.GetFileName(_GameConfig.TargetFileName), JCONFIG_DATA_FOLDER + "\\E2PROM.bin");
            string SramPath = _GameConfig.TargetPath.Replace(Path.GetFileName(_GameConfig.TargetFileName), JCONFIG_DATA_FOLDER + "\\SRAM.bin");

            if (!File.Exists(E2promPath) || !File.Exists(SramPath))
            {
                try
                {
                    Logger.WriteLog("JConfig E2PROM or SRAM not found, copying default ones....");
                    string ZipPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Properties.Resources.DATA_PAYLOADS_FOLDER, "JConfig", _GameConfig.RomName + ".zip");
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
            
            if (e.ExceptionInfo.ExceptionRecord.ExceptionCode == Win32.EXCEPTION_BREAKPOINT)
            {                
                if (_IsFirstException)
                {
                    Logger.WriteLog(" > Initital Breakpoint");
                    Apply_MemoryHacks();
                    _IsFirstException = false;
                }
                else
                {
                    if ((UInt64)e.ExceptionInfo.ExceptionRecord.ExceptionAddress == _JConfigDll_BaseAddress + _JConfigUnpacked_Breakpointstruct.MemoryOffset)
                    {
                        Logger.WriteLog(" > JVSEmuLM.dll unpacked breakpoint reached");
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
                    _JConfigDll_BaseAddress = (UInt64)e.LoadDllInfo.lpBaseOfDll;
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

            //GameHooked event will be raised after a Breakpoint, once the DLL is fully loaded
            if (_GameConfig.NoInputs)
                RaiseGameHookedEvent();
        }

        #region Loader Hack

        protected override void Apply_LoaderMemoryHack()
        {
            _ResolutionWidth = _GameConfig.ResolutionWidth;
            _ResolutionHeight = _GameConfig.ResolutionHeight;

            //Set default resolution param
            WriteBytes(GameProcess_ModuleBaseAddress + _Resolution_Patch_Offset, BitConverter.GetBytes(_ResolutionWidth));
            WriteBytes(GameProcess_ModuleBaseAddress + _Resolution_Patch_Offset + 0x07, BitConverter.GetBytes(_ResolutionHeight));
            WriteByte(GameProcess_ModuleBaseAddress + _Resolution_Patch_Offset + 0x0E, _GameConfig.Fullscreen ? (byte)1 : (byte)0);

            //Remove overwritting default resolution with constructed data from ???
            WriteBytes(GameProcess_ModuleBaseAddress + _LoadedResolution_Patch_Offset, new byte[] { 0x90, 0x90, 0x90 });          //width 
            WriteBytes(GameProcess_ModuleBaseAddress + _LoadedResolution_Patch_Offset + 0x0D, new byte[] { 0x90, 0x90, 0x90, 0x90 });    //Height
            WriteBytes(GameProcess_ModuleBaseAddress + _LoadedResolution_Patch_Offset + 0x1C, new byte[] { 0x90, 0x90, 0x90, 0x90 });    //Windowed

            //Skip kind of fullscreen force resolution -- Can also write 0 to the bit tested instead of noppin
            WriteByte(GameProcess_ModuleBaseAddress + _FullscreenDisabled_Patch_OFfset, 0xEB);

            //Change window style, if windowed
            if (_GameConfig.Windowed)
                WriteBytes(GameProcess_ModuleBaseAddress + _WindowedMode_Patch_Offset, BitConverter.GetBytes(0x00CF0000));

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }
        #endregion        

        #region Inputs Hack

        protected override void Apply_InputsMemoryHack()
        {
            Create_InputsDataBank();
            _SystemButtons_CaveAddress = InputsDatabank_Address;
            _GunButtons_CaveAddress = InputsDatabank_Address + 0x01;            //2 Bytes
            _CoinInput_CaveAddress = InputsDatabank_Address + 0x04;
            _InScreen_CaveAddress = InputsDatabank_Address + 0x08;
            _Axes_CaveAddress = InputsDatabank_Address + 0x10;
            _JConfigDll_Path_CaveAddress = InputsDatabank_Address + 0x20;       //char[255]
            _JConfigSettingsData_CaveAddress = InputsDatabank_Address + 0x120;  //244 Bytes
            _JConfigInputsData_CaveAddress = InputsDatabank_Address + 0x220;    //408 Bytes

            //Writing the DLL path to load it with LoadLibraryA();
            string JconfigDllPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Properties.Resources.DATA_PAYLOADS_FOLDER, "JConfig", JCONFIGDLL_NAME);
            WriteBytes(_JConfigDll_Path_CaveAddress, Encoding.Unicode.GetBytes(JconfigDllPath));

            //Fedding the content of ring_config.bin directly to the DLL, removing the need of the file
            //This is almost "no options activated at all" config for JConfig.
            WriteBytes(_JConfigSettingsData_CaveAddress, new byte[]
            {
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x3C, 0x00, 0x00, 0x00, 0xA0, 0x00, 0x00, 0x00, 0x28, 0x00, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x40, 0x02, 0x00, 0x00, 0x80, 0x07, 0x00, 0x00, 0x38, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
            });

            SetHack_Bootloader();

            SetHack_Inputs();
            WriteByte(_InScreen_CaveAddress, 0x01);
            WriteByte(_InScreen_CaveAddress + 1, 0x01);

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        /// <summary>
        /// At .exe entry, adding a small codecave to call LoadLibraryA() with JConfig dll
        /// </summary>
        private void SetHack_Bootloader()
        {
            Codecave64 CaveMemory = new Codecave64(_Process, GameProcess_ModuleBaseAddress);

            //sub rsp,28
            CaveMemory.Write_StrBytes("48 83 EC 28");
            //mov rcx, _JConfigDll_Path_CaveAddress
            CaveMemory.Write_StrBytes("48 B9");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_JConfigDll_Path_CaveAddress));
            //mov rax, VACUUM.exe+_LoadLibraryA_Ptr_Offset
            CaveMemory.Write_StrBytes("48 B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _LoadLibraryW_Ptr_Offset));
            //call qword ptr [rax]
            CaveMemory.Write_StrBytes("FF 10");
            //sub rsp,28
            CaveMemory.Write_StrBytes("48 83 EC 28");
            //mov rax, VACUUM.exe+_InitialCall_Offset
            CaveMemory.Write_StrBytes("48 B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _InitialCall_Offset));
            //call rax
            CaveMemory.Write_StrBytes("FF D0");
            //add rsp,28
            CaveMemory.Write_StrBytes("48 83 C4 28");
            //jmp VACUUM.exe+30FB79
            CaveMemory.Write_StrBytes("FF 25 00 00 00 00");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + 0x30FB79));

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
            Codecave64 CaveMemory = new Codecave64(_Process, _JConfigDll_BaseAddress);

            //push rsi
            CaveMemory.Write_StrBytes("56");
            //push rcx
            CaveMemory.Write_StrBytes("51");
            //mov rsi,_JConfigSettingsData_CaveAddress
            CaveMemory.Write_StrBytes("48 BE");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_JConfigSettingsData_CaveAddress));
            //mov rdi,JVSemuLM.dll+_JConfig_SettingsBuffer_Offset
            CaveMemory.Write_StrBytes("48 BF");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_JConfigDll_BaseAddress + _JConfig_SettingsBuffer_Offset));
            //mov rcx,000000000000003D
            CaveMemory.Write_StrBytes("48 B9 3D 00 00 00 00 00 00 00");
            //repe movsd
            CaveMemory.Write_StrBytes("F3 A5");
            //pop rcx
            CaveMemory.Write_StrBytes("59");
            //pop rsi
            CaveMemory.Write_StrBytes("5E");
            //mov rax,FFFFFFFFFFFFFFFF
            CaveMemory.Write_StrBytes("48 B8 FF FF FF FF FF FF FF FF");

            //Inject it
            CaveMemory.InjectToOffset(_JconfigSettings_InjectionStruct, "JConfig Settings");
        }

        /// <summary>
        /// Replace reading "monitor_input.bin" by a custom buffer
        /// </summary>
        private void SetHack_JConfigInputs()
        {
            Codecave64 CaveMemory = new Codecave64(_Process, _JConfigDll_BaseAddress);

            //push rdi
            CaveMemory.Write_StrBytes("57");
            //push rsi
            CaveMemory.Write_StrBytes("56");
            //push rcx
            CaveMemory.Write_StrBytes("51");
            //mov rsi,_JConfigInputsData_CaveAddress
            CaveMemory.Write_StrBytes("48 BE");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_JConfigInputsData_CaveAddress));
            //mov rdi,JVSemuLM.dll+_JConfig_InputsBuffer_Offset
            CaveMemory.Write_StrBytes("48 BF");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_JConfigDll_BaseAddress + _JConfig_InputsBuffer_Offset));
            //mov rcx,0000000000000066
            CaveMemory.Write_StrBytes("48 B9 66 00 00 00 00 00 00 00");
            //repe movsd
            CaveMemory.Write_StrBytes("F3 A5");
            //pop rcx
            CaveMemory.Write_StrBytes("59");
            //pop rsi
            CaveMemory.Write_StrBytes("5E");
            //pop rdi
            CaveMemory.Write_StrBytes("5F");
            //ret
            CaveMemory.Write_StrBytes("C3");

            //Inject it
            CaveMemory.InjectToOffset(_JconfigInputs_InjectionStruct, "JConfig Inputs");
        }

        /// <summary>
        /// Overriding what should be JConfig bytes data for inputs before the game reads it
        /// </summary>
        private void SetHack_Inputs()
        {
            Codecave64 CaveMemory = new Codecave64(_Process, GameProcess_ModuleBaseAddress);

            //mov r8,rax
            CaveMemory.Write_StrBytes("4C 8B C0");
            /** Copying System buttons Status **/
            //mov rax,_SystemButtons_CaveAddress
            CaveMemory.Write_StrBytes("48 B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_SystemButtons_CaveAddress));
            //mov dl,[rax]
            CaveMemory.Write_StrBytes("8A 10");
            //mov [r8+2C],dl
            CaveMemory.Write_StrBytes("41 88 50 2C");

            /** Copying Player 1 buttons Status **/
            //mov rax,_GunButtons_CaveAddress
            CaveMemory.Write_StrBytes("48 B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_GunButtons_CaveAddress));
            //mov dl,[rax]
            CaveMemory.Write_StrBytes("8A 10");
            //mov [r8+BD],dl
            CaveMemory.Write_StrBytes("41 88 90 BD 00 00 00");

            /** Copying Player 2 buttons Status **/
            //mov rax,_GunButtons_CaveAddress + 1
            CaveMemory.Write_StrBytes("48 B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_GunButtons_CaveAddress + 1));
            //mov dl,[rax]
            CaveMemory.Write_StrBytes("8A 10");
            //mov [r8+D1],dl
            CaveMemory.Write_StrBytes("41 88 90 D1 00 00 00");

            /** Copying Coin entry input Status **/
            //mov rax,_CoinInput_CaveAddress
            CaveMemory.Write_StrBytes("48 B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_CoinInput_CaveAddress));
            //mov dl,[rax]
            CaveMemory.Write_StrBytes("8A 10");
            //mov [r8+50],dl
            CaveMemory.Write_StrBytes("41 88 90 50 00 00 00");

            /** Copying In/Out screen Status **/
            //mov rax,_InScreen_CaveAddress
            CaveMemory.Write_StrBytes("48 B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_InScreen_CaveAddress));
            //mov dl,[rax]
            CaveMemory.Write_StrBytes("8A 10");
            //mov [r8+B0],dl
            CaveMemory.Write_StrBytes("41 88 90 B0 00 00 00");
            //mov rax,_InScreen_CaveAddress + 1
            CaveMemory.Write_StrBytes("48 B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_InScreen_CaveAddress + 1));
            //mov dl,[rax]
            CaveMemory.Write_StrBytes("8A 10");
            //mov [r8+C4],dl
            CaveMemory.Write_StrBytes("41 88 90 C4 00 00 00");

            /** Copying Axes **/
            //mov rax,_Axes_CaveAddress
            CaveMemory.Write_StrBytes("48 B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Axes_CaveAddress));
            //mov rdx,[rax]
            CaveMemory.Write_StrBytes("48 8B 10");
            //mov [r8+B4],rdx
            CaveMemory.Write_StrBytes("49 89 90 B4 00 00 00");
            //mov rax,_Axes_CaveAddress + 8
            CaveMemory.Write_StrBytes("48 B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Axes_CaveAddress + 8));
            //mov rdx,[rax]
            CaveMemory.Write_StrBytes("48 8B 10");
            //mov [r8+C8],rdx
            CaveMemory.Write_StrBytes("49 89 90 C8 00 00 00");

            //mov rax,r8
            CaveMemory.Write_StrBytes("49 8B C0");

            //mov edx,[rax+000000B0]
            CaveMemory.Write_StrBytes("8B 90 B0 00 00 00");
            //mov r8d,[rax+000000B4]
            CaveMemory.Write_StrBytes("44 8B 80 B4 00 00 00");
            //mov r9d,[rax+000000B8]
            CaveMemory.Write_StrBytes("44 8B 88 B8 00 00 00");

            //Inject it
            CaveMemory.InjectToOffset(_Inputs_InjectionStruct, "Inputs");
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
                Apply_OR_ByteMask(_CoinInput_CaveAddress, 0x01);
                UInt64 CoinIn_Address = ReadPtrChain(GameProcess_ModuleBaseAddress + _CoinIn_Ptr_Offset, new UInt64[] { 0 });
                if (CoinIn_Address != 0)
                    WriteByte(CoinIn_Address, 0x01);
            }
            else if (e.ButtonName.Equals("TEST"))
            {
                Apply_OR_ByteMask(_SystemButtons_CaveAddress, 0x04);
            }
            else if (e.ButtonName.Equals("SERVICE"))
            {
                Apply_OR_ByteMask(_SystemButtons_CaveAddress, 0x02);
            }
            else if (e.ButtonName.Equals("SELECT"))
            {
                Apply_OR_ByteMask(_SystemButtons_CaveAddress, 0x08);
            }
        }

        protected override void GameController_OnButtonUp(object sender, VirtualGameControler.ButtonDownEventArgs e)
        {
            if (e.ButtonName.Equals("COIN"))
            {
                Apply_AND_ByteMask(_CoinInput_CaveAddress, 0x00);
            }
            else if (e.ButtonName.Equals("TEST"))
            {
                Apply_AND_ByteMask(_SystemButtons_CaveAddress, 0xFB);
            }
            else if (e.ButtonName.Equals("SERVICE"))
            {
                Apply_AND_ByteMask(_SystemButtons_CaveAddress, 0xFD);
            }
            else if (e.ButtonName.Equals("SELECT"))
            {
                Apply_AND_ByteMask(_SystemButtons_CaveAddress, 0xF7);
            }
        }

        protected override void UpdateAxis(PlayerID Id, int X, int Y)
        {
            if (Id != PlayerID.None)
            {
                byte[] bufferX = BitConverter.GetBytes(X);
                byte[] bufferY = BitConverter.GetBytes(Y);

                WriteBytes(_Axes_CaveAddress + ((uint)Id * 8), bufferX);
                WriteBytes(_Axes_CaveAddress + 4 + ((uint)Id * 8), bufferY);
            }
        }

        protected override void PlayerController_OnButtonDown(object sender, VirtualGameControler.ButtonDownEventArgs e)
        {
            if (e.Id != PlayerID.None)
            {
                UInt32 PlayerIndex = (UInt32)e.Id;

                if (e.ButtonName.Equals("TRIGGER"))
                {
                    Apply_OR_ByteMask(_GunButtons_CaveAddress + PlayerIndex, 0x40);
                }
                else if (e.ButtonName.Equals("LEVER"))
                {
                    Apply_OR_ByteMask(_GunButtons_CaveAddress + PlayerIndex, 0x20);
                }
                else if (e.ButtonName.Equals("START"))
                {
                    Apply_OR_ByteMask(_GunButtons_CaveAddress + PlayerIndex, 0x10);
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
                    Apply_AND_ByteMask(_GunButtons_CaveAddress + PlayerIndex, 0xBF);
                }
                else if (e.ButtonName.Equals("LEVER"))
                {
                    Apply_AND_ByteMask(_GunButtons_CaveAddress + PlayerIndex, 0xDF);
                }
                else if (e.ButtonName.Equals("START"))
                {
                    Apply_AND_ByteMask(_GunButtons_CaveAddress + PlayerIndex, 0xEF);
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
                new GameOutput(OutputId.P1_Lmp_R),
                new GameOutput(OutputId.P1_Lmp_G),
                new GameOutput(OutputId.P1_Lmp_B),
                new GameOutput(OutputId.P2_Lmp_R),
                new GameOutput(OutputId.P2_Lmp_G),
                new GameOutput(OutputId.P2_Lmp_B),
                new GameOutput(OutputId.P1_LmpGun_R),
                new GameOutput(OutputId.P1_LmpGun_G),
                new GameOutput(OutputId.P1_LmpGun_B),
                new GameOutput(OutputId.P2_LmpGun_R),
                new GameOutput(OutputId.P2_LmpGun_G),
                new GameOutput(OutputId.P2_LmpGun_B),
                new GameOutput(OutputId.P1_LmpWindow_R),
                new GameOutput(OutputId.P1_LmpWindow_G),
                new GameOutput(OutputId.P1_LmpWindow_B),
                new GameOutput(OutputId.P2_LmpWindow_R),
                new GameOutput(OutputId.P2_LmpWindow_G),
                new GameOutput(OutputId.P2_LmpWindow_B),
                new GameOutput(OutputId.P1_GunMotor),
                new GameOutput(OutputId.P2_GunMotor),
                new GameOutput(OutputId.P1_GunRecoil),
                new GameOutput(OutputId.P2_GunRecoil),
                new GameOutput(OutputId.P1_Life),
                new GameOutput(OutputId.P2_Life),
                new AsyncGameOutput(OutputId.P1_Damaged, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_DAMAGED_DELAY), 100, 0),
                new AsyncGameOutput(OutputId.P2_Damaged, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_DAMAGED_DELAY), 100, 0),
                new GameOutput(OutputId.P1_Ammo),
                new GameOutput(OutputId.P2_Ammo),
                new AsyncGameOutput(OutputId.P1_CtmRecoil, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_ON_DELAY), ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_OFF_DELAY), 0),
                new AsyncGameOutput(OutputId.P2_CtmRecoil, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_ON_DELAY), ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_OFF_DELAY), 0),                
                new GameOutput(OutputId.Credits)
            };
        }

        /// <summary>
        /// Update all Outputs values before sending them to MameHooker
        /// </summary>
        public override void UpdateOutputValues()
        {
            UInt64 uOutputs1 = ReadPtrChain(GameProcess_ModuleBaseAddress + _OutputsBasePtr_Offset, new UInt64[] { 0 });
            UInt64 uOutputs2 = ReadPtrChain(GameProcess_ModuleBaseAddress + _OutputsBasePtr_Offset, new UInt64[] { 0, 0x18 });
            UInt64 uOutputs3 = ReadPtrChain(GameProcess_ModuleBaseAddress + _LiveOutputsPtr_Offset, new UInt64[] { 0x48 });

            if (uOutputs2 != 0)
            {
                SetOutputValue(OutputId.P1_LmpStart, ReadByte(uOutputs2 + 0x4C) & 0x01);
                SetOutputValue(OutputId.P2_LmpStart, ReadByte(uOutputs2 + 0x50) & 0x01);

                byte MotorValue = ReadByte(uOutputs2 + 0x2C);
                SetOutputValue(OutputId.P1_GunMotor, MotorValue * 31);
                MotorValue = ReadByte(uOutputs2 + 0x30);
                SetOutputValue(OutputId.P2_GunMotor, MotorValue * 31);

                //For recoil, there are 2 bytes that are changed.
                //The second one seems to be some kind of "duration" (3-2-1-0) of the recoil effect
                //The recoil seems to fire each time a ghost is sucked in the controller
                Byte Recoil1 = ReadByte(uOutputs2 + 0x44);
                Byte Recoil2 = ReadByte(uOutputs2 + 0x48);
                SetOutputValue(OutputId.P1_GunRecoil, Recoil1);
                SetOutputValue(OutputId.P2_GunRecoil, Recoil2);
                //Changing the real output value to a simple 0/1 recoil effect for the custom recoil output
                if (Recoil1 != 0)
                    SetOutputValue(OutputId.P1_CtmRecoil, 1);
                else
                    SetOutputValue(OutputId.P1_CtmRecoil, 0);

                if (Recoil2 != 0)
                    SetOutputValue(OutputId.P2_CtmRecoil, 1);
                else
                    SetOutputValue(OutputId.P2_CtmRecoil, 0);
            }

            if (uOutputs1 != 0)
            {
                SetOutputValue(OutputId.P1_Lmp_R, ReadByte(uOutputs1 + 0x101));
                SetOutputValue(OutputId.P1_Lmp_G, ReadByte(uOutputs1 + 0x102));
                SetOutputValue(OutputId.P1_Lmp_B, ReadByte(uOutputs1 + 0x103));
                SetOutputValue(OutputId.P2_Lmp_R, ReadByte(uOutputs1 + 0x105));
                SetOutputValue(OutputId.P2_Lmp_G, ReadByte(uOutputs1 + 0x106));
                SetOutputValue(OutputId.P2_Lmp_B, ReadByte(uOutputs1 + 0x107));
                SetOutputValue(OutputId.P1_LmpGun_R, ReadByte(uOutputs1 + 0x1AD));
                SetOutputValue(OutputId.P1_LmpGun_G, ReadByte(uOutputs1 + 0x1AE));
                SetOutputValue(OutputId.P1_LmpGun_B, ReadByte(uOutputs1 + 0x1AF));
                SetOutputValue(OutputId.P2_LmpGun_R, ReadByte(uOutputs1 + 0x1B1));
                SetOutputValue(OutputId.P2_LmpGun_G, ReadByte(uOutputs1 + 0x1B2));
                SetOutputValue(OutputId.P2_LmpGun_B, ReadByte(uOutputs1 + 0x1B3));
                SetOutputValue(OutputId.P1_LmpWindow_R, ReadByte(uOutputs1 + 0x16D));
                SetOutputValue(OutputId.P1_LmpWindow_G, ReadByte(uOutputs1 + 0x16E));
                SetOutputValue(OutputId.P1_LmpWindow_B, ReadByte(uOutputs1 + 0x16F));
                SetOutputValue(OutputId.P2_LmpWindow_R, ReadByte(uOutputs1 + 0x18D));
                SetOutputValue(OutputId.P2_LmpWindow_G, ReadByte(uOutputs1 + 0x18E));
                SetOutputValue(OutputId.P2_LmpWindow_B, ReadByte(uOutputs1 + 0x18F));                
            }

            for (uint i = 0; i < _GameConfig.PlayerCount; i++)
            {
                _Life[i] = 0;
                _Ammo[i] = 0;
            }

            //In-game values are pointer affected only during a game (not valid in menus)
            if (uOutputs3 != 0)
            {
                for (uint i = 0; i < _GameConfig.PlayerCount; i++)
                {
                    //Player status :
                    //[1] = Playing
                    //[0] = Not Playing
                    byte PlayerStatus = ReadByte(uOutputs3 + 0x548 + (i * 0x18));

                    if (PlayerStatus == 1)
                    {
                        _Life[i] = ReadByte(uOutputs3 + 0x550 + (i * 0x18));
                        _Ammo[i] = ReadByte(uOutputs3 + 0x57C + (i * 0x0C));

                        //[Damaged] custom Output                
                        if (_Life[i] < _LastLife[i])
                            SetOutputValue(OutputId.P1_Damaged, 1);
                    }

                    _LastAmmo[i] = _Ammo[i];
                    _LastLife[i] = _Life[i];

                    SetOutputValue(OutputId.P1_Ammo + i, _Ammo[i]);
                    SetOutputValue(OutputId.P1_Life + i, _Life[i]);
                }
            }

            SetOutputValue(OutputId.Credits, ReadByte(GameProcess_ModuleBaseAddress + _Credits_Offset));
        }

        #endregion
    }
}
