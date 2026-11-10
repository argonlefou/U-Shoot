using System;
using System.Collections.Generic;
using UsCore.Config;
using UsCore.Logging;
using UsCore.MameOutput;

namespace U_Shoot.Games
{
    public class Game_Pcsx2x6TimeCrisis4 : Game_Pcsx2x6
    {
        #region LOADER MEMORY ADDRESSES 

        #endregion

        #region INPUTS MEMORY ADDRESSES    

        #endregion

        #region OUTPUTS MEMORY ADDRESSES
        private UInt64 _Credits_EEM_Offset = 0x00C1E13C;
        //private UInt64 _PlayerStatus_EEM_Offset = 0x00C1EF58; //Change between 3 and 9 after a continue
        private UInt64 _PlayerStatus_EEM_Offset = 0x005ACB58;
        private UInt64 _P1_Life_EEM_Offset = 0x00A6D854;
        private UInt64 _P1_Weapon_EEM_Offset = 0x00A6D9E4;
        private UInt64 _P1_Ammo_EEM_Offset = 0x00A6D9C4;
        private UInt64 _Lamps_EEM_Offset = 0x00C1E160;
        #endregion

        private int _P1_LastWeapon = 0;

        public Game_Pcsx2x6TimeCrisis4(GameConfig Config) : base(Config)
        { }

        protected override void ProcessTimerTick_Unhooked_CheckProc()
        {
            try
            {
                if (_EEmem_Ptr_Address == 0)
                {
                    _EEmem_Ptr_Address = FindExport("EEmem");
                    if (_EEmem_Ptr_Address == 0)
                    {
                        Logger.WriteLog("EEmem export not found in " + _GameConfig.TargetFileName);
                        return;
                    }
                    Logger.WriteLog("EEmem pointer at 0x" + _EEmem_Ptr_Address.ToString("X16"));
                }

                UInt64 EEmem_Address = ReadPtr(_EEmem_Ptr_Address);
                if (EEmem_Address != 0)
                {
                    if (ReadString(EEmem_Address + 0x005ABEC0, 6).Equals("TSF100"))
                    {
                        Logger.WriteLog("Time Crisis 4 loaded, EE memory at 0x" + EEmem_Address.ToString("X16"));
                        RaiseGameHookedEvent();
                    }
                    else
                    {
                        Logger.WriteLog("ROM not Loaded, waiting...");
                    }
                }
                else
                {
                    Logger.WriteLog("ROM not Loaded, waiting...");
                }
            }
            catch (Exception Ex)
            {
                Logger.WriteLog("Error waiting for the game to be loaded : " + Ex.Message);
            }
        }

        #region Outputs

        /// <summary>
        /// Create the Output list that we will be looking for and forward to MameHooker
        /// </summary>
        protected override void CreateOutputList()
        {
            _Outputs = new List<GameOutput>
            {
                new GameOutput(OutputId.Lmp_MainSpeaker),
                new GameOutput(OutputId.Lmp_GunTower),
                new GameOutput(OutputId.Lmp_VoiceSpeaker),                
                new GameOutput(OutputId.P1_Ammo),
                new AsyncGameOutput(OutputId.P1_CtmRecoil, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_ON_DELAY), ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_OFF_DELAY), 0),
                new GameOutput(OutputId.P1_Life),
                new AsyncGameOutput(OutputId.P1_Damaged, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_DAMAGED_DELAY), 100, 0),
                new GameOutput(OutputId.Credits),
            };
        }

        /// <summary>
        /// Update all Outputs values before sending them to MameHooker
        /// </summary>
        public override void UpdateOutputValues()
        {
            _Ammo[0] = 0;
            _Life[0] = 0;
            int P1_Weapon = 0;

            UInt64 EEmem_Address = ReadPtr(_EEmem_Ptr_Address);
            if (EEmem_Address == 0)
                return;

            SetOutputValue(OutputId.Lmp_MainSpeaker, (ReadByte(EEmem_Address + _Lamps_EEM_Offset) & 0x0F) > 0 ? 1 : 0);
            SetOutputValue(OutputId.Lmp_GunTower, (ReadByte(EEmem_Address + _Lamps_EEM_Offset + 1) & 0x0F) > 0 ? 1 : 0);
            SetOutputValue(OutputId.Lmp_VoiceSpeaker, (ReadByte(EEmem_Address + _Lamps_EEM_Offset) & 0xF0) > 0 ? 1 : 0);

            //Player Status:
            //0= Title Screen
            //1= Attract mode
            //3= Playing
            //11= Continue Screen
            int Playerstatus = ReadInt32(EEmem_Address + _PlayerStatus_EEM_Offset);
            if (Playerstatus == 3)
            {
                _Life[0] = ReadInt32(EEmem_Address + _P1_Life_EEM_Offset);
                P1_Weapon = ReadInt32(EEmem_Address + _P1_Weapon_EEM_Offset);

                if (P1_Weapon >= 0 && P1_Weapon < 4)
                    _Ammo[0] = ReadInt32(EEmem_Address + _P1_Ammo_EEM_Offset + 4 * (UInt64)P1_Weapon);

                //[Recoil] custom Output : a few bullets at most between two reads (machine gun)
                if (P1_Weapon == _P1_LastWeapon && _Ammo[0] < _LastAmmo[0] && _LastAmmo[0] - _Ammo[0] <= 3)
                    SetOutputValue(OutputId.P1_CtmRecoil, 1);

                //[Damaged] custom Output
                if (_Life[0] == _LastLife[0] - 1)
                    SetOutputValue(OutputId.P1_Damaged, 1);
            }

            _LastAmmo[0] = _Ammo[0];
            _LastLife[0] = _Life[0];
            _P1_LastWeapon = P1_Weapon;

            SetOutputValue(OutputId.P1_Ammo, _Ammo[0]);
            SetOutputValue(OutputId.P1_Life, _Life[0]);
            SetOutputValue(OutputId.Credits, ReadInt32(EEmem_Address + _Credits_EEM_Offset));
        }

        #endregion
    }
}