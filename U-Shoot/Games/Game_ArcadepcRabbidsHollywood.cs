using System;
using System.Collections.Generic;
using UsCore.Config;
using UsCore.IPC;
using UsCore.MameOutput;
using UsCore.VirtualControllers;

namespace U_Shoot.Games
{
    public class Game_ArcadepcRabbidsHollywood : BaseGameUnity64
    {
        protected override bool PATCH_PEB_DEBUG_FLAG { get { return false; } }

        private class InputData : Base_InputData
        {
            public float[] Axis_X = null;
            public float[] Axis_Y = null;
            public byte[] Start = null;
            public byte[] Coin = null;
            public byte[] Trigger = null;
            public byte Test = 0;
            public byte MenuUp = 0;
            public byte MenuDown = 0;
            public byte MenuSelect = 0;
            public byte Exit = 0;

            public InputData(int PlayerNumber) : base(PlayerNumber)
            { }
        }

        private class OutputData : Base_OutputData
        {
            public byte[] StartLamp = null;
            public byte[] Recoil = null;
            public byte[] Damaged = null;
            public float[] Life = null;
            public int[] Ammo = null;
            public int[] Credits = null;

            public OutputData(int PlayerNumber) : base(PlayerNumber)
            { }
        }

        public Game_ArcadepcRabbidsHollywood(GameConfig Config) : base(Config)
        {
            _KnownMd5Prints.Add("Rabbids Hollywood Arcade - Clean Dump", "2dac74521cd3bb08b61f93830bf2660d");
            _KnownMd5Prints.Add("Rabbids Hollywood Arcade - Patch by Ducon v2 (dongle+operator)", "72b58266f2d1311b2ba2e7c96ca774fa");
            _KnownMd5Prints.Add("Rabbids Hollywood Arcade - Patch by Ducon v3 (dongle+operator+attract mode)", "7edf14803ae7d43d14e7459b2baa651e");
            _KnownMd5Prints.Add("Rabbids Hollywood Arcade - Patch by Argon (dongle)", "1e74554181161f8a83084e02beeec5fc");

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

                if (e.ButtonName.Equals("TRIGGER"))
                {
                    ((InputData)_InputData).Trigger[PlayerIndex] = 1;
                }
                else if (e.ButtonName.Equals("START"))
                {
                    ((InputData)_InputData).Start[PlayerIndex] = 1;
                }
                else if (e.ButtonName.Equals("COIN"))
                {
                    ((InputData)_InputData).Coin[PlayerIndex] = 1;
                }

                _Tcpclient.SendMessage(_InputData.ToByteArray());
            }
        }
        protected override void PlayerController_OnButtonUp(object sender, VirtualGameControler.ButtonDownEventArgs e)
        {
            if (e.Id != PlayerID.None)
            {
                UInt32 PlayerIndex = (UInt32)e.Id;

                if (e.ButtonName.Equals("TRIGGER"))
                {
                    ((InputData)_InputData).Trigger[PlayerIndex] = 0;
                }
                else if (e.ButtonName.Equals("START"))
                {
                    ((InputData)_InputData).Start[PlayerIndex] = 0;
                }
                else if (e.ButtonName.Equals("COIN"))
                {
                    ((InputData)_InputData).Coin[PlayerIndex] = 0;
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
                SetOutputValue(OutputId.P1_LmpStart, ((OutputData)_OutputData).Recoil[0]);
                SetOutputValue(OutputId.P2_LmpStart, ((OutputData)_OutputData).Recoil[1]);
                SetOutputValue(OutputId.P3_LmpStart, ((OutputData)_OutputData).Recoil[2]);
                SetOutputValue(OutputId.P4_LmpStart, ((OutputData)_OutputData).Recoil[3]);
                SetOutputValue(OutputId.P1_CtmRecoil, ((OutputData)_OutputData).Recoil[0]);
                SetOutputValue(OutputId.P2_CtmRecoil, ((OutputData)_OutputData).Recoil[1]);
                SetOutputValue(OutputId.P3_CtmRecoil, ((OutputData)_OutputData).Recoil[2]);
                SetOutputValue(OutputId.P4_CtmRecoil, ((OutputData)_OutputData).Recoil[3]);
                SetOutputValue(OutputId.P1_Life, (int)((OutputData)_OutputData).Life[0]);
                SetOutputValue(OutputId.P2_Life, (int)((OutputData)_OutputData).Life[1]);
                SetOutputValue(OutputId.P3_Life, (int)((OutputData)_OutputData).Life[2]);
                SetOutputValue(OutputId.P4_Life, (int)((OutputData)_OutputData).Life[3]);
                SetOutputValue(OutputId.P1_Damaged, ((OutputData)_OutputData).Damaged[0]);
                SetOutputValue(OutputId.P2_Damaged, ((OutputData)_OutputData).Damaged[1]);
                SetOutputValue(OutputId.P3_Damaged, ((OutputData)_OutputData).Damaged[2]);
                SetOutputValue(OutputId.P4_Damaged, ((OutputData)_OutputData).Damaged[3]);
                SetOutputValue(OutputId.P1_Credits, ((OutputData)_OutputData).Credits[0]);
                SetOutputValue(OutputId.P2_Credits, ((OutputData)_OutputData).Credits[1]);
                SetOutputValue(OutputId.P3_Credits, ((OutputData)_OutputData).Credits[2]);
                SetOutputValue(OutputId.P4_Credits, ((OutputData)_OutputData).Credits[3]);
            }
        }

        #endregion
    }
}
