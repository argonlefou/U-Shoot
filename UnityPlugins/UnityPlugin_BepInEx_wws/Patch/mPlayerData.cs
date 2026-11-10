using HarmonyLib;

namespace BepInEx_UShoot_Plugin.Patch
{
    internal class mPlayerData
    {
        /// <summary>
        /// Get Damaged Output info
        /// </summary>
        [HarmonyPatch(typeof(PlayerData), "DelLife")]
        class Start
        {
            static bool Prefix(PlayerData __instance, int delNum)
            {
                //DemulShooter_Plugin.MyLogger.LogMessage("PlayerData.DelLife(): delNum="  + delNum);
                UShoot_Plugin.OutputData.Damaged[(int)__instance.playerTpye] = 1;
                return true;
            }
        }
    }
}
