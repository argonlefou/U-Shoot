using System.Collections;
using HarmonyLib;

namespace BepInEx_UShoot_Plugin.Patch
{
    internal class mBaseGun
    {
        [HarmonyPatch(typeof(GUNSDK.BaseGun), "Start")]
        class Start
        {
            static bool Prefix(ref IEnumerator __result)
            {
                UShoot_Plugin.MyLogger.LogMessage("GUNSDK.BaseGun.Start()");
                __result = EmptyEnumerator();
                return false;
            }

            private static IEnumerator EmptyEnumerator()
            {
                yield break;
            }
        }

        [HarmonyPatch(typeof(GUNSDK.BaseGun), "StartNewGun")]
        class StartNewGun
        {
            static bool Prefix(ref IEnumerator __result)
            {
                UShoot_Plugin.MyLogger.LogMessage("GUNSDK.BaseGun.StartNewGun()");
                __result = EmptyEnumerator();
                return false;
            }

            private static IEnumerator EmptyEnumerator()
            {
                yield break;
            }
        }



        //[HarmonyPatch(typeof(GUNSDK.BaseGun), "Awake")]
        //class Awake
        //{
        //    static bool Prefix(GUNSDK.BaseGun __instance, bool ___m_bStartWork)
        //    {
        //        DemulShooter_Plugin.MyLogger.LogMessage("GUNSDK.BaseGun.Awake()");
        //        ___m_bStartWork = true;
        //        return false;
        //    }
        //}

        //[HarmonyPatch(typeof(GUNSDK.BaseGun), "ChangeState")]
        //class ChangeState
        //{
        //    static bool Prefix(int index, bool state)
        //    {
        //        DemulShooter_Plugin.MyLogger.LogMessage("GUNSDK.BaseGun.ChangeState(): index=" + index + ", state=" + state);
        //        return true;
        //    }
        //}

        ///// <summary>
        ///// Returning shoot point coordinates for InGame
        ///// Coordinates are in range [-1.0f ; 1.0f];
        ///// </summary>
        //[HarmonyPatch(typeof(GUNSDK.BaseGun), "GetGunPos")]
        //class GetGunPos
        //{
        //    static bool Prefix(GUNSDK.BaseGun __instance, ref Vector2 __result, int gun_id)
        //    {
        //        DemulShooter_Plugin.MyLogger.LogMessage("GUNSDK.BaseGun.GetGunPos()");
        //        if (gun_id >= 0 && gun_id < __instance.MaxGun)
        //        {
        //            Vector2 v = new Vector2();
        //            v = DemulShooter_Plugin.PluginControllers[gun_id].GetAimingPosition();
        //            v.x = v.x / (float)Screen.width;
        //            v.y = v.y / (float)Screen.height;

        //            __result = v;
        //        }
        //        else
        //            __result = GUNSDK.BaseGun.InvalidPoint;

        //        return false;
        //    }
        //}

        ///// <summary>
        ///// Returning shoot point coordinates for InGame
        ///// Coordinates are in range [WindowWidth ; WindowHeight]
        ///// UnityEngine.Screen is returning Window size
        ///// </summary>
        //[HarmonyPatch(typeof(GUNSDK.BaseGun), "GetGunRealPixPos")]
        //class GetGunRealPixPos
        //{
        //    static bool Prefix(GUNSDK.BaseGun __instance, ref Vector2 __result, int gun_id)
        //    {
        //        DemulShooter_Plugin.MyLogger.LogMessage("GUNSDK.BaseGun.GetGunRealPixPos()");
        //        if (gun_id >= 0 && gun_id < __instance.MaxGun)
        //        {
        //            __result = DemulShooter_Plugin.PluginControllers[gun_id].GetAimingPosition();
        //        }
        //        else
        //            __result = GUNSDK.BaseGun.InvalidPoint;

        //        return false;
        //    }
        //}

        /// <summary>
        /// Null reference error inside
        /// </summary>
        [HarmonyPatch(typeof(GUNSDK.BaseGun), "ChangeCameraMode")]
        class ChangeCameraMode
        {
            static bool Prefix(GUNSDK.BaseGun __instance, GUNSDK.CameraMode newMode)
            {
                UShoot_Plugin.MyLogger.LogMessage("GUNSDK.BaseGun.ChangeCameraMode(): newMode = " + newMode.ToString());
                return false;
            }
        }
    }
}
