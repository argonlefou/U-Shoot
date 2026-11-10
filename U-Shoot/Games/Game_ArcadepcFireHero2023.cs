using System;
using System.Collections.Generic;
using UsCore.Config;
using UsCore.IPC;
using UsCore.MameOutput;
using UsCore.VirtualControllers;

namespace U_Shoot.Games
{
    public class Game_ArcadepcFireHero2023 : BaseGameUnity64
    {
        protected override bool PATCH_PEB_DEBUG_FLAG { get { return false; } }

        private class InputData : Base_InputData
        {
            public float[] Axis_X = null;
            public float[] Axis_Y = null;
            public byte[] Start = null;
            public byte[] Trigger = null;
            public byte[] NextWeapon = null;
            public byte[] Coin = null;
            public byte Test = 0;
            public byte MenuLeft = 0;
            public byte MenuRight = 0;
            public byte Exit = 0;

            public InputData(int PlayerNumber) : base(PlayerNumber)
            { }
        }

        private class OutputData : Base_OutputData
        {
            public byte[] IsPlaying = null;
            public byte[] ChairMotor = null;
            public byte[] GunMotor = null;
            public byte[] SmallGun = null;
            public byte[] BigGun = null;
            public byte[] StartLight = null;
            public int[] Credits = null;
            public byte WaterPump = 0;
            public byte Atomizer = 0;
            public int BoxLight = 0;
            public int LightBelt1 = 0;
            public int LightBelt2 = 0;
            public int LightBelt3 = 0;
            public int LightBelt4 = 0;

            public OutputData(int PlayerNumber) : base(PlayerNumber)
            { }
        }

        public Game_ArcadepcFireHero2023(GameConfig Config) : base(Config)
        {
            _KnownMd5Prints.Add("Fire Hero v1.6 - Heavily patched", "37ee68239821e5b825eac62b16455905");

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
                else if (e.ButtonName.Equals("NEXT WEAPON"))
                {
                    ((InputData)_InputData).NextWeapon[PlayerIndex] = 1;
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
                else if (e.ButtonName.Equals("NEXT WEAPON"))
                {
                    ((InputData)_InputData).NextWeapon[PlayerIndex] = 0;
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
                new GameOutput(OutputId.WaterPump),
                new GameOutput(OutputId.P1_WaterFire),
                new GameOutput(OutputId.P2_WaterFire),
                new GameOutput(OutputId.P1_BigGun),
                new GameOutput(OutputId.P2_BigGun),
                new GameOutput(OutputId.P1_GunMotor),
                new GameOutput(OutputId.P2_GunMotor),
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
                for (uint i = 0; i < _GameConfig.PlayerCount; i++)
                {
                    SetOutputValue(OutputId.P1_LmpStart + i, GetBlinkingLightState(((OutputData)_OutputData).StartLight[i]));
                    SetOutputValue(OutputId.P1_WaterFire + i, ((OutputData)_OutputData).SmallGun[i]);
                    SetOutputValue(OutputId.P1_BigGun + i, ((OutputData)_OutputData).BigGun[i]);
                    SetOutputValue(OutputId.P1_Credits + i, ((OutputData)_OutputData).Credits[i]);

                    if (((OutputData)_OutputData).GunMotor[i] > 0)
                        SetOutputValue(OutputId.P1_GunMotor + i, 1);
                    else
                        SetOutputValue(OutputId.P1_GunMotor + i, 0);
                }
                SetOutputValue(OutputId.WaterPump, ((OutputData)_OutputData).WaterPump);
            }
        }

        //Game sets values as this :
        //0 = OFF
        //1 = CONSTANT ON
        //2 = CONSTANT OFF
        //We need -1 instead of 2 for custom blinking output state
        private int GetBlinkingLightState(int ReadValue)
        {
            if (ReadValue == 2)
                return -1;
            else
                return ReadValue;
        }


        #endregion
    }
}
