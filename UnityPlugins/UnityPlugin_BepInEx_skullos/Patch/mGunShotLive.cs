using HarmonyLib;

namespace BepInEx_UShoot_Plugin.Patch
{
    internal class mGunShotLive
    {
        /// <summary>
        /// Hooking to the result of the called function (each frame) will get our Recoil info
        /// </summary>
        [HarmonyPatch(typeof(GunShotLive), "ShotOnce")]
        class ShotOnce
        {
            static void Postfix(PlayerData PlayerData, bool __result)
            {
                if (__result)
                    UShoot_Plugin.OutputData.Recoil[PlayerData.PlayerID] = 1;
            }
        }
    }
}
