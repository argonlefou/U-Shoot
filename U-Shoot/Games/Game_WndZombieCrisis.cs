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
    public class Game_WndZombieCrisis : BaseGame64
    {
        protected override bool PATCH_PEB_DEBUG_FLAG { get { return false; } }

        private const string _GAMEPORTDLL_Name = "gameport.dll";
        private UInt64 _GamePortDll_BaseAddress;

        private UInt64 _RetZero_CustomFunc_Address = 0;
        private UInt64 _RetOne_CustomFunc_Address = 0;
        private UInt64 _GetSettingKey_CustomFunc_Address = 0;
        private UInt64 _GetLeftKey_CustomFunc_Address = 0;
        private UInt64 _GetRightKey_CustomFunc_Address = 0;
        private UInt64[] _ClearBufferCoin_CustomFunc_Addresses = new UInt64[4];
        private UInt64[] _GetBufferCoin_CustomFunc_Addresses = new UInt64[4];
        private UInt64[] _SubBufferCoin_CustomFunc_Addresses = new UInt64[4];
        private UInt64[] _GetStartKey_CustomFunc_Addresses = new UInt64[4];
        private UInt64[] _GetFrie1_CustomFunc_Addresses = new UInt64[4];
        private UInt64[] _GetFrie2_CustomFunc_Addresses = new UInt64[4];
        private UInt64[] _GetWadcX_CustomFunc_Addresses = new UInt64[4];
        private UInt64[] _GetWadcY_CustomFunc_Addresses = new UInt64[4];
        private UInt64[] _SetStartButtonLight_CustomFunc_Addresses = new UInt64[4];
        private UInt64[] _SetLightbelt_CustomFunc_Addresses = new UInt64[4];
        private UInt64[] _SetElecShake_CustomFunc_Addresses = new UInt64[4];

        #region LOADER MEMORY ADDRESSES                

        #endregion

        #region INPUTS MEMORY ADDRESSES    
        private UInt64 _GamePort_Dll_vfTable_Offset = 0x0002281A8;
        private InjectionStruct64 _MouseKbdDisable_InjectionStruct = new InjectionStruct64(0x1B973D2, 14);

        //Custom Data
        private UInt64 _Coins_Address = 0;
        private UInt64 _StartButtons_Address = 0;
        private UInt64 _FireButtons_Address = 0;
        private UInt64 _Axes_X_Address = 0;
        private UInt64 _Axes_Y_Address = 0;
        private UInt64 _SettingButton_Address = 0;
        private UInt64 _LeftButton_Address = 0;
        private UInt64 _RightButton_Address = 0;
        #endregion

        #region OUTPUTS MEMORY ADDRESSES

        //Custom Data
        private UInt64 _StartLamps_Address = 0;
        private UInt64 _LightBelts_Address = 0;
        private UInt64 _Recoil_Address = 0;
        #endregion

        public Game_WndZombieCrisis(GameConfig Config) : base(Config)
        {
            _KnownMd5Prints.Add("Zombie Crisis - SpiderZSoft unpacked binary", "bf716185268e4096466d9dd95dff94c3");
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
                if (string.Equals(Path.GetFileName(e.DllPath), _GAMEPORTDLL_Name, StringComparison.OrdinalIgnoreCase))
                {
                    Logger.WriteLog("==> Wanted DLL found !");
                    //HANDLE should always be in 32Bits range (https://learn.microsoft.com/fr-fr/windows/win32/winprog64/interprocess-communication?redirectedfrom=MSDN)
                    Logger.WriteLog("  + hModule: 0x" + e.LoadDllInfo.hFile.ToString("X8"));
                    Logger.WriteLog("  + fUnicode: " + e.LoadDllInfo.fUnicode.ToString());

                    _GamePortDll_BaseAddress = (UInt64)e.LoadDllInfo.lpBaseOfDll;

                    if (!_GameConfig.NoInputs)
                        SetHack_GamePortDll_Input();

                    SetHack_GamePortDll_Outputs();

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
                    double dMaxX = 1279.0;//1919.0;
                    double dMaxY = 779.0;// 1079.0;
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

        protected override void Apply_LoaderMemoryHack()
        {  }        

        #endregion

        #region Inputs Hack

        protected override void Apply_InputsMemoryHack()
        {
            Create_InputsDataBank();
            _Coins_Address = InputsDatabank_Address;
            _StartButtons_Address = InputsDatabank_Address + 0x04;
            _FireButtons_Address = InputsDatabank_Address + 0x08;
            _Axes_X_Address = InputsDatabank_Address + 0x10;
            _Axes_Y_Address = InputsDatabank_Address + 0x20;
            _SettingButton_Address = InputsDatabank_Address + 0x30;
            _LeftButton_Address = InputsDatabank_Address + 0x31;
            _RightButton_Address = InputsDatabank_Address + 0x32;

            SetHack_DisableMouseAndKeyboard();

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        /// <summary>
        /// Game uses WndProc messages to control game with mouse cursor and keyboard.
        /// Removing that so that distinct lightguns axes are not disturbed by mouse cursor
        /// </summary>
        private void SetHack_DisableMouseAndKeyboard()
        {
            Codecave64 CaveMemory = new Codecave64(_Process, GameProcess_ModuleBaseAddress);

            //cmp r8d,00000100
            CaveMemory.Write_StrBytes("41 81 F8 00 01 00 00");
            //je Disable
            CaveMemory.Write_StrBytes("74 12");
            //cmp r8d,00000200
            CaveMemory.Write_StrBytes("41 81 F8 00 02 00 00");
            //jl Exit
            CaveMemory.Write_StrBytes("7C 0C");
            //cmp r8d,0000020D
            CaveMemory.Write_StrBytes("41 81 F8 0D 02 00 00");
            //ja Exit
            CaveMemory.Write_StrBytes("77 03");

            //Disable:
            //xor r9,r9
            CaveMemory.Write_StrBytes("4D 31 C9");

            //Exit:
            //movsxd  r13,r8d
            CaveMemory.Write_StrBytes("4D 63 E8");
            //mov r15,r9
            CaveMemory.Write_StrBytes("4D 8B F9");
            //mov r8,r12
            CaveMemory.Write_StrBytes("4D 8B C4");
            //movaps [r11
            CaveMemory.Write_StrBytes("41 0F 29 73 A8");

            //Inject it
            CaveMemory.InjectToOffset(_MouseKbdDisable_InjectionStruct, "WndProc");
        }


        /// <summary>
        /// Patch the IO Dll when it's loaded by the game
        /// </summary>
        private void SetHack_GamePortDll_Input()
        {
            Create_CustomInputsFunctions();

            //First Step : replacing all function by a "Ret 0" generic function
            for (UInt64 i = 1; i < 113; i++)
            {
                ReplaceReadOnlyString(_GamePortDll_BaseAddress, new UInt64[] { _GamePort_Dll_vfTable_Offset + (8 * i) }, BitConverter.GetBytes(_RetZero_CustomFunc_Address));
            }

            //Init() needs to return one
            ReplaceReadOnlyString(_GamePortDll_BaseAddress, new UInt64[] { _GamePort_Dll_vfTable_Offset + 0x348 }, BitConverter.GetBytes(_RetOne_CustomFunc_Address));

            //Then Custom Coin functions
            ReplaceReadOnlyString(_GamePortDll_BaseAddress, new UInt64[] { _GamePort_Dll_vfTable_Offset + 0x88 }, BitConverter.GetBytes(_ClearBufferCoin_CustomFunc_Addresses[0]));
            ReplaceReadOnlyString(_GamePortDll_BaseAddress, new UInt64[] { _GamePort_Dll_vfTable_Offset + 0x98 }, BitConverter.GetBytes(_ClearBufferCoin_CustomFunc_Addresses[1]));
            ReplaceReadOnlyString(_GamePortDll_BaseAddress, new UInt64[] { _GamePort_Dll_vfTable_Offset + 0xA8 }, BitConverter.GetBytes(_ClearBufferCoin_CustomFunc_Addresses[2]));
            ReplaceReadOnlyString(_GamePortDll_BaseAddress, new UInt64[] { _GamePort_Dll_vfTable_Offset + 0xB8 }, BitConverter.GetBytes(_ClearBufferCoin_CustomFunc_Addresses[3]));

            ReplaceReadOnlyString(_GamePortDll_BaseAddress, new UInt64[] { _GamePort_Dll_vfTable_Offset + 0x08 }, BitConverter.GetBytes(_GetBufferCoin_CustomFunc_Addresses[0]));
            ReplaceReadOnlyString(_GamePortDll_BaseAddress, new UInt64[] { _GamePort_Dll_vfTable_Offset + 0x40 }, BitConverter.GetBytes(_GetBufferCoin_CustomFunc_Addresses[1]));
            ReplaceReadOnlyString(_GamePortDll_BaseAddress, new UInt64[] { _GamePort_Dll_vfTable_Offset + 0x58 }, BitConverter.GetBytes(_GetBufferCoin_CustomFunc_Addresses[2]));
            ReplaceReadOnlyString(_GamePortDll_BaseAddress, new UInt64[] { _GamePort_Dll_vfTable_Offset + 0x70 }, BitConverter.GetBytes(_GetBufferCoin_CustomFunc_Addresses[3]));

            ReplaceReadOnlyString(_GamePortDll_BaseAddress, new UInt64[] { _GamePort_Dll_vfTable_Offset + 0x10 }, BitConverter.GetBytes(_SubBufferCoin_CustomFunc_Addresses[0]));
            ReplaceReadOnlyString(_GamePortDll_BaseAddress, new UInt64[] { _GamePort_Dll_vfTable_Offset + 0x48 }, BitConverter.GetBytes(_SubBufferCoin_CustomFunc_Addresses[1]));
            ReplaceReadOnlyString(_GamePortDll_BaseAddress, new UInt64[] { _GamePort_Dll_vfTable_Offset + 0x60 }, BitConverter.GetBytes(_SubBufferCoin_CustomFunc_Addresses[2]));
            ReplaceReadOnlyString(_GamePortDll_BaseAddress, new UInt64[] { _GamePort_Dll_vfTable_Offset + 0x78 }, BitConverter.GetBytes(_SubBufferCoin_CustomFunc_Addresses[3]));

            //Then Custom Start Buttons functions
            ReplaceReadOnlyString(_GamePortDll_BaseAddress, new UInt64[] { _GamePort_Dll_vfTable_Offset + 0x218 }, BitConverter.GetBytes(_GetStartKey_CustomFunc_Addresses[0]));
            ReplaceReadOnlyString(_GamePortDll_BaseAddress, new UInt64[] { _GamePort_Dll_vfTable_Offset + 0x220 }, BitConverter.GetBytes(_GetStartKey_CustomFunc_Addresses[1]));
            ReplaceReadOnlyString(_GamePortDll_BaseAddress, new UInt64[] { _GamePort_Dll_vfTable_Offset + 0x228 }, BitConverter.GetBytes(_GetStartKey_CustomFunc_Addresses[2]));
            ReplaceReadOnlyString(_GamePortDll_BaseAddress, new UInt64[] { _GamePort_Dll_vfTable_Offset + 0x230 }, BitConverter.GetBytes(_GetStartKey_CustomFunc_Addresses[3]));

            //Then Fire buttons
            ReplaceReadOnlyString(_GamePortDll_BaseAddress, new UInt64[] { _GamePort_Dll_vfTable_Offset + 0x238 }, BitConverter.GetBytes(_GetFrie1_CustomFunc_Addresses[0]));
            ReplaceReadOnlyString(_GamePortDll_BaseAddress, new UInt64[] { _GamePort_Dll_vfTable_Offset + 0x240 }, BitConverter.GetBytes(_GetFrie1_CustomFunc_Addresses[1]));
            ReplaceReadOnlyString(_GamePortDll_BaseAddress, new UInt64[] { _GamePort_Dll_vfTable_Offset + 0x248 }, BitConverter.GetBytes(_GetFrie1_CustomFunc_Addresses[2]));
            ReplaceReadOnlyString(_GamePortDll_BaseAddress, new UInt64[] { _GamePort_Dll_vfTable_Offset + 0x268 }, BitConverter.GetBytes(_GetFrie1_CustomFunc_Addresses[3]));

            ReplaceReadOnlyString(_GamePortDll_BaseAddress, new UInt64[] { _GamePort_Dll_vfTable_Offset + 0x250 }, BitConverter.GetBytes(_GetFrie2_CustomFunc_Addresses[0]));
            ReplaceReadOnlyString(_GamePortDll_BaseAddress, new UInt64[] { _GamePort_Dll_vfTable_Offset + 0x258 }, BitConverter.GetBytes(_GetFrie2_CustomFunc_Addresses[1]));
            ReplaceReadOnlyString(_GamePortDll_BaseAddress, new UInt64[] { _GamePort_Dll_vfTable_Offset + 0x260 }, BitConverter.GetBytes(_GetFrie2_CustomFunc_Addresses[2]));
            //GetP4Frie2 Already return zero in code, no call to a vfTable function

            //Axes
            ReplaceReadOnlyString(_GamePortDll_BaseAddress, new UInt64[] { _GamePort_Dll_vfTable_Offset + 0x2C0 }, BitConverter.GetBytes(_GetWadcX_CustomFunc_Addresses[0]));
            ReplaceReadOnlyString(_GamePortDll_BaseAddress, new UInt64[] { _GamePort_Dll_vfTable_Offset + 0x2C8 }, BitConverter.GetBytes(_GetWadcX_CustomFunc_Addresses[1]));
            ReplaceReadOnlyString(_GamePortDll_BaseAddress, new UInt64[] { _GamePort_Dll_vfTable_Offset + 0x2F0 }, BitConverter.GetBytes(_GetWadcX_CustomFunc_Addresses[2]));
            ReplaceReadOnlyString(_GamePortDll_BaseAddress, new UInt64[] { _GamePort_Dll_vfTable_Offset + 0x300 }, BitConverter.GetBytes(_GetWadcX_CustomFunc_Addresses[3]));

            ReplaceReadOnlyString(_GamePortDll_BaseAddress, new UInt64[] { _GamePort_Dll_vfTable_Offset + 0x2D8 }, BitConverter.GetBytes(_GetWadcY_CustomFunc_Addresses[0]));
            ReplaceReadOnlyString(_GamePortDll_BaseAddress, new UInt64[] { _GamePort_Dll_vfTable_Offset + 0x2E0 }, BitConverter.GetBytes(_GetWadcY_CustomFunc_Addresses[1]));
            ReplaceReadOnlyString(_GamePortDll_BaseAddress, new UInt64[] { _GamePort_Dll_vfTable_Offset + 0x2F8 }, BitConverter.GetBytes(_GetWadcY_CustomFunc_Addresses[2]));
            ReplaceReadOnlyString(_GamePortDll_BaseAddress, new UInt64[] { _GamePort_Dll_vfTable_Offset + 0x308 }, BitConverter.GetBytes(_GetWadcY_CustomFunc_Addresses[3]));

            //Settings
            ReplaceReadOnlyString(_GamePortDll_BaseAddress, new UInt64[] { _GamePort_Dll_vfTable_Offset + 0x310 }, BitConverter.GetBytes(_GetSettingKey_CustomFunc_Address));
            ReplaceReadOnlyString(_GamePortDll_BaseAddress, new UInt64[] { _GamePort_Dll_vfTable_Offset + 0x320 }, BitConverter.GetBytes(_GetLeftKey_CustomFunc_Address));
            ReplaceReadOnlyString(_GamePortDll_BaseAddress, new UInt64[] { _GamePort_Dll_vfTable_Offset + 0x328 }, BitConverter.GetBytes(_GetRightKey_CustomFunc_Address));

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        private void Create_CustomInputsFunctions()
        {
            Codecave64 CaveMemory = new Codecave64(_Process, GameProcess_ModuleBaseAddress);

            /** Generic function returning 0 **/
            _RetZero_CustomFunc_Address = CaveMemory.CaveAddress + (UInt64)CaveMemory.CaveOffset;
            //xor rax, rax
            CaveMemory.Write_StrBytes("48 31 C0");
            //ret
            CaveMemory.Write_StrBytes("C3");

            /** Generic function returning 1 **/
            _RetOne_CustomFunc_Address = CaveMemory.CaveAddress + (UInt64)CaveMemory.CaveOffset;
            //xor rax, rax
            CaveMemory.Write_StrBytes("48 31 C0");
            //mov al, 01
            CaveMemory.Write_StrBytes("B0 01");
            //ret
            CaveMemory.Write_StrBytes("C3");

            /** Inputs functions **/
            for (UInt64 i = 0; i < 4; i++)
            {
                _ClearBufferCoin_CustomFunc_Addresses[i] = CaveMemory.CaveAddress + (UInt64)CaveMemory.CaveOffset;
                //mov rax, Address
                CaveMemory.Write_StrBytes("48 B8");
                CaveMemory.Write_Bytes(BitConverter.GetBytes(_Coins_Address + i));
                //mov byte ptr[rax], 0
                CaveMemory.Write_StrBytes("C6 00 00");
                //xor rax, rax
                CaveMemory.Write_StrBytes("48 31 C0");
                //ret
                CaveMemory.Write_StrBytes("C3");

                _GetBufferCoin_CustomFunc_Addresses[i] = CaveMemory.CaveAddress + (UInt64)CaveMemory.CaveOffset;
                //xor rax, rax
                CaveMemory.Write_StrBytes("48 31 C0");
                //mov al, [Address]
                CaveMemory.Write_StrBytes("A0");
                CaveMemory.Write_Bytes(BitConverter.GetBytes(_Coins_Address + i));
                //ret
                CaveMemory.Write_StrBytes("C3");

                _SubBufferCoin_CustomFunc_Addresses[i] = CaveMemory.CaveAddress + (UInt64)CaveMemory.CaveOffset;
                //xor rax, rax
                CaveMemory.Write_StrBytes("48 31 C0");
                //mov al, [Address]
                CaveMemory.Write_StrBytes("A0");
                CaveMemory.Write_Bytes(BitConverter.GetBytes(_Coins_Address + i));
                //dec al
                CaveMemory.Write_StrBytes("FE C8");
                //mov byte ptr[Address], al
                CaveMemory.Write_StrBytes("A2");
                CaveMemory.Write_Bytes(BitConverter.GetBytes(_Coins_Address + i));
                //xor rax, rax
                CaveMemory.Write_StrBytes("48 31 C0");
                //ret
                CaveMemory.Write_StrBytes("C3");

                _GetStartKey_CustomFunc_Addresses[i] = CaveMemory.CaveAddress + (UInt64)CaveMemory.CaveOffset;
                //xor rax, rax
                CaveMemory.Write_StrBytes("48 31 C0");
                //mov al, Address
                CaveMemory.Write_StrBytes("A0");
                CaveMemory.Write_Bytes(BitConverter.GetBytes(_StartButtons_Address + i));
                //ret
                CaveMemory.Write_StrBytes("C3");

                _GetFrie1_CustomFunc_Addresses[i] = CaveMemory.CaveAddress + (UInt64)CaveMemory.CaveOffset;
                //xor rax, rax
                CaveMemory.Write_StrBytes("48 31 C0");
                //mov al, [Address]
                CaveMemory.Write_StrBytes("A0");
                CaveMemory.Write_Bytes(BitConverter.GetBytes(_FireButtons_Address + i));
                //ret
                CaveMemory.Write_StrBytes("C3");

                _GetFrie2_CustomFunc_Addresses[i] = CaveMemory.CaveAddress + (UInt64)CaveMemory.CaveOffset;
                //xor rax, rax
                CaveMemory.Write_StrBytes("48 31 C0");
                //mov al, [Address]
                CaveMemory.Write_StrBytes("A0");
                CaveMemory.Write_Bytes(BitConverter.GetBytes(_FireButtons_Address + 4 + i));
                //ret
                CaveMemory.Write_StrBytes("C3");

                _GetWadcX_CustomFunc_Addresses[i] = CaveMemory.CaveAddress + (UInt64)CaveMemory.CaveOffset;
                //xor rax, rax
                CaveMemory.Write_StrBytes("48 31 C0");
                //mov eax, [Address]
                CaveMemory.Write_StrBytes("A1");
                CaveMemory.Write_Bytes(BitConverter.GetBytes(_Axes_X_Address + (4 * i)));
                //ret
                CaveMemory.Write_StrBytes("C3");

                _GetWadcY_CustomFunc_Addresses[i] = CaveMemory.CaveAddress + (UInt64)CaveMemory.CaveOffset;
                //xor rax, rax
                CaveMemory.Write_StrBytes("48 31 C0");
                //mov eax, [Address]
                CaveMemory.Write_StrBytes("A1");
                CaveMemory.Write_Bytes(BitConverter.GetBytes(_Axes_Y_Address + (4 * i)));
                //ret
                CaveMemory.Write_StrBytes("C3");

                _GetWadcY_CustomFunc_Addresses[i] = CaveMemory.CaveAddress + (UInt64)CaveMemory.CaveOffset;
                //xor rax, rax
                CaveMemory.Write_StrBytes("48 31 C0");
                //mov eax, [Address]
                CaveMemory.Write_StrBytes("A1");
                CaveMemory.Write_Bytes(BitConverter.GetBytes(_Axes_Y_Address + (4 * i)));
                //ret
                CaveMemory.Write_StrBytes("C3");
            }

            _GetSettingKey_CustomFunc_Address = CaveMemory.CaveAddress + (UInt64)CaveMemory.CaveOffset;
            //xor rax, rax
            CaveMemory.Write_StrBytes("48 31 C0");
            //mov al, [Address]
            CaveMemory.Write_StrBytes("A0");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_SettingButton_Address));
            //ret
            CaveMemory.Write_StrBytes("C3");

            _GetLeftKey_CustomFunc_Address = CaveMemory.CaveAddress + (UInt64)CaveMemory.CaveOffset;
            //xor rax, rax
            CaveMemory.Write_StrBytes("48 31 C0");
            //mov al, [Address]
            CaveMemory.Write_StrBytes("A0");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_LeftButton_Address));
            //ret
            CaveMemory.Write_StrBytes("C3");

            _GetRightKey_CustomFunc_Address = CaveMemory.CaveAddress + (UInt64)CaveMemory.CaveOffset;
            //xor rax, rax
            CaveMemory.Write_StrBytes("48 31 C0");
            //mov al, [Address]
            CaveMemory.Write_StrBytes("A0");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_RightButton_Address));
            //ret
            CaveMemory.Write_StrBytes("C3");
        }

        #endregion

        #region Outputs Hack

        protected override void Apply_OutputsMemoryHack()
        {
            Create_OutputsDataBank();
            _StartLamps_Address = OutputsDatabank_Address;
            _LightBelts_Address = OutputsDatabank_Address + 0x08;
            _Recoil_Address = OutputsDatabank_Address + 0x10;

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        private void SetHack_GamePortDll_Outputs()
        {
            Create_CustomOutputsFunctions();

            //Start Ligths
            ReplaceReadOnlyString(_GamePortDll_BaseAddress, new UInt64[] { _GamePort_Dll_vfTable_Offset + 0xC8 }, BitConverter.GetBytes(_SetStartButtonLight_CustomFunc_Addresses[0]));
            ReplaceReadOnlyString(_GamePortDll_BaseAddress, new UInt64[] { _GamePort_Dll_vfTable_Offset + 0xD0 }, BitConverter.GetBytes(_SetStartButtonLight_CustomFunc_Addresses[1]));
            ReplaceReadOnlyString(_GamePortDll_BaseAddress, new UInt64[] { _GamePort_Dll_vfTable_Offset + 0xD8 }, BitConverter.GetBytes(_SetStartButtonLight_CustomFunc_Addresses[2]));
            ReplaceReadOnlyString(_GamePortDll_BaseAddress, new UInt64[] { _GamePort_Dll_vfTable_Offset + 0xE0 }, BitConverter.GetBytes(_SetStartButtonLight_CustomFunc_Addresses[3]));

            //LightBelts
            ReplaceReadOnlyString(_GamePortDll_BaseAddress, new UInt64[] { _GamePort_Dll_vfTable_Offset + 0x1E8 }, BitConverter.GetBytes(_SetLightbelt_CustomFunc_Addresses[0]));
            ReplaceReadOnlyString(_GamePortDll_BaseAddress, new UInt64[] { _GamePort_Dll_vfTable_Offset + 0x1F0 }, BitConverter.GetBytes(_SetLightbelt_CustomFunc_Addresses[1]));
            ReplaceReadOnlyString(_GamePortDll_BaseAddress, new UInt64[] { _GamePort_Dll_vfTable_Offset + 0x1F8 }, BitConverter.GetBytes(_SetLightbelt_CustomFunc_Addresses[2]));
            ReplaceReadOnlyString(_GamePortDll_BaseAddress, new UInt64[] { _GamePort_Dll_vfTable_Offset + 0x200 }, BitConverter.GetBytes(_SetLightbelt_CustomFunc_Addresses[3]));

            //Recoil
            ReplaceReadOnlyString(_GamePortDll_BaseAddress, new UInt64[] { _GamePort_Dll_vfTable_Offset + 0x168 }, BitConverter.GetBytes(_SetElecShake_CustomFunc_Addresses[0]));
            ReplaceReadOnlyString(_GamePortDll_BaseAddress, new UInt64[] { _GamePort_Dll_vfTable_Offset + 0x170 }, BitConverter.GetBytes(_SetElecShake_CustomFunc_Addresses[1]));
            ReplaceReadOnlyString(_GamePortDll_BaseAddress, new UInt64[] { _GamePort_Dll_vfTable_Offset + 0x178 }, BitConverter.GetBytes(_SetElecShake_CustomFunc_Addresses[2]));
            ReplaceReadOnlyString(_GamePortDll_BaseAddress, new UInt64[] { _GamePort_Dll_vfTable_Offset + 0x180 }, BitConverter.GetBytes(_SetElecShake_CustomFunc_Addresses[3]));
        }

        private void Create_CustomOutputsFunctions()
        {
            Codecave64 CaveMemory = new Codecave64(_Process, GameProcess_ModuleBaseAddress);

            for (UInt64 i = 0; i < 4; i++)
            {
                _SetStartButtonLight_CustomFunc_Addresses[i] = CaveMemory.CaveAddress + (UInt64)CaveMemory.CaveOffset;
                //mov rax, Address
                CaveMemory.Write_StrBytes("48 B8");
                CaveMemory.Write_Bytes(BitConverter.GetBytes(_StartLamps_Address + i));
                //mov [rax],dl
                CaveMemory.Write_StrBytes("88 10");
                //ret
                CaveMemory.Write_StrBytes("C3");

                _SetLightbelt_CustomFunc_Addresses[i] = CaveMemory.CaveAddress + (UInt64)CaveMemory.CaveOffset;
                //mov rax, Address
                CaveMemory.Write_StrBytes("48 B8");
                CaveMemory.Write_Bytes(BitConverter.GetBytes(_LightBelts_Address + i));
                //mov [rax],dl
                CaveMemory.Write_StrBytes("88 10");
                //ret
                CaveMemory.Write_StrBytes("C3");

                _SetElecShake_CustomFunc_Addresses[i] = CaveMemory.CaveAddress + (UInt64)CaveMemory.CaveOffset;
                //mov rax, Address
                CaveMemory.Write_StrBytes("48 B8");
                CaveMemory.Write_Bytes(BitConverter.GetBytes(_Recoil_Address + i));
                //mov [rax],dl
                CaveMemory.Write_StrBytes("88 10");
                //ret
                CaveMemory.Write_StrBytes("C3");
            }
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
            else if (e.ButtonName.Equals("SETTINGS"))
            {
                WriteByte(_SettingButton_Address, 0x01);
            }
            else if (e.ButtonName.Equals("MENU LEFT"))
            {
                WriteByte(_LeftButton_Address, 0x01);
            }
            else if (e.ButtonName.Equals("MENU RIGHT"))
            {
                WriteByte(_RightButton_Address, 0x01);
            }
        }

        protected override void GameController_OnButtonUp(object sender, VirtualGameControler.ButtonDownEventArgs e)
        {
            if (e.ButtonName.Equals("SETTINGS"))
            {
                WriteByte(_SettingButton_Address, 0x00);
            }
            else if (e.ButtonName.Equals("MENU LEFT"))
            {
                WriteByte(_LeftButton_Address, 0x00);
            }
            else if (e.ButtonName.Equals("MENU RIGHT"))
            {
                WriteByte(_RightButton_Address, 0x00);
            }
        }

        protected override void UpdateAxis(PlayerID Id, int X, int Y)
        {
            if (Id != PlayerID.None)
            {
                byte[] bufferX = BitConverter.GetBytes(X);
                byte[] bufferY = BitConverter.GetBytes(Y);

                WriteBytes(_Axes_X_Address + 4 * (UInt64)Id, bufferX);
                WriteBytes(_Axes_Y_Address + 4 * (UInt64)Id, bufferY);
            }
        }

        protected override void PlayerController_OnButtonDown(object sender, VirtualGameControler.ButtonDownEventArgs e)
        {
            if (e.Id != PlayerID.None)
            {
                UInt32 PlayerIndex = (UInt32)e.Id;

                if (e.ButtonName.Equals("COIN"))
                {
                    byte b = ReadByte(_Coins_Address + PlayerIndex);
                    WriteByte(_Coins_Address + PlayerIndex, ++b);
                }
                else if (e.ButtonName.Equals("TRIGGER"))
                {
                    WriteByte(_FireButtons_Address + PlayerIndex, 0x01);
                }
                else if (e.ButtonName.Equals("START"))
                {
                    WriteByte(_StartButtons_Address + PlayerIndex, 0x01);
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
                    WriteByte(_FireButtons_Address + PlayerIndex, 0x00);
                }
                else if (e.ButtonName.Equals("START"))
                {
                    WriteByte(_StartButtons_Address + PlayerIndex, 0x00);
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
                new GameOutput(OutputId.P1_LightBelt),
                new GameOutput(OutputId.P2_LightBelt),
                new GameOutput(OutputId.P1_GunMotor),
                new GameOutput(OutputId.P2_GunMotor),
                //new AsyncGameOutput(OutputId.P1_CtmRecoil, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_ON_DELAY), ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_OFF_DELAY), 0),
                //new AsyncGameOutput(OutputId.P2_CtmRecoil, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_ON_DELAY), ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_OFF_DELAY), 0),
                new GameOutput(OutputId.P1_Credits),
                new GameOutput(OutputId.P2_Credits),
            };
        }

        /// <summary>
        /// Update all Outputs values before sending them to MameHooker
        /// </summary>
        public override void UpdateOutputValues()
        {
            SetOutputValue(OutputId.P1_LmpStart, ReadByte(_StartLamps_Address));
            SetOutputValue(OutputId.P2_LmpStart, ReadByte(_StartLamps_Address + 1));
            SetOutputValue(OutputId.P1_LightBelt, ReadByte(_LightBelts_Address));
            SetOutputValue(OutputId.P2_LightBelt, ReadByte(_LightBelts_Address + 1));
            SetOutputValue(OutputId.P1_GunMotor, ReadByte(_Recoil_Address));
            SetOutputValue(OutputId.P2_GunMotor, ReadByte(_Recoil_Address + 1));
            SetOutputValue(OutputId.P1_Credits, ReadByte(_Coins_Address));
            SetOutputValue(OutputId.P2_Credits, ReadByte(_Coins_Address + 1));
        }

        #endregion
    }
}
