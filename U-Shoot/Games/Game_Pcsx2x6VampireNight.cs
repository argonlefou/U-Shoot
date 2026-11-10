using System;
using System.Collections.Generic;
using UsCore.Config;
using UsCore.Logging;
using UsCore.MameOutput;

namespace U_Shoot.Games
{
    public class Game_Pcsx2x6VampireNight : Game_Pcsx2x6
    {
        #region LOADER MEMORY ADDRESSES 
        private UInt64 _RomLoaded_Check_Offset = 0x00217C80;
        #endregion

        #region INPUTS MEMORY ADDRESSES    

        #endregion

        #region OUTPUTS MEMORY ADDRESSES
        private UInt64 _Credits_EEM_Offset = 0x003CBB98;
        private UInt64 _P1_PlayerStatus_EEM_Offset = 0x003CBBA4;
        private UInt64 _P1_Life_EEM_Offset = 0x003CBBB0;
        private UInt64 _P1_Ammo_EEM_Offset = 0x003CBBC4;
        #endregion

        public Game_Pcsx2x6VampireNight(GameConfig Config) : base(Config)
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
                    if (ReadString(EEmem_Address + _RomLoaded_Check_Offset, 6).Equals("VPN3-B"))
                    {
                        Logger.WriteLog("Vampire Night loaded, EE memory at 0x" + EEmem_Address.ToString("X16"));
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
                new GameOutput(OutputId.P2_Ammo),
                new AsyncGameOutput(OutputId.P1_CtmRecoil, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_ON_DELAY), ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_OFF_DELAY), 0),
                new AsyncGameOutput(OutputId.P2_CtmRecoil, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_ON_DELAY), ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_OFF_DELAY), 0),
                new GameOutput(OutputId.P1_Life),
                new GameOutput(OutputId.P2_Life),
                new AsyncGameOutput(OutputId.P1_Damaged, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_DAMAGED_DELAY), 100, 0),
                new AsyncGameOutput(OutputId.P2_Damaged, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_DAMAGED_DELAY), 100, 0),
                new GameOutput(OutputId.Credits),
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
                _Life[i] = 0;
            }

            UInt64 EEmem_Address = ReadPtr(_EEmem_Ptr_Address);
            if (EEmem_Address == 0)
                return;

            for (uint i = 0; i < _GameConfig.PlayerCount; i++)
            {
                //Player Status:
                //4= Continue Screen
                //5= Playing
                //6= Game Over Screen
                //9= Title Screen
                int Playerstatus = ReadInt32(EEmem_Address + _P1_PlayerStatus_EEM_Offset + (i * 0x2B4));
                if (Playerstatus == 5)
                {
                    _Life[i] = ReadInt32(EEmem_Address + _P1_Life_EEM_Offset + (i * 0x2B4));
                    _Ammo[i] = ReadInt32(EEmem_Address + _P1_Ammo_EEM_Offset + (i * 0x2B4));

                    //[Recoil] custom Output : a few bullets at most between two reads (machine gun)
                    if (_Ammo[i] < _LastAmmo[i] && _LastAmmo[i] - _Ammo[i] <= 3)
                        SetOutputValue(OutputId.P1_CtmRecoil + i, 1);

                    //[Damaged] custom Output
                    if (_Life[i] == _LastLife[i] - 1)
                        SetOutputValue(OutputId.P1_Damaged + i, 1);
                }

                _LastAmmo[i] = _Ammo[i];
                _LastLife[i] = _Life[i];

                SetOutputValue(OutputId.P1_Ammo + i, _Ammo[i]);
                SetOutputValue(OutputId.P1_Life + i, _Life[i]);
            }

            SetOutputValue(OutputId.Credits, ReadInt32(EEmem_Address + _Credits_EEM_Offset));
        }

        #endregion
    }
}
