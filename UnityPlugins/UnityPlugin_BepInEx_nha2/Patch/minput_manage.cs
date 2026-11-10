using HarmonyLib;
using UnityEngine;
using UnityPlugin_BepInEx.Core;


namespace BepInEx_UShoot_Plugin
{
    /// <summary>
    /// Change Keyboard KEYS
    /// </summary>
    class minput_manage
    {
        [HarmonyPatch(typeof(input_manage), "init_input_obj_list")]
        class init_input_obj_list
        {
            static bool Prefix(input_manage __instance)
            {
                UShoot_Plugin.MyLogger.LogMessage("minput_manage.init_input_obj_list()");

                input_manage my_base = __instance;
                /*my_base.add_input_obj(new input_obj_start_game((KeyCode)UShoot_Plugin.PluginControllers[0].InputButtons[(int)PluginController.MyInputButtons.Start].KeyCode, 1));
                my_base.add_input_obj(new input_obj_start_game((KeyCode)UShoot_Plugin.PluginControllers[1].InputButtons[(int)PluginController.MyInputButtons.Start].KeyCode, 2));
                //my_base.add_input_obj(new input_obj_pass_movie(KeyCode.Space));
                my_base.add_input_obj(new input_obj_pass_movie((KeyCode)UShoot_Plugin.PluginControllers[0].InputButtons[(int)PluginController.MyInputButtons.Start].KeyCode));
                my_base.add_input_obj(new input_obj_pass_movie((KeyCode)UShoot_Plugin.PluginControllers[1].InputButtons[(int)PluginController.MyInputButtons.Start].KeyCode));
                my_base.add_input_obj(new input_obj_goto_houtai((KeyCode)UShoot_Plugin.Test_Key.KeyCode));
                my_base.add_input_obj(new input_obj_insert_coin((KeyCode)UShoot_Plugin.Coin_Key.KeyCode, 1));
                my_base.add_input_obj(new input_obj_insert_coin((KeyCode)UShoot_Plugin.Coin_Key.KeyCode, 2));*/
                /*my_base.add_input_obj(new input_obj_change_gun(KeyCode.Keypad1, 1));                
                my_base.add_input_obj(new input_obj_change_gun(KeyCode.Keypad2, 2));
                my_base.add_input_obj(new input_obj_xiaoqiang(KeyCode.Keypad3, 1));
                my_base.add_input_obj(new input_obj_xiaoqiang(KeyCode.Keypad4, 2));
                 my_base.add_input_obj(new input_obj_change_input_way(KeyCode.Y));*/

                /*if (!DemulShooter_Plugin.EnableInputHack)
                {
                    my_base.add_input_obj(new input_obj_change_bullet(KeyCode.Mouse1, 1));
                    my_base.add_input_obj(new input_obj_change_bullet(KeyCode.Mouse1, 2));
                    my_base.add_input_obj(new input_obj_big_power(KeyCode.Mouse2, 1));
                    my_base.add_input_obj(new input_obj_big_power(KeyCode.Mouse2, 2));
                }
                else
                {
                    //DemulShooter is handling
                }*/

                //my_base.add_input_obj(new input_obj_goto_houtai(KeyCode.Backspace));
                return false;                
            }
        }
    }
}
