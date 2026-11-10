using HarmonyLib;
using UnityEngine;

namespace BepInEx_UShoot_Plugin
{
    class mBNUsioController
    {  
        /// <summary>
        /// Intercepting START Led conmmands to send to DemulShooter
        /// </summary>
        [HarmonyPatch(typeof(BNUsioController), "setStartLED")]
        class setStartLED
        {
            static void Postfix(int idx, bool isOn)
            {
                //DemulShooter_Plugin.MyLogger.LogMessage("mBNUsioController.setStartLED() => idx : " + idx.ToString() + ", isOn: " + isOn.ToString());
                UShoot_Plugin.OutputData.StartLED[idx - 1] = isOn == true ? (byte)1 : (byte)0;              
            }
        }

        /// <summary>
        /// Intercepting Cabinet Led conmmands to send to DemulShooter
        /// </summary>
        [HarmonyPatch(typeof(BNUsioController), "setLED")]
        class setLED
        {
            static void Postfix(int idx, bool isOn)
            {
                //DemulShooter_Plugin.MyLogger.LogMessage("mBNUsioController.setLED() => idx : " + idx.ToString() + ", isOn: " + isOn.ToString());
                UShoot_Plugin.OutputData.PlayerLED[idx - 1] = isOn == true ? (byte)1 : (byte)0;
            }
        }

        /// <summary>
        /// Recreating the update function to use custom controls instead of original keyboatd ones
        /// </summary>
        [HarmonyPatch(typeof(BNUsioController), "update")]
        class Update
        {
            static bool Prefix(BNUsioController __instance, ref bool ___isTestOn, ref bool ___isTestPrevOn, ref float ___testOnTime, ref float ___testOffTime)
            {
                if (__instance.isFatalError)
                {
                    return false;
                }

                ___isTestPrevOn = ___isTestOn;
                __instance.isServicePrevOn = __instance.isServiceOn;
                __instance.isUpPrevOn = __instance.isUpOn;
                __instance.isDownPrevOn = __instance.isDownOn;
                __instance.isEnterPrevOn = __instance.isEnterOn;
                __instance.isP1StartPrevOn = __instance.isP1StartOn;
                __instance.isP2StartPrevOn = __instance.isP2StartOn;

                if (UShoot_Plugin.GameController.GetButtonDown("TEST"))
                    ___isTestOn = !___isTestOn;

                if (UShoot_Plugin.GameController.GetButton("SERVICE"))
                    __instance.isServiceOn = true;
                else
                    __instance.isServiceOn = false;

                if (UShoot_Plugin.GameController.GetButton("MENU_UP"))
                    __instance.isUpOn = true;
                else
                    __instance.isUpOn = false;

                if (UShoot_Plugin.GameController.GetButton("MENU_DOWN"))
                    __instance.isDownOn = true;
                else
                    __instance.isDownOn = false;

                if (UShoot_Plugin.GameController.GetButton("MENU_SELECT"))
                    __instance.isEnterOn = true;
                else
                    __instance.isEnterOn = false;

                if (UShoot_Plugin.PlayerControllers[0].GetButton("START"))
                    __instance.isP1StartOn = true;
                else
                    __instance.isP1StartOn = false;

                if (UShoot_Plugin.PlayerControllers[1].GetButton("START"))
                    __instance.isP2StartOn = true;
                else
                    __instance.isP2StartOn = false;

                ErrorMessageHandler.setCoinError(UShoot_Plugin.PlayerControllers[0].GetButton("START"));
                ErrorMessageHandler.setServiceError(UShoot_Plugin.PlayerControllers[1].GetButton("START"));

                if (___isTestOn && !___isTestPrevOn)
                {
                    ___testOnTime = 0f;
                }
                else if (!___isTestOn && ___isTestPrevOn)
                {
                    ___testOffTime = 0f;
                }
                if (___isTestOn)
                {
                    ___testOnTime += Time.unscaledDeltaTime;
                }
                else
                {
                    ___testOffTime += Time.unscaledDeltaTime;
                }

                return false;
            }
        }

        



    }
}
