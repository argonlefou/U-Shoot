using System;
using System.Collections.Generic;
using HarmonyLib;

namespace BepInEx_UShoot_Plugin.Patch
{
    class mLSerialPort
    {
        static String ByteArrayToString(byte[] bArray)
        {
            string s = "";
            for (int i = 0; i < bArray.Length; i++)
            {
                s += bArray[i].ToString("X2") + " ";
            }
            return s;
        }

        /// <summary>
        /// Simulate COM connection OK
        /// </summary>
        [HarmonyPatch(typeof(LSerialPort), "Start")]
        class Start
        {
            static bool Prefix(bool ___m_com_opened, bool ___m_loop_thread)
            {
                UShoot_Plugin.MyLogger.LogMessage("mLSerialPort.Start()");
                ___m_loop_thread = true;
                ___m_com_opened = true;                
                return false;
            }
        }

        /// <summary>
        /// Replacing the COM port parsing by Shared Memory Parsing
        /// </summary>
        [HarmonyPatch(typeof(LSerialPort), "GetAllMsg")]
        class GetAllMsg
        {   
            static bool Prefix(ref List<byte[]> msg_list, List<Byte[]> ___m_recv_pack)
            {
                //Entering TEST mode // Moving down in Test Mode
                if (UShoot_Plugin.GameController.GetButtonDown("TEST"))
                    UShoot_Plugin.InputGunByte_Payload |= 0x10;
                else if (UShoot_Plugin.GameController.GetButtonUp("TEST"))
                    UShoot_Plugin.InputGunByte_Payload &= 0xEF;

                //Select in TEST mode
                if (UShoot_Plugin.GameController.GetButtonDown("MENU_SELECT"))
                    UShoot_Plugin.InputGunByte_Payload |= 0x80;
                else if (UShoot_Plugin.GameController.GetButtonUp("MENU_SELECT"))
                    UShoot_Plugin.InputGunByte_Payload &= 0x7F;

                //Move Up TEST mode
                /*if (Input.GetKeyDown(KeyCode.UpArrow))                
                    DemulShooter_Plugin.InputGunByte_Payload |= 0x20;
                else if (Input.GetKeyUp(KeyCode.UpArrow))
                    DemulShooter_Plugin.InputGunByte_Payload &= 0xDF;*/

                if (UShoot_Plugin.InputGunByte_Payload != UShoot_Plugin.InputGunByte_Payload_Before)
                {
                    byte[] b = new Byte[7];
                    b[0] = 0xDD;
                    b[1] = UShoot_Plugin.InputGunByte_Payload;
                    b[2] = 0;
                    b[3] = 0;
                    b[4] = 0;
                    b[5] = 0;
                    b[6] = 0;
                    ___m_recv_pack.Add(b);

                    UShoot_Plugin.InputGunByte_Payload_Before = UShoot_Plugin.InputGunByte_Payload;
                }

                //Trigger 
                bool bFlag = false;
                for (int i = 0; i < UShoot_Plugin.MAX_PLAYERS; i++)
                {
                    if (UShoot_Plugin.PlayerControllers[i].GetButtonDown("TRIGGER") || UShoot_Plugin.PlayerControllers[i].GetButtonUp("TRIGGER"))
                    {
                        bFlag = true;
                        break;
                    }
                }
                if (bFlag)
                {
                    byte[] b = new Byte[7];
                    b[0] = 0xDD;
                    for (int i = 0; i < UShoot_Plugin.MAX_PLAYERS; i++)
                    {
                        b[i + 1] = UShoot_Plugin.PlayerControllers[i].GetButton("TRIGGER") ? (byte)1 : (byte)0;
                    }
                    ___m_recv_pack.Add(b);
                }

                //COIN
                for (int i = 0; i < UShoot_Plugin.MAX_PLAYERS; i++)
                {
                    if (UShoot_Plugin.PlayerControllers[i].GetButtonDown("COIN"))
                    {
                        UInt32 ibuffer = 1;                         //COIN to add
                        byte[] b = new Byte[7];
                        b[0] = 0xCC;
                        b[1] = (byte)( i + 1);                     //PLAYER ID
                        b[2] = (byte)((ibuffer >> 12) & 0x0F);
                        b[3] = (byte)((ibuffer >> 8) & 0x0F);
                        b[4] = (byte)((ibuffer >> 4) & 0x0F);
                        b[5] = (byte)(ibuffer & 0x0F);
                        b[6] = 0;
                        ___m_recv_pack.Add(b);
                    }
                }                    

                //Reload command
                for (int i = 0; i < UShoot_Plugin.MAX_PLAYERS; i++)
                {
                    if (UShoot_Plugin.PlayerControllers[i].GetButtonDown("RELOAD"))
                    {
                        try
                        {
                            if (GameUIController.Instance.playerUI[i] != null)
                                GameUIController.Instance.playerUI[i].ReLoadBullets();
                        }
                        catch { }
                    }
                }

                //Putting results into BaseCom array given as parameter for further actions
                for (int index = 0; index < ___m_recv_pack.Count; ++index)
                {
                    //DemulShooter_Plugin.MyLogger.LogMessage("Received Data Packet = " + ByteArrayToString(___m_recv_pack[index]));
                    msg_list.Add(___m_recv_pack[index]);
                }
                ___m_recv_pack.Clear();

                return false;
            }
        }

