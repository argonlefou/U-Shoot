using HarmonyLib;
using UnityEngine;

namespace BepInEx_UShoot_Plugin
{
    class mGame_Device_data_gun_core
    {

        /// <summary>
        /// Force players 2,3,4 to be enabled when MOUSE controls is set
        /// </summary>
        [HarmonyPatch(typeof(Game_Device_data_gun_core), "device_is_can_work")]
        class device_is_can_work
        {
            static bool Prefix(int device_num, ref bool __result)
            {
                __result = true;
                return false;
            }
        }
        
        /// <summary>
        /// Replacing Axis values
        /// </summary>
        [HarmonyPatch(typeof(Game_Device_data_gun_core), "get_gun_pos")]
        class get_gun_pos
        {
            static bool Prefix(ref UnityEngine.Vector3 __result, game_player ___mygame_player, int ___mygame_player_num)
            {
                //DemulShooter_Plugin.MyLogger.LogMessage("Game_Device_data_gun_core.get_gun_pos(): Player Num=" + ___mygame_player_num);       
                Vector3 vTemp = UShoot_Plugin.PlayerControllers[___mygame_player_num].GetAimingPosition();
                __result = screen_info_lib.change_real_pos_to_edit_pos(vTemp);
                return false;
            }
        }

        /// <summary>
        /// Replacing Axis values
        /// </summary>
        [HarmonyPatch(typeof(Game_Device_data_gun_core), "is_gun_fire")]
        class is_gun_fire
        {
            static bool Prefix(ref bool __result, game_player ___mygame_player, int ___mygame_player_num, Game_Device_data_gun_core __instance)
            {
                //DemulShooter_Plugin.MyLogger.LogMessage("Game_Device_data_gun_core.get_gun_pos(): Player Num=" + ___mygame_player_num);                    
                __result = UShoot_Plugin.PlayerControllers[___mygame_player_num].GetRawButtonDown("TRIGGER");
                return false;
            }
        }
    }


}
