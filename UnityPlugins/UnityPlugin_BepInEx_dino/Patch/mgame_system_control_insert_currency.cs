using System;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace BepInEx_UShoot_Plugin.Patch
{
    internal class mgame_system_control_insert_currency
    {
        /// <summary>
        /// Change COIN keys
        /// Changing the KeysCodes in the list does not really work (??)
        /// So instead, clearing the list and manually adding coin before the procedure starts
        /// </summary>
        [HarmonyPatch(typeof(game_system_control_insert_currency), "update_work")]
        class Update_work
        {
            static bool Prefix(game_system_control_insert_currency __instance)
            {
                __instance.myinsert_keycodes.Clear();
                for (int i = 0; i < UShoot_Plugin.MAX_PLAYERS; i++)
                {
                    if (UShoot_Plugin.PlayerControllers[i].GetRawButtonDown("COIN"))
                    {
                        game_system_control_insert_currency.my_coin_count_s[i]++;
                        if (game_system_control_insert_currency.my_coin_count_s[i] >= 256)
                        {
                            game_system_control_insert_currency.my_coin_count_s[i] -= 256;
                        }
                    }
                }
                
                return true;
            }
        }
    }
}
