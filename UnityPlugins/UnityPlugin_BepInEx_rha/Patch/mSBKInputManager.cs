using HarmonyLib;
using SBK;

namespace BepInEx_UShoot_Plugin.Patch
{
    internal class mSBKInputManager
    {
        /// <summary>
        /// change those keys too ?? (right, left, down, select ???)
        /// </summary>
        [HarmonyPatch(typeof(SBKInputManager), "GetRightKeyDown")]
        class GetRightKeyDown
        {
            static void Prefix()
            {
                UShoot_Plugin.MyLogger.LogMessage("SBKInputManager.GetRightKeyDown()");
            }
        }

        /// <summary>
        /// Changing default Start buttons Keys
        /// </summary>
        [HarmonyPatch(typeof(SBKInputManager), "GetPlayerKeyDown")]
        class GetPlayerKeyDown
        {
            static bool Prefix(ID i_ID, ref bool __result)
            {
                bool result;
                switch (i_ID)
                {
                    case ID.One:
                        result = UShoot_Plugin.PlayerControllers[0].GetButtonDown("START");
                        break;
                    case ID.Two:
                        result = UShoot_Plugin.PlayerControllers[1].GetButtonDown("START");
                        break;
                    case ID.Three:
                        result = UShoot_Plugin.PlayerControllers[2].GetButtonDown("START");
                        break;
                    case ID.Four:
                        result = UShoot_Plugin.PlayerControllers[3].GetButtonDown("START");
                        break;
                    default:
                        result = UShoot_Plugin.PlayerControllers[0].GetButtonDown("START");
                        break;
                }
                __result = result;
                return false;
            }
        }

        [HarmonyPatch(typeof(SBKInputManager), "GetActivePlayerKeyDown")]
        class GetActivePlayerKeyDown
        {
            static bool Prefix(ID i_ID, ref bool __result)
            {
                bool result;
                switch (i_ID)
                {
                    case ID.One:
                        result = UShoot_Plugin.PlayerControllers[0].GetButtonDown("START") && Singleton<PlayerManager>.Instance.CheckPlayerActive(ID.One);
                        break;
                    case ID.Two:
                        result = UShoot_Plugin.PlayerControllers[1].GetButtonDown("START") && Singleton<PlayerManager>.Instance.CheckPlayerActive(ID.Two);
                        break;
                    case ID.Three:
                        result = UShoot_Plugin.PlayerControllers[2].GetButtonDown("START") && Singleton<PlayerManager>.Instance.CheckPlayerActive(ID.Three);
                        break;
                    case ID.Four:
                        result = UShoot_Plugin.PlayerControllers[3].GetButtonDown("START") && Singleton<PlayerManager>.Instance.CheckPlayerActive(ID.Four);
                        break;
                    default:
                        result = UShoot_Plugin.PlayerControllers[0].GetButtonDown("START");
                        break;
                }
                __result = result;
                return false;
            }
        }
    }
}
