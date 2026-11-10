using System;
using System.Collections.Generic;
using UsCore.Config;
using UsCore.IPC;
using UsCore.MameOutput;
using UsCore.VirtualControllers;

namespace U_Shoot.Games
{
    public class Game_ArcadepcMIB : BaseGameUnity64
    {
        protected override bool PATCH_PEB_DEBUG_FLAG { get { return false; } }

        private class InputData : Base_InputData
        {
            public float[] Axis_X = null;
            public float[] Axis_Y = null;
            public byte[] Start = null;
            public byte[] Trigger = null;
            public byte Coin = 0;
            public byte Exit = 0;
            public byte HideGuns = 0;

            public InputData(int PlayerNumber) : base(PlayerNumber)
            {
            }
        }

        private class OutputData : Base_OutputData
        {
            public byte[] IsPlaying = null;
            public byte[] Recoil = null;
            public byte[] Motor = null;
            public byte[] Damaged = null;
            public byte NeuralyzerLamp = 0;
            public byte[] GunLight = null;
            public int Credits = 0;

            public OutputData(int PlayerNumber) : base(PlayerNumber)
            {
            }
        }

        public Game_ArcadepcMIB(GameConfig Config) : base(Config)
        {
            _KnownMd5Prints.Add("Men In Blacks - Original", "2ded47c90d49c4afd1bf5d510a7b6bbd");

            _InputData = new InputData(_GameConfig.PlayerCount);
            _OutputData = new OutputData(_GameConfig.PlayerCount);

            if (_GameConfig.NoGun)
                ((InputData)_InputData).HideGuns = 1;
            else
                ((InputData)_InputData).HideGuns = 0;
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
            else if (e.ButtonName.Equals("COIN"))
            {
                ((InputData)_InputData).Coin = 1;
            }

            _Tcpclient.SendMessage(_InputData.ToByteArray());
        }

        protected override void GameController_OnButtonUp(object sender, VirtualGameControler.ButtonDownEventArgs e)
        {
            if (e.ButtonName.Equals("EXIT"))
            {
                ((InputData)_InputData).Exit = 0;
            }
            else if (e.ButtonName.Equals("COIN"))
            {
                ((InputData)_InputData).Coin = 0;
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

                if (e.ButtonName.Equals("START"))
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

                if (e.ButtonName.Equals("START"))
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
                new GameOutput(OutputId.P1_LmpGun),
                new GameOutput(OutputId.P2_LmpGun),
                new GameOutput(OutputId.P1_GunMotor),
                new GameOutput(OutputId.P2_GunMotor),
                new AsyncGameOutput(OutputId.P1_CtmRecoil, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_ON_DELAY), ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_OFF_DELAY), 0),
                new AsyncGameOutput(OutputId.P2_CtmRecoil, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_ON_DELAY), ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_OFF_DELAY), 0),
                new AsyncGameOutput(OutputId.P1_Damaged, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_DAMAGED_DELAY), 100, 0),
                new AsyncGameOutput(OutputId.P2_Damaged, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_DAMAGED_DELAY), 100, 0),
                new AsyncGameOutput(OutputId.Neuralizer, 200, 100, 0),
                new GameOutput(OutputId.Credits)
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

                if (((OutputData)_OutputData).IsPlaying[0] == 1)
                {
                    SetOutputValue(OutputId.P1_CtmRecoil, ((OutputData)_OutputData).Recoil[0]);
                    SetOutputValue(OutputId.P1_LmpGun, ((OutputData)_OutputData).GunLight[0]);
                    SetOutputValue(OutputId.P1_GunMotor, ((OutputData)_OutputData).Motor[0]);
                    SetOutputValue(OutputId.P1_Damaged, ((OutputData)_OutputData).Damaged[0]);
                }
                else
                {
                    SetOutputValue(OutputId.P1_GunMotor, 0);
                }

                if (((OutputData)_OutputData).IsPlaying[1] == 1)
                {
                    SetOutputValue(OutputId.P2_CtmRecoil, ((OutputData)_OutputData).Recoil[1]);
                    SetOutputValue(OutputId.P2_LmpGun, ((OutputData)_OutputData).GunLight[1]);
                    SetOutputValue(OutputId.P2_GunMotor, ((OutputData)_OutputData).Motor[1]);
                    SetOutputValue(OutputId.P2_Damaged, ((OutputData)_OutputData).Damaged[1]);
                }
                else
                {
                    SetOutputValue(OutputId.P2_GunMotor, 0);
                }

                SetOutputValue(OutputId.Neuralizer, ((OutputData)_OutputData).NeuralyzerLamp);
                SetOutputValue(OutputId.Credits, (int)((OutputData)_OutputData).Credits);
            }
        }

        #endregion
    }
}
