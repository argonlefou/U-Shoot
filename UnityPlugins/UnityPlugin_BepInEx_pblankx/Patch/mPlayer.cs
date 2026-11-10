using HarmonyLib;
using UnityEngine;

namespace BepInEx_UShoot_Plugin
{
    class mPlayer
    {
        /// <summary>
        /// Intercept the shoot event to create Output to DemulShooter
        /// </summary>
        [HarmonyPatch(typeof(Player), "shoot")]
        class Shoot
        {
            static bool Prefix(Vector2 positionOnScreen, Player __instance)
            {
                UShoot_Plugin.MyLogger.LogMessage("mPlayer.Shoot() => Name: " + __instance.getName());

                //Filtering Active player state, because this function is called in Demo mode also
                if (__instance.playerData.state == PlayerData.PlayerState.Active)
                {
                    if (__instance.getName() == "P1")
                        UShoot_Plugin.OutputData.Recoil[0] = 1;
                    else if (__instance.getName() == "P2")
                        UShoot_Plugin.OutputData.Recoil[1] = 1;
                }

                return true;
            }
        }
    }
}
