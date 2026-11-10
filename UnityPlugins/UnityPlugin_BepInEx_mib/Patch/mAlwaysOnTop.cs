using HarmonyLib;

namespace BepInEx_UShoot_Plugin.Patch
{
    internal class mAlwaysOnTop
    {
        /// <summary>
        /// Deactivating the attempt of pinning the game window, it's messing with other windows and I don't like that
        /// </summary>
        [HarmonyPatch(typeof(AlwaysOnTop), "AssignTopmostWindow")]
        class AssignTopmostWindow
        {
            static bool Prefix(string WindowTitle, bool MakeTopmost)
            {
                UShoot_Plugin.MyLogger.LogMessage("AlwaysOnTop.AssignTopmostWindow(): WindowTitle=" + WindowTitle + ", MakeTopMost=" + MakeTopmost);
                return false;
            }
        }
    }
}
