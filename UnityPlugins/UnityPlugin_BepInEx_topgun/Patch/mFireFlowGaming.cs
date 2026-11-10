using HarmonyLib;

namespace BepInEx_USoot_Plugin.Patch
{
    internal class mFireFlowGaming
    {
        /// <summary>
        /// Creating custom recoil data in the game
        /// </summary>
        [HarmonyPatch(typeof(FireFlowGaming), "OnFire")]
        class OnFire
        {
            static bool Prefix(int[] data)
            {
                //DemulShooter_Plugin.MyLogger.LogMessage("FireFlowGaming.OnFire(): player=" + data[0]);
                UShoot_Plugin.OutputData.Recoil = 1;
                return true;
            }
        }
    }
}
