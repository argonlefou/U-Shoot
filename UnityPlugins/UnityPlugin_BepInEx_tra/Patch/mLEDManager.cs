using HarmonyLib;

namespace BepInEx_UShoot_Plugin.Patch
{
    /// <summary>
    /// Prevent connection search on COM port
    /// </summary>
    internal class mLEDManager
    {
        [HarmonyPatch(typeof(LEDManager), "Awake")]
        class Awake
        {
            static bool Prefix()
            {
                UShoot_Plugin.MyLogger.LogMessage("LEDManager.Awake()");
                return false;
            }
        }        
    }
}
