using System;
using System.Collections.Generic;
using UsCore.Config;
using UsCore.IPC;
using UsCore.MameOutput;
using UsCore.VirtualControllers;

namespace U_Shoot.Games
{
    public class Game_ArcadepcMechaTornado : BaseGameUnity32
    {
        private class InputData : Base_InputData
        {
            public float[] Axis_X = null;
            public float[] Axis_Y = null;
            public byte[] Coin = null;
            public byte[] Trigger = null;
            public byte[] Start = null;
            public byte Test = 0;
            public byte MenuLeft = 0;
            public byte MenuRight = 0;
            public byte Exit = 0;

            public InputData(int PlayerNumber) : base(PlayerNumber)
            {
            }
        }

        private class OutputData : Base_OutputData
        {
            public byte Atomizer = 0;
            public byte[] IsPlaying = null;
            public byte[] Shake = null;
            public byte[] RotatingMotor = null;
            public byte[] WaterPower = null;
            public byte[] StartLight = null;
            public byte[] PlayerLight = null;
            public byte[] LightBelt = null;
            public byte[] Damaged = null;
            public byte[] Recoil = null;
            public float[] Life = null;
            public int[] Credits = null;

            public OutputData(int PlayerNumber) : base(PlayerNumber)
            {
            }
        }

