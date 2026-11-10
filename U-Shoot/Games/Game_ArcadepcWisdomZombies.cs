using System;
using System.Collections.Generic;
using UsCore.Config;
using UsCore.IPC;
using UsCore.MameOutput;
using UsCore.VirtualControllers;

namespace U_Shoot.Games
{
    public class Game_ArcadepcWisdomZombies : BaseGameUnity64
    {
        protected override bool PATCH_PEB_DEBUG_FLAG { get { return false; } }

        private class InputData : Base_InputData
        {
            public float[] Axis_X = null;
            public float[] Axis_Y = null;
            public byte[] Coin = null;
            public byte[] Start = null;
            public byte Exit;
            public byte Test;
            public byte MenuUp;
            public byte MenuDown;
            public byte MenuSelect;

            public InputData(int PlayerNumber) : base(PlayerNumber)
            {
            }
        }

        private class OutputData : Base_OutputData
        {
            public byte[] IsPlaying = null;
            public byte[] StartLamp = null;
            public byte[] SmallWater = null;
            public byte[] BigWater = null;
            public byte[] GunMotor = null;
            public byte[] TicketFeeder = null;
            public byte BonusWeaponLamp = 0;
            public byte SeatVibrationMotor = 0;
            public byte WaterLevelLamp = 0;
            public byte[] Life = null;
            public byte[] Damaged = null;
            public int[] Credits = null;

            public OutputData(int PlayerNumber) : base(PlayerNumber)
            {
            }
        }

