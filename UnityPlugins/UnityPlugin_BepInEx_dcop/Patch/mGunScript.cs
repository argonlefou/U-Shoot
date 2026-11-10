using HarmonyLib;

namespace BepInEx_UShoot_Plugin
{
    class mGunScript
    {
        /// <summary>
        /// Get ammo info
        /// </summary>
        [HarmonyPatch(typeof(GunScript), "Update")]
        class Update
        {
            static bool Prefix(bool ___gunEnabled, int ___ammoCount)
            {
                if (___gunEnabled)
                    UShoot_Plugin.OutputData.P1_Ammo = (byte)___ammoCount;
                else
                    UShoot_Plugin.OutputData.P1_Ammo = 0;
                return true;
            }
        }
    }
}
