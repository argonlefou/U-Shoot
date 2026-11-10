using System;
using System.Collections.Generic;
using UsCore.Config;
using UsCore.IPC;
using UsCore.MameOutput;
using UsCore.VirtualControllers;

namespace U_Shoot.Games
{
    public class Game_ArcadepcFireHero : BaseGameUnity32
    {
        protected override bool PATCH_PEB_DEBUG_FLAG { get { return false; } }

        private class InputData : Base_InputData
        {
            public float[] Axis_X = null;
            public float[] Axis_Y = null;
            public byte[] Coin = null;
            public byte[] Start = null;
            public byte Test = 0;
            public byte MenuDown = 0;
            public byte MenuLeft = 0;
            public byte MenuRight = 0;
            public byte Exit = 0;

            public InputData(int PlayerNumber) : base(PlayerNumber)
            { }
        }

        private class OutputData : Base_OutputData
        {
            public byte[] IsPlaying = null;
            public byte[] StartLed = null;
            public byte Leftflash = 0;
            public byte Rightflash = 0;
            public byte ConsoleLight = 0;
            public byte FireLight = 0;
            public byte Fog = 0;
            public byte WaterPump = 0;
            public byte[] GunShake = null;
            public byte[] SmallGun = null;
            public byte[] BigGun = null;
            public byte[] WaterRotate = null;
            public byte[] Ticket = null;
            public float[] Life = null;
            public byte[] Damaged = null;
            public int[] Credits = null;

            public OutputData(int PlayerNumber) : base(PlayerNumber)
            { }
        }

        public Game_ArcadepcFireHero(GameConfig Config) : base(Config)
        {
            _KnownMd5Prints.Add("Fire Hero v1.4.6", "a1b853ec0d1a597afc41ac7a448166f3");

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
            else if (e.ButtonName.Equals("MENU DOWN"))
            {
                ((InputData)_InputData).MenuDown = 1;
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
            else if (e.ButtonName.Equals("MENU DOWN"))
            {
                ((InputData)_InputData).MenuDown = 0;
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
                new GameOutput(OutputId.P1_LmpStart),
                new GameOutput(OutputId.P2_LmpStart),
                new GameOutput(OutputId.LmpLeft),
                new GameOutput(OutputId.LmpRight),
                new GameOutput(OutputId.Lmp_RedLight),
                new GameOutput(OutputId.LmpPanel),
                new GameOutput(OutputId.WaterPump),
                new GameOutput(OutputId.P1_WaterFire),
                new GameOutput(OutputId.P2_WaterFire),
                new GameOutput(OutputId.P1_BigGun),
                new GameOutput(OutputId.P2_BigGun),
                new GameOutput(OutputId.P1_WaterRotate),
                new GameOutput(OutputId.P2_WaterRotate),
                new GameOutput(OutputId.SmokeSwitch),
                new GameOutput(OutputId.P1_GunMotor),
                new GameOutput(OutputId.P2_GunMotor),
                new GameOutput(OutputId.P1_Life),
                new GameOutput(OutputId.P2_Life),
                new AsyncGameOutput(OutputId.P1_Damaged, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_DAMAGED_DELAY), 100, 0),
                new AsyncGameOutput(OutputId.P2_Damaged, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_DAMAGED_DELAY), 100, 0),
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

                SetOutputValue(OutputId.P1_LmpStart, ((OutputData)_OutputData).StartLed[0]);
                SetOutputValue(OutputId.P2_LmpStart, ((OutputData)_OutputData).StartLed[1]);
                SetOutputValue(OutputId.LmpLeft, ((OutputData)_OutputData).Leftflash);
                SetOutputValue(OutputId.LmpRight, ((OutputData)_OutputData).Rightflash);
                SetOutputValue(OutputId.Lmp_RedLight, ((OutputData)_OutputData).FireLight);
                SetOutputValue(OutputId.LmpPanel, ((OutputData)_OutputData).ConsoleLight);
                SetOutputValue(OutputId.WaterPump, ((OutputData)_OutputData).WaterPump);
                SetOutputValue(OutputId.P1_WaterFire, ((OutputData)_OutputData).SmallGun[0]);
                SetOutputValue(OutputId.P2_WaterFire, ((OutputData)_OutputData).SmallGun[1]);
                SetOutputValue(OutputId.P1_BigGun, ((OutputData)_OutputData).BigGun[0]);
                SetOutputValue(OutputId.P2_BigGun, ((OutputData)_OutputData).BigGun[1]);
                SetOutputValue(OutputId.P1_WaterRotate, ((OutputData)_OutputData).WaterRotate[0]);
                SetOutputValue(OutputId.P2_WaterRotate, ((OutputData)_OutputData).WaterRotate[1]);
                SetOutputValue(OutputId.SmokeSwitch, ((OutputData)_OutputData).Fog);
                SetOutputValue(OutputId.P1_GunMotor, ((OutputData)_OutputData).GunShake[0]);
                SetOutputValue(OutputId.P2_GunMotor, ((OutputData)_OutputData).GunShake[1]);
                SetOutputValue(OutputId.P1_Life, (int)((OutputData)_OutputData).Life[0]);
                SetOutputValue(OutputId.P2_Life, (int)((OutputData)_OutputData).Life[1]);
                SetOutputValue(OutputId.P1_Damaged, ((OutputData)_OutputData).Damaged[0]);
                SetOutputValue(OutputId.P2_Damaged, ((OutputData)_OutputData).Damaged[1]);
                SetOutputValue(OutputId.P1_Credits, ((OutputData)_OutputData).Credits[0]);
                SetOutputValue(OutputId.P2_Credits, ((OutputData)_OutputData).Credits[1]);
            }
        }

        #endregion
    }
}
