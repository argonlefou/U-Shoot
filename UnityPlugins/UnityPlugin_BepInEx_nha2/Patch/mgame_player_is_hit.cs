using HarmonyLib;

namespace BepInEx_UShoot_Plugin
{
    /// <summary>
    /// Find when a player is hit for custom outputs
    /// </summary>
    class mgame_player_is_hit
    {
        [HarmonyPatch(typeof(game_player_is_hit), "player_is_hit")]
        class player_is_hit
        {
            static bool Prefix(game_base game_hit_obj1, game_player_is_hit __instance)
            {
                //DemulShooter_Plugin.MyLogger.LogWarning("mgame_player_is_hit.player_is_hit() => userNum: " + __instance.get_user_num()  + ", life: " + __instance.get_life());
                if (__instance.player_status_is_can_be_hit())                
                    UShoot_Plugin.OutputData.Damaged[__instance.get_user_num() - 1] = 1;

                return true;
            }
        }

    }
}
