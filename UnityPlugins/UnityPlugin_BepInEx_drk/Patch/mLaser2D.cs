using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;

// Old patch to remove only Laser sights
// When the no-crosshair patch was just changing crosshair Texture scale to 0

namespace BepInEx_UShoot_Plugin.Patch
{
    /// <summary>
    /// Removing the Lasers on screen
    /// </summary>
    internal class mLaser2D
    {
 /*       
        [HarmonyPatch(typeof(Laser2D), "Awake")]
        class Awake
        {
            static bool Prefix(Laser2D __instance)
            {
                if (!Drakon_Plugin.CrossHairVisibility)
                {
                    RectTransform r = mget_rect.Get_rect(__instance);
                    r.gameObject.SetActive(false);
                    return false;
                }
                else
                {
                    return true;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(Laser2D), "Start")]
        class Start
        {
            static bool Prefix(Laser2D __instance)
            {
                if (!Drakon_Plugin.CrossHairVisibility)
                {
                    RectTransform r = mget_rect.Get_rect(__instance);
                    r.gameObject.SetActive(false); 
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

        [HarmonyPatch(typeof(Laser2D), "OnTargetMoved")]
        class OnTargetMoved
        {
            static bool Prefix(Laser2D __instance, Target t, ref UnityEngine.Vector2 position)
            {
                if (!Drakon_Plugin.CrossHairVisibility)
                {
                    RectTransform r = mget_rect.Get_rect(__instance);
                    r.gameObject.SetActive(false);
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }        

        #region Dummy Calls

        [HarmonyPatch(typeof(Laser2D), "get_rect")]
        class mget_rect
        {
            [HarmonyReversePatch]
            [MethodImpl(MethodImplOptions.NoInlining)]
            public static RectTransform Get_rect(object instance)
            {
                //Used to call the private method
                return null;
            }
        }
        #endregion
 */    
    }

}
