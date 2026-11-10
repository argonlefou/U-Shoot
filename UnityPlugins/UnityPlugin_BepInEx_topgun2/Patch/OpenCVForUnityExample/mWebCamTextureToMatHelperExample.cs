using HarmonyLib;
using UnityEngine;

namespace BepInEx_UShoot_Plugin.Patch
{
    internal class mWebCamTextureToMatHelperExample
    {
        /// <summary>
        /// Replacing the Camera data by our custom data
        /// This is replaced by Input.mousePosition if "PcCursor: 1" in game.json config file
        /// </summary>
        [HarmonyPatch(typeof(OpenCVForUnityExample.WebCamTextureToMatHelperExample), "GetSrcPos")]
        class GetSrcPos
        {
            static bool Prefix(int i, ref Vector2 __result)
            {
                //UShoot_Plugin.MyLogger.LogMessage("OpenCVForUnityExample.WebCamTextureToMatHelperExample.GetSrcPos(): i= " + i);
                __result = UShoot_Plugin.PlayerControllers[i].GetAimingPosition();
                return false;
            }
        }

        /// <summary>
        /// Replacing the Camera data by our custom data
        /// Hardware data is in the range [-Screen/2 ; +Screen/2]
        /// </summary>
        [HarmonyPatch(typeof(OpenCVForUnityExample.WebCamTextureToMatHelperExample), "GetHPos")]
        class GetHPos
        {
            static bool Prefix(int i, ref Vector2 __result)
            {
                //UShoot_Plugin.MyLogger.LogMessage("OpenCVForUnityExample.WebCamTextureToMatHelperExample.GetHPos(): i= " + i);
                Vector2 v = UShoot_Plugin.PlayerControllers[i].GetAimingPosition();
                v.x -= (float)Screen.width / 2.0f;
                v.y -= (float)Screen.height / 2.0f;
                __result = v;
                return false;
            }
        }
    }
}
