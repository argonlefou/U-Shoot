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
    public class Game_WndReload : BaseGame32
    {
        protected override string GAMEDATA_FOLDER { get { return @"Windows\reload"; } }
        protected override bool PATCH_PEB_DEBUG_FLAG { get { return true; } }

        private const string RLD_GAME_DLL_NAME = "rld_game.dll";
        private UInt32 _Rld_Game_Dll_BaseAddress;

        #region LOADER MEMORY ADDRESSES 
        private InjectionStruct32 _NoCrosshair_InjectionStruct = new InjectionStruct32(0x0021874F, 5);
        #endregion

        #region INPUTS MEMORY ADDRESSES    
        /// The game needs 3 different values to be overwritten :
        /// - Menu axis (float between 0 and windows size)
        /// - In Game crosshair (between -0.5 and +0.5)
        /// - In Game shooting axis (between -1.0 and +1.0 )        
        private UInt32 _P1_X_Menu_CaveAddress;
        private UInt32 _P1_Y_Menu_CaveAddress;
        private UInt32 _P1_X_Shoot_CaveAddress;
        private UInt32 _P1_Y_Shoot_CaveAddress;
        private UInt32 _P1_X_Crosshair_CaveAddress;
        private UInt32 _P1_Y_Crosshair_CaveAddress;
        private InjectionStruct32 _P1_X_Menu_InjectionStruct = new InjectionStruct32(0x001BD16C, 8);
        private InjectionStruct32 _P1_Y_Menu_InjectionStruct = new InjectionStruct32(0x001BD1D3, 8);
        private UInt32 _P1_Menu_Fix_Offset = 0x001BD185;
        private InjectionStruct32 _P1_InGame_Crosshair_InjectionStruct = new InjectionStruct32(0x00218756, 11);
        private InjectionStruct32 _P1_InGame_X_InjectionStruct = new InjectionStruct32(0x0027CFD1, 6);
        private InjectionStruct32 _P1_InGame_Y_InjectionStruct = new InjectionStruct32(0x0027D027, 7);
        #endregion

        #region OUTPUTS MEMORY ADDRESSES
        private UInt32 _P1_Recoil_CaveAddress;
        private UInt32 _P1_Ammo_CaveAddress;
        private InjectionStruct32 _Recoil_InjectionStruct = new InjectionStruct32(0x00263C0C, 5);
        private InjectionStruct32 _Ammo_InjectionStruct = new InjectionStruct32(0x0021E3AC, 10);
        #endregion

        //Custom data to inject
        protected float _P1_X_Menu_Value;
        protected float _P1_Y_Menu_Value;
        protected float _P1_X_Shoot_Value;
        protected float _P1_Y_Shoot_Value;
        protected float _P1_Crosshair_X_Value;
        protected float _P1_Crosshair_Y_Value;

        public Game_WndReload(GameConfig Config) : base(Config)
        {
            _KnownMd5Prints.Add("Reload v1.0.0.1 - IGG", "aaaf22c6671c12176d8317d4cc4b478d");
            _KnownMd5Prints.Add("Reload v1.0.0.1 - Unknown release 1", "f3c4068a49f07aa99d2a92544d5c5748");
            _KnownMd5Prints.Add("Reload v1.0.0.1 - Unknown release 2", "2e1e22229c90b53153d2c015371e643a");
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
                if (string.Equals(Path.GetFileName(e.DllPath), RLD_GAME_DLL_NAME, StringComparison.OrdinalIgnoreCase))
                {
                    Logger.WriteLog("==> Wanted DLL found !");
                    //HANDLE should always be in 32Bits range (https://learn.microsoft.com/fr-fr/windows/win32/winprog64/interprocess-communication?redirectedfrom=MSDN)
                    Logger.WriteLog("  + hModule: 0x" + e.LoadDllInfo.hFile.ToString("X8"));
                    Logger.WriteLog("  + fUnicode: " + e.LoadDllInfo.fUnicode.ToString());

                    _Rld_Game_Dll_BaseAddress = (UInt32)e.LoadDllInfo.lpBaseOfDll;
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

                    //During Menus :
                    //Y => [0; RezY] en float
                    //X => [0; RezX] en float                    

                    _P1_X_Menu_Value = (float)PlayerController.Computed_X;
                    _P1_Y_Menu_Value = (float)PlayerController.Computed_Y;

                    if (_P1_X_Menu_Value < 0.0f)
                        _P1_X_Menu_Value = 0.0f;
                    if (_P1_Y_Menu_Value < 0.0f)
                        _P1_Y_Menu_Value = 0.0f;
                    if (_P1_X_Menu_Value > (float)TotalResX)
                        _P1_X_Menu_Value = (float)TotalResX;
                    if (_P1_Y_Menu_Value > (float)TotalResY)
                        _P1_Y_Menu_Value = (float)TotalResY;

                    //In Game, Shoot :
                    //Y => [-1;1] en float
                    //X => [-1;1] en float
                    _P1_X_Shoot_Value = (2.0f * PlayerController.Computed_X / (float)TotalResX) - 1.0f;
                    _P1_Y_Shoot_Value = (2.0f * PlayerController.Computed_Y / (float)TotalResY) - 1.0f;

                    if (_P1_X_Shoot_Value < -1.0f)
                        _P1_X_Shoot_Value = -1.0f;
                    if (_P1_Y_Shoot_Value < -1.0f)
                        _P1_Y_Shoot_Value = -1.0f;
                    if (_P1_X_Shoot_Value > 1.0f)
                        _P1_X_Shoot_Value = 1.0f;
                    if (_P1_Y_Shoot_Value > 1.0f)
                        _P1_Y_Shoot_Value = 1.0f;

                    //In Game, Crosshair :
                    //Y => [-0.5;0.5] en float
                    //X => [-0.5;0.5] en float
                    _P1_Crosshair_X_Value = _P1_X_Shoot_Value / 2.0f;
                    _P1_Crosshair_Y_Value = _P1_Y_Shoot_Value / 2.0f;

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
        /// So far I ran into 2 versions of the game, both with different .exe and .dll files
        /// Although the hack is the same, .dll files have slighty different asm codes so for some of them we need to rewrite
        /// it all differently, because only changing offsets won't be enough
        /// </summary>
        protected override void Apply_MemoryHacks()
        {
            base.Apply_MemoryHacks();
            RaiseGameHookedEvent();
        }

        #region Loader Hack

        protected override void Apply_LoaderMemoryHack()
        {  }

        #endregion        

        #region Inputs Hack

        protected override void Apply_InputsMemoryHack()
        {
            //Common procedure
            Create_InputsDataBank();
            _P1_X_Menu_CaveAddress = InputsDatabank_Address;
            _P1_Y_Menu_CaveAddress = InputsDatabank_Address + 0x04;
            _P1_X_Shoot_CaveAddress = InputsDatabank_Address + 0x10;
            _P1_Y_Shoot_CaveAddress = InputsDatabank_Address + 0x14;
            _P1_X_Crosshair_CaveAddress = InputsDatabank_Address + 0x20;
            _P1_Y_Crosshair_CaveAddress = InputsDatabank_Address + 0x24;

            if (_TargetProcess_Md5Hash == _KnownMd5Prints["Reload v1.0.0.1 - IGG"])
            {
                SetHack_P1X_Menu();
                SetHack_P1Y_Menu();

                //If HideCrossair option is activated, the P1_Crosshair codecave will overwrite the NoCrosshair injection
                //That why we need to check before installing it
                if (!_GameConfig.NoCrosshair)
                    SetHack_P1_Crosshair();

                SetHack_P1_X_Shoot();
                SetHack_P1_Y_Shoot();
            }
            else if (_TargetProcess_Md5Hash == _KnownMd5Prints["Reload v1.0.0.1 - Unknown release 1"] || _TargetProcess_Md5Hash == _KnownMd5Prints["Reload v1.0.0.1 - Unknown release 2"])
            {
                SetHack_P1X_Menu_V2();
                SetHack_P1Y_Menu_V2();

                //If HideCrossair option is activated, the P1_Crosshair codecave will overwrite the NoCrosshair injection
                //That why we need to check before installing it
                if (!_GameConfig.NoCrosshair)
                    SetHack_P1_Crosshair_V2();

                SetHack_P1_X_Shoot_V2();
                SetHack_P1_Y_Shoot_V2();
            }

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        /// <summary>
        /// All Axis codecave are the same :
        /// The game use some fstp [XXX] instruction, but we can't just NOP it as graphical glitches may appear.
        /// So we just add another set of instructions immediatelly after to change the register 
        /// to our own desired value
        /// </summary>
        private void SetHack_P1X_Menu()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //push eax
            CaveMemory.Write_StrBytes("50");
            //mov eax, _P1_X_Address
            CaveMemory.Write_StrBytes("B8");
            byte[] b = BitConverter.GetBytes(_P1_X_Menu_CaveAddress);
            CaveMemory.Write_Bytes(b);
            //movss xmm1, [eax]
            CaveMemory.Write_StrBytes("F3 0F 10 08");
            //pop eax
            CaveMemory.Write_StrBytes("58");
            //movss [ecx+0000013C],xmm1
            CaveMemory.Write_StrBytes("F3 0F 11 89 3C 01 00 00");

            //Inject it
            CaveMemory.InjectToOffset(_Rld_Game_Dll_BaseAddress, _P1_X_Menu_InjectionStruct, "P1 X Menu");
        }

        private void SetHack_P1Y_Menu()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //push eax
            CaveMemory.Write_StrBytes("50");
            //mov eax, _P1_Y_Address
            CaveMemory.Write_StrBytes("B8");
            byte[] b = BitConverter.GetBytes(_P1_Y_Menu_CaveAddress);
            CaveMemory.Write_Bytes(b);
            //movss xmm0, [eax]
            CaveMemory.Write_StrBytes("F3 0F 10 00");
            //pop eax
            CaveMemory.Write_StrBytes("58");
            //movss [ecx+0000014C],xmm0
            CaveMemory.Write_StrBytes("F3 0F 11 81 40 01 00 00");

            //Inject it
            CaveMemory.InjectToOffset(_Rld_Game_Dll_BaseAddress, _P1_Y_Menu_InjectionStruct, "P1 Y Menu");

        }

        /// <summary>
        /// The game is using different procedures to handle mouse position in menu and in-game
        /// Only one codecave for Menus handling as X and Y are 4 instructions next to each other
        /// </summary>
        private void SetHack_P1_Crosshair()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //fld dword ptr[_P1_X_Address]
            CaveMemory.Write_StrBytes("D9 05");
            byte[] b = BitConverter.GetBytes(_P1_X_Crosshair_CaveAddress);
            CaveMemory.Write_Bytes(b);
            //fstp dword ptr[edx+28]
            CaveMemory.Write_StrBytes("D9 5A 28");
            //fld dword ptr[_P1_Y_Address]
            CaveMemory.Write_StrBytes("D9 05");
            b = BitConverter.GetBytes(_P1_Y_Crosshair_CaveAddress);
            CaveMemory.Write_Bytes(b);
            //fstp dword ptr[edx+2C]
            CaveMemory.Write_StrBytes("D9 5A 2C");

            //Inject it
            CaveMemory.InjectToOffset(_Rld_Game_Dll_BaseAddress, _P1_InGame_Crosshair_InjectionStruct, "P1 Crosshair");
        }

        private void SetHack_P1_X_Shoot()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //mov eax [ebp + 10]
            CaveMemory.Write_StrBytes("8B 45 10");
            //fstp dword ptr[esi+48]
            CaveMemory.Write_StrBytes("D9 5E 48");
            //push eax
            CaveMemory.Write_StrBytes("50");
            //mov eax, _P1_X_Shoot_Address
            CaveMemory.Write_StrBytes("A1");
            byte[] b = BitConverter.GetBytes(_P1_X_Shoot_CaveAddress);
            CaveMemory.Write_Bytes(b);
            //push eax
            CaveMemory.Write_StrBytes("50");
            //fld dword ptr[esp]
            CaveMemory.Write_StrBytes("D9 04 24");
            //pop eax
            CaveMemory.Write_StrBytes("58");
            //fstp dword ptr[esi+48]
            CaveMemory.Write_StrBytes("D9 5E 48");
            //pop eax
            CaveMemory.Write_StrBytes("58");

            //Inject it
            CaveMemory.InjectToOffset(_Rld_Game_Dll_BaseAddress, _P1_InGame_X_InjectionStruct, "P1 X In-Game");

        }

        private void SetHack_P1_Y_Shoot()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //push eax
            CaveMemory.Write_StrBytes("50");
            //mov eax, _P1_Y_Shoot_Address
            CaveMemory.Write_StrBytes("A1");
            byte[] b = BitConverter.GetBytes(_P1_Y_Shoot_CaveAddress);
            CaveMemory.Write_Bytes(b);
            //push eax
            CaveMemory.Write_StrBytes("50");
            //fld dword ptr[esp]
            CaveMemory.Write_StrBytes("D9 04 24");
            //pop eax
            CaveMemory.Write_StrBytes("58");
            //fstp dword ptr[esi+4C]
            CaveMemory.Write_StrBytes("D9 5E 4C");
            //pop eax
            CaveMemory.Write_StrBytes("58");
            //cmp byte ptr [esi+000000CC],00
            CaveMemory.Write_StrBytes("80 BE CC 00 00 00 00");

            //Inject it
            CaveMemory.InjectToOffset(_Rld_Game_Dll_BaseAddress, _P1_InGame_Y_InjectionStruct, "P1 Y In-Game");
        }



        /// <summary>
        /// All Axis codecave are the same :
        /// The game use some fstp [XXX] instruction, but we can't just NOP it as graphical glitches may appear.
        /// So we just add another set of instructions immediatelly after to change the register 
        /// to our own desired value
        /// </summary>
        private void SetHack_P1X_Menu_V2()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //push eax
            CaveMemory.Write_StrBytes("50");
            //mov eax, _P1_X_Address
            CaveMemory.Write_StrBytes("B8");
            byte[] b = BitConverter.GetBytes(_P1_X_Menu_CaveAddress);
            CaveMemory.Write_Bytes(b);
            //movss xmm1, [eax]
            CaveMemory.Write_StrBytes("F3 0F 10 08");
            //pop eax
            CaveMemory.Write_StrBytes("58");
            //movss [esp+4+arg0],xmm1
            CaveMemory.Write_StrBytes("F3 0F 11 4C 24 08");

            //Inject it
            CaveMemory.InjectToOffset(_Rld_Game_Dll_BaseAddress, _P1_X_Menu_InjectionStruct, "P1 X Menu");

            //Cursor is not stable, so a little mod:
            WriteByte(_Rld_Game_Dll_BaseAddress + _P1_Menu_Fix_Offset, 0xD9);
        }

        private void SetHack_P1Y_Menu_V2()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //push eax
            CaveMemory.Write_StrBytes("50");
            //mov eax, _P1_Y_Address
            CaveMemory.Write_StrBytes("B8");
            byte[] b = BitConverter.GetBytes(_P1_Y_Menu_CaveAddress);
            CaveMemory.Write_Bytes(b);
            //movss xmm0, [eax]
            CaveMemory.Write_StrBytes("F3 0F 10 00");
            //pop eax
            CaveMemory.Write_StrBytes("58");
            //movss [ecx+00000140],xmm0
            CaveMemory.Write_StrBytes("F3 0F 11 81 40 01 00 00");

            //Inject it
            CaveMemory.InjectToOffset(_Rld_Game_Dll_BaseAddress, _P1_Y_Menu_InjectionStruct, "P1 Y Menu");

            //Cursor is not stable, so a little mod:
            WriteByte(_Rld_Game_Dll_BaseAddress + _P1_Menu_Fix_Offset, 0xD9);
        }

        /// <summary>
        /// The game is using different procedures to handle mouse position in menu and in-game
        /// Only one codecave for Menus handling as X and Y are 4 instructions next to each other
        /// </summary>
        private void SetHack_P1_Crosshair_V2()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //fld dword ptr[_P1_X_Address]
            CaveMemory.Write_StrBytes("D9 05");
            byte[] b = BitConverter.GetBytes(_P1_X_Crosshair_CaveAddress);
            CaveMemory.Write_Bytes(b);
            //fstp dword ptr[edx+24]
            CaveMemory.Write_StrBytes("D9 5A 24");
            //fld dword ptr[_P1_Y_Address]
            CaveMemory.Write_StrBytes("D9 05");
            b = BitConverter.GetBytes(_P1_Y_Crosshair_CaveAddress);
            CaveMemory.Write_Bytes(b);
            //fstp dword ptr[edx+28]
            CaveMemory.Write_StrBytes("D9 5A 28");

            //Inject it
            CaveMemory.InjectToOffset(_Rld_Game_Dll_BaseAddress, _P1_InGame_Crosshair_InjectionStruct, "P1 Crosshair");
        }

        private void SetHack_P1_X_Shoot_V2()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //mov eax [esp + 54h + arg_8]
            CaveMemory.Write_StrBytes("8B 4C 24 60");
            //fstp dword ptr[esi+48]
            CaveMemory.Write_StrBytes("D9 5E 48");
            //push eax
            CaveMemory.Write_StrBytes("50");
            //mov eax, _P1_X_Shoot_Address
            CaveMemory.Write_StrBytes("A1");
            byte[] b = BitConverter.GetBytes(_P1_X_Shoot_CaveAddress);
            CaveMemory.Write_Bytes(b);
            //push eax
            CaveMemory.Write_StrBytes("50");
            //fld dword ptr[esp]
            CaveMemory.Write_StrBytes("D9 04 24");
            //pop eax
            CaveMemory.Write_StrBytes("58");
            //fstp dword ptr[esi+48]
            CaveMemory.Write_StrBytes("D9 5E 48");
            //pop eax
            CaveMemory.Write_StrBytes("58");

            //Inject it
            CaveMemory.InjectToOffset(_Rld_Game_Dll_BaseAddress, _P1_InGame_X_InjectionStruct, "P1 X In-Game");

        }

        private void SetHack_P1_Y_Shoot_V2()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //push eax
            CaveMemory.Write_StrBytes("50");
            //mov eax, _P1_Y_Shoot_Address
            CaveMemory.Write_StrBytes("A1");
            byte[] b = BitConverter.GetBytes(_P1_Y_Shoot_CaveAddress);
            CaveMemory.Write_Bytes(b);
            //push eax
            CaveMemory.Write_StrBytes("50");
            //fld dword ptr[esp]
            CaveMemory.Write_StrBytes("D9 04 24");
            //pop eax
            CaveMemory.Write_StrBytes("58");
            //fstp dword ptr[esi+4C]
            CaveMemory.Write_StrBytes("D9 5E 4C");
            //pop eax
            CaveMemory.Write_StrBytes("58");
            //cmp byte ptr [esi+000000CC],00
            CaveMemory.Write_StrBytes("80 BE CC 00 00 00 00");

            //Inject it
            CaveMemory.InjectToOffset(_Rld_Game_Dll_BaseAddress, _P1_InGame_Y_InjectionStruct, "P1 Y In-Game");
        }

        #endregion

        #region Outputs Hack

        protected override void Apply_OutputsMemoryHack()
        {
            Create_OutputsDataBank();
            _P1_Recoil_CaveAddress = OutputsDatabank_Address;
            _P1_Ammo_CaveAddress = OutputsDatabank_Address + 0x04;

            if (_TargetProcess_Md5Hash == _KnownMd5Prints["Reload v1.0.0.1 - IGG"])
            {
                SetHack_Recoil();
                SetHack_Ammo();
            }
            else if (_TargetProcess_Md5Hash == _KnownMd5Prints["Reload v1.0.0.1 - Unknown release 1"] || _TargetProcess_Md5Hash == _KnownMd5Prints["Reload v1.0.0.1 - Unknown release 2"])
            {
                SetHack_Recoil_V2();
                SetHack_Ammo_V2();
            }

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        /// <summary>
        /// csControllerActor::Shoot()  calls csControllerActorPlayer::Shoot() 	
        /// Intercepting the call will make us create a recoil signal+ 
        /// </summary>
        private void SetHack_Recoil()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //mov byte ptr[_P1_Recoil_CaveAddress], 1
            CaveMemory.Write_StrBytes("C6 05");
            byte[] b = BitConverter.GetBytes(_P1_Recoil_CaveAddress);
            CaveMemory.Write_Bytes(b);
            CaveMemory.Write_StrBytes("01");
            //mov ecx, esi
            CaveMemory.Write_StrBytes("8B CE");
            //fstp dword ptr [esp]
            CaveMemory.Write_StrBytes("D9 1C 24");

            //Inject it
            CaveMemory.InjectToOffset(_Rld_Game_Dll_BaseAddress, _Recoil_InjectionStruct, "Recoil");
        }

        /// <summary>
        /// csControllerActor::Shoot()  calls csControllerActorPlayer::Shoot() 	
        /// Intercepting the call will make us create a recoil signal+ 
        /// </summary>
        private void SetHack_Recoil_V2()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //mov byte ptr[_P1_Recoil_CaveAddress], 1
            CaveMemory.Write_StrBytes("C6 05");
            byte[] b = BitConverter.GetBytes(_P1_Recoil_CaveAddress);
            CaveMemory.Write_Bytes(b);
            CaveMemory.Write_StrBytes("01");
            //mov ecx, edi
            CaveMemory.Write_StrBytes("8B CF");
            //fstp dword ptr [esp]
            CaveMemory.Write_StrBytes("D9 1C 24");

            //Inject it
            CaveMemory.InjectToOffset(_Rld_Game_Dll_BaseAddress, _Recoil_InjectionStruct, "Recoil");
        }

        /// <summary>
        /// Reading the result of GetRemainingBullets() call in the ActorHud::StatusBar_AdvanceTime() loop
        /// </summary>
        private void SetHack_Ammo()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //mov [_P1_Ammo_CaveAddress], eax
            CaveMemory.Write_StrBytes("A3");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_Ammo_CaveAddress));
            //mov esi,eax
            CaveMemory.Write_StrBytes("8B F0");
            //mov edx,[ecx]
            CaveMemory.Write_StrBytes("8B 11");
            //mov [ebp-00000148],esi
            CaveMemory.Write_StrBytes("89 B5 B8 FE FF FF");

            //Inject it
            CaveMemory.InjectToOffset(_Rld_Game_Dll_BaseAddress, _Ammo_InjectionStruct, "Ammo");
        }

        /// <summary>
        /// Reading the result of GetRemainingBullets() call in the ActorHud::StatusBar_AdvanceTime() loop
        /// </summary>
        private void SetHack_Ammo_V2()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //mov [_P1_Ammo_CaveAddress], eax
            CaveMemory.Write_StrBytes("A3");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_Ammo_CaveAddress));
            //mov edx,[ecx]
            CaveMemory.Write_StrBytes("8B 11");
            //mov ebp,eax
            CaveMemory.Write_StrBytes("8B E8");
            //mov eax,[edx+60]
            CaveMemory.Write_StrBytes("8B 42 60");

            //Inject it
            CaveMemory.InjectToOffset(_Rld_Game_Dll_BaseAddress, _Ammo_InjectionStruct, "Ammo");
        }

        #endregion

        #region No Crosshair Hack

        protected override void Apply_NoCrosshairMemoryHack()
        {
            if (_TargetProcess_Md5Hash == _KnownMd5Prints["Reload v1.0.0.1 - IGG"])
                SetHack_NoCrosshair();
            else if (_TargetProcess_Md5Hash == _KnownMd5Prints["Reload v1.0.0.1 - Unknown release 1"] || _TargetProcess_Md5Hash == _KnownMd5Prints["Reload v1.0.0.1 - Unknown release 2"])
                SetHack_NoCrosshair_V2();

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        /// <summary>
        /// Set values to [-1.0; -1.0] to display crosshair in ActorHud::Crosshair_SetPosition() function
        /// </summary>
        private void SetHack_NoCrosshair()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //mov edi,[ebp+0C]
            CaveMemory.Write_StrBytes("8B 7D 0C");
            //mov [edi],BF800000 { -1.00 }
            CaveMemory.Write_StrBytes("C7 07 00 00 80 BF");
            //mov [edi+04],BF800000 { -1.00 }
            CaveMemory.Write_StrBytes("C7 47 04 00 00 80 BF");
            //test edx,edx
            CaveMemory.Write_StrBytes("85 D2");

            //Inject it
            CaveMemory.InjectToOffset(_Rld_Game_Dll_BaseAddress, _NoCrosshair_InjectionStruct, "No Crosshair");
        }

        /// <summary>
        /// Set values to [-1.0; -1.0] to display crosshair in ActorHud::Crosshair_SetPosition() function
        /// </summary>
        private void SetHack_NoCrosshair_V2()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //push edi
            CaveMemory.Write_StrBytes("57");
            //mov edi,[esp+10]
            CaveMemory.Write_StrBytes("8B 7C 24 10");
            //mov [edi],BF800000 { -1.00 }
            CaveMemory.Write_StrBytes("C7 07 00 00 80 BF");
            //mov [edi+04],BF800000 { -1.00 }
            CaveMemory.Write_StrBytes("C7 47 04 00 00 80 BF");

            //Inject it
            CaveMemory.InjectToOffset(_Rld_Game_Dll_BaseAddress, _NoCrosshair_InjectionStruct, "No Crosshair");
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
        }

        protected override void GameController_OnButtonUp(object sender, VirtualGameControler.ButtonDownEventArgs e)
        { }

        protected override void UpdateAxis(PlayerID Id, int X, int Y)
        {
            if (Id == PlayerID.Player1)
            {
                //Setting Values in memory for the Codecave to read it
                byte[] buffer = BitConverter.GetBytes(_P1_X_Menu_Value);
                WriteBytes(_P1_X_Menu_CaveAddress, buffer);
                buffer = BitConverter.GetBytes(_P1_Y_Menu_Value);
                WriteBytes(_P1_Y_Menu_CaveAddress, buffer);

                buffer = BitConverter.GetBytes(_P1_Crosshair_X_Value);
                WriteBytes(_P1_X_Crosshair_CaveAddress, buffer);
                buffer = BitConverter.GetBytes(_P1_Crosshair_Y_Value);
                WriteBytes(_P1_Y_Crosshair_CaveAddress, buffer);

                buffer = BitConverter.GetBytes(_P1_X_Shoot_Value);
                WriteBytes(_P1_X_Shoot_CaveAddress, buffer);
                buffer = BitConverter.GetBytes(_P1_Y_Shoot_Value);
                WriteBytes(_P1_Y_Shoot_CaveAddress, buffer);
            }
        }

        protected override void PlayerController_OnButtonDown(object sender, VirtualGameControler.ButtonDownEventArgs e)
        {
            if (e.Id == PlayerID.Player1)
            {
                UInt32 PlayerIndex = (UInt32)e.Id;

                if (e.ButtonName.Equals("TRIGGER"))
                {

                }
                else if (e.ButtonName.Equals("RELOAD"))
                {

                }
                else if (e.ButtonName.Equals("HOLD_BREATH"))
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

                }
                else if (e.ButtonName.Equals("RELOAD"))
                {

                }
                else if (e.ButtonName.Equals("HOLD_BREATH"))
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
            _Outputs = new List<GameOutput>
            {
                new GameOutput(OutputId.P1_Ammo),
                new AsyncGameOutput(OutputId.P1_CtmRecoil, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_ON_DELAY), ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_OFF_DELAY), 0),
            }; 
        }

        /// <summary>
        /// Update all Outputs values before sending them to MameHooker
        /// </summary>
        public override void UpdateOutputValues()
        {
            //Custom Recoil will simply be read on memory and reset
            //the codecave injected will update it for the "ON" state
            byte P1_RecoilState = ReadByte(_P1_Recoil_CaveAddress);
            SetOutputValue(OutputId.P1_CtmRecoil, P1_RecoilState);
            if (P1_RecoilState == 1)
                WriteByte(_P1_Recoil_CaveAddress, 0x00);

            int P1_ammo = ReadInt32(_P1_Ammo_CaveAddress);
            SetOutputValue(OutputId.P1_Ammo, P1_ammo);
        }

        #endregion
    }
}
