using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UsCore.Config;
using UsCore.Launcher;
using UsCore.Logging;
using UsCore.MameOutput;
using UsCore.Memory;
using UsCore.VirtualControllers;

namespace U_Shoot.Games
{
    public class Game_ArcadepcElevatorActionInvasion : BaseGame64
    {
        protected override bool PATCH_PEB_DEBUG_FLAG { get { return false; } }

        private const string USBPLUGINSDLL_DLL_NAME = "UsbPluginsDll.dll";
        private UInt64 _UsbPluginsDll_BaseAddress;

        #region LOADER MEMORY ADDRESSES                
        private UInt64 _InitDongle_Dll_Patch_Offset = 0x000140CB;
        private UInt64 _IsDongleVerify_Dll_Patch_Offset = 0x000142F7;
        private UInt64 _OpenESIODevice_Dll_Patch_Offset = 0x000143B0;
        private UInt64 _IsExistESIODevice_Dll_Patch_Offset = 0x0000B1A0;
        private UInt64 _GetESIoData_Dll_Patch_Offset = 0x00013B10;
        private UInt64 _ParseIoData_Patch_Offset = 0x007924D6;
        private UInt64 _ParseIoData_CaveAddress = 0x0292D3D4;
        private UInt64 _GameStateTick_Patch_Offset = 0x007B34A7;
        private UInt64 _GameStateTick_CaveAddress = 0x0292D37F;
        private UInt64 _CreditsRead_Patch_Offset = 0x007B81BF;
        private UInt64 _CreditsWrite_Patch_Offset = 0x007B807E;
        private UInt64 _IsGunCalibrated_Patch_Offset = 0x00773717;
        private UInt64 _IoBoard_Payload_Offset = 0x3A91870 + 0x2CC;
        #endregion

        #region INPUTS MEMORY ADDRESSES    
        private  UInt64 _P1_X_Offset = 0x3A91850;
        private  UInt64 _P1_Y_Offset = 0x3A91854;
        private  UInt64 _P1_Trigger_Offset = 0x3A91858;
        private  UInt64 _P1_Credits_Offset = 0x3A9185C;
        //private  UInt64 _P2_X_Offset = 0x3A91860;
        //private  UInt64 _P2_Y_Offset = 0x3A91864;
        private  UInt64 _P2_Trigger_Offset = 0x3A91868;
        private  UInt64 _P2_Credits_Offset = 0x3A9186C;        
        private NopStruct64 _Nop_KeyboardAndMouse = new NopStruct64(0x00B85E4A, 5);        
        #endregion

        #region OUTPUTS MEMORY ADDRESSES
        private UInt64 _DllOutputsBase_Offset = 0x0002a290;
        private InjectionStruct64 _Recoil_InjectionStruct = new InjectionStruct64(0x794D54, 15);
        private InjectionStruct64 _Damage_InjectionStruct = new InjectionStruct64(0x796360, 16);

        //Custom Data
        private UInt64 _Recoil_CaveAddress = 0;
        private UInt64 _Damage_CaveAddress = 0;
        #endregion

        private Dictionary<string, string> _KnownMd5_UsbPluginsDll;

        public Game_ArcadepcElevatorActionInvasion(GameConfig Config) : base(Config)
        {
            _KnownMd5Prints.Add("Elevator Action  Invasion v1.6.1[C] clean dump", "bc34cc1f04de2977eeb4e2fd5895ebf4");
            _KnownMd5Prints.Add("Elevator Action  Invasion v1.6.1[C] patched by Argonlefou", "d2c4c99a86648b34316ca4f61a23c1ea");
            _KnownMd5_UsbPluginsDll = new Dictionary<string, string>
            {
                { "Elevator Action  Invasion v1.6.1[C] clean dll", "b42880b879f5f26e7063a7e86cfc0671" },
                { "Elevator Action  Invasion v1.6.1[C] patched dll by Argonlefou", "ce53b9d2c88c43d72e3f62ee70aa4dc9" }
            };
        }

