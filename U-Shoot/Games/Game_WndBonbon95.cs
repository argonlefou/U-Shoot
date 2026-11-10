using System;
using System.Collections.Generic;
using System.Reflection;
using UsCore.Config;
using UsCore.Launcher;
using UsCore.Logging;
using UsCore.MameOutput;
using UsCore.Memory;
using UsCore.VirtualControllers;

namespace U_Shoot.Games
{
    public class Game_WndBonbon95 : BaseGame32
    {
        protected override bool PATCH_PEB_DEBUG_FLAG { get { return true; } }

        #region LOADER MEMORY ADDRESSES
        private InjectionStruct32 _NoCrosshair_InjectionStruct = new InjectionStruct32(0x2D63A, 11);
        #endregion

        #region INPUTS MEMORY ADDRESSES 
        private UInt32 _P1_ControlType_Offset = 0x00049DE1;
        private UInt32 _P2_ControlType_Offset = 0x00049DE5;
        private UInt32 _TextureDrawingCategoryIndex_Offset = 0x0004BFB4;
        private InjectionStruct32 _P1_MouseControls_InjectionStruct = new InjectionStruct32(0x00004105, 5);
        private InjectionStruct32 _P2_MouseControls_InjectionStruct = new InjectionStruct32(0x00004739, 5);
        private InjectionStruct32 _P1_Recoil_InjectionStruct = new InjectionStruct32(0x00009393, 6);
        private InjectionStruct32 _P2_Recoil_InjectionStruct = new InjectionStruct32(0x00009147, 6);

        //Custom Data
        private UInt32 _P1_Buttons_CaveAddress = 0;
        private UInt32 _P1_X_CaveAddress = 0;
        private UInt32 _P1_Y_CaveAddress = 0;
        private UInt32 _P2_Buttons_CaveAddress = 0;
        private UInt32 _P2_X_CaveAddress = 0;
        private UInt32 _P2_Y_CaveAddress = 0;
        #endregion

        #region OUTPUTS MEMORY ADDRESSES
        private UInt32 _Credits_Offset = 0x00049FE9;

        //Custom Data
        private UInt32 _P1_RecoilStatus_CaveAddress = 0;
        private UInt32 _P2_RecoilStatus_CaveAddress = 0;
        #endregion

        public Game_WndBonbon95(GameConfig Config) : base(Config)
        {
            _KnownMd5Prints.Add("BONBON v1.04 - Original exe", "cd2d16ab00750d4c2ddf010aa5402407");
            if (_GameConfig.Windowed)
                _GameLauncher.Set_CmdLine_Args("-dx");
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
                    double dMaxX = 640.0;
                    double dMaxY = 480.0;
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


            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }
        #endregion        

        #region Inputs Hack

