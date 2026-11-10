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
    public class Game_ArcadepcRaccoonRampage : BaseGame64
    {
        protected override string GAMEDATA_FOLDER { get { return @"Arcadepc\racramp"; } }
        protected override bool PATCH_PEB_DEBUG_FLAG { get { return false; } }

        private const string USBPLUGINSDLL_NAME = "UsbPluginsDll.dll";
        private UInt64 _UsbPluginsDll_BaseAddress;

        #region LOADER MEMORY ADDRESSES      
        private UInt64 _InitDongle_Dll_Patch_Offset = 0x0001592B;
        private UInt64 _IsDongleVerify_Dll_Patch_Offset = 0x00015BE9;
        private UInt64 _InitUnisIo_Dll_Patch_Offset = 0x00010070;
        private UInt64 _IsIoWorking_Dll_Patch_Offset = 0x00015D27;
        private UInt64 _DWInit_Dll_Patch_Offset = 0x0000D880;
        #endregion

        #region INPUTS MEMORY ADDRESSES    
        private NopStruct64 _Nop_DirectInput = new NopStruct64(0x0E38CA7, 5);
        private InjectionStruct64 _Axis_InjectionStruct = new InjectionStruct64(0x009CFF88, 14);
        private InjectionStruct64 _Buttons_InjectionStruct = new InjectionStruct64(0x009B10C8, 15);
        private InjectionStruct64 _GetCoins_InjectionStruct = new InjectionStruct64(0x009DC2AF, 150);
        private InjectionStruct64 _DecCoins_InjectionStruct = new InjectionStruct64(0x009DC17E, 166);
        
        //Custom data
        private UInt64 _P1_X_CaveAddress = 0;
        private UInt64 _P1_Y_CaveAddress = 0;
        private UInt64 _SystemButtons_CaveAddress = 0;
        private UInt64 _StartButtonsAndWater_CaveAddress = 0;
        private UInt64 _P1_Coins_CaveAddress = 0;
        private UInt64 _P2_Coins_CaveAddress = 0;
        private UInt64 _P3_Coins_CaveAddress = 0;
        private UInt64 _P4_Coins_CaveAddress = 0;
        #endregion

        #region OUTPUTS MEMORY ADDRESSES    
        private UInt64 _Outputs_BaseOffset = 0x2E740;
        public enum OutputIndex
        {
            P1_NewGunLightState = 0x19A,        //
            P2_NewGunLightState,                //Changed at the same time than GunLightState    
            P3_NewGunLightState,                //So....Useless ?
            P4_NewGunLightState,                //            
            P1_GunLightState = 0x5AC,
            P2_GunLightState = 0x5B4,
            P3_GunLightState = 0x5BC,
            P4_GunLightState = 0x5C4,
            LackTicketLight_12P = 0x5E8,
            LackTicketLight_34P = 0x5EC,
            DisinfectionLight = 0x5F0,
            P1_InnerWater = 0x5F4,
            P2_InnerWater,
            P3_InnerWater,
            P4_InnerWater,
            P1_OuterWater,
            P2_OuterWater,
            P3_OuterWater,
            P4_OuterWater,
            P1_GunShock = 0x62C,
            P2_GunShock = 0x630,
            P3_GunShock = 0x634,
            P4_GunShock = 0x638,
            Marquee_Character1 = 0x688,
            Marquee_Character2 = 0x68C
        }
        private UInt64 _P1_Damage_CaveAddress = 0;
        private UInt64 _P1_InnerWater_CaveAddress = 0;
        private UInt64 _P1_OuterWater_CaveAddress = 0;


        private InjectionStruct64 _PlayerDamage_InjectionStruct = new InjectionStruct64(0xA3C480, 15);      
        private UInt64 _IsGunCalibrate_Offset = 0x9B7290;
        #endregion

        private Dictionary<string, string> _KnownMd5_UsbPluginsDll;

        public Game_ArcadepcRaccoonRampage(GameConfig Config) : base(Config)
        {
            _KnownMd5Prints.Add("Raccon Rampage v1.4", "0a23d3eff07e265408ef818349f3e08e");
            _KnownMd5_UsbPluginsDll = new Dictionary<string, string>
            {
                { "Raccon Rampage v1.4 clean dll", "d3d3a8b3c7a8f7254d3c7641bb91bd03" },
                { "Racoon Rampage v1.4 patched dll by Argonlefou", "8e3a7b0e5c1065c5669c3f169e5a5c5f" }
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
                if (string.Equals(Path.GetFileName(e.DllPath), USBPLUGINSDLL_NAME, StringComparison.OrdinalIgnoreCase))
                {
                    Logger.WriteLog("==> Wanted DLL found !");
                    //HANDLE should always be in 32Bits range (https://learn.microsoft.com/fr-fr/windows/win32/winprog64/interprocess-communication?redirectedfrom=MSDN)
                    Logger.WriteLog("  + hModule: 0x" + e.LoadDllInfo.hFile.ToString("X8"));
                    Logger.WriteLog("  + fUnicode: " + e.LoadDllInfo.fUnicode.ToString());

                    _UsbPluginsDll_BaseAddress = (UInt64)e.LoadDllInfo.lpBaseOfDll;

                    string UsbPluginPath = _GameConfig.TargetPath.Replace(Path.GetFileName(_GameConfig.TargetFileName), @"..\..\Plugins\" + USBPLUGINSDLL_NAME);
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
            //GameHooked event will be raised in DLL_LOADED event, once the usb DLL has been patched
            base.Apply_MemoryHacks();
        }

        #region Loader Hack
        /// <summary>
        /// Patch the Usb IO Dll when it's loaded by the game
        /// </summary>
        private void SetHack_UsbPluginDll()
        {
            //Patching Dongle Check...
            //Force return true for BIND_InitDongle()
            WriteBytes(_UsbPluginsDll_BaseAddress + _InitDongle_Dll_Patch_Offset, new byte[] { 0x90, 0x90 });
            //Force return true for BIND_IsDongleVerify()
            WriteBytes(_UsbPluginsDll_BaseAddress + _IsDongleVerify_Dll_Patch_Offset, new byte[] { 0xB0, 0x01 });

            //IO Board Check :
            //Force good  BIND_InitUnisIo()
            WriteBytes(_UsbPluginsDll_BaseAddress + _InitUnisIo_Dll_Patch_Offset, new byte[] { 0xEB, 0x14 });
            //Force return true for BIND_IsIoWorking()
            WriteBytes(_UsbPluginsDll_BaseAddress + _IsIoWorking_Dll_Patch_Offset, new byte[] { 0xEB, 0x1D });

            //Patching CardReader...
            //Force return -1 for BIND_DWInit() and Skipping 255 COM port loop opening to try to find a Card Reader
            WriteBytes(_UsbPluginsDll_BaseAddress + _DWInit_Dll_Patch_Offset, new byte[] { 0xE9, 0x63, 0x02, 0x00, 0x00 });

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }
        #endregion

        #region Inputs Hack
        protected override void Apply_InputsMemoryHack()
        {     
            //+B85E4A --> NOP | 5 --> Nop mousebuttons and keyboard keys
            SetNops(GameProcess_ModuleBaseAddress, _Nop_DirectInput);

            //Force return OK at the end of IsGunCalibrate() function
            WriteBytes(GameProcess_ModuleBaseAddress + _IsGunCalibrate_Offset, new byte[] { 0xB0, 0x01 });   //mov al, 01

            Create_InputsDataBank();
            _P1_X_CaveAddress = InputsDatabank_Address;         //
            _P1_Y_CaveAddress = InputsDatabank_Address + 0x04;  // P2, P3 and P4 will be accessed from P1 address, with added offset                
            _SystemButtons_CaveAddress = InputsDatabank_Address + 0x20;
            _StartButtonsAndWater_CaveAddress = InputsDatabank_Address + 0x21;
            _P1_Coins_CaveAddress = InputsDatabank_Address + 0x30;
            _P2_Coins_CaveAddress = InputsDatabank_Address + 0x31;
            _P3_Coins_CaveAddress = InputsDatabank_Address + 0x32;
            _P4_Coins_CaveAddress = InputsDatabank_Address + 0x33;

            //Set Water Level to "normal"
            Apply_OR_ByteMask(_StartButtonsAndWater_CaveAddress, 0x04);

            SetHack_Axis();
            SetHack_StartButtons();
            SetHack_GetCoinsNum();
            SetHack_DecCoins();

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        /// <summary>
        /// Replace UBPGameInstance::SetMouseLocation() floatX and floatY parameter with already screen-accurate data for each player
        /// </summary>
        private void SetHack_Axis()
        {
            Codecave64 CaveMemory = new Codecave64(_Process, GameProcess_ModuleBaseAddress);

            //push rax
            CaveMemory.Write_StrBytes("50");
            //push rdx
            CaveMemory.Write_StrBytes("52");
            //movabs rax, [_P1_X_Offset]
            CaveMemory.Write_StrBytes("48 B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_X_CaveAddress));
            //shl rdx, 3
            CaveMemory.Write_StrBytes("48 C1 E2 03");
            //add rax, rdx
            CaveMemory.Write_StrBytes("48 01 D0");
            //movss xmm2,[rax]
            CaveMemory.Write_StrBytes("F3 0F 10 10");
            //movss xmm3,[rax+04]
            CaveMemory.Write_StrBytes("F3 0F 10 58 04");
            //pop rdx
            CaveMemory.Write_StrBytes("5A");
            //pop rax
            CaveMemory.Write_StrBytes("58");
            //mov eax,[rcx+00000624]
            CaveMemory.Write_StrBytes("8B 81 24 06 00 00");
            //mov r14,rcx
            CaveMemory.Write_StrBytes("4C 8B F1");
            //movaps [rsp+30],xmm7
            CaveMemory.Write_StrBytes("0F 29 7C 24 30");

            //Inject it
            CaveMemory.InjectToOffset(_Axis_InjectionStruct, "Axis");
        }

        //Replace GetIoData() call by reading custom buttons bytes
        private void SetHack_StartButtons()
        {
            Codecave64 CaveMemory = new Codecave64(_Process, GameProcess_ModuleBaseAddress);

            //lea rcx,[r14+000002C0]
            CaveMemory.Write_StrBytes("49 8D 8E C0 02 00 00");

            //movabs rax, [_SystemButtons_CaveAddress]
            CaveMemory.Write_StrBytes("48 B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_SystemButtons_CaveAddress));
            //mov rax, [rax]
            CaveMemory.Write_StrBytes("48 8B 00");
            //mov [rcx+02],al
            CaveMemory.Write_StrBytes("88 41 02");

            //movabs rax, [_StartButtonsAndWater_CaveAddress]
            CaveMemory.Write_StrBytes("48 B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_StartButtonsAndWater_CaveAddress));
            //mov rax, [rax]
            CaveMemory.Write_StrBytes("48 8B 00");
            //mov [rcx+05],al
            CaveMemory.Write_StrBytes("88 41 05");

            //xor rax,rax
            CaveMemory.Write_StrBytes("48 31 C0");
            //mov al,01
            CaveMemory.Write_StrBytes("B0 01");
            //movzx esi,al
            CaveMemory.Write_StrBytes("0F B6 F0");
            //test rbx,rbx
            CaveMemory.Write_StrBytes("48 85 DB");
            //jmp back
            CaveMemory.Write_jmp(GameProcess_ModuleBaseAddress + 0x9B10D7);

            //Inject it
            CaveMemory.InjectToOffset(_Buttons_InjectionStruct, "Buttons");
        }

        /// <summary>
        /// Force the game to read our own coin counters
        /// </summary>
        private void SetHack_GetCoinsNum()
        {
            Codecave64 CaveMemory = new Codecave64(_Process, GameProcess_ModuleBaseAddress);

            //push rcx
            CaveMemory.Write_StrBytes("51");
            //movabs rax, [_P1_Coins_CaveAddress]
            CaveMemory.Write_StrBytes("48 B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_Coins_CaveAddress));
            //add rcx,rax
            CaveMemory.Write_StrBytes("48 01 C1");
            //xor rax,rax
            CaveMemory.Write_StrBytes("48 31 C0");
            //mov al,[rcx]
            CaveMemory.Write_StrBytes("8A 01");
            //pop rcx
            CaveMemory.Write_StrBytes("59");

            //Inject it
            CaveMemory.InjectToOffset(_GetCoins_InjectionStruct, "GetCoinsNum");
        }

        /// <summary>
        /// Force the game to decrement our own coin counters
        /// </summary>
        private void SetHack_DecCoins()
        {
            Codecave64 CaveMemory = new Codecave64(_Process, GameProcess_ModuleBaseAddress);

            //push rax
            CaveMemory.Write_StrBytes("50");
            //push rbx
            CaveMemory.Write_StrBytes("53");
            //movabs rax, [_P1_Coins_CaveAddress]
            CaveMemory.Write_StrBytes("48 B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_Coins_CaveAddress));
            //add rax,rbp
            CaveMemory.Write_StrBytes("48 01 E8");
            //mov rbx,rax
            CaveMemory.Write_StrBytes("48 8B D8");
            //xor rax,rax
            CaveMemory.Write_StrBytes("48 31 C0");
            //mov al,[rbx]
            CaveMemory.Write_StrBytes("8A 03");
            //sub rax,rdx
            CaveMemory.Write_StrBytes("48 29 D0");
            //mov [rbx],al
            CaveMemory.Write_StrBytes("88 03");
            //pop rbx
            CaveMemory.Write_StrBytes("5B");
            //pop rax
            CaveMemory.Write_StrBytes("58");

            //Inject it
            CaveMemory.InjectToOffset(_DecCoins_InjectionStruct, "DecCoins");
        }        

        #endregion

        #region Outputs Hack

        protected override void Apply_OutputsMemoryHack()
        {
            Create_OutputsDataBank();
            _P1_InnerWater_CaveAddress = OutputsDatabank_Address;
            _P1_OuterWater_CaveAddress = OutputsDatabank_Address + 0x01;
            _P1_Damage_CaveAddress = OutputsDatabank_Address + 0x08;

            SetHack_GetDamage();

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        private void SetHack_GetDamage()
        {
            Codecave64 CaveMemory = new Codecave64(_Process, GameProcess_ModuleBaseAddress);

            //push rbx
            CaveMemory.Write_StrBytes("53");

            //movabs rax, [_P1_Damage_CaveAddress]
            CaveMemory.Write_StrBytes("48 B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_Damage_CaveAddress));
            //cmp rbx, 4
            CaveMemory.Write_StrBytes("48 83 FB 04");
            //je All Damaged
            CaveMemory.Write_StrBytes("74 11");

            //Single damage:
            //add rax, rbx
            CaveMemory.Write_StrBytes("48 01 D8");
            //mov rbx, 1
            CaveMemory.Write_StrBytes("48 BB 01 00 00 00 00 00 00 00");
            //mov [rax], bl
            CaveMemory.Write_StrBytes("88 18");
            //jmp exit
            CaveMemory.Write_StrBytes("EB 0C");

            //All Damaged :
            //mov rbx, 01 01 01 01
            CaveMemory.Write_StrBytes("48 BB 01 01 01 01 00 00 00 00");
            //mov [rax], bx
            CaveMemory.Write_StrBytes("89 18");

            //Exit:
            //sub rsp,000000C0
            CaveMemory.Write_StrBytes("48 81 EC C0 00 00 00");
            //mov rax,[rcx]
            CaveMemory.Write_StrBytes("48 8B 01");
            //mov rbx,rcx
            CaveMemory.Write_StrBytes("48 8B D9");

            //Inject it
            CaveMemory.InjectToOffset(_PlayerDamage_InjectionStruct, "Damage");
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
                Apply_OR_ByteMask(_SystemButtons_CaveAddress, 0x04);
            }
            else if (e.ButtonName.Equals("MENU UP"))
            {
                Apply_OR_ByteMask(_SystemButtons_CaveAddress, 0x04);
            }
            else if (e.ButtonName.Equals("MENU DOWN"))
            {
                Apply_OR_ByteMask(_SystemButtons_CaveAddress, 0x02);
            }
            else if (e.ButtonName.Equals("MENU ENTER"))
            {
                Apply_OR_ByteMask(_SystemButtons_CaveAddress, 0x01);
            }
        }

        protected override void GameController_OnButtonUp(object sender, VirtualGameControler.ButtonDownEventArgs e)
        {
            if (e.ButtonName.Equals("TEST"))
            {
                Apply_AND_ByteMask(_SystemButtons_CaveAddress, 0xFB);
            }
            else if (e.ButtonName.Equals("MENU UP"))
            {
                Apply_AND_ByteMask(_SystemButtons_CaveAddress, 0xFB);
            }
            else if (e.ButtonName.Equals("MENU DOWN"))
            {
                Apply_AND_ByteMask(_SystemButtons_CaveAddress, 0xFD);
            }
            else if (e.ButtonName.Equals("MENU ENTER"))
            {
                Apply_AND_ByteMask(_SystemButtons_CaveAddress, 0xFE);
            }
        }

        protected override void UpdateAxis(PlayerID Id, int X, int Y)
        {
            if (Id != PlayerID.None)
            {
                //Writing Axis
                WriteBytes(_P1_X_CaveAddress + 8 * (UInt64)Id, BitConverter.GetBytes((float)X));
                WriteBytes(_P1_Y_CaveAddress + 8 * (UInt64)Id, BitConverter.GetBytes((float)Y));
            }
        }

        protected override void PlayerController_OnButtonDown(object sender, VirtualGameControler.ButtonDownEventArgs e)
        {
            if (e.Id != PlayerID.None)
            {
                int PlayerIndex = (int)e.Id;

                if (e.ButtonName.Equals("TRIGGER"))
                {
                    int bData = 0x10 << (3 - PlayerIndex);
                    Apply_OR_ByteMask(_StartButtonsAndWater_CaveAddress, (byte)bData);
                }
                else if (e.ButtonName.Equals("COIN"))
                {
                    UInt64 CoinAddress = _P1_Coins_CaveAddress + (UInt64)PlayerIndex;
                    byte c = ReadByte(CoinAddress);
                    if (c < 99)
                    {
                        c++;
                        WriteByte(CoinAddress, c);
                    }
                }
            }
        }
        protected override void PlayerController_OnButtonUp(object sender, VirtualGameControler.ButtonDownEventArgs e)
        {
            if (e.Id != PlayerID.None)
            {
                int PlayerIndex = (int)e.Id;

                if (e.ButtonName.Equals("TRIGGER"))
                {
                    int bData = 0x10 << (3 - PlayerIndex);
                    Apply_AND_ByteMask(_StartButtonsAndWater_CaveAddress, (byte)~bData);
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
                new GameOutput(OutputId.P1_LmpGun),
                new GameOutput(OutputId.P2_LmpGun),
                new GameOutput(OutputId.P3_LmpGun),
                new GameOutput(OutputId.P4_LmpGun),
                new GameOutput(OutputId.LmpLeft),
                new GameOutput(OutputId.LmpRight),
                new GameOutput(OutputId.Lmp_Disinfection),
                new GameOutput(OutputId.Lmp_P1_P2_OutOfTickets),
                new GameOutput(OutputId.Lmp_P3_P4_OutOfTickets),
                new GameOutput(OutputId.Lmp_Marquee_1),
                new GameOutput(OutputId.Lmp_Marquee_2),
                new GameOutput(OutputId.P1_InnerWater),
                new GameOutput(OutputId.P2_InnerWater),
                new GameOutput(OutputId.P3_InnerWater),
                new GameOutput(OutputId.P4_InnerWater),
                new GameOutput(OutputId.P1_OuterWater),
                new GameOutput(OutputId.P2_OuterWater),
                new GameOutput(OutputId.P3_OuterWater),
                new GameOutput(OutputId.P4_OuterWater),
                new GameOutput(OutputId.P1_GunMotor),
                new GameOutput(OutputId.P2_GunMotor),
                new GameOutput(OutputId.P3_GunMotor),
                new GameOutput(OutputId.P4_GunMotor),
                new AsyncGameOutput(OutputId.P1_Damaged, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_DAMAGED_DELAY), 100, 0),
                new AsyncGameOutput(OutputId.P2_Damaged, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_DAMAGED_DELAY), 100, 0),
                new AsyncGameOutput(OutputId.P3_Damaged, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_DAMAGED_DELAY), 100, 0),
                new AsyncGameOutput(OutputId.P4_Damaged, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_DAMAGED_DELAY), 100, 0),
                new GameOutput(OutputId.P1_Credits),
                new GameOutput(OutputId.P2_Credits),
                new GameOutput(OutputId.P3_Credits),
                new GameOutput(OutputId.P4_Credits)
            };
        }

        /// <summary>
        /// Update all Outputs values before sending them to MameHooker
        /// </summary>
        public override void UpdateOutputValues()
        {
            //Gun LED status can have different states (BIND_SetGunLight_State())
            // 0 = OFF 
            // 1 = Continuous 
            // 2 = Flash 
            SetOutputValue(OutputId.P1_LmpGun, ReadByte(_UsbPluginsDll_BaseAddress + _Outputs_BaseOffset + (uint)OutputIndex.P1_GunLightState));
            SetOutputValue(OutputId.P2_LmpGun, ReadByte(_UsbPluginsDll_BaseAddress + _Outputs_BaseOffset + (uint)OutputIndex.P2_GunLightState));
            SetOutputValue(OutputId.P3_LmpGun, ReadByte(_UsbPluginsDll_BaseAddress + _Outputs_BaseOffset + (uint)OutputIndex.P3_GunLightState));
            SetOutputValue(OutputId.P4_LmpGun, ReadByte(_UsbPluginsDll_BaseAddress + _Outputs_BaseOffset + (uint)OutputIndex.P4_GunLightState));

            //Side LED status can have different states (BIND_SetLRSideLight_State())
            // 0 = OFF 
            // 1 = Continuous 
            // 2 = Flash 

            //-- TODO : Only one value as the state is changed with 2 calls : one to set the desired LED and one to set the State. Impossible to read everytime both sides status

            //Disinfection LED
            SetOutputValue(OutputId.Lmp_Disinfection, ReadByte(_UsbPluginsDll_BaseAddress + _Outputs_BaseOffset + (uint)OutputIndex.DisinfectionLight));

            //Tickets LED status can have different states (BIND_SetLackTicketLight())
            // 0 = OFF 
            // 1 = Continuous 
            // 2 = Flash 
            SetOutputValue(OutputId.Lmp_P1_P2_OutOfTickets, ReadByte(_UsbPluginsDll_BaseAddress + _Outputs_BaseOffset + (uint)OutputIndex.LackTicketLight_12P));
            SetOutputValue(OutputId.Lmp_P3_P4_OutOfTickets, ReadByte(_UsbPluginsDll_BaseAddress + _Outputs_BaseOffset + (uint)OutputIndex.LackTicketLight_34P));

            //MarqueeLEDs (BIND_SetICO_1() and BIND_SetICO_2())
            SetOutputValue(OutputId.Lmp_Marquee_1, ReadByte(_UsbPluginsDll_BaseAddress + _Outputs_BaseOffset + (uint)OutputIndex.Marquee_Character1));
            SetOutputValue(OutputId.Lmp_Marquee_2, ReadByte(_UsbPluginsDll_BaseAddress + _Outputs_BaseOffset + (uint)OutputIndex.Marquee_Character2));

            //Water mechanics
            SetOutputValue(OutputId.P1_InnerWater, ReadByte(_UsbPluginsDll_BaseAddress + _Outputs_BaseOffset + (uint)OutputIndex.P1_InnerWater));
            SetOutputValue(OutputId.P2_InnerWater, ReadByte(_UsbPluginsDll_BaseAddress + _Outputs_BaseOffset + (uint)OutputIndex.P2_InnerWater));
            SetOutputValue(OutputId.P3_InnerWater, ReadByte(_UsbPluginsDll_BaseAddress + _Outputs_BaseOffset + (uint)OutputIndex.P3_InnerWater));
            SetOutputValue(OutputId.P4_InnerWater, ReadByte(_UsbPluginsDll_BaseAddress + _Outputs_BaseOffset + (uint)OutputIndex.P4_InnerWater));
            SetOutputValue(OutputId.P1_OuterWater, ReadByte(_UsbPluginsDll_BaseAddress + _Outputs_BaseOffset + (uint)OutputIndex.P1_OuterWater));
            SetOutputValue(OutputId.P2_OuterWater, ReadByte(_UsbPluginsDll_BaseAddress + _Outputs_BaseOffset + (uint)OutputIndex.P2_OuterWater));
            SetOutputValue(OutputId.P3_OuterWater, ReadByte(_UsbPluginsDll_BaseAddress + _Outputs_BaseOffset + (uint)OutputIndex.P3_OuterWater));
            SetOutputValue(OutputId.P4_OuterWater, ReadByte(_UsbPluginsDll_BaseAddress + _Outputs_BaseOffset + (uint)OutputIndex.P4_OuterWater));

            //Gun motor data goes from 0 to 2. Power of rumble ?
            SetOutputValue(OutputId.P1_GunMotor, ReadByte(_UsbPluginsDll_BaseAddress + _Outputs_BaseOffset + (uint)OutputIndex.P1_GunShock));
            SetOutputValue(OutputId.P2_GunMotor, ReadByte(_UsbPluginsDll_BaseAddress + _Outputs_BaseOffset + (uint)OutputIndex.P2_GunShock));
            SetOutputValue(OutputId.P3_GunMotor, ReadByte(_UsbPluginsDll_BaseAddress + _Outputs_BaseOffset + (uint)OutputIndex.P3_GunShock));
            SetOutputValue(OutputId.P4_GunMotor, ReadByte(_UsbPluginsDll_BaseAddress + _Outputs_BaseOffset + (uint)OutputIndex.P4_GunShock));

            ////Custom Damage will be generated by a call to one of the game Gun function
            int P1_Damage = ReadByte(_P1_Damage_CaveAddress);
            if (P1_Damage != 0)
            {
                WriteByte(_P1_Damage_CaveAddress, 0x00);
                SetOutputValue(OutputId.P1_Damaged, 1);
            }
            int P2_Damage = ReadByte(_P1_Damage_CaveAddress + 1);
            if (P2_Damage != 0)
            {
                WriteByte(_P1_Damage_CaveAddress + 1, 0x00);
                SetOutputValue(OutputId.P2_Damaged, 1);
            }
            int P3_Damage = ReadByte(_P1_Damage_CaveAddress + 2);
            if (P3_Damage != 0)
            {
                WriteByte(_P1_Damage_CaveAddress + 2, 0x00);
                SetOutputValue(OutputId.P3_Damaged, 1);
            }
            int P4_Damage = ReadByte(_P1_Damage_CaveAddress + 3);
            if (P4_Damage != 0)
            {
                WriteByte(_P1_Damage_CaveAddress + 3, 0x00);
                SetOutputValue(OutputId.P4_Damaged, 1);
            }

            //Credits
            SetOutputValue(OutputId.P1_Credits, ReadByte(_P1_Coins_CaveAddress));
            SetOutputValue(OutputId.P2_Credits, ReadByte(_P2_Coins_CaveAddress));
            SetOutputValue(OutputId.P3_Credits, ReadByte(_P3_Coins_CaveAddress));
            SetOutputValue(OutputId.P4_Credits, ReadByte(_P4_Coins_CaveAddress));
        }

        #endregion
    }
}
