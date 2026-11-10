using System.IO;
using HarmonyLib;

namespace BepInEx_UShoot_Plugin.Patch
{
    internal class mHighscoreManager
    {
        /// <summary>
        /// Create local save folder if needed
        /// </summary>
        [HarmonyPatch(typeof(HighscoreManager), "Awake")]
        class Awake
        {
            static void Postfix(HighscoreManager __instance)
            {
                if (UShoot_Plugin.SaveToGameFolder)
                {
                    if (!Directory.Exists(__instance.path))
                    {
                        Directory.CreateDirectory(__instance.path);
                    }
                }
            }
        }
    }
}
