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
    public class Game_RtHalo : BaseGame64
    {
        protected override bool PATCH_PEB_DEBUG_FLAG { get { return false; } }        

        private const string ELFPATH_OPTION = "Game ELF";
        private const string LOADEROPTIONS_OPTION = "LinuxLoader options";

        #region LOADER MEMORY ADDRESSES                
        //Check instruction for game loaded
        private UInt64 _GameLoaded_Check_Address = 0x0053F28B;
        private InjectionStruct64 _NoCrosshair_InjectionStruct = new InjectionStruct64(0x0044085F, 18);
        #endregion

        #region INPUTS MEMORY ADDRESSES    
        private InjectionStruct64 _Axes_InjectionStruct = new InjectionStruct64(0x006241DB, 15);
        private InjectionStruct64 _Buttons_Injectionstruct = new InjectionStruct64(0x004AD764, 18);

        //Custom Values
        private UInt64 _Axes_CaveAddress = 0;
        private UInt64 _Buttons_CaveAddress = 0;
        #endregion

        #region OUTPUTS MEMORY ADDRESSES
        private UInt64 _Lamp_Address = 0x01C0D1E0;
        private UInt64 _P1_Status_Address = 0x01C0C7C0;
        private UInt64 _P1_Life_Address = 0x01C0C7D4;
        private UInt64 _P1_Ammo_Address = 0x01C0C870;
        private InjectionStruct64 _Recoil_InjectionStruct = new InjectionStruct64(0x0042EB32, 15);
        private InjectionStruct64 _Damage_InjectionStruct = new InjectionStruct64(0x00419954, 15);

        //Custom Values
        private UInt64 _RecoilStatus_CaveAddress = 0;
        private UInt64 _DamageStatus_CaveAddress = 0;
        #endregion

        public Game_RtHalo(GameConfig Config) : base(Config)
        {
            string ElfPath = _GameConfig.GetStringOption(ELFPATH_OPTION + " [game]");
            string LibStdCpp_Path = Path.Combine(Path.GetDirectoryName(_GameConfig.TargetPath), "libs", "libstdc++.so.6.0.25");

            _GameLauncher.Set_CmdLine_Args("--run \"" + ElfPath + "\"" + " \"" + LibStdCpp_Path + "\" " + _GameConfig.GetStringOption(LOADEROPTIONS_OPTION));

            _KnownMd5Prints.Add("Halo Fireteam Raven - v3.11", "279f21163f0c3832d4d180b5e2671b58");

            if (_GameConfig.HideConsole)
            {
                _GameLauncher.RedirectOutputsToNull = true;
                _GameLauncher.CreateNoWindow = true;
            }

            //If the runner Cmd window is visible, it contains HALO in it (because of game opath in Title) and can be wrongly used
            //Ensuring the window starts with HALO (game window)
            _WindowTitleFilterMode = WindowTitleFilter.StartWith;
        }

        protected override void ProcessTimerTick_Unhooked_CheckProc()
        {
            try
            {
                //To make sure LinuxLoader has loaded the rom entirely, we're looking for some random instruction to be present in memory before starting
                byte[] buffer = ReadBytes(_GameLoaded_Check_Address, 3);
                if (buffer.SequenceEqual(new byte[] { 0x8B, 0x40, 0x50 }))
                {
                    Logger.WriteLog("Halo Fireteam Raven - v3.11");
                }
                else
                {
                    Logger.WriteLog("Game not Loaded, waiting...");
                    return;
                }

                ReadGameDataFromMd5Hash(GAMEDATA_FOLDER);
                Apply_MemoryHacks();
            }
            catch (Exception Ex)
            {
                Logger.WriteLog("Error waiting for the game to be loaded : " + Ex.Message);
            }
        }

        #region Debugger Events

        #region EXCEPTION_DEBUG_EVENT

        protected override void ExceptionEvent(object sender, x86_x64_Debugger.ExceptionDebugEventArgs e)
        {
            //No need to to anything here, U-Shoot will have to wait for the ROM to be loaded in memory to apply Memory Hacks
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
                    double dMaxX = 1023.0;
                    double dMaxY = 1023.0;
                    double dRangeX = dMaxX - dMinX + 1;
                    double dRangeY = dMaxY - dMinY + 1;

                    PlayerController.Computed_X = Convert.ToInt32(Math.Round(dRangeX * PlayerController.Computed_X / TotalResX));
                    PlayerController.Computed_Y = Convert.ToInt32(dRangeY - Math.Round(dRangeY * PlayerController.Computed_Y / TotalResY));
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
            _Axes_CaveAddress = InputsDatabank_Address;             //4 x 4 Bytes for X and 4 x 4  Bytes for Y => 32 Bytes
            _Buttons_CaveAddress = InputsDatabank_Address + 0x20;   //Each button slot will take 16 Bytes, there are up to 72 slots => 1152 Bytes (0x480)

            SetHack_Axes();
            SetHack_Buttons();

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        /// <summary>
        /// Instead of calling 00DD6C60() to get float axes values, replacing by our own
        /// </summary>
        private void SetHack_Axes()
        {
            Codecave64 CaveMemory = new Codecave64(_Process, GameProcess_ModuleBaseAddress);

            //mov rsi,[rsp+10]
            CaveMemory.Write_StrBytes("48 8B 74 24 10");
            //mov edi,ebx
            CaveMemory.Write_StrBytes("89 DF");
            //mov r13,rax
            CaveMemory.Write_StrBytes("49 89 C5");

            //mov rax,_Axes_CaveAddress
            CaveMemory.Write_StrBytes("48 B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Axes_CaveAddress));
            //add rax,rdi
            CaveMemory.Write_StrBytes("48 01 F8");
            //add rax,rdi
            CaveMemory.Write_StrBytes("48 01 F8");
            //add rax,rdi
            CaveMemory.Write_StrBytes("48 01 F8");
            //add rax,rdi
            CaveMemory.Write_StrBytes("48 01 F8");
            //mov rax,[rax]
            CaveMemory.Write_StrBytes("48 8B 00");
            //mov [rsi],rax
            CaveMemory.Write_StrBytes("48 89 06");
           
            //mov rax,_Axes_CaveAddress + 0x10
            CaveMemory.Write_StrBytes("48 B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Axes_CaveAddress + 0x10));
            //add rax,rdi
            CaveMemory.Write_StrBytes("48 01 F8");
            //add rax,rdi
            CaveMemory.Write_StrBytes("48 01 F8");
            //add rax,rdi
            CaveMemory.Write_StrBytes("48 01 F8");
            //add rax,rdi
            CaveMemory.Write_StrBytes("48 01 F8");
            //mov rax,[rax]
            CaveMemory.Write_StrBytes("48 8B 00");
            //mov [rdx],rax
            CaveMemory.Write_StrBytes("48 89 02");

            //Inject it
            CaveMemory.InjectToAddress(_Axes_InjectionStruct, "Axes");
        }

        /// <summary>
        /// Halo-Loader handles the inputs and sets memory values that are used by the game to detect button states.
        /// Inserting a custom-buttons-buffer to create similar key press data (increasing a byte each frame the key is pressed) to replace the original ones
        /// </summary>
        private void SetHack_Buttons()
        {
            Codecave64 CaveMemory = new Codecave64(_Process, GameProcess_ModuleBaseAddress);

            /*** Uncomment This to jump over the whole proc ***/
            //jmp
            //CaveMemory.Write_StrBytes("EB 60");

            /*** That check only applies the hack for Player Guns Buttons ***/
            //cmp r14,10
            CaveMemory.Write_StrBytes("49 83 FE 10");
            //jb OriginalCode
            CaveMemory.Write_StrBytes("72 5A");
            //cmp r14,2A
            CaveMemory.Write_StrBytes("49 83 FE 2A");
            //ja OriginalCode
            CaveMemory.Write_StrBytes("77 54");


            //mov r10,0000000000000010
            CaveMemory.Write_StrBytes("49 BA 10 00 00 00 00 00 00 00");
            //imul r10d,r14d
            CaveMemory.Write_StrBytes("45 0F AF D6");
            //mov rax,_Buttons_CaveAddress
            CaveMemory.Write_StrBytes("48 B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Buttons_CaveAddress));    
            //add rax,r10
            CaveMemory.Write_StrBytes("4C 01 D0");
            //cmp byte ptr [rax],01
            CaveMemory.Write_StrBytes("80 38 01");
            //je Pressed
            CaveMemory.Write_StrBytes("74 10");

            //mov [rax+04],00000000
            CaveMemory.Write_StrBytes("C7 40 04 00 00 00 00");
            //mov [rax+0C],00000000
            CaveMemory.Write_StrBytes("C7 40 0C 00 00 00 00");
            //jmp Copy
            CaveMemory.Write_StrBytes("EB 12");

            //Pressed:
            //mov [rax+04],00000001
            CaveMemory.Write_StrBytes("C7 40 04 01 00 00 00");
            //mov r10,[rax+0C]
            CaveMemory.Write_StrBytes("4C 8B 50 0C");
            //inc r10
            CaveMemory.Write_StrBytes("49 FF C2");
            //mov [rax+0C],r10
            CaveMemory.Write_StrBytes("4C 89 50 0C");

            //Copy:
            //mov r10d,[rax+04]
            CaveMemory.Write_StrBytes("44 8B 50 04");
            //mov [r12+04],r10d
            CaveMemory.Write_StrBytes("45 89 54 24 04");
            //mov r10d,[rax+0C]
            CaveMemory.Write_StrBytes("44 8B 50 0C");
            //mov [r12+0C],r10d
            CaveMemory.Write_StrBytes("45 89 54 24 0C");

            //OriginalCode
            //mov eax,[r12+0C]
            CaveMemory.Write_StrBytes("41 8B 44 24 0C");
            //cmp eax,[rbx+0C]
            CaveMemory.Write_StrBytes("3B 43 0C");
            //jna 
            CaveMemory.Write_StrBytes("76 03");
            //mov [rbx+0C],eax
            CaveMemory.Write_StrBytes("89 43 0C");
            //mov eax,[r12+04]
            CaveMemory.Write_StrBytes("41 8B 44 24 04");

            //Inect it
            CaveMemory.InjectToAddress(_Buttons_Injectionstruct, "Buttons");

        }

        #endregion

        #region Outputs Hack

        protected override void Apply_OutputsMemoryHack()
        {
            Create_OutputsDataBank();
            _RecoilStatus_CaveAddress = OutputsDatabank_Address;
            _DamageStatus_CaveAddress = OutputsDatabank_Address + 0x04;

            SetHack_Damage();
            SetHack_Recoil();

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        /// <summary>
        /// Intercep call to log damage taken by a player to set a custom flag to "1"
        /// </summary>
        private void SetHack_Damage()
        {
            Codecave64 CaveMemory = new Codecave64(_Process, GameProcess_ModuleBaseAddress);

            //mov r9,_DamageStatus_CaveAddress
            CaveMemory.Write_StrBytes("49 B9");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_DamageStatus_CaveAddress));
            //add r9,r12
            CaveMemory.Write_StrBytes("4D 01 E1");
            //mov byte ptr [r9],01
            CaveMemory.Write_StrBytes("41 C6 01 01");

            //lea rdi,[rsp+20]
            CaveMemory.Write_StrBytes("48 8D 7C 24 20");
            //lea r9d,[r12+01]
            CaveMemory.Write_StrBytes("45 8D 4C 24 01");
            //mov esi,00000040
            CaveMemory.Write_StrBytes("BE 40 00 00 00");

            //Inject it
            CaveMemory.InjectToAddress(_Damage_InjectionStruct, "Damage");
        }

        /// <summary>
        /// Intercept call for checking ForceFeedback enabled setting on shoot
        /// </summary>
        private void SetHack_Recoil()
        {
            Codecave64 CaveMemory = new Codecave64(_Process, GameProcess_ModuleBaseAddress);

            //mov rax,_RecoilStatus_CaveAddress
            CaveMemory.Write_StrBytes("48 B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_RecoilStatus_CaveAddress));
            //add rax,rbp
            CaveMemory.Write_StrBytes("48 01 E8");
            //mov byte ptr [rax],01
            CaveMemory.Write_StrBytes("C6 00 01");
            //xor rax,rax
            CaveMemory.Write_StrBytes("48 31 C0");

            //mov ecx,00000040
            CaveMemory.Write_StrBytes("B9 40 00 00 00");
            //mov edx,00000001
            CaveMemory.Write_StrBytes("BA 01 00 00 00");
            //mov esi,00000040
            CaveMemory.Write_StrBytes("BE 40 00 00 00");

            //Inject it
            CaveMemory.InjectToAddress(_Recoil_InjectionStruct, "Recoil");
        }

        #endregion

        #region No Crosshair Hack

        /// <summary>
        /// Forcing one axis value to -1.0f to draw crosshairs
        /// </summary>
        protected override void Apply_NoCrosshairMemoryHack()
        {

            Codecave64 CaveMemory = new Codecave64(_Process, GameProcess_ModuleBaseAddress);

            //lea rdx,[rsp+20]
            CaveMemory.Write_StrBytes("48 8D 54 24 20");
            //lea rsi,[rsp+10]
            CaveMemory.Write_StrBytes("48 8D 74 24 10");
            //mov [rsp+18],00000000
            CaveMemory.Write_StrBytes("C7 44 24 18 00 00 00 00");
            //mov [rsi],BF800000
            CaveMemory.Write_StrBytes("C7 06 00 00 80 BF");

            //InjectIt
            CaveMemory.InjectToAddress(_NoCrosshair_InjectionStruct, "NoCrosshair");

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
            else if (e.ButtonName.Equals("TEST"))
            {
                Set_InputSlot_State(0x3A, 1);
            }
        }

        protected override void GameController_OnButtonUp(object sender, VirtualGameControler.ButtonDownEventArgs e)
        {
            if (e.ButtonName.Equals("TEST"))
            {
                Set_InputSlot_State(0x3A, 0);
            }
        }

        protected override void UpdateAxis(PlayerID Id, int X, int Y)
        {
            if (Id != PlayerID.None)
            {
                //Float 'calibrated' axes values
                float cX = 1.0f * (float)X / 1024.0f;
                float cY = 1.0f * (float)Y / 1024.0f;
                WriteBytes(_Axes_CaveAddress + (4 * (UInt32)Id), BitConverter.GetBytes(cX));
                WriteBytes(_Axes_CaveAddress + 0x10 + (4 * (UInt32)Id), BitConverter.GetBytes(cY));
            }
        }

        protected override void PlayerController_OnButtonDown(object sender, VirtualGameControler.ButtonDownEventArgs e)
        {
            if (e.Id != PlayerID.None)
            {
                UInt32 PlayerIndex = (UInt32)e.Id;

                if (e.ButtonName.Equals("TRIGGER"))
                {
                    Set_InputSlot_State(0x10 + 8 * (int)PlayerIndex, 1);
                }
                else if (e.ButtonName.Equals("RELOAD"))
                {
                    Set_InputSlot_State(0x11 + 8 * (int)PlayerIndex, 1);
                }
                else if (e.ButtonName.Equals("GRENADE"))
                {
                    Set_InputSlot_State(0x12 + 8 * (int)PlayerIndex, 1);
                }
                else if (e.ButtonName.Equals("START"))
                {
                    switch (e.Id)
                    {
                        case PlayerID.Player1: Set_InputSlot_State(0x00, 1); break;
                        case PlayerID.Player2: Set_InputSlot_State(0x01, 1); break;
                        case PlayerID.Player3: Set_InputSlot_State(0x04, 1); break;
                        case PlayerID.Player4: Set_InputSlot_State(0x05, 1); break;
                    }
                }
                else if (e.ButtonName.Equals("COIN"))
                {
                    switch (e.Id)
                    {
                        case PlayerID.Player1: Set_InputSlot_State(0x08, 1); break;
                        case PlayerID.Player2: Set_InputSlot_State(0x0A, 1); break;
                        case PlayerID.Player3: Set_InputSlot_State(0x0C, 1); break;
                        case PlayerID.Player4: Set_InputSlot_State(0x0D, 1); break;
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
                    Set_InputSlot_State(0x10 + 8 * (int)PlayerIndex, 0);
                }
                else if (e.ButtonName.Equals("RELOAD"))
                {
                    Set_InputSlot_State(0x11 + 8 * (int)PlayerIndex, 0);
                }
                else if (e.ButtonName.Equals("GRENADE"))
                {
                    Set_InputSlot_State(0x12 + 8 * (int)PlayerIndex, 0);
                }
                else if (e.ButtonName.Equals("START"))
                {
                    switch (e.Id)
                    {
                        case PlayerID.Player1: Set_InputSlot_State(0x00, 0); break;
                        case PlayerID.Player2: Set_InputSlot_State(0x01, 0); break;
                        case PlayerID.Player3: Set_InputSlot_State(0x04, 0); break;
                        case PlayerID.Player4: Set_InputSlot_State(0x05, 0); break;
                    }
                }
                else if (e.ButtonName.Equals("COIN"))
                {
                    switch (e.Id)
                    {
                        case PlayerID.Player1: Set_InputSlot_State(0x08, 0); break;
                        case PlayerID.Player2: Set_InputSlot_State(0x0A, 0); break;
                        case PlayerID.Player3: Set_InputSlot_State(0x0C, 0); break;
                        case PlayerID.Player4: Set_InputSlot_State(0x0D, 0); break;
                    }
                }
            }
        }

        private void Set_InputSlot_State(int SlotId, byte State)
        {
            UInt64 BufferIndex = (UInt64)(0x10 * SlotId);

            WriteByte(_Buttons_CaveAddress + BufferIndex, State);
            if (State == 1)
                WriteByte(_Buttons_CaveAddress + BufferIndex + 1, State);
            else
                WriteByte(_Buttons_CaveAddress + BufferIndex + 2, State);

        }

        #endregion

        #region Outputs

        /// <summary>
        /// Create the Output list that we will be looking for and forward to MameHooker
        /// </summary>
        protected override void CreateOutputList()
        {
            //Gun motor : Is activated permanently while trigger is pressed
            _Outputs = new List<GameOutput>
            {
                new GameOutput(OutputId.Lmp_PodLeft_Panel),
                new GameOutput(OutputId.Lmp_PodLeft_Panel_R),
                new GameOutput(OutputId.Lmp_PodLeft_Panel_G),
                new GameOutput(OutputId.Lmp_PodLeft_Panel_B),
                new GameOutput(OutputId.Lmp_PodLeft_Panel_Front),
                new GameOutput(OutputId.Lmp_PodLeft_Panel_Under),
                new GameOutput(OutputId.Lmp_PodLeft_GunChase_R),
                new GameOutput(OutputId.Lmp_PodLeft_GunChase_G),
                new GameOutput(OutputId.Lmp_PodLeft_GunChase_B),
                new GameOutput(OutputId.Lmp_PodRight_Panel),
                new GameOutput(OutputId.Lmp_PodRight_Panel_R),
                new GameOutput(OutputId.Lmp_PodRight_Panel_G),
                new GameOutput(OutputId.Lmp_PodRight_Panel_B),
                new GameOutput(OutputId.Lmp_PodRight_Panel_Front),
                new GameOutput(OutputId.Lmp_PodRight_Panel_Under),
                new GameOutput(OutputId.Lmp_PodRight_GunChase_R),
                new GameOutput(OutputId.Lmp_PodRight_GunChase_G),
                new GameOutput(OutputId.Lmp_PodRight_GunChase_B),
                new GameOutput(OutputId.Lmp_Marquee_R),
                new GameOutput(OutputId.Lmp_Marquee_G),
                new GameOutput(OutputId.Lmp_Marquee_B),
                new GameOutput(OutputId.Lmp_Speaker),
                new GameOutput(OutputId.Lmp_Speaker_R),
                new GameOutput(OutputId.Lmp_Speaker_G),
                new GameOutput(OutputId.Lmp_Speaker_B),
                new GameOutput(OutputId.Lmp_RearSoldier),
                new GameOutput(OutputId.Lmp_MasterChief),
                new GameOutput(OutputId.Lmp_MonitorStand),
                new GameOutput(OutputId.P1_LmpStart),
                new GameOutput(OutputId.P2_LmpStart),
                new GameOutput(OutputId.P3_LmpStart),
                new GameOutput(OutputId.P4_LmpStart),
                new GameOutput(OutputId.Lmp_P1_Holster),
                new GameOutput(OutputId.Lmp_P1_Holster_R),
                new GameOutput(OutputId.Lmp_P1_Holster_G),
                new GameOutput(OutputId.Lmp_P1_Holster_B),
                new GameOutput(OutputId.Lmp_P2_Holster),
                new GameOutput(OutputId.Lmp_P2_Holster_R),
                new GameOutput(OutputId.Lmp_P2_Holster_G),
                new GameOutput(OutputId.Lmp_P2_Holster_B),
                new GameOutput(OutputId.Lmp_P3_Holster),
                new GameOutput(OutputId.Lmp_P3_Holster_R),
                new GameOutput(OutputId.Lmp_P3_Holster_G),
                new GameOutput(OutputId.Lmp_P3_Holster_B),
                new GameOutput(OutputId.Lmp_P4_Holster),
                new GameOutput(OutputId.Lmp_P4_Holster_R),
                new GameOutput(OutputId.Lmp_P4_Holster_G),
                new GameOutput(OutputId.Lmp_P4_Holster_B),
                new GameOutput(OutputId.Lmp_P1_Side_R),
                new GameOutput(OutputId.Lmp_P1_Side_G),
                new GameOutput(OutputId.Lmp_P1_Side_B),
                new GameOutput(OutputId.Lmp_P2_Side_R),
                new GameOutput(OutputId.Lmp_P2_Side_G),
                new GameOutput(OutputId.Lmp_P2_Side_B),
                new GameOutput(OutputId.Lmp_P3_Side_R),
                new GameOutput(OutputId.Lmp_P3_Side_G),
                new GameOutput(OutputId.Lmp_P3_Side_B),
                new GameOutput(OutputId.Lmp_P4_Side_R),
                new GameOutput(OutputId.Lmp_P4_Side_G),
                new GameOutput(OutputId.Lmp_P4_Side_B),
                new GameOutput(OutputId.Lmp_P1_GunAmmo),
                new GameOutput(OutputId.Lmp_P2_GunAmmo),
                new GameOutput(OutputId.Lmp_P3_GunAmmo),
                new GameOutput(OutputId.Lmp_P4_GunAmmo),
                new GameOutput(OutputId.Lmp_P1_GunGrenade),
                new GameOutput(OutputId.Lmp_P2_GunGrenade),
                new GameOutput(OutputId.Lmp_P3_GunGrenade),
                new GameOutput(OutputId.Lmp_P4_GunGrenade),

                //In Teknoparrot, Guns hardware is not emulated, so the game is not running original gun recoil procedures 
                /*_Outputs.Add(new GameOutput(OutputId.P1_GunMotor));
                _Outputs.Add(new GameOutput(OutputId.P2_GunMotor));*/
                new GameOutput(OutputId.P1_Ammo),
                new GameOutput(OutputId.P2_Ammo),
                new GameOutput(OutputId.P3_Ammo),
                new GameOutput(OutputId.P4_Ammo),
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
                new AsyncGameOutput(OutputId.P4_Damaged, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_DAMAGED_DELAY), 100, 0)
            };
            //_Outputs.Add(new GameOutput(OutputId.Credits));
        }

        /// <summary>
        /// Update all Outputs values before sending them to MameHooker
        /// </summary>
        public override void UpdateOutputValues()
        {

            //Original Outputs

            //Left POD
            SetOutputValue(OutputId.P1_LmpStart, 0);    //Start Lamp are not animated (!!)
            SetOutputValue(OutputId.P2_LmpStart, 0);
            SetOutputValue(OutputId.Lmp_PodLeft_Panel, (int)(ReadFloat(_Lamp_Address) * 200));

            SetOutputValue(OutputId.Lmp_P1_Holster, (int)(ReadFloat(_Lamp_Address + 0x04) * 200));
            SetOutputValue(OutputId.Lmp_P1_Holster_R, (int)(ReadFloat(_Lamp_Address + 0x08) * 200));
            SetOutputValue(OutputId.Lmp_P1_Holster_G, (int)(ReadFloat(_Lamp_Address + 0x0C) * 200));
            SetOutputValue(OutputId.Lmp_P1_Holster_B, (int)(ReadFloat(_Lamp_Address + 0x10) * 200));
            SetOutputValue(OutputId.Lmp_P2_Holster, (int)(ReadFloat(_Lamp_Address + 0x14) * 200));
            SetOutputValue(OutputId.Lmp_P2_Holster_R, (int)(ReadFloat(_Lamp_Address + 0x18) * 200));
            SetOutputValue(OutputId.Lmp_P2_Holster_G, (int)(ReadFloat(_Lamp_Address + 0x1C) * 200));
            SetOutputValue(OutputId.Lmp_P2_Holster_B, (int)(ReadFloat(_Lamp_Address + 0x20) * 200));

            SetOutputValue(OutputId.Lmp_P1_Side_R, (int)(ReadFloat(_Lamp_Address + 0x24) * 200));
            SetOutputValue(OutputId.Lmp_P1_Side_G, (int)(ReadFloat(_Lamp_Address + 0x28) * 200));
            SetOutputValue(OutputId.Lmp_P1_Side_B, (int)(ReadFloat(_Lamp_Address + 0x2C) * 200));
            SetOutputValue(OutputId.Lmp_P2_Side_R, (int)(ReadFloat(_Lamp_Address + 0x30) * 200));
            SetOutputValue(OutputId.Lmp_P2_Side_G, (int)(ReadFloat(_Lamp_Address + 0x34) * 200));
            SetOutputValue(OutputId.Lmp_P2_Side_B, (int)(ReadFloat(_Lamp_Address + 0x38) * 200));

            SetOutputValue(OutputId.Lmp_P1_GunAmmo, (int)(ReadFloat(_Lamp_Address + 0x50) * 200));
            SetOutputValue(OutputId.Lmp_P2_GunAmmo, (int)(ReadFloat(_Lamp_Address + 0x54) * 200));

            SetOutputValue(OutputId.Lmp_PodLeft_Panel_R, (int)(ReadFloat(_Lamp_Address + 0x44) * 200));
            SetOutputValue(OutputId.Lmp_PodLeft_Panel_G, (int)(ReadFloat(_Lamp_Address + 0x48) * 200));
            SetOutputValue(OutputId.Lmp_PodLeft_Panel_B, (int)(ReadFloat(_Lamp_Address + 0x4C) * 200));
            SetOutputValue(OutputId.Lmp_PodLeft_Panel_Front, (int)(ReadFloat(_Lamp_Address + 0x64) * 200));
            SetOutputValue(OutputId.Lmp_PodLeft_Panel_Under, (int)(ReadFloat(_Lamp_Address + 0x5C) * 200));

            //Right POD
            SetOutputValue(OutputId.P3_LmpStart, 0);    //Start Lamp are not animated (!!)
            SetOutputValue(OutputId.P4_LmpStart, 0);
            SetOutputValue(OutputId.Lmp_PodRight_Panel, (int)(ReadFloat(_Lamp_Address + 0x80) * 200));

            SetOutputValue(OutputId.Lmp_P3_Holster, (int)(ReadFloat(_Lamp_Address + 0x84) * 200));
            SetOutputValue(OutputId.Lmp_P3_Holster_R, (int)(ReadFloat(_Lamp_Address + 0x88) * 200));
            SetOutputValue(OutputId.Lmp_P3_Holster_G, (int)(ReadFloat(_Lamp_Address + 0x8C) * 200));
            SetOutputValue(OutputId.Lmp_P3_Holster_B, (int)(ReadFloat(_Lamp_Address + 0x90) * 200));
            SetOutputValue(OutputId.Lmp_P4_Holster, (int)(ReadFloat(_Lamp_Address + 0x94) * 200));
            SetOutputValue(OutputId.Lmp_P4_Holster_R, (int)(ReadFloat(_Lamp_Address + 0x98) * 200));
            SetOutputValue(OutputId.Lmp_P4_Holster_G, (int)(ReadFloat(_Lamp_Address + 0x9C) * 200));
            SetOutputValue(OutputId.Lmp_P4_Holster_B, (int)(ReadFloat(_Lamp_Address + 0xA0) * 200));

            SetOutputValue(OutputId.Lmp_P3_Side_R, (int)(ReadFloat(_Lamp_Address + 0xA4) * 200));
            SetOutputValue(OutputId.Lmp_P3_Side_G, (int)(ReadFloat(_Lamp_Address + 0xA8) * 200));
            SetOutputValue(OutputId.Lmp_P3_Side_B, (int)(ReadFloat(_Lamp_Address + 0xAC) * 200));
            SetOutputValue(OutputId.Lmp_P4_Side_R, (int)(ReadFloat(_Lamp_Address + 0xB0) * 200));
            SetOutputValue(OutputId.Lmp_P4_Side_G, (int)(ReadFloat(_Lamp_Address + 0xB4) * 200));
            SetOutputValue(OutputId.Lmp_P4_Side_B, (int)(ReadFloat(_Lamp_Address + 0xB8) * 200));

            SetOutputValue(OutputId.Lmp_P3_GunAmmo, (int)(ReadFloat(_Lamp_Address + 0xD0) * 200));
            SetOutputValue(OutputId.Lmp_P4_GunAmmo, (int)(ReadFloat(_Lamp_Address + 0xD4) * 200));

            SetOutputValue(OutputId.Lmp_Marquee_R, (int)(ReadFloat(_Lamp_Address + 0xC4) * 200));
            SetOutputValue(OutputId.Lmp_Marquee_G, (int)(ReadFloat(_Lamp_Address + 0xC8) * 200));
            SetOutputValue(OutputId.Lmp_Marquee_B, (int)(ReadFloat(_Lamp_Address + 0xCC) * 200));

            SetOutputValue(OutputId.Lmp_RearSoldier, (int)(ReadFloat(_Lamp_Address + 0xE4) * 200));
            SetOutputValue(OutputId.Lmp_Marquee_1, (int)(ReadFloat(_Lamp_Address + 0xE0) * 200));
            SetOutputValue(OutputId.Lmp_MasterChief, (int)(ReadFloat(_Lamp_Address + 0xDC) * 200));

            SetOutputValue(OutputId.Lmp_Speaker_R, (int)(ReadFloat(_Lamp_Address + 0xE8) * 200));
            SetOutputValue(OutputId.Lmp_Speaker_B, (int)(ReadFloat(_Lamp_Address + 0xEC) * 200));
            SetOutputValue(OutputId.Lmp_Speaker_G, (int)(ReadFloat(_Lamp_Address + 0xF0) * 200));
            SetOutputValue(OutputId.Lmp_Speaker, (int)(ReadFloat(_Lamp_Address + 0xC0) * 200));
            SetOutputValue(OutputId.Lmp_MonitorStand, (int)(ReadFloat(_Lamp_Address + 0xBC) * 200));

            //Custom Outputs
            for (UInt64 i = 0; i < 4; i++)
            {
                _Life[i] = 0;
                _Ammo[i] = 0;

                //Status :
                //0 = Game Over
                //2 = Continue
                //3 = Playing
                byte PlayerStatus = ReadByte(_P1_Status_Address + (i * 0x250));

                if (PlayerStatus == 3)
                {
                    SetOutputValue((OutputId)((int)OutputId.P1_Ammo + i), ReadInt32(_P1_Ammo_Address + (i * 0x250)));
                    SetOutputValue((OutputId)((int)OutputId.P1_Life + i), ReadInt32(_P1_Life_Address + (i * 0x250)));
                }

                //Reading our own flags for damage and recoil and resetting them
                if (ReadByte(_RecoilStatus_CaveAddress + i) == 1)
                {
                    SetOutputValue((OutputId)((int)OutputId.P1_CtmRecoil + (int)i), 1);
                    WriteByte(_RecoilStatus_CaveAddress + i, 0);
                }

                if (ReadByte(_DamageStatus_CaveAddress + i) == 1)
                {
                    SetOutputValue((OutputId)((int)OutputId.P1_Damaged + (int)i), 1);
                    WriteByte(_DamageStatus_CaveAddress + i, 0);
                }

                SetOutputValue(OutputId.P1_Ammo + (uint)i, _Ammo[i]);
                SetOutputValue(OutputId.P1_Life + (uint)i, _Life[i]);
            }

            //SetOutputValue(OutputId.Credits, ReadByte(_Credits_Address));
        }

        #endregion
    }
}
