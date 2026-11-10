using System;
using System.Collections.Generic;
using UsCore.Config;
using UsCore.IPC;
using UsCore.MameOutput;
using UsCore.VirtualControllers;

namespace U_Shoot.Games
{
    public class Game_ArcadepcSkullOfShadow : BaseGameUnity64
    {
        protected override bool PATCH_PEB_DEBUG_FLAG { get { return false; } }

        private class InputData : Base_InputData
        {
            public float[] Axis_X = null;
            public float[] Axis_Y = null;
            public byte[] Coin = null;
            public byte[] Trigger = null;
            public byte[] Action = null;
            public byte Test = 0;
            public byte MenuUp = 0;
            public byte MenuDown = 0;
            public byte MenuLeft = 0;
            public byte MenuRight = 0;
            public byte Exit = 0;

            public InputData(int PlayerNumber) : base(PlayerNumber)
            {
            }
        }

        private class OutputData : Base_OutputData
        {
            public byte[] IsPlaying = null;
            public byte[] Light = null;
            public byte[] SelectedWeapon = null;
            public byte[] GunMotor = null;
            public byte[] Recoil = null;
            public byte[] Damaged = null;
            public byte[] Life = null;
            public int[] Ammo = null;
            public int[] Credits = null;

            public OutputData(int PlayerNumber) : base(PlayerNumber)
            {
            }
        }

        public Game_ArcadepcSkullOfShadow(GameConfig Config) : base(Config)
        {
            _KnownMd5Prints.Add("Skull Of Shadow v2.54", "5089d486f1ea28642f2d746f915a6caa");

            _InputData = new InputData(_GameConfig.PlayerCount);
            _OutputData = new OutputData(_GameConfig.PlayerCount);
        }
        
        #region Inputs

        protected override void GameController_OnButtonDown(object sender, VirtualGameControler.ButtonDownEventArgs e)
        {
            if (e.ButtonName.Equals("EXIT"))
            {
                ((InputData)_InputData).Exit = 1;
                _Tcpclient.SendMessage(_InputData.ToByteArray());
                if (_Tcpclient != null)
                    _Tcpclient.Disconnect();
                Close();
            }
            else if (e.ButtonName.Equals("TEST"))
            {
                ((InputData)_InputData).Test = 1;
            }
            else if (e.ButtonName.Equals("MENU UP"))
            {
                ((InputData)_InputData).MenuUp = 1;
            }
            else if (e.ButtonName.Equals("MENU DOWN"))
            {
                ((InputData)_InputData).MenuDown = 1;
            }
            else if (e.ButtonName.Equals("MENU LEFT"))
            {
                ((InputData)_InputData).MenuLeft = 1;
            }
            else if (e.ButtonName.Equals("MENU RIGHT"))
            {
                ((InputData)_InputData).MenuRight = 1;
            }

            _Tcpclient.SendMessage(_InputData.ToByteArray());
        }

        protected override void GameController_OnButtonUp(object sender, VirtualGameControler.ButtonDownEventArgs e)
        {
            if (e.ButtonName.Equals("EXIT"))
            {
                ((InputData)_InputData).Exit = 0;
            }
            else if (e.ButtonName.Equals("TEST"))
            {
                ((InputData)_InputData).Test = 0;
            }
            else if (e.ButtonName.Equals("MENU UP"))
            {
                ((InputData)_InputData).MenuUp = 0;
            }
            else if (e.ButtonName.Equals("MENU DOWN"))
            {
                ((InputData)_InputData).MenuDown = 0;
            }
            else if (e.ButtonName.Equals("MENU LEFT"))
            {
                ((InputData)_InputData).MenuLeft = 0;
            }
            else if (e.ButtonName.Equals("MENU RIGHT"))
            {
                ((InputData)_InputData).MenuRight = 0;
            }

            _Tcpclient.SendMessage(_InputData.ToByteArray());
        }

        protected override void UpdateAxis(PlayerID Id, int X, int Y)
        {
            if (Id != PlayerID.None)
            {
                ((InputData)_InputData).Axis_X[(uint)Id] = (float)X;
                ((InputData)_InputData).Axis_Y[(uint)Id] = (float)Y;

                _Tcpclient.SendMessage(_InputData.ToByteArray());
            }
        }

