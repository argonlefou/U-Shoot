using HarmonyLib;
using UnityEngine;

namespace BepInEx_UShoot_Plugin.Patch
{
    class mGunPointBase
    {
        /// <summary>
        /// Set CursorPosition based on mouse position
        /// Work for All players at once
        /// </summary>
        [HarmonyPatch(typeof(GunPointBase), "LateUpdate")]
        class LateUpdate
        {
            static bool Prefix(GunPointBase __instance, PlayerType ___playerType)
            {
                if (!UShoot_Plugin.CrossHairVisibility)
                {
                    __instance.transform.localPosition = new Vector3(-1000f, -600f, 0f);
                }
                else
                {
                    //Crosshair is hardcoded on 1920x1080 bounds, whereas base inputs depends on the window size
                    Vector2 v = GUNSDK.BaseGun.Instance.FrameData[(int)___playerType].ScreenPoint;
                    v.x = v.x * 1920.0f / Screen.width;
                    v.y = v.y * 1080.0f / Screen.height;
                    __instance.mRectTF.anchoredPosition = v;
                }
                return false;
            }
        }
    }
}
