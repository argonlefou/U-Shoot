using HarmonyLib;

namespace BepInEx_UShoot_Plugin.Patch
{
    class mBootSequence
    {
        /// <summary>
        /// Changing resolution
        /// </summary>
        [HarmonyPatch(typeof(BootSequence), "Start")]
        class Start
        {
            static void Postfix()
            {
                if (UShoot_Plugin.ForceResolution)
                {
                    UShoot_Plugin.MyLogger.LogMessage("BootSequence.Start(): Changind Screen resolution to " + UShoot_Plugin.ScreenWidth + "x" + UShoot_Plugin.ScreenHeight + ", Fullscreen=" + UShoot_Plugin.Fullscreen);
                    UnityEngine.Screen.SetResolution(UShoot_Plugin.ScreenWidth, UShoot_Plugin.ScreenHeight, UShoot_Plugin.Fullscreen);
                }
            }
        }
    }
}
