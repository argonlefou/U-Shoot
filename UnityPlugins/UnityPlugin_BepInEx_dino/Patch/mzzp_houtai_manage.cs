using HarmonyLib;

namespace BepInEx_UShoot_Plugin.Patch
{
    internal class mzzp_houtai_manage
    {
        /// <summary>
        /// Replacing the Menu keys
        /// </summary>
        [HarmonyPatch(typeof(zzp_houtai_manage), "check_for_zzp_houtai_control")]
        class check_for_zzp_houtai_control
        {
            static bool Prefix(zzp_houtai_manage __instance)
            {
                if (UShoot_Plugin.GameController.GetRawButtonDown("MENU_DOWN"))
                {
                    __instance.myzzp_houtai_control.add_key_work();
                }
                if (UShoot_Plugin.GameController.GetRawButtonDown("MENU_SELECT"))
                {
                    __instance.queding_work();
                }
                return false;
            }
        }
    }
}
