using HarmonyLib;
using UnityEngine;

namespace BepInEx_UShoot_Plugin.Patch
{
    class mBaseGun
    {
        /// <summary>
        /// Returning shoot point coordinates for InGame
        /// Coordinates are in range [-1.0f ; 1.0f];
        /// </summary>
        [HarmonyPatch(typeof(MVSDK.BaseGun), "GetGunPos")]
        class GetGunPos
        {
            static bool Prefix(MVSDK.BaseGun __instance, ref Vector2 __result, int gun_id)
            {
                if (gun_id >= 0 && gun_id < __instance.MaxGun)
                {
                    Vector2 v = UShoot_Plugin.PlayerControllers[gun_id].GetAimingPosition();

                    v.x = v.x / (float)Screen.width;
                    v.y = v.y / (float)Screen.height;

                    __result = v;
                }
                else
                    __result = MVSDK.BaseGun.InvalidPoint;

                return false;
            }
        }

        /// <summary>
        /// Returning shoot point coordinates for InGame
        /// Coordinates are in range [WindowWidth ; WindowHeight]
        /// UnityEngine.Screen is returning Window size
        /// </summary>
        [HarmonyPatch(typeof(MVSDK.BaseGun), "GetGunRealPixPos")]
        class GetGunRealPixPos
        {
            static bool Prefix(MVSDK.BaseGun __instance, ref Vector2 __result, int gun_id)
            {
                if (gun_id >= 0 && gun_id < __instance.MaxGun)
                {
                    __result = UShoot_Plugin.PlayerControllers[gun_id].GetAimingPosition();
                }
                else
                    __result = MVSDK.BaseGun.InvalidPoint;

                return false;
            }
        }
    }
}
