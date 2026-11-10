using System;
using System.Collections.Generic;
using System.Linq;
using UsCore.Config;
using UsCore.Launcher;
using UsCore.Logging;
using UsCore.MameOutput;

namespace U_Shoot.Games
{
    public class Game_Pcsx2x6TimeCrisis3 : Game_Pcsx2x6
    {
        protected override string GAMEDATA_FOLDER { get { return @"Pcsx2x6\timecrs3"; } }

        #region LOADER MEMORY ADDRESSES 
        private UInt64 _TST01_Offset = 0x00348D80;
        private UInt64 _TST02_Offset = 0x0034B630;
        private UInt64 _TST03_Offset = 0x0034AB90;
        #endregion

        #region INPUTS MEMORY ADDRESSES    

        #endregion

        #region OUTPUTS MEMORY ADDRESSES
        private UInt64 _Credits_EEM_Offset = 0x00379598;
        private UInt64 _PlayerStatus_EEM_Offset = 0x003795B8;
        private UInt64 _P1_Life_EEM_Offset = 0x0187C2C0;
        private UInt64 _P1_Weapon_EEM_Offset = 0x0187C430;
        private UInt64 _P1_Ammo_EEM_Offset = 0x0187C434;
        #endregion


        private int _P1_LastWeapon = 0;

        public Game_Pcsx2x6TimeCrisis3(GameConfig Config) : base(Config)
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
                    if (ReadString(EEmem_Address + _TST01_Offset, 7).Equals("TST-01A"))
                    {
                        Logger.WriteLog("Time Crisis TST-01 loaded, EE memory at 0x" + EEmem_Address.ToString("X16"));
                        ReadGameDataFromMd5Hash("TST1", GAMEDATA_FOLDER);
                        RaiseGameHookedEvent();
                    }
                    else if (ReadString(EEmem_Address + _TST02_Offset, 7).Equals("TST-02A"))
                    {
                        Logger.WriteLog("Time Crisis TST-02 loaded, EE memory at 0x" + EEmem_Address.ToString("X16"));
                        ReadGameDataFromMd5Hash("TST2", GAMEDATA_FOLDER);
                        RaiseGameHookedEvent();
                    }
                    else if (ReadString(EEmem_Address + _TST03_Offset, 7).Equals("TST-03A"))
                    {
                        Logger.WriteLog("Time Crisis TST-03 loaded, EE memory at 0x" + EEmem_Address.ToString("X16"));
                        ReadGameDataFromMd5Hash("TST3", GAMEDATA_FOLDER);
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

            int Playerstatus = ReadInt32(EEmem_Address + _PlayerStatus_EEM_Offset);
            if (Playerstatus == 1)
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
