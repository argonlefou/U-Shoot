using HarmonyLib;

namespace BepInEx_UShoot_Plugin.Patch
{
    class mPlayer
    {
        /// <summary>
        /// Intercept Damage for output
        /// </summary>
        [HarmonyPatch(typeof(Player), "PlayerHarm")]
        class PlayerHarm
        {
            static bool Prefix(Player __instance, int ___ID, int value)
            {
                //DemulShooter_Plugin.MyLogger.LogMessage("Player.Harm() : ID=" + ___ID);
                UShoot_Plugin.OutputData.Damaged[___ID] = 1;
                return true;
            }
        }

        /// <summary>
        /// Retrieving player Life
        /// </summary>
        [HarmonyPatch(typeof(Player), "Update")]
        class Update
        {
            static bool Prefix(Player __instance, int ___ID, float ___HP)
            {
                //DemulShooter_Plugin.MyLogger.LogMessage("Player.update() : ID=" + ___ID + ", HP=" + ___HP);
                UShoot_Plugin.OutputData.Life[___ID] = ___HP;
                return true;
            }
        }
    }
}
