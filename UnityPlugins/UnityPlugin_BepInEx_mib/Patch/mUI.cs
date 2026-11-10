using HarmonyLib;

namespace BepInEx_UShoot_Plugin.Patch
{
    internal class mUI
    {
        /// <summary>
        /// Removing in-game crosshairs
        /// </summary>
        [HarmonyPatch(typeof(UI), "Update")]
        class Update
        {
            static bool Prefix(UI __instance)
            {
                if (!UShoot_Plugin.CrossHairVisibility)
                {
                    __instance.Crosshairs[0].enabled = false;
                    __instance.Crosshairs[1].enabled = false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(UI), "PulseReticule")]
        class PulseReticule
        {
            static bool Prefix(UI __instance, int Index)
            {
                if (!UShoot_Plugin.CrossHairVisibility)
                {
                   return false;
                }
                return true;
            }
        }
    }
}
