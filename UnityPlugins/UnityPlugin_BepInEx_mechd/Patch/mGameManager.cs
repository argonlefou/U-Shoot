using HarmonyLib;
using UnityEngine;

namespace BepInEx_UShoot_Plugin.Patch
{
    class mGameManager
    {
        /// <summary>
        /// Change resolution
        /// </summary>
        [HarmonyPatch(typeof(GameMain), "Start")]
        class Start
        {
            static void Postfix()
            {
                UShoot_Plugin.MyLogger.LogMessage("GameMain.Start()");
                if (UShoot_Plugin.ForceResolution)
                    Screen.SetResolution(UShoot_Plugin.ScreenWidth, UShoot_Plugin.ScreenHeight, UShoot_Plugin.Fullscreen);
            }
        }
    }
}
