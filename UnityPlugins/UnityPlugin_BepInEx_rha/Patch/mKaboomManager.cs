using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using KaboomOutput;
using UnityEngine;
using UnityPlugin_BepInEx.Core;

namespace BepInEx_UShoot_Plugin
{
    class mKaboomManager
    {
        /// <summary>
        /// Use this funtion to generate DemulShooter custom recoil output
        /// </summary>
        [HarmonyPatch(typeof(KaboomManager), "StartMotorPwm")]
        class StartMotorPwm
        {
            static bool Prefix(KaboomOutput.MotorPwmId MotorId, byte un8PwmDutyCycle, ushort iMsDuration = 144)
            {
                //UnityEngine.Debug.Log("KaboomManager.StartMotorPwm() : MotorId=" + MotorId.ToString() + ", un8PwmDutyCycle=" + un8PwmDutyCycle + ", iMsDuration=" + iMsDuration);         
                if (MotorId.ToString().Contains("P1"))
                    UShoot_Plugin.OutputData.Recoil[0] = 1;
                else if (MotorId.ToString().Contains("P2"))
                    UShoot_Plugin.OutputData.Recoil[1] = 1;
                else if (MotorId.ToString().Contains("P3"))
                    UShoot_Plugin.OutputData.Recoil[2] = 1;
                else if (MotorId.ToString().Contains("P4"))
                    UShoot_Plugin.OutputData.Recoil[3] = 1;

                return true;
            }
        }

        /// <summary>
        /// Use this to intercept Light ON status
        /// </summary>
        [HarmonyPatch(typeof(KaboomManager), "SetPlayerLight")]
        class SetPlayerLight
        {
            static bool Prefix(OutputPlayerLight i_Mask)
            {
                //UnityEngine.Debug.Log("KaboomManager.SetPlayerLight() : i_Mask=" + i_Mask.ToString());
                if (i_Mask == OutputPlayerLight.OUTPUT_P1)
                    UShoot_Plugin.OutputData.StartLamp[0] = 1;
                else if (i_Mask == OutputPlayerLight.OUTPUT_P2)
                    UShoot_Plugin.OutputData.StartLamp[1] = 1;
                else if (i_Mask == OutputPlayerLight.OUTPUT_P3)
                    UShoot_Plugin.OutputData.StartLamp[2] = 1;
                else if (i_Mask == OutputPlayerLight.OUTPUT_P4)
                    UShoot_Plugin.OutputData.StartLamp[3] = 1;
                else if (i_Mask == OutputPlayerLight.OUTPUT_ALL)
                {
                    UShoot_Plugin.OutputData.StartLamp[0] = 1;
                    UShoot_Plugin.OutputData.StartLamp[1] = 1;
                    UShoot_Plugin.OutputData.StartLamp[2] = 1;
                    UShoot_Plugin.OutputData.StartLamp[3] = 1;
                }

                return true;
            }
        }

        /// <summary>
        /// Use this to intercept Light OFF status
        /// </summary>
        [HarmonyPatch(typeof(KaboomManager), "ClearPlayerLight")]
        class ClearPlayerLight
        {
            static bool Prefix(OutputPlayerLight i_Mask)
            {
                //UnityEngine.Debug.Log("KaboomManager.ClearPlayerLight() : i_Mask=" + i_Mask.ToString());
                if (i_Mask == OutputPlayerLight.OUTPUT_P1)
                    UShoot_Plugin.OutputData.StartLamp[0] = 0;
                else if (i_Mask == OutputPlayerLight.OUTPUT_P2)
                    UShoot_Plugin.OutputData.StartLamp[1] = 0;
                else if (i_Mask == OutputPlayerLight.OUTPUT_P3)
                    UShoot_Plugin.OutputData.StartLamp[2] = 0;
                else if (i_Mask == OutputPlayerLight.OUTPUT_P4)
                    UShoot_Plugin.OutputData.StartLamp[3] = 0;
                else if (i_Mask == OutputPlayerLight.OUTPUT_ALL)
                {
                    UShoot_Plugin.OutputData.StartLamp[0] = 0;
                    UShoot_Plugin.OutputData.StartLamp[1] = 0;
                    UShoot_Plugin.OutputData.StartLamp[2] = 0;
                    UShoot_Plugin.OutputData.StartLamp[3] = 0;
                }

                return true;
            }
        }

