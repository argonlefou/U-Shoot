using System;
using System.Reflection;
using System.Threading;
using CommonBase;
using HarmonyLib;
using UnityEngine;

namespace BepInEx_UShoot_Plugin.Patch
{
    internal class mPortManager
    {
        /// <summary>
        /// Removing the call to Serial.Close as Serial is null
        /// </summary>
        [HarmonyPatch(typeof(PortManager), "ClosePort")]
        class ClosePort
        {
            static bool Prefix(PortManager __instance, ref bool ____StartThread, System.Threading.Thread ____PortThread)
            {
                UShoot_Plugin.MyLogger.LogMessage("PortManager.ClosePort()");
                ____StartThread = false;
                if (____PortThread != null)
                {
                    ____PortThread.Abort();
                }
                return false;
            }
        }

        /// <summary>
        /// Get Coins
        /// Resetting the _LinkTimeCount value (should be reset when COM packet is received) to remove Connection Error message on screen
        /// </summary>
        [HarmonyPatch(typeof(PortManager), "FixedUpdate")]
        class FixedUpdate
        {
            static bool Prefix(PortManager __instance, int[] ___countCoin, ref float ____LinkTimeCount)
            {
                ____LinkTimeCount = 0.0f;

                for (int i = 0; i < MonoSingleton<GameData>.Instance.TotalPlayers; i++)
                {
                    UShoot_Plugin.OutputData.Credits[i] = ___countCoin[i];
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(PortManager), "Inits")]
        class Inits
        {
            static void Postfix(PortManager __instance, ref bool ____StartThread, System.Threading.Thread ____PortThread)
            {
                UShoot_Plugin.MyLogger.LogMessage("PortManager.Inits()");

                foreach (MethodInfo mi in __instance.GetType().GetMethods(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance))
                {
                    
                    if (mi.Name.Equals("set_IsConnected"))
                    {
                        UShoot_Plugin.MyLogger.LogMessage("PortManager.Inits: " + mi.Name);
                        mi.Invoke(__instance, new object[] { true });
                    }
                }
            }
        }



