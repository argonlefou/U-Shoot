using System;
using System.Collections.Generic;
using UsCore.Config;
using UsCore.IPC;
using UsCore.MameOutput;
using UsCore.VirtualControllers;

namespace U_Shoot.Games
{
    public class Game_ArcadepcBullseye : BaseGameUnity64
    {
        protected override bool PATCH_PEB_DEBUG_FLAG { get { return false; } }

        private class InputData : Base_InputData
        {
            public float[] Axis_X = null;
            public float[] Axis_Y = null;
            public byte[] Start = null;
            public byte[] Trigger = null;
            public byte Test = 0;
            public byte Service = 0;
            public byte Coin = 0;
            public byte Left = 0;
            public byte Right = 0;
            public byte Exit = 0;

            public InputData(int PlayerNumber) : base(PlayerNumber)
            { }
        }

        private class OutputData : Base_OutputData
        {
            public byte IsPlaying = 0;
            public byte Recoil = 0;
            public byte LED_LeftButton = 0;
            public byte LED_RightButton = 0;
            public byte LED_StartButton = 0;
            public byte LED_RedLight = 0;
            public byte LED_Screen = 0;
            public byte Damaged = 0;
            public int Ammo = 0;
            public int Credits = 0;

            public OutputData(int PlayerNumber) : base(PlayerNumber)
            { }
        }

        public Game_ArcadepcBullseye(GameConfig Config) : base(Config)
        {
            _KnownMd5Prints.Add("Bullseye Crackshot v4.14.40417 - Original", "F5DC92EB3412CBE3D93E66D840D14B3F");

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
            else if (e.ButtonName.Equals("SERVICE"))
            {
                ((InputData)_InputData).Service = 1;
            }
            else if (e.ButtonName.Equals("COIN"))
            {
                ((InputData)_InputData).Coin = 1;
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
            else if (e.ButtonName.Equals("SERVICE"))
            {
                ((InputData)_InputData).Service = 0;
            }
            else if (e.ButtonName.Equals("COIN"))
            {
                ((InputData)_InputData).Coin = 0;
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

                if (e.ButtonName.Equals("TRIGGER"))
                {
                    ((InputData)_InputData).Trigger[PlayerIndex] = 1;
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

                if (e.ButtonName.Equals("TRIGGER"))
                {
                    ((InputData)_InputData).Trigger[PlayerIndex] = 0;
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
                new GameOutput(OutputId.LmpLeft),
                new GameOutput(OutputId.LmpRight),
                new GameOutput(OutputId.Lmp_RedLight),
                new GameOutput(OutputId.LmpBillboard),
                new AsyncGameOutput(OutputId.P1_CtmRecoil, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_ON_DELAY), ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_OFF_DELAY), 0),
                new GameOutput(OutputId.P1_Credits)
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

                SetOutputValue(OutputId.P1_LmpStart, ((OutputData)_OutputData).LED_StartButton);
                SetOutputValue(OutputId.LmpLeft, ((OutputData)_OutputData).LED_LeftButton);
                SetOutputValue(OutputId.LmpRight, ((OutputData)_OutputData).LED_RightButton);
                SetOutputValue(OutputId.Lmp_RedLight, ((OutputData)_OutputData).LED_RedLight);
                SetOutputValue(OutputId.LmpBillboard, ((OutputData)_OutputData).LED_Screen);
                SetOutputValue(OutputId.P1_CtmRecoil, ((OutputData)_OutputData).Recoil);
                SetOutputValue(OutputId.P1_Ammo, (int)((OutputData)_OutputData).Ammo);
                SetOutputValue(OutputId.P1_Credits, (int)((OutputData)_OutputData).Credits);
            }
        }

        #endregion
    }
}
