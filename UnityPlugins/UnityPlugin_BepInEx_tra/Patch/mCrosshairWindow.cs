using HarmonyLib;
using UnityEngine;

namespace BepInEx_UShoot_Plugin
{
    class mCrosshairWindow
    {
        [HarmonyPatch(typeof(CrosshairWindow), "CrosshairMove")]
        class CrosshairMove
        {
            /// <summary>
            /// Original function is calculating the pixel position of the Crosshair with what seems to be a fixed range [1920x1080]
            /// This is creating an offset between the float real position of the aim [0, 1] and the crosshair drawing
            /// </summary>
            /// <returns></returns>
            static bool Prefix(Vector3 i_Viewport, ID i_PlayerID, CrosshairWindow __instance)
            {
                //DemulShooter_Plugin.MyLogger.LogMessage("---- CrosshairWindow.CrosshairMove(), i_PlayerID=" + i_PlayerID.ToString() + ", i_Viewport=" + i_Viewport.ToString());
                if (SBK.SceneSingleton<GameplayCamera>.Exists())
                {
                    for (int i = 0; i < __instance.m_Crosshairs.Length; i++)
                    {
                        if (__instance.m_Crosshairs[i].m_ID == i_PlayerID)                            
                        {
                            Vector3 v = UShoot_Plugin.PlayerControllers[i].GetAimingPosition();
                            v.x = v.x / (float)Screen.width * 1920.0f;
                            v.y = v.y / (float)Screen.height * 1080.0f;

                           __instance.m_Crosshairs[i].m_CrosshairTr.anchoredPosition = v;

                            if (!UShoot_Plugin.CrossHairVisibility)
                                __instance.m_Crosshairs[i].m_CrosshairTr.localScale = new Vector3();
                            else
                                __instance.m_Crosshairs[i].m_CrosshairTr.localScale = new Vector3(1.0f, 1.0f, 0);
                            break;
                        }
                    }
                    return false;
                }
                return false;
            }
        }
    }
}