        public Game_ArcadepcMechaTornado(GameConfig Config) : base(Config)
        {
            _KnownMd5Prints.Add("Mecha Tornado Arcade - v1.5", "32458270101d83dd6e0f08d0c617bf7e");

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
                else if (e.ButtonName.Equals("START"))
                {
                    ((InputData)_InputData).Start[PlayerIndex] = 1;
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
                else if (e.ButtonName.Equals("START"))
                {
                    ((InputData)_InputData).Start[PlayerIndex] = 0;
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
                new GameOutput(OutputId.P1_LmpStart),
                new GameOutput(OutputId.P2_LmpStart),
                new GameOutput(OutputId.P3_LmpStart),
                new GameOutput(OutputId.P4_LmpStart),
                new GameOutput(OutputId.P1_LmpFront),
                new GameOutput(OutputId.P2_LmpFront),
                new GameOutput(OutputId.P3_LmpFront),
                new GameOutput(OutputId.P4_LmpFront),
                new GameOutput(OutputId.P1_GunMotor),
                new GameOutput(OutputId.P2_GunMotor),
                new GameOutput(OutputId.P3_GunMotor),
                new GameOutput(OutputId.P4_GunMotor),
                new GameOutput(OutputId.P1_Shaker),
                new GameOutput(OutputId.P2_Shaker),
                new GameOutput(OutputId.P3_Shaker),
                new GameOutput(OutputId.P4_Shaker),
                new GameOutput(OutputId.P1_WaterFire),
                new GameOutput(OutputId.P2_WaterFire),
                new GameOutput(OutputId.P3_WaterFire),
                new GameOutput(OutputId.P4_WaterFire),
                new GameOutput(OutputId.P1_BigGun),
                new GameOutput(OutputId.P2_BigGun),
                new GameOutput(OutputId.P3_BigGun),
                new GameOutput(OutputId.P4_BigGun),
                //_Outputs.Add(new GameOutput(OutputId.P1_TicketFeeder));
                //_Outputs.Add(new GameOutput(OutputId.P2_TicketFeeder));
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
                new AsyncGameOutput(OutputId.P4_Damaged, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_DAMAGED_DELAY), 100, 0),
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

                SetOutputValue(OutputId.P1_LmpStart, ((OutputData)_OutputData).StartLight[0]);
                SetOutputValue(OutputId.P2_LmpStart, ((OutputData)_OutputData).StartLight[1]);
                SetOutputValue(OutputId.P3_LmpStart, ((OutputData)_OutputData).StartLight[2]);
                SetOutputValue(OutputId.P4_LmpStart, ((OutputData)_OutputData).StartLight[3]);

                SetOutputValue(OutputId.P1_LmpFront, ((OutputData)_OutputData).PlayerLight[0]);
                SetOutputValue(OutputId.P2_LmpFront, ((OutputData)_OutputData).PlayerLight[1]);
                SetOutputValue(OutputId.P3_LmpFront, ((OutputData)_OutputData).PlayerLight[2]);
                SetOutputValue(OutputId.P4_LmpFront, ((OutputData)_OutputData).PlayerLight[3]);

                SetOutputValue(OutputId.P1_GunMotor, ((OutputData)_OutputData).RotatingMotor[0]);
                SetOutputValue(OutputId.P2_GunMotor, ((OutputData)_OutputData).RotatingMotor[1]);
                SetOutputValue(OutputId.P3_GunMotor, ((OutputData)_OutputData).RotatingMotor[2]);
                SetOutputValue(OutputId.P4_GunMotor, ((OutputData)_OutputData).RotatingMotor[3]);

                SetOutputValue(OutputId.P1_Shaker, ((OutputData)_OutputData).Shake[0]);
                SetOutputValue(OutputId.P2_Shaker, ((OutputData)_OutputData).Shake[1]);
                SetOutputValue(OutputId.P3_Shaker, ((OutputData)_OutputData).Shake[2]);
                SetOutputValue(OutputId.P4_Shaker, ((OutputData)_OutputData).Shake[3]);

                SetOutputValue(OutputId.P1_WaterFire, ((OutputData)_OutputData).WaterPower[0]);
                SetOutputValue(OutputId.P2_WaterFire, ((OutputData)_OutputData).WaterPower[1]);
                SetOutputValue(OutputId.P3_WaterFire, ((OutputData)_OutputData).WaterPower[2]);
                SetOutputValue(OutputId.P4_WaterFire, ((OutputData)_OutputData).WaterPower[3]);

                //SetOutputValue(OutputId.WaterPump, _OutputData.WaterPump);

                if (((OutputData)_OutputData).IsPlaying[0] == 1)
                {
                    SetOutputValue(OutputId.P1_Life, (int)((OutputData)_OutputData).Life[0]);
                    SetOutputValue(OutputId.P1_CtmRecoil, (int)((OutputData)_OutputData).Recoil[0]);
                    SetOutputValue(OutputId.P1_Damaged, (int)((OutputData)_OutputData).Damaged[0]);
                }
                else
                {
                    SetOutputValue(OutputId.P1_Life, 0);
                }

                if (((OutputData)_OutputData).IsPlaying[1] == 1)
                {
                    SetOutputValue(OutputId.P2_Life, (int)((OutputData)_OutputData).Life[1]);
                    SetOutputValue(OutputId.P2_CtmRecoil, (int)((OutputData)_OutputData).Recoil[1]);
                    SetOutputValue(OutputId.P2_Damaged, (int)((OutputData)_OutputData).Damaged[1]);
                }
                else
                {
                    SetOutputValue(OutputId.P2_Life, 0);
                }

                if (((OutputData)_OutputData).IsPlaying[2] == 1)
                {
                    SetOutputValue(OutputId.P3_Life, (int)((OutputData)_OutputData).Life[2]);
                    SetOutputValue(OutputId.P3_CtmRecoil, (int)((OutputData)_OutputData).Recoil[2]);
                    SetOutputValue(OutputId.P3_Damaged, (int)((OutputData)_OutputData).Damaged[2]);
                }
                else
                {
                    SetOutputValue(OutputId.P3_Life, 0);
                }

                if (((OutputData)_OutputData).IsPlaying[3] == 1)
                {
                    SetOutputValue(OutputId.P4_Life, (int)((OutputData)_OutputData).Life[3]);
                    SetOutputValue(OutputId.P4_CtmRecoil, (int)((OutputData)_OutputData).Recoil[3]);
                    SetOutputValue(OutputId.P4_Damaged, (int)((OutputData)_OutputData).Damaged[3]);
                }
                else
                {
                    SetOutputValue(OutputId.P4_Life, 0);
                }

                SetOutputValue(OutputId.P1_Credits, (int)((OutputData)_OutputData).Credits[0]);
                SetOutputValue(OutputId.P2_Credits, (int)((OutputData)_OutputData).Credits[1]);
                SetOutputValue(OutputId.P3_Credits, (int)((OutputData)_OutputData).Credits[2]);
                SetOutputValue(OutputId.P4_Credits, (int)((OutputData)_OutputData).Credits[3]);
            }
        }

        #endregion
    }
}

