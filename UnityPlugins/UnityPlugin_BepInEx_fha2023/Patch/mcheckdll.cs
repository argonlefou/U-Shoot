using HarmonyLib;
using UnityEngine;

namespace BepInEx_UShoot_Plugin.Patch
{
    public class mcheckdll
    {
        /// <summary>
        /// Insert here custom screen resolution
        /// </summary>
        [HarmonyPatch(typeof(checkdll), "Awake")]
        class Awake
        {
            static void Postfix()
            {
                if (UShoot_Plugin.ForceResolution)
                {
                    Screen.SetResolution(UShoot_Plugin.ScreenWidth, UShoot_Plugin.ScreenHeight, UShoot_Plugin.Fullscreen);
                }
            }
        }
    }
}
