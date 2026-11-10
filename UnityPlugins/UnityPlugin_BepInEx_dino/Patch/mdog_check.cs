using HarmonyLib;

namespace BepInEx_UShoot_Plugin
{
    class mdog_check
    {
        /// <summary>
        /// Dongle removal
        /// </summary>
        [HarmonyPatch(typeof(dog_check), MethodType.Constructor)]
        class dog_check_CCtor
        {
            static void Postfix(dog_check __instance)
            {
                UShoot_Plugin.MyLogger.LogMessage("dog_check.CCtor()");
                dog_check.myuse_dog_when_game_run = dog_check.USE_DOG_WHEN_GAME_RUN.NO;
            }
        }

        /// <summary>
        /// Dongle Removal
        /// </summary>
        [HarmonyPatch(typeof(dog_check), "dog_is_ok")]
        class dog_is_ok
        {
            static bool Prefix(ref bool __result)
            {
                UShoot_Plugin.MyLogger.LogMessage("dog_check.dog_is_ok()");
                __result = true;
                return false;
            }
        }
    }
}
