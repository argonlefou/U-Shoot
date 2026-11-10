using HarmonyLib;

namespace BepInEx_UShoot_Plugin.Patch
{
    class mPlayerGunBase
    {
        /// <summary>
        /// Intercept event to create Recoil
        /// </summary>
        [HarmonyPatch(typeof(PlayerGunBase), "Shoot")]
        class Shoot
        {
            static bool Prefix(PlayerGunBase __instance)
            {
                if (__instance.Pid == PID.PID_ONE)
                    UShoot_Plugin.OutputData.Recoil[0] = 1;
                else if (__instance.Pid == PID.PID_TWO)
                    UShoot_Plugin.OutputData.Recoil[1] = 1;

                return true;
            }
        }

        /// <summary>
        /// Get ammo count
        /// </summary>
        [HarmonyPatch(typeof(PlayerGunBase), "Update")]
        class Update
        {
            static bool Prefix(PlayerGunBase __instance)
            {
                if (__instance.Pid == PID.PID_ONE)
                {
                    if (__instance.PGid == PlayerGunID.PGID_P1GL)
                        UShoot_Plugin.OutputData.AmmoGunL[0] = __instance.Magazine.BulletNum;
                    else if (__instance.PGid == PlayerGunID.PGID_P1GR)
                        UShoot_Plugin.OutputData.AmmoGunR[0] = __instance.Magazine.BulletNum;

                }
                else if (__instance.Pid == PID.PID_TWO)
                {
                    if (__instance.PGid == PlayerGunID.PGID_P2GL)
                        UShoot_Plugin.OutputData.AmmoGunL[1] = __instance.Magazine.BulletNum;
                    else if (__instance.PGid == PlayerGunID.PGID_P2GR)
                        UShoot_Plugin.OutputData.AmmoGunR[1] = __instance.Magazine.BulletNum;
                }

                return true;
            }
        }

        /// <summary>
        /// If Screen res has changed, there's an offset between the 2D reticle position (matching the screen size) and the real bullet position (matching original size)
        /// We can fix it here
        /// </summary>
        [HarmonyPatch(typeof(PlayerGunBase), "CalcShotSight")]
        class CalcShotSight
        {
            static bool Prefix(PlayerGunBase __instance, ref UnityEngine.Vector2 pos)
            {
                if (UShoot_Plugin.ForceResolution)
                {
                    pos.x = pos.x * (UShoot_Plugin.ORIGINAL_WIDTH / (float)UShoot_Plugin.ScreenWidth);
                    pos.y = pos.y * (UShoot_Plugin.ORIGINAL_WIDTH / (float)UShoot_Plugin.ScreenWidth);
                }
                return true;
            }
        }
        
    }
}
