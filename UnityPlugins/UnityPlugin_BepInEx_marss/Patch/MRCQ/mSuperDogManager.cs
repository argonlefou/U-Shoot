using HarmonyLib;

namespace BepInEx_UShoot_Plugin.Patch
{
    class mSuperDogManager
    {
        /// <summary>
        /// Removing dongle check
        /// </summary>
        [HarmonyPatch(typeof(MRCQ.SuperDogManager), "DoCheckKey")]
        class Update
        {
            static bool Prefix(int featureId, MRCQ.SuperDogManager.EncryptionArray enArr, ref bool __result)
            {
                UShoot_Plugin.MyLogger.LogMessage("MRCQ.SuperDogManager.DoCheckKey()");
                __result = true;
                return false;
            }
        }
    }
}
