using HarmonyLib;

namespace BepInEx_UShoot_Plugin.Patch
{
    internal class mSBKInputManager
    {
        /// <summary>
        /// Override START keys at start screen
        /// </summary>
        [HarmonyPatch(typeof(SBKInputManager), "GetPlayerKeyDown")]
        class GetPlayerKeyDown
        {
            static bool Prefix(ref bool __result, ID i_ID)
            {
                __result = UShoot_Plugin.PlayerControllers[(int)i_ID].GetButtonDown("START");
                return false;
            }
        }

        /// <summary>
        /// Not used by the game ???
        /// </summary>

        /*[HarmonyPatch(typeof(SBKInputManager), "GetLeftKeyDown")]
        class GetLeftKeyDown
        {
            static bool Prefix(ref bool __result)
            {
                __result = DemulShooter_Plugin.MenuUp_Key.GetButtonDown();
                return false;
            }
        }
        [HarmonyPatch(typeof(SBKInputManager), "GetLeftKeyUp")]
        class GetLeftKeyUp
        {
            static bool Prefix(ref bool __result)
            {
                __result = DemulShooter_Plugin.MenuUp_Key.GetButtonUp();
                return false;
            }
        }
        [HarmonyPatch(typeof(SBKInputManager), "GetRightKeyDown")]
        class GetRightKeyDown
        {
            static bool Prefix(ref bool __result)
            {
                __result = DemulShooter_Plugin.MenuDown_Key.GetButtonDown();
                return false;
            }
        }
        [HarmonyPatch(typeof(SBKInputManager), "GetRightKeyUp")]
        class GetRightKeyUp
        {
            static bool Prefix(ref bool __result)
            {
                __result = DemulShooter_Plugin.MenuDown_Key.GetButtonUp();
                return false;
            }
        }
        [HarmonyPatch(typeof(SBKInputManager), "GetSelectKeyDown")]
        class GetSelectKeyDown
        {
            static bool Prefix(ref bool __result)
            {
                __result = DemulShooter_Plugin.MenuSelect_Key.GetButtonUp();
                return false;
            }
        }*/
    }
}
