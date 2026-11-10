using HarmonyLib;
using UnityEngine;

namespace BepInEx_UShoot_Plugin.Patch
{
    class mPlayerUIController
    {
        /// <summary>
        /// Send Ammo to DemulShooter
        /// </summary>
        [HarmonyPatch(typeof(PlayerUIController), "Update")]
        class Update
        {
            static bool Prefix(PlayerData ___playerData, int ___maxBullets, int ___bulletIndex)
            {
                try
                {
                    UShoot_Plugin.OutputData.Ammo[(int)___playerData.playerTpye] = (___maxBullets - ___bulletIndex);
                }
                catch { }
                return true;
            }
        }
    }
}
