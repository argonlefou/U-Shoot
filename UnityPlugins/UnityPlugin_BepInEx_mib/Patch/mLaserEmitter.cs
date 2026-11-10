using HarmonyLib;
using UnityEngine;

namespace BepInEx_UShoot_Plugin.Patch
{
    internal class mLaserEmitter
    {
        /// <summary>
        /// Removing Laser pointer from gun
        /// </summary>
        [HarmonyPatch(typeof(LaserEmitter), "OnEnable")]
        class OnEnable
        {
            static void Postfix(LaserEmitter __instance)
            {
                if (!UShoot_Plugin.GunVisibility)
                    __instance.GetComponent<LineRenderer>().enabled = false;
            }
        }
        [HarmonyPatch(typeof(LaserEmitter), "Start")]
        class Start
        {
            static void Postfix(LaserEmitter __instance)
            {
                if (!UShoot_Plugin.GunVisibility)
                    __instance.GetComponent<LineRenderer>().enabled = false;
            }
        }
        [HarmonyPatch(typeof(LaserEmitter), "Update")]
        class Update
        {
            static void Postfix(LaserEmitter __instance)
            {
                if (!UShoot_Plugin.GunVisibility)
                    __instance.GetComponent<LineRenderer>().enabled = false;
            }
        }
    }
}