        /// <summary>
        /// Removing Serial read
        /// Replacing by packets send according to our needs
        /// Thread is called once and running an endless loop
        /// 
        /// Buttons ID in KeyManager.UpdateJoystickButton()
        /// </summary>
        [HarmonyPatch(typeof(PortManager), "ReceiveData")]
        class ReceiveData
        {
            static bool Prefix(object obj, PortManager __instance, ref byte[] ___buf, ref bool ____StartThread, ref byte[] ____ReadBuff, ref int ____WriteID, ref int[] ___RemainNeedOutTicket)
            {
                UShoot_Plugin.MyLogger.LogMessage("PortManager.ReceiveData(): StartThread=" + ____StartThread);

                while (____StartThread)
                {
                    int num = 0;
                    UShoot_Plugin.ReceivedDataBuffer = new byte[UShoot_Plugin.INPUT_BUFFER_LENGTH];
                    UShoot_Plugin.ReceivedDataBuffer[0] = 1;  //Cmd 1 = Keys + Axis + Tickets + Credits                                 

                    //System Keys
                    if (UShoot_Plugin.GameController.GetRawButtonDown("TEST"))
                        SetKeyBit(ref UShoot_Plugin.ReceivedDataBuffer, 20);
                    if (UShoot_Plugin.GameController.GetRawButtonDown("MENU_SELECT"))
                        SetKeyBit(ref UShoot_Plugin.ReceivedDataBuffer, 21);
                    if (UShoot_Plugin.GameController.GetRawButtonDown("MENU_DOWN"))
                        SetKeyBit(ref UShoot_Plugin.ReceivedDataBuffer, 22);
                    if (UShoot_Plugin.GameController.GetRawButtonDown("MENU_UP"))
                        SetKeyBit(ref UShoot_Plugin.ReceivedDataBuffer, 23);
                    if (UShoot_Plugin.GameController.GetRawButtonDown("LEFT"))
                    {
                        SetKeyBit(ref UShoot_Plugin.ReceivedDataBuffer, 1);
                        SetKeyBit(ref UShoot_Plugin.ReceivedDataBuffer, 7);
                    }
                    if (UShoot_Plugin.GameController.GetRawButtonDown("RIGHT"))
                    {
                        SetKeyBit(ref UShoot_Plugin.ReceivedDataBuffer, 2);
                        SetKeyBit(ref UShoot_Plugin.ReceivedDataBuffer, 8);
                    }

                    //Start Keys
                    for (int i = 0; i < UShoot_Plugin.MAX_PLAYERS; i++)
                    {
                        if (UShoot_Plugin.PlayerControllers[i].GetRawButtonDown("START"))
                            SetKeyBit(ref UShoot_Plugin.ReceivedDataBuffer, 4 + i);
                    }

                    //Credits Keys
                    for (int i = 0; i < UShoot_Plugin.MAX_PLAYERS; i++)
                    {
                        if (UShoot_Plugin.PlayerControllers[i].GetRawButtonDown("COIN"))
                            UShoot_Plugin.ReceivedDataBuffer[36 + i] = 1;
                    }

                  
                    //Players Buttons & Axis
                    //000 -> FFF ==> PortManager.GetAxis() between +1.0 and -1.0 
                    for (int i = 0; i <UShoot_Plugin.MAX_PLAYERS; i++)
                    {
                        Vector3 ScreenPos = UShoot_Plugin.PlayerControllers[i].GetAimingPosition();
                        float fx = ScreenPos.x / (float)Screen.width * 4095.0f;
                        float fy = ScreenPos.y / (float)Screen.height * 4095.0f;
                        byte[] bArray = BitConverter.GetBytes((UInt16)fx);
                        Array.Reverse(bArray);
                        Array.Copy(bArray, 0, UShoot_Plugin.ReceivedDataBuffer, 4 * i + 4, 2);
                        bArray = BitConverter.GetBytes((UInt16)fy);
                        Array.Reverse(bArray);
                        Array.Copy(bArray, 0, UShoot_Plugin.ReceivedDataBuffer, 4 * i + 6, 2);

                        if (UShoot_Plugin.PlayerControllers[i].GetButton("TRIGGER"))
                            SetKeyBit(ref UShoot_Plugin.ReceivedDataBuffer, i * 6);
                        if (UShoot_Plugin.PlayerControllers[i].GetButton("CHANGE_WEAPON"))
                            SetKeyBit(ref UShoot_Plugin.ReceivedDataBuffer, i * 9);
                    }

                    //Tickets response to command
                    for (int i = 0; i < UShoot_Plugin.MAX_PLAYERS; i++)
                    {
                        if (UShoot_Plugin.RemainingTicketsOut[i] > 0)
                        {
                            byte[] bArray = BitConverter.GetBytes(UShoot_Plugin.RemainingTicketsOut[i]);
                            Array.Reverse(bArray);
                            Array.Copy(bArray, 0, UShoot_Plugin.ReceivedDataBuffer, 4 * i + 20, 4);
                            UShoot_Plugin.RemainingTicketsOut[i] = 0;
                        }
                    }

                    //Validating the buffer only if new data in it
                    for (int j = 0; j < UShoot_Plugin.ReceivedDataBuffer.Length; j++)
                    {
                        if (UShoot_Plugin.ReceivedDataBuffer[j] != UShoot_Plugin.ReceivedDataBuffer_Before[j])
                        {
                            //DemulShooter_Plugin.MyLogger.LogMessage("PortManager.ReceiveData() : " + DemulShooter_Plugin.ByteArrayToString(DemulShooter_Plugin.ReceivedDataBuffer));
                            CmdInfo command = new CmdInfo(1, UShoot_Plugin.ReceivedDataBuffer);
                            ___buf = command.ToArray();
                            num = ___buf.Length;

                            if (___buf != null)
                            {
                                for (int i = 0; i < num; i++)
                                {
                                    ____ReadBuff[____WriteID] = ___buf[i];
                                    ____WriteID = (____WriteID + 1) % ____ReadBuff.Length;
                                }
                            }
                            Array.Copy(UShoot_Plugin.ReceivedDataBuffer, UShoot_Plugin.ReceivedDataBuffer_Before, UShoot_Plugin.ReceivedDataBuffer.Length);
                            break;
                        }
                    }
                                      

                    Thread.Sleep(10);
                }
                return false;
            }
        }
        private static void SetKeyBit(ref byte[] Buffer, int KeyId)
        {
            int BufferByte = KeyId / 8 + 1;
            int ByteMask = KeyId % 8;
            Buffer[BufferByte] |= (byte)(1 << ByteMask);
        }

