using HarmonyLib;

namespace BepInEx_UShoot_Plugin
{
    class mCheatsManager
    {
        /// <summary>
        /// Remove cheats using keyboard keys
        /// </summary>
        [HarmonyPatch(typeof(SBK.CheatsManager), "Update")]
        class Update
        {
            static bool Prefix()
            {
                return false;
            }
        }
    }
}
