using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UsCore.Config;
using UsCore.Launcher;
using UsCore.Logging;
using UsCore.MameOutput;
using UsCore.Memory;
using UsCore.VirtualControllers;

namespace U_Shoot.Games
{
    public class Game_WndHeavyFire3Pc : BaseGame32
    {
        protected override string GAMEDATA_FOLDER { get { return @"Windows\hfa"; } }
        protected override bool PATCH_PEB_DEBUG_FLAG { get { return false; } }

        private const string PEDALMODE_OPTION = "Pedal Mode";
        private const string REVERSECOVER_OPTION = "Reverse Cover";
        private const string COVERSENSIBILITY_OPTION = "Cover Sensibility";

        #region LOADER MEMORY ADDRESSES    
        private UInt32 _NoGun_Patch_Offset = 0x000D7592;
        private InjectionStruct32 _NoCrosshair_InjectionStruct = new InjectionStruct32(0x000AF009, 8);
        #endregion

        #region INPUTS MEMORY ADDRESSES 
        private UInt32 _Mouse_Buttons_PatchOffset = 0x00116390;
        private NopStruct32 _Nop_MouseCursorWhenGetFocus = new NopStruct32(0x001156E0, 8);
        private NopStruct32 _Nop_MouseCursorWhenLooseFocus = new NopStruct32(0x001157FA, 8);
        private NopStruct32 _Nop_SteamForceMouseMainController = new NopStruct32(0x0001E002, 6);
        private InjectionStruct32 _PlayersOnOff_InjectionStruct = new InjectionStruct32(0x0001E634, 6);
        private InjectionStruct32 _Mouse_Buttons_InjectionStruct = new InjectionStruct32(0x0001E82A, 6);
        private InjectionStruct32 _Mouse_X_InjectionStruct = new InjectionStruct32(0x0001E85F, 7);
        private InjectionStruct32 _Mouse_Y_InjectionStruct = new InjectionStruct32(0x0001E87C, 7);

        //Custom Data
        private UInt32 _Axis_X_CaveAddress = 0;
        private UInt32 _Axis_Y_CaveAddress = 0;
        private UInt32 _Trigger_CaveAddress = 0;
        private UInt32 _Reload_CaveAddress = 0;
        private UInt32 _Grenade_CaveAddress = 0;
        private UInt32 _PlayerOnOff_CaveAddress = 0;
        private UInt32 _CoverLeft_CaveAddress = 0;
        private UInt32 _CoverRight_CaveAddress = 0;
        private UInt32 _CoverDown_CaveAddress = 0;
        private UInt32 _QteSpace_CaveAddress = 0;
        private UInt32 _QteW_CaveAddress = 0;
        #endregion

        #region OUTPUTS MEMORY ADDRESSES
        private InjectionStruct32 _Recoil_InjectionStruct = new InjectionStruct32(0x00E8045, 5);
        private InjectionStruct32 _Ammo_InjectionStruct = new InjectionStruct32(0x000C5293, 8);
        private InjectionStruct32 _Damage_InjectionStruct = new InjectionStruct32(0x000C1C7E, 7);
        private InjectionStruct32 _GamePlaying_InjectionStruct = new InjectionStruct32(0x00108AE9, 7);

        //Custom Data
        private UInt32 _GamePlaying_CaveAddress = 0;
        private UInt32 _Recoil_CaveAddress = 0;     //4 bytes : P1~P4
        private UInt32 _Damage_CaveAddress = 0;     //4 bytes : P1~P4
        private UInt32 _Ammo_CaveAddress = 0;       //16 bytes : P1~P4    
        #endregion

        protected float _Axis_X_Min;
        protected float _Axis_X_Max;
        protected float _CoverDelta = 0.3f;

