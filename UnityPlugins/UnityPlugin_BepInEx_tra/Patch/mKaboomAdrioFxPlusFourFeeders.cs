using HarmonyLib;

namespace BepInEx_UShoot_Plugin.Patch
{
    internal class mKaboomAdrioFxPlusFourFeeders
    {
        /// <summary>
        /// Prevent the Lib to search and write to some devices on some computers
        /// May cause the game to hang
        /// </summary>
        [HarmonyPatch(typeof(KaboomAdrioFxPlusFourFeeders), "_Initialize")]
        class _Initialize
        {
            static bool Prefix()
            {
                UShoot_Plugin.MyLogger.LogMessage("KaboomAdrioFxPlusFourFeeders._Initialize()");
                return false;
            }
        }
    }
}
