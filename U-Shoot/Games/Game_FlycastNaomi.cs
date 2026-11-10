using System;
using System.Collections.Generic;
using UsCore.Config;
using UsCore.MameOutput;

namespace U_Shoot.Games
{
    public class Game_FlycastNaomi : Game_Flycast
    {
        public Game_FlycastNaomi(GameConfig Config) : base(Config)
        { }

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
            string RomName = _GameConfig.RomName.ToLower();

            if (RomName.Equals("confmiss"))
                Compute_Confmiss_Outputs();
            else if (RomName.Equals("deathcox"))
                Compute_Deathcox_Outputs();
            else if (RomName.Equals("deathcoxo"))
                Compute_Deathcoxo_Outputs();
            else if (RomName.Equals("hotd2"))
                Compute_Hotd2_Outputs(0x00096FA0);
            else if (RomName.Equals("hotd2e"))   
                Compute_Hotd2_Outputs_v2(0x003D0420);
            else if (RomName.Equals("hotd2o"))
                Compute_Hotd2_Outputs(0x00096F58);
            else if (RomName.Equals("hotd2p"))
                Compute_Hotd2_Outputs(0x00082D00);
            else if (RomName.Equals("lupinsho"))
                Compute_Lupinsho_Outputs();
            else if (RomName.Equals("mok"))
                Compute_Mok_Outputs();
        }

