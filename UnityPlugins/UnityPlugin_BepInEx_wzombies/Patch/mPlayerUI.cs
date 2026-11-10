using HarmonyLib;

namespace BepInEx_UShoot_Plugin.Patch
{
    class mPlayerUI
    {
        /// <summary>
        /// Retrieving Players Lifes
        /// There is no health bar (it's only Timing) but player can have multiple Lifes
        /// </summary>
        [HarmonyPatch(typeof(PlayerUI), "Update")]
        class Update
        {
            static void Postfix(PlayerUI __instance)
            {
                //DemulShooter_Plugin.MyLogger.LogMessage("PlayerUI.update() : ID=" + __instance.PlayerID + ", Life=" + __instance.OnceLife);
                UShoot_Plugin.OutputData.Life[__instance.PlayerID] = (byte)__instance.OnceLife;
            }
        }
    }
}