        public Game_WndHeavyFire3Pc(GameConfig Config) : base(Config)
        {
            _KnownMd5Prints.Add("Heavy Fire 3 - SKIDROW", "3f49951ae8232817a91ef5503374d6b3");
            _KnownMd5Prints.Add("Heavy Fire 3 - STEAM", "1841e571286acb17a98546a439c08e57");
            _KnownMd5Prints.Add("Heavy Fire 3 - MULTi5-PROPHET", "4344f54b91e7a8ef20042d5f0b6e04aa");

            //Title seem to be a blank text
            _IsAnyWindowTitlePossible = true;

            _CoverDelta = float.Parse(_GameConfig.GetStringOption(COVERSENSIBILITY_OPTION)) / 10.0f;
            Logger.WriteLog("Setting Cover delta to screen border to " + _CoverDelta.ToString());
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

                    //Y => [-1 ; 1] float
                    //X => depend on ration Width/Height (ex : [-1.7777; 1.7777] with 1920x1080)

                    float fRatio = (float)TotalResX / (float)TotalResY;
                    _Axis_X_Min = -fRatio;
                    _Axis_X_Max = fRatio;

                    float Y_Value = (2.0f * PlayerController.Computed_Y / (float)TotalResY) - 1.0f;
                    float X_Value = (fRatio * 2 * PlayerController.Computed_X / (float)TotalResX) - fRatio;

                    if (X_Value < _Axis_X_Min)
                        X_Value = _Axis_X_Min;
                    if (Y_Value < -1.0f)
                        Y_Value = -1.0f;
                    if (X_Value > _Axis_X_Max)
                        X_Value = _Axis_X_Max;
                    if (Y_Value > 1.0f)
                        Y_Value = 1.0f;

                    //Store data in [0-1000] range to store as Int and divise later and get float value
                    PlayerController.Computed_X = Convert.ToInt16(X_Value * 1000);
                    PlayerController.Computed_Y = Convert.ToInt16(Y_Value * 1000);

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
            // Game is displaying gun on screen if less than 2 players, forcing it to hide
            if (_GameConfig.NoGun)
                WriteByte(GameProcess_ModuleBaseAddress + _NoGun_Patch_Offset, 0xEB);

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        #endregion

        #region Inputs Hack

        protected override void Apply_InputsMemoryHack()
        {
            Create_InputsDataBank();
            _Axis_X_CaveAddress = InputsDatabank_Address;
            _Axis_Y_CaveAddress = InputsDatabank_Address + 0x10;
            _Trigger_CaveAddress = InputsDatabank_Address + 0x20;
            _Reload_CaveAddress = InputsDatabank_Address + 0x24;
            _Grenade_CaveAddress = InputsDatabank_Address + 0x28;
            _PlayerOnOff_CaveAddress = InputsDatabank_Address + 0x30;
            _CoverLeft_CaveAddress = InputsDatabank_Address + 0x40;
            _CoverRight_CaveAddress = InputsDatabank_Address + 0x41;
            _CoverDown_CaveAddress = InputsDatabank_Address + 0x42;
            _QteSpace_CaveAddress = InputsDatabank_Address + 0x43;
            _QteW_CaveAddress = InputsDatabank_Address + 0x44;

            //Game is changing windows cursor position en enter/quit window focus, causing trouble to debug with a mouse as a lightgun
            SetNops(GameProcess_ModuleBaseAddress, _Nop_MouseCursorWhenGetFocus);
            SetNops(GameProcess_ModuleBaseAddress, _Nop_MouseCursorWhenLooseFocus);

            //STEAM version and ISO version does not have the same controller procedure, so the patch will be different
            //to enable/disable players dynamically
            //Unfortunatelly, for HF3, there are some releases with STEAM name (heavyfire3_final.exe) but with Non-Steam binary content (O_O)
            //Not beeing able to base the corresponding hack on the filename, MD5 check will be used, at the risk of failing if one binary is slightly modified and has a different MD5 sum....
            if (!_TargetProcess_Md5Hash.Equals(_KnownMd5Prints["Heavy Fire 3 - STEAM"]))
            {
                SetHack_EnablePlayers_Skidrow();
            }
            else
            {
                //Steam game has a setting to force mouse as main controller if a gamepad is plugged (or not)
                //Forcing check to act as if the setting is 1
                SetNops(GameProcess_ModuleBaseAddress, _Nop_SteamForceMouseMainController);
                SetHack_EnablePlayers_Steam();
            }

            SetHack_Mouse_Buttons();
            SetHack_Mouse_X();
            SetHack_Mouse_Y();

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        /// <summary>
        /// The game constantly sets Players controllers to either 0(OFF), 1(PAD) or 4(MOUSE)
        /// Forcing it to be our own value allows us to enable disable P2~P4 as we want
        /// </summary>
        private void SetHack_EnablePlayers_Skidrow()
        {
            //First we need to deactivate original set
            WriteBytes(GameProcess_ModuleBaseAddress + _PlayersOnOff_InjectionStruct.InjectionOffset + 0x0C, new byte[] { 0xEB, 0x12 });

            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //push eax
            CaveMemory.Write_StrBytes("50");
            //mov eax,esi
            CaveMemory.Write_StrBytes("8B C6");
            //add eax,_PlayerOnOff_CaveAddress
            CaveMemory.Write_StrBytes("05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_PlayerOnOff_CaveAddress + 1));    //Only looping for P2~P4 (not P1)
            //mov al,[eax]
            CaveMemory.Write_StrBytes("8A 00");
            //mov [ebx],al
            CaveMemory.Write_StrBytes("88 03");
            //pop eax
            CaveMemory.Write_StrBytes("58");

            //Inject it
            CaveMemory.InjectToOffset(_PlayersOnOff_InjectionStruct, "Players ON/OFF (Skidrow)");
        }

        /// <summary>
        /// The game constantly sets Players controllers to either 0(OFF), 1(PAD) or 4(MOUSE)
        /// Forcing it to be our own value allows us to enable disable P2~P4 as we want
        /// </summary>
        private void SetHack_EnablePlayers_Steam()
        {
            //First we need to force some check
            WriteBytes(GameProcess_ModuleBaseAddress + _PlayersOnOff_InjectionStruct.InjectionOffset - 0x35, new byte[] { 0xEB, 0x2F });

            //And force the byte to be stored
            //mov [edi], al
            WriteBytes(GameProcess_ModuleBaseAddress + _PlayersOnOff_InjectionStruct.InjectionOffset + 0x0A, new byte[] { 0x88, 0x07, 0x90, 0x90, 0x90, 0x90 });

            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //mov eax,esi
            CaveMemory.Write_StrBytes("8B C6");
            //add eax,_PlayerOnOff_CaveAddress
            CaveMemory.Write_StrBytes("05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_PlayerOnOff_CaveAddress + 1));    //Only looping for P2~P4 (not P1)
            //mov al,[eax]
            CaveMemory.Write_StrBytes("8A 00");
            //cmp [esi*4+hf4.exe+4234D4],ebp
            CaveMemory.Write_StrBytes("83 3C B5 D4 34 62 00 01");

            //Inject it
            CaveMemory.InjectToOffset(_PlayersOnOff_InjectionStruct, "Players ON/OFF (Steam)");
        }

        /// <summary>
        /// Overwriting results when the game is checking buttons one after the other
        /// also possible to change keys and/or override keyboard input if needed
        /// </summary>
        private void SetHack_Mouse_Buttons()
        {
            //First set to 0 any mouse input when the game is reading them
            WriteBytes(GameProcess_ModuleBaseAddress + _Mouse_Buttons_PatchOffset, new byte[] { 0x31, 0xC0, 0xC3 });

            //Then overriding with our own values
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //mov [esi+0000009B],al
            CaveMemory.Write_StrBytes("88 86 9B 00 00 00");
            //push ebx
            CaveMemory.Write_StrBytes("53");
            //mov ebx,[esp+04]                          //Player Id
            CaveMemory.Write_StrBytes("8B 5C 24 04");
            //add ebx,_P1_Trigger_CaveAddress
            CaveMemory.Write_StrBytes("81 C3");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Trigger_CaveAddress));
            //mov al,[ebx]
            CaveMemory.Write_StrBytes("8A 03");
            //mov [esi+00000080],al
            CaveMemory.Write_StrBytes("88 86 80 00 00 00");
            //mov [esi+00000086],al
            CaveMemory.Write_StrBytes("88 86 86 00 00 00");
            //add ebx,04
            CaveMemory.Write_StrBytes("83 C3 04");      //Reload byte is +4 after Trigger cave address
            //mov al,[ebx]
            CaveMemory.Write_StrBytes("8A 03");
            //mov [esi+00000083],al
            CaveMemory.Write_StrBytes("88 86 83 00 00 00");
            //mov ebx,[esp+04]
            CaveMemory.Write_StrBytes("8B 5C 24 04");
            //add ebx,_Grenade_CaveAddress
            CaveMemory.Write_StrBytes("81 C3");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Grenade_CaveAddress));
            //mov al,[ebx]
            CaveMemory.Write_StrBytes("8A 03");
            //mov [esi+00000082],al
            CaveMemory.Write_StrBytes("88 86 82 00 00 00");

            //If "Pedal Mode" is enabled, pressing the pedal while pointing at a screen border will add possibility to 
            //enable W,A,S,D key press
            //Game initially use 0/1 on button press, but using any value > 0 looks like it' working.
            //That way all 4 player buttons need to be release to be at 0.
            if (_GameConfig.GetBoolOption(PEDALMODE_OPTION))
            {
                //movzx ebx,byte ptr [_CoverLeft_CaveAddress]
                CaveMemory.Write_StrBytes("0F B6 1D");
                CaveMemory.Write_Bytes(BitConverter.GetBytes(_CoverLeft_CaveAddress));
                //or [esi+00000095],bl
                CaveMemory.Write_StrBytes("08 9E 95 00 00 00"); //A

                //movzx ebx,byte ptr [_CoverRight_CaveAddress]
                CaveMemory.Write_StrBytes("0F B6 1D");
                CaveMemory.Write_Bytes(BitConverter.GetBytes(_CoverRight_CaveAddress));
                //or [esi+00000097],bl
                CaveMemory.Write_StrBytes("08 9E 97 00 00 00"); //D

                //movzx ebx,byte ptr [_CoverRight_CaveAddress]
                CaveMemory.Write_StrBytes("0F B6 1D");
                CaveMemory.Write_Bytes(BitConverter.GetBytes(_CoverDown_CaveAddress));
                //or [esi+00000096],bl
                CaveMemory.Write_StrBytes("08 9E 96 00 00 00"); //S

                //movzx ebx,byte ptr [_QteW_CaveAddress]
                CaveMemory.Write_StrBytes("0F B6 1D");
                CaveMemory.Write_Bytes(BitConverter.GetBytes(_QteW_CaveAddress));
                //or [esi+00000092],bl
                CaveMemory.Write_StrBytes("08 9E 92 00 00 00"); //W

                //movzx ebx,byte ptr [_QteW_CaveAddress]
                CaveMemory.Write_StrBytes("0F B6 1D");
                CaveMemory.Write_Bytes(BitConverter.GetBytes(_QteSpace_CaveAddress));
                //or [esi+00000093],bl
                CaveMemory.Write_StrBytes("08 9E 93 00 00 00"); //W
            }

            //Exit:            
            //pop ebx
            CaveMemory.Write_StrBytes("5B");

            //Inject it
            CaveMemory.InjectToOffset(_Mouse_Buttons_InjectionStruct, "P1 Buttons");
        }

