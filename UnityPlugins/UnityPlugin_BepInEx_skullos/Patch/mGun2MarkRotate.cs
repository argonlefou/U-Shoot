using HarmonyLib;

namespace BepInEx_UShoot_Plugin.Patch
{
    internal class mGun2MarkRotate
    {
        /// <summary>
        /// Remove crosshair when Gun is displaying "rotating" crosshair
        /// </summary>
        [HarmonyPatch(typeof(Gun2MarkRotate), "OnEnable")]
        class OnEnable
        {
            static bool Prefix(Gun2MarkRotate __instance)
            {
                UShoot_Plugin.MyLogger.LogWarning("Gun2MarkRotate.OnEnable()");
                if (!UShoot_Plugin.CrossHairVisibility)
                {
                    __instance.transform.localScale = new UnityEngine.Vector3();
                    return false;
                }
                return true;
            }
        }
    }
}
