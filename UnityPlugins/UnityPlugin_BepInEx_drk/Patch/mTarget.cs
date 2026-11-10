using HarmonyLib;
using UnityEngine;

namespace BepInEx_UShoot_Plugin.Patch
{
    internal class mTarget
    {
        /// <summary>
        /// Change the crosshair position for both players
        /// 
        /// Old bhack version:
        /// The crosshair needs to be on an item in menu to activate the item, it's not just the rect drawing
        /// Putting the RectTransform to not active is goosd to remove crosshair in-game, but can't validate in menu
        /// changing the localscale to (0,0,0) make it disappear and still active to clicks !
        /// 
        /// New hack version:
        /// Check if we are in Level Select screen (== IsLevelChosen is false), if not we can put the crosshairs AND the lasers away from sight
        /// at the same time
        [HarmonyPatch(typeof(Target), "OnTurretPosition")]
        class OnTurretPosition
        {
            static bool Prefix(int player, ref Vector3 viewportPosition, Target __instance)
            {
                //Plugin.myLogger.LogMessage("mTarget.OnTurretPosition() : player=" + player.ToString() + ", viewportPosition=" + viewportPosition.ToString());
                /*if (DemulShooter_Plugin.CrossHairVisibility == false)
                {
                    RectTransform r = __instance.RectTransform;
                    r.localScale = new Vector3(0, 0, 0);
                }*/
                if (!UShoot_Plugin.CrossHairVisibility && UShoot_Plugin.IsLevelChosen)
                    viewportPosition = new Vector3(-2.0f, -2.0f, 0);
                return true;
            }
        }
    }
}