        //That game will need to patch DLL a load time before main exe starts
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
                if (string.Equals(Path.GetFileName(e.DllPath), USBPLUGINSDLL_DLL_NAME, StringComparison.OrdinalIgnoreCase))
                {
                    Logger.WriteLog("==> Wanted DLL found !");
                    //HANDLE should always be in 32Bits range (https://learn.microsoft.com/fr-fr/windows/win32/winprog64/interprocess-communication?redirectedfrom=MSDN)
                    Logger.WriteLog("  + hModule: 0x" + e.LoadDllInfo.hFile.ToString("X8"));
                    Logger.WriteLog("  + fUnicode: " + e.LoadDllInfo.fUnicode.ToString());

                    _UsbPluginsDll_BaseAddress = (UInt64)e.LoadDllInfo.lpBaseOfDll;

                    string UsbPluginPath = _GameConfig.TargetPath.Replace(Path.GetFileName(_GameConfig.TargetFileName), @"..\..\Plugins\" + USBPLUGINSDLL_DLL_NAME);
                    CheckMd5(UsbPluginPath, _KnownMd5_UsbPluginsDll);

                    SetHack_UsbPluginDll();

                    RaiseGameHookedEvent();
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
                    double dMaxX = 1079.0;
                    double dMaxY = 1919.0;
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
        }

        #region Loader Hack

        protected override void Apply_LoaderMemoryHack()
        {
            //Force overriding read data from the COM port by our own
            //Creating codecave in an empty space
            // -- Code taken from DemulShooter dll patcher as is ---
            //lea r14,["ESGame
            //movzx ebx,byte ptr [r14+000002CE]
            //jmp "ESGame-Win64-Shipping.exe"+7924DE
            WriteBytes(GameProcess_ModuleBaseAddress + _ParseIoData_CaveAddress, new byte[] { 0x4C, 0x8D, 0x35, 0x95, 0x44, 0x16, 0x01, 0x41, 0x0F, 0xB6, 0x9E, 0xCE, 0x02, 0x00, 0x00, 0xE9, 0xF6, 0x50, 0xE6, 0xFD });
            //Injection
            //jmp _ParseIoData_CaveAddress
            WriteBytes(GameProcess_ModuleBaseAddress + _ParseIoData_Patch_Offset, new byte[] { 0xE9, 0xF9, 0xAE, 0x19, 0x02, 0x90, 0x90, 0x90 });

            //Force overriding bad read values from GUN api with good flag, and force values to be read from another memory location
            //which then will be written by any tool to feed data
            //Creating codecave in an empty space
            // -- Code taken from DemulShooter dll patcher as is ---
            //int 3
            //int 3
            //int 3
            //push rax
            //push rbx
            //shl rbx,04
            //xor rax,rax
            //mov [rcx+r15+34],00000001
            //lea rax,["ESGame-Win64-Shipping.exe"+3A91853]
            //add rax,rbx
            //mov eax,[rax]
            //mov [rcx+r15+14],eax
            //lea rax,["ESGame-Win64-Shipping.exe"+3A91857]
            //add rax,rbx
            //mov eax,[rax]
            //mov [rcx+r15+18],eax
            // lea rax,["ESGame-Win64-Shipping.exe"+3A9185B]
            //add rax,rbx
            //mov eax,[rax]
            //mov [rcx+r15+51],eax
            //pop rbx
            //pop rax
            //cmp dword ptr [rcx+r15+34],00
            //jmp "ESGame-Win64-Shipping.exe"+7B34B0
            WriteBytes(GameProcess_ModuleBaseAddress + _GameStateTick_CaveAddress, new byte[] { 0xCC, 0xCC, 0xCC, 0x50, 0x53, 0x48, 0xC1, 0xE3, 0x04, 0x48, 0x31, 0xC0, 0x42, 0xC7, 0x44, 0x39, 0x34, 0x01, 0x00, 0x00, 0x00, 0x48, 0x8D, 0x05, 0xB5, 0x44, 0x16, 0x01, 0x48, 0x01, 0xD8, 0x8B, 0x00, 0x42, 0x89, 0x44, 0x39, 0x14, 0x48, 0x8D, 0x05, 0xA8, 0x44, 0x16, 0x01, 0x48, 0x01, 0xD8, 0x8B, 0x00, 0x42, 0x89, 0x44, 0x39, 0x18, 0x48, 0x8D, 0x05, 0x9B, 0x44, 0x16, 0x01, 0x48, 0x01, 0xD8, 0x8B, 0x00, 0x42, 0x89, 0x44, 0x39, 0x51, 0x5B, 0x58, 0x42, 0x83, 0x7C, 0x39, 0x34, 0x00, 0xE9, 0xD9, 0x60, 0xE8, 0xFD });
            //Injection
            //jmp _GameStateTick_CaveAddress
            WriteBytes(GameProcess_ModuleBaseAddress + _GameStateTick_Patch_Offset, new byte[] { 0xE9, 0xD6, 0x9E, 0x17, 0x02, 0x90 });

            //Calls to the usb dll credits function will not return any good values (pointer not existing, no data to write/read)
            //There are values that can be used in the main .exe :
            //For Credits we will force the game to read our own value  
            WriteBytes(GameProcess_ModuleBaseAddress + _CreditsRead_Patch_Offset, new byte[] { 0x48, 0x8B, 0xF9, 0x48, 0x8D, 0x05, 0x93, 0x96, 0x2D, 0x03, 0x48, 0xC1, 0xE7, 0x04, 0x48, 0x01, 0xF8, 0x8B, 0x00, 0xE9, 0x81, 0x00, 0x00, 0x00, 0x00 });
            //For Credits we will force the game to write to our own values
            WriteBytes(GameProcess_ModuleBaseAddress + _CreditsWrite_Patch_Offset, new byte[] { 0x48, 0x8D, 0x05, 0xD7, 0x97, 0x2D, 0x03, 0xC1, 0xE5, 0x04, 0x48, 0x01, 0xE8, 0x8B, 0x28, 0x29, 0xD5, 0x89, 0x28, 0xE9, 0x91, 0x00, 0x00, 0x00, 0x90 });

            //Force return OK to IsGunCalibrated call
            //mov al, 01
            WriteBytes(GameProcess_ModuleBaseAddress + _IsGunCalibrated_Patch_Offset, new byte[] { 0xB0, 0x01 });
          
            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        private void SetHack_UsbPluginDll()
        {
            //Force return true for BIND_InitDongle()
            WriteBytes(_UsbPluginsDll_BaseAddress + _InitDongle_Dll_Patch_Offset, new byte[] { 0x90, 0x90 });

            //Force return true for BIND_IsDongleVerify()
            WriteBytes(_UsbPluginsDll_BaseAddress + _IsDongleVerify_Dll_Patch_Offset, new byte[] { 0xB0, 0x01 });

            //Force return true to BIND_OpenESIODevice without opening COM port at all
            WriteBytes(_UsbPluginsDll_BaseAddress + _OpenESIODevice_Dll_Patch_Offset, new byte[] { 0xB8, 0x01, 0x00, 0x00, 0x00, 0xC3 });

            //Force return true for BIND_IsExistESIODevice()
            WriteBytes(_UsbPluginsDll_BaseAddress + _IsExistESIODevice_Dll_Patch_Offset, new byte[] { 0xB8, 0x01, 0x00, 0x00, 0x00, 0x90, 0x90 });

            //Force return true for BIND_GetESIoData()
            //That way the game will constantly call the ABPGameState::PArseIoData where we will inject our values
            WriteBytes(_UsbPluginsDll_BaseAddress + _GetESIoData_Dll_Patch_Offset, new byte[] { 0xB8, 0x01, 0x00, 0x00, 0x00, 0xC3 });
        }

        #endregion        

        #region Inputs Hack

        protected override void Apply_InputsMemoryHack()
        {
            Create_InputsDataBank();

            //+B85E4A --> NOP | 5 --> Nop mousebuttons and keyboard keys
            SetNops(GameProcess_ModuleBaseAddress, _Nop_KeyboardAndMouse);

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
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

        //To sync recoil pulse with fired bullets, we will retrieve the information when the game call the "PlayFireMV"
        //Player Index is in edx
        private void SetHack_Recoil()
        {
            Codecave64 CaveMemory = new Codecave64(_Process, GameProcess_ModuleBaseAddress);

            //push rax
            CaveMemory.Write_StrBytes("50");
            //movabs rax, _Recoil_CaveAddress
            CaveMemory.Write_StrBytes("48 B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Recoil_CaveAddress));
            //add rax, rdx
            CaveMemory.Write_StrBytes("48 01 D0");
            //mov byte ptr [rax], 1
            CaveMemory.Write_StrBytes("C6 00 01");
            //pop rax
            CaveMemory.Write_StrBytes("58");
            //OriginalCode:
            //push rbx
            CaveMemory.Write_StrBytes("53");
            //sub rsp,20
            CaveMemory.Write_StrBytes("48 83 EC 20");
            //movsxd  rbx,edx
            CaveMemory.Write_StrBytes("48 63 DA");
            //mov rdx,[rcx+00000238]
            CaveMemory.Write_StrBytes("48 8B 91 38 02 00 00");

            //inject it
            CaveMemory.InjectToOffset(_Recoil_InjectionStruct, "Recoil");
        }

        //Intercept a call to PlayTakeDamageSound() function to get dammage event
        //Player Index is in edx
        private void SetHack_Damage()
        {
            Codecave64 CaveMemory = new Codecave64(_Process, GameProcess_ModuleBaseAddress);

            //push rax
            CaveMemory.Write_StrBytes("50");
            //movabs rax, _Damage_CaveAddress
            CaveMemory.Write_StrBytes("48 B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Damage_CaveAddress));
            //add rax, rdx
            CaveMemory.Write_StrBytes("48 01 D0");
            //mov byte ptr [rax], 1
            CaveMemory.Write_StrBytes("C6 00 01");
            //pop rax
            CaveMemory.Write_StrBytes("58");
            //OriginalCode:
            //push rdi
            CaveMemory.Write_StrBytes("40 57");
            //push r14
            CaveMemory.Write_StrBytes("41 56");
            //push r15
            CaveMemory.Write_StrBytes("41 57");
            //sub rsp,00000080
            CaveMemory.Write_StrBytes("48 81 EC 80 00 00 00");
            //mov rdi,rcx
            CaveMemory.Write_StrBytes("48 8B F9");

            //Inject it
            CaveMemory.InjectToOffset(_Damage_InjectionStruct, "Damage");
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
            else if (e.ButtonName.Equals("TEST"))
            {
                Apply_OR_ByteMask(GameProcess_ModuleBaseAddress + _IoBoard_Payload_Offset + 2, 0x04);
            }
            else if (e.ButtonName.Equals("MENU UP"))
            {
                Apply_OR_ByteMask(GameProcess_ModuleBaseAddress + _IoBoard_Payload_Offset + 2, 0x04);
            }
            else if (e.ButtonName.Equals("MENU DOWN"))
            {
                Apply_OR_ByteMask(GameProcess_ModuleBaseAddress + _IoBoard_Payload_Offset + 2, 0x02);
            }
            else if (e.ButtonName.Equals("MENU SELECT"))
            {
                Apply_OR_ByteMask(GameProcess_ModuleBaseAddress + _IoBoard_Payload_Offset + 2, 0x01);
            }
        }

        protected override void GameController_OnButtonUp(object sender, VirtualGameControler.ButtonDownEventArgs e)
        {
            if (e.ButtonName.Equals("TEST"))
            {
                Apply_AND_ByteMask(GameProcess_ModuleBaseAddress + _IoBoard_Payload_Offset + 2, 0xFB);
            }
            else if (e.ButtonName.Equals("MENU UP"))
            {
                Apply_AND_ByteMask(GameProcess_ModuleBaseAddress + _IoBoard_Payload_Offset + 2, 0xFB);
            }
            else if (e.ButtonName.Equals("MENU DOWN"))
            {
                Apply_AND_ByteMask(GameProcess_ModuleBaseAddress + _IoBoard_Payload_Offset + 2, 0xFD);
            }
            else if (e.ButtonName.Equals("MENU SELECT"))
            {
                Apply_AND_ByteMask(GameProcess_ModuleBaseAddress + _IoBoard_Payload_Offset + 2, 0xFE);
            }
        }

        protected override void UpdateAxis(PlayerID Id, int X, int Y)
        {
            if (Id != PlayerID.None)
            {
                byte[] bufferX = BitConverter.GetBytes(X);
                byte[] bufferY = BitConverter.GetBytes(Y);

                WriteBytes(GameProcess_ModuleBaseAddress + _P1_X_Offset + ((uint)Id * 0x10), bufferX);
                WriteBytes(GameProcess_ModuleBaseAddress + _P1_Y_Offset + ((uint)Id * 0x10), bufferY);
            }
        }

        protected override void PlayerController_OnButtonDown(object sender, VirtualGameControler.ButtonDownEventArgs e)
        {
            if (e.Id == PlayerID.Player1)
            {
                UInt32 PlayerIndex = (UInt32)e.Id;

                if (e.ButtonName.Equals("TRIGGER"))
                {
                    WriteByte(GameProcess_ModuleBaseAddress + _P1_Trigger_Offset, 1);
                }
                else if (e.ButtonName.Equals("START"))
                {
                    Apply_OR_ByteMask(GameProcess_ModuleBaseAddress + _IoBoard_Payload_Offset + 2, 0x20);
                }
                else if (e.ButtonName.Equals("COIN"))
                {
                    UInt32 c = ReadUInt32(GameProcess_ModuleBaseAddress + _P1_Credits_Offset);
                    c++;
                    WriteBytes(GameProcess_ModuleBaseAddress + _P1_Credits_Offset, BitConverter.GetBytes(c));
                }
            }
            else if (e.Id == PlayerID.Player2)
            {
                if (e.ButtonName.Equals("TRIGGER"))
                {
                    WriteByte(GameProcess_ModuleBaseAddress + _P2_Trigger_Offset, 1);
                }
                else if (e.ButtonName.Equals("START"))
                {
                    Apply_OR_ByteMask(GameProcess_ModuleBaseAddress + _IoBoard_Payload_Offset + 2, 0x10);
                }
                else if (e.ButtonName.Equals("COIN"))
                {
                    UInt32 c = ReadUInt32(GameProcess_ModuleBaseAddress + _P2_Credits_Offset);
                    c++;
                    WriteBytes(GameProcess_ModuleBaseAddress + _P2_Credits_Offset, BitConverter.GetBytes(c));
                }
            }
        }
        protected override void PlayerController_OnButtonUp(object sender, VirtualGameControler.ButtonDownEventArgs e)
        {
            if (e.Id == PlayerID.Player1)
            {
                UInt32 PlayerIndex = (UInt32)e.Id;

                if (e.ButtonName.Equals("TRIGGER"))
                {
                    WriteByte(GameProcess_ModuleBaseAddress + _P1_Trigger_Offset, 0);
                }
                else if (e.ButtonName.Equals("START"))
                {
                    Apply_AND_ByteMask(GameProcess_ModuleBaseAddress + _IoBoard_Payload_Offset + 2, 0xDF);
                }
            }
            else if (e.Id == PlayerID.Player2)
            {
                if (e.ButtonName.Equals("TRIGGER"))
                {
                    WriteByte(GameProcess_ModuleBaseAddress + _P2_Trigger_Offset, 0);
                }
                else if (e.ButtonName.Equals("START"))
                {
                    Apply_AND_ByteMask(GameProcess_ModuleBaseAddress + _IoBoard_Payload_Offset + 2, 0xEF);
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
                new GameOutput(OutputId.P1_GunMotor),
                new GameOutput(OutputId.P2_GunMotor),
                new GameOutput(OutputId.P1_LmpStart),
                new GameOutput(OutputId.P2_LmpStart),
                new GameOutput(OutputId.DoorA),
                new GameOutput(OutputId.DoorB),
                new GameOutput(OutputId.ElevatorLedsStatus),
                new AsyncGameOutput(OutputId.P1_CtmRecoil, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_ON_DELAY), ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_OFF_DELAY), 0),
                new AsyncGameOutput(OutputId.P2_CtmRecoil, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_ON_DELAY), ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_OFF_DELAY), 0),
                /*_Outputs.Add(new GameOutput(OutputId.P1_Life));
                _Outputs.Add(new GameOutput(OutputId.P2_Life));*/
                new AsyncGameOutput(OutputId.P1_Damaged, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_DAMAGED_DELAY), 100, 0),
                new AsyncGameOutput(OutputId.P2_Damaged, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_DAMAGED_DELAY), 100, 0),
                new GameOutput(OutputId.P1_Credits),
                new GameOutput(OutputId.P2_Credits)
            };
        }

