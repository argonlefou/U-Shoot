using HarmonyLib;
using UnityEngine;

namespace BepInEx_UShoot_Plugin.Patch
{
    class mCursorManager
    {
        /// <summary>
        /// GunCorsor Width & Height must be 1920*1080, whatever the screen size is (or crosshair/impact will not be able to cover all screen)
        /// Initialy based on Camera width, but if screen is smaller than 1920p, causing issue
        /// </summary>
        [HarmonyPatch(typeof(CursorManager), "InitGunCursor")]
        class InitGunCursor
        {
            static void Postfix(CursorManager __instance, ref int ___Width, ref int ___Height)
            {
                ___Width = 1920;
                ___Height = 1080;
                UShoot_Plugin.MyLogger.LogMessage("CursorManager.InitGunCursor(): Width=" + ___Width + ", Height=" + ___Height);
            }
        }

        /// <summary>
        /// Called by Gun.UpdateGunCorsor()
        /// CursorManager is hardcoded to be displayed in a range 0-1920 and 0-1080
        /// But Gun bullets are fired in the real screen size range, so we need to change the value returned here so that the bullets are aligned with the 2D impact
        /// </summary>
        [HarmonyPatch(typeof(CursorManager), "GetCunSorPos")]
        class GetCunSorPos
        {
            static bool Prefix(CursorManager __instance, int id, Vector2[] ___GunScreenPoint, ref Vector2 __result, int ___Width, ref int ___Height)
            {
                float fx = ___GunScreenPoint[id].x * (float)UShoot_Plugin.ScreenWidth / (float)___Width;
                float fy = ___GunScreenPoint[id].y * (float)UShoot_Plugin.ScreenHeight / (float)___Height;
                __result = new Vector2(fx, fy);
                return false;
            }
        }
        
    }
}
