using HarmonyLib;
using UnityEngine;


namespace BepInEx_USoot_Plugin.Patch
{
    internal class mFireFlowSettings
    {
        /// <summary>
        /// Creating custom recoil data in the setting
        /// </summary>
        [HarmonyPatch(typeof(FireFlowSettings), "DisplayGunShootPoint")]
        class DisplayGunShootPoint
        {
            static bool Prefix(int id, Vector2Int pos)
            {
                //DemulShooter_Plugin.MyLogger.LogMessage("FireFlowSettings.DisplayGunShootPoint(): id=" + id);
                UShoot_Plugin.OutputData.Recoil = 1;
                return true;
            }
        }
    }
}
