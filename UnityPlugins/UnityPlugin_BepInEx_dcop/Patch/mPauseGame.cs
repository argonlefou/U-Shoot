using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BepInEx_UShoot_Plugin.Patch
{
    internal class mPauseGame
    {
        /// <summary>
        /// Replace the ESC key used to go in pause by TEST key
        /// </summary>
       [HarmonyPatch(typeof(PauseGame), "Update")]
        class Update2
        {
            static bool Prefix(PauseGame __instance, ref bool ___isPaused, ref bool ___isAttractmode, ref PointerEventData ___m_PointerEventData, ref EventSystem ___m_EventSystem, ref GraphicRaycaster ___m_Raycaster, ref Vector3 ___lastMousePosition)
            {
                if (UShoot_Plugin.GameController.GetButtonDown("TEST"))
                {
                    if (___isPaused)
                    {
                        if (__instance.pauseViews[0].activeSelf)
                        {
                            __instance.UnPause();
                        }
                        else
                        {
                            __instance.ResetPauseViews();
                        }
                    }
                    else if (!___isAttractmode)
                    {
                        __instance.Pause();
                    }
                }
                ___m_PointerEventData = new PointerEventData(___m_EventSystem);
                ___m_PointerEventData.position = Input.mousePosition;
                List<RaycastResult> list = new List<RaycastResult>();
                ___m_Raycaster.Raycast(___m_PointerEventData, list);
                GameObject[] array = GameObject.FindGameObjectsWithTag("PauseMenuButton");
                if (!__instance.usingArrows)
                {
                    __instance.CheckRayCast(list);
                }
                foreach (GameObject gameObject in array)
                {
                    if (__instance.selectedButton != null)
                    {
                        if (gameObject != __instance.selectedButton.gameObject)
                        {
                            gameObject.gameObject.GetComponentInChildren<UnityEngine.UI.Text>().color = new Color(0.8862745f, 0.02352941f, 0.007843138f, 1f);
                        }
                    }
                    else
                    {
                        gameObject.gameObject.GetComponentInChildren<UnityEngine.UI.Text>().color = new Color(0.8862745f, 0.02352941f, 0.007843138f, 1f);
                    }
                }
                if (Input.mousePosition != ___lastMousePosition && __instance.usingArrows)
                {
                    __instance.usingArrows = false;
                }
                ___lastMousePosition = Input.mousePosition;

                return false;
            }
        }
    }
}
