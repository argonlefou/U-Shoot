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
    public class Game_RwGunman : BaseGame32
    {
        protected override string GAMEDATA_FOLDER { get { return @"Ringwide\gunman"; } }
        protected override bool PATCH_PEB_DEBUG_FLAG { get { return true; } }

        private const string TESTMENU_OPTION = "TEST menu";
        private const string CREDITSADDED_OPTION = "Credits added";

        #region LOADER MEMORY ADDRESSES
        private UInt32 _Gunman_Initcask_String_Offset = 0x0051D3E8;
        private UInt32 _Gunman_ErrorMain_String_Offset = 0x00537FEC;
        private UInt32 _Gunman_ErrorSate_String_Offset = 0x00537FB4;
        private UInt32 _Gunman_LampSate_String_Offset = 0x0053CCA4;
        private UInt32 _Gunman_LampMain_String_Offset = 0x0053CF3C;
        private UInt32 _Gunman_MxgDeliver_String_Offset = 0x005415E0;
        private UInt32 _Gunman_Initialize_String_Offset = 0x00541C58;
        private UInt32 _Gunman_Initturn_String_Offset = 0x00595554;
        private UInt32 _MouseHandlingFunction_Offset = 0x34A120;
        private InjectionStruct32 _NoCursor_InjectionStruct = new InjectionStruct32(0x003429B3, 6);
        private InjectionStruct32 _P1_NoCrosshair_InjectionStruct = new InjectionStruct32(0x0014386C, 5);
        private InjectionStruct32 _P2_NoCrosshair_InjectionStruct = new InjectionStruct32(0x001436C6, 5);

        //Custom Addresses
        private UInt32 _User32_String_BankAddress;
        private UInt32 _ShowCursor_String_BankAddress;
        #endregion

        #region INPUTS MEMORY ADDRESSES 
        private UInt32 _Credits_Offset = 0x006CBB40;
        private UInt32 _pGunCylinderMgr_Offset = 0x006C9B30;
        private InjectionStruct32 _Axis_InjectionStruct = new InjectionStruct32(0x00142447, 9);
        private UInt32 _P1_TriggerPatch_Offset = 0x0034A1DD;
        private UInt32 _P1_OtherClickPatch_Offset = 0x0034A21E; //equivalent of right-click, used in TEST menu only to navigate
        private UInt32 _P1_ReloadPatch_Offset = 0x0034A25F;
        private InjectionStruct32 _P2_Buttons_InjectionStruct = new InjectionStruct32(0x349622, 6);

        //Custom Input Addresses
        private UInt32 _P1_X_Address;
        private UInt32 _P1_Y_Address;
        private UInt32 _P2_X_Address;
        private UInt32 _P2_Y_Address;
        private UInt32 _P1_Trigger_Address;
        private UInt32 _P1_Reload_Address;
        private UInt32 _P2_Trigger_Address;
        private UInt32 _P2_Reload_Address;
        private UInt32 _Reset_Address;
        #endregion

        #region OUTPUTS MEMORY ADDRESSES
        private InjectionStruct32 _Lamp_InjectionStruct = new InjectionStruct32(0x001DC8C1, 5);
        private UInt32 _CustomLamps_CaveAddress;
        #endregion

        private string _CommandLineArgs = "e_Main_MainUnit=0 e_Serial_Use=0 e_Main_DispInfo=0 e_Main_IsRenge=0 e_Main_IsDongleDevelop=0 e_Main_SelectMode=0 e_AutoPlay_Enable=0 e_NetMgr_Enable=0 e_Debug_Print=0";

        public Game_RwGunman(GameConfig Config) : base(Config)
        {
            _KnownMd5Prints.Add("gunman_dxF.exe - build 8796", "ffd6e3c06a2bf4a0abbe0961589432cc");
            _KnownMd5Prints.Add("gunman_dxR.exe - build 8796", "12666f3917779bf2bda624224a2dd346");

            if (_GameConfig.Fullscreen)
                _CommandLineArgs += " e_Main_Window_FullScreen=1";
            else
                _CommandLineArgs += " e_Main_Window_FullScreen=0";

            if (_GameConfig.GetBoolOption(TESTMENU_OPTION, false))
                _CommandLineArgs += " e_Main_TestMode_Request=1";
            else
                _CommandLineArgs += " e_Main_TestMode_Request=0";

            _GameLauncher.Set_CmdLine_Args(_CommandLineArgs);

            //AppUser folder (originally in %appdata% and it's content must be copied if not existing)
            string AppUserFolderPath = _GameConfig.TargetPath.Replace(Path.GetFileName(_GameConfig.TargetFileName), "..\\AppUser");
            if (!Directory.Exists(AppUserFolderPath))
            {
                try
                {
                    Logger.WriteLog("./AppUser directory not found, copying default ones....");
                    string ZipPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Properties.Resources.DATA_PAYLOADS_FOLDER, "Gunman", "AppUser.zip");
                    using (ZipArchive source = ZipFile.Open(ZipPath, ZipArchiveMode.Read, null))
                    {
                        foreach (ZipArchiveEntry entry in source.Entries)
                        {
                            string fullPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(_GameConfig.TargetPath), "..", entry.FullName));

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
                    double dMaxX = 1023.0;
                    double dMaxY = 767.0;
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
            _User32_String_BankAddress = LoaderDatabank_Address;
            _ShowCursor_String_BankAddress = LoaderDatabank_Address + 0x10;
            WriteBytes(_User32_String_BankAddress, new byte[] { 0x55, 0x73, 0x65, 0x72, 0x33, 0x32, 0x2E, 0x64, 0x6C, 0x6C, 0x00 });    //User32.dll
            WriteBytes(_ShowCursor_String_BankAddress, new byte[] { 0x53, 0x68, 0x6F, 0x77, 0x43, 0x75, 0x72, 0x73, 0x6F, 0x72, 0x00 });//ShowCursor
            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() => Loader Databank Address = 0x" + LoaderDatabank_Address.ToString("X8"));

            Replace_OldAppUser_filePath();
            InvertReloadButtons();

            if (_GameConfig.NoCursor)
            {
                if (CreateTransparentCursor())
                    SetHack_NoCursor();
            }
        }

        /// <summary>
        /// Change path for local /AppUser folder
        /// </summary>
        private void Replace_OldAppUser_filePath()
        {
            try
            {
                ReplaceReadOnlyString(new UInt32[] { _Gunman_Initialize_String_Offset }, Encoding.ASCII.GetBytes("..\\AppUser\\gunman_initialize.txt\0"));
                ReplaceReadOnlyString(new UInt32[] { _Gunman_Initturn_String_Offset }, Encoding.ASCII.GetBytes("..\\AppUser\\gunman_initturn.txt\0"));
                ReplaceReadOnlyString(new UInt32[] { _Gunman_MxgDeliver_String_Offset }, Encoding.ASCII.GetBytes("..\\AppUser\\mxgdeliver.log\0"));
                ReplaceReadOnlyString(new UInt32[] { _Gunman_LampMain_String_Offset }, Encoding.ASCII.GetBytes("..\\AppUser\\gunman_lamp_main.txt\0"));
                ReplaceReadOnlyString(new UInt32[] { _Gunman_LampSate_String_Offset }, Encoding.ASCII.GetBytes("..\\AppUser\\gunman_lamp_sate.txt\0"));
                ReplaceReadOnlyString(new UInt32[] { _Gunman_ErrorSate_String_Offset }, Encoding.ASCII.GetBytes("..\\AppUser\\gunman_error_sate.txt\0"));
                ReplaceReadOnlyString(new UInt32[] { _Gunman_ErrorMain_String_Offset }, Encoding.ASCII.GetBytes("..\\AppUser\\gunman_error_main.txt\0"));
                ReplaceReadOnlyString(new UInt32[] { _Gunman_Initcask_String_Offset }, Encoding.ASCII.GetBytes("..\\AppUser\\gunman_initcask.txt\0"));

                //ReplaceReadOnlyString(new UInt32[] { _Gunman_Initialize_String_Offset }, new byte[] { 0x2E, 0x2E, 0x5C, 0x41, 0x70, 0x70, 0x55, 0x73, 0x65, 0x72, 0x5C, 0x67, 0x75, 0x6E, 0x6D, 0x61, 0x6E, 0x5F, 0x69, 0x6E, 0x69, 0x74, 0x69, 0x61, 0x6C, 0x69, 0x7A, 0x65, 0x2E, 0x74, 0x78, 0x74, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });
                //ReplaceReadOnlyString(new UInt32[] { _Gunman_Initturn_String_Offset }, new byte[] { 0x2E, 0x2E, 0x5C, 0x41, 0x70, 0x70, 0x55, 0x73, 0x65, 0x72, 0x5C, 0x67, 0x75, 0x6E, 0x6D, 0x61, 0x6E, 0x5F, 0x69, 0x6E, 0x69, 0x74, 0x74, 0x75, 0x72, 0x6E, 0x2E, 0x74, 0x78, 0x74, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });
                //ReplaceReadOnlyString(new UInt32[] { _Gunman_MxgDeliver_String_Offset }, new byte[] { 0x2E, 0x2E, 0x5C, 0x41, 0x70, 0x70, 0x55, 0x73, 0x65, 0x72, 0x5C, 0x6D, 0x78, 0x67, 0x64, 0x65, 0x6C, 0x69, 0x76, 0x65, 0x72, 0x2E, 0x6C, 0x6F, 0x67, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });
                //ReplaceReadOnlyString(new UInt32[] { _Gunman_LampMain_String_Offset }, new byte[] { 0x2E, 0x2E, 0x5C, 0x41, 0x70, 0x70, 0x55, 0x73, 0x65, 0x72, 0x5C, 0x67, 0x75, 0x6E, 0x6D, 0x61, 0x6E, 0x5F, 0x6C, 0x61, 0x6D, 0x70, 0x5F, 0x6D, 0x61, 0x69, 0x6E, 0x2E, 0x74, 0x78, 0x74, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });
                //ReplaceReadOnlyString(new UInt32[] { _Gunman_LampSate_String_Offset }, new byte[] { 0x2E, 0x2E, 0x5C, 0x41, 0x70, 0x70, 0x55, 0x73, 0x65, 0x72, 0x5C, 0x67, 0x75, 0x6E, 0x6D, 0x61, 0x6E, 0x5F, 0x6C, 0x61, 0x6D, 0x70, 0x5F, 0x73, 0x61, 0x74, 0x65, 0x2E, 0x74, 0x78, 0x74, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });
                //ReplaceReadOnlyString(new UInt32[] { _Gunman_ErrorSate_String_Offset }, new byte[] { 0x2E, 0x2E, 0x5C, 0x41, 0x70, 0x70, 0x55, 0x73, 0x65, 0x72, 0x5C, 0x67, 0x75, 0x6E, 0x6D, 0x61, 0x6E, 0x5F, 0x65, 0x72, 0x72, 0x6F, 0x72, 0x5F, 0x73, 0x61, 0x74, 0x65, 0x2E, 0x74, 0x78, 0x74, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });
                //ReplaceReadOnlyString(new UInt32[] { _Gunman_ErrorMain_String_Offset }, new byte[] { 0x2E, 0x2E, 0x5C, 0x41, 0x70, 0x70, 0x55, 0x73, 0x65, 0x72, 0x5C, 0x67, 0x75, 0x6E, 0x6D, 0x61, 0x6E, 0x5F, 0x65, 0x72, 0x72, 0x6F, 0x72, 0x5F, 0x6D, 0x61, 0x69, 0x6E, 0x2E, 0x74, 0x78, 0x74, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });
                //ReplaceReadOnlyString(new UInt32[] { _Gunman_Initcask_String_Offset }, new byte[] { 0x2E, 0x2E, 0x5C, 0x41, 0x70, 0x70, 0x55, 0x73, 0x65, 0x72, 0x5C, 0x67, 0x75, 0x6E, 0x6D, 0x61, 0x6E, 0x5F, 0x69, 0x6E, 0x69, 0x74, 0x63, 0x61, 0x73, 0x6B, 0x2E, 0x74, 0x78, 0x74, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });

                Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() succesfully installed");
            }
            catch (Exception Ex)
            {
                Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() Error : " + Ex.Message.ToString());
            }
        }

        //Invert middle and right mouse actions
        private void InvertReloadButtons()
        {
            WriteByte(GameProcess_ModuleBaseAddress + _MouseHandlingFunction_Offset + 0x10F, 0x48);
            WriteByte(GameProcess_ModuleBaseAddress + _MouseHandlingFunction_Offset + 0x112, 0x50);
            WriteByte(GameProcess_ModuleBaseAddress + _MouseHandlingFunction_Offset + 0x12A, 0x48);
            WriteByte(GameProcess_ModuleBaseAddress + _MouseHandlingFunction_Offset + 0x134, 0x50);

            WriteByte(GameProcess_ModuleBaseAddress + _MouseHandlingFunction_Offset + 0x150, 0x3C);
            WriteByte(GameProcess_ModuleBaseAddress + _MouseHandlingFunction_Offset + 0x163, 0x44);
            WriteByte(GameProcess_ModuleBaseAddress + _MouseHandlingFunction_Offset + 0x16B, 0x3C);
            WriteByte(GameProcess_ModuleBaseAddress + _MouseHandlingFunction_Offset + 0x175, 0x44);

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

        #region Input Hack

        protected override void Apply_InputsMemoryHack()
        {
            Create_InputsDataBank();
            _P1_X_Address = InputsDatabank_Address;
            _P2_X_Address = InputsDatabank_Address + 0x04;
            _P1_Y_Address = InputsDatabank_Address + 0x08;            
            _P2_Y_Address = InputsDatabank_Address + 0x0C;
            _P1_Trigger_Address = InputsDatabank_Address + 0x10;
            _P2_Trigger_Address = InputsDatabank_Address + 0x11;
            _P1_Reload_Address = InputsDatabank_Address + 0x12; 
            _P2_Reload_Address = InputsDatabank_Address + 0x13;
            _Reset_Address = InputsDatabank_Address + 0x14;

            SetHack_Axis();
            SetHack_Buttons();

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        /// <summary>
        /// Game is using mouse coordinates to set P1 values
        /// And also for P2 if a command line option is set, or otherwise uses some other data
        /// Changing the values in memory for both after the whole procedure allow us to set data whatever mode is choosen
        /// </summary>
        private void SetHack_Axis()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);
            //mov ecx,[ebp-2C]
            CaveMemory.Write_StrBytes("8B 4D D4");
            //push eax
            CaveMemory.Write_StrBytes("50");
            //mov eax,[_P1_X_Address]
            CaveMemory.Write_StrBytes("8B 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_X_Address));
            //mov [ecx+000000A8],eax
            CaveMemory.Write_StrBytes("89 81 A8 00 00 00");
            //mov eax,[_P1_Y_Address]
            CaveMemory.Write_StrBytes("8B 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_Y_Address));
            //mov [ecx+000000AC],eax
            CaveMemory.Write_StrBytes("89 81 AC 00 00 00");
            //mov eax,[_P2_X_Address]
            CaveMemory.Write_StrBytes("8B 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P2_X_Address));
            //mov [ecx+000000B0],eax
            CaveMemory.Write_StrBytes("89 81 B0 00 00 00");
            //mov eax,[_P2_Y_Address]
            CaveMemory.Write_StrBytes("8B 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P2_Y_Address));
            //mov [ecx+000000B4],eax
            CaveMemory.Write_StrBytes("89 81 B4 00 00 00");
            //pop eax
            CaveMemory.Write_StrBytes("58");
            //add ecx,000000A8
            CaveMemory.Write_StrBytes("81 C1 A8 00 00 00");

            //Inject it
            CaveMemory.InjectToOffset(_Axis_InjectionStruct, "Axis");
        }

        /// <summary>
        /// Replacing the address of the buttons status loaded in the procedure to get trigger status
        /// by our own address
        /// </summary>
        private void SetHack_Buttons()
        {
            //P1:
            //Replace Trigger flag test with our own value
            WriteBytes(GameProcess_ModuleBaseAddress + _P1_TriggerPatch_Offset, new byte[] { 0x0F, 0xB6, 0x15 });
            WriteBytes(GameProcess_ModuleBaseAddress + _P1_TriggerPatch_Offset + 3, BitConverter.GetBytes(_P1_Trigger_Address));
            WriteBytes(GameProcess_ModuleBaseAddress + _P1_TriggerPatch_Offset + 7, new byte[] { 0x80, 0xE2, 0x80 });

            //Replace Reload flag test with our own value
            WriteBytes(GameProcess_ModuleBaseAddress + _P1_OtherClickPatch_Offset, new byte[] { 0x0F, 0xB6, 0x15 });
            WriteBytes(GameProcess_ModuleBaseAddress + _P1_OtherClickPatch_Offset + 3, BitConverter.GetBytes(_P1_Reload_Address));
            WriteBytes(GameProcess_ModuleBaseAddress + _P1_OtherClickPatch_Offset + 7, new byte[] { 0x80, 0xE2, 0x80 });

            //Replace RESET flag test with our own value
            WriteBytes(GameProcess_ModuleBaseAddress + _P1_ReloadPatch_Offset, new byte[] { 0x0F, 0xB6, 0x15 });
            WriteBytes(GameProcess_ModuleBaseAddress + _P1_ReloadPatch_Offset + 3, BitConverter.GetBytes(_Reset_Address));
            WriteBytes(GameProcess_ModuleBaseAddress + _P1_ReloadPatch_Offset + 7, new byte[] { 0x80, 0xE2, 0x80 });

            //P2:
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);
            //mov eax,[_P2_Trigger_Address]
            CaveMemory.Write_StrBytes("8B 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P2_Trigger_Address));
            //mov edx, [_P2_Reload_Address]
            CaveMemory.Write_StrBytes("8B 15");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P2_Reload_Address));
            //mov [ecx+48],eax
            CaveMemory.Write_StrBytes("89 41 48");
            //mov [ecx+4C],edx
            CaveMemory.Write_StrBytes("89 51 4C");

            //Inject it
            CaveMemory.InjectToOffset(_P2_Buttons_InjectionStruct, "P2 Buttons");
        }
        #endregion

        #region Output Hacks
        protected override void Apply_OutputsMemoryHack()
        {
            //Create Databak to store our value
            Create_OutputsDataBank();
            _CustomLamps_CaveAddress = OutputsDatabank_Address;

            SetHack_Lamp();

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        /// <summary>
        /// Intercepting the call to the Lamp-related functions so that we can read the parameters (Lamp ID and status)
        /// Unfortunatelly, the game does not call that function once in-game (only in test-menu)
        /// </summary>
        private void SetHack_Lamp()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //mov ebp,[esp+0C]
            CaveMemory.Write_StrBytes("8B 6C 24 0C");
            //add ebp,_CustomLamps_CaveAddress
            CaveMemory.Write_StrBytes("81 C5");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_CustomLamps_CaveAddress));
            //mov eax,[esp+10]
            CaveMemory.Write_StrBytes("8B 44 24 10");
            //mov [ebp+00],al
            CaveMemory.Write_StrBytes("88 45 00");
            //mov ebp,esp
            CaveMemory.Write_StrBytes("8B EC");
            //mov eax,[ebp+08]
            CaveMemory.Write_StrBytes("8B 45 08");

            //Inject it
            CaveMemory.InjectToOffset(_Lamp_InjectionStruct, "Lamp");
        }
        #endregion

        #region No-Crosshair Hack
        protected override void Apply_NoCrosshairMemoryHack()
        {
            SetHack_NoCrosshair(_P1_NoCrosshair_InjectionStruct, "P1 Nocrosshair");
            SetHack_NoCrosshair(_P2_NoCrosshair_InjectionStruct, "P2 Nocrosshair");

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        /// <summary>
        /// Forcing float value [-100.0, -100.0] for both player reticle display
        /// </summary>
        private void SetHack_NoCrosshair(InjectionStruct32 PlayerInjectionStruct, String InjectionName)
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //mov edx,C2C80000
            CaveMemory.Write_StrBytes("BA 00 00 C8 C2");
            //mov eax,C2C80000
            CaveMemory.Write_StrBytes("B8 00 00 C8 C2");

            //Inject it
            CaveMemory.InjectToOffset(PlayerInjectionStruct, InjectionName);
        }
        #endregion

        #endregion

        #region Inputs

        protected override void UpdateAxis(PlayerID Id, int X, int Y)
        {
            byte[] bufferX = BitConverter.GetBytes((float)X);
            byte[] bufferY = BitConverter.GetBytes((float)Y);

            WriteBytes(_P1_X_Address + 4 * (UInt32)Id, bufferX);
            WriteBytes(_P1_Y_Address + 4 * (UInt32)Id, bufferY);
        }

        protected override void GameController_OnButtonDown(object sender, VirtualGameControler.ButtonDownEventArgs e)
        {
            if (e.ButtonName.Equals("EXIT"))
            {
                Close();
            }
            else if (e.ButtonName.Equals("COIN"))
            {
                int iCredits = ReadInt32(GameProcess_ModuleBaseAddress + _Credits_Offset);
                iCredits += _GameConfig.GetNumericOption(CREDITSADDED_OPTION);
                WriteBytes(GameProcess_ModuleBaseAddress + _Credits_Offset, BitConverter.GetBytes(iCredits));
            }
            else if (e.ButtonName.Equals("RESET"))
            {
                WriteByte(_Reset_Address, 0x80);
            }
        }

        protected override void GameController_OnButtonUp(object sender, VirtualGameControler.ButtonDownEventArgs e)
        {
            if (e.ButtonName.Equals("RESET"))
                WriteByte(_Reset_Address, 0x00);
        }

        protected override void PlayerController_OnButtonDown(object sender, VirtualGameControler.ButtonDownEventArgs e)
        {
            if (e.Id != PlayerID.None)
            {
                UInt32 PlayerIndex = (UInt32)e.Id;

                if (e.ButtonName.Equals("TRIGGER"))
                    WriteByte(_P1_Trigger_Address +  PlayerIndex, 0x80);
                else if (e.ButtonName.Equals("RELOAD"))
                { 
                    WriteByte(_P1_Reload_Address + PlayerIndex, 0x80);

                    //Player 2 needs a "real" offscreen shot instead of a reload flag
                    if (e.Id == PlayerID.Player2)
                    {
                        WriteBytes(_P1_X_Address + 4, new byte[] { 0x00, 0x00, 0x00, 0x00 }); 
                        WriteBytes(_P1_Y_Address + 4, new byte[] { 0x00, 0x00, 0x00, 0x00 });
                        WriteByte(_P1_Trigger_Address + PlayerIndex, 0x80);
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
                    WriteByte(_P1_Trigger_Address + PlayerIndex, 0x00);
                else if (e.ButtonName.Equals("RELOAD"))
                {
                    WriteByte(_P1_Reload_Address + PlayerIndex, 0x00);
                    if (e.Id == PlayerID.Player2)
                        WriteByte(_P1_Trigger_Address + PlayerIndex, 0x00);
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
            //Gun motor : stays activated when trigger is pulled
            //Gun recoil : not used ??
            _Outputs = new List<GameOutput>
            {
                new AsyncGameOutput(OutputId.P1_CtmRecoil, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_ON_DELAY), ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_OFF_DELAY), 0),
                new AsyncGameOutput(OutputId.P2_CtmRecoil, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_ON_DELAY), ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_OFF_DELAY), 0),
                new GameOutput(OutputId.P1_Ammo),
                new GameOutput(OutputId.P2_Ammo),
                new GameOutput(OutputId.Credits)
            };
        }

        /// <summary>
        /// Update all Outputs values before sending them to MameHooker
        /// </summary>
        public override void UpdateOutputValues()
        {
            //Custom Outputs
            int Credits = ReadInt32(GameProcess_ModuleBaseAddress + _Credits_Offset);

            for (uint i = 0; i < _GameConfig.PlayerCount; i++)
            {
                _Ammo[i] = 0;

                //Credits are needed to play (=reload bullets) so it can be used as a filter, as there is no "life"
                if (Credits > 0)
                {
                    _Ammo[i] = ReadByte(ReadPtrChain(GameProcess_ModuleBaseAddress + _pGunCylinderMgr_Offset, new UInt32[] { 0x34 + (4 * i) }) + 0x1C8);
                    //Custom Recoil
                    if (_Ammo[i] < _LastAmmo[i] && (_LastAmmo[i] - _Ammo[i]) <= 1)
                        SetOutputValue(OutputId.P1_CtmRecoil + i, 1);
                }

                _LastAmmo[i] = _Ammo[i];
                SetOutputValue(OutputId.P1_Ammo + i, _Ammo[i]);
            }

            SetOutputValue(OutputId.Credits, Credits);
        }

        #endregion
    }
}
