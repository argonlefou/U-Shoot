using HarmonyLib;

namespace BepInEx_UShoot_Plugin
{
    class mGlobalData
    {
        /// <summary>
        /// Set the "crosshairvisible" flag as we want
        /// </summary>
        [HarmonyPatch(typeof(GlobalData), MethodType.Constructor)]
        class Ctor
        {
            static void Postfix(ref bool ___isCrosshairVisible)
            {
                ___isCrosshairVisible = UShoot_Plugin.CrossHairVisibility;
                UShoot_Plugin.MyLogger.LogMessage("mGlobalData() => isCrosshairVisible: " + ___isCrosshairVisible);
            }
        }
    }
}
