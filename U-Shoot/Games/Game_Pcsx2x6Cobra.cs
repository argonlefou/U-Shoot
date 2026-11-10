using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UsCore.Config;
using UsCore.Logging;
using UsCore.MameOutput;

namespace U_Shoot.Games
{
    public class Game_Pcsx2x6Cobra : Game_Pcsx2x6
    {
        //CBR2, Ver.B used by default
        //Couldn't manage to boot CBR1, Ver.B

        #region LOADER MEMORY ADDRESSES 
        private UInt64 _RomLoaded_Check_Offset = 0x0058CA79;
        #endregion

        #region INPUTS MEMORY ADDRESSES    

        #endregion

        #region OUTPUTS MEMORY ADDRESSES
        private UInt64 _Credits_EEM_Offset = 0x00CE8DD4;
        private UInt64 _PlayerStatus_EEM_Offset = 0x0059AEB8;
        private UInt64 _P1_Life_EEM_Offset = 0x0059BB60;
        private UInt64 _P1_Ammo_EEM_Offset = 0x0059AE98;
        #endregion

        public Game_Pcsx2x6Cobra(GameConfig Config) : base(Config)
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
                    if (ReadString(EEmem_Address + _RomLoaded_Check_Offset, 3).Equals("CBR"))
                    {
                        Logger.WriteLog("Cobra loaded, EE memory at 0x" + EEmem_Address.ToString("X16"));
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

            UInt64 EEmem_Address = ReadPtr(_EEmem_Ptr_Address);
            if (EEmem_Address == 0)
                return;

            //Player Status:
            //0= Not Playing (Title, game over...)
            //3= Playing
            //11= Continue Screen
            int Playerstatus = ReadInt32(EEmem_Address + _PlayerStatus_EEM_Offset);
            if (Playerstatus == 3)
            {
                _Life[0] = ReadInt32(EEmem_Address + _P1_Life_EEM_Offset);
                _Ammo[0] = ReadInt32(EEmem_Address + _P1_Ammo_EEM_Offset);

                //[Recoil] custom Output : a few bullets at most between two reads (machine gun)
                if (_Ammo[0] < _LastAmmo[0] && _LastAmmo[0] - _Ammo[0] <= 3)
                    SetOutputValue(OutputId.P1_CtmRecoil, 1);

                //[Damaged] custom Output
                if (_Life[0] == _LastLife[0] - 1)
                    SetOutputValue(OutputId.P1_Damaged, 1);
            }

            _LastAmmo[0] = _Ammo[0];
            _LastLife[0] = _Life[0];

            SetOutputValue(OutputId.P1_Ammo, _Ammo[0]);
            SetOutputValue(OutputId.P1_Life, _Life[0]);
            SetOutputValue(OutputId.Credits, ReadInt32(EEmem_Address + _Credits_EEM_Offset));
        }

        #endregion
    }
}
