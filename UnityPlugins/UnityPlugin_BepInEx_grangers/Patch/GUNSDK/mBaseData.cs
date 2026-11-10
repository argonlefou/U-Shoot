using GUNSDK;
using HarmonyLib;
using UnityEngine;

namespace BepInEx_UShoot_Plugin.Patch
{
    public class mBaseData
    {
        /// <summary>
        /// Bottom-Left origin, screen coordinates
        /// Only for Crosshair display, can be used to hide them
        /// </summary>
        [HarmonyPatch(typeof(GUNSDK.BaseData), "get_ScreenPoint")]
        class get_ScreenPoint
        {
            static bool Prefix(ref Vector2 __result, BaseData __instance)
            {
                BaseGun instance = BaseGun.Instance;
                if (instance != null)
                {
                    for (int i = 0; i < instance.FrameData.Length; i++)
                    {
                        if (object.ReferenceEquals(instance.FrameData[i], __instance))
                        {
                            __result = UShoot_Plugin.PlayerControllers[i].GetAimingPosition();
                            return false;
                        }
                    }
                }

                __result = new Vector2(-1000.0f, -600.0f);
                return false;
            }
        }
        [HarmonyPatch(typeof(GUNSDK.BaseData), "get_ScreenPoint2")]
        class get_ScreenPoint2
        {
            static bool Prefix(ref Vector2 __result, BaseData __instance)
            {
                BaseGun instance = BaseGun.Instance;
                if (instance != null)
                {
                    for (int i = 0; i < instance.FrameData.Length; i++)
                    {
                        if (object.ReferenceEquals(instance.FrameData[i], __instance))
                        {
                            __result = UShoot_Plugin.PlayerControllers[i].GetAimingPosition();
                            return false;
                        }
                    }
                }

                __result = new Vector2(-1000.0f, -600.0f);
                return false;
            }
        }
    }

}
