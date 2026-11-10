using System;
using System.Collections.Generic;
using UsCore.Config;
using UsCore.IPC;
using UsCore.MameOutput;
using UsCore.VirtualControllers;

namespace U_Shoot.Games
{
    public class Game_WndDcop : BaseGameUnity64
    {
        protected override bool PATCH_PEB_DEBUG_FLAG { get { return false; } }

        private class InputData : Base_InputData
        {
            public float[] Axis_X = null;
            public float[] Axis_Y = null;
            public byte[] Trigger = null;
            public byte[] Reload = null;
            public byte Test = 0;
            public byte Exit = 0;

            public InputData(int PlayerNumber) : base(PlayerNumber)
            { }
        }

        private class OutputData : Base_OutputData
        {
            public byte GunRecoil = 0;
            public byte DirectHit = 0;
            public byte Police_LightBar = 0;
            public byte GreenTestLight = 0;
            public byte RedLight = 0;
            public byte WhiteStrobe = 0;
            public byte GunLight = 0;
            public byte P1_Life = 0;
            public byte P1_Ammo = 0;

            public OutputData(int PlayerNumber) : base(PlayerNumber)
            { }
        }

        public Game_WndDcop(GameConfig Config) : base(Config)
        {
            _KnownMd5Prints.Add("DCOP - Tenoke ISO - Original", "3940b478b0069635b579c8bd2a6729c1");

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

            _Tcpclient.SendMessage(_InputData.ToByteArray());
        }

        protected override void UpdateAxis(PlayerID Id, int X, int Y)
        {
            if (Id == PlayerID.Player1)
            {
                ((InputData)_InputData).Axis_X[(uint)Id] = (float)X;
                ((InputData)_InputData).Axis_Y[(uint)Id] = (float)Y;

                _Tcpclient.SendMessage(_InputData.ToByteArray());
            }
        }

        protected override void PlayerController_OnButtonDown(object sender, VirtualGameControler.ButtonDownEventArgs e)
        {
            if (e.Id == PlayerID.Player1)
            {
                UInt32 PlayerIndex = (UInt32)e.Id;

                if (e.ButtonName.Equals("TRIGGER"))
                {
                    ((InputData)_InputData).Trigger[PlayerIndex] = 1;
                }
                else if (e.ButtonName.Equals("RELOAD"))
                {
                    ((InputData)_InputData).Reload[PlayerIndex] = 1;
                }

                _Tcpclient.SendMessage(_InputData.ToByteArray());
            }
        }
        protected override void PlayerController_OnButtonUp(object sender, VirtualGameControler.ButtonDownEventArgs e)
        {
            if (e.Id == PlayerID.Player1)
            {
                UInt32 PlayerIndex = (UInt32)e.Id;

                if (e.ButtonName.Equals("TRIGGER"))
                {
                    ((InputData)_InputData).Trigger[PlayerIndex] = 0;
                }
                else if (e.ButtonName.Equals("RELOAD"))
                {
                    ((InputData)_InputData).Reload[PlayerIndex] = 0;
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
                new GameOutput(OutputId.P1_GunRecoil),
                new GameOutput(OutputId.Lmp_DirectHit),
                new GameOutput(OutputId.Lmp_PoliceBar),
                new GameOutput(OutputId.Lmp_GreenTestLight),
                new GameOutput(OutputId.Lmp_RedLight),
                new GameOutput(OutputId.Lmp_WhiteStrobe),
                new GameOutput(OutputId.P1_LmpGun),
                new AsyncGameOutput(OutputId.P1_CtmRecoil, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_ON_DELAY), ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_OFF_DELAY), 0),
                new AsyncGameOutput(OutputId.P1_Damaged, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_DAMAGED_DELAY), 100, 0),
                new GameOutput(OutputId.P1_Ammo),
                new GameOutput(OutputId.P1_Life)
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

                SetOutputValue(OutputId.P1_GunRecoil, ((OutputData)_OutputData).GunRecoil);
                SetOutputValue(OutputId.Lmp_DirectHit, ((OutputData)_OutputData).DirectHit);
                SetOutputValue(OutputId.Lmp_PoliceBar, ((OutputData)_OutputData).Police_LightBar);
                SetOutputValue(OutputId.Lmp_GreenTestLight, ((OutputData)_OutputData).GreenTestLight);
                SetOutputValue(OutputId.Lmp_RedLight, ((OutputData)_OutputData).RedLight);
                SetOutputValue(OutputId.Lmp_WhiteStrobe, ((OutputData)_OutputData).WhiteStrobe);
                SetOutputValue(OutputId.P1_LmpGun, ((OutputData)_OutputData).GunLight);
                SetOutputValue(OutputId.P1_Ammo, ((OutputData)_OutputData).P1_Ammo);
                SetOutputValue(OutputId.P1_Life, ((OutputData)_OutputData).P1_Life);

                SetOutputValue(OutputId.P1_CtmRecoil, ((OutputData)_OutputData).GunRecoil);

                if (_LastLife[0] > ((OutputData)_OutputData).P1_Life)
                    SetOutputValue(OutputId.P1_Damaged, 1);

                _LastLife[0] = ((OutputData)_OutputData).P1_Life;
            }
        }

        #endregion
    }
}
