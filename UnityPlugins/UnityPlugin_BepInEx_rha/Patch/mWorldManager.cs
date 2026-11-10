using HarmonyLib;

namespace BepInEx_UShoot_Plugin.Patch
{
    internal class mWorldManager
    {
        /// <summary>
        /// [L] is supposed to bring the world manager screen
        /// </summary>
        [HarmonyPatch(typeof(WorldManager), "Update")]
        class Update
        {
            static bool Prefix()
            {
                return false;
            }
        }
    }
}