        public Game_ArcadepcWisdomZombies(GameConfig Config) : base(Config)
        {
            _KnownMd5Prints.Add("ShootZombies EN v1.2.8 - Original", "f0288515dd04d49c85dedacaaf922edd");

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
            else if (e.ButtonName.Equals("MENU SELECT"))
            {
                ((InputData)_InputData).MenuSelect = 1;
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
            else if (e.ButtonName.Equals("MENU SELECT"))
            {
                ((InputData)_InputData).MenuSelect = 0;
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
                new SyncBlinkingGameOutput(OutputId.P1_LmpStart, 500),
                new SyncBlinkingGameOutput(OutputId.P2_LmpStart, 500),
                new SyncBlinkingGameOutput(OutputId.P3_LmpStart, 500),
                new SyncBlinkingGameOutput(OutputId.P4_LmpStart, 500),
                new GameOutput(OutputId.P1_WaterFire),
                new GameOutput(OutputId.P2_WaterFire),
                new GameOutput(OutputId.P3_WaterFire),
                new GameOutput(OutputId.P4_WaterFire),
                new GameOutput(OutputId.P1_BigGun),
                new GameOutput(OutputId.P2_BigGun),
                new GameOutput(OutputId.P3_BigGun),
                new GameOutput(OutputId.P4_BigGun),
                new GameOutput(OutputId.P1_GunMotor),
                new GameOutput(OutputId.P2_GunMotor),
                new GameOutput(OutputId.P3_GunMotor),
                new GameOutput(OutputId.P4_GunMotor),
                /*_Outputs.Add(new GameOutput(OutputId.P1_TicketFeeder));
                _Outputs.Add(new GameOutput(OutputId.P2_TicketFeeder));
                _Outputs.Add(new GameOutput(OutputId.P3_TicketFeeder));
                _Outputs.Add(new GameOutput(OutputId.P4_TicketFeeder));*/
                new GameOutput(OutputId.BonusWeaponLamp),
                new GameOutput(OutputId.SeatVibrationLamp),
                new GameOutput(OutputId.WaterLevelLamp),
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

                if (((OutputData)_OutputData).StartLamp[0] == 1)
                    SetOutputValue(OutputId.P1_LmpStart, -1);
                else
                    SetOutputValue(OutputId.P1_LmpStart, 0);

                if (((OutputData)_OutputData).StartLamp[1] == 1)
                    SetOutputValue(OutputId.P2_LmpStart, -1);
                else
                    SetOutputValue(OutputId.P2_LmpStart, 0);

                if (((OutputData)_OutputData).StartLamp[2] == 1)
                    SetOutputValue(OutputId.P3_LmpStart, -1);
                else
                    SetOutputValue(OutputId.P3_LmpStart, 0);

                if (((OutputData)_OutputData).StartLamp[3] == 1)
                    SetOutputValue(OutputId.P4_LmpStart, -1);
                else
                    SetOutputValue(OutputId.P4_LmpStart, 0);

                SetOutputValue(OutputId.P1_WaterFire, ((OutputData)_OutputData).SmallWater[0]);
                SetOutputValue(OutputId.P2_WaterFire, ((OutputData)_OutputData).SmallWater[1]);
                SetOutputValue(OutputId.P3_WaterFire, ((OutputData)_OutputData).SmallWater[2]);
                SetOutputValue(OutputId.P4_WaterFire, ((OutputData)_OutputData).SmallWater[3]);

                SetOutputValue(OutputId.P1_BigGun, ((OutputData)_OutputData).BigWater[0]);
                SetOutputValue(OutputId.P2_BigGun, ((OutputData)_OutputData).BigWater[1]);
                SetOutputValue(OutputId.P3_BigGun, ((OutputData)_OutputData).BigWater[2]);
                SetOutputValue(OutputId.P4_BigGun, ((OutputData)_OutputData).BigWater[3]);

                SetOutputValue(OutputId.P1_GunMotor, ((OutputData)_OutputData).GunMotor[0]);
                SetOutputValue(OutputId.P2_GunMotor, ((OutputData)_OutputData).GunMotor[1]);
                SetOutputValue(OutputId.P3_GunMotor, ((OutputData)_OutputData).GunMotor[2]);
                SetOutputValue(OutputId.P4_GunMotor, ((OutputData)_OutputData).GunMotor[3]);

                /*SetOutputValue(OutputId.P1_TicketFeeder, ((OutputData)_OutputData).TicketFeeder[0]);
                SetOutputValue(OutputId.P2_TicketFeeder, ((OutputData)_OutputData).TicketFeeder[1]);
                SetOutputValue(OutputId.P3_TicketFeeder, ((OutputData)_OutputData).TicketFeeder[2]);
                SetOutputValue(OutputId.P4_TicketFeeder, ((OutputData)_OutputData).TicketFeeder[3]);*/

                SetOutputValue(OutputId.BonusWeaponLamp, ((OutputData)_OutputData).BonusWeaponLamp);
                SetOutputValue(OutputId.SeatVibrationLamp, ((OutputData)_OutputData).SeatVibrationMotor);
                SetOutputValue(OutputId.WaterLevelLamp, ((OutputData)_OutputData).WaterLevelLamp);

                SetOutputValue(OutputId.P1_Damaged, ((OutputData)_OutputData).Damaged[0]);
                SetOutputValue(OutputId.P2_Damaged, ((OutputData)_OutputData).Damaged[1]);
                SetOutputValue(OutputId.P3_Damaged, ((OutputData)_OutputData).Damaged[2]);
                SetOutputValue(OutputId.P4_Damaged, ((OutputData)_OutputData).Damaged[3]);

                if (((OutputData)_OutputData).IsPlaying[0] == 1)
                    SetOutputValue(OutputId.P1_Life, (int)((OutputData)_OutputData).Life[0]);
                else
                    SetOutputValue(OutputId.P1_Life, 0);
                if (((OutputData)_OutputData).IsPlaying[1] == 1)
                    SetOutputValue(OutputId.P2_Life, (int)((OutputData)_OutputData).Life[1]);
                else
                    SetOutputValue(OutputId.P2_Life, 0);
                if (((OutputData)_OutputData).IsPlaying[2] == 1)
                    SetOutputValue(OutputId.P3_Life, (int)((OutputData)_OutputData).Life[2]);
                else
                    SetOutputValue(OutputId.P4_Life, 0);
                if (((OutputData)_OutputData).IsPlaying[3] == 1)
                    SetOutputValue(OutputId.P4_Life, (int)((OutputData)_OutputData).Life[3]);
                else
                    SetOutputValue(OutputId.P4_Life, 0);

                SetOutputValue(OutputId.P1_Credits, (int)((OutputData)_OutputData).Credits[0]);
                SetOutputValue(OutputId.P2_Credits, (int)((OutputData)_OutputData).Credits[1]);
                SetOutputValue(OutputId.P3_Credits, (int)((OutputData)_OutputData).Credits[2]);
                SetOutputValue(OutputId.P4_Credits, (int)((OutputData)_OutputData).Credits[3]);
            }
        }

        #endregion
    }
}
