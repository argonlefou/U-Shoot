using System;
using System.Collections.Generic;
using System.IO;
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
    public class Game_KonamiCastlevania : BaseGame32
    {
        protected override bool PATCH_PEB_DEBUG_FLAG { get { return true; } }

        private const string HCVACLIBDLL_NAME = "hcvACLib.dll";
        private UInt32 _HcvACLibDll_BaseAddress;        

        #region LOADER MEMORY ADDRESSES                
        private InjectionStruct32 _NoCursor_InjectionStruct = new InjectionStruct32(0x002490C6, 6);
        private UInt32 _Bookkeeping_Patch1_Offset = 0x00038C34;
        private UInt32 _Bookkeeping_Patch2_Offset = 0x00038C64;
        private UInt32 _Bookkeeping_Patch3_Offset = 0x00038C94;
        #endregion

        #region INPUTS MEMORY ADDRESSES    
        private UInt32 _IoEmulation_Patch_Offset = 0x000057E3;
        private UInt32 _ViewportWidth_Offset = 0x0032508C;
        private UInt32 _ViewportHeight_Offset = 0x00325090;
        private UInt32 _CheckIOEmulated_Offset = 0x00083978;
        private UInt32 _CoinInSIgnal_Offset = 0x0032811C;
        private UInt32 _HardwareID_Patch_Offset = 0x0006A0A5;
        private UInt32 _SoftwareID_Patch_Offset = 0x00006A04F;
        private UInt32 _P1_X_Offset = 0x003281A0;
        private UInt32 _P1_Y_Offset = 0x003281A4;
        //private UInt32 _Buttons_Offset = 0x00328138; //Unused, may be used for Emulated IO
        private InjectionStruct32 _Buttons_InjectionStruct = new InjectionStruct32(0x00080E84, 5); 
        //by default, putting a credit does not produce sound, compared to SERVICE key (but credit does not go into BOOKEEPING log)
        //By doing this we can force the game to play credit sound with CREDIT KEY
        private NopStruct32 _Nop_CreditsSound = new NopStruct32(0x00083065, 4);
        private UInt32 _CreditsSoundMod_Offset = 0x00083061;

        //Custom Data
        private UInt32 _Buttons_CaveAddress = 0;
        private UInt32 _HardwareID_CaveAddress;
        private UInt32 _SoftwareID_CaveAddress;
        #endregion

        #region OUTPUTS MEMORY ADDRESSES
        private UInt32 _Outputs_Offset = 0x00331F24;
        private UInt32 _P1_Life_Offset = 0x00326B48;
        private UInt32 _P1_Ammo_Offset = 0x00326B58;
        private UInt32 _Credits_Offset = 0x003280F8;
        #endregion

        //Disabling adding credits while the key stays pressed
        private bool _IsCoinKeyPressed = false;

        public Game_KonamiCastlevania(GameConfig Config) : base(Config)
        {
            _KnownMd5Prints.Add("Castlevania Arcade v2009-04-22 - clean dump", "8a6dd00e254df6ad68a944da8add6235");
        }

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
                if (string.Equals(Path.GetFileName(e.DllPath), HCVACLIBDLL_NAME, StringComparison.OrdinalIgnoreCase))
                {
                    Logger.WriteLog("==> Wanted DLL found !");
                    //HANDLE should always be in 32Bits range (https://learn.microsoft.com/fr-fr/windows/win32/winprog64/interprocess-communication?redirectedfrom=MSDN)
                    Logger.WriteLog("  + hModule: 0x" + e.LoadDllInfo.hFile.ToString("X8"));
                    Logger.WriteLog("  + fUnicode: " + e.LoadDllInfo.fUnicode.ToString());

                    _HcvACLibDll_BaseAddress = (UInt32)e.LoadDllInfo.lpBaseOfDll;
                    if (_GameConfig.GetBoolOption("Local Save"))
                        SetHack_LocalSave();

                    RaiseGameHookedEvent();
                }
            }
        }

        #endregion

        #endregion

        #region Screen

        /// <summary>
        /// The process is creating 2 windows, so we need to fill the required window handle instead of the default one (not knowing which one it is)
        /// </summary>
        /// <param name="PlayerData"></param>
        /// <returns></returns>
        public override bool ClientScale(VirtualPlayerController PlayerController)
        {
            //Convert Screen location to Client location
            if (_Process != null)
            {
                //Window size
                Win32.Rect TotalRes = new Win32.Rect();
                Win32.GetWindowRect(GetGameWindowHandle(), ref TotalRes);

                Logger.WriteLog("Window position (Px) = [ " + TotalRes.Left + ";" + TotalRes.Top + " ]");

                PlayerController.Computed_X = PlayerController.Computed_X - TotalRes.Left;
                PlayerController.Computed_Y = PlayerController.Computed_Y - TotalRes.Top;
                Logger.WriteLog("Onclient window position (Px) = [ " + PlayerController.Computed_X + "x" + PlayerController.Computed_Y + " ]");

            }
            return true;
        }

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
                   //Reading Resolution in game's memory
                    double dMaxX = (double)ReadUInt32(GameProcess_ModuleBaseAddress + _ViewportWidth_Offset);
                    double dMaxY = (double)ReadUInt32(GameProcess_ModuleBaseAddress + _ViewportHeight_Offset);
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
            //GameHooked event will be raised in the DLL LOAD procedure
            base.Apply_MemoryHacks();
        }

        #region Loader Hack
        protected override void Apply_LoaderMemoryHack()
        {
            //Removing the cursor by setting the Handle to our transparent one when calling RegisterClass() 
            if (_GameConfig.NoCursor)
            {
                if (CreateTransparentCursor()) 
                    SetHack_NoCursor();                    
            }

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        private void SetHack_NoCursor()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //lea eax,[esi+04]
            CaveMemory.Write_StrBytes("8D 46 04");
            //mov [eax+18], 
            CaveMemory.Write_StrBytes("C7 40 18");
            CaveMemory.Write_Bytes(BitConverter.GetBytes((UInt32)_CustomTransparentCursorHandle));
            //push eax
            CaveMemory.Write_StrBytes("50");
            //mov ebx,ecx
            CaveMemory.Write_StrBytes("8B D9");

            //Inject in
            CaveMemory.InjectToOffset(_NoCursor_InjectionStruct, "No Cursor");
        }

        /// <summary>
        /// When hcvACLib.dll is loaded, whanging hardcoded path to d,e,f drives
        /// </summary>
        private void SetHack_LocalSave()
        {
            byte[] NewPath = new byte[] { 0x42, 0x4F, 0x4F, 0x4B, 0x4B, 0x45, 0x45, 0x50, 0x49, 0x4E, 0x47, 0x53, 0x00, 0x00, 0x00, 0x00, 0x42, 0x4F, 0x4F, 0x4B, 0x4B, 0x45, 0x45, 0x50, 0x49, 0x4E, 0x47, 0x53, 0x2F, 0x42, 0x4F, 0x4F, 0x4B, 0x4B, 0x45, 0x45, 0x50, 0x49, 0x4E, 0x47, 0x53, 0x00, 0x00, 0x00 }; //BOOKEEPINGS
            ReplaceReadOnlyString(_HcvACLibDll_BaseAddress, new uint[] { _Bookkeeping_Patch1_Offset, _Bookkeeping_Patch2_Offset, _Bookkeeping_Patch3_Offset }, NewPath);

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        #endregion        

        #region Inputs Hack

        protected override void Apply_InputsMemoryHack()
        {
            Create_InputsDataBank();
            _Buttons_CaveAddress = InputsDatabank_Address;
            _HardwareID_CaveAddress = InputsDatabank_Address + 0x10;
            _SoftwareID_CaveAddress = InputsDatabank_Address + 0x20;

            //Force IoEmulation (normally read in the setting.csv file)
            //mov byte ptr [esi+70],01
            //mov byte ptr [esi+71],01
            //jmp HCV.exe+5807
            WriteBytes(GameProcess_ModuleBaseAddress + _IoEmulation_Patch_Offset, new byte[] { 0xC6, 0x46, 0x70, 0x01, 0xC6, 0x46, 0x71, 0x01, 0xEB, 0x1A, 0x90, 0x90, 0x90 });

            //Replacing the check for Emulated IO so that values are generated by real board
            //+83978 -> JE -----> JNE
            WriteByte(GameProcess_ModuleBaseAddress + _CheckIOEmulated_Offset, 0xEB);

            //Replacing the check for Emulated SENSORS so that values are generated by real board
            //Causes LAG (input check with hardware timeout ??)
            //+83963 -> JE -----> JNE
            //WriteByte(GameProcess_ModuleBaseAddress + 0x83963, 0xEB);

            //Forcing Sound with Credits insertion
            SetNops(GameProcess_ModuleBaseAddress, _Nop_CreditsSound);
            WriteByte(GameProcess_ModuleBaseAddress + _CreditsSoundMod_Offset, 0x29); //ORiginal code is writing in Ptr+2A, and Service is using Ptr+29        

            WriteBytes(_HardwareID_CaveAddress, Encoding.ASCII.GetBytes("ARGONLEFOU"));
            WriteBytes(_SoftwareID_CaveAddress, Encoding.ASCII.GetBytes("U-Shoot v" + System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString()));

            //Just for fun, changing Hardware/Software ID
            //mov eax, custom string
            List<byte> bCode = new List<byte>
            {
                0xB8
            };
            bCode.AddRange(BitConverter.GetBytes(_HardwareID_CaveAddress));
            for (int i = 0; i < 7; i++)
            {
                bCode.Add(0x90);
            }
            WriteBytes(GameProcess_ModuleBaseAddress + _HardwareID_Patch_Offset, bCode.ToArray());

            bCode = new List<byte>
            {
                0xB8
            };
            bCode.AddRange(BitConverter.GetBytes(_SoftwareID_CaveAddress));
            for (int i = 0; i < 7; i++)
            {
                bCode.Add(0x90);
            }
            WriteBytes(GameProcess_ModuleBaseAddress + _SoftwareID_Patch_Offset, bCode.ToArray());

            SetHack_Buttons();

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        private void SetHack_Buttons()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //mov esi, _Buttons_CaveAddress
            CaveMemory.Write_StrBytes("8B 35");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Buttons_CaveAddress));
            //mov edx, edi+48
            CaveMemory.Write_StrBytes("8B 57 48");
            //mov eax, edx
            CaveMemory.Write_StrBytes("8B C2");

            //Inject it
            CaveMemory.InjectToOffset(_Buttons_InjectionStruct, "Buttons");
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
                if (!_IsCoinKeyPressed)
                {
                    Apply_OR_ByteMask(_Buttons_CaveAddress, 0x04);
                    /*int Credits = ReadInt32(GameProcess_ModuleBaseAddress + _Credits_Offset);
                    Credits++;
                    WriteBytes(GameProcess_ModuleBaseAddress + _Credits_Offset, BitConverter.GetBytes(Credits));*/
                    WriteByte(GameProcess_ModuleBaseAddress + _CoinInSIgnal_Offset, 1);
                    _IsCoinKeyPressed = true;
                }
            }
            else if (e.ButtonName.Equals("SERVICE"))
            {
                Apply_OR_ByteMask(_Buttons_CaveAddress, 0x02);
            }
            else if (e.ButtonName.Equals("TEST"))
            {
                Apply_OR_ByteMask(_Buttons_CaveAddress, 0x01);
            }
        }

        protected override void GameController_OnButtonUp(object sender, VirtualGameControler.ButtonDownEventArgs e)
        {
            if (e.ButtonName.Equals("COIN"))
            {
                Apply_AND_ByteMask(_Buttons_CaveAddress, 0xFB);
                WriteByte(GameProcess_ModuleBaseAddress + _CoinInSIgnal_Offset, 0);
                _IsCoinKeyPressed = false;
            }
            else if (e.ButtonName.Equals("SERVICE"))
            {
                Apply_AND_ByteMask(_Buttons_CaveAddress, 0xFD);
            }
            else  if (e.ButtonName.Equals("TEST"))
            {
                Apply_AND_ByteMask(_Buttons_CaveAddress, 0xFE);
            }
        }

        protected override void UpdateAxis(PlayerID Id, int X, int Y)
        {
            if (Id != PlayerID.None)
            {
                byte[] bufferX = BitConverter.GetBytes((float)X);
                byte[] bufferY = BitConverter.GetBytes((float)Y);

                //WriteBytes(0x7281B8, bufferX);    //If using Original Sensor procedure, but causes LAG
                //WriteBytes(0x7281BC, bufferY);    //If using Original Sensor procedure, but causes LAG     
                WriteBytes(GameProcess_ModuleBaseAddress + _P1_X_Offset + ((uint)Id * 0x4E14), bufferX);   //USed with EmulatedSensor
                WriteBytes(GameProcess_ModuleBaseAddress + _P1_Y_Offset + ((uint)Id * 0x4E14), bufferY);   //USed with EmulatedSensor
            }
        }

        protected override void PlayerController_OnButtonDown(object sender, VirtualGameControler.ButtonDownEventArgs e)
        {
            if (e.Id != PlayerID.None)
            {
                UInt32 PlayerIndex = (UInt32)e.Id;

                if (e.ButtonName.Equals("YELLOW BUTTON"))
                {
                    int iMask = 0x20 << (int)PlayerIndex;
                    Apply_OR_ByteMask(_Buttons_CaveAddress, (byte)iMask);
                }
                else if (e.ButtonName.Equals("RED BUTTON"))
                {
                    if (e.Id == PlayerID.Player1)
                        Apply_OR_ByteMask(_Buttons_CaveAddress, 0x80);
                    else if (e.Id == PlayerID.Player2)
                        Apply_OR_ByteMask(_Buttons_CaveAddress + 1, 0x01);
                }
                else if (e.ButtonName.Equals("START"))
                {
                    int iMask = 0x08 << (int)PlayerIndex;
                    Apply_OR_ByteMask(_Buttons_CaveAddress, (byte)iMask);
                }
            }
        }
        protected override void PlayerController_OnButtonUp(object sender, VirtualGameControler.ButtonDownEventArgs e)
        {
            if (e.Id != PlayerID.None)
            {
                UInt32 PlayerIndex = (UInt32)e.Id;

                if (e.ButtonName.Equals("YELLOW BUTTON"))
                {
                    int iMask = 0x20 << (int)PlayerIndex;
                    Apply_AND_ByteMask(_Buttons_CaveAddress, (byte)~iMask);
                }
                else if (e.ButtonName.Equals("RED BUTTON"))
                {
                    if (e.Id == PlayerID.Player1)
                        Apply_AND_ByteMask(_Buttons_CaveAddress, 0x7F);
                    else if (e.Id == PlayerID.Player2)
                        Apply_AND_ByteMask(_Buttons_CaveAddress + 1, 0xFE);
                }
                else if (e.ButtonName.Equals("START"))
                {
                    int iMask = 0x08 << (int)PlayerIndex;
                    Apply_AND_ByteMask(_Buttons_CaveAddress, (byte)~iMask);
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
                new GameOutput(OutputId.P1_LmpCard_R),
                new GameOutput(OutputId.P1_LmpCard_G),
                new GameOutput(OutputId.P2_LmpCard_R),
                new GameOutput(OutputId.P2_LmpCard_G),
                new GameOutput(OutputId.P1_Whip_R),
                new GameOutput(OutputId.P1_Whip_G),
                new GameOutput(OutputId.P1_Whip_B),
                new GameOutput(OutputId.P2_Whip_R),
                new GameOutput(OutputId.P2_Whip_G),
                new GameOutput(OutputId.P2_Whip_B),
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
            byte bOutput = ReadByte(GameProcess_ModuleBaseAddress + _Outputs_Offset);
            SetOutputValue(OutputId.P1_LmpStart, GetLampAnalogValue(GameProcess_ModuleBaseAddress + _Outputs_Offset));
            SetOutputValue(OutputId.P2_LmpStart, GetLampAnalogValue(GameProcess_ModuleBaseAddress + _Outputs_Offset + 4));
            SetOutputValue(OutputId.P1_LmpCard_R, GetLampAnalogValue(GameProcess_ModuleBaseAddress + _Outputs_Offset + 8));
            SetOutputValue(OutputId.P1_LmpCard_G, GetLampAnalogValue(GameProcess_ModuleBaseAddress + _Outputs_Offset + 12));
            SetOutputValue(OutputId.P2_LmpCard_R, GetLampAnalogValue(GameProcess_ModuleBaseAddress + _Outputs_Offset + 16));
            SetOutputValue(OutputId.P2_LmpCard_G, GetLampAnalogValue(GameProcess_ModuleBaseAddress + _Outputs_Offset + 20));
            SetOutputValue(OutputId.P1_Whip_R, GetLampAnalogValue(GameProcess_ModuleBaseAddress + _Outputs_Offset + 24));
            SetOutputValue(OutputId.P1_Whip_G, GetLampAnalogValue(GameProcess_ModuleBaseAddress + _Outputs_Offset + 28));
            SetOutputValue(OutputId.P1_Whip_B, GetLampAnalogValue(GameProcess_ModuleBaseAddress + _Outputs_Offset + 32));
            SetOutputValue(OutputId.P2_Whip_R, GetLampAnalogValue(GameProcess_ModuleBaseAddress + _Outputs_Offset + 36));
            SetOutputValue(OutputId.P2_Whip_G, GetLampAnalogValue(GameProcess_ModuleBaseAddress + _Outputs_Offset + 40));
            SetOutputValue(OutputId.P2_Whip_B, GetLampAnalogValue(GameProcess_ModuleBaseAddress + _Outputs_Offset + 44));

            for (uint i = 0; i < _GameConfig.PlayerCount; i++)
            {
                _Life[i] = ReadInt32(GameProcess_ModuleBaseAddress + _P1_Life_Offset + (i * 0x1C0));
                _Ammo[i] = ReadByte(GameProcess_ModuleBaseAddress + _P1_Ammo_Offset + (i * 0x1C0));
                //When not playing, default value is 0x2710 (10 000)
                if (_Life[i] != 10000)
                {
                    //[Damaged] custom Output                
                    if (_Life[i] < _LastLife[i])
                        SetOutputValue(OutputId.P1_Damaged + i, 1);

                    if (_Life[i] > 0)
                    {
                        //Custom Recoil
                        if (_Ammo[i] < _LastAmmo[i])
                            SetOutputValue(OutputId.P1_CtmRecoil +i, 1);
                    }
                    else
                    {
                        _Ammo[i] = 0;
                    }
                }
                else
                {
                    _Ammo[i] = 0;
                    _Life[i] = 0;
                }

                _LastLife[i] = _Life[i];
                _LastAmmo[i] = _Ammo[i];
                SetOutputValue(OutputId.P1_Life + i, _Life[i]);
                SetOutputValue(OutputId.P1_Ammo + i, _Ammo[i]);
            }

            //Credits
            SetOutputValue(OutputId.Credits, ReadByte(GameProcess_ModuleBaseAddress + _Credits_Offset));
        }

        #endregion

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
    }
}
