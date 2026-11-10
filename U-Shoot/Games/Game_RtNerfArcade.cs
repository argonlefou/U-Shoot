using System;
using System.Collections.Generic;
using UsCore.Config;
using UsCore.IPC;
using UsCore.MameOutput;
using UsCore.VirtualControllers;

namespace U_Shoot.Games
{
    public class Game_RtNerfArcade : BaseGameUnity64
    {
        protected override bool PATCH_PEB_DEBUG_FLAG { get { return false; } }

        private class InputData : Base_InputData
        {
            public float[] Axis_X = null;
            public float[] Axis_Y = null;
            public byte[] Coin = null;
            public byte[] Start = null;
            public byte[] Trigger = null;
            public byte[] Shoulder = null;
            public byte Service = 0;
            public byte Test = 0;
            public byte VolumeUp = 0;
            public byte VolumeDown = 0;
            public byte Dbv = 0;
            public byte Exit = 0;

            public InputData(int PlayerNumber) : base(PlayerNumber)
            { }
        }

        private class OutputData : Base_OutputData
        {
            public byte[] IsPlaying = null;
            public byte[] Recoil = null;
            public UInt32[] Credits = null;
            public UInt16 P1_Lmp_Start = 0;
            public UInt16 P1_Lmp_SeatPuck = 0;
            public UInt16 P1_Lmp_SeatMarquee = 0;
            public UInt16 P1_Lmp_SeatRear_R = 0;
            public UInt16 P1_Lmp_SeatRear_O = 0;
            public UInt16 P1_Lmp_SeatRear_B = 0;
            public UInt16 P2_Lmp_Start = 0;
            public UInt16 P2_Lmp_SeatPuck = 0;
            public UInt16 P2_Lmp_SeatMarquee = 0;
            public UInt16 P2_Lmp_SeatRear_R = 0;
            public UInt16 P2_Lmp_SeatRear_O = 0;
            public UInt16 P2_Lmp_SeatRear_B = 0;
            public UInt16 Cab_Lmp_R = 0;
            public UInt16 Cab_Lmp_G = 0;
            public UInt16 Cab_Lmp_B = 0;
            public UInt16 Cab_Lmp_RearSeat = 0;

            public OutputData(int PlayerNumber) : base(PlayerNumber)
            { }
        }

        public Game_RtNerfArcade(GameConfig Config) : base(Config)
        {
            _KnownMd5Prints.Add("Nerf Arcade v1.55", "7f40b5a56501507b9e899f1d58401817");

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
            else if (e.ButtonName.Equals("SERVICE"))
            {
                ((InputData)_InputData).Service = 1;
            }
            else if (e.ButtonName.Equals("TEST"))
            {
                ((InputData)_InputData).Test = 1;
            }
            else if (e.ButtonName.Equals("VOLUME UP"))
            {
                ((InputData)_InputData).VolumeUp = 1;
            }
            else if (e.ButtonName.Equals("VOLUME DOWN"))
            {
                ((InputData)_InputData).VolumeDown = 1;
            }
            else if (e.ButtonName.Equals("DBV"))
            {
                ((InputData)_InputData).Dbv = 1;
            }

            _Tcpclient.SendMessage(_InputData.ToByteArray());
        }

