using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using U_Shoot.Properties;
using UsCore.Config;
using UsCore.Launcher;
using UsCore.Logging;
using UsCore.MameOutput;
using UsCore.Memory;
using UsCore.VirtualControllers;

namespace U_Shoot.Games
{
    //Enhanced code for native Inputs handling and NAtive outputs support without AMDaemon.exe by PaCiFiKbAllA
    public class Game_AllsHodsd : BaseGame64
    {
        protected override string GAMEDATA_FOLDER { get { return @"Alls\hodsd"; } }
        protected override bool PATCH_PEB_DEBUG_FLAG { get { return false; } }

        private const string SETTINGS_FILENAME = "_Settings.dat";
        private const string RECORDS_FILENAME = "_Records.dat";

        private const int GAME_SETTINGS_SIZE = 0x110;
        private const int GAME_RECORDS_SIZE = 0x20D98;

        private const uint BUTTON_SLOT_COUNT = 0x13;
        private const uint SLOT_P1_START = 0x00;
        private const uint SLOT_P1_TRIGGER = 0x01;
        private const uint SLOT_P1_SELECT = 0x02;
        private const uint SLOT_P1_SPECIAL = 0x03;
        private const uint SLOT_P2_START = 0x05;
        private const uint SLOT_P2_TRIGGER = 0x06;
        private const uint SLOT_P2_SELECT = 0x07;
        private const uint SLOT_P2_SPECIAL = 0x08;
        private const uint SLOT_BAKUON = 0x0A;
        private const uint SLOT_EYE0 = 0x0B;
        private const uint SLOT_PROXIMITY = 0x0C;
        private const uint SLOT_EMONEY_LEFT = 0x0D;
        private const uint SLOT_EMONEY_RIGHT = 0x0E;
        private const uint SLOT_EMONEY_CENTER = 0x0F;
        private const uint SLOT_CABINET_KIND = 0x10;
        private const uint SLOT_TEST = 0x11;
        private const uint SLOT_SERVICE = 0x12;

        private readonly byte[] GUN_ADJ_VALUES = new byte[]
        {
            0x00, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x02, 0x00, 0x02, 0x00, 0x02, 0xFF, 0x03, 0x00, 0x02, 0x00, 0x02, 0xFF, 0x03, 0x00, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x02, 0x00, 0x02, 0x00, 0x02, 0xFF, 0x03, 0x00, 0x02, 0x00, 0x02, 0xFF, 0x03
        };

        //Custom Array to store "Held" status of a button
        private byte[] _ButtonHeld = new byte[BUTTON_SLOT_COUNT];

        private UInt64 _GameId = 0x54454453; //SDET

        #region LOADER MEMORY ADDRESSES   
        private UInt64 _SetCursorFunctionPtr_Offset = 0x01ECFC78;
        private UInt64 _UBookkeepingSetCommonGameTimeCountEndAndRestart_Function_Offset = 0x001D1A60;
        private UInt64 _UBookkeepingsSetPlayerEndMark_Function_Offset = 0x001D0FF0;
        private UInt64 _BackupRecordStatus_PatchOffset = 0x01C65430;
        private UInt64 _BackupSetRecordcount_PatchOffset = 0x01B3C960;
        private UInt64 _BackupConvert_Trampoline_Offset = 0x001CA9B1;
        private UInt64 _SaveSettings_Used_Offset = 0x030873F8;
        private UInt64 _Settings_GunAdjData_Offset = 0x88;  //SDEE & SDET have different Setting dat file location for Gun Adjustment Values
        //private InjectionStruct64 _ReadSettings_InjectionStruct = new InjectionStruct64(0x001D40B0, 22);
        //private InjectionStruct64 _CheckDefaultSettingsValue_InjectionStruct = new InjectionStruct64(0x001D47B0, 15);
        private InjectionStruct64 _BackupConvert_InjectionStruct = new InjectionStruct64(0x001CA920, 6);
        private InjectionStruct64 _GameSettingsSaveRecord_InjectionStruct = new InjectionStruct64(0x001D58E0, 17);
        private InjectionStruct64 _UBookkeepingSaveRecord_InjectionStruct = new InjectionStruct64(0x001CEE80, 14);

        //Custom Data
        private UInt64 _NewSettingsFlag_CaveAddress = 0;
        private UInt64 _NewRecordsFlag_CaveAddress = 0;
        private UInt64 _SettingsBufferPtr_CaveAddress = 0;
        private UInt64 _RecordsBufferPtr_CaveAddress = 0;
        private UInt64 _InitialBackupRecoveredFlag_CaveAddress = 0;
        private UInt64 _SavedSettingsFileFoundFlag_CaveAddress = 0;
        private UInt64 _SavedRecordsFileFoundFlag_CaveAddress = 0;
        private UInt64 _SettingsGunAdjData_CaveAddress = 0;
        private UInt64 _SavedSettingsData_CaveAddress = 0;
        private UInt64 _SavedRecordsData_CaveAddress = 0;
        #endregion

        #region INPUTS MEMORY ADDRESSES    
        private UInt64 _PlayerStatusPointer_Offset = 0x03087750;
        private UInt64 _NativeIoPostUpdateCall_Ofsset = 0x001DD6B0;
        private UInt64 _RawAnalogProvider_JumpInst_Offset = 0x011A5E80;
        private UInt64 _SciOpen_Patch_Offset = 0x001E4670;
        private UInt64 _SciConfig_Patch_Offset = 0x001E4510;
        private UInt64 _GunRecv_Patch_Offset = 0x001E1FA3;
        private UInt64 _GunDotUpdOnline_Patch_Offset = 0x001E2840;
        private NopStruct64 _Nop_GunSendChk = new NopStruct64(0x001E1F7E, 6);
        private NopStruct64 _Nop_GunSensorGate = new NopStruct64(0x001DD6C4, 16);
        private NopStruct64 _Nop_GunAdjustmentCheck1 = new NopStruct64(0x001DC926, 6);
        private NopStruct64 _Nop_GunAdjustmentCheck2 = new NopStruct64(0x001DC93A, 6);
        private InjectionStruct64 _AxisCorrection_InjectionStruct = new InjectionStruct64(0x001DED89, 17);
        private InjectionStruct64 _RawAnalogProvider_InjectionStruct = new InjectionStruct64(0x001DD1F0, 15);
        private InjectionStruct64 _GunSensorAxes_InjectionStruct = new InjectionStruct64(0x001E1BC0, 18);
        private InjectionStruct64 _GunSensorEventDown_InjectionStruct = new InjectionStruct64(0x001E1C90, 16);
        private InjectionStruct64 _GunSensorEventUp_InjectionStruct = new InjectionStruct64(0x001E1D10, 16);
        private InjectionStruct64 _NativeIoPostUpdate_InjectionStruct = new InjectionStruct64(0x001DE7AE, 15);
        private InjectionStruct64 _GunUpdateRdy_InjectionStruct = new InjectionStruct64(0x001E23B0, 19);

        //Custom Data
        private UInt64 _WindowWidth_CaveAddress = 0;
        private UInt64 _WindowHeight_CaveAddress = 0;
        private UInt64 _FGunSensorObject_CaveAddress = 0;
        private UInt64 _GunMirCnt_CaveAddress = 0;
        private UInt64 _P1_RawX_CaveAddress = 0;
        private UInt64 _P1_RawY_CaveAddress = 0;
        private UInt64 _P2_RawX_CaveAddress = 0;
        private UInt64 _P2_RawY_CaveAddress = 0;
        private UInt64 _P1_TriggerDown_CaveAddress = 0;
        private UInt64 _P1_TriggerUp_CaveAddress = 0;
        private UInt64 _P2_TriggerDown_CaveAddress = 0;
        private UInt64 _P2_TriggerUp_CaveAddress = 0;
        private UInt64 _P1_OnScreen_CaveAddress = 0;
        private UInt64 _P1_OutOfScreen_CaveAddress = 0;
        private UInt64 _P2_OnScreen_CaveAddress = 0;
        private UInt64 _P2_OutOfScreen_CaveAddress = 0;
        private UInt64 _ButtonsState_CaveAddress = 0;
        #endregion

        #region OUTPUTS MEMORY ADDRESSES
        private UInt64 _TargetRenderPool_Offset = 0x03238A00;
        private InjectionStruct64 _OutputState_InjectionStruct = new InjectionStruct64(0x001BB200, 19);

        //Custom Data
        private UInt64 _OutputState_CaveAddress = 0;
        #endregion

        private UInt32[] _ShotCount, _LastShotCount;

        public Game_AllsHodsd(GameConfig Config) : base(Config)
        {
            _KnownMd5Prints.Add("Hodzero-Win64-Shipping.exe - SDET", "cde48c217d04caa64ee24a72f73dcce4");
            _KnownMd5Prints.Add("Hodzero-Win64-Shipping.exe - SDEE", "ba74fb972e9207950fdb25db511aadd8");
        }

