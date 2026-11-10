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
    public class Game_TtxMusicGunGun2 : BaseGame32
    {
        protected override bool PATCH_PEB_DEBUG_FLAG { get { return false; } }

        #region LOADER MEMORY ADDRESSES                
        private UInt32 _CryptoCheck_Patch_Offset = 0x0013773F;
        private UInt32 _DataPath_Patch1_Offset = 0x0020D3B0;
        private UInt32 _DataPath_Patch2_Offset = 0x0020D518;
        private UInt32 _DataPath_Patch3_Offset = 0x0020D524;
        private UInt32 _DataPath_Patch4_Offset = 0x0020D530;
        private UInt32 _DataPath_Patch5_Offset = 0x0020D538;
        private UInt32 _NoCrosshair_Patch_Offset = 0x001A41C2;
        private InjectionStruct32 _NoCursor_InjectionStruct = new InjectionStruct32(0x000170DD, 6);
        private InjectionStruct32 _LocalSave_InjectionStruct = new InjectionStruct32(0x00001A3E, 7);
        #endregion

        #region INPUTS MEMORY ADDRESSES    
        private UInt32 _ScreenWidth_Offset = 0x002DE830;
        private UInt32 _ScreenHeight_Offset = 0x002DE834;
        private NopStruct32 _Nop_EnableSecondPlayerAxis = new NopStruct32(0x001B544B, 6);
        private InjectionStruct32 _Inputs_InjectionStruct = new InjectionStruct32(0x001B5617, 6);
        private InjectionStruct32 _Coins_Injectionstruct = new InjectionStruct32(0x001B960E, 5);
        //Custom Data
        private UInt32 _Axes_CaveAddress;
        private UInt32 _OutOfScreen_CaveAddress;
        private UInt32 _GunButtons_CaveAddress;
        private UInt32 _SystemButtons_CaveAddress;
        private UInt32 _CoinFlag_CaveAddress;
        #endregion

        #region OUTPUTS MEMORY ADDRESSES
        private UInt32 _Outputs_Ptr_Offset = 0x002DBE0C;
        private UInt32 _Credits_Ptr_Offset = 0x002DBE08;
        #endregion

        public Game_TtxMusicGunGun2(GameConfig Config) : base(Config)
        {
            _KnownMd5Prints.Add("Music Gun Gun 2 v1.01 JPN - Original", "590677da06b758728a1dd607cbf032de");
            _KnownMd5Prints.Add("Music Gun Gun 2 v1.01 JPN - JConfig", "57fb4970df6ef979d7ffc044e6161e84");

            int Width = _GameConfig.ResolutionWidth != 0 ? _GameConfig.ResolutionWidth : 1280;
            int Height = _GameConfig.ResolutionHeight != 0 ? _GameConfig.ResolutionHeight : 720;

            string sArgs = "-fullscreen ";
            if (_GameConfig.Fullscreen)
                sArgs += "1 ";
            else
                sArgs += "0 ";

            sArgs += "-size " + Width.ToString() + " " + Height.ToString() + " -mts_no_xio 1 -mouse 1";
            _GameLauncher.Set_CmdLine_Args(sArgs);
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
            if (_GameConfig.NoCursor)
            {
                if (CreateTransparentCursor())
                {
                    SetHack_NoCursor();
                }
            }

            //Jumping over Crypto Check procedure
            WriteByte(GameProcess_ModuleBaseAddress + _CryptoCheck_Patch_Offset, 0xEB);

            SetHack_DataPath();            

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        /// <summary>
        /// There are a lot of hardcoded Data Path targettting D:\ drive.
        /// Also, in 401750(), the game sets game parameters (resolution, etc...) including the "local save" flag, based on the config mode parameter.
        /// Config mode is 0 by default here, and it results in the game hanging when a D:\ drive is not present...so forcing local save flag to 1
        /// </summary>
        private void SetHack_DataPath()
        {
            //Changing Hardcoded D:\\.............. datapath to local folder
            ReplaceReadOnlyString(new UInt32[] { _DataPath_Patch1_Offset, _DataPath_Patch2_Offset, _DataPath_Patch3_Offset, _DataPath_Patch4_Offset, _DataPath_Patch5_Offset }, Encoding.ASCII.GetBytes(".\\_"));

            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //mov [edx+48],00000000
            CaveMemory.Write_StrBytes("C7 42 48 00 00 00 00");
            //mov [eax+04],00000001
            CaveMemory.Write_StrBytes("C7 40 04 01 00 00 00");

            //Inject it
            CaveMemory.InjectToOffset(_LocalSave_InjectionStruct, "Local Save");
        }

        /// <summary>
        /// Replacing hCursor for RegisterClass() API
        /// </summary>
        private void SetHack_NoCursor()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //mov eax, _CustomTransparentCursorHandle
            CaveMemory.Write_StrBytes("B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes((UInt32)_CustomTransparentCursorHandle));
            //mov [ebp-30], eax
            CaveMemory.Write_StrBytes("89 45 D0");
            //mov ecx,[ebp-5C]
            CaveMemory.Write_StrBytes("8B 4D A4");

            //Inject it
            CaveMemory.InjectToOffset(_NoCursor_InjectionStruct, "No Cursor");
        }

        #endregion        

        #region Inputs Hack

        protected override void Apply_InputsMemoryHack()
        {
            Create_InputsDataBank();            
            _Axes_CaveAddress = InputsDatabank_Address;
            _GunButtons_CaveAddress = InputsDatabank_Address + 0x20;
            _SystemButtons_CaveAddress = InputsDatabank_Address + 0x50;
            _OutOfScreen_CaveAddress = InputsDatabank_Address + 0x58;
            _CoinFlag_CaveAddress = InputsDatabank_Address + 0x5C;

            SetHack_Inputs();

            //Nopping a jump so that player 2 has our axis values taken 
            SetNops(GameProcess_ModuleBaseAddress, _Nop_EnableSecondPlayerAxis);

            SetHack_Coins();

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        /// <summary>
        /// At the end of a while() loop where the game is setting buttons and axis
        /// Overwritting values with our own
        /// </summary>
        private void SetHack_Inputs()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //push esi
            CaveMemory.Write_StrBytes("56");
            //push edi
            CaveMemory.Write_StrBytes("57");
            //push ecx
            CaveMemory.Write_StrBytes("51");

            /** Copying the whole Memory block with final axis values + Gun Buttons values ***/
            //mov esi,_Axes_CaveAddress
            CaveMemory.Write_StrBytes("BE");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Axes_CaveAddress));
            //mov edi,ecx
            CaveMemory.Write_StrBytes("8B F9");
            //add edi,40
            CaveMemory.Write_StrBytes("83 C7 40");
            //mov ecx,00000014
            CaveMemory.Write_StrBytes("B9 14 00 00 00");
            //repe movsd
            CaveMemory.Write_StrBytes("F3 A5");

            /** Resetting the events flag in the custom DataBank **/
            //mov esi,_GunButtons_CaveAddress+10
            CaveMemory.Write_StrBytes("BE");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_GunButtons_CaveAddress + 0x10));
            //xor ecx,ecx
            CaveMemory.Write_StrBytes("31 C9");
            //ResetFlag:
            //mov byte ptr [esi],00
            CaveMemory.Write_StrBytes("C6 06 00");
            //add esi,04
            CaveMemory.Write_StrBytes("83 C6 04");
            //inc ecx
            CaveMemory.Write_StrBytes("41");
            //cmp ecx,08
            CaveMemory.Write_StrBytes("83 F9 08");
            //jl ResetFlag
            CaveMemory.Write_StrBytes("7C F4");

            /** Overriding Out/On Screen values **/
            //mov esi,_OutOfScreen_CaveAddress
            CaveMemory.Write_StrBytes("BE");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_OutOfScreen_CaveAddress));
            //add edi,00000080
            CaveMemory.Write_StrBytes("81 C7 80 00 00 00");
            //mov ecx,00000002
            CaveMemory.Write_StrBytes("B9 02 00 00 00");
            //repe movsd
            CaveMemory.Write_StrBytes("F3 A5");

            /** Overriding System Buttons value **/
            //mov edx,[_SystemButtons_CaveAddress]
            CaveMemory.Write_StrBytes("8B 15");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_SystemButtons_CaveAddress));    

            //pop ecx
            CaveMemory.Write_StrBytes("59");
            //pop edi
            CaveMemory.Write_StrBytes("5F");
            //pop esi
            CaveMemory.Write_StrBytes("5E");
            
            //mov [ecx+00000128],edx
            CaveMemory.Write_StrBytes("89 91 28 01 00 00");

            //Inject it
            CaveMemory.InjectToOffset(_Inputs_InjectionStruct, "Inputs");
        }

        /// <summary>
        /// Setting a Coin-In flag manually instead of callin a Function
        /// </summary>
        private void SetHack_Coins()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //movzx eax,byte ptr [_CoinFlag_CaveAddress]
            CaveMemory.Write_StrBytes("0F B6 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_CoinFlag_CaveAddress));

            //mov byte ptr [_CoinFlag_CaveAddress],00
            CaveMemory.Write_StrBytes("C6 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_CoinFlag_CaveAddress));
            CaveMemory.Write_StrBytes("00");

            //Inject it
            CaveMemory.InjectToOffset(_Coins_Injectionstruct, "Coins");

            return;
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
            //Jumping over the draw part of the reticle (forcing test false)
            WriteByte(GameProcess_ModuleBaseAddress + _NoCrosshair_Patch_Offset, 0xEB);

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
            else if (e.ButtonName.Equals("CONFIRM"))
            {
                Apply_OR_ByteMask(_SystemButtons_CaveAddress + 2, 0x80);
            }
            else if (e.ButtonName.Equals("LEFT"))
            {
                Apply_OR_ByteMask(_SystemButtons_CaveAddress + 2, 0x08);
            }
            else if (e.ButtonName.Equals("RIGHT"))
            {
                Apply_OR_ByteMask(_SystemButtons_CaveAddress + 2, 0x04);
            }
            else if (e.ButtonName.Equals("ENTER"))
            {
                Apply_OR_ByteMask(_SystemButtons_CaveAddress + 1, 0x02);
            }
            else if (e.ButtonName.Equals("SELECT"))
            {
                Apply_OR_ByteMask(_SystemButtons_CaveAddress + 1, 0x04);
            }
            else if (e.ButtonName.Equals("COIN"))
            {
                Apply_OR_ByteMask(_SystemButtons_CaveAddress + 1, 0x01);
                WriteByte(_CoinFlag_CaveAddress, 0x01);
            }
            else if (e.ButtonName.Equals("SERVICE"))
            {
                Apply_OR_ByteMask(_SystemButtons_CaveAddress + 1, 0x08);
            }
            else if (e.ButtonName.Equals("TEST"))
            {
                Apply_OR_ByteMask(_SystemButtons_CaveAddress + 3, 0x80);
            }
        }

        protected override void GameController_OnButtonUp(object sender, VirtualGameControler.ButtonDownEventArgs e)
        {
            if (e.ButtonName.Equals("CONFIRM"))
            {
                Apply_AND_ByteMask(_SystemButtons_CaveAddress + 2, 0x7F);
            }
            else if (e.ButtonName.Equals("LEFT"))
            {
                Apply_AND_ByteMask(_SystemButtons_CaveAddress + 2, 0xF7);
            }
            else if (e.ButtonName.Equals("RIGHT"))
            {
                Apply_AND_ByteMask(_SystemButtons_CaveAddress + 2, 0xFB);
            }
            else if (e.ButtonName.Equals("ENTER"))
            {
                Apply_AND_ByteMask(_SystemButtons_CaveAddress + 1, 0xFD);
            }
            else if (e.ButtonName.Equals("SELECT"))
            {
                Apply_AND_ByteMask(_SystemButtons_CaveAddress + 1, 0xFB);
            }
            else if (e.ButtonName.Equals("COIN"))
            {
                Apply_AND_ByteMask(_SystemButtons_CaveAddress + 1, 0xFE);
            }
            else if (e.ButtonName.Equals("SERVICE"))
            {
                Apply_AND_ByteMask(_SystemButtons_CaveAddress + 1, 0xF7);
            }
            else if (e.ButtonName.Equals("TEST"))
            {
                Apply_AND_ByteMask(_SystemButtons_CaveAddress + 3, 0x7F);
            }
        }

        protected override void UpdateAxis(PlayerID Id, int X, int Y)
        {
            if (Id != PlayerID.None)
            {
                byte[] bufferX = BitConverter.GetBytes((float)X);
                byte[] bufferY = BitConverter.GetBytes((float)Y);

                WriteBytes(_Axes_CaveAddress + (uint)Id * 0x10, bufferX);
                WriteBytes(_Axes_CaveAddress + 4 + (uint)Id * 0x10, bufferY);
            }
        }

        protected override void PlayerController_OnButtonDown(object sender, VirtualGameControler.ButtonDownEventArgs e)
        {
            if (e.Id != PlayerID.None)
            {
                UInt32 PlayerIndex = (UInt32)e.Id;

                //Game needs 3 states for each buttons :
                // - Pressed event
                // - State
                // - Released event
                if (e.ButtonName.Equals("TRIGGER"))
                {
                    WriteByte(_GunButtons_CaveAddress + (PlayerIndex * 0x08), 0x01);        //Button state
                    WriteByte(_GunButtons_CaveAddress + 0x10 + (PlayerIndex * 0x08), 0x01); //KeyDown
                }
                else if (e.ButtonName.Equals("OFF-SCREEN TRIGGER"))
                {
                    WriteByte(_OutOfScreen_CaveAddress + (PlayerIndex * 0x04), 0x01);       //Out Of Screen
                    WriteByte(_GunButtons_CaveAddress + (PlayerIndex * 0x08), 0x01);        //Button state
                    WriteByte(_GunButtons_CaveAddress + 0x10 + (PlayerIndex * 0x08), 0x01); //KeyDown
                }
                else if (e.ButtonName.Equals("ZAPPER"))
                {
                    WriteByte(_GunButtons_CaveAddress + 4 + (PlayerIndex * 0x08), 0x01);    //Button state
                    WriteByte(_GunButtons_CaveAddress + 0x14 + (PlayerIndex * 0x08), 0x01); //KeyDown
                    
                }
            }
        }
        protected override void PlayerController_OnButtonUp(object sender, VirtualGameControler.ButtonDownEventArgs e)
        {
            if (e.Id != PlayerID.None)
            {
                UInt32 PlayerIndex = (UInt32)e.Id;

                //Game needs 3 states for each buttons :
                // - Pressed event
                // - State
                // - Released event
                if (e.ButtonName.Equals("TRIGGER"))
                {
                    WriteByte(_GunButtons_CaveAddress + (PlayerIndex * 0x08), 0x00);        //Button state
                    WriteByte(_GunButtons_CaveAddress + 0x20 + (PlayerIndex * 0x08), 0x01); //KeyUp
                }
                else if (e.ButtonName.Equals("OFF-SCREEN TRIGGER"))
                {
                    WriteByte(_OutOfScreen_CaveAddress + (PlayerIndex * 0x04), 0x00);       //On Screen
                    WriteByte(_GunButtons_CaveAddress + (PlayerIndex * 0x08), 0x00);        //Button state
                    WriteByte(_GunButtons_CaveAddress + 0x20 + (PlayerIndex * 0x08), 0x01); //KeyUp
                }
                else if (e.ButtonName.Equals("ZAPPER"))
                {
                    WriteByte(_GunButtons_CaveAddress + 4 + (PlayerIndex * 0x08), 0x00);    //Button state
                    WriteByte(_GunButtons_CaveAddress + 0x24 + (PlayerIndex * 0x08), 0x01); //KeyUp                    
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
                new GameOutput(OutputId.P1_Lmp_B),
                new GameOutput(OutputId.P2_Lmp_B),
                new GameOutput(OutputId.P1_Lmp_R),
                new GameOutput(OutputId.P2_Lmp_R),
                new GameOutput(OutputId.LmpLBtn),
                new GameOutput(OutputId.LmpMBtn),
                new GameOutput(OutputId.LmpRBtn),
                new GameOutput(OutputId.Credits)
            };
        }

        /// <summary>
        /// Update all Outputs values before sending them to MameHooker
        /// </summary>
        public override void UpdateOutputValues()
        {
            UInt32 OutputsBaseAddress = ReadPtr(GameProcess_ModuleBaseAddress + _Outputs_Ptr_Offset);
            if (OutputsBaseAddress != 0)
            {
                byte Outputs1 = ReadByte(OutputsBaseAddress + 1);
                byte Outputs2 = ReadByte(OutputsBaseAddress + 2);

                SetOutputValue(OutputId.P1_Lmp_B, Outputs1 >> 7 & 0x01);
                SetOutputValue(OutputId.P2_Lmp_B, Outputs1 >> 6 & 0x01);
                SetOutputValue(OutputId.P1_Lmp_R, Outputs1 >> 5 & 0x01);
                SetOutputValue(OutputId.P2_Lmp_R, Outputs1 >> 4 & 0x01);
                SetOutputValue(OutputId.LmpLBtn, Outputs2 >> 5 & 0x01);
                SetOutputValue(OutputId.LmpMBtn, Outputs2 >> 6 & 0x01);
                SetOutputValue(OutputId.LmpRBtn, Outputs2 >> 4 & 0x01);
            }

            UInt32 CreditsBaseAddress = ReadPtr(GameProcess_ModuleBaseAddress + _Credits_Ptr_Offset);
            if (CreditsBaseAddress != 0) 
            {
                SetOutputValue(OutputId.Credits, ReadInt32(CreditsBaseAddress + 0x18));
            }
        }

        #endregion
    }
}
