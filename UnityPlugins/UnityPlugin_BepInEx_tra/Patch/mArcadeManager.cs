using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using HarmonyLib;
using UnityEngine;

namespace BepInEx_UShoot_Plugin
{
    class mArcadeManager
    {
        [HarmonyPatch(typeof(ArcadeManager), "CheckIOBoard")]
        class CheckIOBoard
        {
            static bool Prefix()
            {
                //DemulShooter_Plugin.MyLogger.LogMessage("ArcadeManager.CheckIOBoard()");
                return false;
            }
        }

        [HarmonyPatch(typeof(ArcadeManager), "GunMalfunctionCheck")]
        class GunMalfunctionCheck
        {
            static bool Prefix()
            {
                //DemulShooter_Plugin.MyLogger.LogMessage("ArcadeManager.GunMalfunctionCheck()");
                return false;
            }
        }

        //Removes the error screen
        [HarmonyPatch(typeof(ArcadeManager), "PushErrorPopup")]
        class PushErrorPopup
        {
            static bool Prefix()
            {
                //DemulShooter_Plugin.MyLogger.LogMessage("ArcadeManager.PushErrorPopup()");
                return false;
            }
        }

        /// <summary>
        /// Changing resolution if needed
        /// </summary>
        [HarmonyPatch(typeof(ArcadeManager), "Start")]
        class Start
        {
            static bool Prefix()
            {
                if (UShoot_Plugin.ForceResolution)
                    Screen.SetResolution(UShoot_Plugin.ScreenWidth, UShoot_Plugin.ScreenHeight, UShoot_Plugin.Fullscreen);
                return true;
            }
        }
    }
}
