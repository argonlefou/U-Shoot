using System;
using System.Collections.Generic;
using UsCore.Config;
using UsCore.MameOutput;

namespace U_Shoot.Games
{
    public class Game_FlycastNinjaslt : Game_Flycast
    {
        private UInt32 _Outputs_Outputs_Offset = 0x0022C42D;
        private UInt32 _Outputs_Credits_Offset = 0x00480D0C;
        private UInt32 _Outputs_PlayerData_Offset = 0x003D9D30;
        private int _P1_LastDammage = 0;
        private int _P2_LastDammage = 0;

        public Game_FlycastNinjaslt(GameConfig Config) : base(Config)
        {
            string RomName = _GameConfig.RomName.ToLower();

            if (RomName.Equals("ninjaslt"))
            {
                _Outputs_Outputs_Offset = 0x0022C42D;
                _Outputs_Credits_Offset = 0x00480D0C;
                //_Outputs_PlayerData_Offset = 0x003D9D30;     Demul
                _Outputs_PlayerData_Offset = 0x003D9CB0;
            }
            else if (RomName.Equals("ninjaslta"))
            {
                _Outputs_Outputs_Offset = 0x0022C4AD;
                _Outputs_Credits_Offset = 0x00480D8C;
                _Outputs_PlayerData_Offset = 0x003D9D30;
            }
            else if (RomName.Equals("ninjasltj"))
            {
                _Outputs_Outputs_Offset = 0x0022C3F1;
                _Outputs_Credits_Offset = 0x00480CD4;
                _Outputs_PlayerData_Offset = 0x003D9C78;
            }
            else if (RomName.Equals("ninjasltu"))
            {
                _Outputs_Outputs_Offset = 0x0022C4AD;
                _Outputs_Credits_Offset = 0x00480D8C;
                _Outputs_PlayerData_Offset = 0x003D9D30;
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
                new GameOutput(OutputId.P1_LmpStart),
                new GameOutput(OutputId.P2_LmpStart),
                new GameOutput(OutputId.P1_GunRecoil),
                new GameOutput(OutputId.P2_GunRecoil),
                new GameOutput(OutputId.P1_Ammo),
                new GameOutput(OutputId.P2_Ammo),
                new AsyncGameOutput(OutputId.P1_CtmRecoil, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_ON_DELAY), ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_OFF_DELAY), 0),
                new AsyncGameOutput(OutputId.P2_CtmRecoil, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_ON_DELAY), ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_OFF_DELAY), 0),
                new GameOutput(OutputId.P1_Life),
                new GameOutput(OutputId.P2_Life),
                new AsyncGameOutput(OutputId.P1_Damaged, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_DAMAGED_DELAY), 100, 0),
                new AsyncGameOutput(OutputId.P2_Damaged, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_DAMAGED_DELAY), 100, 0),
                new GameOutput(OutputId.Credits)
            };
        }

        /// <summary>
        /// Update all Outputs values before sending them to MameHooker
        /// </summary>
        public override void UpdateOutputValues()
        {
            UInt64 Outputs_Address = _GameRAM_Address + _Outputs_Outputs_Offset;

            //Check if the game is in Gameplay mode
            if (ReadByte(_GameRAM_Address + _Outputs_PlayerData_Offset + 0x1A) == 1)
            {
                //Didn't find any reliable "player state", but Life seem to stay at 0 when not playing, so we will use that
                //Note that at start, life may be > 0 if the player has never entered a game :(
                _Life[0] = (int)BitConverter.ToInt16(ReadBytes((IntPtr)(_GameRAM_Address + _Outputs_PlayerData_Offset + 0x54), 2), 0);
                _Life[1] = (int)BitConverter.ToInt16(ReadBytes((IntPtr)(_GameRAM_Address + _Outputs_PlayerData_Offset + 0x56), 2), 0);

                //For custom dammaged : 
                //1) Solution 1 : Decrease life = small delay between the hit and the life beeing lost
                /*//[Damaged] custom Output                
                if (_P1_Life < _P1_LastLife)
                    SetOutputValue(OutputId.P1_Damaged, 1);

                //[Damaged] custom Output                
                if (_P2_Life < _P2_LastLife)
                    SetOutputValue(OutputId.P2_Damaged, 1);*/

                //2) solution 2 : Read a byte value wich is != 0 when hit (invicibility duration ?) but the "1" state duration is long and may trigger the output multiple times                
                int P1_Dammage = ReadByte(_GameRAM_Address + _Outputs_PlayerData_Offset + 0xBC);
                int P2_Dammage = ReadByte(_GameRAM_Address + _Outputs_PlayerData_Offset + 0xBE);
                if (P1_Dammage != 0 & _P1_LastDammage == 0)
                    SetOutputValue(OutputId.P1_Damaged, 1);
                if (P2_Dammage != 0 & _P2_LastDammage == 0)
                    SetOutputValue(OutputId.P2_Damaged, 1);
                _P1_LastDammage = P1_Dammage;
                _P2_LastDammage = P2_Dammage;

                if (_Life[0] > 0)
                {
                    _Ammo[0] = (int)BitConverter.ToInt16(ReadBytes(_GameRAM_Address + _Outputs_PlayerData_Offset + 0x62, 2), 0);

                    //Custom Recoil
                    if (_Ammo[0] < _LastAmmo[0])
                        SetOutputValue(OutputId.P1_CtmRecoil, 1);
                }

                if (_Life[1] > 0)
                {
                    _Ammo[1] = (int)BitConverter.ToInt16(ReadBytes(_GameRAM_Address + _Outputs_PlayerData_Offset + 0x64, 2), 0);

                    //Custom Recoil
                    if (_Ammo[1] < _LastAmmo[1])
                        SetOutputValue(OutputId.P2_CtmRecoil, 1);
                }
            }

            for (int i = 0; i < _GameConfig.PlayerCount; i++)
            {
                _LastAmmo[i] = _Ammo[i];
                _LastLife[i] = _Life[i];
            }

            SetOutputValue(OutputId.P1_Ammo, _Ammo[0]);
            SetOutputValue(OutputId.P2_Ammo, _Ammo[1]);
            SetOutputValue(OutputId.P1_Life, _Life[0]);
            SetOutputValue(OutputId.P2_Life, _Life[1]);

            //Genuine Outputs
            SetOutputValue(OutputId.P1_LmpStart, ReadByte((IntPtr)Outputs_Address) >> 6 & 0x01);
            SetOutputValue(OutputId.P2_LmpStart, ReadByte((IntPtr)Outputs_Address) >> 4 & 0x01);
            SetOutputValue(OutputId.P1_GunRecoil, ReadByte((IntPtr)Outputs_Address) >> 7 & 0x01);
            SetOutputValue(OutputId.P2_GunRecoil, ReadByte((IntPtr)Outputs_Address) >> 5 & 0x01);

            //Custom recoil will be activated just like original one
            //REMOVED !! The recoil is activated when shooting offscreen !!
            /*SetOutputValue(OutputId.P1_CtmRecoil, ReadByte((IntPtr)Outputs_Address) >> 7 & 0x01);
            SetOutputValue(OutputId.P2_CtmRecoil, ReadByte((IntPtr)Outputs_Address) >> 5 & 0x01);*/

            //Credits
            SetOutputValue(OutputId.Credits, ReadByte(_GameRAM_Address + _Outputs_Credits_Offset));
        }

        #endregion
    }
}
