using HarmonyLib;

namespace BepInEx_UShoot_Plugin
{
    class mLifeTracker
    {
        /// <summary>
        /// Get life info
        /// </summary>
        [HarmonyPatch(typeof(LifeTracker), "Update")]
        class Update
        {
            static bool Prefix(int ___lifes)
            {
                UShoot_Plugin.OutputData.P1_Life = (byte)___lifes;                    
                return true;
            }
        }
    }
}
