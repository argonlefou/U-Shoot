using HarmonyLib;
using Virtuallyz.VRShooter.Characters.Players;

namespace BepInEx_UShoot_Plugin
{
    class mPlayer
    {
        /// <summary>
        /// Intercept dammage event to send output to Demulshooter
        /// </summary>
        [HarmonyPatch(typeof(Player), "OnDamageTaken")]
        class OnDamageTaken
        {
            static bool Prefix(Player __instance)
            {
                //DemulShooter_Plugin.MyLogger.LogMessage("Virtuallyz.VRShooter.Characters.Players.OnDamageTaken()");
                UShoot_Plugin.OutputData.Damaged[0] = 1;
                UShoot_Plugin.OutputData.Damaged[1] = 1;
                return true;
            }
        }
    }
}
