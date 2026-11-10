using HarmonyLib;
using UnityEngine;

namespace BepInEx_UShoot_Plugin.Patch
{
    class mInputManager
    {
        [HarmonyPatch(typeof(InputManager), "Start")]
        class Start
        {
            static void Postfix(InputManager __instance, InputMode[] ___m_inputMode)
            {
                UShoot_Plugin.MyLogger.LogMessage("InputManager.Start(): " + ___m_inputMode[0].ToString() + ", " + ___m_inputMode[1].ToString());
            }
        }

        /// <summary>
        /// Set both player to mouse mode
        /// By default, without shell, it would be AUTOPLAY mode
        /// </summary>
        [HarmonyPatch(typeof(InputManager), "ChangeInputMode")]
        class ChangeInputMode
        {
            static bool Prefix(InputManager __instance, ref InputMode[] ___m_inputMode)
            {
                UShoot_Plugin.MyLogger.LogMessage("InputManager.ChangeInputMode(): " + ___m_inputMode[0].ToString() + ", " + ___m_inputMode[1].ToString());
                ___m_inputMode[0] = InputMode.MOUSE;
                ___m_inputMode[1] = InputMode.MOUSE;                
                return true;
            }
        }

        /// <summary>
        /// Replacing BUTTON data
        /// </summary>
        [HarmonyPatch(typeof(InputManager), "GetGameButtonOn")]
        class GetGameButtonOn
        {
            static bool Prefix(InputType it, ref bool __result)
            {
                switch (it)
                {
                    case InputType.P1L:
                        __result = UShoot_Plugin.PlayerControllers[0].GetButton("TRIGGER_L");
                        break;
                    case InputType.P1R:
                        __result = UShoot_Plugin.PlayerControllers[0].GetButton("TRIGGER_R");
                        break;
                    case InputType.P2L:
                        __result = UShoot_Plugin.PlayerControllers[1].GetButton("TRIGGER_L");
                        break;
                    case InputType.P2R:
                        __result = UShoot_Plugin.PlayerControllers[1].GetButton("TRIGGER_R");
                        break;
                    case InputType.P1START:
                        __result = UShoot_Plugin.PlayerControllers[0].GetButton("START");
                        break;
                    case InputType.P2START:
                        __result = UShoot_Plugin.PlayerControllers[1].GetButton("START");
                        break;
                    default:
                        __result = false;
                        break;
                }
                return false;
            }
        }

        [HarmonyPatch(typeof(InputManager), "GetGameButtonPress")]
        class GetGameButtonPress
        {
            static bool Prefix(InputType it, ref bool __result)
            {
                switch (it)
                {
                    case InputType.P1L:
                        __result = UShoot_Plugin.PlayerControllers[0].GetRawButtonDown("TRIGGER_L");
                        break;
                    case InputType.P1R:
                        __result = UShoot_Plugin.PlayerControllers[0].GetRawButtonDown("TRIGGER_R");
                        break;
                    case InputType.P2L:
                        __result = UShoot_Plugin.PlayerControllers[1].GetRawButtonDown("TRIGGER_L");
                        break;
                    case InputType.P2R:
                        __result = UShoot_Plugin.PlayerControllers[1].GetRawButtonDown("TRIGGER_R");
                        break;
                    case InputType.P1START:
                        __result = UShoot_Plugin.PlayerControllers[0].GetRawButtonDown("START");
                        break;
                    case InputType.P2START:
                        __result = UShoot_Plugin.PlayerControllers[1].GetRawButtonDown("START");
                        break;
                    default:
                        __result = false;
                        break;
                }
                return false;
            }
        }

        [HarmonyPatch(typeof(InputManager), "GetGameButtonRelease")]
        class GetGameButtonRelease
        {
            static bool Prefix(InputType it, ref bool __result)
            {
                switch (it)
                {
                    case InputType.P1L:
                        __result = UShoot_Plugin.PlayerControllers[0].GetButtonUp("TRIGGER_L");
                        break;
                    case InputType.P1R:
                        __result = UShoot_Plugin.PlayerControllers[0].GetButtonUp("TRIGGER_R");
                        break;
                    case InputType.P2L:
                        __result = UShoot_Plugin.PlayerControllers[1].GetButtonUp("TRIGGER_L");
                        break;
                    case InputType.P2R:
                        __result = UShoot_Plugin.PlayerControllers[1].GetButtonUp("TRIGGER_R");
                        break;
                    case InputType.P1START:
                        __result = UShoot_Plugin.PlayerControllers[0].GetButtonUp("START");
                        break;
                    case InputType.P2START:
                        __result = UShoot_Plugin.PlayerControllers[1].GetButtonUp("START");
                        break;
                    default:
                        __result = false;
                        break;
                }
                return false;
            }
        }

        /// <summary>
        /// Replacing Axis data
        /// </summary>
        [HarmonyPatch(typeof(InputManager), "GetPosition")]
        class GetPosition
        {
            static bool Prefix(PID pid, ref Vector3 __result)
            {
                if (pid == PID.PID_ONE)
                {
                    __result = UShoot_Plugin.PlayerControllers[0].GetAimingPosition();
                }
                else if (pid == PID.PID_TWO)
                {
                    __result = UShoot_Plugin.PlayerControllers[1].GetAimingPosition();
                }
                return false;
            }
        }
         
        
    }
}
