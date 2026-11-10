using HarmonyLib;
using UnityEngine;
using System;

namespace BepInEx_UShoot_Plugin
{
    class mGame_Device_data_gun_core
    {
        /// <summary>
        /// Sometimes, activating P2 after P2 change the mouse from P1 to P2, thus disabing P1 (why ??)
        /// Easiest thing to do is to always return true here
        /// </summary>
        [HarmonyPatch(typeof(Game_Device_data_gun_core), "device_is_can_work")]
        class device_is_can_work
        {
            static bool Prefix(Game_Device_data_gun_core __instance, int device_num, ref bool __result)
            {
                //DemulShooter_Plugin.MyLogger.LogMessage("mGame_Device_data_gun_core.device_is_can_work() => player: " + __instance.mygame_player_num + ", myis_use_mouse:  " + __instance.myis_use_mouse + ", result: " + __result);
                __result = true;
                return false;
            }
        }


        /// <summary>
        /// Called by the game to get mouse coordinates to handle Guns
        /// </summary>
        [HarmonyPatch(typeof(Game_Device_data_gun_core), "get_gun_pos_by_mouse")]
        class get_gun_pos_by_mouse
        {
            static bool Prefix(Game_Device_data_gun_core __instance, ref Vector3 __result)
            {
                int PlayerNum = __instance.get_player_num() - 1;
                Vector3 mouse_pos = UShoot_Plugin.PlayerControllers[PlayerNum].GetAimingPosition();
                //DemulShooter_Plugin.MyLogger.LogMessage("mGame_Device_data_gun_core.get_gun_pos_by_mouse() => player: " + __instance.mygame_player_num + ", Value: " + mouse_pos.ToString());
                __result = zhichi_hanshu_pos.change_mouse_positon_to_gun_pos(mouse_pos);
                return false;
            }
        }

        /// <summary>
        /// Same thing for Fire triggering
        /// </summary>
        [HarmonyPatch(typeof(Game_Device_data_gun_core), "is_gun_fire")]
        class is_gun_fire
        {
            static bool Prefix(Game_Device_data_gun_core __instance, ref bool __result)
            {
                __result = false;
                int PlayerNum = __instance.get_player_num() - 1;
                if (__instance.is_single_fire())
                {
                    if (UShoot_Plugin.PlayerControllers[PlayerNum].GetButtonDown("TRIGGER"))
                    {
                        __result = true;
                    }
                }
                else
                {
                    if (UShoot_Plugin.PlayerControllers[PlayerNum].GetButton("TRIGGER"))
                    {
                        __result = true;
                    }
                }
                return false;
            }
        }
    }
}
