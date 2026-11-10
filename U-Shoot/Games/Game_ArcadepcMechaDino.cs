using System;
using System.Collections.Generic;
using UsCore.Config;
using UsCore.IPC;
using UsCore.MameOutput;
using UsCore.VirtualControllers;

namespace U_Shoot.Games
{
    public class Game_ArcadepcMechaDino : BaseGameUnity64
    {
        private class InputData : Base_InputData
        {
            public float[] Axis_X = null;
            public float[] Axis_Y = null;
            public byte[] Coin = null;
            public byte[] Start = null;
            public byte[] Trigger = null;
            public byte Test = 0;
            public byte MenuUp = 0;
            public byte MenuDown = 0;
            public byte MenuSelect = 0;
            public byte Exit = 0;

            public InputData(int PlayerNumber) : base(PlayerNumber)
            {
            }
        }

        private class OutputData : Base_OutputData
        {
            public byte[] IsPlaying = null;
            public byte[] BallMotor = null;
            public byte[] StartLamp = null;
            public byte[] PlayerBonusWeaponLamp = null;
            public byte[] SpineMotor = null;
            public byte BonusWeaponLamp = 0;
            public byte SeatVibrationLamp = 0;
            public byte WaterFallLamp = 0;
            public byte WaterLevelLamp = 0;
            public float[] Life = null;
            public byte[] Damaged = null;
            public int[] Credits = null;

            public OutputData(int PlayerNumber) : base(PlayerNumber)
            {
            }
        }

        public Game_ArcadepcMechaDino(GameConfig Config) : base(Config)
        {
            _KnownMd5Prints.Add("Mechanical Dinosaur v6.5 - Original", "25dd74d2eeef2f61ed32ce439bcd264a");
            _KnownMd5Prints.Add("Mechanical Dinosaur v6.5 - Patched by SpiderZSoft", "bda88c575a80d789ea1c9860e088ac51");

            _InputData = new InputData(_GameConfig.PlayerCount);
            _OutputData = new OutputData(_GameConfig.PlayerCount);

            _GameLauncher.Set_CmdLine_Args(CreateDoorstopArguments(_GameConfig.RomName));
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
                new SyncBlinkingGameOutput(OutputId.P1_LmpStart, 500),
                new SyncBlinkingGameOutput(OutputId.P2_LmpStart, 500),
                new SyncBlinkingGameOutput(OutputId.P3_LmpStart, 500),
                new SyncBlinkingGameOutput(OutputId.P4_LmpStart, 500),
                new GameOutput(OutputId.P1_LmpGun),
                new GameOutput(OutputId.P2_LmpGun),
                new GameOutput(OutputId.P3_LmpGun),
                new GameOutput(OutputId.P4_LmpGun),
                new GameOutput(OutputId.P1_GunMotor),
                new GameOutput(OutputId.P2_GunMotor),
                new GameOutput(OutputId.P3_GunMotor),
                new GameOutput(OutputId.P4_GunMotor),
                new GameOutput(OutputId.P1_LmpBonusWeapon),
                new GameOutput(OutputId.P2_LmpBonusWeapon),
                new GameOutput(OutputId.P3_LmpBonusWeapon),
                new GameOutput(OutputId.P4_LmpBonusWeapon),
                /*_Outputs.Add(new GameOutput(OutputId.P1_TicketFeeder));
                _Outputs.Add(new GameOutput(OutputId.P2_TicketFeeder));
                _Outputs.Add(new GameOutput(OutputId.P3_TicketFeeder));
                _Outputs.Add(new GameOutput(OutputId.P4_TicketFeeder));*/
                new GameOutput(OutputId.BonusWeaponLamp),
                new GameOutput(OutputId.SeatVibrationLamp),
                new GameOutput(OutputId.WaterFallLamp),
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

                //Handling Start Lamps based on player status
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

                SetOutputValue(OutputId.P1_LmpGun, ((OutputData)_OutputData).BallMotor[0]);
                SetOutputValue(OutputId.P2_LmpGun, ((OutputData)_OutputData).BallMotor[1]);
                SetOutputValue(OutputId.P3_LmpGun, ((OutputData)_OutputData).BallMotor[2]);
                SetOutputValue(OutputId.P4_LmpGun, ((OutputData)_OutputData).BallMotor[3]);

                SetOutputValue(OutputId.P1_GunMotor, (int)(sbyte)((OutputData)_OutputData).SpineMotor[0]);
                SetOutputValue(OutputId.P2_GunMotor, (int)(sbyte)((OutputData)_OutputData).SpineMotor[1]);
                SetOutputValue(OutputId.P3_GunMotor, (int)(sbyte)((OutputData)_OutputData).SpineMotor[2]);
                SetOutputValue(OutputId.P4_GunMotor, (int)(sbyte)((OutputData)_OutputData).SpineMotor[3]);

                SetOutputValue(OutputId.P1_LmpBonusWeapon, ((OutputData)_OutputData).PlayerBonusWeaponLamp[0]);
                SetOutputValue(OutputId.P2_LmpBonusWeapon, ((OutputData)_OutputData).PlayerBonusWeaponLamp[1]);
                SetOutputValue(OutputId.P3_LmpBonusWeapon, ((OutputData)_OutputData).PlayerBonusWeaponLamp[2]);
                SetOutputValue(OutputId.P4_LmpBonusWeapon, ((OutputData)_OutputData).PlayerBonusWeaponLamp[3]);

                SetOutputValue(OutputId.BonusWeaponLamp, ((OutputData)_OutputData).BonusWeaponLamp);
                SetOutputValue(OutputId.SeatVibrationLamp, ((OutputData)_OutputData).SeatVibrationLamp);
                SetOutputValue(OutputId.WaterFallLamp, ((OutputData)_OutputData).WaterLevelLamp);
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
                    SetOutputValue(OutputId.P1_Life, (int)((OutputData)_OutputData).Life[3]);
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