        /// <summary>
        /// Remove Serail port writing
        /// </summary>
        [HarmonyPatch(typeof(PortManager), "SendData", new Type[] { typeof(byte[]) })]
        class SendData
        {
            static bool Prefix(PortManager __instance, byte[] data)
            {
                //DemulShooter_Plugin.MyLogger.LogMessage("PortManager.SendData(): length=" + data.Length + " -> " + DemulShooter_Plugin.ByteArrayToString(data));
                return false;
            }
        }



        /// <summary>
        /// Intercepting Outputs. Id are:
        /// 11 : Quickly blink when a coin is added (P1 or P2)
        /// 17 :
        /// 18 : 
        /// </summary>
        [HarmonyPatch(typeof(PortManager), "SendOpenClosePower")]
        class SendOpenClosePower
        {
            static bool Prefix(PortManager __instance, int id, bool isOpen)
            {
                //DemulShooter_Plugin.MyLogger.LogMessage("PortManager.SendOpenClosePower: id=" + id + " ,isOpen=" + isOpen);
                if (id == 11)
                    UShoot_Plugin.OutputData.Lamp = isOpen ? (byte)1 : (byte)0;
                else if (id == 17)
                    UShoot_Plugin.OutputData.GunLamp[0] = isOpen ? (byte)1 : (byte)0;
                else if (id == 18)
                    UShoot_Plugin.OutputData.GunLamp[1] = isOpen ? (byte)1 : (byte)0; 

                /*if (id >= 0 && id < 4)
                    DemulShooter_Plugin.OutputData.BallMotor[id] = isOpen ? (byte)1 : (byte)0;
                else if (id >= 10 && id < 14)
                    DemulShooter_Plugin.OutputData.PlayerBonusWeaponLamp[id - 10] = isOpen ? (byte)1 : (byte)0;
                else if (id == 14)
                    DemulShooter_Plugin.OutputData.SeatVibrationLamp = isOpen ? (byte)1 : (byte)0;
                else if (id == 16)
                    DemulShooter_Plugin.OutputData.BonusWeaponLamp = isOpen ? (byte)1 : (byte)0;
                else */
                return true;
            }
        }

        /// <summary>
        /// Storing request to print Tickets
        /// </summary>
        [HarmonyPatch(typeof(PortManager), "SendOut")]
        class SendOut
        {
            static bool Prefix(PortManager __instance, int id, int num, bool keyout = false)
            {
                //DemulShooter_Plugin.MyLogger.LogMessage("PortManager.SendOut: id=" + id + " ,num=" + num + ", keyout=" + keyout);
                UShoot_Plugin.RemainingTicketsOut[id] += num;
                return true;
            }
        }

        /// <summary>
        /// Intercepting Blinkin Outputs, mapping it to START Lamp
        /// if On=2 and Off=2 --> State blinking
        /// if On=10 and Off=0 --> Off (?)
        /// </summary>
        [HarmonyPatch(typeof(PortManager), "SendPowerSpark")]
        class SendPowerSpark
        {
            static bool Prefix(PortManager __instance, int id, byte on, byte off)
            {
                //DemulShooter_Plugin.MyLogger.LogMessage("PortManager.SendPowerSpark: id=" + id + " ,on=" + on + ", off=" + off);
                if (id == 0) 
                    UShoot_Plugin.OutputData.StartLamp[0] = (on == 2 && off == 2) ? (byte)1 : (byte)0;
                else if (id ==2)
                    UShoot_Plugin.OutputData.StartLamp[1] = (on == 2 && off == 2) ? (byte)1 : (byte)0;

                return true;
            }
        }

    }
}
