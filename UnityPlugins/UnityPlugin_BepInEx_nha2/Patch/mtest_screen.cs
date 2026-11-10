using HarmonyLib;

namespace BepInEx_UShoot_Plugin
{
    class mtest_screen
    {
        /// <summary>
        /// Changing resolution when the game is opening it's window
        /// </summary>
        [HarmonyPatch(typeof(test_screen), "Start")]
        class start
        {
            static bool Prefix()
            {
                if (UShoot_Plugin.ForceResolution)
                {
                    UShoot_Plugin.MyLogger.LogMessage("test_screen.start() => Changing resolution to " + UShoot_Plugin.ScreenWidth + "x" + UShoot_Plugin.ScreenHeight + " fullscreen: " + UShoot_Plugin.Fullscreen);
                    UnityEngine.Screen.SetResolution(UShoot_Plugin.ScreenWidth, (int)UShoot_Plugin.ScreenHeight, UShoot_Plugin.Fullscreen);
                    return false;
                }
                return true;
            }
        }
    }
}
