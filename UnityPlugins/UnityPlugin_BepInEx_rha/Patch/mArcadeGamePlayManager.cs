using HarmonyLib;

namespace BepInEx_UShoot_Plugin
{
    class mArcadeGamePlayManager
    {
        [HarmonyPatch(typeof(ArcadeGameplayManager), "LoadDemoMode")]
        class LoadDemoMode
        {
            static bool Prefix(WorldManager.WorldIndex i_World)
            {
                //DemulShooter_Plugin.MyLogger.LogMessage("mArcadeGameplayManager.LoadDemoMode()");
                UShoot_Plugin.IsDemoMode = true;
                return true;
            }
        }

        [HarmonyPatch(typeof(ArcadeGameplayManager), "OnDemoStateExit")]
        class OnDemoStateExit
        {
            static bool Prefix()
            {
                //DemulShooter_Plugin.MyLogger.LogMessage("mArcadeGameplayManager.OnDemoStateExit()");
                UShoot_Plugin.IsDemoMode = false;
                return true;
            }
        }

    }
}
