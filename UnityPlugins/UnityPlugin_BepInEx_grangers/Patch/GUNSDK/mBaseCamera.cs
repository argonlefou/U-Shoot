using HarmonyLib;

namespace BepInEx_UShoot_Plugin.Patch
{
    internal class mBaseCamera
    {
        [HarmonyPatch(typeof(GUNSDK.BaseCamera), "InitCamera")]
        class InitCamera
        {
            static bool Prefix(GUNSDK.BaseGun __instance, ref bool __result)
            {
                UShoot_Plugin.MyLogger.LogMessage("GUNSDK.BaseCamera.InitCamera(): Type=" + __instance.GetType().ToString());
                return true;
            }
            static void Postfix(GUNSDK.BaseGun __instance, ref bool __result)
            {
                UShoot_Plugin.MyLogger.LogMessage("GUNSDK.BaseCamera.InitCamera() POSTFIX: result=" + __result);
            }
        }
    }
}
