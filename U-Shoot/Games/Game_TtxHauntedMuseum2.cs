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
    public class Game_TtxHauntedMuseum2 : BaseGame32
    {
        protected override string GAMEDATA_FOLDER { get { return @"Ttx\hmuseum2"; } }
        protected override bool PATCH_PEB_DEBUG_FLAG { get { return false; } }

        private const string LANGUAGE_OPTION = "Language";

        #region LOADER MEMORY ADDRESSES                
        private UInt32 _FolderAttribute_Patch_Offset = 0x000017F2;
        private UInt32 _NoCrosshair_Patch1_Offset = 0x007257D;
        private UInt32 _NoCrosshair_Patch2_Offset = 0x0072149;
        private UInt32 _AmmoFont_Patch1_Offset = 0x0029DE6C;
        private UInt32 _AmmoFont_Patch2_Offset = 0x0029D0E0;
        private UInt32 _AmmoPosition1_Patch_Offset = 0x00072251;
        private UInt32 _AmmoPosition2_Patch_Offset = 0x000722EF;
        private UInt32 _AmmoPosition3_Patch_Offset = 0x00072385;
        private InjectionStruct32 _Language_InjectionStruct = new InjectionStruct32(0x000B1107, 5);
        private InjectionStruct32 _NoCursor_InjectionStruct = new InjectionStruct32(0x000A074A, 6);
        private InjectionStruct32 _AmmoPosition_InjectionStruct = new InjectionStruct32(0x00072184, 5);

        //Custom Data
        private UInt32 _AmmoDisplayX_CaveAddress;
        private UInt32 _AmmoDisplayY_CaveAddress;
        #endregion

        #region INPUTS MEMORY ADDRESSES    
        private UInt32 _ScreenWidth_Offset = 0x00332EF0;
        private UInt32 _ScreenHeight_Offset = 0x00332EF4;
        private UInt32 _JvsUpdateRemove_Patch_Offset = 0x000010CA;
        private UInt32 _JvsEnable_Patch_Offset = 0x0009E5BA;
        private UInt32 _GunEnable_Patch_Offset = 0x0009E5DB;
        private UInt32 _JVS_Trigger_Offset = 0x00AE8920;
        private UInt32 _JVS_Axis_Offset = 0x00AE889C;
        private UInt32 _LibVer_Patch_Offset = 0x000A970D;
        private InjectionStruct32 _Axis_InjectionStruct = new InjectionStruct32(0x0009ECD0, 13);
        private InjectionStruct32 _Buttons_InjectionStruct = new InjectionStruct32(0x000B2A89, 7);

        //Custom Data
        private UInt32 _AxesX_CaveAddress;
        private UInt32 _AxesY_CaveAddress;
        private UInt32 _LpKeyState_CaveAddress;
        private UInt32 _LibVer_CaseAddress;
        #endregion

        #region OUTPUTS MEMORY ADDRESSES
        private UInt32 _PlayersStatus_Offset = 0x00AD1704;
        private UInt32 _PlayersLife_Offset = 0x00AD4028;
        private UInt32 _PlayersAmmo_Offset = 0x00AD40DC;
        private UInt32 _Player2AmmoIndex = 0x8C;
        private UInt32 _Outputs_Offset = 0x0029AE434;
        private UInt32 _Credits_Offset = 0x00AE7AF8;
        #endregion

        //0 = ENGLISH
        //1 = JAPANESE
        //2 = CHINEESE
        protected byte _LanguageID = 1;    

        public Game_TtxHauntedMuseum2(GameConfig Config) : base(Config)
        {
            //Keeping all those CRC for debug data, but they are all based on 3 different dumps.
            //Loading corrsponding memory data will be done on process creation, based on a byte check in the EXE
            _KnownMd5Prints.Add("Haunted Museum 2 v1.01 JPN - First Release", "ede2a10fe37221d3994f31553b3a4ef5");
            _KnownMd5Prints.Add("Haunted Museum 2 v1.01 JPN - First Release patched  for NoCrosshair", "e7d9f02fa52130707c16e9a83ba72dff");
            _KnownMd5Prints.Add("Haunted Museum 2 v1.00 - For JConfig (v1.6) - game_v1.00_BGR.exe", "9b5567bda69941923feb3f2e05619c68");
            _KnownMd5Prints.Add("Haunted Museum 2 v1.00 - For JConfig (v1.6) - game_v1.00_BGR.exe patched  for NoCrosshair", "f8c9d82705e04e4e03e010ce51ffc16b");

            _KnownMd5Prints.Add("Haunted Museum 2 v1.00 - For JConfig (v1.6) - game_v1.01_JPN.exe", "264d671b83282a09701b27f2249c5d0d");
            _KnownMd5Prints.Add("Haunted Museum 2 v1.00 - For JConfig (v1.6) - game_v1.01_JPN.exe patched  for NoCrosshair", "6b6452843f2d07fd66814e7d0d6af765");            
            _KnownMd5Prints.Add("Fright Fear Land v1.01 JPN - Original", "d3639bf04ababc7246df5a0a0892408b");
            _KnownMd5Prints.Add("Fright Fear Land v1.01 JPN - Original + Patched Header", "228a3d0e81141c3850459bfaf968321a");
            _KnownMd5Prints.Add("Fright Fear Land v1.01 JPN - Original + Patched Header + NoCrosshair", "80e10a8e5363205b670f9366e3fe78b1");
            _KnownMd5Prints.Add("Fright Fear Land v1.00 USA - Stripped", "0320d68acfb7ee7b4784ed43b113f0a0");
            _KnownMd5Prints.Add("Fright Fear Land v1.00 USA - Stripped + Patched Header", "06ef6c57a1200bed1c9b0fa8ad172aee");
            _KnownMd5Prints.Add("Fright Fear Land v1.00 USA - Stripped + Patched Header + NoCrosshair", "93dcb39c43ce0906676d6673eb09c5e6");

            _KnownMd5Prints.Add("Haunted Museum 2 v1.01 JPN - Second release", "fb493eda4cbc8a0866fa733fb784f0e5");
            _KnownMd5Prints.Add("Haunted Museum 2 v1.00 - For JConfig (v1.6) - game_v1.01_JPN_v2.exe", "0e8f49eb448ff7c9bd448940a53ac16a");

            //This is one of the 3 but I don't remember which one...
            _KnownMd5Prints.Add("Haunted Museum 2 v1.00 - GameLoaderAllRH patched binary", "8372323ffceebfd064707064ac00c043");
        }

        #region Debugger Events

        #region CREATE_PROCESS_DEBUG_EVENT

        protected override void CreateProcessEvent(object sender, x86_x64_Debugger.CreateProcessDebugEventArgs e)
        {
            base.CreateProcessEvent(sender, e);

            //Loading corresponding data based on where the string will be found in memory
            //Easier to just maintain 3 .cfg files instead of one for each CRC difference of the same dump
            string sGolng = "golng\0";

            if (Encoding.ASCII.GetString(ReadBytes(GameProcess_ModuleBaseAddress + 0x00291C7C, 6)).Equals(sGolng))
            {
                Logger.WriteLog("Loading data based on 'Haunted Museum 2 v1.01 JPN - First Release' dump...");
                ReadGameDataFromMd5Hash("ede2a10fe37221d3994f31553b3a4ef5", GAMEDATA_FOLDER);
            }
            else if (Encoding.ASCII.GetString(ReadBytes(GameProcess_ModuleBaseAddress + 0x003093A4, 6)).Equals(sGolng))
            {
                Logger.WriteLog("Loading data based on 'Frigh Fear Land' dump...");
                ReadGameDataFromMd5Hash("264d671b83282a09701b27f2249c5d0d", GAMEDATA_FOLDER);
            }
            else if (Encoding.ASCII.GetString(ReadBytes(GameProcess_ModuleBaseAddress + 0x0028EEF4, 6)).Equals(sGolng))
            {
                Logger.WriteLog("Loading data based on 'Haunted Museum 2 v1.01 JPN - Second Release' dump...");
                ReadGameDataFromMd5Hash("fb493eda4cbc8a0866fa733fb784f0e5", GAMEDATA_FOLDER);
            }

            //If not one of those 3 case.....just continue with CRC check as oroginal
        }

        #endregion

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
                    double GameResX = (double)ReadInt32(GameProcess_ModuleBaseAddress + _ScreenWidth_Offset);
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
            Create_LoaderDataBank();
            _AmmoDisplayX_CaveAddress = LoaderDatabank_Address;
            _AmmoDisplayY_CaveAddress = LoaderDatabank_Address + 0x04;

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
            _AxesX_CaveAddress = InputsDatabank_Address + 0x10; 
            _AxesY_CaveAddress = InputsDatabank_Address + 0x18;
            _LpKeyState_CaveAddress = InputsDatabank_Address + 0x20;

            //If JVSENABLE is set in config file, make sure it will be read as 0:
            //Setting default value as "0" instead of 1
            //Faking setting not found in ini file to not override value
            WriteByte(GameProcess_ModuleBaseAddress + _JvsEnable_Patch_Offset + 1, 0x00);
            WriteByte(GameProcess_ModuleBaseAddress + _JvsEnable_Patch_Offset + 7, 0xEB);

            //Same thing for GUNENABLE
            WriteByte(GameProcess_ModuleBaseAddress + _GunEnable_Patch_Offset + 1, 0x00);
            WriteByte(GameProcess_ModuleBaseAddress + _GunEnable_Patch_Offset + 5, 0xEB);

            // At start, game is running ujvs_up_silent.exe to try to update JVS
            // Removing that (and also removing popup if update failed, as well as removing COM2 access)
            WriteBytes(GameProcess_ModuleBaseAddress + _JvsUpdateRemove_Patch_Offset, new byte[] { 0xEB, 0x52 });

            // Displaying LibVer string in TEST menu
            WriteBytes(_LibVer_CaseAddress, Encoding.ASCII.GetBytes(System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString()));
            WriteBytes(GameProcess_ModuleBaseAddress + _LibVer_Patch_Offset, BitConverter.GetBytes(_LibVer_CaseAddress));

            SetHack_Axis();
            SetHack_Buttons();

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        /// <summary>
        /// P1 and P2 share same memory values so we split them :
        /// Changing proc so that X and Y will be read on custom memomy values.
        /// We will feed it with device axis data.
        /// Player index is stored on ESI ( from 0 to 1) so basically we will use some indexing to get values :
        /// _AxesX_CaveAddress[4*ESI] and _AxesY_CaveAddress[4*ESI]
        /// </summary>
        private void SetHack_Axis()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //mov edx,[_AxesX_CaveAddress + edi*4]
            CaveMemory.Write_StrBytes("8B 14 B5");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_AxesX_CaveAddress));
            //mov [edi], edx
            CaveMemory.Write_StrBytes("89 17");
            //mov eax,[_AxesY_CaveAddress + esi*4]
            CaveMemory.Write_StrBytes("8B 04 B5");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_AxesY_CaveAddress));

            //Inject it
            CaveMemory.InjectToOffset(_Axis_InjectionStruct, "Axis");
        }

        /// <summary>
        /// Replacing GetKeyboardState call, filling the buffer with our own values (see WINUSER.H for virtualkey codes)
        /// EAX has LpKeyState address
        /// </summary>
        private void SetHack_Buttons()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

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
            CaveMemory.Write_StrBytes("89 C7");
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
               
        #region No Crosshair Hack

        protected override void Apply_NoCrosshairMemoryHack()
        {
            //Removing the crosshair design at runtime by replacing the instruction :
            // - push    offset aSight   ; "sight"
            //by :
            // - push 00, Nop, Nop, Nop
            WriteBytes(GameProcess_ModuleBaseAddress + _NoCrosshair_Patch1_Offset, new byte[] { 0x6A, 0x00, 0x90, 0x90, 0x90 });
            WriteBytes(GameProcess_ModuleBaseAddress + _NoCrosshair_Patch2_Offset, new byte[] { 0x6A, 0x00, 0x90, 0x90, 0x90 });

            //Changing the Ammo display font by modifying float value :
            // - float1 : 0,33000001 -> 0,8
            // - float2 : 0,23 -> 0,5
            ReplaceReadOnlyString(new uint[] { _AmmoFont_Patch1_Offset }, new byte[] { 0xCD, 0xCC, 0x4C, 0x3F });
            ReplaceReadOnlyString(new uint[] { _AmmoFont_Patch2_Offset }, new byte[] { 0x00, 0x00, 0x00, 0x3F });

            //Force the game to read Ammo position calculated in the custom codecave
            //add esp, 8
            //push [_AmmoY_CaveAddress]
            //push [_AmmoY_CaveAddress]
            List<byte> bPatch = new List<byte>()
            {
                0x83,
                0xC4,
                0x08,
                0xFF,
                0x35
            };
            bPatch.AddRange(BitConverter.GetBytes(_AmmoDisplayY_CaveAddress));
            bPatch.AddRange(new byte[] { 0xFF, 0x35 });
            bPatch.AddRange(BitConverter.GetBytes(_AmmoDisplayX_CaveAddress));
            WriteBytes(GameProcess_ModuleBaseAddress + _AmmoPosition1_Patch_Offset, bPatch.ToArray());
            WriteBytes(GameProcess_ModuleBaseAddress + _AmmoPosition2_Patch_Offset, bPatch.ToArray());
            WriteBytes(GameProcess_ModuleBaseAddress + _AmmoPosition3_Patch_Offset, bPatch.ToArray());

            //Add codecave to calculate Ammo position dynamically during the game
            SetHack_CalculateAmmoPosition();

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        /// <summary>
        /// Based on EAX value (check P1 or P2), setting Ammo Text location (X) to the left or to the right of the screen
        /// Y position stays bottom for both players
        /// </summary>
        private void SetHack_CalculateAmmoPosition()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //sub esp,10
            CaveMemory.Write_StrBytes("83 EC 10");
            //movdqu [esp],xmm0
            CaveMemory.Write_StrBytes("F3 0F 7F 04 24");
            //sub esp,10
            CaveMemory.Write_StrBytes("83 EC 10");
            //movdqu [esp],xmm1
            CaveMemory.Write_StrBytes("F3 0F 7F 0C 24");
            //cmp eax,00
            CaveMemory.Write_StrBytes("83 F8 00");
            //jbe Player1
            CaveMemory.Write_StrBytes("76 17");
            //cvtsi2ss xmm0,[game.exe + _ScreenWidth_Offset]
            CaveMemory.Write_StrBytes("F3 0F 2A 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _ScreenWidth_Offset));
            //mov eax,00000028
            CaveMemory.Write_StrBytes("B8 28 00 00 00");
            //cvtsi2ss xmm1,eax
            CaveMemory.Write_StrBytes("F3 0F 2A C8");
            //subss xmm0,xmm1
            CaveMemory.Write_StrBytes("F3 0F 5C C1");
            //jmp Next
            CaveMemory.Write_StrBytes("EB 09");
            //Player 1:
            //mov eax,00000028
            CaveMemory.Write_StrBytes("B8 28 00 00 00");
            //cvtsi2ss xmm0,eax
            CaveMemory.Write_StrBytes("F3 0F 2A C0");
            //Next:
            //movss [_AmmoDisplayX_CaveAddress],xmm0
            CaveMemory.Write_StrBytes("F3 0F 11 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_AmmoDisplayX_CaveAddress));
            //cvtsi2ss xmm0,[game.exe + _ScreenHeight_Offset]
            CaveMemory.Write_StrBytes("F3 0F 2A 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _ScreenHeight_Offset));
            //mov eax,00000078
            CaveMemory.Write_StrBytes("B8 78 00 00 00");
            //cvtsi2ss xmm1,eax
            CaveMemory.Write_StrBytes("F3 0F 2A C8");
            //subss xmm0,xmm1
            CaveMemory.Write_StrBytes("F3 0F 5C C1");
            //movss [_AmmoDisplayY_CaveAddress],xmm0
            CaveMemory.Write_StrBytes("F3 0F 11 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_AmmoDisplayY_CaveAddress));
            //mov eax,92492493
            CaveMemory.Write_StrBytes("B8 93 24 49 92");
            //movdqu xmm1,[esp]
            CaveMemory.Write_StrBytes("F3 0F 6F 0C 24");
            //add esp,10
            CaveMemory.Write_StrBytes("83 C4 10");
            //movdqu xmm0,[esp]
            CaveMemory.Write_StrBytes("F3 0F 6F 04 24");
            //add esp,10
            CaveMemory.Write_StrBytes("83 C4 10");

            //Inject it
            CaveMemory.InjectToOffset(_AmmoPosition_InjectionStruct, "ammo position calculation");
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
                byte[] bufferX = BitConverter.GetBytes((Int16)X);
                byte[] bufferY = BitConverter.GetBytes((Int16)Y);

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
                    WriteByte(_LpKeyState_CaveAddress + 0x01 + PlayerIndex, 0x80);
                    //This is only for TEST MENU input test
                    WriteByte(GameProcess_ModuleBaseAddress + _JVS_Trigger_Offset + (PlayerIndex * 0x04), 0x01);
                }
                else if (e.ButtonName.Equals("START"))
                {
                    WriteByte(_LpKeyState_CaveAddress + 0x31 + PlayerIndex, 0x80);
                }
                else if (e.ButtonName.Equals("ACTION"))
                {
                    WriteByte(_LpKeyState_CaveAddress + 0x33 + PlayerIndex, 0x80);
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
                    WriteByte(_LpKeyState_CaveAddress + 0x01 + PlayerIndex, 0x00);
                    //This is only for TEST MENU input test
                    WriteByte(GameProcess_ModuleBaseAddress + _JVS_Trigger_Offset + (PlayerIndex * 0x04), 0x00);
                }
                else if (e.ButtonName.Equals("START"))
                {
                    WriteByte(_LpKeyState_CaveAddress + 0x31 + PlayerIndex, 0x00);
                }
                else if (e.ButtonName.Equals("ACTION"))
                {
                    WriteByte(_LpKeyState_CaveAddress + 0x33 + PlayerIndex, 0x00);
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
                new GameOutput(OutputId.P1_GunRecoil),
                new GameOutput(OutputId.P2_GunRecoil),
                new GameOutput(OutputId.P1_GunMotor),
                new GameOutput(OutputId.P2_GunMotor),
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
            int P1_RecoilState = ReadByte(GameProcess_ModuleBaseAddress + _Outputs_Offset) >> 5 & 0x01;
            int P1_RumbleState = ReadByte(GameProcess_ModuleBaseAddress + _Outputs_Offset + 2) >> 2 & 0x01;
            int P2_RecoilState = ReadByte(GameProcess_ModuleBaseAddress + _Outputs_Offset) >> 4 & 0x01;
            int P2_RumbleState = ReadByte(GameProcess_ModuleBaseAddress + _Outputs_Offset + 2) >> 1 & 0x01;

            //Orginal
            SetOutputValue(OutputId.P1_LmpStart, ReadByte(GameProcess_ModuleBaseAddress + _Outputs_Offset + 2) >> 6 & 0x01);
            SetOutputValue(OutputId.P2_LmpStart, ReadByte(GameProcess_ModuleBaseAddress + _Outputs_Offset + 2) >> 5 & 0x01);
            SetOutputValue(OutputId.P1_Lmp_R, ReadByte(GameProcess_ModuleBaseAddress + _Outputs_Offset + 2) >> 7 & 0x01);
            SetOutputValue(OutputId.P1_Lmp_G, ReadByte(GameProcess_ModuleBaseAddress + _Outputs_Offset + 2) >> 4 & 0x01);
            SetOutputValue(OutputId.P1_Lmp_B, ReadByte(GameProcess_ModuleBaseAddress + _Outputs_Offset + 2) >> 3 & 0x01);
            SetOutputValue(OutputId.P2_Lmp_R, ReadByte(GameProcess_ModuleBaseAddress + _Outputs_Offset + 1) >> 7 & 0x01);
            SetOutputValue(OutputId.P2_Lmp_G, ReadByte(GameProcess_ModuleBaseAddress + _Outputs_Offset + 1) >> 6 & 0x01);
            SetOutputValue(OutputId.P2_Lmp_B, ReadByte(GameProcess_ModuleBaseAddress + _Outputs_Offset + 1) >> 5 & 0x01);
            SetOutputValue(OutputId.P1_GunRecoil, P1_RecoilState);
            SetOutputValue(OutputId.P1_GunMotor, P1_RumbleState);
            SetOutputValue(OutputId.P2_GunRecoil, P2_RecoilState);
            SetOutputValue(OutputId.P2_GunMotor, P2_RumbleState);

            for (uint i = 0; i < _GameConfig.PlayerCount; i++)
            {
                _Ammo[i] = 0;
                _Life[i] = 0;

                //Player Status :
                //[0] : Inactive
                //[1] : In-Game
                //[2] : Continue Screen
                //[3] : Game Over
                int Player_Status = ReadByte(GameProcess_ModuleBaseAddress + _PlayersStatus_Offset + (i * 0x08));
                if (Player_Status == 1)
                {
                    _Ammo[i] = ReadInt32(GameProcess_ModuleBaseAddress + _PlayersAmmo_Offset + (i * _Player2AmmoIndex));
                    _Life[i] = (int)(100.0 * ReadFloat(GameProcess_ModuleBaseAddress + _PlayersLife_Offset + (i * 0x0C)));

                    if (_Life[i] < 0)
                        _Life[i] = 0;

                    if (_Life[i] < _LastLife[i])
                        SetOutputValue(OutputId.P1_Damaged + i, 1);
                }

                _LastLife[i] = _Life[i];

                SetOutputValue(OutputId.P1_Life + i, _Life[i]);
                SetOutputValue(OutputId.P1_Ammo + i, _Ammo[i]);
            }

            //Custom Recoil will simply be activated just like original Recoil
            SetOutputValue(OutputId.P1_CtmRecoil, P1_RecoilState);
            SetOutputValue(OutputId.P2_CtmRecoil, P2_RecoilState);

            //Credits
            SetOutputValue(OutputId.Credits, ReadByte(GameProcess_ModuleBaseAddress + _Credits_Offset));
        }

        #endregion
    }
}
