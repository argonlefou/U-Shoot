using HarmonyLib;
using System;
using System.Collections.Generic;

namespace BepInEx_UShoot_Plugin.Patch
{
    internal class mLSerialPort
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
        /// Only used by BaseCom.SendGunSignal()
        /// ??? Purpose ???
        /// </summary>
        [HarmonyPatch(typeof(LSerialPort), "DiretSend")]
        class DiretSend
        {
            static bool Prefix(byte[] buf)
            {

                if (buf[1] == 0xD6 && buf.Length > 7)    //SendTestGun()
                {
                    for (int i = 0; i < UShoot_Plugin.MAX_PLAYERS; i++)
                    {
                        UShoot_Plugin.OutputData.Recoil[i] = buf[2 + i];
                        UShoot_Plugin.MyLogger.LogWarning(Environment.StackTrace);
                    }
                }
                else if (buf[1] == 0xF8)    //ChangeWorkMode()
                {
                    //
                }
                else
                {
                    UShoot_Plugin.MyLogger.LogMessage("mLSerialPort.DirectSend(byte[]) => data_buf = " + ByteArrayToString(buf));
                    UShoot_Plugin.MyLogger.LogMessage(Environment.StackTrace);
                }
                
                return false;
            }
        }

        [HarmonyPatch(typeof(LSerialPort), "SendData", new[] { typeof(byte[]), typeof(int) })]
        class SendData
        {
            static bool Prefix(List<Byte[]> ___m_recv_pack, byte[] data_buf, int send_count = 1)
            {
                byte[] destinationArray = new byte[8];
                destinationArray[0] = byte.MaxValue;
                Array.Copy((Array)data_buf, 0, (Array)destinationArray, 1, data_buf.Length);
                
                if (destinationArray[1] == 0x5A) //CheckIO Board
                {
                    ___m_recv_pack.Add(data_buf);   //reply with same packet
                }
                else
                {
                    //DemulShooter_Plugin.MyLogger.LogMessage(Environment.StackTrace);
                    if (destinationArray[1] == 0xF9)    //SendOpen()
                    {
                        //for (int i = 0; i < DemulShooter_Plugin.MAX_PLAYERS; i++)
                        //{
                        //    UShoot_Plugin.OutputData.GunOpened[i] = destinationArray[2 + i];
                        //}
                    }
                    else if (destinationArray[1] == 0xD1)    //Damaged rumble ? KillLife() -> ShowBLood() -> SendGunMoto
                    {
                        for (int i = 0; i < UShoot_Plugin.MAX_PLAYERS; i++)
                        {
                            UShoot_Plugin.OutputData.Damaged[i] = destinationArray[2 + i];
                        }
                    }
                    else if (destinationArray[1] == 0xD6)    //SendTestGun()
                    {
                        for (int i = 0; i < UShoot_Plugin.MAX_PLAYERS; i++)
                        {
                            UShoot_Plugin.OutputData.Recoil[i] = destinationArray[2 + i];
                        }
                    }
                    else if (destinationArray[1] == 0xD9)    //SendState()
                    {
                        //for (int i = 0; i < DemulShooter_Plugin.MAX_PLAYERS; i++)
                        //{
                        //    UShoot_Plugin.OutputData.GunState[i] = destinationArray[2 + i];
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
                        //destinationArray[2] = Player ID [1-4]
                        //destinationArray[6] = Tickets Number
                    }
                    else
                    {
                        UShoot_Plugin.MyLogger.LogMessage("mLSerialPort.SendData(byte[]) => data_buf = " + ByteArrayToString(destinationArray));
                        UShoot_Plugin.MyLogger.LogMessage(Environment.StackTrace);
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
                //UShoot_Plugin.MyLogger.LogMessage("data_buf = " + ByteArrayToString(destinationArray));

                /*this.frame_ready.WaitOne();
                this.frame_ready.Reset();
                for (int index = 0; index < send_count; ++index)
                    this.m_send_pack.Add(destinationArray);*/

                return false;
            }
        }
    }
}
