using HarmonyLib;

namespace BepInEx_UShoot_Plugin
{
    class mtest_screen
    {
        [HarmonyPatch(typeof(test_screen), "Start")]
        class Start
        {
            static bool Prefix()
            {
                UShoot_Plugin.MyLogger.LogWarning("test_screen.Start()");
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
