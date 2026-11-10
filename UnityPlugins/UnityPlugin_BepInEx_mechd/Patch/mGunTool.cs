using HarmonyLib;
using UnityEngine;

namespace BepInEx_UShoot_Plugin.Patch
{
    class mGunTool
    {
        /// <summary>
        /// Fixing Aiming at screen
        /// </summary>
        [HarmonyPatch(typeof(GunTool), "GetMyPosition")]
        class GetMyPosition
        {
            static bool Prefix(ref Vector3 __result, Vector2 vec, float ____Width, float ____Height)
            {
                float x = ____Width * ((vec.x  + 1f) / 2f);
                float y = ____Height * ((vec.y  + 1f) / 2f);
                __result = new Vector3(x, y, 0f);
                return false;
            }
        }
    }
}