        /// <summary>
        /// Update all Outputs values before sending them to MameHooker
        /// </summary>
        public override void UpdateOutputValues()
        {
            //Gun motor data goes from 0 to 10. Power of rumble ?
            SetOutputValue(OutputId.P1_GunMotor, ReadByte(_UsbPluginsDll_BaseAddress + _DllOutputsBase_Offset + 0x45C));
            SetOutputValue(OutputId.P2_GunMotor, ReadByte(_UsbPluginsDll_BaseAddress + _DllOutputsBase_Offset + 0x460));
            //Start Led have 3 original values : 
            //0 = ON
            //1 = BLINK
            //2 = OFF
            byte b = ReadByte((IntPtr)((UInt64)_UsbPluginsDll_BaseAddress + _DllOutputsBase_Offset + 0x450));
            SetOutputValue(OutputId.P1_LmpStart, 2 - b);
            b = ReadByte((IntPtr)((UInt64)_UsbPluginsDll_BaseAddress + _DllOutputsBase_Offset + 0x454));
            SetOutputValue(OutputId.P2_LmpStart, 2 - b);
            //Door can be 0x09 or 0x11...don't know how it works
            SetOutputValue(OutputId.DoorB, ReadByte(_UsbPluginsDll_BaseAddress + _DllOutputsBase_Offset + 0x448));
            SetOutputValue(OutputId.DoorA, ReadByte(_UsbPluginsDll_BaseAddress + _DllOutputsBase_Offset + 0x44C));
            //Game LED status can have a lot of states :
            // 0 = Standby 
            // 1 = OFF 
            // 2 = NormalFight 
            // 3 = Intense Fight
            // 4 = climax Fight 
            // 5 = Damage 
            // 6 = Continue 
            // 7 = Level Passed 
            // 8 = Elevator CLosed
            //I suppose this is handling the whole set of lights on the cabinet
            SetOutputValue(OutputId.ElevatorLedsStatus, ReadByte(_UsbPluginsDll_BaseAddress + _DllOutputsBase_Offset + 0x458));

            for (uint i = 0; i < _GameConfig.PlayerCount; i++)
            {
                if (ReadByte(_Recoil_CaveAddress + (UInt64)i) != 0)
                {
                    WriteByte(_Recoil_CaveAddress + (UInt64)i, 0x00);
                    SetOutputValue(OutputId.P1_CtmRecoil + i, 1);
                }

                if (ReadByte(_Damage_CaveAddress + (UInt64)i) != 0)
                {
                    WriteByte(_Damage_CaveAddress + (UInt64)i, 0x00);
                    SetOutputValue(OutputId.P1_Damaged + i, 1);
                }
            }

            //Credits
            SetOutputValue(OutputId.P1_Credits, ReadInt32(GameProcess_ModuleBaseAddress + _P1_Credits_Offset));
            SetOutputValue(OutputId.P2_Credits, ReadInt32(GameProcess_ModuleBaseAddress + _P2_Credits_Offset));
        }

        #endregion
    }
}