        protected override void Apply_InputsMemoryHack()
        {
            Create_InputsDataBank();
            _P1_Buttons_CaveAddress = InputsDatabank_Address;
            _P1_X_CaveAddress = InputsDatabank_Address + 4;
            _P1_Y_CaveAddress = InputsDatabank_Address + 8;
            _P2_Buttons_CaveAddress = InputsDatabank_Address + 0x10;
            _P2_X_CaveAddress = InputsDatabank_Address + 0x14;
            _P2_Y_CaveAddress = InputsDatabank_Address + 0x18;

            //Force the game to act as if MOUSE was choosen in the Players Controls reading function
            // Keyboard = 0
            // Mouse = 1
            // Other ? To check...
            WriteByte(GameProcess_ModuleBaseAddress + _P1_ControlType_Offset, 1);
            WriteByte(GameProcess_ModuleBaseAddress + _P2_ControlType_Offset, 1);

            //Create custom function to handle MOUSE controls update and call them
            SetHack_P1Controls();
            SetHack_P2Controls();


            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        /// <summary>
        /// Creating a function that will replace the existing one to update buttons + axis values
        /// sub_4321EA(_DWORD *a1, _DWORD *a2, _DWORD *a3)
        /// a1 = Buttons
        /// a2 = X
        /// a3 = Y
        /// </summary>
        private void SetHack_P1Controls()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //push eax
            CaveMemory.Write_StrBytes("50");
            //push ebx
            CaveMemory.Write_StrBytes("53");
            //mov ebx, [_P1_Buttons_CaveAddress]
            CaveMemory.Write_StrBytes("8B 1D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_Buttons_CaveAddress));
            //mov eax,[esp+08]
            CaveMemory.Write_StrBytes("8B 44 24 08");
            //mov [eax],ebx
            CaveMemory.Write_StrBytes("89 18");

            //mov ebx, [_P1_X_CaveAddress]
            CaveMemory.Write_StrBytes("8B 1D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_X_CaveAddress));
            //mov eax,[esp+0C]
            CaveMemory.Write_StrBytes("8B 44 24 0C");
            //mov [eax],ebx
            CaveMemory.Write_StrBytes("89 18");

            //mov ebx, [_P1_Y_CaveAddress]
            CaveMemory.Write_StrBytes("8B 1D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_Y_CaveAddress));
            //mov eax,[esp+10]
            CaveMemory.Write_StrBytes("8B 44 24 10");
            //mov [eax],ebx
            CaveMemory.Write_StrBytes("89 18");

            //pop ebx
            CaveMemory.Write_StrBytes("5B");
            //pop eax
            CaveMemory.Write_StrBytes("58");

            //Inject it
            CaveMemory.InjectToOffset(_P1_MouseControls_InjectionStruct, "P1 Controls");
        }
        private void SetHack_P2Controls()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //push eax
            CaveMemory.Write_StrBytes("50");
            //push ebx
            CaveMemory.Write_StrBytes("53");
            //mov ebx, [_P2_Buttons_CaveAddress]
            CaveMemory.Write_StrBytes("8B 1D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P2_Buttons_CaveAddress));
            //mov eax,[esp+08]
            CaveMemory.Write_StrBytes("8B 44 24 08");
            //mov [eax],ebx
            CaveMemory.Write_StrBytes("89 18");

            //mov ebx, [_P2_X_CaveAddress]
            CaveMemory.Write_StrBytes("8B 1D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P2_X_CaveAddress));
            //mov eax,[esp+0C]
            CaveMemory.Write_StrBytes("8B 44 24 0C");
            //mov [eax],ebx
            CaveMemory.Write_StrBytes("89 18");

            //mov ebx, [_P2_Y_CaveAddress]
            CaveMemory.Write_StrBytes("8B 1D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P2_Y_CaveAddress));
            //mov eax,[esp+10]
            CaveMemory.Write_StrBytes("8B 44 24 10");
            //mov [eax],ebx
            CaveMemory.Write_StrBytes("89 18");

            //pop ebx
            CaveMemory.Write_StrBytes("5B");
            //pop eax
            CaveMemory.Write_StrBytes("58");

            //Inject it
            CaveMemory.InjectToOffset(_P2_MouseControls_InjectionStruct, "P2 Controls");
        }

        #endregion

        #region Outputs Hack

        protected override void Apply_OutputsMemoryHack()
        {
            Create_OutputsDataBank();
            _P1_RecoilStatus_CaveAddress = OutputsDatabank_Address;
            _P2_RecoilStatus_CaveAddress = OutputsDatabank_Address + 4;

            SetHack_Recoil_P1();
            SetHack_Recoil_P2();

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        /// <summary>
        /// Intercepting some kind of call to draw impact texture when trigger is pressed
        /// </summary>
        private void SetHack_Recoil_P1()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //mov byte ptr[_P1_RecoilStatus_CaveAddress],00000001
            CaveMemory.Write_StrBytes("C6 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_RecoilStatus_CaveAddress));
            CaveMemory.Write_StrBytes("01");
            //mov eax,[ebp+14]
            CaveMemory.Write_StrBytes("8B 45 14");
            //push [eax+58]
            CaveMemory.Write_StrBytes("FF 70 58");

            //Inject it
            CaveMemory.InjectToOffset(_P1_Recoil_InjectionStruct, "P1 Recoil");
        }

        /// <summary>
        /// Intercepting some kind of call to draw impact texture when trigger is pressed
        /// </summary>
        private void SetHack_Recoil_P2()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //mov byte ptr[_P1_RecoilStatus_CaveAddress],00000001
            CaveMemory.Write_StrBytes("C6 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P2_RecoilStatus_CaveAddress));
            CaveMemory.Write_StrBytes("01");
            //mov eax,[ebp+14]
            CaveMemory.Write_StrBytes("8B 45 14");
            //push [eax+58]
            CaveMemory.Write_StrBytes("FF 70 58");

            //Inject it
            CaveMemory.InjectToOffset(_P2_Recoil_InjectionStruct, "P2 Recoil");
        }
        #endregion

        #region No Crosshair Hack

        /// <summary>
        /// To remove crosshair, we can force out-of -screen corrdinates when the game is drawing the needed texture
        /// To find the texture, it looks like the _TextureDrawingCategoryIndex_Offset is 0x11 when the function is called to draw crosshair-related resources
        /// And the ECX value (Texture ID ?) is 0x17 and 0x1C for Impact texture (untouched)
        /// Other Id (0x192, 0x193, etc..) will be changed to not be visible
        /// </summary>
        protected override void Apply_NoCrosshairMemoryHack()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //cmp dword ptr [Main95.exe+4BFB4],11
            CaveMemory.Write_StrBytes("83 3D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(GameProcess_ModuleBaseAddress + _TextureDrawingCategoryIndex_Offset));
            CaveMemory.Write_StrBytes("11");
            //jne Originalcode
            CaveMemory.Write_StrBytes("75 11");
            //cmp ecx, 17
            CaveMemory.Write_StrBytes("83 F9 1C");
            //je OriginalCode
            CaveMemory.Write_StrBytes("74 0C");
            //cmp ecx, 1C
            CaveMemory.Write_StrBytes("83 F9 17");
            //je OriginalCode
            CaveMemory.Write_StrBytes("74 07");
            //Patch:
            //mov eax, 3000
            CaveMemory.Write_StrBytes("B8 00 03 00 00");
            //jmp Next
            CaveMemory.Write_StrBytes("EB 03");
            //OriginalCode:
            //mov eax,[ebx+54]
            CaveMemory.Write_StrBytes("8B 43 54");
            //mov [edi],eax
            CaveMemory.Write_StrBytes("89 07");
            //mov eax,[ebx+58]
            CaveMemory.Write_StrBytes("8B 43 58");
            //mov [edi+04],eax
            CaveMemory.Write_StrBytes("89 47 04");

            //Inject it
            CaveMemory.InjectToOffset(_NoCrosshair_InjectionStruct, "No Crosshair");

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
                int Coins = ReadInt32(GameProcess_ModuleBaseAddress + _Credits_Offset);
                Coins++;
                WriteBytes(GameProcess_ModuleBaseAddress + _Credits_Offset, BitConverter.GetBytes(Coins));
            }
            else if (e.ButtonName.Equals("TEST"))
            {

            }
        }

        protected override void GameController_OnButtonUp(object sender, VirtualGameControler.ButtonDownEventArgs e)
        {
            if (e.ButtonName.Equals("TEST"))
            {

            }
        }

        protected override void UpdateAxis(PlayerID Id, int X, int Y)
        {
            if (Id != PlayerID.None)
            {
                byte[] bufferX = BitConverter.GetBytes((UInt32)X);
                byte[] bufferY = BitConverter.GetBytes((UInt32)Y);

                WriteBytes(_P1_X_CaveAddress + ((uint)Id * 0x10), bufferX);
                WriteBytes(_P1_Y_CaveAddress + ((uint)Id * 0x10), bufferY);
            }
        }

        protected override void PlayerController_OnButtonDown(object sender, VirtualGameControler.ButtonDownEventArgs e)
        {
            if (e.Id != PlayerID.None)
            {
                UInt32 PlayerIndex = (UInt32)e.Id;

                if (e.ButtonName.Equals("TRIGGER"))
                {
                    Apply_OR_ByteMask(_P1_Buttons_CaveAddress + (PlayerIndex * 0x10), 0x01);
                }
                else if (e.ButtonName.Equals("RELOAD"))
                {
                    Apply_OR_ByteMask(_P1_Buttons_CaveAddress + (PlayerIndex * 0x10), 0x02);
                }
                else if (e.ButtonName.Equals("START"))
                {

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
                    Apply_AND_ByteMask(_P1_Buttons_CaveAddress + (PlayerIndex * 0x10), 0xFE);
                }
                else if (e.ButtonName.Equals("RELOAD"))
                {
                    Apply_AND_ByteMask(_P1_Buttons_CaveAddress + (PlayerIndex * 0x10), 0xFD);
                }
                else if (e.ButtonName.Equals("START"))
                {

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
            _Outputs = new List<GameOutput>();
        }

        /// <summary>
        /// Update all Outputs values before sending them to MameHooker
        /// </summary>
        public override void UpdateOutputValues()
        {
        }

        #endregion
    }
}
