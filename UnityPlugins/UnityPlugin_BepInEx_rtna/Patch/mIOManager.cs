using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace BepInEx_UShoot_Plugin
{
    class mIOManager
    {
        /// <summary>
        /// Force not using IO board
        /// </summary>
        [HarmonyPatch(typeof(IOManager), "set_m_UseRio")]
        class set_m_UseRio
        {
            static bool Prefix(ref bool value)
            {
                UShoot_Plugin.MyLogger.LogMessage("IOManager.set_m_UseRio() : value=" + value);
                value = false;
                return false;
            }
        }

        
        /// <summary>
        /// Update buttons based on Keyboard / Custom input data
        /// That function is called when m_UseRio is set to false
        /// </summary>
        [HarmonyPatch(typeof(IOManager), "UpdateInput_Unity")]
        class UpdateInput_Unity
        {
            static bool Prefix(DigitalInputData[] ___m_DigitalInputData, IOManager __instance)
            {
                //DemulShooter_Plugin.MyLogger.LogMessage("IOManager.ButtonNewlyPressed() : inputIn=" + inputIn.ToString());
                for (int i = 0; i < ___m_DigitalInputData.Length; i++)
                {
                    ___m_DigitalInputData[i].switchCntLast = ___m_DigitalInputData[i].switchCnt;

                    uint iKeyPressed = 0;
                    switch (i)
                    {
                        case (int)DigitalGameInput.Coin1: iKeyPressed = UShoot_Plugin.PlayerControllers[0].GetButton("COIN") ? (uint)1 : (uint)0; break;
                        case (int)DigitalGameInput.Coin2: iKeyPressed = UShoot_Plugin.PlayerControllers[1].GetButton("COIN") ? (uint)1 : (uint)0; break;
                        case (int)DigitalGameInput.DBV: iKeyPressed = UShoot_Plugin.GameController.GetButton("DBV") ? (uint)1 : (uint)0; break;
                        case (int)DigitalGameInput.GunShoulder1: iKeyPressed = UShoot_Plugin.PlayerControllers[0].GetButton("SHOULDER") ? (uint)1 : (uint)0; break;
                        case (int)DigitalGameInput.GunShoulder2: iKeyPressed = UShoot_Plugin.PlayerControllers[1].GetButton("SHOULDER") ? (uint)1 : (uint)0; break;
                        case (int)DigitalGameInput.GunTrigger1: iKeyPressed = UShoot_Plugin.PlayerControllers[0].GetButton("TRIGGER") ? (uint)1 : (uint)0; break;
                        case (int)DigitalGameInput.GunTrigger2: iKeyPressed = UShoot_Plugin.PlayerControllers[1].GetButton("TRIGGER") ? (uint)1 : (uint)0; break;
                        case (int)DigitalGameInput.Service: iKeyPressed = UShoot_Plugin.GameController.GetButton("SERVICE") ? (uint)1 : (uint)0; break;
                        case (int)DigitalGameInput.Start1: iKeyPressed = UShoot_Plugin.PlayerControllers[0].GetButton("START") ? (uint)1 : (uint)0; break;
                        case (int)DigitalGameInput.Start2: iKeyPressed = UShoot_Plugin.PlayerControllers[1].GetButton("START") ? (uint)1 : (uint)0; break;
                        case (int)DigitalGameInput.Test: iKeyPressed = UShoot_Plugin.GameController.GetButton("TEST") ? (uint)1 : (uint)0; break;
                        case (int)DigitalGameInput.VolumeDown: iKeyPressed = UShoot_Plugin.GameController.GetButton("VOLUME_DOWN") ? (uint)1 : (uint)0; break;
                        case (int)DigitalGameInput.VolumeUp: iKeyPressed = UShoot_Plugin.GameController.GetButton("VOLUME_UP") ? (uint)1 : (uint)0; break;
                        default: break;
                    }
                    ___m_DigitalInputData[i].switchCnt = iKeyPressed;

                    if (___m_DigitalInputData[i].switchCntLast == ___m_DigitalInputData[i].switchCnt)
                    {
                        ___m_DigitalInputData[i].time += Time.deltaTime;
                    }
                    else
                    {
                        ___m_DigitalInputData[i].time = 0f;
                    }
                }

                MethodInfo mi = __instance.GetType().GetMethod("SetAnalogValue", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (mi != null)
                {
                    mi.Invoke(__instance, new object[] { AnalogGameInput.Gun1X, UShoot_Plugin.PlayerControllers[0].GetAimingPosition().x / (float)Screen.width });
                    mi.Invoke(__instance, new object[] { AnalogGameInput.Gun1Y, UShoot_Plugin.PlayerControllers[0].GetAimingPosition().y / (float)Screen.height });
                    mi.Invoke(__instance, new object[] { AnalogGameInput.Gun2X, UShoot_Plugin.PlayerControllers[1].GetAimingPosition().x / (float)Screen.width });
                    mi.Invoke(__instance, new object[] { AnalogGameInput.Gun2Y, UShoot_Plugin.PlayerControllers[1].GetAimingPosition().y / (float)Screen.height });
                }
               
                return false;                
            }
        }
    }
}
