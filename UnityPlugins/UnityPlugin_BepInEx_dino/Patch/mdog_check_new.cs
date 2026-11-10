using HarmonyLib;

namespace BepInEx_UShoot_Plugin
{
    class mdog_check_new
    {
        /// <summary>
        /// Dongle Removal
        /// </summary>
        [HarmonyPatch(typeof(dog_check_new), "is_dog_ok_hareware")]
        class is_dog_ok_hareware
        {
            static bool Prefix(ref bool __result)
            {
                UShoot_Plugin.MyLogger.LogMessage("dog_check_new.is_dog_ok_hareware()");
                __result = true;
                return false;
            }
        }
    }
}