        private void Compute_Confmiss_Outputs()
        {
            //Player status :
            //[0] = Calibration/InGame
            //[1] = InGame
            //[2] = Continue
            //[4] = Game Over / Attract Mode / Menu
            UInt64 P1_Status_Address = _GameRAM_Address + (ReadUInt32(_GameRAM_Address + 0x2FBAC) & 0x01FFFFFF);
            UInt64 P2_Status_Address = P1_Status_Address + 0x40;
            for (int i = 0; i < _GameConfig.PlayerCount; i++)
            {
                _Life[i] = 0;
                _Ammo[i] = 0;
            }

            UInt64 P1_Ammo_Address = _GameRAM_Address + (ReadUInt32(_GameRAM_Address + 0x2AA50) & 0x01FFFFFF) - 0x14;  // = P1_Status_Address + 0xB4C ?
            UInt64 P2_Ammo_Address = _GameRAM_Address + (ReadUInt32(_GameRAM_Address + 0x2AA50) & 0x01FFFFFF) + 0x100;
            UInt64 Credits_Address = _GameRAM_Address + (ReadUInt32(_GameRAM_Address + 0x2F88C) & 0x01FFFFFF);

            if (ReadByte(P1_Status_Address) == 0 || ReadByte(P1_Status_Address) == 1)
            {
                _Life[0] = ReadByte(P1_Status_Address + 0x14);
                _Ammo[0] = ReadByte(P1_Ammo_Address);

                //Custom Recoil
                if (_Ammo[0] < _LastAmmo[0])
                    SetOutputValue(OutputId.P1_CtmRecoil, 1);

                //[Damaged] custom Output                
                if (_Life[0] < _LastLife[0])
                    SetOutputValue(OutputId.P1_Damaged, 1);
            }

            if (ReadByte(P2_Status_Address) == 0 || ReadByte(P2_Status_Address) == 1)
            {
                _Life[1] = ReadByte(P2_Status_Address + 0x14);
                _Ammo[1] = ReadByte(P2_Ammo_Address);

                //Custom Recoil
                if (_Ammo[1] < _LastAmmo[1])
                    SetOutputValue(OutputId.P2_CtmRecoil, 1);

                //[Damaged] custom Output                
                if (_Life[1] < _LastLife[1])
                    SetOutputValue(OutputId.P2_Damaged, 1);
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
            SetOutputValue(OutputId.P1_LmpStart, ReadByte(_GameRAM_Address + 0x000152AEA) >> 7 & 0x01);
            SetOutputValue(OutputId.P2_LmpStart, ReadByte(_GameRAM_Address + 0x000152AEA) >> 4 & 0x01);

            SetOutputValue(OutputId.Credits, ReadByte(Credits_Address));
        }

        private void Compute_Deathcox_Outputs()
        {
            //InGame Status : 0 = AttractMode/Demo/Menu, 1 = InGame
            UInt64 InGame_Address = _GameRAM_Address + 0x000982B8;
            for (int i = 0; i < _GameConfig.PlayerCount; i++)
            {
                _Life[i] = 0;
                _Ammo[i] = 0;
            }

            UInt64 P1_Ammo_Address = _GameRAM_Address + 0x0018E395;
            UInt64 P2_Ammo_Address = P1_Ammo_Address + 0x3C;
            UInt64 Credits_Address = _GameRAM_Address + 0x009917C;
            //P1 and P2 Enable : Display ammo and life when it's 0 ( not reliable but well...)
            //UInt64 P1_Enable_Address = _GameRAM_Address + 0x00096634;
            //UInt64 P2_Enable_Address = _GameRAM_Address + 0x00096638;

            //---> Ammo + 0xC => Byte going from 0 to 1 when alive...use for status ???
            UInt64 P1_Enable_Address = P1_Ammo_Address + 0xC;
            UInt64 P2_Enable_Address = P2_Ammo_Address + 0xC;

            if (ReadByte(P1_Enable_Address) == 1 && ReadByte(InGame_Address) == 1)
            {
                _Life[0] = (int)(ReadFloat(Credits_Address + 0x04) * 100);
                _Ammo[0] = ReadByte(P1_Ammo_Address);

                //Custom Recoil
                if (_Ammo[0] < _LastAmmo[0])
                    SetOutputValue(OutputId.P1_CtmRecoil, 1);

                //[Damaged] custom Output                
                if (_Life[0] < _LastLife[0])
                    SetOutputValue(OutputId.P1_Damaged, 1);
            }

            if (ReadByte(P2_Enable_Address) == 1 && ReadByte(InGame_Address) == 1)
            {
                _Life[1] = (int)(ReadFloat(Credits_Address + 0x08) * 100);
                _Ammo[1] = ReadByte(P2_Ammo_Address);

                //Custom Recoil
                if (_Ammo[1] < _LastAmmo[1])
                    SetOutputValue(OutputId.P2_CtmRecoil, 1);

                //[Damaged] custom Output                
                if (_Life[1] < _LastLife[1])
                    SetOutputValue(OutputId.P2_Damaged, 1);
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
            //Genuine Outputs memory location still need to be find on this rom
            //SetOutputValue(OutputId.P1_LmpStart, ReadByte(_GameRAM_Address + 0x0001C8D16) >> 7 & 0x01);
            //SetOutputValue(OutputId.P2_LmpStart, ReadByte(_GameRAM_Address + 0x0001C8D16) >> 4 & 0x01);
            //In the meantime, there is some values that can be used, but does not blink Lamps on the attract/menu screen
            SetOutputValue(OutputId.P1_LmpStart, ReadByte(_GameRAM_Address + 0x000098258));
            SetOutputValue(OutputId.P2_LmpStart, ReadByte(_GameRAM_Address + 0x00009825C));

            SetOutputValue(OutputId.Credits, ReadByte(Credits_Address));
        }

        private void Compute_Deathcoxo_Outputs()
        {
            //InGame Status : 0 = AttractMode/Demo/Menu, 1 = InGame
            UInt64 InGame_Address = _GameRAM_Address + 0x00096680;
            for (int i = 0; i < _GameConfig.PlayerCount; i++)
            {
                _Life[i] = 0;
                _Ammo[i] = 0;
            }

            UInt64 P1_Ammo_Address = _GameRAM_Address + 0x0018BFC9;
            UInt64 P2_Ammo_Address = P1_Ammo_Address + 0x3C;
            UInt64 Credits_Address = _GameRAM_Address + 0x00974A8;
            //P1 and P2 Enable : Display ammo and life when it's 0 ( not reliable but well...)
            //UInt64 P1_Enable_Address = _GameRAM_Address + 0x00096634;
            //UInt64 P2_Enable_Address = _GameRAM_Address + 0x00096638;

            //---> Ammo + 0xC => Byte going from 0 to 1 when alive...use for status ???
            UInt64 P1_Enable_Address = P1_Ammo_Address + 0xC;
            UInt64 P2_Enable_Address = P2_Ammo_Address + 0xC;

            if (ReadByte(P1_Enable_Address) == 1 && ReadByte(InGame_Address) == 1)
            {
                _Life[0] = (int)(ReadFloat(Credits_Address + 0x04) * 100);
                _Ammo[0] = ReadByte(P1_Ammo_Address);

                //Custom Recoil
                if (_Ammo[0] < _LastAmmo[0])

                    //[Damaged] custom Output                
                    if (_Life[0] < _LastLife[0])
                        SetOutputValue(OutputId.P1_Damaged, 1);
            }

            if (ReadByte(P2_Enable_Address) == 1 && ReadByte(InGame_Address) == 1)
            {
                _Life[1] = (int)(ReadFloat(Credits_Address + 0x08) * 100);
                _Ammo[1] = ReadByte(P2_Ammo_Address);

                //Custom Recoil
                if (_Ammo[1] < _LastAmmo[1])
                    SetOutputValue(OutputId.P2_CtmRecoil, 1);

                //[Damaged] custom Output                
                if (_Life[1] < _LastLife[1])
                    SetOutputValue(OutputId.P2_Damaged, 1);
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
            SetOutputValue(OutputId.P1_LmpStart, ReadByte(_GameRAM_Address + 0x0001C8D16) >> 7 & 0x01);
            SetOutputValue(OutputId.P2_LmpStart, ReadByte(_GameRAM_Address + 0x0001C8D16) >> 4 & 0x01);
            SetOutputValue(OutputId.Credits, ReadByte(Credits_Address));
        }

        private void Compute_Hotd2_Outputs(UInt32 DataPtr)
        {
            //Player status :
            //[4] = Continue Screen
            //[5] = InGame
            //[6] = Game Over
            //[9] = Menu or Attract Mode             
            UInt64 P1_Status_Address = _GameRAM_Address + (ReadUInt32(_GameRAM_Address + DataPtr) & 0x01FFFFFF) + 0x04;
            UInt64 P2_Status_Address = P1_Status_Address + 0x100;
            for (int i = 0; i < _GameConfig.PlayerCount; i++)
            {
                _Life[i] = 0;
                _Ammo[i] = 0;
            }

            if (ReadByte(P1_Status_Address) == 5)
            {
                _Life[0] = ReadByte(P1_Status_Address + 0x0C);
                _Ammo[0] = ReadByte(P1_Status_Address + 0x20);

                //Custom Recoil
                if (_Ammo[0] < _LastAmmo[0])
                    SetOutputValue(OutputId.P1_CtmRecoil, 1);

                //[Damaged] custom Output                
                if (_Life[0] < _LastLife[0])
                    SetOutputValue(OutputId.P1_Damaged, 1);
            }

            if (ReadByte(P2_Status_Address) == 5)
            {
                _Life[1] = ReadByte(P2_Status_Address + 0x0C);
                _Ammo[1] = ReadByte(P2_Status_Address + 0x20);

                //Custom Recoil
                if (_Ammo[1] < _LastAmmo[0])
                    SetOutputValue(OutputId.P2_CtmRecoil, 1);

                //[Damaged] custom Output                
                if (_Life[1] < _LastLife[1])
                    SetOutputValue(OutputId.P2_Damaged, 1);
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
            SetOutputValue(OutputId.P1_LmpStart, ReadByte(P1_Status_Address + 0xFB) >> 6 & 0x01);
            SetOutputValue(OutputId.P2_LmpStart, ReadByte(P2_Status_Address + 0xFB) >> 6 & 0x01);
            SetOutputValue(OutputId.Credits, ReadByte(P1_Status_Address + 0x75C));
        }

        /// <summary>
        /// Using Flycast to find game memory values gives different results than the old method I used with Flycast
        /// Data Address is fixed offset, no need to search for a Pointer
        /// </summary>
        private void Compute_Hotd2_Outputs_v2(UInt32 DataOffset)
        {
            //Player status :
            //[4] = Continue Screen
            //[5] = InGame
            //[6] = Game Over
            //[9] = Menu or Attract Mode             
            UInt64 P1_Status_Address = _GameRAM_Address + DataOffset + 0x04;
            UInt64 P2_Status_Address = P1_Status_Address + 0x100;
            for (int i = 0; i < _GameConfig.PlayerCount; i++)
            {
                _Life[i] = 0;
                _Ammo[i] = 0;
            }

            if (ReadByte(P1_Status_Address) == 5)
            {
                _Life[0] = ReadByte(P1_Status_Address + 0x0C);
                _Ammo[0] = ReadByte(P1_Status_Address + 0x20);

                //Custom Recoil
                if (_Ammo[0] < _LastAmmo[0])
                    SetOutputValue(OutputId.P1_CtmRecoil, 1);

                //[Damaged] custom Output                
                if (_Life[0] < _LastLife[0])
                    SetOutputValue(OutputId.P1_Damaged, 1);
            }

            if (ReadByte(P2_Status_Address) == 5)
            {
                _Life[1] = ReadByte(P2_Status_Address + 0x0C);
                _Ammo[1] = ReadByte(P2_Status_Address + 0x20);

                //Custom Recoil
                if (_Ammo[1] < _LastAmmo[0])
                    SetOutputValue(OutputId.P2_CtmRecoil, 1);

                //[Damaged] custom Output                
                if (_Life[1] < _LastLife[1])
                    SetOutputValue(OutputId.P2_Damaged, 1);
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
            SetOutputValue(OutputId.P1_LmpStart, ReadByte(P1_Status_Address + 0xFB) >> 6 & 0x01);
            SetOutputValue(OutputId.P2_LmpStart, ReadByte(P2_Status_Address + 0xFB) >> 6 & 0x01);
            SetOutputValue(OutputId.Credits, ReadByte(P1_Status_Address + 0x75C));
        }

        private void Compute_Lupinsho_Outputs()
        {
            for (int i = 0; i < _GameConfig.PlayerCount; i++)
            {
                _Life[i] = 0;
                _Ammo[i] = 0;
            }

            //Game Status (float) :
            //0 : Title Screen
            //1 : Gameplay
            //2 : Demo play
            float GameStatus = ReadFloat(_GameRAM_Address + 0x00A4C780);

            if (GameStatus == 1.0f)
            {
                //Check if P1 and P2 are active to display their information
                float P1_Active = ReadFloat(_GameRAM_Address + 0x00A4C760);
                float P2_Active = ReadFloat(_GameRAM_Address + 0x00A4C764);

                if (P1_Active == 1.0f)
                {
                    _Life[0] = (int)ReadFloat(_GameRAM_Address + 0x00A4C8B0);
                    _Ammo[0] = (int)ReadFloat(_GameRAM_Address + 0x00A4C830);

                    //Custom Recoil
                    if (_Ammo[0] < _LastAmmo[0])
                        SetOutputValue(OutputId.P1_CtmRecoil, 1);

                    //[Damaged] custom Output                
                    if (_Life[0] < _LastLife[0])
                        SetOutputValue(OutputId.P1_Damaged, 1);
                }

                if (P2_Active == 1.0f)
                {
                    _Life[1] = (int)ReadFloat(_GameRAM_Address + 0x00A4C8B4);
                    _Ammo[1] = (int)ReadFloat(_GameRAM_Address + 0x00A4C834);

                    //Custom Recoil
                    if (_Ammo[1] < _LastAmmo[1])
                        SetOutputValue(OutputId.P2_CtmRecoil, 1);

                    //[Damaged] custom Output                
                    if (_Life[1] < _LastLife[1])
                        SetOutputValue(OutputId.P2_Damaged, 1);
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
            SetOutputValue(OutputId.P1_LmpStart, ReadByte(_GameRAM_Address + 0x00B77C16) >> 7 & 0x01);
            SetOutputValue(OutputId.P2_LmpStart, ReadByte(_GameRAM_Address + 0x00B77C16) >> 4 & 0x01);
            SetOutputValue(OutputId.Credits, ReadByte(_GameRAM_Address + 0x00B69FF8));
        }

        private void Compute_Mok_Outputs()
        {
            //Player status :
            UInt64 P1_Data_Address = _GameRAM_Address + (ReadUInt32(_GameRAM_Address + 0x00023464) & 0x01FFFFFF);
            UInt64 P2_Data_Address = P1_Data_Address + 0x64;
            for (int i = 0; i < _GameConfig.PlayerCount; i++)
            {
                _Life[i] = 0;
                _Ammo[i] = 0;
            }

            //Player Status :
            //1 : Title Screen
            //2,3,4 : Demo
            //5 : Attract Mode
            //6 : Game Over
            //7 : Playing (cut scene)
            //8 : Playing
            //9 : continue Screen
            Byte P1_Status = ReadByte(P1_Data_Address + 0x48);
            Byte P2_Status = ReadByte(P2_Data_Address + 0x48);
            if (P1_Status == 8 || P1_Status == 7)
            {
                _Life[0] = ReadByte(P1_Data_Address + 0x5C);
                _Ammo[0] = ReadByte(P1_Data_Address + 0x58);

                //Custom Recoil
                if (_Ammo[0] < _LastAmmo[0])
                    SetOutputValue(OutputId.P1_CtmRecoil, 1);

                //[Damaged] custom Output                
                if (_Life[0] < _LastLife[0])
                    SetOutputValue(OutputId.P1_Damaged, 1);
            }
            if (P2_Status == 8 || P2_Status == 7)
            {
                _Life[1] = ReadByte(P2_Data_Address + 0x5C);
                _Ammo[1] = ReadByte(P2_Data_Address + 0x58);

                //Custom Recoil
                if (_Ammo[1] < _LastAmmo[1])
                    SetOutputValue(OutputId.P2_CtmRecoil, 1);

                //[Damaged] custom Output                
                if (_Life[1] < _LastLife[1])
                    SetOutputValue(OutputId.P2_Damaged, 1);
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
            SetOutputValue(OutputId.P1_LmpStart, ReadByte(_GameRAM_Address + 0x000EC396) >> 7 & 0x01);
            SetOutputValue(OutputId.P2_LmpStart, ReadByte(_GameRAM_Address + 0x000EC396) >> 4 & 0x01);
            SetOutputValue(OutputId.Credits, ReadByte(P1_Data_Address + 0x7C));
        }

        #endregion
    }
}
