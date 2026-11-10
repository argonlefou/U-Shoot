using HarmonyLib;

namespace BepInEx_UShoot_Plugin.Patch
{
    class mGameBeginUIController
    {
        /// <summary>
        /// Removing mouse click on Title Screen to start game
        /// </summary>
        [HarmonyPatch(typeof(GameBeginUIController), "Update")]
        class Update
        {
            static bool Prefix()
            {                
                return false;
            }
        }
    }
}
