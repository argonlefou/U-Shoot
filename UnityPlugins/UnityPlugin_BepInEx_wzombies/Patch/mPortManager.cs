using HarmonyLib;
using System.IO;
using System.Text;
using LitJson;
using CommonBase;
using System.Reflection;
using UnityEngine;
using System.Collections.Generic;
using System;

namespace BepInEx_UShoot_Plugin.Patch
{
    class mPortManager
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
        /// Remove Serial port oppening
        /// </summary>
        [HarmonyPatch(typeof(PortManager), "Start")]
        class Start
        {
            static bool Prefix(PortManager __instance)
            {
                UShoot_Plugin.MyLogger.LogMessage("PortManager.Start()");
                string json = File.ReadAllText("Game.json", Encoding.UTF8);
                JsonData jsonData = JsonMapper.ToObject(json);

                MonoSingleton<GameData>.Instance.PowerNote = jsonData["Power"];
                MonoSingleton<GameData>.Instance.P4GunAngle = jsonData["P4GunAngle"];
                MonoSingleton<GameData>.Instance.P3GunAngle = jsonData["P3GunAngle"];
                MonoSingleton<GameData>.Instance.P2GunAngle = jsonData["P2GunAngle"];
                MonoSingleton<GameData>.Instance.P2GunAngle = jsonData["P2GunAngle"];
                MonoSingleton<GameData>.Instance.VidioShow = (int)jsonData["VidioShow"];
                MonoSingleton<GameData>.Instance.Version_code = (string)jsonData["version_code"];
                MonoSingleton<GameData>.Instance.VersionNo = (string)jsonData["update_msg"];

                //__instance.StartCheckIOLink();
                foreach (MethodInfo mi in __instance.GetType().GetMethods(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance))
                {
                    if (mi.Name.Equals("StartCheckIOLink"))
                    {
                        mi.Invoke(__instance, null);
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Removing Serial read
        /// Replacing by packets send according to our needs
        /// Thread is called once per Frame by the FixedUpdate() function
        /// </summary>
        [HarmonyPatch(typeof(PortManager), "ReceiveData")]
        class ReceiveData
        {
            static bool Prefix(PortManager __instance, ref byte[] ___buf, ref bool ____StartThread, ref byte[] ____ReadBuff, ref int ____WriteID, ref int[] ___RemainNeedOutTicket)
            {
                //DemulShooter_Plugin.MyLogger.LogMessage("PortManager.ReceiveData()");
                int num = 0;

                byte[] Buffer = new byte[0x28];
                Buffer[0] = 1;  //Cmd 1 = Keys + Axis + Tickets + Credits                                 

                //Water level switch, must be ON
                SetKeyBit(ref Buffer, 19);

                //System Keys
                if (UShoot_Plugin.GameController.GetRawButtonDown("TEST"))
                    SetKeyBit(ref Buffer, 20);
                if (UShoot_Plugin.GameController.GetRawButtonDown("MENU_SELECT"))
                    SetKeyBit(ref Buffer, 21);
                if (UShoot_Plugin.GameController.GetRawButtonDown("MENU_DOWN"))
                    SetKeyBit(ref Buffer, 22);
                if (UShoot_Plugin.GameController.GetRawButtonDown("MENU_UP"))
                    SetKeyBit(ref Buffer, 23);

                //Start Keys
                for (int i = 0; i < UShoot_Plugin.MAX_PLAYERS; i++)
                {
                    if (UShoot_Plugin.PlayerControllers[i].GetRawButtonDown("START"))
                        SetKeyBit(ref Buffer, 4 + i);
                }

                //Credits Keys
                for (int i = 0; i < UShoot_Plugin.MAX_PLAYERS; i++)
                {
                    if (UShoot_Plugin.PlayerControllers[i].GetRawButtonDown("COIN"))
                        Buffer[36 + i] = 1;
                }

                //Players Buttons & Axis
                //000 -> FFF ==> PortManager.GetAxis() between +1.0 and -1.0 
                for (int i = 0; i < MonoSingleton<GameData>.Instance.TotalPlayer; i++)
                {
                    Vector3 ScreenPos = UShoot_Plugin.PlayerControllers[i].GetAimingPosition();
                    float fx = ScreenPos.x / (float)Screen.width * 4095.0f;
                    float fy = ScreenPos.y / (float)Screen.height * 4095.0f;
                    byte[] bArray = BitConverter.GetBytes((UInt16)fx);
                    Array.Reverse(bArray);
                    Array.Copy(bArray, 0, Buffer, 4 * i + 4, 2);
                    bArray = BitConverter.GetBytes((UInt16)fy);
                    Array.Reverse(bArray);
                    Array.Copy(bArray, 0, Buffer, 4 * i + 6, 2);
                }

                //Tickets response to command
                for (int i = 0; i < MonoSingleton<GameData>.Instance.TotalPlayer; i++)
                {
                    if (UShoot_Plugin.RemainingTicketsOut[i] > 0)
                    {
                        byte[] bArray = BitConverter.GetBytes(UShoot_Plugin.RemainingTicketsOut[i]);
                        Array.Reverse(bArray);
                        Array.Copy(bArray, 0, Buffer, 4 * i + 20, 4);

                        //Resetting the counter will activate auto-check Ticket Sensor (if no device is plugged).
                        //Otherwise, counter decrease need to be handled (sensor, keyboard key, etc...)
                        if (!UShoot_Plugin.UseTicketsFeeder)
                            UShoot_Plugin.RemainingTicketsOut[i] = 0;
                    }
                }

                CmdInfo command = new CmdInfo(1, Buffer);
                ___buf = command.ToArray();
                num = ___buf.Length;
                //DemulShooter_Plugin.MyLogger.LogMessage("PortManager.ReceiveData(): " + DemulShooter_Plugin.ByteArrayToString(___buf));  

                if (num == 0)
                {
                    ____StartThread = true;
                    return false;
                }


                if (___buf != null)
                {
                    for (int i = 0; i < num; i++)
                    {
                        ____ReadBuff[____WriteID] = ___buf[i];
                        ____WriteID = (____WriteID + 1) % ____ReadBuff.Length;
                    }
                }

                ____StartThread = true;
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
        [HarmonyPatch(typeof(PortManager), "SendData")]
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
        /// 00 - 03 : Small water
        /// 04 - 07 : Big Water
        /// 10 - 13 : GunMotor
        /// 14 : SeatVibration Motor        
        /// 16 : Weapon Bonus Light
        /// 17 : Water Level OK Light
        /// 18 : ??
        /// </summary>
        [HarmonyPatch(typeof(PortManager), "SendOpenClosePower")]
        class SendOpenClosePower
        {
            static bool Prefix(PortManager __instance, int id, bool isOpen)
            {
                UShoot_Plugin.MyLogger.LogMessage("PortManager.SendOpenClosePower: id=" + id + " ,isOpen=" + isOpen);
                if (id >= 0 && id < 4)
                    UShoot_Plugin.OutputData.SmallWater[id] = isOpen ? (byte)1 : (byte)0;
                else if (id >= 4 && id < 7)
                    UShoot_Plugin.OutputData.BigWater[id - 4] = isOpen ? (byte)1 : (byte)0;                
                else if (id >= 10 && id < 14)
                    UShoot_Plugin.OutputData.GunMotor[id - 10] = isOpen ? (byte)1 : (byte)0;
                else if (id == 14)
                    UShoot_Plugin.OutputData.SeatVibrationMotor = isOpen ? (byte)1 : (byte)0;
                else if (id == 16)
                    UShoot_Plugin.OutputData.BonusWeaponLamp = isOpen ? (byte)1 : (byte)0;
                else if (id == 17)
                    UShoot_Plugin.OutputData.WaterLevelLamp = isOpen ? (byte)1 : (byte)0;
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
                UShoot_Plugin.MyLogger.LogMessage("PortManager.SendOut: id=" + id + " ,num=" + num + ", keyout=" + keyout);
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
                UShoot_Plugin.MyLogger.LogMessage("PortManager.SendPowerSpark: id=" + id + " ,on=" + on + ", off=" + off);
                UShoot_Plugin.OutputData.StartLamp[id] = (on == 2 && off == 2) ? (byte)1 : (byte)0;
                return true;
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

                for (int i = 0; i < MonoSingleton<GameData>.Instance.TotalPlayer; i++)
                {
                    UShoot_Plugin.OutputData.Credits[i] = ___countCoin[i];
                }
                return true;
            }
        }
    }
}
