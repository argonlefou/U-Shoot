using HarmonyLib;
using UnityEngine;

namespace BepInEx_UShoot_Plugin.Patch
{
    class mShowGamePointController
    {
        /// <summary>
        /// Displaying cursors on Level Select screen
        /// Looks like X must be in range [0-1920] whatever the resolution is
        /// And Y in range [0-Ymax], Ymax depending on the original screen ratio (based on 1920x1080)
        /// </summary>
        [HarmonyPatch(typeof(ShowGamePointController), "Update")]
        class Update
        {
            static bool Prefix(ShowGamePointController __instance)
            {
                for (int index = 0; index < __instance.gunPoint.Length; ++index)
                {                   
                    if (GameData.GetPlayerData(index).CanShoot() && UShoot_Plugin.CrossHairVisibility)
                    {
                        Vector2 v = UShoot_Plugin.PlayerControllers[index].GetAimingPosition();
                        
                        float fRatio = (float)Screen.width / Screen.height;
                        v.x = v.x / (float)Screen.width * 1920.0f;
                        v.y = v.y / (float)Screen.height * 1920.0f / fRatio;
                        __instance.gunPoint[index].mRectTF.anchoredPosition = v;
                    }
                    else
                        __instance.gunPoint[index].transform.localPosition = new Vector3(-1000f, -1000f);
                }
                return false;
            }
        }
    }
}