        protected override void PlayerController_OnButtonDown(object sender, VirtualGameControler.ButtonDownEventArgs e)
        {
            if (e.Id != PlayerID.None)
            {
                UInt32 PlayerIndex = (UInt32)e.Id;

                if (e.ButtonName.Equals("COIN"))
                {
                    ((InputData)_InputData).Coin[PlayerIndex] = 1;
                }
                else if (e.ButtonName.Equals("ACTION"))
                {
                    ((InputData)_InputData).Action[PlayerIndex] = 1;
                }
                else if (e.ButtonName.Equals("TRIGGER"))
                {
                    ((InputData)_InputData).Trigger[PlayerIndex] = 1;
                }

                _Tcpclient.SendMessage(_InputData.ToByteArray());
            }
        }
        protected override void PlayerController_OnButtonUp(object sender, VirtualGameControler.ButtonDownEventArgs e)
        {
            if (e.Id != PlayerID.None)
            {
                UInt32 PlayerIndex = (UInt32)e.Id;

                if (e.ButtonName.Equals("COIN"))
                {
                    ((InputData)_InputData).Coin[PlayerIndex] = 0;
                }
                else if (e.ButtonName.Equals("ACTION"))
                {
                    ((InputData)_InputData).Action[PlayerIndex] = 0;
                }
                else if (e.ButtonName.Equals("TRIGGER"))
                {
                    ((InputData)_InputData).Trigger[PlayerIndex] = 0;
                }
                
                _Tcpclient.SendMessage(_InputData.ToByteArray());
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
                new GameOutput(OutputId.P1_LmpGun),
                new GameOutput(OutputId.P2_LmpGun),
                new GameOutput(OutputId.P3_LmpGun),
                new GameOutput(OutputId.P4_LmpGun),
                new GameOutput(OutputId.P1_GunMotor),
                new GameOutput(OutputId.P2_GunMotor),
                new GameOutput(OutputId.P3_GunMotor),
                new GameOutput(OutputId.P4_GunMotor),
                new AsyncGameOutput(OutputId.P1_CtmRecoil, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_ON_DELAY), ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_OFF_DELAY), 0),
                new AsyncGameOutput(OutputId.P2_CtmRecoil, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_ON_DELAY), ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_OFF_DELAY), 0),
                new AsyncGameOutput(OutputId.P3_CtmRecoil, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_ON_DELAY), ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_OFF_DELAY), 0),
                new AsyncGameOutput(OutputId.P4_CtmRecoil, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_ON_DELAY), ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_OFF_DELAY), 0),
                new GameOutput(OutputId.P1_Ammo),
                new GameOutput(OutputId.P2_Ammo),
                new GameOutput(OutputId.P3_Ammo),
                new GameOutput(OutputId.P4_Ammo),
                new AsyncGameOutput(OutputId.P1_Damaged, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_DAMAGED_DELAY), 100, 0),
                new AsyncGameOutput(OutputId.P2_Damaged, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_DAMAGED_DELAY), 100, 0),
                new AsyncGameOutput(OutputId.P3_Damaged, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_DAMAGED_DELAY), 100, 0),
                new AsyncGameOutput(OutputId.P4_Damaged, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_DAMAGED_DELAY), 100, 0),
                new GameOutput(OutputId.P1_Life),
                new GameOutput(OutputId.P2_Life),
                new GameOutput(OutputId.P3_Life),
                new GameOutput(OutputId.P4_Life),
                new GameOutput(OutputId.P1_Credits),
                new GameOutput(OutputId.P2_Credits),
                new GameOutput(OutputId.P3_Credits),
                new GameOutput(OutputId.P4_Credits)
            };
        }

        /// <summary>
        /// Update all Outputs values before sending them to MameHooker
        /// </summary>
        public override void UpdateOutputValues()
        {
            //Nothing to do here, update will be done by the Tcp packet received event
        }

        protected override void DsTcp_Client_PacketReceived(Object Sender, DsTcp_Client.PacketReceivedEventArgs e)
        {
            if (e.Packet.GetHeader() == DsTcp_TcpPacket.PacketHeader.Outputs)
            {
                _OutputData.Update(e.Packet.GetPayload());

                SetOutputValue(OutputId.P1_LmpGun, ((OutputData)_OutputData).Light[0]);
                SetOutputValue(OutputId.P2_LmpGun, ((OutputData)_OutputData).Light[1]);
                SetOutputValue(OutputId.P3_LmpGun, ((OutputData)_OutputData).Light[2]);
                SetOutputValue(OutputId.P4_LmpGun, ((OutputData)_OutputData).Light[3]);

                //Gun motor is activated permanently while the gun is firing
                //Value is vibration power
                SetOutputValue(OutputId.P1_GunMotor, (int)((OutputData)_OutputData).GunMotor[0]);
                SetOutputValue(OutputId.P2_GunMotor, (int)((OutputData)_OutputData).GunMotor[1]);
                SetOutputValue(OutputId.P3_GunMotor, (int)((OutputData)_OutputData).GunMotor[2]);
                SetOutputValue(OutputId.P4_GunMotor, (int)((OutputData)_OutputData).GunMotor[3]);

                SetOutputValue(OutputId.P1_CtmRecoil, ((OutputData)_OutputData).Recoil[0]);
                SetOutputValue(OutputId.P2_CtmRecoil, ((OutputData)_OutputData).Recoil[1]);
                SetOutputValue(OutputId.P3_CtmRecoil, ((OutputData)_OutputData).Recoil[2]);
                SetOutputValue(OutputId.P4_CtmRecoil, ((OutputData)_OutputData).Recoil[3]);

                SetOutputValue(OutputId.P1_Ammo, ((OutputData)_OutputData).Ammo[0]);
                SetOutputValue(OutputId.P2_Ammo, ((OutputData)_OutputData).Ammo[1]);
                SetOutputValue(OutputId.P3_Ammo, ((OutputData)_OutputData).Ammo[2]);
                SetOutputValue(OutputId.P4_Ammo, ((OutputData)_OutputData).Ammo[3]);

                SetOutputValue(OutputId.P1_Damaged, ((OutputData)_OutputData).Damaged[0]);
                SetOutputValue(OutputId.P2_Damaged, ((OutputData)_OutputData).Damaged[1]);
                SetOutputValue(OutputId.P3_Damaged, ((OutputData)_OutputData).Damaged[2]);
                SetOutputValue(OutputId.P4_Damaged, ((OutputData)_OutputData).Damaged[3]);

                SetOutputValue(OutputId.P1_Life, ((OutputData)_OutputData).Life[0]);
                SetOutputValue(OutputId.P2_Life, ((OutputData)_OutputData).Life[1]);
                SetOutputValue(OutputId.P3_Life, ((OutputData)_OutputData).Life[2]);
                SetOutputValue(OutputId.P4_Life, ((OutputData)_OutputData).Life[3]);

                //Gun motor is activated permanently while the gun is firing
                //Value is vibration power
                SetOutputValue(OutputId.P1_GunMotor, (int)((OutputData)_OutputData).GunMotor[0]);
                SetOutputValue(OutputId.P2_GunMotor, (int)((OutputData)_OutputData).GunMotor[1]);
                SetOutputValue(OutputId.P3_GunMotor, (int)((OutputData)_OutputData).GunMotor[2]);
                SetOutputValue(OutputId.P4_GunMotor, (int)((OutputData)_OutputData).GunMotor[3]);

                SetOutputValue(OutputId.P1_Credits, (int)((OutputData)_OutputData).Credits[0]);
                SetOutputValue(OutputId.P2_Credits, (int)((OutputData)_OutputData).Credits[1]);
                SetOutputValue(OutputId.P3_Credits, (int)((OutputData)_OutputData).Credits[2]);
                SetOutputValue(OutputId.P4_Credits, (int)((OutputData)_OutputData).Credits[3]);
            }
        }

        #endregion
    }
}
