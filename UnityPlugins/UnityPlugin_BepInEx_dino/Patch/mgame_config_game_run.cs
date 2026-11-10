using HarmonyLib;

namespace BepInEx_UShoot_Plugin.Patch
{
    class mgame_config_game_run
    {
        /// <summary>
        /// Force MOUSE
        /// </summary>
        [HarmonyPatch(typeof(game_config_game_run), "check_for_control_way")]
        class check_for_control_way
        {
            static bool Prefix(ref all_begin.CONTROL_WAY __result)
            {
                __result = all_begin.CONTROL_WAY.MOUSE;
                return false;
            }
        }        
    }
}
