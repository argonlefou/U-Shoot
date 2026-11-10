using System;
using System.Collections.Generic;
using UsCore.Config;
using UsCore.IPC;
using UsCore.MameOutput;
using UsCore.VirtualControllers;

namespace U_Shoot.Games
{
    public class Game_ArcadepcNightHunter2 : BaseGameUnity64
    {
        protected override bool PATCH_PEB_DEBUG_FLAG { get { return false; } }

        private class InputData : Base_InputData
        {
            public float[] Axis_X = null;
            public float[] Axis_Y = null;
            public byte[] Start = null;
            public byte[] Trigger = null;
            public byte[] Action = null;
            public byte[] ChangeWeapon = null;
            public byte Coin = 0;
            public byte Test = 0;
            public byte WheelLeft = 0;
            public byte WheelRight = 0;
            public byte MenuDown = 0;
            public byte MenuSelect = 0;
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
            public byte[] Damaged = null;
            public int[] Life = null;
            public int[] GunType = null;
            public int Credits = 0;

            public OutputData(int PlayerNumber) : base(PlayerNumber)
            {
            }
        }

        public Game_ArcadepcNightHunter2(GameConfig Config) : base(Config)
        {
            _KnownMd5Prints.Add("Night Hunter II v2.0.6 - Clean Dump", "1d6cbc2b0ebaf0bacbcbea84aa0f4a27");
            _KnownMd5Prints.Add("Night Hunter II v2.0.6 - Dongle patched", "d223a04447e84073a178a14d50349da8");
            _KnownMd5Prints.Add("Night Hunter II v3.0.2 - Clean Dump", "a7ddc5d05f9081b2fb5086cdd807deae");

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
            else if (e.ButtonName.Equals("TEST"))
            {
                ((InputData)_InputData).Test = 1;
            }
            else if (e.ButtonName.Equals("COIN"))
            {
                ((InputData)_InputData).Coin = 1;
            }
            else if (e.ButtonName.Equals("WHEEL LEFT"))
            {
                ((InputData)_InputData).WheelLeft = 1;
            }
            else if (e.ButtonName.Equals("WHEEL RIGHT"))
            {
                ((InputData)_InputData).WheelRight = 1;
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
            else if (e.ButtonName.Equals("COIN"))
            {
                ((InputData)_InputData).Coin = 0;
            }
            else if (e.ButtonName.Equals("TEST"))
            {
                ((InputData)_InputData).Test = 0;
            }
            else if (e.ButtonName.Equals("WHEEL LEFT"))
            {
                ((InputData)_InputData).WheelLeft = 0;
            }
            else if (e.ButtonName.Equals("WHEEL RIGHT"))
            {
                ((InputData)_InputData).WheelRight = 0;
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

                if (e.ButtonName.Equals("START"))
                {
                    ((InputData)_InputData).Start[PlayerIndex] = 1;
                }
                else if (e.ButtonName.Equals("TRIGGER"))
                {
                    ((InputData)_InputData).Trigger[PlayerIndex] = 1;
                }
                else if (e.ButtonName.Equals("ACTION"))
                {
                    ((InputData)_InputData).Action[PlayerIndex] = 1;
                }
                else if (e.ButtonName.Equals("CHANGE WEAPON"))
                {
                    ((InputData)_InputData).ChangeWeapon[PlayerIndex] = 1;
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
                else if (e.ButtonName.Equals("ACTION"))
                {
                    ((InputData)_InputData).Action[PlayerIndex] = 0;
                }
                else if (e.ButtonName.Equals("CHANGE WEAPON"))
                {
                    ((InputData)_InputData).ChangeWeapon[PlayerIndex] = 0;
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
                new AsyncGameOutput(OutputId.P1_CtmRecoil, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_ON_DELAY), ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_OFF_DELAY), 0),
                new AsyncGameOutput(OutputId.P2_CtmRecoil, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_ON_DELAY), ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_RECOIL_OFF_DELAY), 0),
                new GameOutput(OutputId.P1_GunType),
                new GameOutput(OutputId.P2_GunType),
                new GameOutput(OutputId.P1_Life),
                new GameOutput(OutputId.P2_Life),
                new GameOutput(OutputId.LmpBillboard),    //Will be triggered when a super weapon is active
                new AsyncGameOutput(OutputId.P1_Damaged, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_DAMAGED_DELAY), 100, 0),
                new AsyncGameOutput(OutputId.P2_Damaged, ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_DAMAGED_DELAY), 100, 0),
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

                //GunType:
                //0 = No
                //1 = Blue
                //2 = Red
                //3 = Big Blue
                //4 = Big Red

                int SuperGunAvailable = 0;
                if (((OutputData)_OutputData).IsPlaying[0] == 1)
                {
                    SetOutputValue(OutputId.P1_LmpStart, 0);
                    SetOutputValue(OutputId.P1_CtmRecoil, ((OutputData)_OutputData).Recoil[0]);
                    SetOutputValue(OutputId.P1_Life, (int)((OutputData)_OutputData).Life[0]);
                    SetOutputValue(OutputId.P1_Damaged, ((OutputData)_OutputData).Damaged[0]);
                    SetOutputValue(OutputId.P1_GunType, ((OutputData)_OutputData).GunType[0]);
                    if (((OutputData)_OutputData).GunType[0] > 2)
                        SuperGunAvailable = 1;
                }
                else
                {
                    SetOutputValue(OutputId.P1_LmpStart, -1);
                    SetOutputValue(OutputId.P1_Ammo, 0);
                    SetOutputValue(OutputId.P1_Life, 0);
                    SetOutputValue(OutputId.P1_GunType, 0);
                }

                if (((OutputData)_OutputData).IsPlaying[1] == 1)
                {
                    SetOutputValue(OutputId.P2_LmpStart, 0);
                    SetOutputValue(OutputId.P2_CtmRecoil, ((OutputData)_OutputData).Recoil[1]);
                    SetOutputValue(OutputId.P2_Life, (int)((OutputData)_OutputData).Life[1]);
                    SetOutputValue(OutputId.P2_Damaged, ((OutputData)_OutputData).Damaged[1]);
                    SetOutputValue(OutputId.P2_GunType, ((OutputData)_OutputData).GunType[1]);
                    if (((OutputData)_OutputData).GunType[1] > 2)
                        SuperGunAvailable = 1;
                }
                else
                {
                    SetOutputValue(OutputId.P2_LmpStart, -1);
                    SetOutputValue(OutputId.P2_Ammo, 0);
                    SetOutputValue(OutputId.P2_Life, 0);
                    SetOutputValue(OutputId.P2_GunType, 0);
                }

                SetOutputValue(OutputId.LmpBillboard, SuperGunAvailable);
                SetOutputValue(OutputId.Credits, ((OutputData)_OutputData).Credits);
            }
        }

        #endregion
    }
}