        /// <summary>
        /// Changing some default keyboard controls
        /// </summary>
        [HarmonyPatch(typeof(KaboomManager), "Update")]
        class Update
        {
            static bool Prefix(KaboomManager __instance, KaboomAdrioFxPlusTwoFeeders ___m_KaboomAdrioFxPlusTwoFeeders, KaboomAdrioFxPlusFourFeeders ___m_KaboomAdrioFxPlusFourFeeders)
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    Application.Quit();
                    mQuit.Quit(__instance);
                    Process.GetCurrentProcess().Kill();
                }
                if (UShoot_Plugin.PlayerControllers[0].GetButtonDown("COIN"))
                {
                    mKaboomOnCoinEvent.KaboomOnCoinEvent(__instance, 1, 0, 0, 0);
                }
                if (UShoot_Plugin.PlayerControllers[1].GetButtonDown("COIN"))
                {
                    mKaboomOnCoinEvent.KaboomOnCoinEvent(__instance, 0, 1, 0, 0);
                }
                if (UShoot_Plugin.PlayerControllers[2].GetButtonDown("COIN"))
                {
                    mKaboomOnCoinEvent.KaboomOnCoinEvent(__instance, 0, 0, 1, 0);
                }
                if (UShoot_Plugin.PlayerControllers[3].GetButtonDown("COIN"))
                {
                    mKaboomOnCoinEvent.KaboomOnCoinEvent(__instance, 0, 0, 0, 1);
                }
                if (UShoot_Plugin.GameController.GetButtonDown("TEST"))
                {
                    mKaboomOnKeyboardEvent.KaboomOnKeyboardEvent(__instance, KaboomButton.Button.BUTTON_OPERATOR_PRESS);
                }
                else if (UShoot_Plugin.GameController.GetButtonUp("TEST"))
                {
                    mKaboomOnKeyboardEvent.KaboomOnKeyboardEvent(__instance, KaboomButton.Button.BUTTON_OPERATOR_RELEASE);
                }
                if (UShoot_Plugin.GameController.GetButtonDown("MENU_UP"))
                {
                    mKaboomOnKeyboardEvent.KaboomOnKeyboardEvent(__instance, KaboomButton.Button.BUTTON_UP_PRESS);
                }
                else if (UShoot_Plugin.GameController.GetButtonUp("MENU_UP"))
                {
                    mKaboomOnKeyboardEvent.KaboomOnKeyboardEvent(__instance, KaboomButton.Button.BUTTON_UP_RELEASE);
                }
                if (UShoot_Plugin.GameController.GetButtonDown("MENU_SELECT"))
                {
                    mKaboomOnKeyboardEvent.KaboomOnKeyboardEvent(__instance, KaboomButton.Button.BUTTON_SELECT_PRESS);
                }
                else if (UShoot_Plugin.GameController.GetButtonUp("MENU_SELECT"))
                {
                    mKaboomOnKeyboardEvent.KaboomOnKeyboardEvent(__instance, KaboomButton.Button.BUTTON_SELECT_RELEASE);
                }
                if (UShoot_Plugin.GameController.GetButtonDown("MENU_DOWN"))
                {
                    mKaboomOnKeyboardEvent.KaboomOnKeyboardEvent(__instance, KaboomButton.Button.BUTTON_DOWN_PRESS);
                }
                else if (UShoot_Plugin.GameController.GetButtonUp("MENU_DOWN"))
                {
                    mKaboomOnKeyboardEvent.KaboomOnKeyboardEvent(__instance, KaboomButton.Button.BUTTON_DOWN_RELEASE);
                }
                               
                if (___m_KaboomAdrioFxPlusTwoFeeders != null)
                {
                    ___m_KaboomAdrioFxPlusTwoFeeders.Update();
                }
                else if (___m_KaboomAdrioFxPlusFourFeeders != null)
                {
                    ___m_KaboomAdrioFxPlusFourFeeders.Update();
                }

                return false;
            }
        }

        [HarmonyPatch(typeof(KaboomManager), "Quit")]
        class mQuit
        {
            [HarmonyReversePatch]
            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void Quit(object instance)
            {
                //Used to call the private method    
            }
        }

        [HarmonyPatch(typeof(KaboomManager), "KaboomOnCoinEvent")]
        class mKaboomOnCoinEvent
        {
            [HarmonyReversePatch]
            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void KaboomOnCoinEvent(object instance, ushort aUn16CoinCount1, ushort aUn16CoinCount2, ushort aUn16CoinCount3, ushort aUn16CoinCount4)
            {
                //Used to call the private method    
            }
        }

        [HarmonyPatch(typeof(KaboomManager), "KaboomOnKeyboardEvent")]
        class mKaboomOnKeyboardEvent
        {
            [HarmonyReversePatch]
            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void KaboomOnKeyboardEvent(object instance, KaboomButton.Button aKey)
            {
                //Used to call the private method    
            }
        }
    }
}