        /// <summary>
        /// Only used by BaseCom.SendGunSignal()
        /// ??? Purpose ???
        /// Frame = FF F8 [P1] [P2] [P3] [P4] etc...
        /// </summary>
        [HarmonyPatch(typeof(LSerialPort), "DiretSend")]
        class DiretSend
	    {
            static bool Prefix(byte[] buf)
            {
                //DemulShooter_Plugin.MyLogger.LogMessage("mLSerialPort.DirectSend(byte[]) => data_buf = " + ByteArrayToString(buf));
                return false;
            }
	    }              

        [HarmonyPatch(typeof(LSerialPort), "SendData", new[] { typeof(byte []), typeof(int) })]
        class SendData
        {
            static bool Prefix(List<Byte[]> ___m_recv_pack, byte[] data_buf, int send_count = 1)
            {
                byte[] destinationArray = new byte[8];
                destinationArray[0] = byte.MaxValue;
                Array.Copy((Array) data_buf, 0, (Array) destinationArray, 1, data_buf.Length);
                //DemulShooter_Plugin.MyLogger.LogMessage("mLSerialPort.SendData(byte[]) => data_buf = " + ByteArrayToString(destinationArray));

                if (destinationArray[1] == 0x5A) //CheckIO Board
                {
                    ___m_recv_pack.Add(data_buf);   //reply with same packet
                }
                else
                {
                    if (destinationArray[1] == 0xF9)    //SendOpen()
                    {
                        //for (int i = 0; i < DemulShooter_Plugin.MAX_PLAYERS; i++)
                        //{
                        //    DemulShooter_Plugin.OutputData.GunOpened[i] = destinationArray[2 + i];
                        //}
                    }
                    else if (destinationArray[1] == 0xD6)    //SendTestGun() but only activate recoil on repeated shoot (trigger maintained)
                    {
                        /*for (int i = 0; i < DemulShooter_Plugin.MAX_PLAYERS; i++)
                        {
                            DemulShooter_Plugin.OutputData.Recoil[i]= destinationArray[2 + i];
                        }*/
                    }
                    else if (destinationArray[1] == 0xD9)    //SendState()
                    {
                        //for (int i = 0; i < DemulShooter_Plugin.MAX_PLAYERS; i++)
                        //{
                        //    DemulShooter_Plugin.OutputData.GunState[i] = destinationArray[2 + i];
                        //}
                    }
                    else if (destinationArray[1] == 0xBB)   //Clear Lack Ticket
                    {
                    }
                    else if (destinationArray[1] == 0xD0)   //Open Click Verify
                    {
                        //Do nothing, Just sent once at start
                    }
                    else if (destinationArray[1] == 0xEE)   //Send Tickets
                    {                        
                    }
                    
                }
                return false;
            }            
        }

        /// <summary>
        /// This one seems to be unused
        /// </summary>
        [HarmonyPatch(typeof(LSerialPort), "SendData", new[] { typeof(List<byte>), typeof(int) })]
        class SendData_1
        {
            static bool Prefix(List<byte> data_buf, int send_count = 1)
            {
                UShoot_Plugin.MyLogger.LogMessage("mLSerialPort.SendData(List<Byte>)");
                byte[] destinationArray = new byte[data_buf.Count];
                Array.Copy((Array)data_buf.ToArray(), 0, (Array)destinationArray, 0, data_buf.Count);
                //DemulShooter_Plugin.MyLogger.LogMessage("data_buf = " + ByteArrayToString(destinationArray));

                /*this.frame_ready.WaitOne();
                this.frame_ready.Reset();
                for (int index = 0; index < send_count; ++index)
                    this.m_send_pack.Add(destinationArray);*/

                return false;
            }
        }
    }
}