        protected override void GameController_OnButtonUp(object sender, VirtualGameControler.ButtonDownEventArgs e)
        {
            if (e.ButtonName.Equals("EXIT"))
            {
                ((InputData)_InputData).Exit = 0;
            }
            else if (e.ButtonName.Equals("SERVICE"))
            {
                ((InputData)_InputData).Service = 0;
            }
            else if (e.ButtonName.Equals("TEST"))
            {
                ((InputData)_InputData).Test = 0;
            }
            else if (e.ButtonName.Equals("VOLUME UP"))
            {
                ((InputData)_InputData).VolumeUp = 0;
            }
            else if (e.ButtonName.Equals("VOLUME DOWN"))
            {
                ((InputData)_InputData).VolumeDown = 0;
            }
            else if (e.ButtonName.Equals("DBV"))
            {
                ((InputData)_InputData).Dbv = 0;
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
                else if (e.ButtonName.Equals("SHOULDER"))
                {
                    ((InputData)_InputData).Shoulder[PlayerIndex] = 1;
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
                else if (e.ButtonName.Equals("SHOULDER"))
                {
                    ((InputData)_InputData).Shoulder[PlayerIndex] = 0;
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
                new GameOutput(OutputId.P1_Lmp_SeatPuck),
                new GameOutput(OutputId.P1_Lmp_SeatMarquee),
                new GameOutput(OutputId.P1_Lmp_SeatSpeaker_R),
                new GameOutput(OutputId.P1_Lmp_SeatSpeaker_O),
                new GameOutput(OutputId.P1_Lmp_SeatSpeaker_B),
                new GameOutput(OutputId.P2_LmpStart),
                new GameOutput(OutputId.P2_Lmp_SeatPuck),
                new GameOutput(OutputId.P2_Lmp_SeatMarquee),
                new GameOutput(OutputId.P2_Lmp_SeatSpeaker_R),
                new GameOutput(OutputId.P2_Lmp_SeatSpeaker_O),
                new GameOutput(OutputId.P2_Lmp_SeatSpeaker_B),
                new GameOutput(OutputId.Lmp_TMolding_R),
                new GameOutput(OutputId.Lmp_TMolding_G),
                new GameOutput(OutputId.Lmp_TMolding_B),
                new GameOutput(OutputId.Lmp_SeatDownLight),
                new AsyncGameOutput(OutputId.P1_CtmRecoil, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_ON_DELAY), ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_OFF_DELAY), 0),
                new AsyncGameOutput(OutputId.P2_CtmRecoil, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_ON_DELAY), ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_OFF_DELAY), 0),
                new GameOutput(OutputId.P1_Credits),
                new GameOutput(OutputId.P2_Credits)
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

                SetOutputValue(OutputId.P1_CtmRecoil, ((OutputData)_OutputData).Recoil[0]);
                SetOutputValue(OutputId.P2_CtmRecoil, ((OutputData)_OutputData).Recoil[1]);
                SetOutputValue(OutputId.P1_Credits, (int)((OutputData)_OutputData).Credits[0]);
                SetOutputValue(OutputId.P2_Credits, (int)((OutputData)_OutputData).Credits[1]);
                //Lamps
                SetOutputValue(OutputId.P1_LmpStart, ((OutputData)_OutputData).P1_Lmp_Start);
                SetOutputValue(OutputId.P1_Lmp_SeatPuck, ((OutputData)_OutputData).P1_Lmp_SeatPuck);
                SetOutputValue(OutputId.P1_Lmp_SeatMarquee, ((OutputData)_OutputData).P1_Lmp_SeatMarquee);
                SetOutputValue(OutputId.P1_Lmp_SeatSpeaker_R, ((OutputData)_OutputData).P1_Lmp_SeatRear_R);
                SetOutputValue(OutputId.P1_Lmp_SeatSpeaker_O, ((OutputData)_OutputData).P1_Lmp_SeatRear_O);
                SetOutputValue(OutputId.P1_Lmp_SeatSpeaker_B, ((OutputData)_OutputData).P1_Lmp_SeatRear_B);
                SetOutputValue(OutputId.P2_LmpStart, ((OutputData)_OutputData).P2_Lmp_Start);
                SetOutputValue(OutputId.P2_Lmp_SeatPuck, ((OutputData)_OutputData).P2_Lmp_SeatPuck);
                SetOutputValue(OutputId.P2_Lmp_SeatMarquee, ((OutputData)_OutputData).P2_Lmp_SeatMarquee);
                SetOutputValue(OutputId.P2_Lmp_SeatSpeaker_R, ((OutputData)_OutputData).P2_Lmp_SeatRear_R);
                SetOutputValue(OutputId.P2_Lmp_SeatSpeaker_O, ((OutputData)_OutputData).P2_Lmp_SeatRear_O);
                SetOutputValue(OutputId.P2_Lmp_SeatSpeaker_B, ((OutputData)_OutputData).P2_Lmp_SeatRear_B);
                SetOutputValue(OutputId.Lmp_TMolding_R, ((OutputData)_OutputData).Cab_Lmp_R);
                SetOutputValue(OutputId.Lmp_TMolding_G, ((OutputData)_OutputData).Cab_Lmp_G);
                SetOutputValue(OutputId.Lmp_TMolding_B, ((OutputData)_OutputData).Cab_Lmp_B);
                SetOutputValue(OutputId.Lmp_SeatDownLight, ((OutputData)_OutputData).Cab_Lmp_RearSeat);
            }
        }

        #endregion
    }
}
