using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UsCore.Config;
using UsCore.IPC;
using UsCore.MameOutput;
using UsCore.VirtualControllers;
using static U_Shoot.Games.Game_WndHod3pc;

namespace U_Shoot.Games
{
    public class Game_ArcadepcTopGun2 : BaseGameUnity64
    {
        protected override bool PATCH_PEB_DEBUG_FLAG { get { return false; } }

        private class InputData : Base_InputData
        {
            public float[] Axis_X = null;
            public float[] Axis_Y = null;
            public byte[] Trigger = null;
            public byte[] Coin = null;
            public byte[] Start = null;
            public byte Test = 0;
            public byte MenuUp = 0;
            public byte MenuDown = 0;
            public byte MenuSelect = 0;
            public byte Left = 0;
            public byte Right = 0;
            public byte Exit = 0;

            public InputData(int PlayerNumber) : base(PlayerNumber)
            { }
        }

        private class OutputData : Base_OutputData
        {
            public byte[] IsPlaying = null;
            public byte[] Recoil = null;
            public byte[] StartLamp = null;
            public byte[] GunLamp = null;
            public byte Lamp = 0;
            public int[] Credits = null;

            public OutputData(int PlayerNumber) : base(PlayerNumber)
            { }
        }

        public Game_ArcadepcTopGun2(GameConfig Config) : base(Config)
        {
            _KnownMd5Prints.Add("Top Gun 2 (War of gun) v2.0.5_20250325 - Original", "e57da49af7557076e77d5e3f89786f61");

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
            else if (e.ButtonName.Equals("MENU DOWN"))
            {
                ((InputData)_InputData).MenuDown = 1;
            }
            else if (e.ButtonName.Equals("MENU UP"))
            {
                ((InputData)_InputData).MenuUp = 1;
            }
            else if (e.ButtonName.Equals("MENU SELECT"))
            {
                ((InputData)_InputData).MenuSelect = 1;
            }
            else if (e.ButtonName.Equals("LEFT"))
            {
                ((InputData)_InputData).Left = 1;
            }
            else if (e.ButtonName.Equals("RIGHT"))
            {
                ((InputData)_InputData).Right = 1;
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
            else if (e.ButtonName.Equals("MENU DOWN"))
            {
                ((InputData)_InputData).MenuDown = 0;
            }
            else if (e.ButtonName.Equals("MENU UP"))
            {
                ((InputData)_InputData).MenuUp = 0;
            }
            else if (e.ButtonName.Equals("MENU SELECT"))
            {
                ((InputData)_InputData).MenuSelect = 0;
            }
            else if (e.ButtonName.Equals("LEFT"))
            {
                ((InputData)_InputData).Left = 0;
            }
            else if (e.ButtonName.Equals("RIGHT"))
            {
                ((InputData)_InputData).Right = 0;
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
                if (e.ButtonName.Equals("TRIGGER"))
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
                if (e.ButtonName.Equals("TRIGGER"))
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
                new SyncBlinkingGameOutput(OutputId.P1_CtmLmpStart, 500),
                new SyncBlinkingGameOutput(OutputId.P2_CtmLmpStart, 500),
                new GameOutput(OutputId.P1_LmpGun),
                new GameOutput(OutputId.P2_LmpGun),
                new GameOutput(OutputId.LmpPanel),
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

                //Handling Start Lamps based on player status
                if (((OutputData)_OutputData).StartLamp[0] == 1)
                    SetOutputValue(OutputId.P1_CtmLmpStart, -1);
                else
                    SetOutputValue(OutputId.P1_CtmLmpStart, 0);

                if (((OutputData)_OutputData).StartLamp[1] == 1)
                    SetOutputValue(OutputId.P2_CtmLmpStart, -1);
                else
                    SetOutputValue(OutputId.P2_CtmLmpStart, 0);

                SetOutputValue(OutputId.P1_LmpGun, ((OutputData)_OutputData).GunLamp[0]);
                SetOutputValue(OutputId.P2_LmpGun, ((OutputData)_OutputData).GunLamp[1]);
                SetOutputValue(OutputId.P1_CtmRecoil, ((OutputData)_OutputData).Recoil[0]);
                SetOutputValue(OutputId.P2_CtmRecoil, ((OutputData)_OutputData).Recoil[1]);

                SetOutputValue(OutputId.P1_Credits, (int)((OutputData)_OutputData).Credits[0]);
                SetOutputValue(OutputId.P2_Credits, (int)((OutputData)_OutputData).Credits[1]);
            }
        }

        #endregion
    }
}
