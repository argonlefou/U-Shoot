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
    public class Game_WndHod3pc : BaseGame32
    {
        protected override string GAMEDATA_FOLDER { get { return @"Windows\hod3pc"; } }
        protected override bool PATCH_PEB_DEBUG_FLAG { get { return false; } }

        private const string ARCADE_MOD_OPTION = "Arcade Mod";

        private const string HOD3_CUSTOM_LOCAL_FOLDER = "NVRAM";
        private const string HOD3_DAT_FILENAME = "HOD3.DAT";
        private const string HOD3_SAVE_FILENAME = "SAVE.DAT";

        //Custom Data Bank
        private UInt32 _ArcadeMod_DataBank_Address = 0;
        private enum DataBank_Offset
        {
            CreditsKeyPushed = 0,
            BlinkTickCount = 4,
            FrameTickCount = 8,
            CustomSpriteVisibility = 12,
            SkipIntro = 16,
            ShowIntro = 20,
            ShowReloadP1 = 24,
            ShowReloadP2 = 28,
            CustomDatFilePath = 50, //char[255]
            CustomSaveFilePath = 305, //char[255]
            CoinSoundFileName = 560,
        }

        #region LOADER MEMORY ADDRESSES                
        //Audio table for /fs/media sound is located at @5E56A8 :
        //4 Bytes => audi ID to use when calling the function 4A3D80
        //4 Bytes => memory address corresponding to the file path to load
        private UInt32 _SfxID_Reload = 0x0210FA9;
//        private UInt32 _SfxID_Reload02 = 0x220FA9;  //--> For future use if we need to play that sound after the first "RELOAD !" when player continue to push the trigger without ammo
        //Coin sound is not in the table, that's why we will use the start SFX entry (not used anymore, at the title screen)
        private UInt32 _SfxID_ToUseForCoin = 0x10BA9;   //start02.aif

        private UInt32 _EnterKeyPressed_Offset = 0x0003ECF48;
        private UInt32 _FlagFreeplay_Offset = 0x3B7E17;
        //private UInt32 _FlagDisplayScore_Offset = 0x3B8A78;
        private UInt32 _Credits_Offset = 0x003B7DD0;
        private UInt32 _CreditsToStart_Offset = 0x003B7E1C;
        private UInt32 _CreditsToContinue_Offset = 0x003B7E1D;
        private UInt32 _ContinueTimerP1_Offset = 0x003B807C;
        private UInt32 _ContinueTimerP2_Offset = 0x003B82A8;
        private UInt32 _ContinueTimer_Offset = 0x003B8924;
        private UInt32 _CurrentSceneID_Offset = 0x003B8B74;
        private UInt32 _NextSceneID_Offset = 0x003B8780;
        private UInt32 _Player1GameStatus_Offset = 0x003B8038;
        private UInt32 _Player2GameStatus_Offset = 0x003B8264;
        private UInt32 _Player1Ammo_Offset = 0x003B8090;
        private UInt32 _Player2Ammo_Offset = 0x003B82BC;
        private UInt32 _DrawFs2SpritesFunction_Offset = 0x000A5EE0;
        private UInt32 _DrawFsSpritesFunction_Offset = 0x000A5EA0;
        private UInt32 _GetLocalTimeFunction_Offset = 0x0003D710;   //Caled in HOD3.DAT default values
        private UInt32 _CurrentPath_Offset = 0x0059C5F4;
        private UInt32 _SoundPlayerHandle_Offset = 0x005D2E18;
        private UInt32 _FltConstant_16f_Offset = 0x0001C26BC;
        private UInt32 _FltConstant_58f_Offset = 0x0001C28C4;
        private UInt32 _FltConstant_10f_Offset = 0x0001C2718;
        private UInt32 _FltConstant_46f_Offset = 0x0001C2938;
        private UInt32 _AllowStartGame = 0x00366748;
        private UInt32 _StartGameFromByteJmpTable_Offset = 0x0008E134;

        // Base Address of Loaded sprites ID
        private UInt32 _SprMenuExtraSprites_Offset = 0x199C58;
        //Used in Menu :
        //+599C58	:	EXIT (English)
        //+599C64	:	EXIT (german)                   Attract "reload"             
        //+599C70	:	EXIT (french)
        //+599C7C	:	EXIT (spanish)                  Rankings
        //+599C88	:	EXIT (italian)                  Attract "Shells"
        //+599D60	:	Sega trademark

        //Used in Options :
        //+599C94	:	Save game data (english)	
        //+599CA0	:	Save game data (german)		
        //+599CAC	:	Save game data (french)		
        //+599CB8	:	Save game data (spanish)	
        //+599CC4	:	Save game data (italian)	
        //+599CD0	:	Save game data (japanese)		CREDITS	

        //+599CDC	:	Load game data (english)	
        //+599CE8	:	Load game data (german)			FREEPLAY	
        //+599CF4	:	Load game data (french)		
        //+599D00	:	Load game data (spanish)	
        //+599D0C	:	Load game data (italian)	
        //+599D18	:	Load game data (japanese)		PRESS START

        //+599D24	:	Saving Game (english)		
        //+599D30	:	Saving Game (german)			INSERT_COIN
        //+599C3C	:	Saving Game (french)		
        //+599D48	:	Saving Game (spanish)		
        //+599D54	:	Saving Game (italian)		

        //[+599D60	:	Sega trademark]

        //+599D6C	:	Unable to load (english)		0
        //+599D78	:	Unable to load (german)			1
        //+599D84	:	Unable to load (french)			2
        //+599D90	:	Unable to load (spanish)		3
        //+599D9C	:	Unable to load (italian)		4
        //+599DA8	:	Unable to load (japanese)		5

        //+599DB4	:	Crosshair tooltip (english)		6
        //+599DC0	:	Crosshair tooltip (german)		7
        //+599DCC	:	Crosshair tooltip (french)		8
        //+599DD8	:	Crosshair tooltip (spanish)		9
        //+599DE4	:	Crosshair tooltip (italian)		Title Trademark
        //+599DF0	:	Crosshair tooltip (japanese)	Title 2

        //+599DFC	:	END (english)
        //+599E08	:	END (german)
        //+599E14	:	END (french)
        //+599E20	:	END (spanish)
        //+599E2C	:	END (italian)
        private enum SprMenuExtraSprite
        {
            AttractReload = 0x0C,
            Rankings = 0x24,
            AttractShells = 0x30,
            Credits = 0x78,
            Freeplay = 0x90,
            PressStart = 0xC0,
            InsertCoin = 0xD8,
            Num0 = 0x114,
            //+0x0C for eaxh number untill 9            
            Logo = 0x198,
            Trademark = 0x18C
        }

        //For our custom INSERT COINS / PRESS START sprites, fixed values (screen is 640x480)
        //Origin : center of the sprite (so that different lenght/language are still centered)
        private const float _Players_UpperSpriteInfo_Y = 426.0f;
        private const float _Player1_SpriteInfo_X = 142.0f;
        private const float _Player2_SpriteInfo_X = 496.0f;
        //For our custom FREEPLAY / CREDITS sprites, fixed values (screen is 640x480)
        //Origin : center of the sprite (so that different lenght/language are still centered)
        private const float _Players_LowerSpriteInfo_Y = 444.0f;
        //FREEPLAY will be centered (only 1 sprite), but CREDITS and DIGITS need adjustments (and more according to the CREDITS lenght in language)
        private float _Players_LowerSpriteInfo_OffsetX1 = -15.0f; //CREDITS offset
        private float _Players_LowerSpriteInfo_OffsetX2 = 32.0f; //Digit#1 offset

        private UInt32 _IntroLogoChange_Offset1 = 0x00076383;
        private UInt32 _IntroLogoChange_Offset2 = 0x000763F9;
        private UInt32 _IntroLogoChange_Offset3 = 0x00076505;
        private UInt32 _AppDataFolderAndRegistry_SaveFile_Offset = 0x0003CF3F;
        private UInt32 _AppDataFolderAndRegistry_DatFile_Offset = 0x0003CB6F;
        private UInt32 _RegistryWritePlayed_Offset = 0x000B2E26;
        private UInt32 _RegistryCheckPlayed_Offset = 0x000B1E38;
        private UInt32 _RegistryReadPath = 0x000B1EB8;
        private UInt32 _FreeplayInit_Offset = 0x0003E0F1;
        private UInt32 _OverrideDatFile_CheckName_Offset = 0x0003CA26;
        private UInt32 _OverrideDatFile_ReadName_Offset = 0x0003CA42;
        private UInt32 _OverrideDatFile_WriteName_Offset = 0x0003D38D;
        private UInt32 _OverrideSaveFile_WriteName_Offset = 0x0003D621;
        private UInt32 _OverrideSaveFile_CheckName_Offset = 0x0003D4B0;
        private UInt32 _OverrideSaveFile_ReadName_Offset = 0x0003D4C8;
        private UInt32 _ReplaceTitleLogo_Offset = 0x000A8D3D;
        private UInt32 _DisableTimeAttackAttractMode_Offset = 0x000A9120;
        private UInt32 _DisableTimeAttackRankingDisplay_Offset = 0x0009B106;
        private UInt32 _EnableContinueScreen_Offset = 0x0003E549;
        private UInt32 _HideCrosshairs_Offset = 0x0008E989;
        private UInt32 _MaxLife_Offset = 0x0008E39E;
        private UInt32 _DisableEnterKeyUp_Offset = 0x000B2A52;
        private UInt32 _DisableEnterKeyDown_Offset = 0x000B29A2;
        private UInt32 _DisableSetFocus_Offset = 0x000B29C7;
        private UInt32 _DisableExitConfirm_Offset = 0x000B2AB2;
        private UInt32 _OverrideSprMenuExtrasSize_Offset = 0x00077737;
        private UInt32 _OverrideSprLogoSize_Offset = 0x00075F3F;
        private UInt32 _FastReload_Offset = 0x00204DCC;
        private UInt32 _DisableInGamePause_v1_Offset = 0x008B5A0;
        private UInt32 _DisableInGamePause_v2_Offset = 0x008C2B3;
        private UInt32 _StartGameFromTitle_Offset1 = 0x0003E4F2;
        private UInt32 _StartGameFromTitle_Offset2 = 0x0008D9A4;
        private UInt32 _StartGameFromTitleSwitchCase_Offset = 0x0008E01E;
        private UInt32 _NameEntryTitleCorrection_Offset = 0x00080705;
        private UInt32 _AutoSaveAfterEndGame_Offset = 0x0002E3A1;
        private UInt32 _DefaultRankingValues_Offset = 0x00206E48;

        private NopStruct32 _Nop_RegQuerryValueExA1 = new NopStruct32(0x000B25BA, 6);
        private NopStruct32 _Nop_RegQuerryValueExA2 = new NopStruct32(0x000B25EC, 6);
        private NopStruct32 _Nop_CreditsInit_1 = new NopStruct32(0x0003E660, 6);
        private NopStruct32 _Nop_CreditsInit_2 = new NopStruct32(0x0003E610, 2);
        private NopStruct32 _Nop_CreditsInit_3 = new NopStruct32(0x0003E640, 2);
        private NopStruct32 _Nop_CreditsInit_4 = new NopStruct32(0x0003E600, 7);
        private NopStruct32 _Nop_CreditsToStartAndContinueInit = new NopStruct32(0x0003E0E3, 10);    //CreditsToStart & CreditsToContinue
        private NopStruct32 _Nop_SfxPlayStart = new NopStruct32(0x0008E005, 16);
        private NopStruct32 _Nop_NoAutoReload_1 = new NopStruct32(0x0008DEDB, 3);
        private NopStruct32 _Nop_Arcade_Mode_Display = new NopStruct32(0x0008FD29, 2);
        private NopStruct32 _Nop_ConfirmExitGame = new NopStruct32(0x000B2AB2, 12);
        //private NopStruct32 _Nop_ResetScoreDisplayFlag = new NopStruct32(0x00077F75, 9);
        private NopStruct32 _Nop_ScoreDisplayFlagTest = new NopStruct32(0x0008F1C7, 2);

        private UInt32 _WndProcCredits_CalledFunctionOffset = 0x000A3D80;
        private InjectionStruct32 _ForceCoinSoundEffect_InjectionStruct = new InjectionStruct32(0x000A3FC8, 5);
        private InjectionStruct32 _OverideLoadingHod3Dat_InjectionStruct = new InjectionStruct32(0x0003CAE9, 6);
        private InjectionStruct32 _OverideDefaultHod3Dat1_InjectionStruct = new InjectionStruct32(0x0003D30D, 5);
        private InjectionStruct32 _OverideDefaultHod3Dat2_InjectionStruct = new InjectionStruct32(0x0003D331, 5);
        private InjectionStruct32 _OverideDefaultHod3Dat3_InjectionStruct = new InjectionStruct32(0x0003D355, 5);
        private InjectionStruct32 _ChangeTitleLogo_InjectionStruct = new InjectionStruct32(0x000A8D95, 5);
        private InjectionStruct32 _ReturnToMenu_InjectionStruct = new InjectionStruct32(0x00032BB0, 5);
        private InjectionStruct32 _InitLifeLimitToMax_InjectionStruct = new InjectionStruct32(0x0008DC08, 7);
        private InjectionStruct32 _LoopThreadVariousThings_InjectionStruct = new InjectionStruct32(0x0003E193, 6);
        private InjectionStruct32 _OverrideSprMenuExtras_InjectionStruct = new InjectionStruct32(0x00077759, 5);
        private InjectionStruct32 _OverrideSprLogo_InjectionStruct = new InjectionStruct32(0x00075F5f, 5);
        private InjectionStruct32 _OverrideRegistryRead_InjectionStruct = new InjectionStruct32(0x000B24E1, 6);
        private InjectionStruct32 _ReplaceFreeplaySpriteInGame_InjectionStruct = new InjectionStruct32(0x0003E212, 5);
        private InjectionStruct32 _ReplaceCreditsSpriteInGame_InjectionStruct = new InjectionStruct32(0x0003E25E, 5);
        private InjectionStruct32 _ReplaceCreditsDigit1SpriteInGame_InjectionStruct = new InjectionStruct32(0x0003E2C3, 5);
        private InjectionStruct32 _ReplaceCreditsDigit2SpriteInGame_InjectionStruct = new InjectionStruct32(0x0003E30B, 5);
        private InjectionStruct32 _ReplacePressStartButtonSpriteInGame_InjectionStruct = new InjectionStruct32(0x0003E1CC, 5);
        private InjectionStruct32 _StartGameFromTitle_InjectionStruct = new InjectionStruct32(0x0008DFED, 7);
        private InjectionStruct32 _AddReloadSfx1_InjectionStruct = new InjectionStruct32(0x0008DF1E, 5);
        private InjectionStruct32 _AddReloadSfx2_InjectionStruct = new InjectionStruct32(0x0008DED4, 7);
        private InjectionStruct32 _ReplaceAttractModeSprites_InjectionStruct = new InjectionStruct32(0x000A960A, 5);
        private InjectionStruct32 _RemoveEndgameSavePopup_InjectionStruct = new InjectionStruct32(0x0002BA56, 6);
        private InjectionStruct32 _ReplaceRankingTitleSprite_InjectionStruct = new InjectionStruct32(0x000A68CB, 5);
        private InjectionStruct32 _NoCursor_InjectionStruct = new InjectionStruct32(0x000B2747, 6);

        //Custom Data
        //Custom functions that will need to be addresses by Assembly codecaves to be called
        private UInt32 _CustomDrawSprite1_Function_Address = 0;
        private UInt32 _CustomDrawSprite2_Function_Address = 0;
        private UInt32 _CustomDrawSpriteAll_Function_Address = 0;
        #endregion

        #region INPUTS MEMORY ADDRESSES    
        private UInt32 _P1_X_Offset = 0x00367AA4;
        private UInt32 _P1_Y_Offset = 0x00367AA6;
        //private UInt32 _P2_X_Offset = 0x00367AD8;
        //private UInt32 _P2_Y_Offset = 0x00367ADA;
        private UInt32 _P1_Buttons_Offset = 0x0003ECF88;
        private UInt32 _P2_Buttons_Offset = 0x0003ED0F0;
        private UInt32 _Buttons_Patch_Offset = 0x000B4BB6;
        private NopStruct32 _Nop_X = new NopStruct32(0x0006BE70, 4);
        private NopStruct32 _Nop_Y = new NopStruct32(0x0006BE7B, 4);
        #endregion

        #region OUTPUTS MEMORY ADDRESSES
        //Not yet in cfg files (always the same for all releases ?)
        private UInt32 _P1_Ammo_Offset = 0x003B8090;
        private UInt32 _P1_Life_Offset = 0x003B8044;
        private UInt32 _P2_Ammo_Offset = 0x003B82BC;
        private UInt32 _P2_Life_Offset = 0x003B8270;
        private UInt32 _P1_Status_Offset = 0x003B8038;
        private UInt32 _P2_Status_Offset = 0x003B8264;
        #endregion

        private enum Hotd3_Scene
        {
            Sega_Logo = 0,
            MainMenu,
            MainMenu2,
            Sega_Logo2,
            IntroductionMovie,
            MainTitleScreen,
            AttractMode,
            ScoringScreen,
            ScoringScreen2
        }

        private Configurator _ArcadeModConfigurator;
        private string _RootGameFolder = string.Empty;

        private UInt32[] _PlayersButtons_Offsets = null;

        public Game_WndHod3pc(GameConfig Config) : base(Config)
        {
            _KnownMd5Prints.Add("hod3pc SEGA Windows", "4bf19dcb7f0182596d93f038189f2301");
            _KnownMd5Prints.Add("hod3pc RELOADED cracked", "3a4501d39bbb7271712421fb992ad37b");
            _KnownMd5Prints.Add("hod3pc REVELATION No-CD", "b8af47f16d5e43cddad8df0a6fdb46f5");
            _KnownMd5Prints.Add("hod3pc MYTH Release", "0228818e9412fc218fcd24bfd829a5a0");
            _KnownMd5Prints.Add("hod3pc TEST", "733da4e3cfe24b015e5f795811d481e6");
            _KnownMd5Prints.Add("hod3pc Unknown Release #1", "51dd72f83c0de5b27c0358ad11e2a036");
            _KnownMd5Prints.Add("hod3pc Unknown Release #2", "e4819dcf2105b85a7e7bc9dc66159f5c");

            //Root folder is 2 folders up from the actual EXE. This is where data will be saved instead of user settings
            _RootGameFolder = _GameConfig.TargetPath.Substring(0, _GameConfig.TargetPath.IndexOf(@"\exe\hod3pc.exe"));
            String CustomSaveFolder = Path.Combine(_RootGameFolder, HOD3_CUSTOM_LOCAL_FOLDER);
            if (!Directory.Exists(CustomSaveFolder))
            {
                try
                {
                    Directory.CreateDirectory(CustomSaveFolder);
                }
                catch (Exception Ex)
                {
                    Logger.WriteLog("Hotd3Arcade_Launcher() => Impossible to create the directory " + CustomSaveFolder + " : " + Ex.Message.ToString());
                }
            }
        }

        protected override void AttachDebuggerEvents()
        { }

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

                    double dMinX = -319.0;
                    double dMinY = -239.0;
                    double dMaxX = 320.0;
                    double dMaxY = 240.0;
                    double dRangeX = dMaxX - dMinX + 1;
                    double dRangeY = dMaxY - dMinY + 1;

                    PlayerController.Computed_X = Convert.ToInt32(Math.Round(dRangeX * PlayerController.Computed_X / TotalResX) - dRangeX / 2);
                    PlayerController.Computed_Y = Convert.ToInt32((Math.Round(dRangeY * PlayerController.Computed_Y / TotalResY) - dRangeY / 2) * -1);
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

        /// <summary>
        /// Overriding starndard procedure to force input hack if Arcade Mod is enabled
        /// </summary>
        protected override void Apply_MemoryHacks()
        {
            base.Apply_MemoryHacks();
            RaiseGameHookedEvent();
        }

        #region Loader Hack

        protected override void Apply_LoaderMemoryHack()
        {
            //Loader databank will be used as _ArcadeMod_DataBank_Address later
            Create_LoaderDataBank();

            if (_GameConfig.NoCursor)
            {
                if (CreateTransparentCursor())
                    SetHack_NoCursor();
            }

            if (_GameConfig.GetBoolOption(ARCADE_MOD_OPTION))
            {
                Apply_ArcadeMod_Hacks();
            }
            else
            {
                //Remove registry check "Played = 1" at start 
                WriteByte(GameProcess_ModuleBaseAddress + _RegistryCheckPlayed_Offset - 0x76, 0xEB);   //check if QuerryValue success
                WriteByte(GameProcess_ModuleBaseAddress + _RegistryCheckPlayed_Offset, 0xEB);          //check if value != 0
            }

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

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
        /// Basically apply all patches from my HOD3 PC arcade mod project : https://github.com/argonlefou/Hotd3pc_ArcadeMod
        /// </summary>
        private void Apply_ArcadeMod_Hacks()
        {
            string ConfigfolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Properties.Resources.DATA_PAYLOADS_FOLDER, _GameConfig.RomName);
            _ArcadeModConfigurator = new Configurator(ConfigfolderPath);
            if (!_ArcadeModConfigurator.ReadConf())
                Logger.WriteLog("Apply_ArcadeMod_Hacks() => No config file found");

            if (_ArcadeModConfigurator.Language == 3)
            {
                _Players_LowerSpriteInfo_OffsetX2 = _Players_LowerSpriteInfo_OffsetX2 + 10.0f;
            }

            //Create functions and storage for later
            _ArcadeMod_DataBank_Address = LoaderDatabank_Address;

            WriteByte(_ArcadeMod_DataBank_Address + (uint)DataBank_Offset.SkipIntro, 0x01);
            WriteByte(_ArcadeMod_DataBank_Address + (uint)DataBank_Offset.ShowIntro, 0x01);
            WriteByte(_ArcadeMod_DataBank_Address + (uint)DataBank_Offset.CreditsKeyPushed, 0x00);

            String Hod3DatPath = Path.Combine(_RootGameFolder, HOD3_CUSTOM_LOCAL_FOLDER, HOD3_DAT_FILENAME);
            WriteBytes(_ArcadeMod_DataBank_Address + (uint)DataBank_Offset.CustomDatFilePath, ASCIIEncoding.ASCII.GetBytes(Hod3DatPath));

            String Hod3SavePath = Path.Combine(_RootGameFolder, HOD3_CUSTOM_LOCAL_FOLDER, HOD3_SAVE_FILENAME);
            WriteBytes(_ArcadeMod_DataBank_Address + (uint)DataBank_Offset.CustomSaveFilePath, ASCIIEncoding.ASCII.GetBytes(Hod3SavePath));

            String CoinSoundFileName = @"..\media\coin002.aif";
            WriteBytes(_ArcadeMod_DataBank_Address + (uint)DataBank_Offset.CoinSoundFileName, Encoding.ASCII.GetBytes(CoinSoundFileName));

            //Small Signature :)
            WriteBytes(GameProcess_ModuleBaseAddress + _DefaultRankingValues_Offset, Properties.Resources.DefaultRanking);

            //Start Modding:    
            Mod_CreateDrawSprite1_Function(0.25f, 0.25f, 0x0A);
            Mod_CreateDrawSprite2_Function(0.25f, 0.25f, 0x0A);
            Mod_CreateDrawSpritesAll_Function();

            Mod_RegistryChecks();

            Mod_OverrideLoadingHod3DatFile();
            Mod_OverrideDefaultHod3DatFile(_OverideDefaultHod3Dat1_InjectionStruct);
            Mod_OverrideDefaultHod3DatFile(_OverideDefaultHod3Dat2_InjectionStruct);
            Mod_OverrideDefaultHod3DatFile(_OverideDefaultHod3Dat3_InjectionStruct);
            Mod_OverrideSaveFile();
            Mod_WndProcLoop();
            Mod_ForceCoinSoundEffect();
            Mod_CreditsHandling();
            Mod_InjectSprMenuExtras();
            Mod_InjectSprLogo(); 
            Mod_AddLotOfThingsIntoQuickThread();

            Mod_IntroductionLoop();
            Mod_TitleScreen();
            Mod_ReplaceAttractModeSprites();

            Mod_StartGameFromTitleScreen(); 
            Mod_DisableTimeAttackAttractMode();

            Mod_ReturnToMenu();

            if (_ArcadeModConfigurator.DisablePause == 1)
                Mod_DisableInGamePause_v2();

            if (_ArcadeModConfigurator.DisplayScore == 1)
                Mod_DisplayScore();

            Mod_SetMaxLife();
            Mod_FastReload();
            Mod_HideGuns();
            Mod_NoAutoReload();
            Mod_AddReloadEffects();
            Mod_ReplaceFreeplaySpriteInGame();
            Mod_ReplaceCreditsSpriteInGame();
            Mod_ReplaceCreditsDigit1SpriteInGame();
            Mod_ReplaceCreditsDigit2SpriteInGame();
            Mod_ReplacePushStartButtonSpriteInGame();
            Mod_ForceContinueDispay();
            Mod_NameEntryTitleCorrection();
            Mod_RemoveEndgameSavePopup();
            Mod_AutoSaveAfterEndGame();
            Mod_ReplaceRankingTitleSprite();
            Mod_ConfirmExitGame();
        }

        // Remove all registry functions : no writing nor reading
        // Original config option read in registry will be injected by the loader
        // Registry checks also removed
        private void Mod_RegistryChecks()
        {
            /*** Disable Registry writing, we don't need it ***/
            //43CF3F => Shunt whole procedure to create documents and settigns game folder and write register
            WriteBytes(GameProcess_ModuleBaseAddress + _AppDataFolderAndRegistry_SaveFile_Offset, new byte[] { 0xE9, 0x55, 0x03, 0x00, 0x00, 0x90 });
            //Just the registry write :
            //WriteByte(GameProcess_ModuleBaseAddress + 0x3CE9D, 0xEB);

            //43CF3F => Shunt whole procedure to create documents and settings game folder and write register
            WriteBytes(GameProcess_ModuleBaseAddress + _AppDataFolderAndRegistry_DatFile_Offset, new byte[] { 0xE9, 0x55, 0x03, 0x00, 0x00, 0x90 });
            //Just the registry :
            //WriteByte(GameProcess_ModuleBaseAddress + 0x3D26D, 0xEB);

            //Disable writing Played=0 at program exit
            WriteByte(GameProcess_ModuleBaseAddress + _RegistryWritePlayed_Offset, 0xEB);

            //Disable RegOpenKey error if registry not existing
            WriteBytes(GameProcess_ModuleBaseAddress + _RegistryCheckPlayed_Offset - 0xAC, new byte[] { 0x90, 0x90, 0x90, 0x90, 0x90, 0x90 });

            //Remove registry check "Played = 1" at start 
            WriteByte(GameProcess_ModuleBaseAddress + _RegistryCheckPlayed_Offset - 0x76, 0xEB);   //check if QuerryValue success
            WriteByte(GameProcess_ModuleBaseAddress + _RegistryCheckPlayed_Offset, 0xEB);          //check if value != 0

            //Remove the call for RegQueryValue do get path, and use custom data for the call to SetCurrentDirectory()
            WriteBytes(GameProcess_ModuleBaseAddress + _RegistryReadPath, new byte[] { 0x83, 0xC4, 0x14 });            
            WriteBytes(GameProcess_ModuleBaseAddress + _CurrentPath_Offset, ASCIIEncoding.ASCII.GetBytes(_RootGameFolder));

            //Replacing the options values from registry by our own
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);
            int offset = 0x250;
            //Forcing KEYBOARD controls for both players with U-Shoot :
            _ArcadeModConfigurator.RegValues[0] = 0;
            _ArcadeModConfigurator.RegValues[1] = 0;
            for (int i = 0; i < _ArcadeModConfigurator.RegValues.Length; i++)
            {
                CaveMemory.Write_StrBytes("C7 86");
                if (i == 4 || i == 30)
                    offset += 0x08;

                //For that release, it seems settinf Framerate > 20 Fps makes the game to crash on start
                if (i == 32 && _TargetProcess_Md5Hash.Equals(_KnownMd5Prints["hod3pc Unknown Release #2"]) && _ArcadeModConfigurator.RegValues[i] < 2)
                    _ArcadeModConfigurator.RegValues[i] = 2;

                CaveMemory.Write_Bytes(BitConverter.GetBytes(offset));
                CaveMemory.Write_Bytes(BitConverter.GetBytes(_ArcadeModConfigurator.RegValues[i]));
                offset += 0x04;
            }
            //mov eax,[esi+000002DC]
            CaveMemory.Write_StrBytes("8B 86 DC 02 00 00");

            //Inject it
            CaveMemory.InjectToOffset(_OverrideRegistryRead_InjectionStruct, "Registry Check");
           
            //Disable the last 2 readings, already set by the previous codecave
            SetNops(GameProcess_ModuleBaseAddress, _Nop_RegQuerryValueExA1);
            SetNops(GameProcess_ModuleBaseAddress, _Nop_RegQuerryValueExA2);
        }

        //Modifying the read buffer from the DAT file
        //If need be, the data is finally saved in fixed memory location, pointer in 0x9D3008
        private void Mod_OverrideLoadingHod3DatFile()
        {
            //Force the gameto use our own DAT file, while letting the original path in registry
            WriteBytes(GameProcess_ModuleBaseAddress + _OverrideDatFile_CheckName_Offset + 1, BitConverter.GetBytes(_ArcadeMod_DataBank_Address + (uint)DataBank_Offset.CustomDatFilePath));
            WriteBytes(GameProcess_ModuleBaseAddress + _OverrideDatFile_ReadName_Offset + 1, BitConverter.GetBytes(_ArcadeMod_DataBank_Address + (uint)DataBank_Offset.CustomDatFilePath));
            WriteBytes(GameProcess_ModuleBaseAddress + _OverrideDatFile_WriteName_Offset + 1, BitConverter.GetBytes(_ArcadeMod_DataBank_Address + (uint)DataBank_Offset.CustomDatFilePath));

            //Freeplay WORD in memory is set by confing only before a game is starting
            //To make our custom credits handling work at title, we need to set it when it's initialized 
            if (_ArcadeModConfigurator.Freeplay == 1)
            {
                WriteBytes(GameProcess_ModuleBaseAddress + _FreeplayInit_Offset, new byte[] { 0xC6, 0x05, 0x17, 0x7E, 0x7B, 0x00, 0x01, 0x88, 0x0D, 0x13, 0x7E, 0x7B, 0x00, 0x89, 0x08, 0x5F, 0xC2, 0x04, 0x00 });
            }

            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //mov [edx+000000F0], BGM Volume
            CaveMemory.Write_StrBytes("C6 82 F0 00 00 00");
            CaveMemory.Write_Byte(_ArcadeModConfigurator.VolumeBGM);
            //mov [edx+000000F1], SFX Volume
            CaveMemory.Write_StrBytes("C6 82 F1 00 00 00");
            CaveMemory.Write_Byte(_ArcadeModConfigurator.VolumeSFX);
            //mov [edx+000000F2], Vopice Volume
            CaveMemory.Write_StrBytes("C6 82 F2 00 00 00");
            CaveMemory.Write_Byte(_ArcadeModConfigurator.VolumeVCE);

            //mov [edx+00000184], Difficulty
            CaveMemory.Write_StrBytes("C6 82 84 01 00 00");
            CaveMemory.Write_Byte(_ArcadeModConfigurator.GameDifficulty);
            //mov [edx+00000186], InitialLifes
            CaveMemory.Write_StrBytes("C6 82 86 01 00 00");
            CaveMemory.Write_Byte(_ArcadeModConfigurator.InitialLife);
            if (_ArcadeModConfigurator.Freeplay == 1)
            {
                //mov [edx+00000187], Freeplay
                CaveMemory.Write_StrBytes("C6 82 87 01 00 00 FF");
            }
            else
            {
                //mov [edx+00000187], 01
                CaveMemory.Write_StrBytes("C6 82 87 01 00 00 05");
            }
            //mov [edx+00000188], Violence
            CaveMemory.Write_StrBytes("C6 82 88 01 00 00");
            CaveMemory.Write_Byte(_ArcadeModConfigurator.Violence);
            //mov [edx+00000189], BloodColor
            CaveMemory.Write_StrBytes("C6 82 89 01 00 00");
            CaveMemory.Write_Byte(_ArcadeModConfigurator.BloodColor);
            CaveMemory.Write_StrBytes("C6 82 8A 01 00 00");
            CaveMemory.Write_Byte(_ArcadeModConfigurator.BloodColor);
            //mov [edx+00000190], Crosshairdesign
            CaveMemory.Write_StrBytes("C6 82 90 01 00 00");
            CaveMemory.Write_Byte(_ArcadeModConfigurator.CrosshairDesign);
            //mov [edx+00000191], CrosshairSpeed
            CaveMemory.Write_StrBytes("C6 82 91 01 00 00");
            CaveMemory.Write_Byte(_ArcadeModConfigurator.CrosshairSpeed);
            // mov [edx+18],si
            CaveMemory.Write_StrBytes("66 89 72 18");
            //ebx,ebx
            CaveMemory.Write_StrBytes("85 DB");

            //Inject it
            CaveMemory.InjectToOffset(_OverideLoadingHod3Dat_InjectionStruct, "DAT Loading override");
        }

        //If no DAT file is found, the game is loading default values
        //We will put our own inside this part
        //The function is called by different places, so we will modify our values before the return is called
        //There are 3 return in this function...so 3 codecave do do
        private void Mod_OverrideDefaultHod3DatFile(InjectionStruct32 MyInjection)
        {
            //Freeplay WORD in memory is set by confing only before a game is starting
            //To make our custom credits handling work at title, we need to set it when it's initialized 
            if (_ArcadeModConfigurator.Freeplay == 1)
            {
                WriteBytes(GameProcess_ModuleBaseAddress + _FreeplayInit_Offset, new byte[] { 0xC6, 0x05, 0x17, 0x7E, 0x7B, 0x00, 0x01, 0x88, 0x0D, 0x13, 0x7E, 0x7B, 0x00, 0x89, 0x08, 0x5F, 0xC2, 0x04, 0x00 });
            }

            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //Call GeTLocalTime Sub
            CaveMemory.Write_call(GameProcess_ModuleBaseAddress + _GetLocalTimeFunction_Offset);
            //mov [edi+000000F0], BGM Volume
            CaveMemory.Write_StrBytes("C6 87 F0 00 00 00");
            CaveMemory.Write_Byte(_ArcadeModConfigurator.VolumeBGM);
            //mov [edi+000000F1], SFX Volume
            CaveMemory.Write_StrBytes("C6 87 F1 00 00 00");
            CaveMemory.Write_Byte(_ArcadeModConfigurator.VolumeSFX);
            //mov [edi+000000F2], Vopice Volume
            CaveMemory.Write_StrBytes("C6 87 F2 00 00 00");
            CaveMemory.Write_Byte(_ArcadeModConfigurator.VolumeVCE);

            //mov [edi+00000184], Difficulty
            CaveMemory.Write_StrBytes("C6 87 84 01 00 00");
            CaveMemory.Write_Byte(_ArcadeModConfigurator.GameDifficulty);
            //mov [edi+00000186], InitialLifes
            CaveMemory.Write_StrBytes("C6 87 86 01 00 00");
            CaveMemory.Write_Byte(_ArcadeModConfigurator.InitialLife);
            if (_ArcadeModConfigurator.Freeplay == 1)
            {
                //mov [edi+00000187], Freeplay
                CaveMemory.Write_StrBytes("C6 87 87 01 00 00 FF");
            }
            //mov [edi+00000188], Violence
            CaveMemory.Write_StrBytes("C6 87 88 01 00 00");
            CaveMemory.Write_Byte(_ArcadeModConfigurator.Violence);
            //mov [edi+00000189], BloodColor
            CaveMemory.Write_StrBytes("C6 87 89 01 00 00");
            CaveMemory.Write_Byte(_ArcadeModConfigurator.BloodColor);
            CaveMemory.Write_StrBytes("C6 87 8A 01 00 00");
            CaveMemory.Write_Byte(_ArcadeModConfigurator.BloodColor);
            //mov [edi+00000190], Crosshairdesign
            CaveMemory.Write_StrBytes("C6 87 90 01 00 00");
            CaveMemory.Write_Byte(_ArcadeModConfigurator.CrosshairDesign);
            //mov [edi+00000191], CrosshairSpeed
            CaveMemory.Write_StrBytes("C6 87 91 01 00 00");
            CaveMemory.Write_Byte(_ArcadeModConfigurator.CrosshairSpeed);

            //Inject it
            CaveMemory.InjectToOffset(MyInjection, "Override Default Hod3Dat file");
        }

        //Instead of saving to the file from the game computed filename, directly putting our own path before CreateFileA call
        //Same thing for the "check if exists" call and when the game wants to read
        private void Mod_OverrideSaveFile()
        {
            WriteBytes(GameProcess_ModuleBaseAddress + _OverrideSaveFile_WriteName_Offset + 1, BitConverter.GetBytes(_ArcadeMod_DataBank_Address + (uint)DataBank_Offset.CustomSaveFilePath));
            WriteBytes(GameProcess_ModuleBaseAddress + _OverrideSaveFile_CheckName_Offset + 1, BitConverter.GetBytes(_ArcadeMod_DataBank_Address + (uint)DataBank_Offset.CustomSaveFilePath));
            WriteBytes(GameProcess_ModuleBaseAddress + _OverrideSaveFile_ReadName_Offset + 1, BitConverter.GetBytes(_ArcadeMod_DataBank_Address + (uint)DataBank_Offset.CustomSaveFilePath));
        }

        //Keyboard keys handling (disabling [RETURN]
        //SetFocus handling (remove pause when lost focus)
        private void Mod_WndProcLoop()
        {
            //Disabling [RETURN] keyboard key
            WriteByte(GameProcess_ModuleBaseAddress + _DisableEnterKeyUp_Offset, 0xEB);
            WriteByte(GameProcess_ModuleBaseAddress + _DisableEnterKeyDown_Offset, 0xEB);

            //Disabling Lost focus = pause
            WriteByte(GameProcess_ModuleBaseAddress + _DisableSetFocus_Offset, 0xEB);
        }

        //Change the sound effect file played when using the Credits button
        //The game does dot allow to play all sounf files on disk, so we choose to play the start sound effect ID
        private void Mod_ForceCoinSoundEffect()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //push C8
            CaveMemory.Write_StrBytes("68 C8 00 00 00");
            //cmp ebp, _SfxID_ToUseForCoin
            CaveMemory.Write_StrBytes("81 FD");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_SfxID_ToUseForCoin));
            //jne NextCheck
            CaveMemory.Write_StrBytes("75 05");
            //mov ebx, DataBank.CoinSoundFile
            CaveMemory.Write_StrBytes("BB");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_ArcadeMod_DataBank_Address + (uint)DataBank_Offset.CoinSoundFileName));

            //Inject it
            CaveMemory.InjectToOffset(_ForceCoinSoundEffect_InjectionStruct, "Mod_ForceCoinSoundEffect()");            
        }

        //Replace the original spr_menuextras.zip by the one we have in memory, at loading time
        //First step is to change the malloc size called to read the file
        //Second step is to copy our own data in the malloc space reserved
        //To acces the sprites, use following EDI values when calling Sub_4A5AC0() :
        //0x00599E38 = SEGA.dds
        //0x00599E44 = SEGA_WOW.dds
        private void Mod_InjectSprLogo()
        {
            string SprLogoFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Properties.Resources.DATA_PAYLOADS_FOLDER, _GameConfig.RomName, "spr_logo.zip");
            if (File.Exists(SprLogoFilePath))
            {
                try
                {
                    //byte[] b_MyFile = Properties.Resources.spr_logo;
                    byte[] b_MyFile = File.ReadAllBytes(SprLogoFilePath);

                    //Store zip file in game memory 
                    Codecave32 SprMemoryDump = new Codecave32(_Process, GameProcess_ModuleBaseAddress, (uint)b_MyFile.Length);
                    SprMemoryDump.Write_Bytes(b_MyFile);

                    //mov eax, Size of new file instead of seeking the original
                    WriteByte(GameProcess_ModuleBaseAddress + _OverrideSprLogoSize_Offset, 0xB8);
                    WriteBytes(GameProcess_ModuleBaseAddress + _OverrideSprLogoSize_Offset + 1, BitConverter.GetBytes((UInt32)(b_MyFile.Length)));

                    //Replacing the fread function call, instead we fill the memory directly
                    Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

                    //push edi
                    CaveMemory.Write_StrBytes("57");
                    //push esi
                    CaveMemory.Write_StrBytes("56");
                    //push ecx
                    CaveMemory.Write_StrBytes("51");
                    //mov esi, ZipMemoryAddress
                    CaveMemory.Write_StrBytes("BE");
                    CaveMemory.Write_Bytes(BitConverter.GetBytes(SprMemoryDump.CaveAddress));
                    //mov edi, ebx
                    CaveMemory.Write_StrBytes("8B FB");
                    //mov ecx, ZipMemoryAddress.Size
                    CaveMemory.Write_StrBytes("B9");
                    CaveMemory.Write_Bytes(BitConverter.GetBytes((UInt32)b_MyFile.Length));
                    //rep movsb
                    CaveMemory.Write_StrBytes("F3 A4");
                    //pop edi
                    CaveMemory.Write_StrBytes("59");
                    //pop esi
                    CaveMemory.Write_StrBytes("5E");
                    //pop ecx
                    CaveMemory.Write_StrBytes("5F");

                    //Inject it
                    CaveMemory.InjectToOffset(_OverrideSprLogo_InjectionStruct, "Mod_InjectSprTitleExtras()");
                }
                catch (Exception Ex)
                {
                    Logger.WriteLog("Mod_InjectSprLogo() Error :" + Ex.Message.ToString());
                }
            }
            else
            {
                Logger.WriteLog("Mod_InjectSprLogo() file not found :" + SprLogoFilePath);
            }
        }

        //Replace the original spr_menuextras.zip by the one we have in memory, at loading time
        //First step is to change the malloc size called to read the file
        //Second step is to copy our own data in the malloc space reserved
        //Menu is skipped so we can use these sprites without any risks
        //To acces the sprites, use following EDI values when calling Sub_4A5AC0() :
        //599DB4 = crosshair_e.dds
        //599DC0 = crosshair_g.dds
        //599DCC = crosshair_f.dds
        //599DD8 = crosshair_s.dds
        //599DE4 = crosshair_i.dds
        //599DF0 = crosshair_j.dds
        //599DFC = end.dds
        //etc...
        private void Mod_InjectSprMenuExtras()
        {
            /*byte[] b_MyFile = Properties.Resources.spr_menuextras_en;

            //OR we can check for language setting and load the corresponding spr_menuextra from Resource file.
            //BUT this will cause issues with sprites spacing as each language has different size
            switch (_GameConfigurator.Language)
            {
                case 0: b_MyFile = Properties.Resources.spr_menuextras_en; break;
                case 1: b_MyFile = Properties.Resources.spr_menuextras_fr; break;
                case 2: b_MyFile = Properties.Resources.spr_menuextras_it; break;
                case 3: b_MyFile = Properties.Resources.spr_menuextras_es; break;
                default: b_MyFile = Properties.Resources.spr_menuextras_en; break;
            }*/

            string SprMenuExtrasFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Properties.Resources.DATA_PAYLOADS_FOLDER, _GameConfig.RomName);
            //We can check for language setting and load the corresponding spr_menuextra from Resource file.
            //BUT this will cause issues with sprites spacing as each language has different size
            switch (_ArcadeModConfigurator.Language)
            {
                case 0: SprMenuExtrasFilePath = Path.Combine(SprMenuExtrasFilePath, "spr_menuextras_en.zip"); break;
                case 1: SprMenuExtrasFilePath = Path.Combine(SprMenuExtrasFilePath, "spr_menuextras_fr.zip"); break;
                case 2: SprMenuExtrasFilePath = Path.Combine(SprMenuExtrasFilePath, "spr_menuextras_it.zip"); break;
                case 3: SprMenuExtrasFilePath = Path.Combine(SprMenuExtrasFilePath, "spr_menuextras_es.zip"); break;
                default: SprMenuExtrasFilePath = Path.Combine(SprMenuExtrasFilePath, "spr_menuextras_en.zip"); break;
            }

            if (File.Exists(SprMenuExtrasFilePath))
            {
                try
                {
                    byte[] b_MyFile = File.ReadAllBytes(SprMenuExtrasFilePath);

                    //Store zip file in game memory 
                    Codecave32 SprMemoryDump = new Codecave32(_Process, GameProcess_ModuleBaseAddress, (uint)b_MyFile.Length);
                    SprMemoryDump.Write_Bytes(b_MyFile);

                    //mov eax, Size of new file instead of seeking the original
                    WriteByte(GameProcess_ModuleBaseAddress + _OverrideSprMenuExtrasSize_Offset, 0xB8);
                    WriteBytes(GameProcess_ModuleBaseAddress + _OverrideSprMenuExtrasSize_Offset + 1, BitConverter.GetBytes((UInt32)b_MyFile.Length));

                    //Replacing the fread function call, instead we fill the memory directly
                    Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

                    //push edi
                    CaveMemory.Write_StrBytes("57");
                    //push esi
                    CaveMemory.Write_StrBytes("56");
                    //push ecx
                    CaveMemory.Write_StrBytes("51");
                    //mov esi, ZipMemoryAddress
                    CaveMemory.Write_StrBytes("BE");
                    CaveMemory.Write_Bytes(BitConverter.GetBytes(SprMemoryDump.CaveAddress));
                    //mov edi, ebx
                    CaveMemory.Write_StrBytes("8B FB");
                    //mov ecx, ZipMemoryAddress.Size
                    CaveMemory.Write_StrBytes("B9");
                    CaveMemory.Write_Bytes(BitConverter.GetBytes((UInt32)b_MyFile.Length));
                    //rep movsb
                    CaveMemory.Write_StrBytes("F3 A4");
                    //pop edi
                    CaveMemory.Write_StrBytes("59");
                    //pop esi
                    CaveMemory.Write_StrBytes("5E");
                    //pop ecx
                    CaveMemory.Write_StrBytes("5F");

                    //Inject it
                    CaveMemory.InjectToOffset(_OverrideSprMenuExtras_InjectionStruct, "Mod_InjectSprMenuExtras()");
                }
                catch (Exception Ex)
                {
                    Logger.WriteLog("Mod_InjectSprMenuExtras() Error :" + Ex.Message.ToString());
                }
            }
            else
            {
                Logger.WriteLog("Mod_InjectSprMenuExtras() file not found :" + SprMenuExtrasFilePath);
            }
        }

        //Draw PRESS START BUTTON sprite (PRESS START or INSERT COIN(S))
        //X and Y values passed by Stack by previous calling functions   
        private void Mod_CreateDrawSprite1_Function(float fScaleX, float fScaleY, byte bTransform)
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);
            
            //cmp [Databank_CustomSpriteVisibility], 0
            CaveMemory.Write_StrBytes("83 3D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_ArcadeMod_DataBank_Address + (uint)DataBank_Offset.CustomSpriteVisibility));
            CaveMemory.Write_StrBytes("00");
            //je Exit
            CaveMemory.Write_StrBytes("74 4E");
            //push eax
            CaveMemory.Write_StrBytes("50");
            //push edi
            CaveMemory.Write_StrBytes("57");
            //cmp [FREEPLAY], 1
            CaveMemory.Write_StrBytes("83 3D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _FlagFreeplay_Offset));
            CaveMemory.Write_StrBytes("01");
            //je PRESSSTART
            CaveMemory.Write_StrBytes("74 14");
            //mov al, [CreditsToStart]
            CaveMemory.Write_StrBytes("A0");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _CreditsToStart_Offset));
            //cmp [Credits], eax
            CaveMemory.Write_StrBytes("39 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _Credits_Offset));
            //jge PRESSSTART
            CaveMemory.Write_StrBytes("7D 07");
            //INSERTCOIN :
            //mov edi, ID_SPRITE_INSERTCOIN
            CaveMemory.Write_StrBytes("BF");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _SprMenuExtraSprites_Offset + (uint)SprMenuExtraSprite.InsertCoin));
            //jmp draw_sprite
            CaveMemory.Write_StrBytes("EB 05");
            //PRESS START:
            //mov edi, ID_SPRITE_PRESSSTART
            CaveMemory.Write_StrBytes("BF");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _SprMenuExtraSprites_Offset + (uint)SprMenuExtraSprite.PressStart));
            //push 3F800000
            CaveMemory.Write_StrBytes("68 00 00 80 3F");
            //push Transform
            CaveMemory.Write_StrBytes("6A");
            CaveMemory.Write_Bytes(new byte[] { bTransform });
            //push 0
            CaveMemory.Write_StrBytes("6A 00");
            //push ScaleY
            CaveMemory.Write_StrBytes("68");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(fScaleX));
            //push ScaleX
            CaveMemory.Write_StrBytes("68");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(fScaleY));
            //push 38D1B717
            CaveMemory.Write_StrBytes("68 17 B7 D1 38");
            //push [esp+2C] = Y
            CaveMemory.Write_StrBytes("FF 74 24 2C");
            //push [ESP+2C] = X
            CaveMemory.Write_StrBytes("FF 74 24 2C");
            //call 4A5EE0
            CaveMemory.Write_call(GameProcess_ModuleBaseAddress + _DrawFs2SpritesFunction_Offset);
            //add esp, 20
            CaveMemory.Write_StrBytes("83 C4 20");
            //pop edi
            CaveMemory.Write_StrBytes("5F");
            //pop eax
            CaveMemory.Write_StrBytes("58");
            //ret
            CaveMemory.Write_StrBytes("C3");

            _CustomDrawSprite1_Function_Address = CaveMemory.CaveAddress;
            Logger.WriteLog("Mod_TitleScreen() => Mod_CreateDrawSprite1_Function at : 0x" + CaveMemory.CaveAddress.ToString("X8"));
        }

        //Draw CREDITS sprite or FREEPLAY
        //X and Y values passed by Stack by previous calling functions               
        private void Mod_CreateDrawSprite2_Function(float fScaleX, float fScaleY, byte bTransform)
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);
            
            //push edi
            CaveMemory.Write_StrBytes("57");
            //cmp [FREEPLAY], 0
            CaveMemory.Write_StrBytes("83 3D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _FlagFreeplay_Offset));
            CaveMemory.Write_StrBytes("00");
            //je CREDITS
            CaveMemory.Write_StrBytes("74 0A");
            //FREEPLAY:
            //mov edi, ID_SPRITE_FREEPLAY
            CaveMemory.Write_StrBytes("BF");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _SprMenuExtraSprites_Offset + (uint)SprMenuExtraSprite.Freeplay));
            //jmp draw_freeplay
            CaveMemory.Write_StrBytes("E9 92 00 00 00");

            //CREDITS:
            //mov edi, CRREDITS
            CaveMemory.Write_StrBytes("BF");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _SprMenuExtraSprites_Offset + (uint)SprMenuExtraSprite.Credits));
            //push 3F800000
            CaveMemory.Write_StrBytes("68 00 00 80 3F");
            //push Transform
            CaveMemory.Write_StrBytes("6A");
            CaveMemory.Write_Bytes(new byte[] { bTransform });
            //push 0
            CaveMemory.Write_StrBytes("6A 00");
            //push ScaleY
            CaveMemory.Write_StrBytes("68");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(fScaleX));
            //push ScaleX
            CaveMemory.Write_StrBytes("68");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(fScaleY));
            //push 38D1B717
            CaveMemory.Write_StrBytes("68 17 B7 D1 38");
            //push [esp+30] = Y
            CaveMemory.Write_StrBytes("FF 74 24 30");
            //push [ESP+30] = X
            CaveMemory.Write_StrBytes("FF 74 24 30");
            //fld dword ptr [esp]
            CaveMemory.Write_StrBytes("D9 04 24");
            //fsub dword ptr [flt_16.0f]
            CaveMemory.Write_StrBytes("D8 25");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _FltConstant_16f_Offset));
            //fstp dword ptr [esp]
            CaveMemory.Write_StrBytes("D9 1C 24");

            //call 4A5EE0
            CaveMemory.Write_call(GameProcess_ModuleBaseAddress + _DrawFs2SpritesFunction_Offset);

            //DIGIT 0:
            //fld dword ptr [esp]
            CaveMemory.Write_StrBytes("D9 04 24");
            //fadd dword ptr [flt_46.0f]
            CaveMemory.Write_StrBytes("D8 05");
            if (_ArcadeModConfigurator.Language == 3)
                CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _FltConstant_58f_Offset));
            else
                CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _FltConstant_46f_Offset));
            //fstp dword ptr [esp]
            CaveMemory.Write_StrBytes("D9 1C 24");
            //mov eax,[CREDITS]
            CaveMemory.Write_StrBytes("A1");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _Credits_Offset));
            //mov ebx,0000000A
            CaveMemory.Write_StrBytes("BB 0A 00 00 00");
            //div bl
            CaveMemory.Write_StrBytes("F6 F3");
            //movzx eax, al
            CaveMemory.Write_StrBytes("0F B6 C0");
            //mov bl,0C
            CaveMemory.Write_StrBytes("B3 0C");
            //mul bl
            CaveMemory.Write_StrBytes("F6 E3");
            //movzx edi,al
            CaveMemory.Write_StrBytes("0F B6 F8");
            //add edi, SPRITES_ADD
            CaveMemory.Write_StrBytes("81 C7");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _SprMenuExtraSprites_Offset + (uint)SprMenuExtraSprite.Num0));
            //call 4A5EE0
            CaveMemory.Write_call(GameProcess_ModuleBaseAddress + _DrawFs2SpritesFunction_Offset);

            //DIGIT 1
            //fld dword ptr [esp]
            CaveMemory.Write_StrBytes("D9 04 24");
            //fadd dword ptr [flt_10.0f]
            CaveMemory.Write_StrBytes("D8 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _FltConstant_10f_Offset));
            //fstp dword ptr [esp]
            CaveMemory.Write_StrBytes("D9 1C 24");
            //mov eax,[CREDITS]
            CaveMemory.Write_StrBytes("A1");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _Credits_Offset));
            //mov ebx,0000000A
            CaveMemory.Write_StrBytes("BB 0A 00 00 00");
            //div bl
            CaveMemory.Write_StrBytes("F6 F3");
            //shr eax, 8
            CaveMemory.Write_StrBytes("C1 E8 08");
            //mov bl,0C
            CaveMemory.Write_StrBytes("B3 0C");
            //mul bl
            CaveMemory.Write_StrBytes("F6 E3");
            //movzx edi,al
            CaveMemory.Write_StrBytes("0F B6 F8");
            //add edi, SPRITES_ADD
            CaveMemory.Write_StrBytes("81 C7");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _SprMenuExtraSprites_Offset + (uint)SprMenuExtraSprite.Num0));
            //call 4A5EE0
            CaveMemory.Write_call(GameProcess_ModuleBaseAddress + _DrawFs2SpritesFunction_Offset);
            //jmp Exit
            CaveMemory.Write_StrBytes("EB 20");

            //Draw Freeplay :
            //push 3F800000
            CaveMemory.Write_StrBytes("68 00 00 80 3F");
            //push Transform
            CaveMemory.Write_StrBytes("6A");
            CaveMemory.Write_Bytes(new byte[] { bTransform });
            //push 0
            CaveMemory.Write_StrBytes("6A 00");
            //push ScaleY
            CaveMemory.Write_StrBytes("68");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(fScaleX));
            //push ScaleX
            CaveMemory.Write_StrBytes("68");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(fScaleY));
            //push 38D1B717
            CaveMemory.Write_StrBytes("68 17 B7 D1 38");
            //push [esp+30] = Y
            CaveMemory.Write_StrBytes("FF 74 24 30");
            //push [ESP+30] = X
            CaveMemory.Write_StrBytes("FF 74 24 30");
            //call 4A5EE0
            CaveMemory.Write_call(GameProcess_ModuleBaseAddress + _DrawFs2SpritesFunction_Offset);
            //add esp, 20
            CaveMemory.Write_StrBytes("83 C4 20");
            //pop edi
            CaveMemory.Write_StrBytes("5F");
            //ret
            CaveMemory.Write_StrBytes("C3");

            _CustomDrawSprite2_Function_Address = CaveMemory.CaveAddress;
            Logger.WriteLog("Mod_TitleScreen() => Mod_CreateDrawSprite2_Function at : 0x" + CaveMemory.CaveAddress.ToString("X8"));
        }

        //Call both sprites functions to draw the whole thing
        //X and Y coordinates are in the stack as arguments
        private void Mod_CreateDrawSpritesAll_Function()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);
            
            //Call CustomDrawSpriteFunction_1            
            CaveMemory.Write_call(_CustomDrawSprite1_Function_Address);
            //Call CustomDrawSpriteFunction_2
            CaveMemory.Write_call(_CustomDrawSprite2_Function_Address);
            //ret
            CaveMemory.Write_StrBytes("C3");

            _CustomDrawSpriteAll_Function_Address = CaveMemory.CaveAddress;
            Logger.WriteLog("Mod_AddScreenSprites() => Adding Codecave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));
        }

        //Add CREDITS and replace the original PRESS START BUTTON sprite by new ones (PRESS START or INSERT COIN(S))  
        //Also check for Intro Movie to be skipped on 1st time to get to Title Screen directly (send 1 to the byte written on a [ENTER] press
        //And skip 1st attract mode to show unseen intro movie instead (change screen ID)
        //And skip Menu (sending [ENTER])
        private void Mod_AddLotOfThingsIntoQuickThread()
        {
            uint P1Start_ScanCode = _ArcadeModConfigurator.RegValues[16];
            uint P1Start_VirtualKeycode = Win32.MapVirtualKeyEx(P1Start_ScanCode, Win32.MAPVK_VSC_TO_VK_EX, IntPtr.Zero);
            if (P1Start_VirtualKeycode == 0)
            {
                Logger.WriteLog("Mod_AddLotOfThingsIntoQuickThread() => could not convert P1 Start Scancode 0x" + P1Start_ScanCode.ToString("x2") + "to VirtualScancode. Reverting to default : 0x31 (Key [1])");
                P1Start_VirtualKeycode = 0x31;
            }
            else
                Logger.WriteLog("Mod_AddLotOfThingsIntoQuickThread() =>P1 Start Scancode 0x" + P1Start_ScanCode.ToString("x2") + " converted to VirtualScancode 0x" + P1Start_VirtualKeycode.ToString("X2"));

            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);
            
            //First : save registers...lots of functions will be called, and modify them
            //push ebx
            CaveMemory.Write_StrBytes("53");
            //push edx
            CaveMemory.Write_StrBytes("52");

            //Adding Credits if coin key is pressed
            //cmp byte ptr [CreditsKeyPushed],01
            CaveMemory.Write_StrBytes("80 3D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_ArcadeMod_DataBank_Address + (uint)DataBank_Offset.CreditsKeyPushed));
            CaveMemory.Write_StrBytes("01");
            //jne Next
            CaveMemory.Write_StrBytes("75 58");
            //cmp dword ptr _CurrentSceneID_Offset,03
            CaveMemory.Write_StrBytes("83 3D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes((UInt32)_Process_MemoryBaseAddress + _CurrentSceneID_Offset));
            CaveMemory.Write_StrBytes("03");
            //je Next
            CaveMemory.Write_StrBytes("74 4F");
            //cmp Credits,00000099
            CaveMemory.Write_StrBytes("81 3D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes((UInt32)_Process_MemoryBaseAddress + _Credits_Offset));
            CaveMemory.Write_StrBytes("63 00 00 00");
            //jnl Next
            CaveMemory.Write_StrBytes("7D 43");
            //mov byte ptr[CreditsKeyPushed], 0
            CaveMemory.Write_StrBytes("C6 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_ArcadeMod_DataBank_Address + (uint)DataBank_Offset.CreditsKeyPushed));
            CaveMemory.Write_StrBytes("00");
            //add Credits, 1
            CaveMemory.Write_StrBytes("83 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes((UInt32)_Process_MemoryBaseAddress + _Credits_Offset));
            CaveMemory.Write_StrBytes("01");
            //mov [_ContinueTimerP1_Offset],00009FFF
            CaveMemory.Write_StrBytes("C7 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes((UInt32)_Process_MemoryBaseAddress + _ContinueTimerP1_Offset));
            CaveMemory.Write_StrBytes("FF 9F 00 00");
            //mov [_ContinueTimerP2_Offset],00009FFF
            CaveMemory.Write_StrBytes("C7 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes((UInt32)_Process_MemoryBaseAddress + _ContinueTimerP2_Offset));
            CaveMemory.Write_StrBytes("FF 9F 00 00");
            //mov [_ContinueTimer_Offset],00009FFF
            CaveMemory.Write_StrBytes("C7 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes((UInt32)_Process_MemoryBaseAddress + _ContinueTimer_Offset));
            CaveMemory.Write_StrBytes("FF 9F 00 00");
            //push eax
            CaveMemory.Write_StrBytes("50");
            //push ecx
            CaveMemory.Write_StrBytes("51");
            //push edx
            CaveMemory.Write_StrBytes("52");
            //push 00
            CaveMemory.Write_StrBytes("6A 00");
            //push 00010BA9 = ID for Start sound
            CaveMemory.Write_StrBytes("68 A9 0B 01 00");
            //push 9D2E18
            CaveMemory.Write_StrBytes("68");
            CaveMemory.Write_Bytes(BitConverter.GetBytes((UInt32)_Process_MemoryBaseAddress + _SoundPlayerHandle_Offset));
            //call 4A3D80
            CaveMemory.Write_call((UInt32)_Process_MemoryBaseAddress + _WndProcCredits_CalledFunctionOffset);
            //pop eax
            CaveMemory.Write_StrBytes("5A");
            //pop ecx
            CaveMemory.Write_StrBytes("59");
            //pop edx
            CaveMemory.Write_StrBytes("58");

            //Reset display RELOAD srpites if reloaded
            //cmp dword ptr [P1_Ammo], 00
            CaveMemory.Write_StrBytes("83 3D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _Player1Ammo_Offset));
            CaveMemory.Write_StrBytes("00");
            //jle CheckP2
            CaveMemory.Write_StrBytes("7E 0A");
            //mov [DataBank.DisplayP1Reload], 0
            CaveMemory.Write_StrBytes("C7 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_ArcadeMod_DataBank_Address + (uint)DataBank_Offset.ShowReloadP1));
            CaveMemory.Write_StrBytes("00 00 00 00");
            //cmp dword ptr [P2_Ammo], 00
            CaveMemory.Write_StrBytes("83 3D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _Player2Ammo_Offset));
            CaveMemory.Write_StrBytes("00");
            //jle Next
            CaveMemory.Write_StrBytes("7E 0A");
            //mov [DataBank.DisplayP2Reload], 0
            CaveMemory.Write_StrBytes("C7 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_ArcadeMod_DataBank_Address + (uint)DataBank_Offset.ShowReloadP2));
            CaveMemory.Write_StrBytes("00 00 00 00");

            //Second : Check to display RELOAD srpites
            //cmp dword ptr [DataBank_Offset.ShowReloadP1], 00
            CaveMemory.Write_StrBytes("83 3D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_ArcadeMod_DataBank_Address + (uint)DataBank_Offset.ShowReloadP1));
            CaveMemory.Write_StrBytes("00");
            //jne CheckP2
            CaveMemory.Write_StrBytes("74 38");
            //cmp dword ptr [P1_GameStatus], 05
            CaveMemory.Write_StrBytes("83 3D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _Player1GameStatus_Offset));
            CaveMemory.Write_StrBytes("05");
            //jne CheckP2
            CaveMemory.Write_StrBytes("75 2F");
            //push 3F800000
            CaveMemory.Write_StrBytes("68 00 00 80 3F");
            //push 0x05
            CaveMemory.Write_StrBytes("6A 05");
            //push 0x00
            CaveMemory.Write_StrBytes("6A 00");
            //push 3F800000
            CaveMemory.Write_StrBytes("68 00 00 80 3F");
            //push 3F800000
            CaveMemory.Write_StrBytes("68 00 00 80 3F");
            //push 38D1B717
            CaveMemory.Write_StrBytes("68 17 B7 D1 38");
            //push Y
            CaveMemory.Write_StrBytes("68");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(240.0f));
            //push X
            CaveMemory.Write_StrBytes("68");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(70.0f));
            //push 0x90 (Reload sprite ID)
            CaveMemory.Write_StrBytes("68 90 00 00 00");
            //Call draw
            CaveMemory.Write_call(GameProcess_ModuleBaseAddress + _DrawFsSpritesFunction_Offset);
            //add esp, 24
            CaveMemory.Write_StrBytes("83 C4 24");
            //CheckP2
            //cmp dword ptr [DataBank_Offset.ShowReloadP2],01
            CaveMemory.Write_StrBytes("83 3D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_ArcadeMod_DataBank_Address + (uint)DataBank_Offset.ShowReloadP2));
            CaveMemory.Write_StrBytes("00");
            //je Next
            CaveMemory.Write_StrBytes("74 38");
            //cmp dword ptr [P2_GameStatus], 05
            CaveMemory.Write_StrBytes("83 3D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _Player2GameStatus_Offset));
            CaveMemory.Write_StrBytes("05");
            //jne Next
            CaveMemory.Write_StrBytes("75 2F");
            //push 3F800000
            CaveMemory.Write_StrBytes("68 00 00 80 3F");
            //push 0x07
            CaveMemory.Write_StrBytes("6A 07");
            //push 0x00
            CaveMemory.Write_StrBytes("6A 00");
            //push 3F800000
            CaveMemory.Write_StrBytes("68 00 00 80 3F");
            //push 3F800000
            CaveMemory.Write_StrBytes("68 00 00 80 3F");
            //push 38D1B717
            CaveMemory.Write_StrBytes("68 17 B7 D1 38");
            //push Y
            CaveMemory.Write_StrBytes("68");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(240.0f));
            //push X
            CaveMemory.Write_StrBytes("68");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(570.0f));
            //push 0x90 (Reload sprite ID)
            CaveMemory.Write_StrBytes("68 90 00 00 00");
            //Call draw
            CaveMemory.Write_call(GameProcess_ModuleBaseAddress + _DrawFsSpritesFunction_Offset);
            //add esp, 24
            CaveMemory.Write_StrBytes("83 C4 24");

            //Skip Intro Movie the first time to go directly to title screen
            //cmp dword ptr [Scene_ID],04
            CaveMemory.Write_StrBytes("83 3D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _CurrentSceneID_Offset));
            CaveMemory.Write_StrBytes("04");
            //jne Next
            CaveMemory.Write_StrBytes("75 13");
            //cmp dword ptr [SkipIntro],01
            CaveMemory.Write_StrBytes("83 3D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_ArcadeMod_DataBank_Address + (uint)DataBank_Offset.SkipIntro));
            CaveMemory.Write_StrBytes("01");
            //jne Next
            CaveMemory.Write_StrBytes("75 A");
            //mov [hod3pc.exe+3ECF48],00000001
            CaveMemory.Write_StrBytes("C7 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _EnterKeyPressed_Offset));
            CaveMemory.Write_StrBytes("01 00 00 00");

            //On title screen reset the "skip intro movie" flag and send KeyUp Start button (else, user need to push 2x the buttons to enter the game)
            //cmp byte ptr [Scene_ID],05
            CaveMemory.Write_StrBytes("80 3D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _CurrentSceneID_Offset));
            CaveMemory.Write_StrBytes("05");
            //jne Next
            CaveMemory.Write_StrBytes("75 20");
            //cmp byte ptr [SkipIntro],01
            CaveMemory.Write_StrBytes("80 3D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_ArcadeMod_DataBank_Address + (uint)DataBank_Offset.SkipIntro));
            CaveMemory.Write_StrBytes("01");
            //jne Next
            CaveMemory.Write_StrBytes("75 17");
            //mov byte ptr[SkipIntro],00
            CaveMemory.Write_StrBytes("C6 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_ArcadeMod_DataBank_Address + (uint)DataBank_Offset.SkipIntro));
            CaveMemory.Write_StrBytes("00");
            //Write P1 Start byte in Inputs to simulate KeyPress
            //push eax
            CaveMemory.Write_StrBytes("50");
            //movzx eax, [hod3pc.exe+_P1_Buttons_Offset]
            CaveMemory.Write_StrBytes("0F B6 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _P1_Buttons_Offset));
            //and al, EF
            CaveMemory.Write_StrBytes("24 EF");
            //mov byte ptr [hod3pc.exe+_P1_Buttons_Offset], al
            CaveMemory.Write_StrBytes("A2");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _P1_Buttons_Offset));
            //pop eax
            CaveMemory.Write_StrBytes("58");
                  
            //First time in attract mode, replace it by the skipped movie intro
            //cmp dword ptr [Scene_ID],06
            CaveMemory.Write_StrBytes("83 3D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _CurrentSceneID_Offset));
            CaveMemory.Write_StrBytes("06");
            //jne Next
            CaveMemory.Write_StrBytes("75 1D");
            //cmp dword ptr [ShowIntro],01
            CaveMemory.Write_StrBytes("83 3D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_ArcadeMod_DataBank_Address + (uint)DataBank_Offset.ShowIntro));
            CaveMemory.Write_StrBytes("01");
            //jne Next
            CaveMemory.Write_StrBytes("75 14");
            //mov [NextScene_ID],00000004
            CaveMemory.Write_StrBytes("C7 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _NextSceneID_Offset));
            CaveMemory.Write_StrBytes("04 00 00 00");
            //mov [ShowIntro],00000000
            CaveMemory.Write_StrBytes("C7 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_ArcadeMod_DataBank_Address + (uint)DataBank_Offset.ShowIntro));
            CaveMemory.Write_StrBytes("00 00 00 00");

            //call dword ptr [hod3pc.exe+1491C0] GetTickCount()
            CaveMemory.Write_StrBytes("FF 15 C0 91 54 00");
            //cmp dword ptr [Databank_BlinkTinkCount],00
            CaveMemory.Write_StrBytes("83 3D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_ArcadeMod_DataBank_Address + (uint)DataBank_Offset.BlinkTickCount));
            CaveMemory.Write_StrBytes("00");
            //jne CheckBlinkDelay
            CaveMemory.Write_StrBytes("75 0C");
            //mov [Databank_BlinkTinkCount], eax
            CaveMemory.Write_StrBytes("A3");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_ArcadeMod_DataBank_Address + (uint)DataBank_Offset.BlinkTickCount));
            //mov [Databank_FrameTickCount], eax
            CaveMemory.Write_StrBytes("A3");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_ArcadeMod_DataBank_Address + (uint)DataBank_Offset.FrameTickCount));
            //jmp DrawSprites
            CaveMemory.Write_StrBytes("EB 3A");
            //CheckBlinkDelay:
            //mov ebx, eax
            CaveMemory.Write_StrBytes("8B D8");
            //sub ebx, [Databank_BlinkTinkCount]
            CaveMemory.Write_StrBytes("2B 1D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_ArcadeMod_DataBank_Address + (uint)DataBank_Offset.BlinkTickCount));
            //cmp ebx,00000300
            CaveMemory.Write_StrBytes("81 FB 00 03 00 00");
            //jle CheckFrameDelay
            CaveMemory.Write_StrBytes("7E 18");
            //SwitchVisibility:
            //mov [Databank_BlinkTinkCount],eax
            CaveMemory.Write_StrBytes("A3");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_ArcadeMod_DataBank_Address + (uint)DataBank_Offset.BlinkTickCount));
            //mov ebx, [Databank_SpriteVisibility]
            CaveMemory.Write_StrBytes("8B 1D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_ArcadeMod_DataBank_Address + (uint)DataBank_Offset.CustomSpriteVisibility));
            // mov ecx,00000001
            CaveMemory.Write_StrBytes("B9 01 00 00 00");
            //sub ecx,ebx
            CaveMemory.Write_StrBytes("29 D9");
            //mov [Databank_SpriteVisibility], ecx
            CaveMemory.Write_StrBytes("89 0D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_ArcadeMod_DataBank_Address + (uint)DataBank_Offset.CustomSpriteVisibility));
            //CheckFrameDelay:
            //mov ebx,eax
            CaveMemory.Write_StrBytes("8B D8");
            //sub ebx, [Databank_FrameTinkCount]
            CaveMemory.Write_StrBytes("2B 1D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_ArcadeMod_DataBank_Address + (uint)DataBank_Offset.FrameTickCount));
            //cmp ebx,10
            CaveMemory.Write_StrBytes("83 FB 01");
            //jl Exit
            CaveMemory.Write_StrBytes("7C 35");
            //ShowSprite:
            //mov [Databank_BlinkTinkCount],eax
            CaveMemory.Write_StrBytes("A3");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_ArcadeMod_DataBank_Address + (uint)DataBank_Offset.FrameTickCount));
            //mov eax,  [SceneID]
            CaveMemory.Write_StrBytes("A1");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _CurrentSceneID_Offset));
            //cmp eax, 2
            CaveMemory.Write_StrBytes("83 F8 02");
            //je exit
            CaveMemory.Write_StrBytes("74 26");
            //cmp eax, 10
            CaveMemory.Write_StrBytes("83 F8 0A");
            //je exit
            CaveMemory.Write_StrBytes("74 21");
            //cmp eax, 11
            CaveMemory.Write_StrBytes("83 F8 0B");
            //je exit
            CaveMemory.Write_StrBytes("74 1C");
            //push Y2 = 450.0f
            CaveMemory.Write_StrBytes("68");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Players_LowerSpriteInfo_Y));
            //push X2 = 320.0f
            CaveMemory.Write_StrBytes("68");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(320.0f));
            //push Y2 = 431.0f
            CaveMemory.Write_StrBytes("68");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Players_UpperSpriteInfo_Y));
            //push X2 = 320.0f
            CaveMemory.Write_StrBytes("68");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(320.0f));
            //Call CustomDrawSpriteFunction_1
            CaveMemory.Write_call(_CustomDrawSpriteAll_Function_Address);
            //Call CustomDrawSpriteFunction_1
            //add esp, 10
            CaveMemory.Write_StrBytes("83 C4 10");

            //Resore registers before returning
            //pop edx
            CaveMemory.Write_StrBytes("5A");
            //pop ebx
            CaveMemory.Write_StrBytes("5B");
            //mov eax, [esp+14]
            CaveMemory.Write_StrBytes("8B 44 24 14");
            //mov ecx, [eax]
            CaveMemory.Write_StrBytes("8B 08");

            //Inject it
            CaveMemory.InjectToOffset(_LoopThreadVariousThings_InjectionStruct, "Mod_AddLotOfThingsIntoQuickThread()");
        }

        //Changing the flow of intro logo at boot, more like arcade
        private void Mod_IntroductionLoop()
        {
            WriteByte(GameProcess_ModuleBaseAddress + _IntroLogoChange_Offset1, 0x01);
            WriteByte(GameProcess_ModuleBaseAddress + _IntroLogoChange_Offset2, 0x01);
            WriteByte(GameProcess_ModuleBaseAddress + _IntroLogoChange_Offset3, 0x07);
        }

        //Making the Title Screen a little more "arcade"
        private void Mod_TitleScreen()
        {
            //Removing the sound when validating. The sound from the automated next menu will play, and we can use this sound for credits instead
            SetNops(GameProcess_ModuleBaseAddress, _Nop_SfxPlayStart);

            ///Removing old Title (alpha=0)
            WriteBytes(GameProcess_ModuleBaseAddress + _ReplaceTitleLogo_Offset, new byte[] { 0x90, 0x90, 0x90, 0x57, 0x6A, 0x00 });  //pushing alpha=0 instead of fading EAX value

            //Remove old PRESS START
            WriteBytes(GameProcess_ModuleBaseAddress + _ReplaceTitleLogo_Offset + 0xB7, new byte[] { 0x31, 0xD2, 0x90, 0x90 });

            //New Logo
            //pushing fading alpha in ESP
            WriteBytes(GameProcess_ModuleBaseAddress + _ReplaceTitleLogo_Offset + 0x2F, new byte[] { 0x90, 0xFF, 0x74, 0x24, 0x2C });
            WriteByte(GameProcess_ModuleBaseAddress + _ReplaceTitleLogo_Offset + 0x35, 0x0A);  //Centered

            //Method 2 : Keep Old Trademark (change location) and add a new sprite
            //Location of old trademark
            WriteBytes(GameProcess_ModuleBaseAddress + _ReplaceTitleLogo_Offset + 0x48, BitConverter.GetBytes(390.0f));
            WriteBytes(GameProcess_ModuleBaseAddress + _ReplaceTitleLogo_Offset + 0x4D, BitConverter.GetBytes(320.0f));
            //ID
            WriteBytes(GameProcess_ModuleBaseAddress + _ReplaceTitleLogo_Offset + 0x54, BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _SprMenuExtraSprites_Offset + (uint)SprMenuExtraSprite.Trademark));
            //New Sprite for Logo
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);
            //call 04A5EE0
            CaveMemory.Write_call(GameProcess_ModuleBaseAddress + _DrawFs2SpritesFunction_Offset);
            //mov [esp],00000000
            CaveMemory.Write_StrBytes("C7 04 24");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(320.0f));
            //mov [esp-04],00000000
            CaveMemory.Write_StrBytes("C7 44 24 04");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(240.0f));
            //mov edi,hod3pc.exe+199D54
            CaveMemory.Write_StrBytes("BF");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _SprMenuExtraSprites_Offset + (uint)SprMenuExtraSprite.Logo));
            //call 04A5EE0
            CaveMemory.Write_call(GameProcess_ModuleBaseAddress + _DrawFs2SpritesFunction_Offset);

            //Inject it
            CaveMemory.InjectToOffset(_ChangeTitleLogo_InjectionStruct, "Mod_TitleScreen()");
        }

        //Replaceing instructions sprites with arcade ones (no more autoreload, etc...)
        //To enable the Reload in the attract, we just need to write 1 in 767AB0.....but we need to find where to put the instruction :(
        private void Mod_ReplaceAttractModeSprites()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);
            
            //cmp [esp], C4
            CaveMemory.Write_StrBytes("81 3C 24 C4 00 00 00");
            //jne Check2
            CaveMemory.Write_StrBytes("75 3F");
            //push edi
            CaveMemory.Write_StrBytes("57");
            //push ecx
            CaveMemory.Write_StrBytes("51");
            //push edx
            CaveMemory.Write_StrBytes("52");
            //mov edi, RELOAD_SPRITE_ID
            CaveMemory.Write_StrBytes("BF");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _SprMenuExtraSprites_Offset + (uint)SprMenuExtraSprite.AttractReload));
            //mov eax, [esp+2C]
            CaveMemory.Write_StrBytes("8B 44 24 2C");
            //push eax
            CaveMemory.Write_StrBytes("50");
            //push 05
            CaveMemory.Write_StrBytes("6A 05");
            //push 0
            CaveMemory.Write_StrBytes("6A 00");
            //push 3f800000
            CaveMemory.Write_StrBytes("68 00 00 80 3F");
            //push 3f800000
            CaveMemory.Write_StrBytes("68 00 00 80 3F");
            //push 37a7c5ac
            CaveMemory.Write_StrBytes("68 AC C5 A7 37");
            //push Y
            CaveMemory.Write_StrBytes("68");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(160.0f));
            //push X
            CaveMemory.Write_StrBytes("68");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(96.0f));
            //call 4A5EE0
            CaveMemory.Write_call(GameProcess_ModuleBaseAddress + _DrawFs2SpritesFunction_Offset);
            //add esp, 20
            CaveMemory.Write_StrBytes("83 C4 20");
            //pop edx
            CaveMemory.Write_StrBytes("5A");
            //pop ecx
            CaveMemory.Write_StrBytes("59");
            //pop edi
            CaveMemory.Write_StrBytes("5F");
            //mov [esp+20], 0
            CaveMemory.Write_StrBytes("C7 44 24 20 00 00 00 00");
            //jmp return
            CaveMemory.Write_StrBytes("EB 62");

            //Check 2:
            //cmp [esp], BF
            CaveMemory.Write_StrBytes("81 3C 24 BF 00 00 00");
            //jne Check3
            CaveMemory.Write_StrBytes("75 3F");
            //push edi
            CaveMemory.Write_StrBytes("57");
            //push ecx
            CaveMemory.Write_StrBytes("51");
            //push edx
            CaveMemory.Write_StrBytes("52");
            //mov edi, RELOAD_SPRITE_ID
            CaveMemory.Write_StrBytes("BF");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _SprMenuExtraSprites_Offset + (uint)SprMenuExtraSprite.AttractShells));
            //mov eax, [esp+2C]
            CaveMemory.Write_StrBytes("8B 44 24 2C");
            //push eax
            CaveMemory.Write_StrBytes("50");
            //push 05
            CaveMemory.Write_StrBytes("6A 05");
            //push 0
            CaveMemory.Write_StrBytes("6A 00");
            //push 3f800000
            CaveMemory.Write_StrBytes("68 00 00 80 3F");
            //push 3f800000
            CaveMemory.Write_StrBytes("68 00 00 80 3F");
            //push 37a7c5ac
            CaveMemory.Write_StrBytes("68 AC C5 A7 37");
            //push Y
            CaveMemory.Write_StrBytes("68");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(284.0f));
            //push X
            CaveMemory.Write_StrBytes("68");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(96.0f));
            //call 4A5EE0
            CaveMemory.Write_call(GameProcess_ModuleBaseAddress + _DrawFs2SpritesFunction_Offset);
            //add esp, 20
            CaveMemory.Write_StrBytes("83 C4 20");
            //pop edx
            CaveMemory.Write_StrBytes("5A");
            //pop ecx
            CaveMemory.Write_StrBytes("59");
            //pop edi
            CaveMemory.Write_StrBytes("5F");
            //mov [esp+20], 0
            CaveMemory.Write_StrBytes("C7 44 24 20 00 00 00 00");
            //jmp return
            CaveMemory.Write_StrBytes("EB 1A");

            //Check3:
            //cmp [esp], CB
            CaveMemory.Write_StrBytes("81 3C 24 CB 00 00 00");
            //je remove
            CaveMemory.Write_StrBytes("74 09");
            //cmp [esp], C6
            CaveMemory.Write_StrBytes("81 3C 24 C6 00 00 00");
            //jne return
            CaveMemory.Write_StrBytes("75 08");
            //remove:
            //mov [esp+20], 0
            CaveMemory.Write_StrBytes("C7 44 24 20 00 00 00 00");
            //return:
            //call hod3pc.exe+A5EA0
            CaveMemory.Write_call(GameProcess_ModuleBaseAddress + _DrawFsSpritesFunction_Offset);

            //Inject it
            CaveMemory.InjectToOffset(_ReplaceAttractModeSprites_InjectionStruct, "Mod_ReplaceAttractModeSprite()");
        }

        //The game is alterning between "arcade" attract mode and "time attack" attract mode by switching a byte 0/1
        //We will just force the jump whatever the value is to only keep Arcade
        //Same thing for Ranking, remove Time Attack display by forcing the read value to 0
        private void Mod_DisableTimeAttackAttractMode()
        {
            WriteBytes(GameProcess_ModuleBaseAddress + _DisableTimeAttackAttractMode_Offset, new byte[] { 0xEB, 0x23 });
            WriteBytes(GameProcess_ModuleBaseAddress + _DisableTimeAttackRankingDisplay_Offset, new byte[] { 0x31, 0xC0, 0x90 });
        }

        //By Default the game set Credits to "0" and init credits at each new game with the right number based on CreditsToStart
        //We will override this so that we are the only ones to add them and no reset at the end of the game
        private void Mod_CreditsHandling()
        {
            SetNops(GameProcess_ModuleBaseAddress, _Nop_CreditsInit_1);
            SetNops(GameProcess_ModuleBaseAddress, _Nop_CreditsInit_2);
            SetNops(GameProcess_ModuleBaseAddress, _Nop_CreditsInit_3);
            SetNops(GameProcess_ModuleBaseAddress, _Nop_CreditsInit_4);
            SetNops(GameProcess_ModuleBaseAddress, _Nop_CreditsToStartAndContinueInit);
            WriteByte(GameProcess_ModuleBaseAddress + _CreditsToStart_Offset, (byte)_ArcadeModConfigurator.CreditsToStart);
            WriteByte(GameProcess_ModuleBaseAddress + _CreditsToContinue_Offset, (byte)_ArcadeModConfigurator.CreditsToContinue);
        }

        // Add the Title SCENE_ID in allowed ID to start a game, instead of the MENU_ID
        private void Mod_StartGameFromTitleScreen()
        {
            //Enable the code to run when in Title screen instead of Menu screen
            WriteByte(GameProcess_ModuleBaseAddress + _StartGameFromTitle_Offset1, 0x05);
            WriteByte(GameProcess_ModuleBaseAddress + _StartGameFromTitle_Offset2, 0x05);

            //Adding possibility to go into the good prrocedure, when SCENE_ID is Title, in a switch loop
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);
            
            //cmp edi,05
            CaveMemory.Write_StrBytes("83 FF 05");
            //jne 00B10018
            CaveMemory.Write_StrBytes("75 0F");
            //mov [hod3pc.exe+366748],00000001
            CaveMemory.Write_StrBytes("C7 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _AllowStartGame));
            CaveMemory.Write_StrBytes("01 00 00 00");
            //jmp Case"5"
            CaveMemory.Write_jmp(GameProcess_ModuleBaseAddress + _StartGameFromTitleSwitchCase_Offset);
            //movzx eax,byte ptr [eax+hod3pc.exe+8E134]
            CaveMemory.Write_StrBytes("0F B6 80");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _StartGameFromByteJmpTable_Offset));

            //Inject it
            CaveMemory.InjectToOffset(_StartGameFromTitle_InjectionStruct, "Mod_BlockTitleIfNoCredits()");
        }

        //On the game, pausing then returning to the main menu will be redirected to the Title Screen (Main menu does not exists)
        //For that, when the game asks for scene ID 2 (menu) from scene ID 10 (game) > redirect to scene ID 5 (title)
        private void Mod_ReturnToMenu()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //cmp esi, 2
            CaveMemory.Write_StrBytes("83 FE 02");
            //jne originalcode
            CaveMemory.Write_StrBytes("75 0E");
            //cmp [CURRENT_SCENE_ID],0000000A
            CaveMemory.Write_StrBytes("83 3D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _CurrentSceneID_Offset));
            CaveMemory.Write_StrBytes("0A");
            //jne originalcode
            CaveMemory.Write_StrBytes("75 05");
            //mov esi,00000005
            CaveMemory.Write_StrBytes("BE 05 00 00 00");
            //originalcode : cmp esi, 8
            CaveMemory.Write_StrBytes("83 FE 08");
            //jl @offset_32BD5
            CaveMemory.Write_jl(GameProcess_ModuleBaseAddress + _ReturnToMenu_InjectionStruct.InjectionOffset + 0x25);

            //Inject it
            CaveMemory.InjectToOffset(_ReturnToMenu_InjectionStruct, "Mod_RemoveMenu_Screen()");
        }

        //By default the game is limiting Max_Life to 9, whereas in arcade you can choose from 5 to 9
        //And we need to limit the game to max life number when it is initializing the game, as start life can be greater than max
        private void Mod_SetMaxLife()
        {
            WriteByte(GameProcess_ModuleBaseAddress + _MaxLife_Offset, (byte)_ArcadeModConfigurator.MaxLife);

            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //movsx eax,byte ptr [edx+00000186]
            CaveMemory.Write_StrBytes("0F BE 82 86 01 00 00");
            //cmp eax, _Option_MaxLife
            CaveMemory.Write_StrBytes("83 F8");
            CaveMemory.Write_Bytes(new byte[] { (byte)(_ArcadeModConfigurator.MaxLife - 1) });    //Game is using a table indexed from 0 to load life number
            //jle
            CaveMemory.Write_StrBytes("7E 05");
            //mov eax, _Option_MaxLife
            CaveMemory.Write_StrBytes("B8");
            CaveMemory.Write_Bytes(new byte[] { (byte)(_ArcadeModConfigurator.MaxLife - 1), 0x00, 0x00, 0x00 });

            //Inject it
            CaveMemory.InjectToOffset(_InitLifeLimitToMax_InjectionStruct, "Mod_SetMaxLife()");
        }

        //Disable the Pause menu when hitting Start in-game
        private void Mod_DisableInGamePause_v1()
        {
            //Solution 1 : force jmp if pause is detected
            WriteByte(GameProcess_ModuleBaseAddress + _DisableInGamePause_v1_Offset, 0xEB);
        }
        private void Mod_DisableInGamePause_v2()
        {
            //Other possibility : xor eax, eax @48C2B3 
            WriteBytes(GameProcess_ModuleBaseAddress + _DisableInGamePause_v2_Offset, new byte[] { 0x31, 0xC0, 0x90, 0x90 });
        }

        //Replace the Cheat sequence to be done in the main menu to display score in game
        private void Mod_DisplayScore()
        {
            //Set 1 to the flag
            //WriteByte(GameProcess_ModuleBaseAddress + _FlagDisplayScore_Offset, 0x01);
            //Nop the reset procedure : by default the flag is reset after each end of game and return to the main menu
            //SetNops(GameProcess_ModuleBaseAddress, _Nop_ResetScoreDisplayFlag);

            //Can also force true the Flag test @+8F1C8, maybe easier
            SetNops(GameProcess_ModuleBaseAddress, _Nop_ScoreDisplayFlagTest);
        }

        //Hide gun display during gameplay, like arcade version of the game
        //Only display them during reload
        private void Mod_HideGuns()
        {
            SetNops(GameProcess_ModuleBaseAddress, _Nop_Arcade_Mode_Display);
        }

        //Fastening the reload animation so that it's more like arcade version of the game
        //Otherwise rthe reload animation is toooooo long
        private void Mod_FastReload()
        {
            //If no auto-reload, we also shrink the reload animation time to make it faster, like in arcade
            WriteByte(GameProcess_ModuleBaseAddress + _FastReload_Offset + 0x04, 0x11); //Reload Animation duration, in Frames.
            WriteByte(GameProcess_ModuleBaseAddress + _FastReload_Offset + 0x1C, 0x11); //Reload Animation duration, in Frames.
            WriteByte(GameProcess_ModuleBaseAddress + _FastReload_Offset + 0x18, 0x09); //Gun (?) Animation duration, in Frames.
            WriteByte(GameProcess_ModuleBaseAddress + _FastReload_Offset, 0x09); //Hand (?) Animation duration, in Frames.
        }

        //Disable auto-reload added on the PC game
        private void Mod_NoAutoReload()
        {
            SetNops(GameProcess_ModuleBaseAddress, _Nop_NoAutoReload_1);
        }

        //Enable the RELOAD text to appear, and RELOAD Sfx to play when it's needed
        //SFX drawing will be done on the "VariousThread Mod" because overwise, sprites are poors without transparency :(
        private void Mod_AddReloadEffects()
        {
            //Adding the Sfx when trigger is pushed with no bullets.
            Codecave32 CaveMemory_Reload1 = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //push eax
            CaveMemory_Reload1.Write_StrBytes("50");
            //push ecx
            CaveMemory_Reload1.Write_StrBytes("51");
            //mov eax, [eax+4]
            CaveMemory_Reload1.Write_StrBytes("8B 40 04");
            //shl eax, 2
            CaveMemory_Reload1.Write_StrBytes("C1 E0 02");
            //add eax, CustomDatabank.DisplayReload
            CaveMemory_Reload1.Write_StrBytes("05");
            CaveMemory_Reload1.Write_Bytes(BitConverter.GetBytes(_ArcadeMod_DataBank_Address + (uint)DataBank_Offset.ShowReloadP1));
            //mov [eax, 1]
            CaveMemory_Reload1.Write_StrBytes("C7 00 01 00 00 00");
            //push 00
            CaveMemory_Reload1.Write_StrBytes("6A 00");
            //push REloadSfx_ID
            CaveMemory_Reload1.Write_StrBytes("68");
            CaveMemory_Reload1.Write_Bytes(BitConverter.GetBytes(_SfxID_Reload));
            //push 9D2E18
            CaveMemory_Reload1.Write_StrBytes("68");
            CaveMemory_Reload1.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _SoundPlayerHandle_Offset));
            //call play audio
            CaveMemory_Reload1.Write_call(GameProcess_ModuleBaseAddress + _WndProcCredits_CalledFunctionOffset);
            //pop ecx
            CaveMemory_Reload1.Write_StrBytes("59");
            //pop eax
            CaveMemory_Reload1.Write_StrBytes("58");

            //Inject it
            CaveMemory_Reload1.InjectToOffset(_AddReloadSfx1_InjectionStruct, "Mod_AddReloadEffects()");

            //-----------------------------------//

            //Adding the Sfx when bullet count hits 0.
            Codecave32 CaveMemory_Reload2 = new Codecave32(_Process, GameProcess_ModuleBaseAddress);
            //mov [eax+68], 0
            CaveMemory_Reload2.Write_StrBytes("C7 40 68 00 00 00 00");
            //Keeping autoreload for attract mode
            //cmp dword ptr [SCENE_ID],06
            CaveMemory_Reload2.Write_StrBytes("83 3D");
            CaveMemory_Reload2.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _CurrentSceneID_Offset));
            CaveMemory_Reload2.Write_StrBytes("06");
            //jne NExt
            CaveMemory_Reload2.Write_StrBytes("75 03");
            //mov [ebp+18], bl
            CaveMemory_Reload2.Write_StrBytes("88 5D 18");
            //NExt:
            //push eax
            CaveMemory_Reload2.Write_StrBytes("50");
            //push ecx
            CaveMemory_Reload2.Write_StrBytes("51");
            //mov eax, [eax+4]
            CaveMemory_Reload2.Write_StrBytes("8B 40 04");
            //shl eax, 2
            CaveMemory_Reload2.Write_StrBytes("C1 E0 02");
            //add eax, CustomDatabank.DisllayReload
            CaveMemory_Reload2.Write_StrBytes("05");
            CaveMemory_Reload2.Write_Bytes(BitConverter.GetBytes(_ArcadeMod_DataBank_Address + (uint)DataBank_Offset.ShowReloadP1));
            //mov [eax, 1]
            CaveMemory_Reload2.Write_StrBytes("C7 00 01 00 00 00");
            //--------------------------------------------------------------------------------------------------------------------------------------
            // Reload SFX should not be played when ammo counter reaches 0, but only if the player push the trigger with no Ammo 
            //--------------------------------------------------------------------------------------------------------------------------------------
            //            //push 00
            //            CaveMemory_Reload2.Write_StrBytes("6A 00");
            //            //push REloadSfx_ID
            //            CaveMemory_Reload2.Write_StrBytes("68");
            //            CaveMemory_Reload2.Write_Bytes(BitConverter.GetBytes(_SfxID_Reload));
            //            //push 9D2E18
            //            CaveMemory_Reload2.Write_StrBytes("68");
            //            CaveMemory_Reload2.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _SoundPlayerHandle_Offset));
            //            //call play audio
            //            CaveMemory_Reload2.Write_call(GameProcess_ModuleBaseAddress + _WndProcCredits_CalledFunctionOffset);
            //--------------------------------------------------------------------------------------------------------------------------------------
            //pop ecx
            CaveMemory_Reload2.Write_StrBytes("59");
            //pop eax
            CaveMemory_Reload2.Write_StrBytes("58");

            //Inject it
            CaveMemory_Reload1.InjectToOffset(_AddReloadSfx2_InjectionStruct, "Mod_AddReloadEffects()");
        }

        //Replace old and ugly "FREEPLAY" displayed on lower screen during gameplay
        private void Mod_ReplaceFreeplaySpriteInGame()
        {
            //Adding the new one
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //push edi
            CaveMemory.Write_StrBytes("57");
            //push ecx
            CaveMemory.Write_StrBytes("51");
            //push edx
            CaveMemory.Write_StrBytes("52");
            //push 3F800000
            CaveMemory.Write_StrBytes("68 00 00 80 3F");
            //push 0A
            CaveMemory.Write_StrBytes("6A 0A");
            //push 00
            CaveMemory.Write_StrBytes("6A 00");
            //push 0.25
            CaveMemory.Write_StrBytes("68");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(0.25f));
            //push 0.25
            CaveMemory.Write_StrBytes("68");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(0.25f));
            //push 38D1B717
            CaveMemory.Write_StrBytes("68 17 B7 D1 38");
            //push Y
            CaveMemory.Write_StrBytes("68");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Players_LowerSpriteInfo_Y));
            //push X1
            CaveMemory.Write_StrBytes("68");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Player1_SpriteInfo_X));
            //cmp edi,01
            CaveMemory.Write_StrBytes("83 FF 01");
            //mov edi, [FREEPLAY SPRITE ID]
            CaveMemory.Write_StrBytes("BF");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _SprMenuExtraSprites_Offset + (uint)SprMenuExtraSprite.Freeplay));
            //jne Next
            CaveMemory.Write_StrBytes("75 07");
            //mov [esp], X2
            CaveMemory.Write_StrBytes("C7 04 24");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Player2_SpriteInfo_X));
            //Next:            
            //call hod3pc.exe+A5EE0
            CaveMemory.Write_call(GameProcess_ModuleBaseAddress + _DrawFs2SpritesFunction_Offset);
            //add esp, 20
            CaveMemory.Write_StrBytes("83 C4 20");
            //pop edx
            CaveMemory.Write_StrBytes("5A");
            //pop ecx
            CaveMemory.Write_StrBytes("59");
            //pop edi
            CaveMemory.Write_StrBytes("5F");
            //push 00000000
            CaveMemory.Write_StrBytes("68 00 00 00 00");

            //Inject it
            CaveMemory.InjectToOffset(_ReplaceFreeplaySpriteInGame_InjectionStruct, "Mod_ReplaceFreeplaySpriteInGame()");
        }

        //Same thing for the CREDITS sprite
        private void Mod_ReplaceCreditsSpriteInGame()
        {
            //Adding the new one
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);
            //push edi
            CaveMemory.Write_StrBytes("57");
            //push ecx
            CaveMemory.Write_StrBytes("51");
            //push edx
            CaveMemory.Write_StrBytes("52");
            //push 3F800000
            CaveMemory.Write_StrBytes("68 00 00 80 3F");
            //push 0A
            CaveMemory.Write_StrBytes("6A 0A");
            //push 00
            CaveMemory.Write_StrBytes("6A 00");
            //push 0.25
            CaveMemory.Write_StrBytes("68");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(0.25f));
            //push 0.25
            CaveMemory.Write_StrBytes("68");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(0.25f));
            //push 38D1B717
            CaveMemory.Write_StrBytes("68 17 B7 D1 38");
            //push Y
            CaveMemory.Write_StrBytes("68");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Players_LowerSpriteInfo_Y));
            //push X1
            CaveMemory.Write_StrBytes("68");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Player1_SpriteInfo_X + _Players_LowerSpriteInfo_OffsetX1));
            //cmp edi,01
            CaveMemory.Write_StrBytes("83 FF 01");
            //mov edi, [CREDITS SPRITE ID]
            CaveMemory.Write_StrBytes("BF");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _SprMenuExtraSprites_Offset + (uint)SprMenuExtraSprite.Credits));
            //jne Next
            CaveMemory.Write_StrBytes("75 07");
            //mov [esp], X2
            CaveMemory.Write_StrBytes("C7 04 24");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Player2_SpriteInfo_X + _Players_LowerSpriteInfo_OffsetX1));
            //Next:            
            //call hod3pc.exe+A5EE0
            CaveMemory.Write_call(GameProcess_ModuleBaseAddress + _DrawFs2SpritesFunction_Offset);
            //add esp, 20
            CaveMemory.Write_StrBytes("83 C4 20");
            //pop edx
            CaveMemory.Write_StrBytes("5A");
            //pop ecx
            CaveMemory.Write_StrBytes("59");
            //pop edi
            CaveMemory.Write_StrBytes("5F");
            //push 00000000
            CaveMemory.Write_StrBytes("68 00 00 00 00");

            //Inject it
            CaveMemory.InjectToOffset(_ReplaceCreditsSpriteInGame_InjectionStruct, "Mod_ReplaceCreditsSpriteInGame()");
        }

        //Original Sprite ID is [0x40 - 0x49]
        //Has to be converted for my table in the range [0-9]
        //During cutscenes, a bug make them diseappear behind bottom black borders.....
        //Only solution is to know how to override genuine spritesfrom SPR.AFS
        private void Mod_ReplaceCreditsDigit1SpriteInGame()
        {
            //Forcing the display of the 2 digits
            WriteBytes(GameProcess_ModuleBaseAddress + _ReplaceCreditsDigit1SpriteInGame_InjectionStruct.InjectionOffset - 0x06, new byte[] { 0x90, 0x90 });

            //Adding the new one
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);
            //push edi
            CaveMemory.Write_StrBytes("57");
            //push ecx
            CaveMemory.Write_StrBytes("51");
            //push edx
            CaveMemory.Write_StrBytes("52");
            //push 3F800000
            CaveMemory.Write_StrBytes("68 00 00 80 3F");
            //push 0A
            CaveMemory.Write_StrBytes("6A 0A");
            //push 00
            CaveMemory.Write_StrBytes("6A 00");
            //push 3E800000
            CaveMemory.Write_StrBytes("68");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(0.25f));
            //push 3E800000
            CaveMemory.Write_StrBytes("68");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(0.25f));
            //push 38D1B717
            CaveMemory.Write_StrBytes("68 17 B7 D1 38");
            //push Y
            CaveMemory.Write_StrBytes("68");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Players_LowerSpriteInfo_Y));
            //push X1
            CaveMemory.Write_StrBytes("68");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Player1_SpriteInfo_X + _Players_LowerSpriteInfo_OffsetX2));
            //mov ecx,0000000C
            CaveMemory.Write_StrBytes("B9 0C 00 00 00");
            //mul ecx
            CaveMemory.Write_StrBytes("F7 E1");
            //add eax, [☺SPRITE_0 ID]
            CaveMemory.Write_StrBytes("05");
            CaveMemory.Write_Bytes((BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _SprMenuExtraSprites_Offset + (uint)SprMenuExtraSprite.Num0)));
            //mov edi, eax
            CaveMemory.Write_StrBytes("8B F8");
            //cmp [esp+2C], 1
            CaveMemory.Write_StrBytes("83 7C 24 2C 01");
            //jne Draw
            CaveMemory.Write_StrBytes("75 07");
            //mov [esp], X2
            CaveMemory.Write_StrBytes("C7 04 24");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Player2_SpriteInfo_X + _Players_LowerSpriteInfo_OffsetX2));
            //Draw:
            //call hod3pc.exe+A5EE0
            CaveMemory.Write_call(GameProcess_ModuleBaseAddress + _DrawFs2SpritesFunction_Offset);
            //add esp, 20
            CaveMemory.Write_StrBytes("83 C4 20");
            //pop edx
            CaveMemory.Write_StrBytes("5A");
            //pop ecx
            CaveMemory.Write_StrBytes("59");
            //pop edi
            CaveMemory.Write_StrBytes("5F");
            //push 00000000
            CaveMemory.Write_StrBytes("68 00 00 00 00");

            //Inject it
            CaveMemory.InjectToOffset(_ReplaceCreditsDigit1SpriteInGame_InjectionStruct, "Mod_ReplaceCreditsDigit1SpriteInGame()");
        }
        private void Mod_ReplaceCreditsDigit2SpriteInGame()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);
            //push edi
            CaveMemory.Write_StrBytes("57");
            //push ecx
            CaveMemory.Write_StrBytes("51");
            //push edx
            CaveMemory.Write_StrBytes("52");
            //push 3F800000
            CaveMemory.Write_StrBytes("68 00 00 80 3F");
            //push 0A
            CaveMemory.Write_StrBytes("6A 0A");
            //push 00
            CaveMemory.Write_StrBytes("6A 00");
            //push 3E800000
            CaveMemory.Write_StrBytes("68");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(0.25f));
            //push 3E800000
            CaveMemory.Write_StrBytes("68");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(0.25f));
            //push 38D1B717
            CaveMemory.Write_StrBytes("68 17 B7 D1 38");
            //push Y
            CaveMemory.Write_StrBytes("68");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Players_LowerSpriteInfo_Y));
            //push X1
            CaveMemory.Write_StrBytes("68");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Player1_SpriteInfo_X + _Players_LowerSpriteInfo_OffsetX2 + 10.0f));
            //mov eax, esi
            CaveMemory.Write_StrBytes("8B C6");
            //mov ecx,0000000C
            CaveMemory.Write_StrBytes("B9 0C 00 00 00");
            //mul ecx
            CaveMemory.Write_StrBytes("F7 E1");
            //add eax, [☺SPRITE_0 ID]
            CaveMemory.Write_StrBytes("05");
            CaveMemory.Write_Bytes((BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _SprMenuExtraSprites_Offset + (uint)SprMenuExtraSprite.Num0)));
            //mov edi, eax
            CaveMemory.Write_StrBytes("8B F8");
            //cmp [esp+2C], 1
            CaveMemory.Write_StrBytes("83 7C 24 2C 01");
            //jne Draw
            CaveMemory.Write_StrBytes("75 07");
            //mov [esp], X2
            CaveMemory.Write_StrBytes("C7 04 24");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Player2_SpriteInfo_X + _Players_LowerSpriteInfo_OffsetX2 + 10.0f));
            //Draw:
            //call hod3pc.exe+A5EE0
            CaveMemory.Write_call(GameProcess_ModuleBaseAddress + _DrawFs2SpritesFunction_Offset);
            //add esp, 20
            CaveMemory.Write_StrBytes("83 C4 20");
            //pop edx
            CaveMemory.Write_StrBytes("5A");
            //pop ecx
            CaveMemory.Write_StrBytes("59");
            //pop edi
            CaveMemory.Write_StrBytes("5F");
            //push 00000000
            CaveMemory.Write_StrBytes("68 00 00 00 00");

            //Inject it
            CaveMemory.InjectToOffset(_ReplaceCreditsDigit2SpriteInGame_InjectionStruct, "Mod_ReplaceCreditsDigit2SpriteInGame()");
        }

        //Sae thing as above, but we will check Credits counter to select either PRESS START or INSERT COIN(S)
        private void Mod_ReplacePushStartButtonSpriteInGame()
        {
            //Adding the new one
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);
            //push edi
            CaveMemory.Write_StrBytes("57");
            //push ecx
            CaveMemory.Write_StrBytes("51");
            //push edx
            CaveMemory.Write_StrBytes("52");
            //push 3F800000
            CaveMemory.Write_StrBytes("68 00 00 80 3F");
            //push 0A
            CaveMemory.Write_StrBytes("6A 0A");
            //push 00
            CaveMemory.Write_StrBytes("6A 00");
            //push 0.25
            CaveMemory.Write_StrBytes("68");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(0.25f));
            //push 0.25
            CaveMemory.Write_StrBytes("68");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(0.25f));
            //push 38D1B717
            CaveMemory.Write_StrBytes("68 17 B7 D1 38");
            //push Y
            CaveMemory.Write_StrBytes("68");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Players_UpperSpriteInfo_Y));
            //push X1
            CaveMemory.Write_StrBytes("68");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Player1_SpriteInfo_X));
            //mov eax,edi
            CaveMemory.Write_StrBytes("8B C7");
            //mov ecx, 0x2C
            CaveMemory.Write_StrBytes("B9 2C 02 00 00");
            //mul ecx
            CaveMemory.Write_StrBytes("F7 E1");
            //add eax, [Player1Status]
            CaveMemory.Write_StrBytes("05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _Player1GameStatus_Offset));
            //cmp edi,01
            CaveMemory.Write_StrBytes("83 FF 01");
            //mov edi, [PRESS START SPRITE ID]
            CaveMemory.Write_StrBytes("BF");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _SprMenuExtraSprites_Offset + (uint)SprMenuExtraSprite.PressStart));
            //jne Next
            CaveMemory.Write_StrBytes("75 07");
            //mov [esp], X2
            CaveMemory.Write_StrBytes("C7 04 24");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Player2_SpriteInfo_X));
            //Next:
            //cmp byte ptr [FREEPLAY],01
            CaveMemory.Write_StrBytes("80 3D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _FlagFreeplay_Offset));
            CaveMemory.Write_StrBytes("01");
            //je draw
            CaveMemory.Write_StrBytes("74 1E");
            //xor ecx,ecx
            CaveMemory.Write_StrBytes("31 C9");
            //mov cl,[CREDITS TO CONTINUE]
            CaveMemory.Write_StrBytes("8A 0D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _CreditsToContinue_Offset));
            //cmp [eax], 9
            CaveMemory.Write_StrBytes("83 38 09");
            //jne Next2
            CaveMemory.Write_StrBytes("75 06");
            //mov cl, [CREDITS_TO_START]
            CaveMemory.Write_StrBytes("8A 0D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _CreditsToStart_Offset));
            //Next2
            //cmp [CREDITS], ecx
            CaveMemory.Write_StrBytes("39 0D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _Credits_Offset));
            //jg draw
            CaveMemory.Write_StrBytes("7D 03");
            //add edi, 0x18
            CaveMemory.Write_StrBytes("83 C7 18");
            //draw:
            //call hod3pc.exe+A5EE0
            CaveMemory.Write_call(GameProcess_ModuleBaseAddress + _DrawFs2SpritesFunction_Offset);
            //add esp, 20
            CaveMemory.Write_StrBytes("83 C4 20");
            //pop edx
            CaveMemory.Write_StrBytes("5A");
            //pop ecx
            CaveMemory.Write_StrBytes("59");
            //pop edi
            CaveMemory.Write_StrBytes("5F");
            //push 3F800000
            CaveMemory.Write_StrBytes("68 00 00 00 00");

            //Inject it
            CaveMemory.InjectToOffset(_ReplacePressStartButtonSpriteInGame_InjectionStruct, "Mod_ReplacePushStartButtonSpriteInGame()");
        }

        //By default, if not on freeplay, the game display directly a Game Over screen if there is not enough Credits to continue the game
        //This mod will enable the display of the Continue Screen so that a player can insert more Credits to continue            
        private void Mod_ForceContinueDispay()
        {
            WriteBytes(GameProcess_ModuleBaseAddress + _EnableContinueScreen_Offset, new byte[] { 0xEB, 0x05 });
        }

        //Changing opacity to "0" on the sprite display
        private void Mod_HideCrosshairs()
        {
            WriteByte(GameProcess_ModuleBaseAddress + _HideCrosshairs_Offset, 0x00);
        }

        //Removing the "SurvivalMode" subtitle and recentering the title
        private void Mod_NameEntryTitleCorrection()
        {
            WriteByte(GameProcess_ModuleBaseAddress + _NameEntryTitleCorrection_Offset + 3, 0x28);
            WriteBytes(GameProcess_ModuleBaseAddress + _NameEntryTitleCorrection_Offset + 0x1F, new byte[] { 0x00, 0x90, 0x90, 0x90 });
        }

        //Removing the "Save Game ?" popup displayed after beating the game : items are put in a loop, we just set alpha to 0
        private void Mod_RemoveEndgameSavePopup()
        {
            //Adding the new one
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);
            //mov ecx,[esi+24] (= original alpha value)
            CaveMemory.Write_StrBytes("8B 4E 24");
            //cmp dword ptr [SCENE_ID],0A
            CaveMemory.Write_StrBytes("83 3D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _CurrentSceneID_Offset));
            CaveMemory.Write_StrBytes("0A");
            //jne originalcode
            CaveMemory.Write_StrBytes("75 02");
            //xor ecx,ecx (= set alpha to 0)
            CaveMemory.Write_StrBytes("31 C9");
            //Originalcode:
            //mov edx,[esi+20]
            CaveMemory.Write_StrBytes("8B 56 20");

            //Inject it
            CaveMemory.InjectToOffset(_RemoveEndgameSavePopup_InjectionStruct, "Mod_RemoveEndgameSavePopup()");
        }

        //Now that the popup is removed, autosaving is needed !
        //Forcing jmp to "NO" so that the following popud does not appear, replacing the validating sound by the save file write call
        private void Mod_AutoSaveAfterEndGame()
        {
            WriteBytes(GameProcess_ModuleBaseAddress + _AutoSaveAfterEndGame_Offset, new byte[] { 0x52, 0x56, 0xBE, 0x60, 0x30, 0x9D, 0x00, 0xE8, 0x33, 0xF2, 0x00, 0x00, 0x5E, 0x5A, 0xEB, 0x1A });
        }

        //The windows game is displaying "SURVIVAL MODE" instead of just "RANKINGS"
        private void Mod_ReplaceRankingTitleSprite()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);
            //cmp [esp],00000357
            CaveMemory.Write_StrBytes("81 3C 24 57 03 00 00");
            //jne Next
            CaveMemory.Write_StrBytes("75 2A");
            //mov [esp+1C],00000006     //center sprite
            CaveMemory.Write_StrBytes("C7 44 24 1C 06 00 00 00");
            //mov [esp+04],43A00000     //mid screen X
            CaveMemory.Write_StrBytes("C7 44 24 04");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(320.0f));
            //push edi
            CaveMemory.Write_StrBytes("57");
            //push eax
            CaveMemory.Write_StrBytes("50");
            //push ecx
            CaveMemory.Write_StrBytes("51");
            //push edx
            CaveMemory.Write_StrBytes("52");
            //mov edi, SPRITE_ID RANKING
            CaveMemory.Write_StrBytes("BF");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _SprMenuExtraSprites_Offset + (uint)SprMenuExtraSprite.Rankings));
            //add esp, 14
            CaveMemory.Write_StrBytes("83 C4 14");
            //Call draw
            CaveMemory.Write_call(GameProcess_ModuleBaseAddress + _DrawFs2SpritesFunction_Offset);
            //sub esp, 14
            CaveMemory.Write_StrBytes("83 EC 14");
            //push edx
            CaveMemory.Write_StrBytes("5A");
            //push ecx
            CaveMemory.Write_StrBytes("59");
            //push eax
            CaveMemory.Write_StrBytes("58");
            //push edi
            CaveMemory.Write_StrBytes("5F");
            //jmp exit
            //push edi
            CaveMemory.Write_StrBytes("EB 05");
            //Original draw
            CaveMemory.Write_call(GameProcess_ModuleBaseAddress + _DrawFsSpritesFunction_Offset);

            //Inject it
            CaveMemory.InjectToOffset(_ReplaceRankingTitleSprite_InjectionStruct, "Mod_ReplaceRankingTitleSprite()");
        }

        //On exit (close window Cross, not ALT+F4) there's a popup that we can remove
        private void Mod_ConfirmExitGame()
        {
            SetNops(GameProcess_ModuleBaseAddress, _Nop_ConfirmExitGame);
            WriteBytes(GameProcess_ModuleBaseAddress + _DisableExitConfirm_Offset, new byte[] { 0xE9, 0xE5, 0x00, 0x00, 0x00 });
        }

        #endregion

        #region Inputs Hack

        protected override void Apply_InputsMemoryHack()
        {
            Create_InputsDataBank();            

            _PlayersButtons_Offsets = new UInt32[] { _P1_Buttons_Offset, _P2_Buttons_Offset };

            //Block Axis
            SetNops(GameProcess_ModuleBaseAddress, _Nop_X);
            SetNops(GameProcess_ModuleBaseAddress, _Nop_Y);

            //Blocking buttons : Jumping over the whole loop
            WriteBytes(GameProcess_ModuleBaseAddress + _Buttons_Patch_Offset, new byte[] { 0x90, 0xE9 });

            //If Arcade Mod is not applied, only Input change will be to force Input mode to KEYBOARD for both players
            if (!_GameConfig.GetBoolOption(ARCADE_MOD_OPTION))
                SetHack_InputMode();

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        /// <summary>
        /// Forcing first 2 parameters of the struct to 0 (= KEYBOARD)
        /// </summary>
        private void SetHack_InputMode()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //mov [esi+00000250],00000000
            CaveMemory.Write_StrBytes("C7 86 50 02 00 00 00 00 00 00");
            //mov [esi+00000254],00000000
            CaveMemory.Write_StrBytes("C7 86 54 02 00 00 00 00 00 00");
            //mov eax,[esi+000002DC]
            CaveMemory.Write_StrBytes("8B 86 DC 02 00 00");

            //Inject it
            CaveMemory.InjectToOffset(_OverrideRegistryRead_InjectionStruct, "Registry Check");
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
            //Changing opacity to "0" on the sprite display
            WriteByte(GameProcess_ModuleBaseAddress + _HideCrosshairs_Offset, 0x00);

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
                if (_ArcadeModConfigurator.Freeplay == 0)
                    WriteByte(_ArcadeMod_DataBank_Address + (uint)DataBank_Offset.CreditsKeyPushed, 0x01);
            }
        }

        protected override void GameController_OnButtonUp(object sender, VirtualGameControler.ButtonDownEventArgs e)
        {  }

        protected override void UpdateAxis(PlayerID Id, int X, int Y)
        {
            if (Id != PlayerID.None)
            {
                byte[] bufferX = BitConverter.GetBytes((Int16)X);
                byte[] bufferY = BitConverter.GetBytes((Int16)Y);

                WriteBytes(GameProcess_ModuleBaseAddress + _P1_X_Offset + ((uint)Id * 0x34), bufferX);
                WriteBytes(GameProcess_ModuleBaseAddress + _P1_Y_Offset + ((uint)Id * 0x34), bufferY);
            }
        }

        protected override void PlayerController_OnButtonDown(object sender, VirtualGameControler.ButtonDownEventArgs e)
        {
            if (e.Id != PlayerID.None && _PlayersButtons_Offsets != null)
            {
                UInt32 PlayerIndex = (UInt32)e.Id;

                if (e.ButtonName.Equals("TRIGGER"))
                {
                    Apply_OR_ByteMask(GameProcess_ModuleBaseAddress + _PlayersButtons_Offsets[PlayerIndex] + 2, 0x01);
                }
                else if (e.ButtonName.Equals("RELOAD"))
                {
                    Apply_OR_ByteMask(GameProcess_ModuleBaseAddress + _PlayersButtons_Offsets[PlayerIndex] + 3, 0x01);
                }
                else if (e.ButtonName.Equals("START"))
                {
                    Apply_OR_ByteMask(GameProcess_ModuleBaseAddress + _PlayersButtons_Offsets[PlayerIndex], 0x10);
                }
                else if (e.ButtonName.Equals("LEFT"))
                {
                    Apply_OR_ByteMask(GameProcess_ModuleBaseAddress + _PlayersButtons_Offsets[PlayerIndex], 0x04);
                }
                else if (e.ButtonName.Equals("RIGHT"))
                {
                    Apply_OR_ByteMask(GameProcess_ModuleBaseAddress + _PlayersButtons_Offsets[PlayerIndex], 0x08);
                }
                else if (e.ButtonName.Equals("ACTION"))
                {
                    //Check aiming position to set the action of MiddleButton according to wich half of the screen the user is pointing
                    VirtualPlayerController c = (VirtualPlayerController)sender;
                    if (c.Computed_X < 0)
                        Apply_OR_ByteMask(GameProcess_ModuleBaseAddress + _PlayersButtons_Offsets[PlayerIndex], 0x04);
                    else
                        Apply_OR_ByteMask(GameProcess_ModuleBaseAddress + _PlayersButtons_Offsets[PlayerIndex], 0x08);
                }
            }
        }
        protected override void PlayerController_OnButtonUp(object sender, VirtualGameControler.ButtonDownEventArgs e)
        {
            if (e.Id != PlayerID.None && _PlayersButtons_Offsets != null)
            {
                UInt32 PlayerIndex = (UInt32)e.Id;

                if (e.ButtonName.Equals("TRIGGER"))
                {
                    Apply_AND_ByteMask(GameProcess_ModuleBaseAddress + _PlayersButtons_Offsets[PlayerIndex] + 2, 0xFE);
                }
                else if (e.ButtonName.Equals("RELOAD"))
                {
                    Apply_AND_ByteMask(GameProcess_ModuleBaseAddress + _PlayersButtons_Offsets[PlayerIndex] + 3, 0xFE);
                }
                else if (e.ButtonName.Equals("START"))
                {
                    Apply_AND_ByteMask(GameProcess_ModuleBaseAddress + _PlayersButtons_Offsets[PlayerIndex], 0xEF);
                }
                else if (e.ButtonName.Equals("LEFT"))
                {
                    Apply_AND_ByteMask(GameProcess_ModuleBaseAddress + _PlayersButtons_Offsets[PlayerIndex], 0xFB);
                }
                else if (e.ButtonName.Equals("RIGHT"))
                {
                    Apply_AND_ByteMask(GameProcess_ModuleBaseAddress + _PlayersButtons_Offsets[PlayerIndex], 0xF7);
                }
                else if (e.ButtonName.Equals("ACTION"))
                {
                    Apply_AND_ByteMask(GameProcess_ModuleBaseAddress + _PlayersButtons_Offsets[PlayerIndex], 0xF3);
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
            for (int i = 0; i < _GameConfig.PlayerCount; i++)
            {
                _Life[i] = 0;
                _Ammo[i] = 0;
            }

            //Player status :
            //[4] = Continue Screen
            //[5] = InGame
            //[6] = Game Over
            //[7] = Time attack Scoreboard
            //[9] = Menu or Attract Mode
            // We will use these values to compute ourselve Recoil and P1/P2 Start Button Lights
            UInt32 P1_Status = ReadByte(GameProcess_ModuleBaseAddress + _P1_Status_Offset);
            UInt32 P2_Status = ReadByte(GameProcess_ModuleBaseAddress + _P2_Status_Offset);

            if (P1_Status == 5)
            {
                //Force Start Lamp to Off
                SetOutputValue(OutputId.P1_CtmLmpStart, 0);

                _Life[0] = ReadByte(GameProcess_ModuleBaseAddress + _P1_Life_Offset);
                _Ammo[0] = ReadByte(GameProcess_ModuleBaseAddress + _P1_Ammo_Offset);

                //Custom Recoil
                if (_Ammo[0] < _LastAmmo[0])
                    SetOutputValue(OutputId.P1_CtmRecoil, 1);

                //[Damaged] custom Output                
                if (_Life[0] < _LastLife[0])
                    SetOutputValue(OutputId.P1_Damaged, 1);
            }
            else
            {
                //Enable Start Lamp Blinking
                SetOutputValue(OutputId.P1_CtmLmpStart, -1);
            }

            if (P2_Status == 5)
            {
                //Force Start Lamp to Off
                SetOutputValue(OutputId.P2_CtmLmpStart, 0);

                _Life[1] = ReadByte(GameProcess_ModuleBaseAddress + _P2_Life_Offset);
                _Ammo[1] = ReadByte(GameProcess_ModuleBaseAddress + _P2_Ammo_Offset);

                //Custom Recoil
                if (_Ammo[1] < _LastAmmo[1])
                    SetOutputValue(OutputId.P2_CtmRecoil, 1);

                //[Damaged] custom Output                
                if (_Life[1] < _LastLife[1])
                    SetOutputValue(OutputId.P2_Damaged, 1);
            }
            else
            {
                //Enable Start Lamp Blinking
                SetOutputValue(OutputId.P2_CtmLmpStart, -1);
            }

            for (int i = 0; i < _GameConfig.PlayerCount; i++)
            {
                _LastLife[i] = _Life[i];
                _LastAmmo[i] = _Ammo[i];
            }

            SetOutputValue(OutputId.P1_Ammo, _Ammo[0]);
            SetOutputValue(OutputId.P2_Ammo, _Ammo[1]);
            SetOutputValue(OutputId.P1_Life, _Life[0]);
            SetOutputValue(OutputId.P2_Life, _Life[1]);
            SetOutputValue(OutputId.Credits, ReadByte(GameProcess_ModuleBaseAddress + _Credits_Offset));
        }

        #endregion

        internal class Configurator
        {
            #region Constants

            private const String CONF_FILENAME = "settings.ini";

            private const string OPTION_VOLUME_BGM = "VolumeBGM";
            private const string OPTION_VOLUME_SFX = "VolumeSFX";
            private const string OPTION_VOLUME_VCE = "VolumeVCE";
            private const string OPTION_DIFFICULTY = "Difficulty";
            private const string OPTION_INITIAL_LIFE = "InitialLife";
            private const string OPTION_MAX_LIFE = "MaxLife";
            private const string OPTION_BLOOD_COLOR = "BloodColor";
            private const string OPTION_VIOLENCE = "Violence";
            private const string OPTION_ADVERTISE = "AdvertiseSound";
            private const string OPTION_CREDITS_TO_START = "CreditsToStart";
            private const string OPTION_CREDITS_TO_CONTINUE = "CreditsToContinue";
            private const string OPTION_FREEPLAY = "Freeplay";
            private const string OPTION_CROSSHAIR_DESIGN = "CrosshairDesign";
            private const string OPTION_CROSSHAIR_SPEED = "CrosshairSpeed";

            private const string OPTION_ALIASING = "AntiAliasing";
            private const string OPTION_COLORS = "Colors";
            private const string OPTION_TEXTURES = "Textures";
            private const string OPTION_RESOLUTION = "Resolution";
            private const string OPTION_FRAMERATE = "Framerate";
            private const string OPTION_FULLSCREEN = "Fullscreen";
            private const string OPTION_KEYBOARD1P_CENTER = "Keyboard1pCenter";
            private const string OPTION_KEYBOARD1P_UP = "Keyboard1pUp";
            private const string OPTION_KEYBOARD1P_DOWN = "Keyboard1pDown";
            private const string OPTION_KEYBOARD1P_LEFT = "Keyboard1pLeft";
            private const string OPTION_KEYBOARD1P_RIGHT = "Keyboard1pRight";
            private const string OPTION_KEYBOARD1P_START = "Keyboard1pStart";
            private const string OPTION_KEYBOARD1P_SHOOT = "Keyboard1pShoot";
            private const string OPTION_KEYBOARD1P_RELOAD = "Keyboard1pReload";
            private const string OPTION_KEYBOARD2P_CENTER = "Keyboard2pCenter";
            private const string OPTION_KEYBOARD2P_UP = "Keyboard2pUp";
            private const string OPTION_KEYBOARD2P_DOWN = "Keyboard2Down";
            private const string OPTION_KEYBOARD2P_LEFT = "Keyboard2pLeft";
            private const string OPTION_KEYBOARD2P_RIGHT = "Keyboard2pRight";
            private const string OPTION_KEYBOARD2P_START = "Keyboard2pStart";
            private const string OPTION_KEYBOARD2P_SHOOT = "Keyboard2pShoot";
            private const string OPTION_KEYBOARD2P_RELOAD = "Keyboard2pReload";
            private const string OPTION_LANGUAGE = "Language";

            private const string OPTION_DISABLE_PAUSE = "DisableInGamePause";
            private const string OPTION_DISPLAY_SCORE = "DisplayScore";

            #endregion

            #region ConfigurationItems

            //Byte array for replacing Register values
            private UInt32[] _RegValues = new UInt32[] {
            0x01,   //CtrlType1P            0=Keyb, 1=Mouse, 2=Gamepad
            0x00,   //CtrlType2p            0=Keyb, 1=Mouse, 2=Gamepad
            0x00,   //Mouseshoot            0=Lbtn, 1=Rbtn 
            0x01,   //MouseReload           0=Lbtn, 1=Rbtn
            //+0x0C offset
            0x18,   //KeyBoard1pShoot
            0x25,   //KeyBoard2pShoot
            0x19,   //KeyBoard1pReload
            0x26,   //KeyBoard2pReload
            0xC8,   //KeyBoard1pUp
            0x48,   //KeyBoard2pUp
            0x50,   //KeyBoard1pDown
            0xD0,   //KeyBoard2pDown
            0xCB,   //KeyBoard1pLeft
            0x4B,   //KeyBoard2pLeft
            0xCD,   //KeyBoard1pRight
            0x4D,   //KeyBoard2pRight
            0x02,   //KeyBoard1pStart
            0x03,   //KeyBoard2pStart
            0x11,   //KeyBoard1pCenter
            0x15,   //KeyBoard2pCenter
            0x00,   //GamePad1p1
            0x00,   //GamePad2p1
            0x01,   //GamePad1p2
            0x01,   //GamePad2p2
            0x02,   //GamePad1p3
            0x02,   //GamePad2p3
            0x03,   //GamePad1p4
            0x03,   //GamePad2p4
            0x00,   //GamePad1Vb
            0x00,   //GamePad2Vb
            //+0x0C offset
            0x03,   //Graphicalresolution       //0=640x480, 1=800x600, 2=1024x768, 3=1280x960, 4=1280x1024
            0x01,   //Fullscreen                //0=OFF, 1=ON
            0x00,   //Framerate                 //0=60, 1=30, 2=20, 3=15, 4=10
            0x01,   //Antialiasing              //1=OFF, 0=ON
            0x02,   //Texture                   //0=Low, 1=Medium, 2=High
            0x01,   //ColorMode                 //1=32Bits, 2=16Bits
            0x00,   //Model
            0x00    //Language                  //0=English; 1=French, 2=Italian, 3=Spanish
        };

            //These one are store in HOD3.DAT file save
            private byte _VolumeBGM = 0xFF;         //[00-FF] step 0x08
            private byte _VolumeSFX = 0xFF;         //[00-FF] step 0x08
            private byte _VolumeVCE = 0xFF;         //[00-FF] step 0x08
            private byte _GameDifficulty = 2;       //[0-4] VERYEASY-MEDIUM EASY-NORMAL-MEDIUM HARD-VERY HARD
            private byte _InitialLife = 3;          //[0-4]
            private byte _Freeplay = 1;             //put FF in credits to start
            private byte _Violence = 0;             //[0-1-2]GRATUITOUS, MEDIUM or MILD
            private byte _BloodColor = 1;           //RED-GREEN [00 00 / 01 01]
            private byte _CrosshairDesign = 2;      //[0-3]
            private byte _CrosshairSpeed = 0xFF;    //[00-FF]

            //These ones are custom        
            private UInt32 _DisablePause = 1;
            private UInt32 _DisplayScore = 0;
            private UInt32 _CreditsToStart = 1;
            private UInt32 _CreditsToContinue = 1;
            private UInt32 _MaxLife = 5;

            #endregion

            #region Accessors

            public byte VolumeBGM
            { get { return _VolumeBGM; } }

            public byte VolumeSFX
            { get { return _VolumeSFX; } }

            public byte VolumeVCE
            { get { return _VolumeVCE; } }

            public byte GameDifficulty
            { get { return _GameDifficulty; } }

            public byte InitialLife
            { get { return (byte)(_InitialLife - 1); } }

            public byte Freeplay
            { get { return _Freeplay; } }

            public byte Violence
            { get { return _Violence; } }

            public byte BloodColor
            { get { return _BloodColor; } }

            public byte CrosshairDesign
            { get { return _CrosshairDesign; } }

            public byte CrosshairSpeed
            { get { return _CrosshairSpeed; } }

            public UInt32 DisablePause
            { get { return _DisablePause; } }

            public UInt32 DisplayScore
            { get { return _DisplayScore; } }

            public UInt32 CreditsToStart
            { get { return _CreditsToStart; } }

            public UInt32 CreditsToContinue
            { get { return _CreditsToContinue; } }

            public UInt32 MaxLife
            { get { return _MaxLife; } }

            public UInt32[] RegValues
            { get { return _RegValues; } }

            public UInt32 Language
            { get { return _RegValues[37]; } }

            #endregion

            private string _ConfigFilePath = string.Empty;

            public Configurator(string ConfigFilePath)
            {
                _ConfigFilePath = Path.Combine(ConfigFilePath, CONF_FILENAME);
            }

            private void SetOptionInArray(UInt32 iValue, int Index)
            {
                _RegValues[Index] = iValue;
            }

            public bool ReadConf()
            {                
                try
                {
                    string[] lines = File.ReadAllLines(_ConfigFilePath);
                    foreach (string line in lines)
                    {
                        if (!line.StartsWith("[") && !line.StartsWith(";"))
                        {
                            string[] buffer = line.Split(':');
                            string Key = buffer[0];
                            string Value = buffer[1];
                            UInt32 i_Value = 0;

                            if (UInt32.TryParse(Value, out i_Value))
                            {
                                switch (Key)
                                {                                    
                                    case OPTION_KEYBOARD1P_SHOOT:
                                        {
                                            SetOptionInArray(i_Value, 4);
                                        }
                                        break;
                                    case OPTION_KEYBOARD2P_SHOOT:
                                        {
                                            SetOptionInArray(i_Value, 5);
                                        }
                                        break;
                                    case OPTION_KEYBOARD1P_RELOAD:
                                        {
                                            SetOptionInArray(i_Value, 6);
                                        }
                                        break;
                                    case OPTION_KEYBOARD2P_RELOAD:
                                        {
                                            SetOptionInArray(i_Value, 7);
                                        }
                                        break;
                                    case OPTION_KEYBOARD1P_UP:
                                        {
                                            SetOptionInArray(i_Value, 8);
                                        }
                                        break;
                                    case OPTION_KEYBOARD2P_UP:
                                        {
                                            SetOptionInArray(i_Value, 9);
                                        }
                                        break;
                                    case OPTION_KEYBOARD1P_DOWN:
                                        {
                                            SetOptionInArray(i_Value, 10);
                                        }
                                        break;
                                    case OPTION_KEYBOARD2P_DOWN:
                                        {
                                            SetOptionInArray(i_Value, 11);
                                        }
                                        break;
                                    case OPTION_KEYBOARD1P_LEFT:
                                        {
                                            SetOptionInArray(i_Value, 12);
                                        }
                                        break;
                                    case OPTION_KEYBOARD2P_LEFT:
                                        {
                                            SetOptionInArray(i_Value, 13);
                                        }
                                        break;
                                    case OPTION_KEYBOARD1P_RIGHT:
                                        {
                                            SetOptionInArray(i_Value, 14);
                                        }
                                        break;
                                    case OPTION_KEYBOARD2P_RIGHT:
                                        {
                                            SetOptionInArray(i_Value, 15);
                                        }
                                        break;
                                    case OPTION_KEYBOARD1P_START:
                                        {
                                            SetOptionInArray(i_Value, 16);
                                        }
                                        break;
                                    case OPTION_KEYBOARD2P_START:
                                        {
                                            SetOptionInArray(i_Value, 17);
                                        }
                                        break;
                                    case OPTION_KEYBOARD1P_CENTER:
                                        {
                                            SetOptionInArray(i_Value, 18);
                                        }
                                        break;
                                    case OPTION_KEYBOARD2P_CENTER:
                                        {
                                            SetOptionInArray(i_Value, 19);
                                        }
                                        break;
                                    

                                    case OPTION_RESOLUTION:
                                        {
                                            SetOptionInArray(i_Value, 30);
                                        }
                                        break;
                                    case OPTION_FULLSCREEN:
                                        {
                                            SetOptionInArray(i_Value, 31);
                                        }
                                        break;
                                    case OPTION_FRAMERATE:
                                        {
                                            SetOptionInArray(i_Value, 32);
                                        }
                                        break;
                                    case OPTION_ALIASING:
                                        {
                                            SetOptionInArray(i_Value, 33);
                                        }
                                        break;
                                    case OPTION_TEXTURES:
                                        {
                                            SetOptionInArray(i_Value, 34);
                                        }
                                        break;
                                    case OPTION_COLORS:
                                        {
                                            SetOptionInArray(i_Value, 35);
                                        }
                                        break;
                                    case OPTION_LANGUAGE:
                                        {
                                            SetOptionInArray(i_Value, 37);
                                        }
                                        break;

                                    //

                                    case OPTION_VOLUME_BGM:
                                        {
                                            _VolumeBGM = (byte)i_Value;
                                        }
                                        break;
                                    case OPTION_VOLUME_SFX:
                                        {
                                            _VolumeSFX = (byte)i_Value;
                                        }
                                        break;
                                    case OPTION_VOLUME_VCE:
                                        {
                                            _VolumeVCE = (byte)i_Value;
                                        }
                                        break;
                                    case OPTION_DIFFICULTY:
                                        {
                                            _GameDifficulty = (byte)i_Value;
                                        }
                                        break;
                                    case OPTION_INITIAL_LIFE:
                                        {
                                            _InitialLife = (byte)i_Value;
                                        }
                                        break;
                                    case OPTION_MAX_LIFE:
                                        {
                                            _MaxLife = i_Value;
                                        }
                                        break;
                                    case OPTION_BLOOD_COLOR:
                                        {
                                            _BloodColor = (byte)i_Value;
                                        }
                                        break;
                                    case OPTION_VIOLENCE:
                                        {
                                            _Violence = (byte)i_Value;
                                        }
                                        break;
                                    case OPTION_ADVERTISE:
                                        {

                                        }
                                        break;
                                    case OPTION_CREDITS_TO_START:
                                        {
                                            _CreditsToStart = i_Value;
                                        }
                                        break;
                                    case OPTION_CREDITS_TO_CONTINUE:
                                        {
                                            _CreditsToContinue = i_Value;
                                        }
                                        break;
                                    case OPTION_FREEPLAY:
                                        {
                                            _Freeplay = (byte)i_Value;
                                        }
                                        break;
                                    case OPTION_CROSSHAIR_DESIGN:
                                        {
                                            _CrosshairDesign = (byte)i_Value;
                                        }
                                        break;
                                    case OPTION_CROSSHAIR_SPEED:
                                        {
                                            _CrosshairSpeed = (byte)i_Value;
                                        }
                                        break;
                                    case OPTION_DISABLE_PAUSE:
                                        {
                                            _DisablePause = (byte)i_Value;
                                        }
                                        break;
                                    case OPTION_DISPLAY_SCORE:
                                        {
                                            _DisplayScore = (byte)i_Value;
                                        }
                                        break;

                                    default: break;
                                }
                            }
                            else
                            {
                                Logger.WriteLog("Hod3pc Arcade Mod Configurator() => Error parsing value : " + Value + " for Key : " + Key);
                            }                            
                        }
                    }
                    return true;
                }
                catch (Exception Ex)
                {
                    Logger.WriteLog("Hod3pc Arcade Mod Configurator() => Can't read config data :\n" + AppDomain.CurrentDomain.BaseDirectory + CONF_FILENAME + " :\n\n" + Ex.Message.ToString());
                    return false;
                }
            }
        }
    }
}
