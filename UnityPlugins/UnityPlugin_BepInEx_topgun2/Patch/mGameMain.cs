using HarmonyLib;
using UnityEngine;

namespace BepInEx_UShoot_Plugin.Patch
{
    internal class mGameMain
    {
        /// <summary>
        /// Skip Dongle check
        /// </summary>
        [HarmonyPatch(typeof(GameMain), "CheckDogKey")]
        class CheckDogKey
        {
            static bool Prefix(bool ____IsDog)
            {
                UShoot_Plugin.MyLogger.LogMessage("GameMain.CheckDogKey()");
                ____IsDog = true;
                return false;
            }
        }

        /// <summary>
        /// Skip Dongle check
        /// </summary>
        [HarmonyPatch(typeof(GameMain), "CheckKey")]
        class CheckKey
        {
            static bool Prefix(ref bool __result)
            {
                UShoot_Plugin.MyLogger.LogMessage("GameMain.CheckKey()");
                __result = true;
                return false;
            }
        }

        /// <summary>
        /// Game is setting screen resolution here, so we can change it if needed
        /// </summary>
        [HarmonyPatch(typeof(GameMain), "Start")]
        class Start
        {
            static void Prefix(ref bool ____CheckDog)
            {
                UShoot_Plugin.MyLogger.LogMessage("GameMain.Start()");
            }
            static void Postfix(ref bool ____CheckDog)
            {
                UShoot_Plugin.MyLogger.LogMessage("GameMain.Start() POST, TotalPlayers=" + MonoSingleton<GameData>.Instance.TotalPlayers);
                ____CheckDog = false;

                if (UShoot_Plugin.ForceResolution)
                    Screen.SetResolution(UShoot_Plugin.ScreenWidth, UShoot_Plugin.ScreenHeight, UShoot_Plugin.Fullscreen);
            }
        }
    }
}