        protected override void ProcessTimerTick_Hooked_CheckProc()
        {
            //Polling for data to save
            byte IsNewSettings = ReadByte(_NewSettingsFlag_CaveAddress);
            byte IsNewRecord = ReadByte(_NewRecordsFlag_CaveAddress);

            if (IsNewSettings != 0)
            {
                UInt64 sAddress = ReadPtr(_SettingsBufferPtr_CaveAddress);
                if (sAddress != 0)
                {
                    byte[] SettingsData = ReadBytes(sAddress, GAME_SETTINGS_SIZE);
                    WriteDatBytes(GetSettingsFilePath(), SettingsData);
                }
                WriteByte(_NewSettingsFlag_CaveAddress, 0);
            }

            if (IsNewRecord != 0)
            {
                UInt64 rAddress = ReadPtr(_RecordsBufferPtr_CaveAddress);
                if (rAddress != 0)
                {
                    byte[] RecordData = ReadBytes(rAddress, GAME_RECORDS_SIZE);                    
                    WriteDatBytes(GetRecordsFilePath(), RecordData);
                }
                WriteByte(_NewRecordsFlag_CaveAddress, 0);
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
                    double dMaxY = 1023.0;
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

                    WriteBytes(_WindowWidth_CaveAddress, BitConverter.GetBytes((UInt64)TotalResX));
                    WriteBytes(_WindowHeight_CaveAddress, BitConverter.GetBytes((UInt64)TotalResY));

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

            SetHack_PersistentDataSaving();

            //To remove cursor from screen, intercepting call to USER32.DLL.SetCursor() and changing param RCX to zero
            if (_GameConfig.NoCursor)
            {
                UInt64 SetCursorFunction_Address = ReadPtr(GameProcess_ModuleBaseAddress + _SetCursorFunctionPtr_Offset);
                if (SetCursorFunction_Address != 0)
                {
                    Codecave64 CaveMemory = new Codecave64(_Process, GameProcess_ModuleBaseAddress);

                    //xor rcx, rcx
                    CaveMemory.Write_StrBytes("48 31 C9");
                    //jmp User32.dll.SetCursor()
                    CaveMemory.Write_StrBytes("FF 25 00 00 00 00");
                    CaveMemory.Write_Bytes(BitConverter.GetBytes(SetCursorFunction_Address));

                    ReplaceReadOnlyString(new UInt64[] { _SetCursorFunctionPtr_Offset }, BitConverter.GetBytes(CaveMemory.CaveAddress));
                }
            }

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        #endregion        

        #region Data saving Hacks

        /// <summary>
        /// Game settings does not save without AmDaemon
        /// Adding some codecave to make the game write a custom dat file when Exiting TEST menu
        /// Adding some codecave to make the game load custom data at start
        /// </summary>
        private void SetHack_PersistentDataSaving()
        {
            //Size = 0x20D98
            Codecave64 DataFileHack_Cave = new Codecave64(_Process, GameProcess_ModuleBaseAddress, 0x20100);

            UInt64 FileHackDataBank_Address = DataFileHack_Cave.CaveAddress;
            _NewSettingsFlag_CaveAddress = FileHackDataBank_Address;
            _NewRecordsFlag_CaveAddress = FileHackDataBank_Address + 0x01;
            _InitialBackupRecoveredFlag_CaveAddress = FileHackDataBank_Address + 0x02;
            _SavedSettingsFileFoundFlag_CaveAddress = FileHackDataBank_Address + 0x03;
            _SavedRecordsFileFoundFlag_CaveAddress = FileHackDataBank_Address + 0x04;
            _SettingsBufferPtr_CaveAddress = FileHackDataBank_Address + 0x10;
            _RecordsBufferPtr_CaveAddress = FileHackDataBank_Address + 0x18;
            _SettingsGunAdjData_CaveAddress = FileHackDataBank_Address + 0x20; //Size = 40 Bytes (20 bytes / Player)
            _SavedSettingsData_CaveAddress = FileHackDataBank_Address + 0x50; //Size = 0x110
            _SavedRecordsData_CaveAddress = FileHackDataBank_Address + 0x170; //Size = 0x20D98

            WriteBytes(_SavedSettingsData_CaveAddress, ReadDatBytes(GetSettingsFilePath(), GAME_SETTINGS_SIZE));
            WriteBytes(_SavedRecordsData_CaveAddress, ReadDatBytes(GetRecordsFilePath(), GAME_RECORDS_SIZE));
            WriteBytes(_SettingsGunAdjData_CaveAddress, GUN_ADJ_VALUES);

            if (File.Exists(GetSettingsFilePath()))
                WriteByte(_SavedSettingsFileFoundFlag_CaveAddress, 0x01);

            if (File.Exists(GetRecordsFilePath()))
                WriteByte(_SavedRecordsFileFoundFlag_CaveAddress, 0x01);

            /***** Loading Files *****/
            SetHack_ConvertBackupData();

            /***** Asetting / Adjustment Data *****/
            // NOP some tests in AAppPlayerController::BeginPlay when looking for GunAdjustment Params, to avoid Failure
            SetNops(GameProcess_ModuleBaseAddress, _Nop_GunAdjustmentCheck1);
            SetNops(GameProcess_ModuleBaseAddress, _Nop_GunAdjustmentCheck2);

            //Intercepting save procedure to write file
            SetHack_SaveSettings();

            /***** Records/Rankings *****/
            //Spoofing BackupRecordStatus check (recordIndex) result
            //cmp edx, 02
            //jae Next
            //mov xor eax, eax
            //ret
            //Next:
            //mov eax, FFFFFFFF
            //ret
            WriteBytes(GameProcess_ModuleBaseAddress + _BackupRecordStatus_PatchOffset, new byte[] { 0x83, 0xFA, 0x02, 0x73, 0x03, 0x33, 0xC0, 0xC3, 0xB8, 0xFF, 0xFF, 0xFF, 0xFF, 0xC3, 0x90, 0x90 });

            //Forcing recordIndex to 02
            //mov eax, 02
            //ret
            WriteBytes(GameProcess_ModuleBaseAddress + _BackupSetRecordcount_PatchOffset, new byte[] { 0xB8, 0x02, 0x00, 0x00, 0x00, 0xC3 });

            //Intercepting save procedure to write file
            SetHack_SaveRecords();
        }

        /// <summary>
        /// That function is called at the Initialization screen to check Settings and Records data block validity in memory
        /// Injecting some code here will allow to Read memory data from disk
        /// That Initialization screen is also called when quitting TEST mode and resetting the game
        /// 
        /// Plan is as follow :
        /// - Step 1 : Make sure file from disk is only loaded when the initial check is done (not to overwrite dynamic change in memory)
        /// - Step 2 : Check and skip if Settings or Records dat file are not present to use default value
        /// - Step 3 : Override Calibration data, either from disk or from default values
        /// In everycase : saving Records/Settings buffer addresses
        /// </summary>
        private void SetHack_ConvertBackupData()
        {
            Codecave64 CaveMemory = new Codecave64(_Process, GameProcess_ModuleBaseAddress);

            //push rcx
            CaveMemory.Write_StrBytes("51");
            //push rdx
            CaveMemory.Write_StrBytes("52");
            //push rsi
            CaveMemory.Write_StrBytes("56");
            //push rdi
            CaveMemory.Write_StrBytes("57");
            //mov rax, _InitialBackupRecovered
            CaveMemory.Write_StrBytes("48 B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_InitialBackupRecoveredFlag_CaveAddress));
            //cmp byte ptr[rax], 01
            CaveMemory.Write_StrBytes("80 38 01");
            //je GunAdjust
            CaveMemory.Write_StrBytes("74 77");

            //mov rax, _SavedSettingsFileFoundFlag_CaveAddress
            CaveMemory.Write_StrBytes("48 B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_SavedSettingsFileFoundFlag_CaveAddress));
            //cmp byte ptr[rax], 01
            CaveMemory.Write_StrBytes("80 38 01");
            //jne RecordsCheck
            CaveMemory.Write_StrBytes("75 1D");
            //mov rdx,[rcx+30]
            CaveMemory.Write_StrBytes("48 8B 51 30");            
            //mov rsi, _SavedSettingsData_CaveAddress
            CaveMemory.Write_StrBytes("48 BE");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_SavedSettingsData_CaveAddress));
            //mov rdi,rdx
            CaveMemory.Write_StrBytes("48 8B FA");
            //mov rcx, GAME_SETTINGS_SIZE
            CaveMemory.Write_StrBytes("48 B9");
            CaveMemory.Write_Bytes(BitConverter.GetBytes((UInt64)GAME_SETTINGS_SIZE));
            //repe movsb
            CaveMemory.Write_StrBytes("F3 A4");

            //RecordsCheck:
            //mov rcx,[rsp+18]
            CaveMemory.Write_StrBytes("48 8B 4C 24 18");
            //mov rdx,[rcx+28]
            CaveMemory.Write_StrBytes("48 8B 51 28");
            //mov rax, _RecordBuffer_Address
            CaveMemory.Write_StrBytes("48 B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_RecordsBufferPtr_CaveAddress));
            //mov [rax],rdx
            CaveMemory.Write_StrBytes("48 89 10");
            //mov rax, _SavedRecordsFileFoundFlag_CaveAddress
            CaveMemory.Write_StrBytes("48 B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_SavedRecordsFileFoundFlag_CaveAddress));
            //cmp byte ptr[rax], 01
            CaveMemory.Write_StrBytes("80 38 01");
            //jne GunAdjust
            CaveMemory.Write_StrBytes("75 26");            
            //mov rsi, _SavedRecordsData_CaveAddress
            CaveMemory.Write_StrBytes("48 BE");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_SavedRecordsData_CaveAddress));
            //mov rdi,rdx
            CaveMemory.Write_StrBytes("48 8B FA");
            //mov rcx, GAME_RECORDS_SIZE
            CaveMemory.Write_StrBytes("48 B9");
            CaveMemory.Write_Bytes(BitConverter.GetBytes((UInt64)GAME_RECORDS_SIZE));
            //repe movsb
            CaveMemory.Write_StrBytes("F3 A4");
            //mov rax, _InitialBackupRecovered
            CaveMemory.Write_StrBytes("48 B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_InitialBackupRecoveredFlag_CaveAddress));
            //mov byte ptr[rax], 1
            CaveMemory.Write_StrBytes("C6 00 01");

            //GunAdjust:
            //mov rcx,[rsp+18]
            CaveMemory.Write_StrBytes("48 8B 4C 24 18");
            //mov rdx,[rcx+30]
            CaveMemory.Write_StrBytes("48 8B 51 30");
            //mov rax, _SettingsBuffer_Address
            CaveMemory.Write_StrBytes("48 B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_SettingsBufferPtr_CaveAddress));
            //mov [rax],rdx
            CaveMemory.Write_StrBytes("48 89 10");
            //mov rsi, _GunAdjustData_CaveAddress
            CaveMemory.Write_StrBytes("48 BE");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_SettingsGunAdjData_CaveAddress));
            //lea rdi, [rdx + _Settings_GunAdjData_Offset]
            CaveMemory.Write_StrBytes("48 8D BA");
            CaveMemory.Write_Bytes(BitConverter.GetBytes((UInt32)_Settings_GunAdjData_Offset));
            //mov rcx, GUN_ADJ_VALUES.length
            CaveMemory.Write_StrBytes("48 B9");
            CaveMemory.Write_Bytes(BitConverter.GetBytes((UInt64)GUN_ADJ_VALUES.Length));
            //repe movsb
            CaveMemory.Write_StrBytes("F3 A4");

            //pop rdi
            CaveMemory.Write_StrBytes("5F");
            //pop rsi
            CaveMemory.Write_StrBytes("5E");
            //pop rdx
            CaveMemory.Write_StrBytes("5A");
            //pop rcx
            CaveMemory.Write_StrBytes("59");

            //OriginalCode:
            //push rbx
            CaveMemory.Write_StrBytes("40 53");
            //sub rsp,30
            CaveMemory.Write_StrBytes("48 83 EC 30");

            //Inject it
            CaveMemory.InjectToOffset_WithTrampoline(_BackupConvert_InjectionStruct, _BackupConvert_Trampoline_Offset, "BackupCOnvert");
        }

        /// <summary>
        /// That Codecave is installed at start of the procedure where the game saves Records values
        /// That way, it is possible to set the flag for U-Shoot to make a backup of the data
        /// </summary>
        private void SetHack_SaveRecords()
        {
            Codecave64 CaveMemory = new Codecave64(_Process, GameProcess_ModuleBaseAddress);

            //push rcx
            CaveMemory.Write_StrBytes("51");
            //push rdx
            CaveMemory.Write_StrBytes("52");
            //push r8
            CaveMemory.Write_StrBytes("41 50");
            //push r9
            CaveMemory.Write_StrBytes("41 51");
            //push rcx
            CaveMemory.Write_StrBytes("51");
            //push r10
            CaveMemory.Write_StrBytes("41 52");
            //push r11
            CaveMemory.Write_StrBytes("41 53");

            //mov rcx,[rsp+10]
            CaveMemory.Write_StrBytes("48 8B 4C 24 10");
            //xor edx,edx
            CaveMemory.Write_StrBytes("33 D2");
            //xor r8d,r8d
            CaveMemory.Write_StrBytes("45 33 C0");
            //xor r9d,r9d
            CaveMemory.Write_StrBytes("45 33 C9");
            //sub rsp,20
            CaveMemory.Write_StrBytes("48 83 EC 20");
            //mov rax, UBookkeeping::SetPlayerEndMark
            CaveMemory.Write_StrBytes("48 B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _UBookkeepingsSetPlayerEndMark_Function_Offset));
            //call rax
            CaveMemory.Write_StrBytes("FF D0");
            //add rsp,20
            CaveMemory.Write_StrBytes("48 83 C4 20");
            //mov rcx,[rsp+10]
            CaveMemory.Write_StrBytes("48 8B 4C 24 10");
            //mov edx,00000001
            CaveMemory.Write_StrBytes("BA 01 00 00 00");
            //xor r8d,r8d
            CaveMemory.Write_StrBytes("45 33 C0");
            //xor r9d,r9d
            CaveMemory.Write_StrBytes("45 33 C9");
            //sub rsp,20
            CaveMemory.Write_StrBytes("48 83 EC 20");
            //mov rax,UBookkeeping::SetPlayerEndMark
            CaveMemory.Write_StrBytes("48 B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _UBookkeepingsSetPlayerEndMark_Function_Offset));
            //call rax
            CaveMemory.Write_StrBytes("FF D0");
            //add rsp,20
            CaveMemory.Write_StrBytes("48 83 C4 20");

            //SDEE Japanese version does not have UBookkeeping::SetCommonGameTimeCountEndAndRestart() function to call
            if (_TargetProcess_Md5Hash == _KnownMd5Prints["Hodzero-Win64-Shipping.exe - SDET"])
            {
                //mov rcx,[rsp+10]
                CaveMemory.Write_StrBytes("48 8B 4C 24 10");
                //xor edx,edx
                CaveMemory.Write_StrBytes("33 D2");
                //xor r8d,r8d
                CaveMemory.Write_StrBytes("45 33 C0");
                //sub rsp,20
                CaveMemory.Write_StrBytes("48 83 EC 20");
                //mov rax, UBookkeeping::SetCommonGameTimeCountEndAndRestart
                CaveMemory.Write_StrBytes("48 B8");
                CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _UBookkeepingSetCommonGameTimeCountEndAndRestart_Function_Offset));
                //call rax
                CaveMemory.Write_StrBytes("FF D0");
                //add rsp,20
                CaveMemory.Write_StrBytes("48 83 C4 20");
                //mov rcx,[rsp+10]
                CaveMemory.Write_StrBytes("48 8B 4C 24 10");
                //xor edx,edx
                CaveMemory.Write_StrBytes("33 D2");
                //mov r8d,00000001
                CaveMemory.Write_StrBytes("41 B8 01 00 00 00");
                //sub rsp,20
                CaveMemory.Write_StrBytes("48 83 EC 20");
                //mov rax,"Hodzero
                CaveMemory.Write_StrBytes("48 B8 60 1A 1D 40 01 00 00 00");
                //call rax
                CaveMemory.Write_StrBytes("FF D0");
                //add rsp,20
                CaveMemory.Write_StrBytes("48 83 C4 20");
            }

            //mov rax, _RecordBuffer_Address
            CaveMemory.Write_StrBytes("48 B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_RecordsBufferPtr_CaveAddress));
            //mov rcx,[rsp+10]
            CaveMemory.Write_StrBytes("48 8B 4C 24 10");
            //mov rcx,[rcx+28]
            CaveMemory.Write_StrBytes("48 8B 49 28");
            //mov [rax],rcx
            CaveMemory.Write_StrBytes("48 89 08");
            //mov rax, [_NewRecordFlag]
            CaveMemory.Write_StrBytes("48 B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_NewRecordsFlag_CaveAddress));
            //mov byte ptr[rax], 1
            CaveMemory.Write_StrBytes("C6 00 01");

            //OriginalCode:
            //pop r11
            CaveMemory.Write_StrBytes("41 5B");
            //pop r10
            CaveMemory.Write_StrBytes("41 5A");
            //pop rcx
            CaveMemory.Write_StrBytes("59");
            //pop r9
            CaveMemory.Write_StrBytes("41 59");
            //pop r8
            CaveMemory.Write_StrBytes("41 58");
            //pop rdx
            CaveMemory.Write_StrBytes("5A");
            //pop rcx
            CaveMemory.Write_StrBytes("59");
            //ret
            CaveMemory.Write_StrBytes("C3");

            CaveMemory.InjectToOffset(_UBookkeepingSaveRecord_InjectionStruct, "Save Records");
        }

        /// <summary>
        /// That Codecave is installed at start of the procedure saving data when exiting TEST menu
        /// That way, it is possible to set the flag for U-Shoot to make a backup of the data
        /// </summary>
        private void SetHack_SaveSettings()
        {
            Codecave64 CaveMemory = new Codecave64(_Process, GameProcess_ModuleBaseAddress);

            //mov rax, _SettingsBuffer_Address
            CaveMemory.Write_StrBytes("48 B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_SettingsBufferPtr_CaveAddress));
            //mov rbx,[rcx+28]
            CaveMemory.Write_StrBytes("48 8B 59 28");
            //mov [rax],rbx
            CaveMemory.Write_StrBytes("48 89 18");
            //mov rax, [_NewSettingsFlag]
            CaveMemory.Write_StrBytes("48 B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_NewSettingsFlag_CaveAddress));
            //mov byte ptr[rax], 1
            CaveMemory.Write_StrBytes("C6 00 01");

            //mov [rsp+10],rbx
            CaveMemory.Write_StrBytes("48 89 5C 24 10");
            //push rdi
            CaveMemory.Write_StrBytes("57");
            //sub rsp,50
            CaveMemory.Write_StrBytes("48 83 EC 50");
            //mov rbx,Hodzero + 0x30873F8
            CaveMemory.Write_StrBytes("48 BB");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _SaveSettings_Used_Offset));
            //cmp byte ptr [rbx],05
            CaveMemory.Write_StrBytes("80 3B 05");

            //Inject it
            CaveMemory.InjectToOffset(_GameSettingsSaveRecord_InjectionStruct, "Save Settings");

        }

        private string GetDataFolder()
        {
            string DataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Hodzero", "Saved", "Data");
            Directory.CreateDirectory(DataPath);
            return DataPath;
        }
        private string GetSettingsFilePath()
        {
            return Path.Combine(GetDataFolder(), Encoding.ASCII.GetString(BitConverter.GetBytes((UInt32)_GameId)) + SETTINGS_FILENAME);
        }
        private string GetRecordsFilePath()
        {            
            return Path.Combine(GetDataFolder(), Encoding.ASCII.GetString(BitConverter.GetBytes((UInt32)_GameId)) + RECORDS_FILENAME);
        }

        private byte[] ReadDatBytes(string PathName, int Length)
        {
            byte[] Data = new byte[Length];
            try
            {
                using (FileStream Stream = new FileStream(PathName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    int Read = 0;
                    while (Read < Length)
                    {
                        int Count = Stream.Read(Data, Read, Length - Read);
                        if (Count <= 0) break;
                        Read += Count;
                    }
                }
            }
            catch { }
            return Data;
        }

        private void WriteDatBytes(string PathName, byte[] Data)
        {
            if (Data == null || Data.Length == 0) return;
            try
            {
                using (FileStream Stream = new FileStream(PathName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read))
                {
                    Stream.Seek(0, SeekOrigin.Begin);
                    Stream.Write(Data, 0, Data.Length);
                    Stream.Flush();
                }
            }
            catch { }
        }

        #endregion

        #region Inputs Hack

        protected override void Apply_InputsMemoryHack()
        {
            Create_InputsDataBank();

            _WindowWidth_CaveAddress = InputsDatabank_Address;
            _WindowHeight_CaveAddress = InputsDatabank_Address + 0x08;
            _FGunSensorObject_CaveAddress = InputsDatabank_Address + 0x10;
            _GunMirCnt_CaveAddress = InputsDatabank_Address + 0x18;
            _P1_RawX_CaveAddress = InputsDatabank_Address + 0x110;
            _P1_RawY_CaveAddress = InputsDatabank_Address + 0x114;
            _P2_RawX_CaveAddress = InputsDatabank_Address + 0x118;
            _P2_RawY_CaveAddress = InputsDatabank_Address + 0x11C;
            _P1_TriggerDown_CaveAddress = InputsDatabank_Address + 0x120;
            _P1_TriggerUp_CaveAddress = InputsDatabank_Address + 0x121;
            _P2_TriggerDown_CaveAddress = InputsDatabank_Address + 0x122;
            _P2_TriggerUp_CaveAddress = InputsDatabank_Address + 0x123;
            _P1_OnScreen_CaveAddress = InputsDatabank_Address + 0x124;
            _P1_OutOfScreen_CaveAddress = InputsDatabank_Address + 0x125;
            _P2_OnScreen_CaveAddress = InputsDatabank_Address + 0x126;
            _P2_OutOfScreen_CaveAddress = InputsDatabank_Address + 0x127;
            _ButtonsState_CaveAddress = InputsDatabank_Address + 0x130;

            WriteBytes(_ButtonsState_CaveAddress, new byte[0x100]);
            WriteBytes(_FGunSensorObject_CaveAddress, BitConverter.GetBytes((UInt64)0));
            WriteBytes(_GunMirCnt_CaveAddress, BitConverter.GetBytes((UInt64)0));

            SetHack_AxisCorrection();
            SetHack_RawAnalogProvider();
            SetHack_FeedGunSensor();
            SetHack_GunSensorEventDown();
            SetHack_GunSensorEventUp();
            SetHack_NativeProviderBank();

            InstallSciOpenAndConfigForceSuccessPatch();
            InstallFGunSensorRecvParserBypass();
            InstallFGunSensorDotUpdateForceOnline();
            InstallFGunSensorUpdateForceReady();
            //InstallFGunSensorCommand04SuccessPatch(); Not needed ? already patched by InstallFGunSensorRecvParserBypass() ???*/

            SetNops(GameProcess_ModuleBaseAddress, _Nop_GunSendChk);
            SetNops(GameProcess_ModuleBaseAddress, _Nop_GunSensorGate);

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        private void SetHack_AxisCorrection()
        {
            Codecave64 CaveMemory = new Codecave64(_Process, GameProcess_ModuleBaseAddress);

            //push rax
            CaveMemory.Write_StrBytes("50");
            //mov rax, _PlayerStatusPointer_Offset
            CaveMemory.Write_StrBytes("48 B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _PlayerStatusPointer_Offset));
            //mov rax, [rax]
            CaveMemory.Write_StrBytes("48 8B 00");
            //add rax, 0x231
            CaveMemory.Write_StrBytes("48 05 31 02 00 00");
            //cmp dword ptr[rax], 01
            CaveMemory.Write_StrBytes("83 38 01");
            //je Label1
            CaveMemory.Write_StrBytes("0F 84 26 00 00 00");
            //add rax, 01
            CaveMemory.Write_StrBytes("48 83 C0 01");
            //cmp dword ptr[rax], 01
            CaveMemory.Write_StrBytes("83 38 01");
            //je Label1
            CaveMemory.Write_StrBytes("0F 84 19 00 00 00");
            //mov [rdi+00000D4C],00000780
            CaveMemory.Write_StrBytes("C7 87 4C 0D 00 00 80 07 00 00");
            //mov [rdi+00000D50],00000438
            CaveMemory.Write_StrBytes("C7 87 50 0D 00 00 38 04 00 00");
            //jmp Next
            CaveMemory.Write_StrBytes("E9 26 00 00 00");

            //Label1:
            //mov rax, _WindowWidth_CaveAddress
            CaveMemory.Write_StrBytes("48 B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_WindowWidth_CaveAddress));
            //mov rax, [rax]
            CaveMemory.Write_StrBytes("48 8B 00");
            //mov [rdi+00000D4C],eax
            CaveMemory.Write_StrBytes("89 87 4C 0D 00 00");
            //mov rax, _WindowHeight_CaveAddress
            CaveMemory.Write_StrBytes("48 B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_WindowHeight_CaveAddress));
            //mov rax, [rax]
            CaveMemory.Write_StrBytes("48 8B 00");
            //mov [rdi+00000D50],eax
            CaveMemory.Write_StrBytes("89 87 50 0D 00 00");

            //Next:
            //pop rax
            CaveMemory.Write_StrBytes("58");

            //mov [rdi+00000D04],00000000
            CaveMemory.Write_StrBytes("C7 87 04 0D 00 00 00 00 00 00");
            //mov [rdi+00000D0C],00000400
            CaveMemory.Write_StrBytes("C7 87 0C 0D 00 00 00 04 00 00");
            //mov[rdi + 00000D14],00000200
            CaveMemory.Write_StrBytes("C7 87 14 0D 00 00 00 02 00 00");
            //mov[rdi + 00000D44],00000000
            CaveMemory.Write_StrBytes("C7 87 44 0D 00 00 00 00 00 00");
            //mov[rdi + 00000CF8],00000000
            CaveMemory.Write_StrBytes("C7 87 F8 0C 00 00 00 00 00 00");
            //mov[rdi + 00000D00],00000400
            CaveMemory.Write_StrBytes("C7 87 00 0D 00 00 00 04 00 00");
            //mov[rdi + 00000D18],00000200
            CaveMemory.Write_StrBytes("C7 87 18 0D 00 00 00 02 00 00");
            //mov[rdi + 00000D48],00000000
            CaveMemory.Write_StrBytes("C7 87 48 0D 00 00 00 00 00 00");
            //movd xmm8,[rdi + 00000D4C]
            CaveMemory.Write_StrBytes("66 44 0F 6E 87 4C 0D 00 00");
            //movd xmm0,[rdi + 00000D0C]
            CaveMemory.Write_StrBytes("66 0F 6E 87 0C 0D 00 00");

            //Inject it
            CaveMemory.InjectToOffset(_AxisCorrection_InjectionStruct, "AxisCorrection");
        }

        /// <summary>
        /// Original functions calls another function to return 0.0 in xmm0
        /// If dl is one of needed values for axis, replacing the call with our own xmm0 value and returning
        /// </summary>
        private void SetHack_RawAnalogProvider()
        {
            Codecave64 CaveMemory = new Codecave64(_Process, GameProcess_ModuleBaseAddress);

            //cmp dl,00
            CaveMemory.Write_StrBytes("80 FA 00");
            //je P1_X
            CaveMemory.Write_StrBytes("74 22");
            //cmp dl,01
            CaveMemory.Write_StrBytes("80 FA 01");
            //je P1_Y
            CaveMemory.Write_StrBytes("74 2C");
            //cmp dl,02
            CaveMemory.Write_StrBytes("80 FA 02");
            //je P2_X
            CaveMemory.Write_StrBytes("74 36");
            //cmp dl,03
            CaveMemory.Write_StrBytes("80 FA 03");
            //je P2_Y
            CaveMemory.Write_StrBytes("74 40");
            //add rcx,00000938
            CaveMemory.Write_StrBytes("48 81 C1 38 09 00 00");
            //mov rax,Hodzero-Win64-Shipping.exe+11A5E80
            CaveMemory.Write_StrBytes("48 B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _RawAnalogProvider_JumpInst_Offset));
            //jmp rax
            CaveMemory.Write_StrBytes("FF E0");

            //P1_X:
            //mov rax,_P1_RawX_CaveAddress
            CaveMemory.Write_StrBytes("48 B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_RawX_CaveAddress));
            //movss xmm0,[rax]
            CaveMemory.Write_StrBytes("F3 0F 10 00");
            //ret
            CaveMemory.Write_StrBytes("C3");

            //P1_Y:
            //mov rax,_P1_RawY_CaveAddress
            CaveMemory.Write_StrBytes("48 B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_RawY_CaveAddress));
            //movss xmm0,[rax]
            CaveMemory.Write_StrBytes("F3 0F 10 00");
            //ret
            CaveMemory.Write_StrBytes("C3");

            //P2_X
            //mov rax,_P2_RawX_CaveAddress
            CaveMemory.Write_StrBytes("48 B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P2_RawX_CaveAddress));
            //movss xmm0,[rax]
            CaveMemory.Write_StrBytes("F3 0F 10 00");
            //ret
            CaveMemory.Write_StrBytes("C3");

            //P2_Y:
            //mov rax,_P2_RawY_CaveAddress
            CaveMemory.Write_StrBytes("48 B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P2_RawY_CaveAddress));
            //movss xmm0,[rax]
            CaveMemory.Write_StrBytes("F3 0F 10 00");
            //ret
            CaveMemory.Write_StrBytes("C3");

            //Inject it
            CaveMemory.InjectToOffset(_RawAnalogProvider_InjectionStruct, "RawAnalogProvider");
        }

        /// <summary>
        /// At that moment the game is asking for a float axis value from the GunSensor struct
        /// Replacing the value by our own
        /// </summary>
        private void SetHack_FeedGunSensor()
        {
            Codecave64 CaveMemory = new Codecave64(_Process, GameProcess_ModuleBaseAddress);

            //push rax
            CaveMemory.Write_StrBytes("50");
            //movzx r8d,dl
            CaveMemory.Write_StrBytes("44 0F B6 C2");
            //shl r8d,02
            CaveMemory.Write_StrBytes("41 C1 E0 02");
            //mov rax,_P1_RawX_CaveAddress
            CaveMemory.Write_StrBytes("48 B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_RawX_CaveAddress));
            //add rax,r8
            CaveMemory.Write_StrBytes("4C 01 C0");
            //movss xmm0,[rax]
            CaveMemory.Write_StrBytes("F3 0F 10 00");
            //pop rax
            CaveMemory.Write_StrBytes("58");
            //xor r8d,r8d
            CaveMemory.Write_StrBytes("45 31 C0");
            //ret
            CaveMemory.Write_StrBytes("C3");

            //Inject it
            CaveMemory.InjectToOffset(_GunSensorAxes_InjectionStruct, "Feed GunSensor");
        }

        /// <summary>
        /// At that moment the game is asking for a GunSensor "Down" Event status (bit operation before / current)
        /// DL has the Event ID :
        /// 1 = P1 Trigger
        /// 6 = P2 Trigger
        /// 4 = P1 ON-SCREEN
        /// 9 = P2 ON-SCREEN
        /// Replacing the value by our own
        /// </summary>
        private void SetHack_GunSensorEventDown()
        {
            Codecave64 CaveMemory = new Codecave64(_Process, GameProcess_ModuleBaseAddress);

            //cmp dl,01
            CaveMemory.Write_StrBytes("80 FA 01");
            //je P1_Trigger
            CaveMemory.Write_StrBytes("74 11");
            //cmp dl,06
            CaveMemory.Write_StrBytes("80 FA 06");
            //je P2_Trigger
            CaveMemory.Write_StrBytes("74 28");
            //cmp dl,04
            CaveMemory.Write_StrBytes("80 FA 04");
            //je P1_OnScreen
            CaveMemory.Write_StrBytes("74 3F");
            //cmp dl,09
            CaveMemory.Write_StrBytes("80 FA 09");
            //je P2_OnScreen
            CaveMemory.Write_StrBytes("74 56");
            //jmp OriginalCode
            CaveMemory.Write_StrBytes("EB 70");

            //P1_Trigger:
            //xor rax, rax
            CaveMemory.Write_StrBytes("48 31 C0");
            //mov al,[_P1_TriggerDown_CaveAddress]
            CaveMemory.Write_StrBytes("A0");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_TriggerDown_CaveAddress));
            //push rax
            CaveMemory.Write_StrBytes("50");
            //mov rax,_P1_TriggerDown_CaveAddress
            CaveMemory.Write_StrBytes("48 B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_TriggerDown_CaveAddress));
            //mov byte ptr [rax],00
            CaveMemory.Write_StrBytes("C6 00 00");
            //pop rax
            CaveMemory.Write_StrBytes("58");
            //ret
            CaveMemory.Write_StrBytes("C3");

            //P2_Trigger
            //xor rax, rax
            CaveMemory.Write_StrBytes("48 31 C0");
            //mov al,[_P2_TriggerDown_CaveAddress]
            CaveMemory.Write_StrBytes("A0");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P2_TriggerDown_CaveAddress));
            //push rax
            CaveMemory.Write_StrBytes("50");
            //mov rax,_P2_TriggerDown_CaveAddress
            CaveMemory.Write_StrBytes("48 B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P2_TriggerDown_CaveAddress));
            //mov byte ptr [rax],00
            CaveMemory.Write_StrBytes("C6 00 00");
            //pop rax
            CaveMemory.Write_StrBytes("58");
            //ret
            CaveMemory.Write_StrBytes("C3");

            //P1_OnScreen
            //xor rax, rax
            CaveMemory.Write_StrBytes("48 31 C0");
            //mov al,[_P1_OnScreen_CaveAddress]
            CaveMemory.Write_StrBytes("A0");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_OnScreen_CaveAddress));
            //push rax
            CaveMemory.Write_StrBytes("50");
            //mov rax,_P1_OnScreen_CaveAddress
            CaveMemory.Write_StrBytes("48 B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_OnScreen_CaveAddress));
            //mov byte ptr [rax],00
            CaveMemory.Write_StrBytes("C6 00 00");
            //pop rax
            CaveMemory.Write_StrBytes("58");
            //ret
            CaveMemory.Write_StrBytes("C3");

            //P2_OnScreen
            //xor rax, rax
            CaveMemory.Write_StrBytes("48 31 C0");
            //mov al,[_P2_OnScreen_CaveAddress]
            CaveMemory.Write_StrBytes("A0");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P2_OnScreen_CaveAddress));
            //push rax
            CaveMemory.Write_StrBytes("50");
            //mov rax,_P2_OnScreen_CaveAddress
            CaveMemory.Write_StrBytes("48 B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P2_OnScreen_CaveAddress));
            //mov byte ptr [rax],00
            CaveMemory.Write_StrBytes("C6 00 00");
            //pop rax
            CaveMemory.Write_StrBytes("58");
            //ret
            CaveMemory.Write_StrBytes("C3");

            //OriginalCode:
            //movzx r8d,dl
            CaveMemory.Write_StrBytes("44 0F B6 C2");
            //sub r8d,01
            CaveMemory.Write_StrBytes("41 83 E8 01");
            //xor al, al
            CaveMemory.Write_StrBytes("32 C0");
            //ret
            CaveMemory.Write_StrBytes("C3");

            //Inject it
            CaveMemory.InjectToOffset(_GunSensorEventDown_InjectionStruct, "GunSensor EventDown");
        }

        /// <summary>
        /// At that moment the game is asking for a Sensor "Up" Event status (bit operation before / current)
        /// DL has the Event ID :
        /// 1 = P1 Trigger
        /// 6 = P2 Trigger
        /// 4 = P1 ON-SCREEN
        /// 9 = P2 ON-SCREEN
        /// Replacing the value by our own
        /// </summary>
        private void SetHack_GunSensorEventUp()
        {
            Codecave64 CaveMemory = new Codecave64(_Process, GameProcess_ModuleBaseAddress);

            //cmp dl,01
            CaveMemory.Write_StrBytes("80 FA 01");
            //je P1_Trigger
            CaveMemory.Write_StrBytes("74 11");
            //cmp dl,06
            CaveMemory.Write_StrBytes("80 FA 06");
            //je P2_Trigger
            CaveMemory.Write_StrBytes("74 28");
            //cmp dl,04
            CaveMemory.Write_StrBytes("80 FA 04");
            //je P1_OutOfScreen
            CaveMemory.Write_StrBytes("74 3F");
            //cmp dl,09
            CaveMemory.Write_StrBytes("80 FA 09");
            //je P2_OutOfScreen
            CaveMemory.Write_StrBytes("74 56");
            //jmp OriginalCode
            CaveMemory.Write_StrBytes("EB 70");

            //P1_Trigger:
            //xor rax, rax
            CaveMemory.Write_StrBytes("48 31 C0");
            //mov al,[_P1_TriggerUp_CaveAddress]
            CaveMemory.Write_StrBytes("A0");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_TriggerUp_CaveAddress));
            //push rax
            CaveMemory.Write_StrBytes("50");
            //mov rax,_P1_TriggerUp_CaveAddress
            CaveMemory.Write_StrBytes("48 B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_TriggerUp_CaveAddress));
            //mov byte ptr [rax],00
            CaveMemory.Write_StrBytes("C6 00 00");
            //pop rax
            CaveMemory.Write_StrBytes("58");
            //ret
            CaveMemory.Write_StrBytes("C3");

            //P2_Trigger
            //xor rax, rax
            CaveMemory.Write_StrBytes("48 31 C0");
            //mov al,[_P2_TriggerUp_CaveAddress]
            CaveMemory.Write_StrBytes("A0");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P2_TriggerUp_CaveAddress));
            //push rax
            CaveMemory.Write_StrBytes("50");
            //mov rax,_P2_TriggerUp_CaveAddress
            CaveMemory.Write_StrBytes("48 B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P2_TriggerUp_CaveAddress));
            //mov byte ptr [rax],00
            CaveMemory.Write_StrBytes("C6 00 00");
            //pop rax
            CaveMemory.Write_StrBytes("58");
            //ret
            CaveMemory.Write_StrBytes("C3");

            //P1_OutOfScreen:
            //xor rax, rax
            CaveMemory.Write_StrBytes("48 31 C0");
            //mov al,[_P1_OutOfScreen_CaveAddress]
            CaveMemory.Write_StrBytes("A0");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_OutOfScreen_CaveAddress));
            //push rax
            CaveMemory.Write_StrBytes("50");
            //mov rax,_P1_OutOfScreen_CaveAddress
            CaveMemory.Write_StrBytes("48 B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_OutOfScreen_CaveAddress));
            //mov byte ptr [rax],00
            CaveMemory.Write_StrBytes("C6 00 00");
            //pop rax
            CaveMemory.Write_StrBytes("58");
            //ret
            CaveMemory.Write_StrBytes("C3");

            //P2_OutOfScreen
            //xor rax, rax
            CaveMemory.Write_StrBytes("48 31 C0");
            //mov al,[_P2_OutOfScreen_CaveAddress]
            CaveMemory.Write_StrBytes("A0");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P2_OutOfScreen_CaveAddress));
            //push rax
            CaveMemory.Write_StrBytes("50");
            //mov rax,_P2_OutOfScreen_CaveAddress
            CaveMemory.Write_StrBytes("48 B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P2_OutOfScreen_CaveAddress));
            //mov byte ptr [rax],00
            CaveMemory.Write_StrBytes("C6 00 00");
            //pop rax
            CaveMemory.Write_StrBytes("58");
            //ret
            CaveMemory.Write_StrBytes("C3");

            //OriginalCode:
            //movzx r8d,dl
            CaveMemory.Write_StrBytes("44 0F B6 C2");
            //sub r8d,01
            CaveMemory.Write_StrBytes("41 83 E8 01");
            //xor al, al
            CaveMemory.Write_StrBytes("32 C0");
            //ret
            CaveMemory.Write_StrBytes("C3");

            //Inject it
            CaveMemory.InjectToOffset(_GunSensorEventUp_InjectionStruct, "GunSensor EventUp");
        }

        /// <summary>
        /// Copy staged inputs into Hodzero's native provider bank at RDI+0x938.
        /// </summary>
        private void SetHack_NativeProviderBank()
        {
            uint[] Slots = new uint[]
            {
                SLOT_P1_START,
                SLOT_P1_SELECT,
                SLOT_P1_SPECIAL,
                SLOT_P2_START,
                SLOT_P2_SELECT,
                SLOT_P2_SPECIAL,
                SLOT_BAKUON,
                SLOT_EYE0,
                SLOT_PROXIMITY,
                SLOT_EMONEY_LEFT,
                SLOT_EMONEY_RIGHT,
                SLOT_EMONEY_CENTER,
                SLOT_CABINET_KIND,
                SLOT_TEST,
                SLOT_SERVICE
            };

            uint[] ProviderOffsets = new uint[]
            {
                0x028, 0x040, 0x058, 0x070, 0x088, 0x0A0, 0x0B8, 0x0D0, 0x0E8, 0x100, 0x118, 0x130, 0x148, 0x160, 0x178
            };

            Codecave64 CaveMemory = new Codecave64(_Process, GameProcess_ModuleBaseAddress);

            //push rax
            CaveMemory.Write_StrBytes("50");
            //push r11
            CaveMemory.Write_StrBytes("41 53");
            //movabs r11, _ButtonState_CaveAddress
            CaveMemory.Write_StrBytes("49 BB");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_ButtonsState_CaveAddress));

            for (int i = 0; i < Slots.Length; i++)
            {
                uint StageOffset = Slots[i] * 5;
                uint ProviderOffset = 0x938 + ProviderOffsets[i];

                //mov al,[r11 + StageOffset]
                CaveMemory.Write_StrBytes("41 8A 83 " + ToHexBytes(StageOffset));
                //mov [rdi+ProviderOffset], al
                CaveMemory.Write_StrBytes("88 87 " + ToHexBytes(ProviderOffset));
                //mov byte ptr[r11 + StageOffset], 00
                CaveMemory.Write_StrBytes("41 C6 83 " + ToHexBytes(StageOffset) + " 00");
                //mov al,[r11 + StageOffset + 1]
                CaveMemory.Write_StrBytes("41 8A 83 " + ToHexBytes(StageOffset + 1));
                //mov [rdi+ProviderOffset + 1], al
                CaveMemory.Write_StrBytes("88 87 " + ToHexBytes(ProviderOffset + 1));
                //mov al,[r11 + StageOffset + 2]
                CaveMemory.Write_StrBytes("41 8A 83 " + ToHexBytes(StageOffset + 2));
                //mov [rdi+ProviderOffset + 2], al
                CaveMemory.Write_StrBytes("88 87 " + ToHexBytes(ProviderOffset + 2));
                //mov byte ptr[r11 + StageOffset + 2], 00
                CaveMemory.Write_StrBytes("41 C6 83 " + ToHexBytes(StageOffset + 2) + " 00");
            }

            //pop r11
            CaveMemory.Write_StrBytes("41 5B");
            //pop rax
            CaveMemory.Write_StrBytes("58");
            //mov rcx, rdi
            CaveMemory.Write_StrBytes("48 8B CF");
            //movabs rax, _NativeIoPostUpdateCall_Ofsset
            CaveMemory.Write_StrBytes("48 B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _NativeIoPostUpdateCall_Ofsset));
            //call rax
            CaveMemory.Write_StrBytes("FF D0");
            //movzx  edx,BYTE PTR [rdi+0xb54]
            CaveMemory.Write_StrBytes("0F B6 97 54 0B 00 00");

            //Inject it
            CaveMemory.InjectToOffset(_NativeIoPostUpdate_InjectionStruct, "Native provider-bank bridge");
        }
        private string ToHexBytes(UInt32 Value)
        {
            return BitConverter.ToString(BitConverter.GetBytes(Value)).Replace("-", " ");
        }

        /// <summary>
        /// For all these calls, force return 1
        /// </summary>
        private void InstallSciOpenAndConfigForceSuccessPatch()
        {
            //mov eax, 1
            //ret
            WriteBytes(GameProcess_ModuleBaseAddress + _SciOpen_Patch_Offset, new byte[] { 0xB8, 0x01, 0x00, 0x00, 0x00, 0xC3, 0x90, 0x90 });
            WriteBytes(GameProcess_ModuleBaseAddress + _SciConfig_Patch_Offset, new byte[] { 0xB8, 0x01, 0x00, 0x00, 0x00, 0xC3, 0x90, 0x90 });
        }

        /// <summary>
        /// Force jump to a null return
        /// </summary>
        private void InstallFGunSensorRecvParserBypass()
        {
            //jmp Hodzero-Win64-Shipping.exe+1E22F2
            WriteBytes(GameProcess_ModuleBaseAddress + _GunRecv_Patch_Offset, new byte[] { 0xE9, 0x4A, 0x03, 0x00, 0x00, 0x90 });
        }

        /// <summary>
        /// Force return 1
        /// </summary>
        private void InstallFGunSensorDotUpdateForceOnline()
        {
            //mov eax, 1
            //ret
            WriteBytes(GameProcess_ModuleBaseAddress + _GunDotUpdOnline_Patch_Offset, new byte[] { 0xB8, 0x01, 0x00, 0x00, 0x00, 0xC3 });
        }

        /// <summary>
        /// Retrieve FGunSensor object for later use and sets the Update loop to succes/return
        /// </summary>
        private void InstallFGunSensorUpdateForceReady()
        {
            Codecave64 CaveMemory = new Codecave64(_Process, GameProcess_ModuleBaseAddress);

            //mov r11, _FGunSensorObject_CaveAddress
            CaveMemory.Write_StrBytes("49 BB");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_FGunSensorObject_CaveAddress));
            //mov [r11],rcx
            CaveMemory.Write_StrBytes("49 89 0B");
            //mov r11, _GunMirCnt_CaveAddress
            CaveMemory.Write_StrBytes("49 BB");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_GunMirCnt_CaveAddress));
            //inc qword ptr [r11]
            CaveMemory.Write_StrBytes("49 FF 03");

            //mov byte ptr [rcx+10],01
            CaveMemory.Write_StrBytes("C6 41 10 01");
            //mov byte ptr [rcx+11],01
            CaveMemory.Write_StrBytes("C6 41 11 01");
            //mov byte ptr [rcx+000000A5],01
            CaveMemory.Write_StrBytes("C6 81 A5 00 00 00 01");
            //mov byte ptr [rcx+000000A7],01
            CaveMemory.Write_StrBytes("C6 81 A7 00 00 00 01");
            //mov byte ptr [rcx+000000B9],01
            CaveMemory.Write_StrBytes("C6 81 B9 00 00 00 01");
            //mov byte ptr [rcx+000000BB],01
            CaveMemory.Write_StrBytes("C6 81 BB 00 00 00 01");
            //mov byte ptr [rcx+000000BF],01
            CaveMemory.Write_StrBytes("C6 81 BF 00 00 00 01");
            //mov byte ptr [rcx+000000C0],01
            CaveMemory.Write_StrBytes("C6 81 C0 00 00 00 01");
            //mov [rcx+000000C4],00000000
            CaveMemory.Write_StrBytes("C7 81 C4 00 00 00 00 00 00 00");
            //mov [rcx+000000C8],00000000
            CaveMemory.Write_StrBytes("C7 81 C8 00 00 00 00 00 00 00");
            //ret
            CaveMemory.Write_StrBytes("C3");

            //Inject it
            CaveMemory.InjectToOffset(_GunUpdateRdy_InjectionStruct, "GunUpdate");
        }

        //private void InstallFGunSensorCommand04SuccessPatch()
        //{
        //    private InjectionStruct _GunCmd4OkPatch = new InjectionStruct(0x001E1FC9, 16);
        //    Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
        //    CaveMemory.Open();
        //    CaveMemory.Alloc(0x100);
        //    CaveMemory.Write_StrBytes("31 C0");
        //    CaveMemory.Write_jmp(GameProcess_ModuleBaseAddress + 0x001E22F2);
        //    InstallAbsoluteJump(_GunCmd4OkPatch, CaveMemory.CaveAddress);
        //}

        #endregion

        #region Outputs Hack

        protected override void Apply_OutputsMemoryHack()
        {
            Create_OutputsDataBank();
            _OutputState_CaveAddress = OutputsDatabank_Address;

            SetHack_CaptureOutputState();

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        private void SetHack_CaptureOutputState()
        {
            Codecave64 CaveMemory = new Codecave64(_Process, GameProcess_ModuleBaseAddress);

            //push rax
            CaveMemory.Write_StrBytes("50");
            //mov rax, _OutputState_CaveAddress
            CaveMemory.Write_StrBytes("48 B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_OutputState_CaveAddress));
            //mov QWORD PTR [rax],rcx
            CaveMemory.Write_StrBytes("48 89 08");
            //pop rax
            CaveMemory.Write_StrBytes("58");
            //mov QWORD PTR [rsp+0x8],rcx
            CaveMemory.Write_StrBytes("48 89 4C 24 08");
            //push rbx
            CaveMemory.Write_StrBytes("53");
            //push rsi
            CaveMemory.Write_StrBytes("56");
            //push rdi
            CaveMemory.Write_StrBytes("57");
            //push r14
            CaveMemory.Write_StrBytes("41 56");
            //push r15
            CaveMemory.Write_StrBytes("41 57");
            //sub rsp, 0x90
            CaveMemory.Write_StrBytes("48 81 EC 90 00 00 00");

            //Inject it
            CaveMemory.InjectToOffset(_OutputState_InjectionStruct, "Output state");
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
            else if (e.ButtonName.Equals("SERVICE"))
            {
                SetNativeDatabankButtonSlot(SLOT_SERVICE, true);
            }
            else if (e.ButtonName.Equals("TEST"))
            {
                SetNativeDatabankButtonSlot(SLOT_TEST, true);
            }
            else if (e.ButtonName.Equals("BAKUON"))
            {
                SetNativeDatabankButtonSlot(SLOT_BAKUON, true);
            }
            else if (e.ButtonName.Equals("E-MONEY LEFT"))
            {
                SetNativeDatabankButtonSlot(SLOT_EMONEY_LEFT, true);
            }
            else if (e.ButtonName.Equals("E-MONEY CENTER"))
            {
                SetNativeDatabankButtonSlot(SLOT_EMONEY_CENTER, true);
            }
            else if (e.ButtonName.Equals("E-MONEY RIGHT"))
            {
                SetNativeDatabankButtonSlot(SLOT_EMONEY_RIGHT, true);
            }
            else if (e.ButtonName.Equals("EYE COVER"))
            {
                SetNativeDatabankButtonSlot(SLOT_EYE0, true);
            }
            else if (e.ButtonName.Equals("MOTION SENSOR"))
            {
                SetNativeDatabankButtonSlot(SLOT_PROXIMITY, true);
            }
            else if (e.ButtonName.Equals("CABINET KIND"))
            {
                SetNativeDatabankButtonSlot(SLOT_CABINET_KIND, true);
            }
        }

        protected override void GameController_OnButtonUp(object sender, VirtualGameControler.ButtonDownEventArgs e)
        {
            if (e.ButtonName.Equals("SERVICE"))
            {
                SetNativeDatabankButtonSlot(SLOT_SERVICE, false);
            }
            else if (e.ButtonName.Equals("TEST"))
            {
                SetNativeDatabankButtonSlot(SLOT_TEST, false);
            }
            else if (e.ButtonName.Equals("BAKUON"))
            {
                SetNativeDatabankButtonSlot(SLOT_BAKUON, false);
            }
            else if (e.ButtonName.Equals("E-MONEY LEFT"))
            {
                SetNativeDatabankButtonSlot(SLOT_EMONEY_LEFT, false);
            }
            else if (e.ButtonName.Equals("E-MONEY CENTER"))
            {
                SetNativeDatabankButtonSlot(SLOT_EMONEY_CENTER, false);
            }
            else if (e.ButtonName.Equals("E-MONEY RIGHT"))
            {
                SetNativeDatabankButtonSlot(SLOT_EMONEY_RIGHT, false);
            }
            else if (e.ButtonName.Equals("EYE COVER"))
            {
                SetNativeDatabankButtonSlot(SLOT_EYE0, false);
            }
            else if (e.ButtonName.Equals("MOTION SENSOR"))
            {
                SetNativeDatabankButtonSlot(SLOT_PROXIMITY, false);
            }
            else if (e.ButtonName.Equals("CABINET KIND"))
            {
                SetNativeDatabankButtonSlot(SLOT_CABINET_KIND, false);
            }
        }

        protected override void UpdateAxis(PlayerID Id, int X, int Y)
        {
            if (Id != PlayerID.None)
            {
                byte[] bufferX = BitConverter.GetBytes(X);
                byte[] bufferY = BitConverter.GetBytes(Y);

                float fX = (float)X / 1023.0f;
                float fY = (float)Y / 1023.0f;

                WriteBytes(_P1_RawX_CaveAddress + (8 * (uint)Id), BitConverter.GetBytes(fX));
                WriteBytes(_P1_RawY_CaveAddress + (8 * (uint)Id), BitConverter.GetBytes(fY));

                //Activating Reload when aiming near the screen borders
                if (X > 2 && X < 1021 && Y > 2 && Y < 1021)
                    WriteByte(_P1_OnScreen_CaveAddress + (2 * (uint)Id), 0x01);
                else
                    WriteByte(_P1_OutOfScreen_CaveAddress + (2 * (uint)Id), 0x01);
            }
        }

        protected override void PlayerController_OnButtonDown(object sender, VirtualGameControler.ButtonDownEventArgs e)
        {
            if (e.Id != PlayerID.None)
            {
                UInt32 PlayerIndex = (UInt32)e.Id;

                if (e.ButtonName.Equals("TRIGGER"))
                {
                    WriteByte(_P1_TriggerDown_CaveAddress + (2 * PlayerIndex), 0x01);
                    SetNativeDatabankButtonSlot(SLOT_P1_TRIGGER + (5 * PlayerIndex), true);
                }
                else if (e.ButtonName.Equals("RELOAD"))
                {
                    WriteByte(_P1_OutOfScreen_CaveAddress + (2 * PlayerIndex), 0x01);
                }
                else if (e.ButtonName.Equals("SELECT"))
                {
                    SetNativeDatabankButtonSlot(SLOT_P1_SELECT + (5 * PlayerIndex), true);
                }
                else if (e.ButtonName.Equals("SPECIAL"))
                {
                    SetNativeDatabankButtonSlot(SLOT_P1_SPECIAL + (5 * PlayerIndex), true);
                }
                else if (e.ButtonName.Equals("START"))
                {
                    SetNativeDatabankButtonSlot(SLOT_P1_START + (5 * PlayerIndex), true);
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
                    WriteByte(_P1_TriggerUp_CaveAddress + (2 * PlayerIndex), 0x01);
                    SetNativeDatabankButtonSlot(SLOT_P1_TRIGGER + (5 * PlayerIndex), false);
                }
                else if (e.ButtonName.Equals("RELOAD"))
                {
                    WriteByte(_P1_OnScreen_CaveAddress + (2 * PlayerIndex), 0x01);
                }
                else if (e.ButtonName.Equals("SELECT"))
                {
                    SetNativeDatabankButtonSlot(SLOT_P1_SELECT + (5 * PlayerIndex), false);
                }
                else if (e.ButtonName.Equals("SPECIAL"))
                {
                    SetNativeDatabankButtonSlot(SLOT_P1_SPECIAL + (5 * PlayerIndex), false);
                }
                else if (e.ButtonName.Equals("START"))
                {
                    SetNativeDatabankButtonSlot(SLOT_P1_START + (5 * PlayerIndex), false);
                }
            }
        }

        private UInt64 GetNativeButtonDatabankSlotAddress(uint Slot)
        {
            if (_ButtonsState_CaveAddress == 0 || Slot < 0 || Slot >= BUTTON_SLOT_COUNT)
                return 0;

            return _ButtonsState_CaveAddress + ((UInt64)Slot * 5);
        }

        private void SetNativeDatabankButtonSlot(uint Slot, bool Held)
        {
            UInt64 SlotAddress = GetNativeButtonDatabankSlotAddress(Slot);
            if (SlotAddress == 0)
                return;

            byte PreviousHeld = _ButtonHeld[Slot];
            byte CurrentHeld = Held ? (byte)1 : (byte)0;
            byte Rising = (CurrentHeld == 1 && PreviousHeld == 0) ? (byte)1 : (byte)0;
            byte Falling = (CurrentHeld == 0 && PreviousHeld == 1) ? (byte)1 : (byte)0;

            if (Rising != 0)
                WriteByte(SlotAddress, Rising);
            WriteByte(SlotAddress + 1, CurrentHeld);
            if (Falling != 0)
                WriteByte((SlotAddress + 2), Falling);
            WriteByte(SlotAddress + 3, 0);
            WriteByte(SlotAddress + 4, PreviousHeld);

            _ButtonHeld[Slot] = CurrentHeld;
        }

        #endregion

        #region Outputs

        /// <summary>
        /// Create the Output list that we will be looking for and forward to MameHooker
        /// </summary>
        protected override void CreateOutputList()
        {
            _ShotCount = new UInt32[_GameConfig.PlayerCount];
            _LastShotCount = new UInt32[_GameConfig.PlayerCount];

            _Outputs = new List<GameOutput>
            {
                new GameOutput(OutputId.CoinBlocker_1),
                new GameOutput(OutputId.CoinBlocker_2),
                new GameOutput(OutputId.P1_LmpStart),
                new GameOutput(OutputId.P2_LmpStart),
                new GameOutput(OutputId.Lmp_Bakuon),
                new GameOutput(OutputId.Lmp_EMoneyCenter),
                new GameOutput(OutputId.Lmp_EMoneyRight),
                new GameOutput(OutputId.Lmp_EMoneyLeft),
                new GameOutput(OutputId.Lmp_Eye0),
                new GameOutput(OutputId.P1_LmpGun_R),
                new GameOutput(OutputId.P1_LmpGun_G),
                new GameOutput(OutputId.P1_LmpGun_B),
                new GameOutput(OutputId.P2_LmpGun_R),
                new GameOutput(OutputId.P2_LmpGun_G),
                new GameOutput(OutputId.P2_LmpGun_B),
                new GameOutput(OutputId.P1_LmpCeiling_R),
                new GameOutput(OutputId.P1_LmpCeiling_G),
                new GameOutput(OutputId.P1_LmpCeiling_B),
                new GameOutput(OutputId.P2_LmpCeiling_R),
                new GameOutput(OutputId.P2_LmpCeiling_G),
                new GameOutput(OutputId.P2_LmpCeiling_B),
                new GameOutput(OutputId.Lmp_ZombieObject_R),
                new GameOutput(OutputId.Lmp_ZombieObject_G),
                new GameOutput(OutputId.Lmp_ZombieObject_B),
                new GameOutput(OutputId.LmpRear_R),
                new GameOutput(OutputId.LmpRear_G),
                new GameOutput(OutputId.LmpRear_B),
                new GameOutput(OutputId.P1_Ammo),
                new GameOutput(OutputId.P2_Ammo),
                new AsyncGameOutput(OutputId.P1_CtmRecoil, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_ON_DELAY), ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_OFF_DELAY), 0),
                new AsyncGameOutput(OutputId.P2_CtmRecoil, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_ON_DELAY), ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_OFF_DELAY), 0),
                new GameOutput(OutputId.P1_Life),
                new GameOutput(OutputId.P2_Life),
                new AsyncGameOutput(OutputId.P1_Damaged, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_DAMAGED_DELAY), 100, 0),
                new AsyncGameOutput(OutputId.P2_Damaged, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_DAMAGED_DELAY), 100, 0),
                //_Outputs.Add(new GameOutput(OutputId.Credits));
            };
        }

        /// <summary>
        /// Update all Outputs values before sending them to MameHooker
        /// </summary>
        public override void UpdateOutputValues()
        {
            UpdateNativeScarletDawnOutputs();

            //Japanese and US version of the game have totally different pointers for custom outputs fetching
            if (_TargetProcess_Md5Hash == _KnownMd5Prints["Hodzero-Win64-Shipping.exe - SDET"])
                UpdateCustomOutputs_SDET();
            else if (_TargetProcess_Md5Hash == _KnownMd5Prints["Hodzero-Win64-Shipping.exe - SDEE"])
                UpdateCustomOutputs_SDEE();
        }

        // Additional Outputs by PaCiFiKbAllA
        private void UpdateNativeScarletDawnOutputs()
        {
            try
            {
                UInt64 State = ReadPtr(_OutputState_CaveAddress);
                if (State == 0)
                    return;

                UInt32 Flags = ReadUInt32(State + 0x18);
                SetOutputValue(OutputId.CoinBlocker_2, ((Flags & 0x00000004) != 0) ? 1 : 0);
                SetOutputValue(OutputId.P2_LmpStart, ((Flags & 0x00000008) != 0) ? 1 : 0);
                SetOutputValue(OutputId.CoinBlocker_1, ((Flags & 0x00000020) != 0) ? 1 : 0);
                SetOutputValue(OutputId.P1_LmpStart, ((Flags & 0x00000040) != 0) ? 1 : 0);
                SetOutputValue(OutputId.Lmp_Bakuon, ((Flags & 0x00000400) != 0) ? 1 : 0);
                SetOutputValue(OutputId.Lmp_EMoneyCenter, ((Flags & 0x00000800) != 0) ? 1 : 0);
                SetOutputValue(OutputId.Lmp_EMoneyRight, ((Flags & 0x00004000) != 0) ? 1 : 0);
                SetOutputValue(OutputId.Lmp_EMoneyLeft, ((Flags & 0x00008000) != 0) ? 1 : 0);
                SetOutputValue(OutputId.Lmp_Eye0, ((Flags & 0x00400000) != 0) ? 1 : 0);
                SetOutputValue(OutputId.P1_LmpGun_R, ReadByte(State + 0x1E));
                SetOutputValue(OutputId.P1_LmpGun_G, ReadByte(State + 0x20));
                SetOutputValue(OutputId.P1_LmpGun_B, ReadByte(State + 0x22));
                SetOutputValue(OutputId.P2_LmpGun_R, ReadByte(State + 0x1F));
                SetOutputValue(OutputId.P2_LmpGun_G, ReadByte(State + 0x21));
                SetOutputValue(OutputId.P2_LmpGun_B, ReadByte(State + 0x23));
                SetOutputValue(OutputId.P1_LmpCeiling_R, ReadFloatRgbByte(State + 0x30));
                SetOutputValue(OutputId.P1_LmpCeiling_G, ReadFloatRgbByte(State + 0x34));
                SetOutputValue(OutputId.P1_LmpCeiling_B, ReadFloatRgbByte(State + 0x38));
                SetOutputValue(OutputId.P2_LmpCeiling_R, ReadFloatRgbByte(State + 0x3C));
                SetOutputValue(OutputId.P2_LmpCeiling_G, ReadFloatRgbByte(State + 0x40));
                SetOutputValue(OutputId.P2_LmpCeiling_B, ReadFloatRgbByte(State + 0x44));
                SetOutputValue(OutputId.Lmp_ZombieObject_R, ReadFloatRgbByte(State + 0x60));
                SetOutputValue(OutputId.Lmp_ZombieObject_G, ReadFloatRgbByte(State + 0x64));
                SetOutputValue(OutputId.Lmp_ZombieObject_B, ReadFloatRgbByte(State + 0x68));
                SetOutputValue(OutputId.LmpRear_R, ReadFloatRgbByte(State + 0x6C));
                SetOutputValue(OutputId.LmpRear_G, ReadFloatRgbByte(State + 0x70));
                SetOutputValue(OutputId.LmpRear_B, ReadFloatRgbByte(State + 0x74));
            }
            catch { }
        }
        private int ReadFloatRgbByte(UInt64 Address)
        {
            return FloatRgbToByte(BitConverter.ToSingle(ReadBytes(Address, 4), 0));
        }
        private int FloatRgbToByte(float Value)
        {
            if (Single.IsNaN(Value) || Single.IsInfinity(Value))
                return 0;

            if (Value < 0.0f)
                Value = 0.0f;

            if (Value > 1.0f)
                Value = 1.0f;

            return Convert.ToInt32(Math.Round(Value * 255.0f));
        }

        //Custom Outputs
        private void UpdateCustomOutputs_SDET()
        {
            for (int i = 0; i < _GameConfig.PlayerCount; i++)
            {
                _Life[i] = 0;
                _Ammo[i] = 0;
            }

            //TargetRenderPool byte value has nothing to do with outputs, but for some reasons it's only set to "1" if at least one player is playing
            //Using that as a pre-filter to make sure Players pointers are assigned
            byte bFilter = ReadByte(GameProcess_ModuleBaseAddress + _TargetRenderPool_Offset + 0x158);
            if (bFilter != 0)
            {
                UInt64 Ptr1 = ReadPtr(GameProcess_ModuleBaseAddress + _PlayerStatusPointer_Offset);
                if (Ptr1 != 0)
                {
                    //Customs Outputs
                    //Player Status :
                    //[0] : Inactive
                    //[1] : In-Game
                    int P1_Status = ReadByte(Ptr1 + 0x231);
                    int P2_Status = ReadByte(Ptr1 + 0x232);

                    if (P1_Status != 0)
                    {
                        UInt64 PtrAmmo = ReadPtrChain(GameProcess_ModuleBaseAddress + 0x032B5B88, new UInt64[] { 0x30, 0xB0, 0x90 });
                        _Ammo[0] = ReadByte(PtrAmmo + 0x378);
                        _Life[0] = ReadByte(Ptr1 + 0x364);

                        //Custom Recoil:
                        //Computing based on THAT ammo value is wrong, as reloading sets it to "0" before adding full bullet count...this generates unwanted recoils
                        //Same thing for gun change....goes through the zero state
                        //if (_Ammo[0] < _LastAmmo[0])
                        //   SetOutputValue(OutputId.P1_CtmRecoil, 1);

                        //Instead, that value increase for each bullet fired. Not always +1 (depending on the weapon), but as long at it's increasing....
                        _ShotCount[0] = ReadUInt32(Ptr1 + 0x3A0);
                        if (_ShotCount[0] > _LastShotCount[0])
                            SetOutputValue(OutputId.P1_CtmRecoil, 1);

                        //[Damaged] custom Output                
                        if (_Life[0] < _LastLife[0])
                            SetOutputValue(OutputId.P1_Damaged, 1);
                    }

                    if (P2_Status != 0)
                    {
                        UInt64 PtrAmmo = ReadPtrChain(GameProcess_ModuleBaseAddress + 0x032B5B88, new UInt64[] { 0x30, 0xB0, 0x98 });
                        _Ammo[1] = ReadByte(PtrAmmo + 0x378);
                        _Life[1] = ReadByte(Ptr1 + 0x43C);

                        //Custom Recoil:
                        //Computing based on THAT ammo value is wrong, as reloading sets it to "0" before adding full bullet count...this generates unwanted recoils
                        //Same thing for gun change....goes through the zero state
                        //if (_Ammo[1] < _LastAmmo[1])
                        //    SetOutputValue(OutputId.P2_CtmRecoil, 1);
                        //Instead, that value increase for each bullet fired. Not always +1 (depending on the weapon), but as long at it's increasing....
                        _ShotCount[1] = ReadUInt32(Ptr1 + 0x478);
                        if (_ShotCount[1] > _LastShotCount[1])
                            SetOutputValue(OutputId.P2_CtmRecoil, 1);

                        //[Damaged] custom Output  
                        if (_Life[1] < _LastLife[1])
                            SetOutputValue(OutputId.P2_Damaged, 1);
                    }
                }
            }

            for (uint i = 0; i < _GameConfig.PlayerCount; i++)
            {
                _LastAmmo[i] = _Ammo[i];
                _LastLife[i] = _Life[i];
                _LastShotCount[i] = _ShotCount[i];

                SetOutputValue(OutputId.P1_Ammo + i, _Ammo[i]);
                SetOutputValue(OutputId.P1_Life + i, _Life[i]);
            }

            UInt64 PtrCredits = ReadPtrChain(GameProcess_ModuleBaseAddress + 0x032FC700, new UInt64[] { 0x88, 0x90, 0x50 });
            SetOutputValue(OutputId.Credits, ReadByte(PtrCredits + 0x21C));
        }

        private void UpdateCustomOutputs_SDEE()
        {
            for (int i = 0; i < _GameConfig.PlayerCount; i++)
            {
                _Life[i] = 0;
                _Ammo[i] = 0;
            }

            //TargetRenderPool byte value has nothing to do with outputs, but for some reasons it's only set to "1" if at least one player is playing
            //Using that as a pre-filter to make sure Players pointers are assigned
            byte bFilter = ReadByte(GameProcess_ModuleBaseAddress + _TargetRenderPool_Offset + 0x158);
            if (bFilter != 0)
            {
                UInt64 Ptr1 = ReadPtrChain(GameProcess_ModuleBaseAddress + 0x032F0260, new UInt64[] { 0x68, 0x10, 0x380 });
                if (Ptr1 != 0)
                {
                    //Customs Outputs
                    //Player Status :
                    //[0] : Inactive
                    //[1] : In-Game
                    int P1_Status = ReadByte(Ptr1);
                    int P2_Status = ReadByte(Ptr1 + 0x01);

                    if (P1_Status != 0)
                    {
                        UInt64 PtrAmmo = ReadPtrChain(GameProcess_ModuleBaseAddress + 0x032F06B0, new UInt64[] { 0x8, 0x08, 0x3B0 });
                        _Ammo[0] = ReadByte(PtrAmmo + 0x350);
                        UInt64 PtrLife = ReadPtrChain(GameProcess_ModuleBaseAddress + 0x032F2988, new UInt64[] { 0x140 });
                        _Life[0] = ReadByte(PtrLife + 0x324);

                        //Custom Recoil:
                        //Computing based on THAT ammo value is wrong, as reloading sets it to "0" before adding full bullet count...this generates unwanted recoils
                        //Same thing for gun change....goes through the zero state
                        //if (_Ammo[0] < _LastAmmo[0])
                        //   SetOutputValue(OutputId.P1_CtmRecoil, 1);

                        //Instead, that value increase for each bullet fired. Not always +1 (depending on the weapon), but as long at it's increasing....
                        _ShotCount[0] = ReadUInt32(PtrLife + 0x3A0);
                        if (_ShotCount[0] > _LastShotCount[0])
                            SetOutputValue(OutputId.P1_CtmRecoil, 1);

                        //[Damaged] custom Output                
                        if (_Life[0] < _LastLife[0])
                            SetOutputValue(OutputId.P1_Damaged, 1);
                    }

                    if (P2_Status != 0)
                    {
                        UInt64 PtrAmmo = ReadPtrChain(GameProcess_ModuleBaseAddress + 0x032F06B0, new UInt64[] { 0x08, 0x08, 0x3C0 });
                        _Ammo[1] = ReadByte(PtrAmmo + 0x350);
                        UInt64 PtrLife = ReadPtrChain(GameProcess_ModuleBaseAddress + 0x032F2988, new UInt64[] { 0x140 });
                        _Life[1] = ReadByte(PtrLife + 0x3FC);

                        //Custom Recoil:
                        //Computing based on THAT ammo value is wrong, as reloading sets it to "0" before adding full bullet count...this generates unwanted recoils
                        //Same thing for gun change....goes through the zero state
                        //if (_Ammo[1] < _LastAmmo[1])
                        //    SetOutputValue(OutputId.P2_CtmRecoil, 1);
                        //Instead, that value increase for each bullet fired. Not always +1 (depending on the weapon), but as long at it's increasing....
                        _ShotCount[1] = ReadUInt32(PtrLife + 0x478);
                        if (_ShotCount[1] > _LastShotCount[1])
                            SetOutputValue(OutputId.P2_CtmRecoil, 1);

                        //[Damaged] custom Output  
                        if (_Life[1] < _LastLife[1])
                            SetOutputValue(OutputId.P2_Damaged, 1);
                    }
                }
            }

            for (uint i = 0; i < _GameConfig.PlayerCount; i++)
            {
                _LastAmmo[i] = _Ammo[i];
                _LastLife[i] = _Life[i];
                _LastShotCount[i] = _ShotCount[i];

                SetOutputValue(OutputId.P1_Ammo + i, _Ammo[i]);
                SetOutputValue(OutputId.P1_Life + i, _Life[i]);
            }
        }

        #endregion
    }
}
