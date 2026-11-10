using HarmonyLib;

namespace BepInEx_UShoot_Plugin
{
    class mgame_player
    {
        /// <summary>
        /// Use this call to generate Damaged event
        /// </summary>
        [HarmonyPatch(typeof(game_player), "is_hit")]
        class is_hit
        {
            static bool Prefix(game_player __instance)
            {
                UShoot_Plugin.MyLogger.LogWarning("game_player.is_hit() : Num=" + __instance.get_user_num());
                UShoot_Plugin.OutputData.Damaged[__instance.get_user_num() - 1] = 1;
                return true;
            }
        }

        /// <summary>
        /// USe this call to generate Recoil event
        /// </summary>
        [HarmonyPatch(typeof(game_player), "fashe")]
        class fashe
        {
            static bool Prefix(game_player __instance, game_base target, bool is_fire_air)
            {
                UShoot_Plugin.MyLogger.LogWarning("game_player.fashe() : Num=" + __instance.get_user_num());
                UShoot_Plugin.OutputData.Recoil[__instance.get_user_num() - 1] = 1;
                return true;
            }
        }
    }
}
