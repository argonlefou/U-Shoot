using System.Collections;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BepInEx_UShoot_Plugin.Patch
{
    class mLogoSceneUI
    {
        /// <summary>
        /// Inserting Init code when that Scene is starting
        /// </summary>
        [HarmonyPatch(typeof(LogoSceneUI), "Start")]
        class Start
        {
            static bool Prefix(ref IEnumerator __result)
            {
                UShoot_Plugin.MyLogger.LogMessage("LogoSceneUI.Start()");
                __result = mEnumerator();
                return false;
            }

            private static IEnumerator mEnumerator()
            {
                GameData.GamePlayerCount = (MaxPlayer)UShoot_Plugin.PlayerCount;
                Cursor.visible = false;

                if (UShoot_Plugin.ForceResolution)
                    Screen.SetResolution(UShoot_Plugin.ScreenWidth, UShoot_Plugin.ScreenHeight, UShoot_Plugin.Fullscreen);

                yield return null;

                SceneManager.LoadScene(GameData.Init);
                yield break;
            }
        }
    }
}