        /// <summary>
        /// All Axis codecave are the same :
        /// The game use some fstp [XXX] instruction, but we can't just NOP it as graphical glitches may appear.
        /// So we just add another set of instructions instruction immediatelly after to change the register 
        /// to our own desired value
        /// </summary>
        private void SetHack_Mouse_X()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //fstp [esi+edi*8+000000F0]
            CaveMemory.Write_StrBytes("D9 9C FE F0 00 00 00");
            //push eax
            CaveMemory.Write_StrBytes("50");
            //mov eax,edi
            CaveMemory.Write_StrBytes("8B C7");
            //shl eax,02
            CaveMemory.Write_StrBytes("C1 E0 02");
            //add eax,_Axis_X_CaveAddress
            CaveMemory.Write_StrBytes("05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Axis_X_CaveAddress));
            //mov eax,[eax]
            CaveMemory.Write_StrBytes("8B 00");
            //mov [esi+edi*8+000000F0], eax
            CaveMemory.Write_StrBytes("89 84 FE F0 00 00 00");
            //pop eax
            CaveMemory.Write_StrBytes("58");

            //Inject it
            CaveMemory.InjectToOffset(_Mouse_X_InjectionStruct, "P1 X Axis");
        }

        private void SetHack_Mouse_Y()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //fstp [esi+edi*8+000000F4]
            CaveMemory.Write_StrBytes("D9 9C FE F4 00 00 00");
            //push eax
            CaveMemory.Write_StrBytes("50");
            //mov eax,edi
            CaveMemory.Write_StrBytes("8B C7");
            //shl eax,02
            CaveMemory.Write_StrBytes("C1 E0 02");
            //add eax,_Axis_Y_CaveAddress
            CaveMemory.Write_StrBytes("05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Axis_Y_CaveAddress));
            //mov eax,[eax]
            CaveMemory.Write_StrBytes("8B 00");
            //mov [esi+edi*8+000000F4], eax
            CaveMemory.Write_StrBytes("89 84 FE F4 00 00 00");
            //pop eax
            CaveMemory.Write_StrBytes("58");

            //Inject it
            CaveMemory.InjectToOffset(_Mouse_Y_InjectionStruct, "P1 Y Axis");
        }

