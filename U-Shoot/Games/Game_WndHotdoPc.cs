using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using UsCore.Config;
using UsCore.Launcher;
using UsCore.Logging;
using UsCore.MameOutput;
using UsCore.Memory;
using UsCore.VirtualControllers;

namespace U_Shoot.Games
{
    public class Game_WndHotdoPc : BaseGame32
    {
        protected override string GAMEDATA_FOLDER { get { return @"Windows\hodo"; } }
        protected override bool PATCH_PEB_DEBUG_FLAG { get { return false; } }

        #region OUTPUTS MEMORY ADDRESSES
        private UInt32 _PlayerStatus_Offset = 0x0059C378;
        private UInt32 _CreditsPtr_Offset = 0x005996C8;
        private UInt32 _PlayerInfoPtr_Offset = 0x00599688;
        private InjectionStruct32 _Recoil_InjectionStruct = new InjectionStruct32(0x00118C5F, 6);
        private InjectionStruct32 _Damage_InjectionStruct = new InjectionStruct32(0x0018B399, 6);

        //Custom data
        private UInt32 _Recoil_CaveAddress;
        private UInt32 _Damage_CaveAddress;
        #endregion

        public Game_WndHotdoPc(GameConfig Config) : base(Config)
        {
            _KnownMd5Prints.Add("Typing Of The Dead SEGA Windows", "da39156a426e3f3faca25d3c8cb2b401");
            _KnownMd5Prints.Add("Typing Of The Dead SEGA Windows STEAM", "9dcb7083e3e3ede186c9a809498a0d3b");
            _KnownMd5Prints.Add("Typing of The Dead STEAM", "ac70ca4b6d310fe1d5f18965575ece68");
        }

        #region Debugger Events

        #region CREATE_PROCESS_DEBUG_EVENT

        protected override void CreateProcessEvent(object sender, x86_x64_Debugger.CreateProcessDebugEventArgs e)
        {
            base.CreateProcessEvent(sender, e);

            //The game has a debugger detection system and will refuse to run if the Debugger is still attached passed that point
            DetachDebuggerAndReopenProcess();
            Apply_MemoryHacks();
        }

        #endregion        

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

        #region Outputs Hack

        protected override void Apply_OutputsMemoryHack()
        {
            Create_OutputsDataBank();
            _Recoil_CaveAddress = OutputsDatabank_Address;
            _Damage_CaveAddress = OutputsDatabank_Address + 0x04;

            SetHack_Recoil();
            SetHack_Damage();

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        /// <summary>
        /// Using Ammo count to compute recoil does not work :
        /// - Game set ammo to zero before reloading (and trigger recoil)
        /// - Gaitling gun has no ammo count -> no recoil
        /// Using this codecave, we can intercept call in function when the bullet is fired
        /// </summary>
        private void SetHack_Recoil()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //cmp [esi+000007A0],bl
            CaveMemory.Write_StrBytes("38 9E A0 07 00 00");
            //mov byte ptr [_Recoil_CaveAddress],01
            CaveMemory.Write_StrBytes("C6 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Recoil_CaveAddress));
            CaveMemory.Write_StrBytes("01");

            //Inject it
            CaveMemory.InjectToOffset(_Recoil_InjectionStruct, "Recoil");
        }

        /// <summary>
        /// To prevent false positive damage flag based on life number (set to 0 at the end of a level)
        /// Intercepting call in function where player health is decreased when hit
        /// </summary>
        private void SetHack_Damage()
        {
            Codecave32 CaveMemory = new Codecave32(_Process, GameProcess_ModuleBaseAddress);

            //mov [esi+0000020C],edi
            CaveMemory.Write_StrBytes("89 BE 0C 02 00 00");
            //mov byte ptr [_Damage_CaveAddress],01
            CaveMemory.Write_StrBytes("C6 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Damage_CaveAddress));
            CaveMemory.Write_StrBytes("01");

            //Inject it
            CaveMemory.InjectToOffset(_Damage_InjectionStruct, "Damage");
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
                UInt32 Credits_Address = ReadPtrChain(GameProcess_ModuleBaseAddress + _CreditsPtr_Offset, new UInt32[] { 0x228 }) + 0x17C;// ReadByte(_Credits_Address);
                byte Credits = ReadByte(Credits_Address);
                Credits++;
                WriteByte(Credits_Address, Credits);
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
            _Outputs = new List<GameOutput>
            {
                new GameOutput(OutputId.P1_Ammo),
                new AsyncGameOutput(OutputId.P1_CtmRecoil, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_ON_DELAY), ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_OFF_DELAY), 0),
                new GameOutput(OutputId.P1_Life),
                new AsyncGameOutput(OutputId.P1_Damaged, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_DAMAGED_DELAY), 100, 0),
                new GameOutput(OutputId.Credits)
            };
        }

        /// <summary>
        /// Update all Outputs values before sending them to MameHooker
        /// </summary>
        public override void UpdateOutputValues()
        {
            //Player status :
            //[0] = Not Playing
            //[1] = Playing
            UInt32 P1_Status = ReadByte(GameProcess_ModuleBaseAddress + _PlayerStatus_Offset);

            UInt32 iTemp = ReadPtr(GameProcess_ModuleBaseAddress + _PlayerInfoPtr_Offset);
            UInt32 PlayerInfo = 0;
            //Player nfo struct is created and allocated on game, point to NULL in menus
            if (iTemp != 0)
            {
                PlayerInfo = ReadPtr(iTemp + 0x24);
                _Life[0] = ReadByte(PlayerInfo + 0x20C);
                _Ammo[0] = ReadByte(PlayerInfo + 0x28C);
            }

            if (P1_Status != 0)
            {
                //Custom Recoil
                if (ReadByte(_Recoil_CaveAddress) == 1)
                {
                    SetOutputValue(OutputId.P1_CtmRecoil, 1);
                    WriteByte(_Recoil_CaveAddress, 0);
                }

                //[Damaged] custom Output                
                if (ReadByte(_Damage_CaveAddress) == 1)
                {
                    SetOutputValue(OutputId.P1_Damaged, 1);
                    WriteByte(_Damage_CaveAddress, 0);
                }
            }

            SetOutputValue(OutputId.P1_Ammo, _Ammo[0]);
            SetOutputValue(OutputId.P1_Life, _Life[0]);
            SetOutputValue(OutputId.Credits, ReadByte(ReadPtrChain(GameProcess_ModuleBaseAddress + _CreditsPtr_Offset, new UInt32[] { 0x228 }) + 0x17C));
        }

        #endregion
    }
}
