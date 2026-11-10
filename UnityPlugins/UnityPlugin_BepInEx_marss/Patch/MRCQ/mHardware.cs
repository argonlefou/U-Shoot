using HarmonyLib;
using MRCQ;
using System;
using UnityEngine;

namespace BepInEx_UShoot_Plugin.Patch
{
    class mHardware
    {
        /// <summary>
        /// axisName is "x01" ... "x06" and "y01" ... "y06"
        /// X value is 0.0f (left) to 1.0f (right)
        /// Y value is 0.0f (Bottom) to 1.0f (top)
        /// Value range is [0.0; 1.0]
        /// </summary>
        [HarmonyPatch(typeof(Hardware), "GetInputAxis", new Type[] { typeof(string) })]
        class GetInputAxis
        {
            static void Postfix(string axisName, ref float __result)
            {
                switch (axisName)
                {
                    case "x01":
                        __result = UShoot_Plugin.PlayerControllers[0].Axis_X / Screen.width; break;
                    case "x02":
                        __result = UShoot_Plugin.PlayerControllers[1].Axis_X / Screen.width; break;
                    case "x03":
                        __result = UShoot_Plugin.PlayerControllers[2].Axis_X / Screen.width; break;
                    case "x04":
                        __result = UShoot_Plugin.PlayerControllers[3].Axis_X / Screen.width; break;

                    case "y01":
                        __result = UShoot_Plugin.PlayerControllers[0].Axis_Y / Screen.height; break;
                    case "y02":
                        __result = UShoot_Plugin.PlayerControllers[1].Axis_Y / Screen.height; break;
                    case "y03":
                        __result = UShoot_Plugin.PlayerControllers[2].Axis_Y / Screen.height; break;
                    case "y04":
                        __result = UShoot_Plugin.PlayerControllers[3].Axis_Y / Screen.height; break;

                    default:
                        __result = 0.0f;
                        break;
                }

                //UShoot_Plugin.MyLogger.LogWarning("Hardware.GetInputAxis(" + axisName + ") = " + __result);
            }
        }


        /// <summary>
        /// Update buttons with our own values by sending signals
        /// </summary>
        [HarmonyPatch(typeof(Hardware), "Update")]
        class Update
        {
            static bool Prefix()
            {
                for (int i = 0; i < 4; i++)
                {
                    //Trigger input
                    bool flag = false;
                    if (UShoot_Plugin.PlayerControllers[i].GetButton("TRIGGER"))
                        flag = true;

                    MRCQ.Hardware.SetInputKey(i, "startGameKey", flag);
                    MRCQ.Hardware.SetInputKey(i, "fire", flag);

                    //Switch Weapon
                    flag = false;
                    if (UShoot_Plugin.PlayerControllers[i].GetButton("CHANGE_WEAPON"))
                        flag = true;
                    MRCQ.Hardware.SetInputKey("switchWeapon0" + ((i + 1).ToString()), flag);
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(Hardware), "SetOutputKey", new Type[] { typeof(string), typeof(bool) })]
        class SetOutputKey
        {
            static void Prefix(string keyName, bool keyStatus)
            {
                //MarsSortie_BepInEx_Plugin.MyLogger.LogMessage("MRCQ.Hardware.SetOutputKey(): keyName=" + keyName + " ,keyS=" + keyStatus);
                switch (keyName)
                {
                    case "flashlight": UShoot_Plugin.OutputData.Flashlight = keyStatus ? (byte)1 : (byte)0; break;
                    case "start01": UShoot_Plugin.OutputData.IsPlaying[0] = keyStatus ? (byte)1 : (byte)0; break;
                    case "start02": UShoot_Plugin.OutputData.IsPlaying[1] = keyStatus ? (byte)1 : (byte)0; break;
                    case "start03": UShoot_Plugin.OutputData.IsPlaying[2] = keyStatus ? (byte)1 : (byte)0; break;
                    case "start04": UShoot_Plugin.OutputData.IsPlaying[3] = keyStatus ? (byte)1 : (byte)0; break;
                    case "fire01": UShoot_Plugin.OutputData.Recoil[0] = keyStatus ? (byte)1 : (byte)0; break;
                    case "fire02": UShoot_Plugin.OutputData.Recoil[1] = keyStatus ? (byte)1 : (byte)0; break;
                    case "fire03": UShoot_Plugin.OutputData.Recoil[2] = keyStatus ? (byte)1 : (byte)0; break;
                    case "fire04": UShoot_Plugin.OutputData.Recoil[3] = keyStatus ? (byte)1 : (byte)0; break;
                    default: break;
                }
            }
        }
    }
}
