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
    public class Game_TtxGaiaAttack4 : BaseGame32
    {
        protected override bool PATCH_PEB_DEBUG_FLAG { get { return false; } }

        private const string LANGUAGE_OPTION = "Language";

        #region LOADER MEMORY ADDRESSES                
        private UInt32 _FolderAttribute_Patch_Offset = 0x000016C2;
        private InjectionStruct32 _Language_InjectionStruct = new InjectionStruct32(0x001264D7, 5);
        private InjectionStruct32 _NoCursor_InjectionStruct = new InjectionStruct32(0x001160BB, 6);
        #endregion

        #region INPUTS MEMORY ADDRESSES    
        private const UInt32 _ScreenWidth_Offset = 0x0032FC10;
        private const UInt32 _ScreenHeight_Offset = 0x0032FC14;
        private UInt32 _JvsEnable_Patch_Offset = 0x001146FA;
        private UInt32 _GunEnable_Patch_Offset = 0x0011471B;
        private UInt32 _LpKeyState_Ptr_Offset = 0x00B416C0;
        private UInt32 _JVS_Trigger_Offset = 0x00B3B890;
        private UInt32 _JVS_Axis_Offset = 0x00B3B834;
        private UInt32 _LibVer_Patch_Offset = 0x0011F861;
        private InjectionStruct32 _Axes_InjectionStruct = new InjectionStruct32(0x00114C99, 13);
        private InjectionStruct32 _Buttons_InjectionStruct = new InjectionStruct32(0x000012ED, 6);

        //Custom Data
        private UInt32 _AxesX_CaveAddress;
        private UInt32 _AxesY_CaveAddress;
        private UInt32 _LpKeyState_CaveAddress;
        private UInt32 _LibVer_CaseAddress;
        #endregion

        #region OUTPUTS MEMORY ADDRESSES
        private UInt32 _PlayersStatus_Offset = 0x00B16A98;
        private UInt32 _PlayersLife_Offset = 0x00B16E60;
        private UInt32 _OutputsOffset = 0x01FE7200;
        private UInt32 _Credits_Offset = 0x00B3B7C8;
        private InjectionStruct32 _Recoil_InjectionStruct = new InjectionStruct32(0x00073BD4, 6);
        private InjectionStruct32 _Damage1_InjectionStruct = new InjectionStruct32(0x00076651, 7);
        private InjectionStruct32 _Damage2_InjectionStruct = new InjectionStruct32(0x00076499, 7);

        //Custom Data
        private UInt32 _DamageStatus_CaveAddress;
        private UInt32 _RecoilStatus_CaveAddress;
        #endregion

        //0 = ENGLISH
        //1 = JAPANESE
        //2 = CHINEESE
        protected byte _LanguageID = 1;

        public Game_TtxGaiaAttack4(GameConfig Config) : base(Config)
        {
            _KnownMd5Prints.Add("Gaia Attack 4 v1.02 - Original", "f2d8e8f7d3a9a29d4804ff7cb29aa8a2");
            _KnownMd5Prints.Add("Gaia Attack 4 v1.02 - For JConfig", "94fe4e73f6fd915ffb10885dc091d5cb");
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

                    //This engine (common with other TTX shooter) is waiting for X and Y value in range [0 ; WindowSize]
                    //BUT using the raw window size is troublesome when the game is combined with DxWnd as the
                    //resulting real window is not the same size as the game engine parameters (SCREEN_WITH, RENDER_WIDTH, etc...)
                    //That's why we're going to read the memory to find the INI parameter and scale the X,Y values accordingly
                    double GameResX = (double)ReadUInt32(GameProcess_ModuleBaseAddress + _ScreenWidth_Offset);
                    double GameResY = (double)ReadInt32(GameProcess_ModuleBaseAddress + _ScreenHeight_Offset);

                    Logger.WriteLog("Game engine render resolution (Px) = [ " + GameResX + "x" + GameResY + " ]");

                    double dMinX = 0.0;
                    double dMaxX = GameResX;
                    double dMinY = 0.0;
                    double dMaxY = GameResY;
                    double dRangeX = dMaxX - dMinX + 1;
                    double dRangeY = dMaxY - dMinY + 1;

                    double RatioX = GameResX / TotalResX;
                    double RatioY = GameResY / TotalResY;

                    PlayerController.Computed_X = Convert.ToInt16(Math.Round(RatioX * PlayerController.Computed_X));
                    PlayerController.Computed_Y = Convert.ToInt16(Math.Round(RatioY * PlayerController.Computed_Y));

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
            /// On start, game is checking the attributes of the "Data" folder
            /// If it not EXACTLY 'FILE_ATTRIBUTE_DIRECTORY', the game use SetCurrentDirectoryA() to parent folder 
            /// This causes the game to not find HAUNTED2.INI file, and also can't find the game files in Data folder
            ///
            /// Newer windows can have FILE_ATTRIBUTE_ARCHIVE or FILE_ATTRIBUTE_NOT_CONTENT_INDEXED attributes on top of the checked one.....failing ....
            WriteByte(GameProcess_ModuleBaseAddress + _FolderAttribute_Patch_Offset, 0xEB);

            if (_GameConfig.GetStringOption(LANGUAGE_OPTION).Equals("English"))
            {
                _LanguageID = 0;
            }
            else if (_GameConfig.GetStringOption(LANGUAGE_OPTION).Equals("Japanese"))
            {
                _LanguageID = 1;
            }
            else if (_GameConfig.GetStringOption(LANGUAGE_OPTION).Equals("Chineese"))
            {
                _LanguageID = 2;
            }

            SetHack_Language();

            if (_GameConfig.NoCursor)
            {
                if (CreateTransparentCursor())
                {
                    SetHack_NoCursor();
                }
            }

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        /// <summary>
        /// Replacing call to LoadCursorA() with value of Handle
        /// Need to adjust ESP as function is not called and pushed happened before
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

        /// <summary>
        /// Language option in MUSEUM.ini does not have any effect
        /// The setting is in the savefiles, saved on the disk.
        /// But we can force it to override in the game memory (without saving the data to the savefile)
        /// The function is called to find some option based on a string identifier, "golng" is the one we want to alter
        /// </summary>
        private void SetHack_Language()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //cmp [esi],6E6C6F67 (=> "goln" string for "golng")
            CaveMemory.Write_StrBytes("81 3E 67 6F 6C 6E");
            //jne OriginalCode
            CaveMemory.Write_StrBytes("75 15");
            //test eax,eax
            CaveMemory.Write_StrBytes("85 C0");
            //je OriginalCode
            CaveMemory.Write_StrBytes("74 11");

            //cmp dword ptr [eax+04],00
            CaveMemory.Write_StrBytes("83 78 04 00");
            //jne OriginalCode
            CaveMemory.Write_StrBytes("75 0B");

            //push eax
            CaveMemory.Write_StrBytes("50");
            //mov eax,[eax+8]
            CaveMemory.Write_StrBytes("8B 40 08");
            //mov [eax], _LanguagueID
            CaveMemory.Write_StrBytes("C7 00");
            CaveMemory.Write_Bytes(BitConverter.GetBytes((UInt32)_LanguageID));
            //pop eax
            CaveMemory.Write_StrBytes("58");
            //OriginalCode
            //ret
            CaveMemory.Write_StrBytes("C3");

            //Inject it
            CaveMemory.InjectToOffset(_Language_InjectionStruct, "Language");
        }

        #endregion        

        #region Inputs Hack

        protected override void Apply_InputsMemoryHack()
        {
            Create_InputsDataBank();
            _LibVer_CaseAddress = InputsDatabank_Address;
            _AxesX_CaveAddress = InputsDatabank_Address + 0x10;     //4 * 4 Bytes (P1 - P4)
            _AxesY_CaveAddress = InputsDatabank_Address + 0x20;     //4 * 4 Bytes (P1 - P4)
            _LpKeyState_CaveAddress = InputsDatabank_Address + 0x30;

            //If JVSENABLE is set in config file, make sure it will be read as 0:
            //Setting default value as "0" instead of 1
            //Faking setting not found in ini file to not override value
            WriteByte(GameProcess_ModuleBaseAddress + _JvsEnable_Patch_Offset + 1, 0x00);
            WriteByte(GameProcess_ModuleBaseAddress + _JvsEnable_Patch_Offset + 7, 0xEB);

            //Same thing for GUNENABLE
            WriteByte(GameProcess_ModuleBaseAddress + _GunEnable_Patch_Offset + 1, 0x00);
            WriteByte(GameProcess_ModuleBaseAddress + _GunEnable_Patch_Offset + 5, 0xEB);

            // Displaying LibVer string in TEST menu
            WriteBytes(_LibVer_CaseAddress, Encoding.ASCII.GetBytes(System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString()));
            WriteBytes(GameProcess_ModuleBaseAddress + _LibVer_Patch_Offset, BitConverter.GetBytes(_LibVer_CaseAddress));

            SetHack_Axes();
            SetHack_Buttons();

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        /// <summary>
        /// With debug controls, all players share same memory values so we split them :
        /// Changing proc so that X and Y will be read on custom memomy values
        /// We will feed it with devices axis data
        /// Player index is stored on ESI ( from 0 to 3) so basically we will use some indexing to get values :
        /// _AxesX_CaveAddress[4*ESI] and _AxesY_CaveAddress[4*ESI]
        private void SetHack_Axes()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //mov edx,[_AxesX_CaveAddress + esi*4]
            CaveMemory.Write_StrBytes("8B 14 B5");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_AxesX_CaveAddress));
            //mov [edi], edx
            CaveMemory.Write_StrBytes("89 17");
            //mov eax,[_AxesY_CaveAddress + esi*4]
            CaveMemory.Write_StrBytes("8B 04 B5");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_AxesY_CaveAddress));

            //Inject it
            CaveMemory.InjectToOffset(_Axes_InjectionStruct, "Axes");
        }

        /// <summary>
        /// Replacing GetKeyboardState call, filling the buffer with our own values (see WINUSER.H for virtualkey codes)
        /// </summary>
        private void SetHack_Buttons()
        {
            //Nopping the Call to GetKeyboardState located after the codecave, I would usually make the codecave jmp over all this but the code has
            //reference to some hardcoded program memory location, which isn't the best thing to do if another binary comes out
            WriteBytes(GameProcess_ModuleBaseAddress + _Buttons_InjectionStruct.InjectionOffset + 0x0C, new byte[] { 0x90, 0x90 });

            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //mov eax,[game.exe + _LpKeyState_Ptr_Offset]
            CaveMemory.Write_StrBytes("8B 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _LpKeyState_Ptr_Offset));
            //push ecx
            CaveMemory.Write_StrBytes("51");
            //push esi
            CaveMemory.Write_StrBytes("56");
            //push edi
            CaveMemory.Write_StrBytes("57");
            //mov ecx,00000040
            CaveMemory.Write_StrBytes("B9 40 00 00 00");
            //mov esi,_LpKeyState_CaveAddress
            CaveMemory.Write_StrBytes("BE");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_LpKeyState_CaveAddress));
            //mov edi,eax
            CaveMemory.Write_StrBytes("8B F8");
            //repe movsd
            CaveMemory.Write_StrBytes("F3 A5");
            //pop edi
            CaveMemory.Write_StrBytes("5F");
            //pop esi
            CaveMemory.Write_StrBytes("5E");
            //pop ecx
            CaveMemory.Write_StrBytes("59");

            //Inject it
            CaveMemory.InjectToOffset(_Buttons_InjectionStruct, "Buttons");
        }

        #endregion

        #region Outputs Hack

        protected override void Apply_OutputsMemoryHack()
        {
            Create_OutputsDataBank();
            _RecoilStatus_CaveAddress = OutputsDatabank_Address;
            _DamageStatus_CaveAddress = OutputsDatabank_Address + 0x04;

            SetHack_Recoil();
            SetHack_Damage(_Damage1_InjectionStruct);
            SetHack_Damage(_Damage2_InjectionStruct);

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        /// <summary>
        /// Code injection where the game is calling for rumble because of Recoil
        /// That way we can knwo when a bullet is fired and create our own output
        /// </summary>
        private void SetHack_Recoil()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //Looks like a byte value set when vibration starts and decreasing each frame (vibration duration ?)
            //cmp dword ptr [ecx*8+game.exe+B16E34], 00
            CaveMemory.Write_StrBytes("83 3C CD");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + 0x00B16E34));
            CaveMemory.Write_StrBytes("00");
            //jne originalCode
            CaveMemory.Write_StrBytes("75 07");
            //mov byte ptr [ecx + _P1_RecoilStatus_CaveAddress],01
            CaveMemory.Write_StrBytes("C6 81");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_RecoilStatus_CaveAddress));
            CaveMemory.Write_StrBytes("01");
            //OriginalCode:
            //movss dword ptr [esp+3Ch], xmm0
            CaveMemory.Write_StrBytes("F3 0F 11 44 24 3C");

            //Inject it
            CaveMemory.InjectToOffset(_Recoil_InjectionStruct, "Recoil");
        }

        /// <summary>
        /// Code injection where the game is calling for rumble because of damage.
        /// That way we can known when a player is damaged and make our own output.
        /// </summary>
        private void SetHack_Damage(InjectionStruct32 Injection)
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //mov ecx, edi
            CaveMemory.Write_StrBytes("8B CF");
            //Looks like a byte value set when vibration starts and decreasing each frame (vibration duration ?)
            //cmp dword ptr [ecx*8+game.exe+B16E34], 00
            CaveMemory.Write_StrBytes("83 3C CD");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + 0x00B16E34));
            CaveMemory.Write_StrBytes("00");
            //jne originalCode
            CaveMemory.Write_StrBytes("75 07");
            //mov byte ptr [ecx + _P1_DamageStatus_CaveAddress],01
            CaveMemory.Write_StrBytes("C6 81");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_DamageStatus_CaveAddress));
            CaveMemory.Write_StrBytes("01");
            //OriginalCode:
            //mov eax, 4
            CaveMemory.Write_StrBytes("B8 04 00 00 00");

            //Inject it
            CaveMemory.InjectToOffset(Injection, "Damage");
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
                WriteByte(_LpKeyState_CaveAddress + 0x53, 0x80);
            }
            else if (e.ButtonName.Equals("SERVICE"))
            {
                WriteByte(_LpKeyState_CaveAddress + 0x53, 0x80);
            }
            else if (e.ButtonName.Equals("TEST"))
            {
                WriteByte(_LpKeyState_CaveAddress + 0x1B, 0x80);
            }
        }

        protected override void GameController_OnButtonUp(object sender, VirtualGameControler.ButtonDownEventArgs e)
        {
            if (e.ButtonName.Equals("COIN"))
            {
                WriteByte(_LpKeyState_CaveAddress + 0x53, 0x00);
            }
            else if (e.ButtonName.Equals("SERVICE"))
            {
                WriteByte(_LpKeyState_CaveAddress + 0x53, 0x00);
            }
            else if (e.ButtonName.Equals("TEST"))
            {
                WriteByte(_LpKeyState_CaveAddress + 0x1B, 0x00);
            }
        }

        protected override void UpdateAxis(PlayerID Id, int X, int Y)
        {
            if (Id != PlayerID.None)
            {
                byte[] bufferX = BitConverter.GetBytes(X);
                byte[] bufferY = BitConverter.GetBytes(Y);

                WriteBytes(_AxesX_CaveAddress + ((uint)Id * 0x04), bufferX);
                WriteBytes(_AxesY_CaveAddress + ((uint)Id * 0x04), bufferY);

                //Only for TEST menu display
                double dRawX = (double)X * 0x3FFF / (double)BitConverter.ToUInt16(ReadBytes(GameProcess_ModuleBaseAddress + _ScreenWidth_Offset, 2), 0);
                double dRawY = (double)Y * 0x3FFF / (double)BitConverter.ToUInt16(ReadBytes(GameProcess_ModuleBaseAddress + _ScreenHeight_Offset, 2), 0);
                WriteBytes(GameProcess_ModuleBaseAddress + _JVS_Axis_Offset + ((uint)Id * 0x0A), BitConverter.GetBytes((UInt16)dRawX));
                WriteBytes(GameProcess_ModuleBaseAddress + _JVS_Axis_Offset + ((uint)Id * 0x0A) + 2, BitConverter.GetBytes((UInt16)dRawY));
            }
        }

        protected override void PlayerController_OnButtonDown(object sender, VirtualGameControler.ButtonDownEventArgs e)
        {
            if (e.Id != PlayerID.None)
            {
                UInt32 PlayerIndex = (UInt32)e.Id;

                if (e.ButtonName.Equals("TRIGGER"))
                {
                    switch (e.Id)
                    {
                        case PlayerID.Player1: WriteByte(_LpKeyState_CaveAddress + 0x01, 0x80); break;
                        case PlayerID.Player2: WriteByte(_LpKeyState_CaveAddress + 0x02, 0x80); break;
                        case PlayerID.Player3: WriteByte(_LpKeyState_CaveAddress + 0x63, 0x80); break;
                        case PlayerID.Player4: WriteByte(_LpKeyState_CaveAddress + 0x64, 0x80); break;
                        default: break;
                    }
                    //This is only for TEST MENU input test
                    WriteByte(GameProcess_ModuleBaseAddress + _JVS_Trigger_Offset + (PlayerIndex * 0x04), 0x01);
                }
                else if (e.ButtonName.Equals("START"))
                {
                    WriteByte(_LpKeyState_CaveAddress + 0x31 + PlayerIndex, 0x80);
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
                    switch (e.Id)
                    {
                        case PlayerID.Player1: WriteByte(_LpKeyState_CaveAddress + 0x01, 0x00); break;
                        case PlayerID.Player2: WriteByte(_LpKeyState_CaveAddress + 0x02, 0x00); break;
                        case PlayerID.Player3: WriteByte(_LpKeyState_CaveAddress + 0x63, 0x00); break;
                        case PlayerID.Player4: WriteByte(_LpKeyState_CaveAddress + 0x64, 0x00); break;
                        default: break;
                    }
                    //This is only for TEST MENU input test
                    WriteByte(GameProcess_ModuleBaseAddress + _JVS_Trigger_Offset + (PlayerIndex * 0x04), 0x00);
                }
                else if (e.ButtonName.Equals("START"))
                {
                    WriteByte(_LpKeyState_CaveAddress + 0x31 + PlayerIndex, 0x00);
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
                new GameOutput(OutputId.P1_Lmp_R),
                new GameOutput(OutputId.P1_Lmp_G),
                new GameOutput(OutputId.P1_Lmp_B),
                new GameOutput(OutputId.P2_Lmp_R),
                new GameOutput(OutputId.P2_Lmp_G),
                new GameOutput(OutputId.P2_Lmp_B),
                new GameOutput(OutputId.P1_GunMotor),
                new GameOutput(OutputId.P2_GunMotor),
                new GameOutput(OutputId.P3_GunMotor),
                new GameOutput(OutputId.P4_GunMotor),
                new AsyncGameOutput(OutputId.P1_CtmRecoil, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_ON_DELAY), ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_OFF_DELAY), 0),
                new AsyncGameOutput(OutputId.P2_CtmRecoil, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_ON_DELAY), ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_OFF_DELAY), 0),
                new AsyncGameOutput(OutputId.P3_CtmRecoil, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_ON_DELAY), ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_OFF_DELAY), 0),
                new AsyncGameOutput(OutputId.P4_CtmRecoil, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_ON_DELAY), ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_OFF_DELAY), 0),
                new GameOutput(OutputId.P1_Life),
                new GameOutput(OutputId.P2_Life),
                new GameOutput(OutputId.P3_Life),
                new GameOutput(OutputId.P4_Life),
                new AsyncGameOutput(OutputId.P1_Damaged, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_DAMAGED_DELAY), 100, 0),
                new AsyncGameOutput(OutputId.P2_Damaged, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_DAMAGED_DELAY), 100, 0),
                new AsyncGameOutput(OutputId.P3_Damaged, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_DAMAGED_DELAY), 100, 0),
                new AsyncGameOutput(OutputId.P4_Damaged, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_DAMAGED_DELAY), 100, 0),
                new GameOutput(OutputId.Credits)
            };
        }

        /// <summary>
        /// Update all Outputs values before sending them to MameHooker
        /// </summary>
        public override void UpdateOutputValues()
        {
            //Original outputs
            SetOutputValue(OutputId.P1_LmpStart, ReadByte(GameProcess_ModuleBaseAddress + _OutputsOffset + 2) >> 6 & 0x01);
            SetOutputValue(OutputId.P2_LmpStart, ReadByte(GameProcess_ModuleBaseAddress + _OutputsOffset + 2) >> 5 & 0x01);
            SetOutputValue(OutputId.P3_LmpStart, ReadByte(GameProcess_ModuleBaseAddress + _OutputsOffset) >> 5 & 0x01);
            SetOutputValue(OutputId.P4_LmpStart, ReadByte(GameProcess_ModuleBaseAddress + _OutputsOffset) >> 4 & 0x01);
            SetOutputValue(OutputId.P1_Lmp_R, ReadByte(GameProcess_ModuleBaseAddress + _OutputsOffset + 2) >> 7 & 0x01);
            SetOutputValue(OutputId.P1_Lmp_G, ReadByte(GameProcess_ModuleBaseAddress + _OutputsOffset + 2) >> 4 & 0x01);
            SetOutputValue(OutputId.P1_Lmp_B, ReadByte(GameProcess_ModuleBaseAddress + _OutputsOffset + 2) >> 3 & 0x01);
            SetOutputValue(OutputId.P2_Lmp_R, ReadByte(GameProcess_ModuleBaseAddress + _OutputsOffset + 1) >> 7 & 0x01);
            SetOutputValue(OutputId.P2_Lmp_G, ReadByte(GameProcess_ModuleBaseAddress + _OutputsOffset + 1) >> 6 & 0x01);
            SetOutputValue(OutputId.P2_Lmp_B, ReadByte(GameProcess_ModuleBaseAddress + _OutputsOffset + 1) >> 5 & 0x01);
            SetOutputValue(OutputId.P1_GunMotor, ReadByte(GameProcess_ModuleBaseAddress + _OutputsOffset + 2) >> 2 & 0x01);
            SetOutputValue(OutputId.P2_GunMotor, ReadByte(GameProcess_ModuleBaseAddress + _OutputsOffset + 2) >> 1 & 0x01);
            SetOutputValue(OutputId.P3_GunMotor, ReadByte(GameProcess_ModuleBaseAddress + _OutputsOffset) >> 3 & 0x01);
            SetOutputValue(OutputId.P4_GunMotor, ReadByte(GameProcess_ModuleBaseAddress + _OutputsOffset) >> 2 & 0x01);

            for (uint i = 0; i < _GameConfig.PlayerCount; i++)
            {
                //[Recoil] custom Output
                if (ReadByte(_RecoilStatus_CaveAddress + i) == 1)
                {
                    SetOutputValue(OutputId.P1_CtmRecoil + i, 1);
                    WriteByte(_RecoilStatus_CaveAddress + i, 0x00);
                }

                //[Damaged] custom Output
                if (ReadByte(_DamageStatus_CaveAddress + i) == 1)
                {
                    SetOutputValue(OutputId.P1_Damaged + i, 1);
                    WriteByte(_DamageStatus_CaveAddress + i, 0x00);
                }


                //Life if player is InGame
                //Player Status :
                //[0] : Inactive
                //[1] : In-Game
                //[2] : Continue Screen
                //[3] : Game Over
                int Player_Status = ReadByte(GameProcess_ModuleBaseAddress + _PlayersStatus_Offset + (i * 0x08));

                if (Player_Status == 1)
                    SetOutputValue(OutputId.P1_Life + i, ReadByte(GameProcess_ModuleBaseAddress + _PlayersLife_Offset + (i * 0x08)));
                else
                    SetOutputValue(OutputId.P1_Life + i, 0);
            }
            SetOutputValue(OutputId.Credits, ReadByte(GameProcess_ModuleBaseAddress + _Credits_Offset));
        }

        #endregion
    }
}
