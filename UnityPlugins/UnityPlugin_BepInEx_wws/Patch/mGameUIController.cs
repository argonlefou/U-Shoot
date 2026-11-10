using HarmonyLib;
using UnityEngine;

namespace BepInEx_UShoot_Plugin.Patch
{
    class mGameUIController
    {
        /// <summary>
        /// Displaying cursors InGame
        /// </summary>
        [HarmonyPatch(typeof(GameUIController), "Update")]
        class Update
        {
            static bool Prefix(GameUIController __instance, bool ___isLoadEnd)
            {
                for (int i = 0; i < __instance.gunPoint.Length; ++i)
                {
                    if (GameData.GetPlayerData(i).CanShoot() && !___isLoadEnd && UShoot_Plugin.CrossHairVisibility)
                    {
                        Vector2 v = UShoot_Plugin.PlayerControllers[i].GetAimingPosition();

                        float fRatio = (float)Screen.width / Screen.height;
                        v.x = v.x / (float)Screen.width * 1920.0f;
                        v.y = v.y / (float)Screen.height * 1920.0f / fRatio;

                        __instance.gunPoint[i].mRectTF.anchoredPosition = v;
                    }
                    else
                        __instance.gunPoint[i].transform.localPosition = new Vector3(-1000f, -600f);
                }
                return false;
            }
        }
    }
}