        #endregion

        #region Outputs Hack

        protected override void Apply_OutputsMemoryHack()
        {
            Create_OutputsDataBank();
            _GamePlaying_CaveAddress = OutputsDatabank_Address;
            _Recoil_CaveAddress = OutputsDatabank_Address + 0x04;
            _Damage_CaveAddress = OutputsDatabank_Address + 0x08;
            _Ammo_CaveAddress = OutputsDatabank_Address + 0x10;

            SetHack_Recoil();
            SetHack_Ammo();
            SetHack_IsPlaying();
            SetHack_Damage();

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        /// <summary>
        /// This code is run constantly and sets some 0/1 flag we can intercept for our own use
        /// </summary>
        private void SetHack_IsPlaying()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //mov [_P1_Playing_CaveAddress],al
            CaveMemory.Write_StrBytes("A2");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_GamePlaying_CaveAddress));
            //mov ecx,[esp+00000450]
            CaveMemory.Write_StrBytes("8B 8C 24 50 04 00 00");

            //Inject it
            CaveMemory.InjectToOffset(_GamePlaying_InjectionStruct, "Player Playing");
        }

        /// <summary>
        /// Set a flag when the game calls for a shooting procedure
        /// </summary>
        private void SetHack_Recoil()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //mov edi,esi
            CaveMemory.Write_StrBytes("8B FE");
            //add edi,_Recoil_CaveAddress
            CaveMemory.Write_StrBytes("81 C7");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Recoil_CaveAddress));
            //mov byte ptr[edi], 01
            CaveMemory.Write_StrBytes("C6 07 01");
            //mov edi,[esp+18]
            CaveMemory.Write_StrBytes("8B 7C 24 18");
            //push esi
            CaveMemory.Write_StrBytes("56");

            //Inject it
            CaveMemory.InjectToOffset(_Recoil_InjectionStruct, "Recoil");
        }

        /// <summary>
        /// This function is called in a loop while the player is playing
        /// We can get Ammo status in it
        /// </summary>
        private void SetHack_Ammo()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //push eax
            CaveMemory.Write_StrBytes("50");
            //push edx
            CaveMemory.Write_StrBytes("52");
            //mov edx,[eax+esi+0000051C]
            CaveMemory.Write_StrBytes("8B 94 30 1C 05 00 00");
            //mov eax,ebx
            CaveMemory.Write_StrBytes("8B C3");
            //shl eax,02
            CaveMemory.Write_StrBytes("C1 E0 02");
            //add eax,_Ammo_CaveAddress
            CaveMemory.Write_StrBytes("05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Ammo_CaveAddress));
            //mov [eax],edx
            CaveMemory.Write_StrBytes("89 10");
            //pop edx
            CaveMemory.Write_StrBytes("5A");
            //pop eax
            CaveMemory.Write_StrBytes("58");
            //cmp dword ptr [eax+esi+0000051C],00
            CaveMemory.Write_StrBytes("83 BC 30 1C 05 00 00 00");

            //Inject it
            CaveMemory.InjectToOffset(_Ammo_InjectionStruct, "Ammo");
        }

        /// <summary>
        /// That code is called when the game needs to increment the hit counter of the player, and also run the "hit_00" soune effect ?
        /// We can get the player ID in ESP+24 after pushing EAX
        /// </summary>
        private void SetHack_Damage()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //push eax
            CaveMemory.Write_StrBytes("50");
            //mov eax,[esp+24]
            CaveMemory.Write_StrBytes("8B 44 24 24");
            //add eax,_Damage_CaveAddress
            CaveMemory.Write_StrBytes("05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Damage_CaveAddress));
            //mov byte ptr[eax],01
            CaveMemory.Write_StrBytes("C6 00 01");
            //pop eax
            CaveMemory.Write_StrBytes("58");
            //add [ebx+eax+00000524],ecx
            CaveMemory.Write_StrBytes("01 8C 03 24 05 00 00");

            //Inject it
            CaveMemory.InjectToOffset(_Damage_InjectionStruct, "Damage");
        }
        #endregion

        #region No Crosshair Hack

        /// <summary>
        /// Changing X/Y values to -10.0 for every elements in the HUD (crosshair, reload, etc...)
        /// </summary>
        protected override void Apply_NoCrosshairMemoryHack()
        {
             Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //fst qword ptr [esp+44]
            CaveMemory.Write_StrBytes("DD 54 24 44");
            //mov [esp+50],C1200000
            CaveMemory.Write_StrBytes("C7 44 24 50 00 00 20 C1");
            //mov [esp+4C],C1200000
            CaveMemory.Write_StrBytes("C7 44 24 4C 00 00 20 C1");
            //fsubr dword ptr [esp+50]
            CaveMemory.Write_StrBytes("D8 6C 24 50");

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
                //Close();
            }
        }

        protected override void GameController_OnButtonUp(object sender, VirtualGameControler.ButtonDownEventArgs e)
        {  }

        protected override void UpdateAxis(PlayerID Id, int X, int Y)
        {
            if (Id != PlayerID.None)
            {
                float fX = (float)X / 1000.0f;
                float fY = (float)Y / 1000.0f;

                byte[] bufferX = BitConverter.GetBytes(fX);
                byte[] bufferY = BitConverter.GetBytes(fY);

                //Setting Values in memory for the Codecave to read it
                WriteBytes(_Axis_X_CaveAddress + ((uint)Id * 0x04), bufferX);
                WriteBytes(_Axis_Y_CaveAddress + ((uint)Id * 0x04), bufferY);
            }
        }

        protected override void PlayerController_OnButtonDown(object sender, VirtualGameControler.ButtonDownEventArgs e)
        {
            if (e.Id != PlayerID.None)
            {
                UInt32 PlayerIndex = (UInt32)e.Id;

                if (e.ButtonName.Equals("TRIGGER"))
                {
                    WriteByte(_Trigger_CaveAddress + PlayerIndex, 0x01);
                }
                else if (e.ButtonName.Equals("RELOAD"))
                {
                    WriteByte(_Reload_CaveAddress + PlayerIndex, 0x01);
                }
                /// Using those custom keys, we can force players to be created (4=mouse controller) or not in the game
                /// Setting value to 1 ==> Auto validate popup to select profile, but need some patch to force the game to compute mouse procedure
                /// Setting value to 4 ==> Need validate popup to select profile (genuine way ??)
                else if (e.ButtonName.Equals("START"))
                {
                    if (ReadByte(_PlayerOnOff_CaveAddress + PlayerIndex) != 0)
                        WriteByte(_PlayerOnOff_CaveAddress + PlayerIndex, 0x00);
                    else
                        WriteByte(_PlayerOnOff_CaveAddress + PlayerIndex, 0x04);
                }
                else if (e.ButtonName.Equals("GRENADE"))
                {
                    WriteByte(_Grenade_CaveAddress + PlayerIndex, 0x01);
                }
                else if (e.ButtonName.Equals("PEDAL"))
                {
                    float fX = (float)((VirtualPlayerController)sender).Computed_X / 1000.0f;
                    float fY = (float)((VirtualPlayerController)sender).Computed_Y / 1000.0f;
                    int iMask = 0x01 << (int)PlayerIndex;

                    //If the player is aiming on the of the screen pressing the button
                    //Left Side => Cover Left
                    //Right side => Cover Right
                    //Bottom Side => Cover Center
                    //If Option "Reverse" Cover is checked = mirror
                    if ((fX < _Axis_X_Min + _CoverDelta))
                    {
                        if (!_GameConfig.GetBoolOption(REVERSECOVER_OPTION))
                            Apply_OR_ByteMask(_CoverLeft_CaveAddress, (byte)iMask);
                        else
                            Apply_OR_ByteMask(_CoverRight_CaveAddress, (byte)iMask);
                    }
                    else if ((fX > _Axis_X_Max - _CoverDelta))
                    {
                        if (!_GameConfig.GetBoolOption(REVERSECOVER_OPTION))
                            Apply_OR_ByteMask(_CoverRight_CaveAddress, (byte)iMask);
                        else
                            Apply_OR_ByteMask(_CoverLeft_CaveAddress, (byte)iMask);
                    }
                    else if (fY > (1.0f - _CoverDelta))
                    {
                        if (!_GameConfig.GetBoolOption(REVERSECOVER_OPTION))
                            Apply_OR_ByteMask(_CoverDown_CaveAddress, (byte)iMask);
                        else
                            Apply_OR_ByteMask(_QteW_CaveAddress, (byte)iMask);
                    }
                    else if (fY < (-1.0f + _CoverDelta))
                    {
                        if (!_GameConfig.GetBoolOption(REVERSECOVER_OPTION))
                            Apply_OR_ByteMask(_QteW_CaveAddress, (byte)iMask);
                        else
                            Apply_OR_ByteMask(_CoverDown_CaveAddress, (byte)iMask);
                    }
                    else
                    {
                        Apply_OR_ByteMask(_QteSpace_CaveAddress, (byte)iMask);
                    }
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
                    WriteByte(_Trigger_CaveAddress + PlayerIndex, 0x00);
                }
                else if (e.ButtonName.Equals("RELOAD"))
                {
                    WriteByte(_Reload_CaveAddress + PlayerIndex, 0x00);
                }
                else if (e.ButtonName.Equals("GRENADE"))
                {
                    WriteByte(_Grenade_CaveAddress + PlayerIndex, 0x00);
                }
                else if (e.ButtonName.Equals("PEDAL"))
                {
                    float fX = (float)((VirtualPlayerController)sender).Computed_X / 1000.0f;
                    float fY = (float)((VirtualPlayerController)sender).Computed_Y / 1000.0f;
                    int iMask = 0x01 << (int)PlayerIndex;

                    //Releasing "Pedal" will disable all QTE/Hide events
                    Apply_AND_ByteMask(_CoverLeft_CaveAddress, (byte)~iMask);
                    Apply_AND_ByteMask(_CoverRight_CaveAddress, (byte)~iMask);
                    Apply_AND_ByteMask(_CoverDown_CaveAddress, (byte)~iMask);
                    Apply_AND_ByteMask(_QteW_CaveAddress, (byte)~iMask);
                    Apply_AND_ByteMask(_QteSpace_CaveAddress, (byte)~iMask);
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
                new GameOutput(OutputId.P2_Ammo),
                new GameOutput(OutputId.P3_Ammo),
                new GameOutput(OutputId.P4_Ammo),
                new AsyncGameOutput(OutputId.P1_CtmRecoil, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_ON_DELAY), ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_OFF_DELAY), 0),
                new AsyncGameOutput(OutputId.P2_CtmRecoil, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_ON_DELAY), ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_OFF_DELAY), 0),
                new AsyncGameOutput(OutputId.P3_CtmRecoil, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_ON_DELAY), ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_OFF_DELAY), 0),
                new AsyncGameOutput(OutputId.P4_CtmRecoil, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_ON_DELAY), ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_OFF_DELAY), 0),
                new AsyncGameOutput(OutputId.P1_Damaged, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_DAMAGED_DELAY), 100, 0),
                new AsyncGameOutput(OutputId.P2_Damaged, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_DAMAGED_DELAY), 100, 0),
                new AsyncGameOutput(OutputId.P3_Damaged, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_DAMAGED_DELAY), 100, 0),
                new AsyncGameOutput(OutputId.P4_Damaged, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_DAMAGED_DELAY), 100, 0)
            };
        }

        /// <summary>
        /// Update all Outputs values before sending them to MameHooker
        /// </summary>
        public override void UpdateOutputValues()
        {
            for (uint i = 0; i < _GameConfig.PlayerCount; i++)
            {
                _Ammo[i] = 0;

                if (ReadByte(_GamePlaying_CaveAddress) == 1)
                {
                    //Recoil
                    if (ReadByte(_Recoil_CaveAddress + i) == 1)
                    {
                        SetOutputValue(OutputId.P1_CtmRecoil + i, 1);
                        WriteByte(_Recoil_CaveAddress + i, 0);
                    }
                }

                _Ammo[i] = ReadInt32(_Ammo_CaveAddress + (i * 0x04));
                SetOutputValue(OutputId.P1_Ammo + i, _Ammo[i]);

                //Damage
                if (ReadByte(_Damage_CaveAddress + i) == 1)
                {
                    SetOutputValue(OutputId.P1_Damaged + i, 1);
                    WriteByte(_Damage_CaveAddress + i, 0);
                }
            }
        }
        #endregion
    }
}
