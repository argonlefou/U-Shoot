using HarmonyLib;

namespace BepInEx_UShoot_Plugin.Patch
{
    class mLEDManager
    {
        /// <summary>
        /// Disable COM polling to search for LEDs
        /// </summary>
        [HarmonyPatch(typeof(LEDManager), "Awake")]
        class Awake
        {
            static bool Prefix()
            {
                return false;
            }
        }
    }
}
