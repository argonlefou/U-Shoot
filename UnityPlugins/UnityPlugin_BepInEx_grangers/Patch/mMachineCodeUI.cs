using HarmonyLib;

namespace BepInEx_UShoot_Plugin.Patch
{
    class mMachineCodeUI
    {
        [HarmonyPatch(typeof(MachineCodeUI), "CheckCode")]
        class CheckCode
        {
            static bool Prefix()
            {
                UShoot_Plugin.MyLogger.LogWarning("MachineCodeUI.CheckCode(): " + SecuritySystem.Instance.CheckTimes);
                SecuritySystem.Instance.CheckSuccess();
                return false;
            }
        }
    }
}
