using System.Diagnostics;
using System.Runtime.CompilerServices;
using HarmonyLib;
using KaboomButton;
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
                UShoot_Plugin.MyLogger.LogMessage("KaboomManager.StartMotorPwm(): MotorId=" + MotorId.ToString());
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
        /// Changing Key Inputs
        /// </summary>
        [HarmonyPatch(typeof(KaboomManager), "Update")]
        class Update
        {
            static bool Prefix(KaboomAdrioFxPlusTwoFeeders ___m_KaboomAdrioFxPlusTwoFeeders, KaboomAdrioFxPlusFourFeeders ___m_KaboomAdrioFxPlusFourFeeders, KaboomManager __instance)
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    Application.Quit();
                    mQuit.Quit(__instance);
                    Process.GetCurrentProcess().Kill();
                }

                //COINS
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

                //TEST MENU
                if (UShoot_Plugin.GameController.GetButtonDown("TEST"))
                {
                    mKaboomOnKeyboardEvent.KaboomOnKeyboardEvent(__instance, Button.BUTTON_OPERATOR_PRESS);
                }
                else if (UShoot_Plugin.GameController.GetButtonUp("TEST"))
                {
                    mKaboomOnKeyboardEvent.KaboomOnKeyboardEvent(__instance, Button.BUTTON_OPERATOR_RELEASE);
                }
                if (UShoot_Plugin.GameController.GetButtonDown("MENU_UP"))
                {
                    mKaboomOnKeyboardEvent.KaboomOnKeyboardEvent(__instance, Button.BUTTON_UP_PRESS);
                }
                else if (UShoot_Plugin.GameController.GetButtonUp("MENU_UP"))
                {
                    mKaboomOnKeyboardEvent.KaboomOnKeyboardEvent(__instance, Button.BUTTON_UP_RELEASE);
                }
                if (UShoot_Plugin.GameController.GetButtonDown("MENU_SELECT"))
                {
                    mKaboomOnKeyboardEvent.KaboomOnKeyboardEvent(__instance, Button.BUTTON_SELECT_PRESS);
                }
                else if (UShoot_Plugin.GameController.GetButtonUp("MENU_SELECT"))
                {
                    mKaboomOnKeyboardEvent.KaboomOnKeyboardEvent(__instance, Button.BUTTON_SELECT_RELEASE);
                }
                if (UShoot_Plugin.GameController.GetButtonDown("MENU_DOWN"))
                {
                    mKaboomOnKeyboardEvent.KaboomOnKeyboardEvent(__instance, Button.BUTTON_DOWN_PRESS);
                }
                else if (UShoot_Plugin.GameController.GetButtonUp("MENU_DOWN"))
                {
                    mKaboomOnKeyboardEvent.KaboomOnKeyboardEvent(__instance, Button.BUTTON_DOWN_RELEASE);
                }

                //START
                if (UShoot_Plugin.PlayerControllers[0].GetButtonDown("START"))
                {
                    mKaboomOnKeyboardEvent.KaboomOnKeyboardEvent(__instance, Button.BUTTON_PLAYER1_B1_PRESS);
                }
                else if (UShoot_Plugin.PlayerControllers[0].GetButtonUp("START"))
                {
                    mKaboomOnKeyboardEvent.KaboomOnKeyboardEvent(__instance, Button.BUTTON_PLAYER1_B1_RELEASE);
                }
                if (UShoot_Plugin.PlayerControllers[1].GetButtonDown("START"))
                {
                    mKaboomOnKeyboardEvent.KaboomOnKeyboardEvent(__instance, Button.BUTTON_PLAYER2_B1_PRESS);
                }
                else if (UShoot_Plugin.PlayerControllers[1].GetButtonUp("START"))
                {
                    mKaboomOnKeyboardEvent.KaboomOnKeyboardEvent(__instance, Button.BUTTON_PLAYER2_B1_RELEASE);
                }
                if (UShoot_Plugin.PlayerControllers[2].GetButtonDown("START"))
                {
                    mKaboomOnKeyboardEvent.KaboomOnKeyboardEvent(__instance, Button.BUTTON_PLAYER3_B1_PRESS);
                }
                else if (UShoot_Plugin.PlayerControllers[2].GetButtonUp("START"))
                {
                    mKaboomOnKeyboardEvent.KaboomOnKeyboardEvent(__instance, Button.BUTTON_PLAYER3_B1_RELEASE);
                }
                if (UShoot_Plugin.PlayerControllers[3].GetButtonDown("START"))
                {
                    mKaboomOnKeyboardEvent.KaboomOnKeyboardEvent(__instance, Button.BUTTON_PLAYER4_B1_PRESS);
                }
                else if (UShoot_Plugin.PlayerControllers[3].GetButtonUp("START"))
                {
                    mKaboomOnKeyboardEvent.KaboomOnKeyboardEvent(__instance, Button.BUTTON_PLAYER4_B1_RELEASE);
                }
                
                /*if (Input.GetKeyDown(KeyCode.F1))
                {
                    mGiveTicketsToFeeder.GiveTicketsToFeeder(__instance, 0, 12, true);
                }
                if (Input.GetKeyDown(KeyCode.F2))
                {
                    mGiveTicketsToFeeder.GiveTicketsToFeeder(__instance, 1, 12, true);
                }*/

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

        /*[HarmonyPatch(typeof(KaboomManager), "GiveTicketsToFeeder")]
        class mGiveTicketsToFeeder
        {
            [HarmonyReversePatch]
            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void GiveTicketsToFeeder(object instance, ushort iFeederID, ushort iTicket = 0, bool b_finalize = false)
            {
                //Used to call the private method    
            }
        }*/
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
    }
}
