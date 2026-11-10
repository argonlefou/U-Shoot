using HarmonyLib;
using UnityEngine;

namespace BepInEx_UShoot_Plugin
{
    class mGame
    {
        /// <summary>
        /// Remove mouse cursor
        /// </summary>
        [HarmonyPatch(typeof(Game), "Awake")]
        class Awake
        {
            static void Postfix()
            {
                //DemulShooter_Plugin.MyLogger.LogMessage("Game.Awake()");
                UnityEngine.Cursor.visible = false;
            }
        }

        /// <summary>
        /// Force periodic reboot to always OFF
        /// </summary>
        [HarmonyPatch(typeof(Game), "ShouldPeriodicReboot")]
        class ShouldPeriodicReboot
        {
            static bool Prefix(ref bool __result)
            {
                //DemulShooter_Plugin.MyLogger.LogMessage("Game.ShouldPeriodicReboot()");
                __result = false;
                return false;
            }
        }

        [HarmonyPatch(typeof(Game), "Start")]
        class Start
        {
            static void Postfix()
            {
                if (UShoot_Plugin.ForceResolution)
                    Screen.SetResolution(UShoot_Plugin.ScreenWidth, UShoot_Plugin.ScreenHeight, UShoot_Plugin.Fullscreen);
            }
        }

        /// <summary>
        /// Force periodic reboot to always OFF
        /// </summary>
        //[HarmonyPatch(typeof(Game), "Start")]
        //class Start
        //{
        //    static bool Prefix()
        //    {
        //        //DemulShooter_Plugin.MyLogger.LogMessage("Game.Start()");
        //        if (DemulShooter_Plugin.ForceResolution)
        //            Screen.SetResolution(DemulShooter_Plugin.ScreenWidth, DemulShooter_Plugin.ScreenHeight, DemulShooter_Plugin.Fullscreen);                    

        //        return true;
        //    }
        //}


    }
}
