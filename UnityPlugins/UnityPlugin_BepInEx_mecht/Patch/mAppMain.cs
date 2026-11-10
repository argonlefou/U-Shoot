using HarmonyLib;

namespace BepInEx_UShoot_Plugin.Patch
{
    class mAppMain
    {
        /// <summary>
        /// Changing resolution
        /// </summary>
        [HarmonyPatch(typeof(AppMain), "Awake")]
        class Awake
        {
            static bool Prefix()
            {
                UShoot_Plugin.MyLogger.LogMessage("AppMain.Awake()");
                if (UShoot_Plugin.ForceResolution)
                {
                    UnityEngine.Screen.SetResolution(UShoot_Plugin.ScreenWidth, UShoot_Plugin.ScreenHeight, UShoot_Plugin.Fullscreen);
                    return false;
                }
                return true;
            }
        }
    }
}
