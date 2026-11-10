using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using UnityEngine;

namespace BepInEx_UShoot_Plugin.Patch
{
    internal class mArcadeShowCursor
    {
        /// <summary>
        /// Using that call to insert Resolution Change
        /// </summary>
        [HarmonyPatch(typeof(ArcadeShowCursor), "OnEnter")]
        class OnEnter
        {
            static bool Prefix(ArcadeShowCursor __instance)
            {
                UShoot_Plugin.MyLogger.LogWarning("ArcadeShowCursor.Onenter()");
                __instance.showCursor = false;
                if (UShoot_Plugin.ForceResolution)
                    Screen.SetResolution(UShoot_Plugin.ScreenWidth, UShoot_Plugin.ScreenHeight, UShoot_Plugin.Fullscreen);
                return true;
            }
        }
    }
}
