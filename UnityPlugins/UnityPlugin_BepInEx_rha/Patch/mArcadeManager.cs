using HarmonyLib;
using SixenseCore;

namespace BepInEx_UShoot_Plugin
{
    class mArcadeManager
    {
        [HarmonyPatch(typeof(ArcadeManager), "CheckIOBoard")]
        class CheckIOBoard
        {
            static bool Prefix()
            {
                UShoot_Plugin.MyLogger.LogMessage("ArcadeManager.CheckIOBoard()");
                return false;         
            }
        }

        [HarmonyPatch(typeof(ArcadeManager), "GunFatalError")]
        class GunFatalError
        {
            static bool Prefix(LocID i_Loc, Hardware i_Hardware, int i_Error)
            {
                UShoot_Plugin.MyLogger.LogMessage("ArcadeManager.GunFatalError()");
                return false;
            }
        }

        [HarmonyPatch(typeof(ArcadeManager), "GunMalfunctionCheck")]
        class GunMalfunctionCheck
        {
            static bool Prefix()
            {
                UShoot_Plugin.MyLogger.LogMessage("ArcadeManager.GunMalfunctionCheck()");
                return false;
            }
        }

        /// <summary>
        /// No !
        /// </summary>
        [HarmonyPatch(typeof(ArcadeManager), "KillGame")]
        class KillGame
        {
            static bool Prefix()
            {
                UShoot_Plugin.MyLogger.LogMessage("ArcadeManager.KillGame()");
                return false;
            }
        }

        /// <summary>
        /// Removes the error screen
        /// </summary>
        [HarmonyPatch(typeof(ArcadeManager), "PushErrorPopup")]
        class PushErrorPopup
        {
            static bool Prefix()
            {
                UShoot_Plugin.MyLogger.LogMessage("ArcadeManager.PushErrorPopup()");
                return false;
            }
        }
    }
}
